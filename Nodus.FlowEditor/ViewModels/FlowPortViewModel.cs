using System;
using FlowEditor.Models;
using Nodus.Core.Reactive;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace FlowEditor.ViewModels;

public class FlowPortViewModel : PortViewModel, IDisposable
{
    public BoundProperty<Type> PortValueType { get; }
    
    public FlowPortViewModel(IFlowPortModel model) : base(model)
    {
        PortValueType = model.ValueType.ToBound();
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