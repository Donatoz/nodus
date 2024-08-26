using System.Reactive.Subjects;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan.Sync;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Rendering;

public interface IVkRenderPresenter : IDisposable
{
    IObservable<RenderPresentEvent> EventStream { get; }

    bool TryPrepareNewFrame(IVkSemaphore semaphore, uint frameIndex);
    void ProcessRenderQueue(Queue queue, IVkSemaphore semaphore, IVkFence fence);
    Framebuffer GetAvailableFramebuffer();
    IVkFence GetPresentationFence(uint frameIndex);
}

public enum RenderPresentEvent
{
    PresenterUpdated
}

public class VkSwapchainRenderPresenter : IVkRenderPresenter, IDisposable
{
    public IObservable<RenderPresentEvent> EventStream => eventSubject;

    private readonly IVkSwapChain swapChain;
    private readonly IVkKhrSurface surface;
    private readonly IVkRenderPass renderPass;

    private readonly Subject<RenderPresentEvent> eventSubject;
    private readonly IVkFence[] inFlightFences;

    private uint currentImageIndex;

    public VkSwapchainRenderPresenter(IVkContext context, IVkLogicalDevice device, IVkSwapChain swapChain,
        IVkKhrSurface surface, IVkRenderPass renderPass, int maxConcurrentFrames)
    {
        this.swapChain = swapChain;
        this.surface = surface;
        this.renderPass = renderPass;

        eventSubject = new Subject<RenderPresentEvent>();
        inFlightFences = new IVkFence[maxConcurrentFrames];

        for (var i = 0; i < maxConcurrentFrames; i++)
        {
            inFlightFences[i] = new VkFence(context, device, true);
        }
    }

    public bool TryPrepareNewFrame(IVkSemaphore semaphore, uint frameIndex)
    {
        var acquireResult = swapChain.AcquireNextImage(out var imgIndex, semaphore);
        
        if (acquireResult == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapChain(frameIndex);
            return false;
        }
        if (acquireResult != Result.Success && acquireResult != Result.SuboptimalKhr)
        {
            throw new Exception("Failed to acquire next swapchain image.");
        }

        currentImageIndex = imgIndex;
        return true;
    }

    public unsafe void ProcessRenderQueue(Queue queue, IVkSemaphore semaphore, IVkFence fence)
    {
        var swapChainKhr = swapChain.WrappedSwapChain;
        var signalSemaphore = semaphore.WrappedSemaphore;
        var imgIndex = currentImageIndex;

        var presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &signalSemaphore,
            SwapchainCount = 1,
            PSwapchains = &swapChainKhr,
            PImageIndices = &imgIndex
        };

        swapChain.SwapChainExtension.QueuePresent(queue, in presentInfo);
    }

    public Framebuffer GetAvailableFramebuffer()
    {
        if (swapChain.FrameBuffers == null)
        {
            throw new Exception("Failed to get available framebuffer: buffers were not initialized by the swapchain.");
        }

        return swapChain.FrameBuffers![currentImageIndex];
    }

    public IVkFence GetPresentationFence(uint frameIndex)
    {
        if (frameIndex >= inFlightFences.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(frameIndex));
        }
        
        return inFlightFences[frameIndex];
    }

    private void RecreateSwapChain(uint frameIndex)
    {
        inFlightFences
            .Where(x => x != inFlightFences[frameIndex])
            .ForEach(x => x.Await());
        
        swapChain.DiscardCurrentState();
        surface.Update();
        swapChain.RecreateState();
        swapChain.CreateFrameBuffers(renderPass.WrappedPass);
        
        eventSubject.OnNext(RenderPresentEvent.PresenterUpdated);
    }

    public void Dispose()
    {
        inFlightFences.DisposeAll();
        eventSubject.Dispose();
    }
}