namespace Nodus.RenderEngine.Vulkan;

/// <summary>
/// Represents a managed wrapper for unmanaged resources in a Vulkan-based render engine.
/// </summary>
public interface IVkUnmanagedHook : IDisposable
{
    /// <summary>
    /// Get the resource state. If the resource is present, the value is equal to 'true'.
    /// </summary>
    bool IsPresent { get; }

    void AddDependency(IVkUnmanagedHook another);
    void RemoveDependency(IVkUnmanagedHook another);
}