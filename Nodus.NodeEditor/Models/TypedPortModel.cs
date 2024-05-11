using System;
using Nodus.Core.Reactive;
using Nodus.NodeEditor.Meta;

namespace Nodus.NodeEditor.Models;

public interface ITypedPortModel : IPortModel, IDisposable
{
    IReactiveProperty<Type> ValueType { get; }
    
    void ChangeValueType(Type type);
}

public class TypedPortModel : PortModel, ITypedPortModel
{
    private readonly MutableReactiveProperty<Type> valueType;

    public IReactiveProperty<Type> ValueType => valueType;
    
    public TypedPortModel(string header, PortType type, PortCapacity capacity, string? id = null) : base(header, type, capacity, id)
    {
        valueType = new MutableReactiveProperty<Type>(typeof(object));
    }
    
    public void ChangeValueType(Type type)
    {
        valueType.SetValue(type);
    }
    
    public override PortData Serialize()
    {
        return new TypedPortData(base.Serialize(), ValueType.Value);
    }

    public override bool IsCompatible(IPortModel other)
    {
        return other is ITypedPortModel m 
               && base.IsCompatible(other) 
               && ArePortTypesHierarchicallyCompatible(m);
    }
    
    protected bool ArePortTypesHierarchicallyCompatible(ITypedPortModel other)
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