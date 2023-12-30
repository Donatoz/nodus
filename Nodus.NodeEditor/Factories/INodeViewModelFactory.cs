using Ninject.Parameters;
using Nodus.DI.Factories;
using Nodus.DI.Runtime;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.NodeEditor.Factories;

public interface INodeViewModelFactory
{
    NodeViewModel Create(INodeModel model);
}

public class NodeViewModelFactory : RuntimeElementConsumer, INodeViewModelFactory
{
    public NodeViewModelFactory(IRuntimeElementProvider elementProvider) : base(elementProvider)
    {
    }
    
    public NodeViewModel Create(INodeModel model)
    {
        return ElementProvider.GetRuntimeElement<NodeViewModel>(new TypeMatchingConstructorArgument(typeof(INodeModel), (_, _) => model));
    }
}