using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.App.Views;

public partial class MainWindow : Window
{
    public NodeCanvasViewModel NodeCanvas { get; }
    
    public MainWindow()
    {
        var node = new NodeModel("My Node", new NodeTooltip("My Node", "This is example node"));
        var port = new PortModel("Some Port", PortType.Output, PortCapacity.Multiple);
        node.AddPort(port);
        
        var node2 = new NodeModel("My Node2", new NodeTooltip("My Node", "This is example node"));
        var port2 = new PortModel("Some Port2", PortType.Input, PortCapacity.Single);
        node2.AddPort(port2);

        var canvas = new NodeCanvasModel();
        canvas.Operator.AddNode(node);
        canvas.Operator.AddNode(node2);
        
        NodeCanvas = new NodeCanvasViewModel(canvas);
        
        InitializeComponent();
        
        this.AttachDevTools();
    }
}