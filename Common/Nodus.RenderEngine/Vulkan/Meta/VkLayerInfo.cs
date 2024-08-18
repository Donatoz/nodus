using Silk.NET.Core.Native;

namespace Nodus.RenderEngine.Vulkan.Meta;

/// <summary>
/// Represents layer information for Vulkan.
/// </summary>
public readonly unsafe struct VkLayerInfo : IDisposable
{
    /// <summary>
    /// The count of enabled layers.
    /// </summary>
    public uint EnabledLayersCount { get; }
    /// <summary>
    /// The names of the enabled layers.
    /// </summary>
    public byte** EnabledLayerNames { get; }

    
    public VkLayerInfo(IReadOnlyList<string> layerNames)
    {
        EnabledLayersCount = (uint)layerNames.Count;
        EnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(layerNames);
    }

    
    public void Dispose()
    {
        SilkMarshal.Free((nint)EnabledLayerNames);
    }
}