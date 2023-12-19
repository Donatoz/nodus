using System.Collections.Generic;
using Nodus.Core.Reactive;

namespace Nodus.NodeEditor.Models;

public interface INodeSearchModalModel
{
    IReactiveProperty<IEnumerable<NodeTemplate>> AvailableNodes { get; }

    void FetchAvailableNodes();
}

public class NodeSearchModalModel : INodeSearchModalModel
{
    private readonly MutableReactiveProperty<IEnumerable<NodeTemplate>> availableNodes;

    public IReactiveProperty<IEnumerable<NodeTemplate>> AvailableNodes => availableNodes;

    public NodeSearchModalModel()
    {
        availableNodes = new MutableReactiveProperty<IEnumerable<NodeTemplate>>();
        
        FetchAvailableNodes();
    }

    public virtual void FetchAvailableNodes()
    {
        availableNodes.SetValue(NodeTemplateLocator.FetchTemplatesFromAssemblies());
    }
}