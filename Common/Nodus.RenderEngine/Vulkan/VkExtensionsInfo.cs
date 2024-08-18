using Nodus.RenderEngine.Vulkan.Meta;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan.Extensions.EXT;

namespace Nodus.RenderEngine.Vulkan;

/// <summary>
/// Represents information about Vulkan extensions required by the application.
/// </summary>
public readonly struct VkExtensionsInfo
{
    /// <summary>
    /// The required extensions for the Vulkan instance.
    /// </summary>
    public string[] RequiredExtensions { get; }

    /// <summary>
    /// The required device extensions for a Vulkan logical device.
    /// </summary>
    /// <remarks>
    /// This property is used to specify the required device extensions
    /// when creating a Vulkan logical devices. These device extensions are necessary for the
    /// logical device to support certain features or functionality.
    /// </remarks>
    public string[] RequiredDeviceExtensions { get; }
    
    public unsafe VkExtensionsInfo(IVkSurface khrSurface, VkLayerInfo? layerInfo, string[] requiredDeviceExtensions)
    {
        RequiredDeviceExtensions = requiredDeviceExtensions;
        var windowExtensions = khrSurface.GetRequiredExtensions(out var glfwExtensionCount);
        var extensions = SilkMarshal.PtrToStringArray((nint)windowExtensions, (int)glfwExtensionCount).AsEnumerable();

        if (layerInfo != null)
        {
            extensions = extensions.Append(ExtDebugUtils.ExtensionName);
        }

        RequiredExtensions = extensions.ToArray();
    }
}