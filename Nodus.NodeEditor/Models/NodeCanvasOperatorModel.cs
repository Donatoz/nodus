﻿using System;
using System.Diagnostics;
using System.Linq;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.Extensions;
using Nodus.NodeEditor.Factories;

namespace Nodus.NodeEditor.Models;

public interface ICanvasOperatorModel
{
    void CreateNode(NodeTemplate template);
    void AddNode(INodeModel node);
    void RemoveNode(string nodeId);
    void Connect(string sourceNode, string sourcePort, string destinationNode, string destinationPort);
    void Disconnect(Connection connection);
}

public class NodeCanvasOperatorModel : ICanvasOperatorModel
{
    protected INodeCanvasModel Canvas { get; }
    protected INodeModelFactory NodeFactory { get; }
    protected IPortModelFactory PortFactory { get; }
    protected INodeCanvasMutationProvider MutationProvider { get; }
    
    public NodeCanvasOperatorModel(INodeCanvasModel canvas, INodeCanvasMutationProvider mutationProvider)
    {
        Canvas = canvas;
        MutationProvider = mutationProvider;
        
        NodeFactory = CreateNodeFactory();
        PortFactory = CreatePortFactory();
    }
    
    public void CreateNode(NodeTemplate template)
    {
        var node = NodeFactory.CreateNode(template, PortFactory);
        AddNode(node);
    }

    public void AddNode(INodeModel node)
    {
        if (Canvas.Nodes.Value.Contains(node))
        {
            throw new ArgumentException($"Node {node} was already added to canvas");
        }
        
        MutationProvider.AddNode(node);
    }

    public void RemoveNode(string nodeId)
    {
        var node = Canvas.Nodes.Value.FirstOrDefault(x => x.NodeId == nodeId);

        if (node == null)
        {
            throw new ArgumentException($"Node with id ({nodeId}) was not found.");
        }

        MutationProvider.RemoveNode(node);
        
        Canvas.Connections.Value
            .Where(x => x.SourceNodeId == nodeId || x.TargetNodeId == nodeId)
            .ReverseForEach(Disconnect);
        
        node.Dispose();
    }

    public void Connect(string sourceNode, string sourcePort, string destinationNode, string destinationPort)
    {
        var sourcePortModel = Canvas.Nodes.Value.FirstOrDefault(x => x.NodeId == sourceNode)?.TryFindPort(sourcePort)
            .NotNull($"Failed find source port: {sourcePort}");
        var targetPortModel = Canvas.Nodes.Value.FirstOrDefault(x => x.NodeId == destinationNode)?.TryFindPort(destinationPort)
            .NotNull($"Failed to find destination port: {destinationPort}");

        // Make sure that only input-type ports can be destinations and only output-type ports can be sources
        if (sourcePortModel!.Type == PortType.Input || targetPortModel!.Type == PortType.Output)
        {
            (sourcePort, destinationPort) = (destinationPort, sourcePort);
            (sourceNode, destinationNode) = (destinationNode, sourceNode);
        }
        
        var connection = new Connection(sourceNode, destinationNode, sourcePort, destinationPort);

        try
        {
            connection.ValidateConnection(Canvas.Context);
        }
        catch (Exception e)
        {
            Trace.WriteLine(e.Message);
            return;
        }
        
        ResolveConnection(connection);
    }

    public void Disconnect(Connection connection)
    {
        MutationProvider.RemoveConnection(connection);
    }

    protected virtual void ResolveConnection(Connection connection)
    {
        MutationProvider.AddConnection(connection);
    }
    
    protected virtual INodeModelFactory CreateNodeFactory()
    {
        return new NodeModelFactory();
    }

    protected virtual IPortModelFactory CreatePortFactory()
    {
        return new PortModelFactory();
    }
}