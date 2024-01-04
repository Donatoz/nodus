using System.Collections.Generic;
using System.Linq;
using FlowEditor.Factories;
using Nodus.DI.Factories;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;

namespace FlowEditor.Models;

public interface IGraphFlowBuilder
{
    IFlowUnit BuildFlow(GraphContext graph, IFlowNodeModel root);
}

public class GraphFlowBuilder : IGraphFlowBuilder
{
    protected IFlowProducer Producer { get; }
    
    public GraphFlowBuilder(IFlowProducer producer)
    {
        Producer = producer;
    }
    
    public IFlowUnit BuildFlow(GraphContext graph, IFlowNodeModel root)
    {
        var rootToken = GetRootToken(graph, root);

        return Producer.BuildFlow(rootToken);
    }

    protected virtual IFlowToken GetRootToken(GraphContext graph, IFlowNodeModel root)
    {
        var tokens = new List<IFlowToken>();

        var current = root;
        
        while (current != null)
        {
            var token = current.TryGetFlowContext()?.GetFlowToken(graph) ?? new EmptyToken();
            
            tokens.Add(token);

            current = graph.GetFlowSuccessor(current);
        }

        for (var i = 0; i < tokens.Count; i++)
        {
            tokens[i].Predecessor = i > 0 ? tokens[i - 1] : null;
            tokens[i].Successor = i < tokens.Count - 1 ? tokens[i + 1] : null;
        }

        return tokens.First();
    }
}