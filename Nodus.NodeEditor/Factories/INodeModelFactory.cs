using Nodus.Core.Entities;
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
    public INodeModel CreateNode(NodeTemplate template, IPortModelFactory portFactory)
    {
        var node = CreateBase(template.Data);
        node.Attach(template.Data);
        
        template.Data.Ports.ForEach(x => node.AddPort(portFactory.CreatePort(x)));
        node.ChangeContext(template.ContextFactory.Invoke());

        if (template.Data.ContextData != null)
        {
            node.Context.Value?.Deserialize(template.Data.ContextData);
        }

        return node;
    }

    protected virtual INodeModel CreateBase(NodeData data)
    {
        return new NodeModel(data.Title, data.Tooltip, data.ElementId, data.Group, data.ContextId);
    }
}