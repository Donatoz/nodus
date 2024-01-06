using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FlowEditor.Models;
using FlowEditor.ViewModels;
using Nodus.Core.Extensions;
using ReactiveUI;

namespace FlowEditor.Views;

public partial class FlowController : UserControl
{
    private IDisposable? executableContract;
    
    public FlowController()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        executableContract?.Dispose();

        if (DataContext is FlowCanvasViewModel vm)
        {
            executableContract = vm.FlowExecutable.AlterationStream.Subscribe(OnExecutableChanged);
        }
    }

    private void OnExecutableChanged(IFlowCanvasExecutable? e)
    {
        Container.SwitchBetweenClasses("visible", "invisible", e != null);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        executableContract?.Dispose();
    }
}