namespace Nodus.RenderEngine.Vulkan.Convention;

public static partial class MemoryGroups
{
    /// <summary>
    /// A memory group specified for storing rendered objects data (vertices, indices, ubos).
    /// </summary>
    public const string ObjectBufferMemory = nameof(ObjectBufferMemory);
    /// <summary>
    /// A memory group specified for storing general compute data and results.
    /// </summary>
    public const string ComputeStorageMemory = nameof(ComputeStorageMemory);
    /// <summary>
    /// A memory group specified for storing sampled images.
    /// </summary>
    public const string RgbaSampledImageMemory = nameof(RgbaSampledImageMemory);
    /// <summary>
    /// A memory group specified for storing depth images.
    /// </summary>
    public const string DepthImageMemory = nameof(DepthImageMemory);
}