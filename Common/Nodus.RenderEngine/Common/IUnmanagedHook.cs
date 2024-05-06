namespace Nodus.RenderEngine.Common;

public interface IUnmanagedHook : IDisposable
{
    uint Handle { get; }
}