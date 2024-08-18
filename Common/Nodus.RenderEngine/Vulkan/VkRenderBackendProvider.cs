using Nodus.RenderEngine.Common;

namespace Nodus.RenderEngine.Vulkan;

public readonly struct VkRenderBackendProvider(IVkContext context) : IRenderBackendProvider
{
    private readonly IVkContext context = context;

    public T GetBackend<T>() where T : class
    {
        return typeof(T) == typeof(IVkContext)
            ? (T)context
            : throw new Exception($"Failed to provide context of type: {typeof(T)}");
    }
}