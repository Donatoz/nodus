using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FlowEditor.Factories;
using Nodus.DI.Factories;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace FlowEditor.Models;

public interface IFlowCanvasModel : INodeCanvasModel
{
}

public class FlowCanvasModel : NodeCanvasModel, IFlowCanvasModel
{
    protected IFlowProducer FlowProducer { get; }
    
    public FlowCanvasModel(IComponentFactoryProvider<INodeCanvasModel> componentFactoryProvider, 
        INodeContextProvider contextProvider, IComponentFactoryProvider<IFlowCanvasModel> flowComponentFactoryProvider) 
        : base(componentFactoryProvider, contextProvider)
    {
        FlowProducer = flowComponentFactoryProvider.GetFactory<IFlowProducerFactory>().Create();
    }

    public async void RunFlowFrom(string nodeId)
    {
        var node = Nodes.Value.First(x => x.NodeId == nodeId) as IFlowNodeModel;
        var rootToken = Context.GetRootFlowToken(node!);
        
        await FlowProducer.BuildFlow(rootToken).GetContext();
    }
}