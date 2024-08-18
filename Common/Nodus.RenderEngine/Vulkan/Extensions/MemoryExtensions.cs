using Nodus.RenderEngine.Vulkan.Memory;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nodus.RenderEngine.Vulkan.Extensions;

public static class MemoryExtensions
{
    public static void AllocateForBuffer(this IVkMemory memory, IVkContext context, Buffer buffer, IVkLogicalDevice device)
    {
        context.Api.GetBufferMemoryRequirements(device.WrappedDevice, buffer, out var requirements);
        
        memory.Allocate(requirements.Size, requirements.MemoryTypeBits);
    }
}