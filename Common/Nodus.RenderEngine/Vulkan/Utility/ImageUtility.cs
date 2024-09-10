using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan.Convention;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Memory;
using Nodus.RenderEngine.Vulkan.Meta;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace Nodus.RenderEngine.Vulkan.Utility;

public static class ImageUtility
{
    public static Format GetSupportedFormat(Format[] candidateFormats, IVkPhysicalDevice physicalDevice, ImageTiling tiling, FormatFeatureFlags features)
    {
        foreach (var format in candidateFormats)
        {
            var props = physicalDevice.GetFormatProperties(format);

            switch (tiling)
            {
                case ImageTiling.Linear when (props.LinearTilingFeatures & features) == features:
                    return format;
                case ImageTiling.Optimal when (props.OptimalTilingFeatures & features) == features:
                    return format;
                case ImageTiling.DrmFormatModifierExt:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tiling), tiling, null);
            }
        }

        throw new ArgumentException("Failed to find supported format.");
    }
    
    public static void UploadData(this IVkImage image, IVkContext context, CommandBuffer commandBuffer, Span<byte> data, ImageLayout? resultingLayout = null,
        VkImageCopyRange uploadRange = default, Queue? submitQueue = null, Fence? fence = null)
    {
        var device = context.RenderServices.Devices.LogicalDevice;
        var physicalDevice = context.RenderServices.Devices.PhysicalDevice;

        context.Api.GetImageMemoryRequirements(device.WrappedDevice, image.WrappedImage, out var requirements);

        using var lease = context.RenderServices.MemoryLessor.LeaseMemory(MemoryGroups.StagingStorageMemory,
            requirements.Size);

        using var stagingBuffer = new VkBoundBuffer(context, device,
            new VkBoundBufferContext(requirements.Size, BufferUsageFlags.TransferSrcBit, SharingMode.Exclusive));
        stagingBuffer.BindToMemory(lease);
        stagingBuffer.UpdateData(data, 0);
        
        commandBuffer.BeginBuffer(context);

        if (image.CurrentLayout != ImageLayout.TransferDstOptimal)
        {
            image.CmdTransitionLayout(commandBuffer, ImageLayout.TransferDstOptimal);
        }
        
        stagingBuffer.CmdCopyToImage(context, commandBuffer, image, imageCopyRange: uploadRange);
        
        if (resultingLayout != null)
        {
            image.CmdTransitionLayout(commandBuffer, resultingLayout.Value);
        }
        
        commandBuffer.SubmitBuffer(context, submitQueue ?? device.RequireGraphicsQueue(physicalDevice.QueueInfo), fence);
    }

    public static unsafe void SaveAsPng(this IVkImage image, IVkContext context, CommandBuffer commandBuffer, Vector2D<int> imageSize, string path, Queue? submitQueue = null)
    {
        var device = context.RenderServices.Devices.LogicalDevice;
        var physicalDevice = context.RenderServices.Devices.PhysicalDevice;
        var effectiveQueue = submitQueue ?? device.RequireGraphicsQueue(physicalDevice.QueueInfo);
        
        var imgSize = imageSize.X * imageSize.Y * 4;
        
        var regions = new BufferImageCopy
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageExtent = new Extent3D((uint)imageSize.X, (uint)imageSize.Y, 1),
            ImageOffset = new Offset3D(0, 0, 0),
            ImageSubresource = new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseArrayLayer = 0,
                LayerCount = 1,
                MipLevel = 0
            }
        };
        
        using var stagingImageBuffer = new VkAllocatedBuffer<byte>(context, device, physicalDevice,
            new VkBufferContext((ulong)imgSize, 
                BufferUsageFlags.TransferDstBit, SharingMode.Exclusive,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit));
        stagingImageBuffer.Allocate();
        
        commandBuffer.BeginBuffer(context);
        
        image.CmdTransitionLayout(commandBuffer, ImageLayout.TransferSrcOptimal);
        context.Api.CmdCopyImageToBuffer(commandBuffer, image.WrappedImage, ImageLayout.TransferSrcOptimal, stagingImageBuffer.WrappedBuffer, 1, &regions);

        commandBuffer.SubmitBuffer(context, effectiveQueue);
        
        stagingImageBuffer.MapToHost();
        var data = stagingImageBuffer.GetMappedData((uint)imgSize);

        using var img = Image.LoadPixelData<Bgra32>(data, imageSize.X, imageSize.Y);
        img.SaveAsPng(path);
    }
}