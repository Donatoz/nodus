using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Alias;
using FlowEditor.ViewModels;
using Nodus.Core.Extensions;
using Nodus.Core.Reactive;
using Nodus.DI.Factories;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

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
    public IReactiveProperty<IFlowCanvasExecutable?> CurrentlyResolvedFlow => currentFlow;
    
    private readonly MutableReactiveProperty<IFlowCanvasExecutable?> currentFlow;
    
    protected IGraphFlowBuilder FlowBuilder { get; }

    public FlowCanvasModel(INodeContextProvider contextProvider, IGraphFlowBuilder flowBuilder,
        IFactory<IGraphElementTemplate, IGraphElementModel> elementFactory,
        IFactory<IGraphElementData, IGraphElementTemplate> templateFactory) 
        : base(contextProvider, elementFactory, templateFactory)
    {
        currentFlow = new MutableReactiveProperty<IFlowCanvasExecutable?>();
        FlowBuilder = flowBuilder;
    }

    public void RunFlowFrom(string nodeId)
    {
        var node = Elements.OfType<IFlowNodeModel>().First(x => x.NodeId == nodeId);
        
        CurrentlyResolvedFlow.Value?.Dispose();
        
        var executable = new FlowExecutable(FlowBuilder.BuildFlow(Context, node!));
        executable.Execute();
        
        currentFlow.SetValue(executable);
    }

    public override void LoadGraph(NodeGraph graph)
    {
        base.LoadGraph(graph);
        TryDestroyFlow();
    }

    public void TryRestartFlow()
    {
        CurrentlyResolvedFlow.Value?.Stop();
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