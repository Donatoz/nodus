using Ninject.Parameters;
using Nodus.Core.Extensions;
using Nodus.DI.Factories;
using Nodus.DI.Runtime;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.RenderEditor.Models;
using Nodus.RenderEditor.ViewModels;

namespace Nodus.RenderEditor.Factories;

public class RenderNodeModelFactory : NodeModelFactory
{
    protected override INodeModel CreateBase(NodeData data)
    {
        return new RenderNodeModel(data.Title, data.Tooltip, data.ElementId, data.Group, data.ContextId);
    }
}

public class RenderNodeViewModelFactory : RuntimeElementConsumer, INodeViewModelFactory
{
    public RenderNodeViewModelFactory(IRuntimeElementProvider elementProvider) : base(elementProvider)
    {
    }

    public NodeViewModel Create(INodeModel model)
    {
        return ElementProvider.GetRuntimeElement<RenderNodeViewModel>(
            new TypeMatchingConstructorArgument(typeof(IRenderNodeModel), (_, _) => model.MustBe<IRenderNodeModel>()));
    }
}