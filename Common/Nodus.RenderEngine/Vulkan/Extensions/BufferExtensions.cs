using System.Numerics;
using Nodus.RenderEngine.Vulkan.Memory;
using Nodus.RenderEngine.Vulkan.Meta;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Extensions;

public static class BufferExtensions
{
    public static void CmdCopyTo(this IVkBuffer buffer, IVkBuffer another, IVkContext context, CommandBuffer commandBuffer, Queue queue, Fence? fence = null, Vector2 offsets = default)
    {
        var copyContext = new BufferCopy
        {
            Size = buffer.Size,
            SrcOffset = (ulong)offsets.X,
            DstOffset = (ulong)offsets.Y
        };
        
        commandBuffer.SubmitCommandToQueue(() =>
        {
            context.Api.CmdCopyBuffer(commandBuffer, buffer.WrappedBuffer, another.WrappedBuffer, 1, in copyContext);
        }, context, queue, fence);
    }
    
    public static unsafe void CmdCopyToImage(this IVkAllocatedBuffer<byte> allocatedBuffer, IVkContext context, CommandBuffer commandBuffer, IVkImage image)
    {
        var imageCopy = new BufferImageCopy
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource = new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = image.Specification.ArrayLayers
            },
            ImageOffset = new Offset3D(0, 0, 0),
            ImageExtent = image.Specification.Size
        };
        
        context.Api.CmdCopyBufferToImage(commandBuffer, allocatedBuffer.WrappedBuffer, image.WrappedImage, ImageLayout.TransferDstOptimal, 1, &imageCopy);
    }

    public static CommandBufferSubmitInfo CreateSubmitInfo(this CommandBuffer buffer)
    {
        return new CommandBufferSubmitInfo
        {
            SType = StructureType.CommandBufferSubmitInfo,
            CommandBuffer = buffer,
            DeviceMask = 0
        };
    }
}