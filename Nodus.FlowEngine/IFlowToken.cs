using System.Collections.Immutable;

namespace Nodus.FlowEngine;

/// <summary>
/// Represents a meta-object which is used to build the flow, i.e. it describes the logic of the flow alteration.
/// </summary>
public interface IFlowToken
{
    IFlowToken? Successor { get; set; }
    IList<IFlowToken>? Children { get; set; }
    ImmutableArray<IFlowToken>? DescendantTokens { get; set; }

    void Resolve(IFlow flow);
}

public sealed class EmptyToken : IFlowToken
{
    public IFlowToken? Successor { get; set; }
    
    public IList<IFlowToken>? Children { get; set; }
    public ImmutableArray<IFlowToken>? DescendantTokens { get; set; }

    public void Resolve(IFlow flow) { }
}

public static class FlowTokenExtensions
{
    public static void AddChild(this IFlowToken token, IFlowToken child)
    {
        token.Children ??= new List<IFlowToken>();
        token.Children.Add(child);
    }
    
    public static void RemoveChild(this IFlowToken token, IFlowToken child)
    {
        token.Children?.Remove(child);
    }
}