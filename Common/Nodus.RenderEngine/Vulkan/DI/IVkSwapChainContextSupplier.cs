using Nodus.RenderEngine.Vulkan.Meta;

namespace Nodus.RenderEngine.Vulkan.DI;

public interface IVkSwapChainContextSupplier
{
    VkSurfaceInfo GetSurfaceInfo();
    VkQueueInfo GetQueueInfo();
}

public record VkSwapChainContextSupplier : IVkSwapChainContextSupplier
{
    private readonly Func<VkSurfaceInfo> surfaceInfoProvider;
    private readonly Func<VkQueueInfo> queueInfoProvider;

    public VkSwapChainContextSupplier(Func<VkSurfaceInfo> surfaceInfoProvider, Func<VkQueueInfo> queueInfoProvider)
    {
        this.surfaceInfoProvider = surfaceInfoProvider;
        this.queueInfoProvider = queueInfoProvider;
    }

    public VkSurfaceInfo GetSurfaceInfo() => surfaceInfoProvider.Invoke();

    public VkQueueInfo GetQueueInfo() => queueInfoProvider.Invoke();
}