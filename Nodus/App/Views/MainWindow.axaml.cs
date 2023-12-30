using System;
using Avalonia;
using Avalonia.Controls;
using Nodus.DI.Runtime;
using Nodus.NodeEditor.Views;
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
            var canvas = ElementProvider.GetRuntimeElement<NodeCanvas>();
            canvas.DataContext = vm.CanvasViewModel;
            Container.Children.Add(canvas);
        }
    }
}