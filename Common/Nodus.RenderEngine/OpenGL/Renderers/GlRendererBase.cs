using System.Collections.Concurrent;
using Nodus.RenderEngine.Common;

namespace Nodus.RenderEngine.OpenGL;

public abstract class GlRendererBase : IRenderer
{
    private readonly ConcurrentQueue<Action> renderWorkerQueue;

    protected GlRendererBase()
    {
        renderWorkerQueue = new ConcurrentQueue<Action>();
    }
    
    public void Enqueue(Action workItem)
    {
        renderWorkerQueue.Enqueue(workItem);
    }
    
    public virtual void RenderFrame()
    {
        ExecuteRenderWorkerQueue();
    }
    
    private void ExecuteRenderWorkerQueue()
    {
        while (!renderWorkerQueue.IsEmpty)
        {
            if (renderWorkerQueue.TryDequeue(out var action))
            {
                action.Invoke();
            }
        }
    }

    public abstract void Initialize(IRenderContext context, IRenderBackendProvider backendProvider);
    
    public abstract void UpdateShaders(IEnumerable<IShaderDefinition> shaders);
}