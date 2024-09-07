using Nodus.RenderEngine.Vulkan.Presentation;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Meta;

/// <summary>
/// Represents information about a Vulkan surface.
/// </summary>
public readonly struct VkSurfaceInfo
{
    /// <summary>
    /// The size of the frame buffer.
    /// </summary>
    /// <remarks>
    /// The size is defined as a vector value, where the X component represents the width of the frame buffer
    /// and the Y component represents the height of the frame buffer.
    /// </remarks>
    public Vector2D<int> FrameBufferSize { get; }
    /// <summary>
    /// The capabilities of the surface.
    /// </summary>
    public SurfaceCapabilitiesKHR Capabilities { get; }
    /// <summary>
    /// The formats supported by the surface.
    /// </summary>
    public SurfaceFormatKHR[] Formats { get; }
    /// <summary>
    /// The present modes of the surface.
    /// </summary>
    public PresentModeKHR[] PresentModes { get; }

    public unsafe VkSurfaceInfo(Vector2D<int> frameBufferSize, PhysicalDevice device, IVkKhrSurface surface)
    {
        FrameBufferSize = frameBufferSize;

        var capabilities = new SurfaceCapabilitiesKHR();
        surface.Extension.GetPhysicalDeviceSurfaceCapabilities(device, surface.SurfaceKhr, &capabilities);

        var formatCount = 0u;
        surface.Extension.GetPhysicalDeviceSurfaceFormats(device, surface.SurfaceKhr, ref formatCount, null);
        var formats = new SurfaceFormatKHR[formatCount];

        if (formats.Any())
        {
            fixed (SurfaceFormatKHR* p = formats)
            {
                surface.Extension.GetPhysicalDeviceSurfaceFormats(device, surface.SurfaceKhr, ref formatCount, p);
            }
        }

        var presentModeCount = 0u;
        surface.Extension.GetPhysicalDeviceSurfacePresentModes(device, surface.SurfaceKhr, ref presentModeCount, null);
        var presentModes = new PresentModeKHR[presentModeCount];

        if (presentModes.Any())
        {
            fixed (PresentModeKHR* p = presentModes)
            {
                surface.Extension.GetPhysicalDeviceSurfacePresentModes(device, surface.SurfaceKhr, ref presentModeCount, p);
            }
        }
        
        Capabilities = capabilities;
        Formats = formats;
        PresentModes = presentModes;
    }
}