using Nodus.RenderEngine.Vulkan.Meta;
using Silk.NET.Vulkan;

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
}