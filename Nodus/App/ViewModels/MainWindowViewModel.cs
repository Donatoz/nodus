using System;
using System.Diagnostics;
using FlowEditor.Models;
using FlowEditor.Models.Contexts;
using FlowEditor.ViewModels;
using Ninject.Parameters;
using Nodus.Core.Reactive;
using Nodus.DI;
using Nodus.DI.Factories;
using Nodus.DI.Runtime;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using ReactiveUI;

namespace Nodus.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    public NodeCanvasViewModel CanvasViewModel { get; }
    
    public MainWindowViewModel(IRuntimeElementProvider elementProvider, IRuntimeModuleLoader moduleLoader)
    {
        moduleLoader.Repopulate();
        
        moduleLoader.LoadModulesForType<NodeCanvasModel>();
        moduleLoader.LoadModulesForType<FlowCanvasModel>();
        
        DefaultFlowContexts.Register(elementProvider.GetRuntimeElement<INodeContextProvider>());
        
        CanvasViewModel = elementProvider.GetRuntimeElement<FlowCanvasViewModel>(
            new ConstructorArgument("model", PrepareCanvas(elementProvider)));
    }

    private INodeCanvasModel PrepareCanvas(IRuntimeElementProvider elementProvider)
    {
        var node = new FlowNodeModel("My Node", new NodeTooltip("My Node", "This is example node"));
        var port = new FlowPortModel("Some Port", PortType.Output, PortCapacity.Multiple);
        node.AddPort(port);
        
        var node2 = new FlowNodeModel("My Node2", new NodeTooltip("My Node", "This is example node"));
        var port2 = new FlowPortModel("Some Port2", PortType.Input, PortCapacity.Single);
        node2.AddPort(port2);

        var canvas = elementProvider.GetRuntimeElement<FlowCanvasModel>();
        canvas.Operator.AddNode(node);
        canvas.Operator.AddNode(node2);

        return canvas;
    }
}