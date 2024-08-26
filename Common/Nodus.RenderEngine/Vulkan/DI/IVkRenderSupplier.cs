using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.DI;

public interface IVkRenderSupplier
{
    Extent2D CurrentRenderExtent { get; }
}

public readonly struct VkRenderSupplier(Func<Extent2D> extentGetter) : IVkRenderSupplier
{
    public Extent2D CurrentRenderExtent => extentGetter.Invoke();
}