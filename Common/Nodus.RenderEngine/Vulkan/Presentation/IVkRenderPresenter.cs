using System.Reactive.Subjects;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Rendering;
using Nodus.RenderEngine.Vulkan.Sync;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Nodus.RenderEngine.Vulkan.Presentation;

public interface IVkRenderPresenter : IDisposable
{
    IObservable<RenderPresentEvent> EventStream { get; }

    IVkTask CreateFramePreparationTask(uint frameIndex);
    IVkTask CreatePresentationTask(Queue queue);
    Framebuffer GetAvailableFramebuffer();
}

public enum RenderPresentEvent
{
    PresenterUpdated
}

public class VkSwapchainRenderPresenter : IVkRenderPresenter
{
    public IObservable<RenderPresentEvent> EventStream => eventSubject;

    private readonly IVkContext context;
    private readonly IVkSwapChain swapChain;
    private readonly IVkKhrSurface surface;
    private readonly IVkRenderPass renderPass;

    private readonly Subject<RenderPresentEvent> eventSubject;
    private readonly IVkSemaphore[] framePrepareReadySemaphores;

    private uint currentImageIndex;

    public VkSwapchainRenderPresenter(IVkContext context, IVkLogicalDevice device, IVkSwapChain swapChain,
        IVkKhrSurface surface, IVkRenderPass renderPass, int maxConcurrentFrames)
    {
        this.context = context;
        this.swapChain = swapChain;
        this.surface = surface;
        this.renderPass = renderPass;

        eventSubject = new Subject<RenderPresentEvent>();

        framePrepareReadySemaphores = new IVkSemaphore[maxConcurrentFrames];

        for (var i = 0u; i < maxConcurrentFrames; i++)
        {
            framePrepareReadySemaphores[i] = new VkSemaphore(context, device);
        }
    }
    
    public IVkTask CreateFramePreparationTask(uint frameIndex)
    {
        return new VkTask(PipelineStageFlags.ColorAttachmentOutputBit, (sm, st) => PrepareNewFrameImpl(sm, st, frameIndex))
        {
            SignalSemaphore = framePrepareReadySemaphores[frameIndex]
        };
    }

    private VkTaskResult PrepareNewFrameImpl(Semaphore[]? waitSemaphores, PipelineStageFlags[]? _, uint frameIndex)
    {
        waitSemaphores?.AwaitAll(context);
        
        var acquireResult = swapChain.AcquireNextImage(out var imgIndex, framePrepareReadySemaphores[frameIndex]);
        
        if (acquireResult == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapChain();
            return VkTaskResult.Failure;
        }
        if (acquireResult != Result.Success && acquireResult != Result.SuboptimalKhr)
        {
            throw new Exception("Failed to acquire next swapchain image.");
        }
        
        currentImageIndex = imgIndex;
        return VkTaskResult.Success;
    }
    
    public unsafe IVkTask CreatePresentationTask(Queue queue)
    {
        return new VkTask(PipelineStageFlags.None, (sm, _) =>
        {
            var swapChainKhr = swapChain.WrappedSwapChain;
            var imgIndex = currentImageIndex;

            fixed (Semaphore* pWaitSemaphores = sm)
            {
                var presentInfo = new PresentInfoKHR
                {
                    SType = StructureType.PresentInfoKhr,
                    WaitSemaphoreCount = (uint)(sm?.Length ?? 0),
                    PWaitSemaphores = pWaitSemaphores,
                    SwapchainCount = 1,
                    PSwapchains = &swapChainKhr,
                    PImageIndices = &imgIndex
                };

                swapChain.SwapChainExtension.QueuePresent(queue, in presentInfo);
            }

            return VkTaskResult.Success;
        });
    }

    public Framebuffer GetAvailableFramebuffer()
    {
        if (swapChain.FrameBuffers == null)
        {
            throw new Exception("Failed to get available framebuffer: buffers were not initialized by the swapchain.");
        }

        return swapChain.FrameBuffers![currentImageIndex];
    }

    private void RecreateSwapChain()
    {
        swapChain.DiscardCurrentState();
        surface.Update();
        swapChain.RecreateState();
        swapChain.CreateFrameBuffers(renderPass.WrappedPass);
        
        eventSubject.OnNext(RenderPresentEvent.PresenterUpdated);
    }

    public void Dispose()
    {
        eventSubject.Dispose();
        framePrepareReadySemaphores.DisposeAll();
    }
}