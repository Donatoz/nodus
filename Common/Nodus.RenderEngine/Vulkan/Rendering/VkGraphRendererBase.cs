using System.Collections.Concurrent;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.Vulkan.Components;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Presentation;
using Nodus.RenderEngine.Vulkan.Sync;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Rendering;

public interface IVkRenderContext : IRenderContext
{
    IVkRenderPresenter Presenter { get; }
    
    IVkRenderComponent[]? Components { get; }
}

public abstract class VkGraphRendererBase : IRenderer, IDisposable
{
    /// <summary>
    /// Attached vulkan context.
    /// </summary>
    protected IVkContext? Context { get; private set; }
    /// <summary>
    /// Logical device.
    /// </summary>
    protected IVkLogicalDevice? Device { get; private set; }
    /// <summary>
    /// Attached components.
    /// </summary>
    protected IVkRenderComponent[]? Components { get; private set; }
    /// <summary>
    /// Attached presenter.
    /// </summary>
    protected IVkRenderPresenter? Presenter { get; private set; }
    /// <summary>
    /// Primary render graph.
    /// </summary>
    protected IVkTaskGraph? RenderGraph { get; private set; }

    private IVkRenderContext? renderContext;
    
    private bool isInitialized;
    
    private readonly object initializationLock;
    private readonly ConcurrentQueue<Action> workerQueue;

    protected VkGraphRendererBase()
    {
        initializationLock = new object();
        workerQueue = new ConcurrentQueue<Action>();
    }
    
    public void Initialize(IRenderContext context, IRenderBackendProvider backendProvider)
    {
        Monitor.Enter(initializationLock);
        
        isInitialized = false;

        try
        {
            renderContext = context.MustBe<IVkRenderContext>();
            Context = backendProvider.GetBackend<IVkContext>();
            Device = Context.RenderServices.Devices.LogicalDevice;
            Components = renderContext.Components;
            Presenter = renderContext.Presenter;
            
            Initialize(renderContext);
            BuildRenderGraph();

            isInitialized = true;
        }
        finally
        {
            Monitor.Exit(initializationLock);
        }
    }

    /// <summary>
    /// Rebuild the <see cref="RenderGraph"/>.
    /// </summary>
    protected void BuildRenderGraph()
    {
        RenderGraph?.Dispose();
        
        RenderGraph = CreateRenderGraph();
    }
    
    public void RenderFrame()
    {
        ValidateRenderState();
        ExecuteWorkerQueue();
        
        PrepareFrame();
        
        RenderGraph!.Execute();
    }
    
    /// <summary>
    /// Initialize the renderer with <see cref="IVkRenderContext"/>.
    /// </summary>
    /// <param name="context"></param>
    protected abstract void Initialize(IVkRenderContext context);
    /// <summary>
    /// Acquire render tasks that <see cref="RenderGraph"/> will execute per each frame.
    /// </summary>
    protected abstract IEnumerable<IVkTask> GetRenderTasks();
    /// <summary>
    /// Prepare the renderer state for the next frame.
    /// </summary>
    protected virtual void PrepareFrame() { }

    public void Enqueue(Action workItem)
    {
        workerQueue.Enqueue(workItem);
    }

    protected void ExecuteWorkerQueue()
    {
        while (!workerQueue.IsEmpty)
        {
            if (workerQueue.TryDequeue(out var action))
            {
                action.Invoke();
            }
        }
    }

    protected void ValidateRenderState()
    {
        if (!IsReadyToRender())
        {
            throw new InvalidOperationException("Render is not ready to render the frame.");
        }
    }
    
    public virtual bool IsReadyToRender()
    {
        return isInitialized;
    }

    public virtual void UpdateShaders(IEnumerable<IShaderDefinition> shaders)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Create new instance of <see cref="IVkTaskGraph"/> that will be used as primary render graph (<see cref="RenderGraph"/>).
    /// </summary>
    protected virtual IVkTaskGraph CreateRenderGraph()
    {
        var graph = new VkTaskGraph(Context!);
        
        GetRenderTasks().ForEach(x => graph.AddTask(x));
        
        graph.Bake();

        return graph;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            RenderGraph?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}