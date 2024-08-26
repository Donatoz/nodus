using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.DI;

public interface IVkImageTransitionResolver
{
    void ResolveOldLayout(ImageLayout oldLayout, ref PipelineStageFlags srcStage, ref ImageMemoryBarrier barrier);
    void ResolveNewLayout(ImageLayout newLayout, ref PipelineStageFlags dstStage, ref ImageMemoryBarrier barrier);
}

public class VkImageTransitionResolver : IVkImageTransitionResolver
{
    public void ResolveOldLayout(ImageLayout oldLayout, ref PipelineStageFlags srcStage, ref ImageMemoryBarrier barrier)
    {
        if(oldLayout == ImageLayout.Undefined)
        {
            barrier.SrcAccessMask = 0;
            srcStage = PipelineStageFlags.TopOfPipeBit;
        }
        else if(oldLayout == ImageLayout.General)
        {
            barrier.SrcAccessMask = AccessFlags.ShaderReadBit;
            srcStage = PipelineStageFlags.ComputeShaderBit;
        }
        else if(oldLayout == ImageLayout.TransferSrcOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.TransferReadBit;
            srcStage = PipelineStageFlags.TransferBit;
        }
        else if(oldLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            srcStage = PipelineStageFlags.TransferBit;
        }
        else if(oldLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.ShaderReadBit;
            srcStage = PipelineStageFlags.FragmentShaderBit;
        }
        else
        {
            throw new Exception($"Unsupported transition from the old layout: {oldLayout}");
        }
    }

    public void ResolveNewLayout(ImageLayout newLayout, ref PipelineStageFlags dstStage, ref ImageMemoryBarrier barrier)
    {
        if (newLayout == ImageLayout.TransferSrcOptimal)
        {
            barrier.DstAccessMask = AccessFlags.TransferReadBit;
            dstStage = PipelineStageFlags.TransferBit;
        }
        else if (newLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.DstAccessMask = AccessFlags.TransferWriteBit;
            dstStage = PipelineStageFlags.TransferBit;
        }
        else if (newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;
            dstStage = PipelineStageFlags.FragmentShaderBit;
        }
        else if (newLayout == ImageLayout.General)
        {
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;
            dstStage = PipelineStageFlags.ComputeShaderBit;
        }
        else
        {
            throw new Exception($"Unsupported transition to the new layout: {newLayout}");
        }
    }
}