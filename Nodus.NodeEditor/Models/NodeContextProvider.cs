using System;
using System.Collections.Generic;

namespace Nodus.NodeEditor.Models;

public interface INodeContextProvider
{
    NodeContextFactory? TryGetContextFactory(string contextId);
    void RegisterFactory(string contextId, NodeContextFactory factory);
}

public class NodeContextProvider : INodeContextProvider
{
    private readonly IDictionary<string, NodeContextFactory> contexts;

    public NodeContextProvider()
    {
        contexts = new Dictionary<string, NodeContextFactory>();
    }
    
    public NodeContextFactory? TryGetContextFactory(string contextId)
    {
        return contexts.TryGetValue(contextId, out var context) ? context : null;
    }

    public void RegisterFactory(string contextId, NodeContextFactory factory)
    {
        if (!contexts.TryAdd(contextId, factory))
        {
            throw new ArgumentException($"Context factory with id ({contextId}) was already registered.");
        }
    }
}