using System;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using ReactiveUI;

namespace Nodus.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    public NodeCanvasViewModel CanvasViewModel { get; }
    
    public MainWindowViewModel(IServiceProvider serviceProvider, INodeCanvasViewModelComponentFactory componentFactory)
    {
        CanvasViewModel = new NodeCanvasViewModel(PrepareCanvas(), serviceProvider, componentFactory);
    }

    private INodeCanvasModel PrepareCanvas()
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

        return canvas;
    }
}