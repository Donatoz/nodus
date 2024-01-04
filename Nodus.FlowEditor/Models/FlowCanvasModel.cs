using System.Linq;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Models;

namespace FlowEditor.Models;

public interface IFlowCanvasModel : INodeCanvasModel
{
    void RunFlowFrom(string nodeId);
}

public class FlowCanvasModel : NodeCanvasModel, IFlowCanvasModel
{
    protected IGraphFlowBuilder FlowBuilder { get; }
    
    public FlowCanvasModel(IComponentFactoryProvider<INodeCanvasModel> componentFactoryProvider, 
        INodeContextProvider contextProvider, IGraphFlowBuilder flowBuilder) 
        : base(componentFactoryProvider, contextProvider)
    {
        FlowBuilder = flowBuilder;
    }

    public async void RunFlowFrom(string nodeId)
    {
        var node = Nodes.Value.First(x => x.NodeId == nodeId) as IFlowNodeModel;
        await FlowBuilder.BuildFlow(Context, node).GetContext();
    }
}