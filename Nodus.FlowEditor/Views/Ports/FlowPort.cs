using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using FlowEditor.Controls;
using FlowEditor.ViewModels;
using Nodus.Core.Controls;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.Views;
using ReactiveUI;

namespace FlowEditor.Views;

public abstract class FlowPort : Port
{
    public Type ValueType { get; private set; }
    
    private IDisposable? valueTypeContract;

    protected virtual string ValueTypeClassPrefix => "port-value-type-";
    protected virtual Control? TooltipContainer { get; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (TooltipContainer != null)
        {
            ToolTip.SetTip(TooltipContainer, CreateTooltipControl());
        }
        
        AddHandler(PortPressedEvent, OnPortPressed);
    }

    private void OnPortPressed(object? sender, PortDragEventArgs e)
    {
        if (TooltipContainer != null)
        {
            ToolTip.SetIsOpen(TooltipContainer, false);
        }
    }

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
        
        if (TooltipContainer?.GetValue(ToolTip.TipProperty) is Tooltip t)
        {
            t.Text = $"Port value type: {type.Name}";
        }

        ValueType = type;
    }
    
    protected virtual Control CreateTooltipControl()
    {
        return new FlowPortTooltip();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        
        valueTypeContract?.Dispose();
    }
}