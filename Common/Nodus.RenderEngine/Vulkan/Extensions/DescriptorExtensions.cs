using Nodus.RenderEngine.Vulkan.Meta;

namespace Nodus.RenderEngine.Vulkan.Extensions;

public static class DescriptorExtensions
{
    public static void WriteIntoDescriptor(this IVkImage image, uint binding, IVkDescriptorPool pool, int setIndex)
    {
        if (image.View == null || image.Sampler == null)
        {
            throw new Exception("Image view and sampler are required for the descriptor write.");
        }
        
        using var writeToken = new VkImageSamplerWriteToken(binding, 0, image.View!.Value, image.Sampler!.Value);
        
        pool.UpdateSet(setIndex, writeToken);
    }
}