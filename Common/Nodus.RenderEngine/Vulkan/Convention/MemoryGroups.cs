namespace Nodus.RenderEngine.Vulkan.Convention;

public static partial class MemoryGroups
{
    /// <summary>
    /// A memory group specified for storing a general purpose images.
    /// </summary>
    public const string GeneralImageMemory = nameof(GeneralImageMemory);
    /// <summary>
    /// A memory group specified for storing rendered objects data (vertices, indices, ubos).
    /// </summary>
    public const string ObjectBufferMemory = nameof(ObjectBufferMemory);
}