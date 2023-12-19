using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using Nodus.Core.Common;
using Nodus.Core.Extensions;
using Nodus.Core.Reactive;
using Nodus.Core.Selection;
using Nodus.Core.ViewModels;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Models;
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
public class NodeCanvasViewModel : ReactiveObject, INodeCanvasOperatorViewModel, IDisposable
{
    /// <summary>
    /// Represents a selector for nodes.
    /// </summary>
    /// <remarks>
    /// This property allows you to select nodes based on certain criteria.
    /// </remarks>
    public Selector<NodeViewModel> NodesSelector { get; }
    public BoundCollection<INodeModel, NodeViewModel> Nodes { get; }
    public BoundProperty<IEnumerable<ConnectionViewModel>> Connections { get; }
    public ModalCanvasViewModel ModalCanvas { get; }
    
    public ICommand RequestNodeSelectionCommand { get; }
    public ICommand AddNodeCommand { get; }

    private readonly NodeSearchModalViewModel nodeSearchModal;
    private readonly IDisposable nodeAlterationContract;

    protected readonly INodeCanvasModel model;

    /// <summary>
    /// Initializes a new instance of the NodeCanvasViewModel class.
    /// </summary>
    /// <param name="model">The INodeCanvasModel instance.</param>
    public NodeCanvasViewModel(INodeCanvasModel model)
    {
        this.model = model;
        
        NodesSelector = new Selector<NodeViewModel>();
        Nodes = new BoundCollection<INodeModel, NodeViewModel>(model.Nodes, CreateNode);
        nodeAlterationContract = Nodes.AlterationStream.Subscribe(Observer.Create<CollectionChangedEvent<NodeViewModel>>(OnNodesAlteration));
        
        Connections = new BoundProperty<IEnumerable<ConnectionViewModel>>(
            () => model.Connections.Value.Select(CreateConnection),
            model.Connections);
        ModalCanvas = new ModalCanvasViewModel();
        nodeSearchModal = new NodeSearchModalViewModel(this, model.SearchModal);

        RequestNodeSelectionCommand = ReactiveCommand.Create<NodeViewModel?>(OnNodeSelectionRequested);
        AddNodeCommand = ReactiveCommand.Create(CreateNewNode);
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
    
    private void OnNodeRemoval(NodeDeleteRequest evt)
    {
        if (NodesSelector.CurrentlySelected.Value == evt.Node)
        {
            NodesSelector.DeselectAll();
        }
        
        model.Operator.RemoveNode(evt.Node.NodeId);
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
        model.Operator.CreateNode(template);
    }

    public void CreateConnection(string sourceNode, string sourcePort, string targetNode, string targetPort)
    {
        model.Operator.Connect(sourceNode, sourcePort, targetNode, targetPort);
    }
    
    public void RemoveConnection(ConnectionViewModel connection)
    {
        model.Operator.Disconnect(connection.Data);
    }

    public void Dispose()
    {
        Nodes.Dispose();
        Connections.Dispose();
        ModalCanvas.Dispose();
        nodeAlterationContract.Dispose();
        nodeSearchModal.Dispose();
    }
}