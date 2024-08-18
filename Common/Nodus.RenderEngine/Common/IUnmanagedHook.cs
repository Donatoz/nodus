namespace Nodus.RenderEngine.Common;

/// <summary>
/// Represents a managed wrapper for unmanaged resources.
/// </summary>
/// <typeparam name="T">The type of unmanaged resource.</typeparam>
public interface IUnmanagedHook<out T> : IDisposable where T : unmanaged
{
    /// <summary>
    /// Represents a handle to a resource.
    /// </summary>
    /// <typeparam name="T">The underlying type of the handle.</typeparam>
    T Handle { get; }
}