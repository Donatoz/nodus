using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.DI;

public interface IVkImageTransitionResolver
{
    void ResolveOldLayout(ImageLayout oldLayout, ref ImageMemoryBarrier2 barrier);
    void ResolveNewLayout(ImageLayout newLayout, ref ImageMemoryBarrier2 barrier);
}

public readonly struct VkImageMemoryBarrierMasks
{
    public AccessFlags2 SrcAccessMask { get; init; }
    public PipelineStageFlags2 SrcStageMask { get; init; }
    public AccessFlags2 DstAccessMask { get; init; }
    public PipelineStageFlags2 DstStageMask { get; init; }
    
    
    #region Common masks

    public static VkImageMemoryBarrierMasks ColorAttachmentPreRender { get; } = new()
    {
        SrcAccessMask = AccessFlags2.None,
        DstAccessMask = AccessFlags2.ColorAttachmentWriteBit,
        SrcStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
        DstStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
    };

    public static VkImageMemoryBarrierMasks ColorAttachmentPrePresent { get; } = new()
    {
        SrcAccessMask = AccessFlags2.None,
        DstAccessMask = AccessFlags2.ColorAttachmentWriteBit,
        SrcStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
        DstStageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
    };

    public static VkImageMemoryBarrierMasks DepthStencilAttachmentPreRender { get; } = new()
    {
        SrcAccessMask = AccessFlags2.None,
        DstAccessMask = AccessFlags2.DepthStencilAttachmentWriteBit,
        SrcStageMask = PipelineStageFlags2.EarlyFragmentTestsBit | PipelineStageFlags2.LateFragmentTestsBit,
        DstStageMask = PipelineStageFlags2.EarlyFragmentTestsBit | PipelineStageFlags2.LateFragmentTestsBit
    };

    #endregion
}

public class VkImageTransitionResolver : IVkImageTransitionResolver
{
    public static VkImageTransitionResolver Default { get; } = new();
    
    public void ResolveOldLayout(ImageLayout oldLayout, ref ImageMemoryBarrier2 barrier)
    {
        if (oldLayout == ImageLayout.Undefined)
        {
            barrier.SrcAccessMask = 0;
            barrier.SrcStageMask = PipelineStageFlags2.AllCommandsBit;
        }
        else if(oldLayout == ImageLayout.General)
        {
            barrier.SrcAccessMask = AccessFlags2.ShaderReadBit;
            barrier.SrcStageMask = PipelineStageFlags2.ComputeShaderBit;
        }
        else if(oldLayout == ImageLayout.TransferSrcOptimal)
        {
            barrier.SrcAccessMask = AccessFlags2.TransferReadBit;
            barrier.SrcStageMask = PipelineStageFlags2.TransferBit;
        }
        else if(oldLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = AccessFlags2.TransferWriteBit;
            barrier.SrcStageMask = PipelineStageFlags2.TransferBit;
        }
        else if(oldLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags2.ShaderReadBit;
            barrier.SrcStageMask = PipelineStageFlags2.FragmentShaderBit;
        }
        else
        {
            throw new VulkanException($"Unsupported transition from the old layout: {oldLayout}");
        }
    }

    public void ResolveNewLayout(ImageLayout newLayout, ref ImageMemoryBarrier2 barrier)
    {
        if (newLayout == ImageLayout.TransferSrcOptimal)
        {
            barrier.DstAccessMask = AccessFlags2.TransferReadBit;
            barrier.DstStageMask = PipelineStageFlags2.AllCommandsBit;
        }
        else if (newLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.DstAccessMask = AccessFlags2.TransferWriteBit;
            barrier.DstStageMask = PipelineStageFlags2.TransferBit;
        }
        else if (newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.DstAccessMask = AccessFlags2.ShaderReadBit;
            barrier.DstStageMask = PipelineStageFlags2.FragmentShaderBit;
        }
        else if (newLayout == ImageLayout.General)
        {
            barrier.DstAccessMask = AccessFlags2.ShaderReadBit;
            barrier.DstStageMask = PipelineStageFlags2.ComputeShaderBit;
        }
        else if (newLayout == ImageLayout.DepthStencilAttachmentOptimal)
        {
            barrier.DstAccessMask =
                AccessFlags2.DepthStencilAttachmentReadBit | AccessFlags2.DepthStencilAttachmentWriteBit;
            barrier.DstStageMask = PipelineStageFlags2.EarlyFragmentTestsBit;
        }
        else
        {
            throw new VulkanException($"Unsupported transition to the new layout: {newLayout}");
        }
    }
}