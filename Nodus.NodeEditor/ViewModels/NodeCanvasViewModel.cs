using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using Nodus.Core.Common;
using Nodus.Core.Extensions;
using Nodus.Core.Reactive;
using Nodus.Core.Selection;
using Nodus.Core.ViewModels;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels.Events;
using ReactiveUI;

namespace Nodus.NodeEditor.ViewModels;

public interface INodeCanvasOperatorViewModel
{
    void CreateNode(NodeTemplate template);
    void CreateConnection(string sourceNode, string sourcePort, string targetNode, string targetPort);
    void RemoveConnection(ConnectionViewModel connection);
}

/// <summary>
/// Represents the view model for a node-based canvas.
/// </summary>
public class NodeCanvasViewModel : ReactiveViewModel, INodeCanvasOperatorViewModel, IDisposable
{
    /// <summary>
    /// Represents a selector for nodes.
    /// </summary>
    /// <remarks>
    /// This property allows you to select nodes based on certain criteria.
    /// </remarks>
    public Selector<NodeViewModel> NodesSelector { get; }
    public BoundCollection<INodeModel, NodeViewModel> Nodes { get; }
    public BoundCollection<Connection, ConnectionViewModel> Connections { get; }
    public ModalCanvasViewModel ModalCanvas { get; }
    public NodeCanvasToolbarViewModel Toolbar { get; }
    
    public ICommand RequestNodeSelectionCommand { get; }
    public ICommand AddNodeCommand { get; }
    public ICommand RemoveSelectedCommand { get; }

    private readonly NodeSearchModalViewModel nodeSearchModal;
    private readonly IDisposable nodeAlterationContract;

    protected INodeCanvasModel Model { get; }
    protected IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Initializes a new instance of the NodeCanvasViewModel class.
    /// </summary>
    /// <param name="model">The INodeCanvasModel instance.</param>
    /// <param name="serviceProvider">Service provider</param>
    public NodeCanvasViewModel(INodeCanvasModel model, IServiceProvider serviceProvider)
    {
        Model = model;
        ServiceProvider = serviceProvider;
        
        NodesSelector = new Selector<NodeViewModel>();
        
        Nodes = new BoundCollection<INodeModel, NodeViewModel>(model.Nodes, CreateNode);
        nodeAlterationContract = Nodes.AlterationStream.Subscribe(Observer.Create<CollectionChangedEvent<NodeViewModel>>(OnNodesAlteration));
        Connections = new BoundCollection<Connection, ConnectionViewModel>(model.Connections, CreateConnection);
        
        Toolbar = new NodeCanvasToolbarViewModel(serviceProvider, model);
        
        ModalCanvas = new ModalCanvasViewModel();
        nodeSearchModal = new NodeSearchModalViewModel(this, model.SearchModal);

        RequestNodeSelectionCommand = ReactiveCommand.Create<NodeViewModel?>(OnNodeSelectionRequested);
        AddNodeCommand = ReactiveCommand.Create(CreateNewNode);
        RemoveSelectedCommand = ReactiveCommand.Create(RemoveCurrent);
        
        model.EventStream.OnEvent<MutationEvent<NodeData>>(OnNodeDataMutation);
    }

    /// <summary>
    /// Handles the request to select a node.
    /// </summary>
    /// <param name="node">The node to be selected.</param>
    private void OnNodeSelectionRequested(NodeViewModel? node)
    {
        if (node == null || NodesSelector.CurrentlySelected.Value == node)
        {
            NodesSelector.DeselectAll();
        }
        else
        {
            NodesSelector.Select(node);
        }
    }

    private void RemoveCurrent()
    {
        if (NodesSelector.CurrentlySelected.Value != null)
        {
            RemoveNode(NodesSelector.CurrentlySelected.Value);
        }
    }

    protected virtual NodeViewModel CreateNode(INodeModel model)
    {
        var vm = NodeViewModelFactory.Create(model);

        vm.EventStream.OnEvent<NodeDeleteRequest>(OnNodeRemoval);
        
        return vm;
    }

    private void OnNodesAlteration(CollectionChangedEvent<NodeViewModel> evt)
    {
        if (!evt.Added)
        {
            evt.Item.Dispose();
        }
    }
    
    private void OnNodeDataMutation(MutationEvent<NodeData> evt)
    {
        evt.MutatedValue.VisualData ??= new NodeVisualData();

        var nodeVm = Nodes.Items.FirstOrDefault(x => x.NodeId == evt.MutatedValue.NodeId)
            .NotNull($"Failed to find mutated node view model: {evt.MutatedValue.NodeId}");
        
        Trace.WriteLine($"Node data mutation");
        
        RaiseEvent(new NodeVisualMutationEvent(nodeVm, evt.MutatedValue.VisualData));
    }
    
    private void OnNodeRemoval(NodeDeleteRequest evt)
    {
        RemoveNode(evt.Node);
    }

    protected void RemoveNode(NodeViewModel node)
    {
        if (NodesSelector.CurrentlySelected.Value == node)
        {
            NodesSelector.DeselectAll();
        }
        
        Model.Operator.RemoveNode(node.NodeId);
    }

    protected virtual ConnectionViewModel CreateConnection(Connection connection)
    {
        return new ConnectionViewModel(connection, Nodes.Items, this);
    }
    
    private void CreateNewNode()
    {
        ModalCanvas.OpenModal(nodeSearchModal);
    }

    public void CreateNode(NodeTemplate template)
    {
        Model.Operator.CreateNode(template);
    }

    public void CreateConnection(string sourceNode, string sourcePort, string targetNode, string targetPort)
    {
        Model.Operator.Connect(sourceNode, sourcePort, targetNode, targetPort);
    }
    
    public void RemoveConnection(ConnectionViewModel connection)
    {
        Model.Operator.Disconnect(connection.Data);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        if (!disposing) return;
        
        Nodes.Dispose();
        Connections.Dispose();
        ModalCanvas.Dispose();
        nodeAlterationContract.Dispose();
        nodeSearchModal.Dispose();
    }
}