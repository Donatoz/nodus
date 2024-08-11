namespace Nodus.RenderEngine.Common;

public interface IRenderBackendProvider
{
    T GetBackend<T>() where T : class;
}