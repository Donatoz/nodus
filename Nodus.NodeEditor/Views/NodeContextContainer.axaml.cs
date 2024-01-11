using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.NodeEditor.Views;

public partial class NodeContextContainer : UserControl
{
    private IDisposable? selectedContextContract;
    
    public NodeContextContainer()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        selectedContextContract?.Dispose();

        if (DataContext is NodeContextContainerViewModel vm)
        {
            selectedContextContract = vm.SelectedContext.AlterationStream.Subscribe(OnContextChanged);
        }
    }

    private void OnContextChanged(INodeContextViewModel? ctx)
    {
        Container.SwitchBetweenClasses("visible", "invisible", ctx != null);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        selectedContextContract?.Dispose();
    }
}