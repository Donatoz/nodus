using System.Collections.Generic;
using System.Linq;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace FlowEditor.Models;

public interface IGraphFlowBuilder
{
    IFlowUnit BuildFlow(GraphContext graph, IFlowNodeModel root);
    IFlowToken GetRootToken(GraphContext graph, IFlowNodeModel root, Connection? sourceConnection = null);
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

    public virtual IFlowToken GetRootToken(GraphContext graph, IFlowNodeModel root, Connection? sourceConnection = null)
    {
        var tokens = new List<IFlowToken>();
        var tokenCache = new Dictionary<string, IFlowToken>();
        var descendants = new List<IFlowToken>();

        var current = root;
        var currentConnection = sourceConnection;
        
        while (current != null)
        {
            // Get primary token
            var token = tokenCache.TryGetValue(current.NodeId, out var t) 
                ? t 
                : GetNodeToken(current, graph, currentConnection);
            
            descendants.Clear();
            
            // Get descendants
            var successionCandidates = current.GetSuccessionCandidates(graph);

            foreach (var (node, connection) in successionCandidates)
            {
                var successionToken = GetNodeToken(node, graph, connection);
                TryAddToTokenCache(node.NodeId, successionToken);
                descendants.Add(successionToken);
            }

            token.DescendantTokens = descendants.ToArray();
            
            TryAddToTokenCache(current.NodeId, token);
            tokens.Add(token);

            (current, currentConnection) = graph.GetFlowSuccessor(current);
        }

        for (var i = 0; i < tokens.Count; i++)
        {
            tokens[i].Successor = i < tokens.Count - 1 ? tokens[i + 1] : null;
        }

        return tokens.First();

        void TryAddToTokenCache(string id, IFlowToken token)
        {
            if (!tokenCache.ContainsKey(id))
            {
                tokenCache[id] = token;
            }
        }
    }

    private IFlowToken GetNodeToken(IFlowNodeModel node, GraphContext context, Connection? connection)
    {
        return node.TryGetFlowContext()?.GetFlowToken(context, connection) ?? new EmptyToken();
    }
}