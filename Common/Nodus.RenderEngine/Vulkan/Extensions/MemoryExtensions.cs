using Nodus.RenderEngine.Vulkan.Memory;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nodus.RenderEngine.Vulkan.Extensions;

public static class MemoryExtensions
{
    public static void AllocateForBuffer(this IVkMemory memory, IVkContext context, Buffer buffer, IVkLogicalDevice device)
    {
        context.Api.GetBufferMemoryRequirements(device.WrappedDevice, buffer, out var requirements);
        
        memory.Allocate(requirements.Size, requirements.MemoryTypeBits);
    }

    public static void AllocateForImage(this IVkMemory memory, IVkContext context, Image image, IVkLogicalDevice device)
    {
        context.Api.GetImageMemoryRequirements(device.WrappedDevice, image, out var requirements);
        
        memory.Allocate(requirements.Size, requirements.MemoryTypeBits);
    }

    public static bool IsAllocated(this IVkMemory memory)
    {
        return memory.WrappedMemory != null;
    }
}