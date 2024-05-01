using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

    private readonly IFlowLogger logger;
    
    public GraphFlowBuilder(IFlowProducer producer, IFlowLogger logger)
    {
        Producer = producer;
        this.logger = logger;
    }
    
    public IFlowUnit BuildFlow(GraphContext graph, IFlowNodeModel root)
    {
        var now = DateTime.Now;
        var rootToken = GetRootToken(graph, root);

        var flow =  Producer.BuildFlow(rootToken);

        logger.Debug($"Built flow in {DateTime.Now - now}");
        
        return flow;
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

            // Assign succession candidates to the current token
            foreach (var (node, connection) in successionCandidates)
            {
                var successionToken = GetNodeToken(node, graph, connection);
                tokenCache.TryAdd(node.NodeId, successionToken);
                descendants.Add(successionToken);
            }

            token.DescendantTokens = descendants.ToImmutableArray();
            tokenCache.TryAdd(current.NodeId, token);
            tokens.Add(token);

            (current, currentConnection) = graph.GetFlowSuccessor(current);
        }

        for (var i = 0; i < tokens.Count; i++)
        {
            tokens[i].Successor = i < tokens.Count - 1 ? tokens[i + 1] : null;
        }

        return tokens.First();
    }

    private IFlowToken GetNodeToken(IFlowNodeModel node, GraphContext context, Connection? connection)
    {
        return node.TryGetFlowContext()?.GetFlowToken(context, connection) ?? new EmptyToken();
    }
}