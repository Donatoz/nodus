using System;
using System.Collections.Generic;
using System.Linq;
using Nodus.Core.Reactive;

namespace FlowEditor.Models;

public readonly struct FlowContextMutatorProperty
{
    public string PropertyName { get; }
    public Type PropertyType { get; }
    // TODO: Binding can require additional memory as it includes lambdas, consider optimizing this stuff, especially in the context of struct
    public ValueBinding PropertyBinding { get; }
    public string? Description { get; }

    public FlowContextMutatorProperty(string propertyName, Type propertyType, ValueBinding binding, string? description = null)
    {
        PropertyBinding = binding;
        PropertyType = propertyType;
        Description = description;
        PropertyName = propertyName;
    }
}

public interface IFlowContextMutator
{
    IEnumerable<FlowContextMutatorProperty> GetProperties();
}

public sealed record GenericFlowContextMutator : IFlowContextMutator
{
    private readonly IEnumerable<FlowContextMutatorProperty>? props;
    
    public GenericFlowContextMutator(params FlowContextMutatorProperty[]? props)
    {
        this.props = props;
    }
    
    public IEnumerable<FlowContextMutatorProperty> GetProperties()
    {
        return props ?? Enumerable.Empty<FlowContextMutatorProperty>();
    }
}