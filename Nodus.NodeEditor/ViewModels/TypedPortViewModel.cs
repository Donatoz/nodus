using System;
using Nodus.Core.Reactive;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.ViewModels;

public class TypedPortViewModel : PortViewModel, IDisposable
{
    public NotifyingBoundProperty<Type> PortValueType { get; }

    public TypedPortViewModel(ITypedPortModel model) : base(model)
    {
        PortValueType = model.ValueType.ToNotifying();
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            PortValueType.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}