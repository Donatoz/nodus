using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using DynamicData;
using FlowEditor.Views;
using Nodus.Core.Interaction;
using Nodus.DI.Runtime;
using Nodus.NodeEditor.Views;
using Nodus.RenderEngine.Avalonia;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.OpenGL;
using Nodus.ViewModels;
using PropertyModels.ComponentModel;

namespace Nodus.App.Views;

public partial class MainWindow : Window
{
    protected IRuntimeElementProvider ElementProvider { get; }
    
    public MainWindow(IRuntimeElementProvider elementProvider)
    {
        ElementProvider = elementProvider;
        
        InitializeComponent();
        
        var binder = new WindowHotkeyBinder(this);
        binder.BindHotkey(KeyGesture.Parse("Space"), () => Surface?.UpdateShaders());
        
        this.AttachDevTools();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        if (DataContext is MainWindowViewModel vm)
        {
            var canvas = ElementProvider.GetRuntimeElement<FlowCanvas>();
            canvas.DataContext = vm.CanvasViewModel;
            //Container.Children.Insert(0, canvas);
        }
    }

    private void OnSurfacePressed(object? sender, PointerPressedEventArgs e)
    {
        Trace.WriteLine("press");
    }
}