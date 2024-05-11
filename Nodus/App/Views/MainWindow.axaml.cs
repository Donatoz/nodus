using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using FlowEditor.ViewModels;
using FlowEditor.Views;
using Nodus.Core.Interaction;
using Nodus.DI.Runtime;
using Nodus.NodeEditor.ViewModels;
using Nodus.NodeEditor.Views;
using Nodus.RenderEditor.ViewModels;
using Nodus.RenderEditor.Views;
using Nodus.RenderEngine.Avalonia;
using Nodus.RenderEngine.OpenGL;
using Nodus.ViewModels;

namespace Nodus.App.Views;

public partial class MainWindow : Window
{
    protected IRuntimeElementProvider ElementProvider { get; }
    
    public MainWindow(IRuntimeElementProvider elementProvider)
    {
        ElementProvider = elementProvider;
        
        InitializeComponent();
        
        this.AttachDevTools();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        if (DataContext is MainWindowViewModel vm)
        {
            var canvas = CrateNodeCanvas(vm.CanvasViewModel);
            canvas.DataContext = vm.CanvasViewModel;
            Container.Children.Insert(0, canvas);
        }
    }

    private NodeCanvas CrateNodeCanvas(NodeCanvasViewModel vm)
    {
        return vm switch
        {
            FlowCanvasViewModel => ElementProvider.GetRuntimeElement<FlowCanvas>(),
            RenderCanvasViewModel => ElementProvider.GetRuntimeElement<RenderCanvas>(),
            _ => throw new Exception($"Node canvas of type ({vm}) is not supported.")
        };
    }
}