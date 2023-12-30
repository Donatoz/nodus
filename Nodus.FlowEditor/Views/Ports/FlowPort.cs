using System;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using FlowEditor.ViewModels;
using Nodus.Core.Controls;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.Views;
using ReactiveUI;

namespace FlowEditor.Views;

public abstract class FlowPort : Port
{
    private IDisposable? valueTypeContract;

    protected virtual string ValueTypeClassPrefix => "port-value-type-";
    protected abstract Control PortContainer { get; }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        valueTypeContract?.Dispose();
        
        if (DataContext is FlowPortViewModel vm)
        {
            valueTypeContract = vm.PortValueType.WhenAnyValue(x => x.Value)
                .Subscribe(Observer.Create<Type>(OnValueTypeChanged));
        }
    }

    private void OnValueTypeChanged(Type type)
    {
        PortHandler.Classes
            .Where(x => x.StartsWith(ValueTypeClassPrefix))
            .ReverseForEach(x => PortHandler.Classes.Remove(x));
        
        PortHandler.Classes.Add(ValueTypeClassPrefix + type.Name);
        
        if (PortContainer.GetValue(ToolTip.TipProperty) is Tooltip t)
        {
            t.Text = $"Port value type: {type.Name}";
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        
        valueTypeContract?.Dispose();
    }
}