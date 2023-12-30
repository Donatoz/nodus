using System;
using System.Diagnostics;
using System.Linq;
using Nodus.Core.Entities;
using Nodus.Core.Extensions;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Extensions;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Meta;

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
    protected INodeCanvasMutationProvider MutationProvider { get; }
    protected IComponentFactoryProvider<INodeCanvasModel> FactoryProvider { get; }
    protected INodeContextProvider NodeContextProvider { get; }
    
    public NodeCanvasOperatorModel(INodeCanvasModel canvas, INodeCanvasMutationProvider mutationProvider, 
        IComponentFactoryProvider<INodeCanvasModel> factoryProvider, INodeContextProvider nodeContextProvider)
    {
        Canvas = canvas;
        MutationProvider = mutationProvider;
        FactoryProvider = factoryProvider;
        NodeContextProvider = nodeContextProvider;
    }
    
    public void CreateNode(NodeTemplate template)
    {
        var nodeFactory = FactoryProvider.GetFactory<INodeModelFactory>();
        var portFactory = FactoryProvider.GetFactory<IPortModelFactory>();
        
        var node = nodeFactory.CreateNode(template.WithContext(NodeContextProvider.TryGetContextFactory(template.Data.ContextId ?? string.Empty)), portFactory);

        node.AddComponent(new ValueContainer<NodeData>(template.Data));
        
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
            .NotNull($"Failed to find source port: {sourcePort}");
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
}