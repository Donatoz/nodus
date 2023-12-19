using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.Models;
using ReactiveUI;

namespace Nodus.NodeEditor.ViewModels;

/// <summary>
/// Represents a view model for a connection between nodes.
/// </summary>
public class ConnectionViewModel
{
    public Connection Data { get; }
    public NodeViewModel SourceNode { get; }
    public NodeViewModel TargetNode { get; }
    public PortViewModel SourcePort { get; }
    public PortViewModel TargetPort { get; }
    
    public ICommand DeleteSelf { get; }

    protected readonly INodeCanvasOperatorViewModel operatorViewModel;

    public ConnectionViewModel(Connection data, IEnumerable<NodeViewModel> nodes, INodeCanvasOperatorViewModel operatorVm)
    {
        Data = data;
        operatorViewModel = operatorVm;

        SourceNode = GetNodeById(nodes, data.SourceNodeId);
        TargetNode = GetNodeById(nodes, data.TargetNodeId);
        SourcePort = GetPortById(SourceNode, data.SourcePortId);
        TargetPort = GetPortById(TargetNode, data.TargetPortId);

        ValidateConnection(SourceNode, TargetNode, SourcePort, TargetPort);

        DeleteSelf = ReactiveCommand.Create(OnDeleteSelf);
    }

    private void OnDeleteSelf()
    {
        operatorViewModel.RemoveConnection(this);
    }

    private NodeViewModel GetNodeById(IEnumerable<NodeViewModel> nodes, string nodeId)
    {
        return nodes.FirstOrDefault(n => n.NodeId == nodeId)
            .NotNull($"Failed to find VM for node: {nodeId}");
    }

    private PortViewModel GetPortById(NodeViewModel node, string portId)
    {
        return node.Ports.Items.FirstOrDefault(p => p.PortId == portId)
            .NotNull($"Failed to find port VM on node VM: {node}");
    }

    private void ValidateConnection(NodeViewModel sourceNode, NodeViewModel targetNode, 
        PortViewModel sourcePort, PortViewModel targetPort)
    {
        if (sourcePort == null || targetNode == null || sourcePort == null || targetPort == null)
        {
            throw new ArgumentException("Connection is not valid.");
        }
    }
}