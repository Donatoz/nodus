using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nodus.Core.Reactive;
using Nodus.DI.Factories;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Models;

namespace FlowEditor.Models;

public interface IFlowCanvasModel : INodeCanvasModel
{
    IReactiveProperty<IFlowCanvasExecutable?> CurrentlyResolvedFlow { get; }
    
    void RunFlowFrom(string nodeId);
    void TryRestartFlow();
    void TryDestroyFlow();
}

public interface IFlowCanvasExecutable : IDisposable
{
    IReactiveProperty<bool> IsExecuting { get; }
    
    Task Execute();
    void Stop();
}

public class FlowCanvasModel : NodeCanvasModel, IFlowCanvasModel
{
    private readonly MutableReactiveProperty<IFlowCanvasExecutable?> currentFlow;
    
    protected IGraphFlowBuilder FlowBuilder { get; }
    public IReactiveProperty<IFlowCanvasExecutable?> CurrentlyResolvedFlow => currentFlow;

    public FlowCanvasModel(IComponentFactoryProvider<INodeCanvasModel> componentFactoryProvider, 
        INodeContextProvider contextProvider, IGraphFlowBuilder flowBuilder) 
        : base(componentFactoryProvider, contextProvider)
    {
        currentFlow = new MutableReactiveProperty<IFlowCanvasExecutable?>();
        FlowBuilder = flowBuilder;
    }

    public void RunFlowFrom(string nodeId)
    {
        var node = Nodes.Value.First(x => x.NodeId == nodeId) as IFlowNodeModel;
        
        CurrentlyResolvedFlow.Value?.Dispose();
        
        var executable = new FlowExecutable(FlowBuilder.BuildFlow(Context, node!));
        executable.Execute();
        
        currentFlow.SetValue(executable);
    }

    public void TryRestartFlow()
    {
        CurrentlyResolvedFlow.Value?.Execute();
    }

    public void TryDestroyFlow()
    {
        CurrentlyResolvedFlow.Value?.Dispose();
        currentFlow.SetValue(null);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        if (!disposing) return;
        
        currentFlow.Value?.Stop();
        currentFlow.Dispose();
    }
    
    private class FlowExecutable : IFlowCanvasExecutable
    {
        private readonly MutableReactiveProperty<bool> isExecuting;
        public IReactiveProperty<bool> IsExecuting => isExecuting;

        private readonly IFlowUnit unit;
        private CancellationTokenSource? cancellation;
        
        public FlowExecutable(IFlowUnit unit)
        {
            this.unit = unit;
            isExecuting = new MutableReactiveProperty<bool>();
        }
        
        public Task Execute()
        {
            ResetCancellation();

            isExecuting.SetValue(true);
            cancellation = new CancellationTokenSource();
            
            return Task.Run(async () =>
            {
                await unit.Execute(cancellation.Token);
                isExecuting.SetValue(false);
            });
        }

        public void Stop()
        {
            ResetCancellation();
            cancellation = null;
            isExecuting.SetValue(false);
        }

        private void ResetCancellation()
        {
            cancellation?.Cancel();
            cancellation?.Dispose();
        }

        public void Dispose()
        {
            Stop();
            isExecuting.Dispose();
        }
    }
}