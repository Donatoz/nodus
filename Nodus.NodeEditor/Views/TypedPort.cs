using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Nodus.Core.Controls;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.NodeEditor.Views;

public abstract class TypedPort : Port
{
    public virtual string PortTypeName => "Typed Port";
    
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
        
        if (DataContext is TypedPortViewModel vm)
        {
            valueTypeContract = vm.PortValueType.AlterationStream.Subscribe(OnValueTypeChanged);
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
    }

    protected virtual Control CreateTooltipControl()
    {
        return new TypedPortTooltip();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        
        valueTypeContract?.Dispose();
    }
}