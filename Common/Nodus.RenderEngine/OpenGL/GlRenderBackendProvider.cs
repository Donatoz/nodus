using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public readonly struct GlRenderBackendProvider : IRenderBackendProvider
{
    private readonly GL context;
    
    public GlRenderBackendProvider(GL context)
    {
        this.context = context;
    }
    
    public T GetBackend<T>() where T : class
    {
        return typeof(T) == typeof(GL) 
            ? (context as T)! 
            : throw new Exception($"Failed to provide context of type: {typeof(T)}");
    }
}