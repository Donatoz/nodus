using Nodus.RenderEngine.Vulkan.Extensions;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Sync;

public interface IVkFence : IVkUnmanagedHook
{
    Fence WrappedFence { get; }

    void Await(ulong timeOut = ulong.MaxValue);
    void Reset();
}

public unsafe class VkFence : VkObject, IVkFence
{
    public Fence WrappedFence { get; }

    private readonly IVkLogicalDevice device;
    
    public VkFence(IVkContext vkContext, IVkLogicalDevice device, bool signaled = false) : base(vkContext)
    {
        this.device = device;
        
        var createInfo = new FenceCreateInfo
        {
            SType = StructureType.FenceCreateInfo,
            Flags = signaled ? FenceCreateFlags.SignaledBit : FenceCreateFlags.None
        };
        
        Context.Api.CreateFence(device.WrappedDevice, in createInfo, null, out var fence)
            .TryThrow("Failed to create fence.");

        WrappedFence = fence;
    }

    public void Await(ulong timeOut = ulong.MaxValue)
    {
        Context.Api.WaitForFences(device.WrappedDevice, 1, WrappedFence, Vk.True, timeOut);
    }

    public void Reset()
    {
        Context.Api.ResetFences(device.WrappedDevice, 1, WrappedFence);
    }

    public static void Await(IVkContext context, Action<Fence> action)
    {
        using var fence = new VkFence(context, context.RenderServices.Devices.LogicalDevice);
        
        action.Invoke(fence.WrappedFence);
        
        fence.Await();
    }
    
    protected override void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            Context.Api.DestroyFence(device.WrappedDevice, WrappedFence, null);
        }
        
        base.Dispose(disposing);
    }
}