using Nodus.RenderEngine.Vulkan.Extensions;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Nodus.RenderEngine.Vulkan.Sync;

public interface IVkSemaphore : IVkUnmanagedHook
{
    Semaphore WrappedSemaphore { get; }

    SemaphoreSubmitInfo CreateSubmitInfo(PipelineStageFlags2 stageMask);
}

public class VkSemaphore : VkObject, IVkSemaphore
{
    public Semaphore WrappedSemaphore { get; }

    private readonly IVkLogicalDevice device;
    
    public unsafe VkSemaphore(IVkContext vkContext, IVkLogicalDevice device) : base(vkContext)
    {
        this.device = device;
        
        var createInfo = new SemaphoreCreateInfo
        {
            SType = StructureType.SemaphoreCreateInfo
        };
        
        Context.Api.CreateSemaphore(device.WrappedDevice, in createInfo, null, out var semaphore)
            .TryThrow("Failed to create semaphore.");

        WrappedSemaphore = semaphore;
    }

    public SemaphoreSubmitInfo CreateSubmitInfo(PipelineStageFlags2 stageMask)
    {
        return new SemaphoreSubmitInfo
        {
            SType = StructureType.SemaphoreSubmitInfo,
            StageMask = stageMask,
            Semaphore = WrappedSemaphore,
            DeviceIndex = 0,
            Value = 1
        };
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            Context.Api.DestroySemaphore(device.WrappedDevice, WrappedSemaphore, null);
        }
        
        base.Dispose(disposing);
    }
}