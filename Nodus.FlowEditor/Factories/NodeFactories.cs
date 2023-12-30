using FlowEditor.Models;
using FlowEditor.ViewModels;
using Ninject.Parameters;
using Nodus.Core.Extensions;
using Nodus.DI.Factories;
using Nodus.DI.Runtime;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace FlowEditor.Factories;

public class FlowNodeViewModelFactory : RuntimeElementConsumer, INodeViewModelFactory
{
    public FlowNodeViewModelFactory(IRuntimeElementProvider elementProvider) : base(elementProvider)
    {
    }

    public NodeViewModel Create(INodeModel model)
    {
        return ElementProvider.GetRuntimeElement<FlowNodeViewModel>(
            new TypeMatchingConstructorArgument(typeof(IFlowNodeModel), (_, _) => model.MustBe<IFlowNodeModel>()));
    }
}

public class FlowNodeModelFactory : NodeModelFactory
{
    protected override INodeModel CreateBase(NodeData data)
    {
        return new FlowNodeModel(data.Title, data.Tooltip, data.NodeId, data.Group, data.ContextId);
    }
}