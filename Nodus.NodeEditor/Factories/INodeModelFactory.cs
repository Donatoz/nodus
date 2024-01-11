using Nodus.Core.Extensions;
using Nodus.DI.Runtime;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Factories;

public interface INodeModelFactory
{
    INodeModel CreateNode(NodeTemplate template, IPortModelFactory portFactory);
}

public class NodeModelFactory : INodeModelFactory
{
    private readonly IRuntimeInjector injector;
    
    public NodeModelFactory(IRuntimeInjector injector)
    {
        this.injector = injector;
    }
    
    public INodeModel CreateNode(NodeTemplate template, IPortModelFactory portFactory)
    {
        var node = CreateBase(template.Data);
        
        template.Data.Ports.ForEach(x => node.AddPort(portFactory.CreatePort(x)));
        node.ChangeContext(template.ContextFactory.Invoke());

        if (node.Context.Value != null)
        {
            injector.Inject(node.Context.Value);
        }

        if (template.Data.ContextData != null)
        {
            node.Context.Value?.Deserialize(template.Data.ContextData);
        }

        return node;
    }

    protected virtual INodeModel CreateBase(NodeData data)
    {
        return new NodeModel(data.Title, data.Tooltip, data.NodeId, data.Group, data.ContextId);
    }
}