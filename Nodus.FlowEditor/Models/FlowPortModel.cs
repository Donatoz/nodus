using System;
using FlowEditor.Meta;
using Nodus.Core.Reactive;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace FlowEditor.Models;

public interface IFlowPortModel : IPortModel, IDisposable
{
    IReactiveProperty<Type> ValueType { get; }
    
    void ChangeValueType(Type type);
}

public class FlowPortModel : PortModel, IFlowPortModel
{
    private readonly MutableReactiveProperty<Type> valueType;

    public IReactiveProperty<Type> ValueType => valueType;
    
    public FlowPortModel(string header, PortType type, PortCapacity capacity, string? id = null) : base(header, type, capacity, id)
    {
        valueType = new MutableReactiveProperty<Type>(typeof(object));
    }

    public override PortData Serialize()
    {
        return new FlowPortData(base.Serialize(), ValueType.Value);
    }

    public void ChangeValueType(Type type)
    {
        valueType.SetValue(type);
    }

    public override bool IsCompatible(IPortModel other)
    {
        return other is IFlowPortModel m 
               && base.IsCompatible(other) 
               && ArePortTypesHierarchicallyCompatible(m);
    }

    protected bool ArePortTypesHierarchicallyCompatible(IFlowPortModel other)
    {
        return Type == PortType.Output ?
            ValueType.Value.IsAssignableTo(other.ValueType.Value) 
            : other.ValueType.Value.IsAssignableTo(ValueType.Value);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            valueType.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}