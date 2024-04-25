using System;
using System.Collections.Generic;
using Nodus.DI.Runtime;

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

public static partial class NodeContextProviderExtensions
{
    public static void RegisterFactory<T>(this INodeContextProvider provider, string contextId, IRuntimeElementProvider elementProvider) where T : INodeContext
    {
        provider.RegisterFactory(contextId, () => elementProvider.GetRuntimeElement<T>());
    }
}