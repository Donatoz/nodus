using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nodus.RenderEngine.Vulkan.Memory;

public interface IVkBuffer : IVkUnmanagedHook
{
    Buffer WrappedBuffer { get; }
    ulong Size { get; }

    void MapToHost();
    void Unmap();
}