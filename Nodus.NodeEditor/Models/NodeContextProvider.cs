using System;
using System.Collections.Generic;
using System.Diagnostics;
using Nodus.Core.Extensions;

namespace Nodus.NodeEditor.Models;

public interface INodeContextProvider
{
    Func<INodeContext>? TryGetContextFactory(string contextId);
    void RegisterFactory(string contextId, Func<INodeContext> factory);
}

public class NodeContextProvider : INodeContextProvider
{
    private readonly IDictionary<string, Func<INodeContext>> contexts;

    public NodeContextProvider()
    {
        contexts = new Dictionary<string, Func<INodeContext>>();
    }
    
    public Func<INodeContext>? TryGetContextFactory(string contextId)
    {
        return contexts.TryGetValue(contextId, out var context) ? context : null;
    }

    public void RegisterFactory(string contextId, Func<INodeContext> factory)
    {
        if (!contexts.TryAdd(contextId, factory))
        {
            throw new ArgumentException($"Context factory with id ({contextId}) was already registered.");
        }
    }
}