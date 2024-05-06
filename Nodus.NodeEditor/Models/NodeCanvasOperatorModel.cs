using System;
using System.Diagnostics;
using System.Linq;
using Nodus.Core.Entities;
using Nodus.Core.Extensions;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Extensions;
using Nodus.NodeEditor.Meta;

namespace Nodus.NodeEditor.Models;

public interface ICanvasOperatorModel
{
    IGraphElementModel CreateElement(IGraphElementTemplate template);
    void RemoveElement(string elementId);
    IGraphElementModel? TryDuplicateElement(string elementId);
    void Connect(string sourceNode, string sourcePort, string destinationNode, string destinationPort);
    void Disconnect(Connection connection);
}

public class NodeCanvasOperatorModel : ICanvasOperatorModel
{
    protected INodeCanvasModel Canvas { get; }
    protected INodeCanvasMutationProvider MutationProvider { get; }
    protected INodeContextProvider NodeContextProvider { get; }
    protected IFactory<IGraphElementTemplate, IGraphElementModel> ElementFactory { get; }
    protected IFactory<IGraphElementData, IGraphElementTemplate> TemplateFactory { get; }
    
    public NodeCanvasOperatorModel(INodeCanvasModel canvas, 
        INodeCanvasMutationProvider mutationProvider,
        IFactory<IGraphElementTemplate, IGraphElementModel> elementFactory, 
        INodeContextProvider nodeContextProvider,
        IFactory<IGraphElementData, IGraphElementTemplate> templateFactory)
    {
        Canvas = canvas;
        MutationProvider = mutationProvider;
        ElementFactory = elementFactory;
        NodeContextProvider = nodeContextProvider;
        TemplateFactory = templateFactory;
    }

    public IGraphElementModel CreateElement(IGraphElementTemplate template)
    {
        var element = ElementFactory.Create(template);
        MutationProvider.AddElement(element);

        return element;
    }

    public void RemoveElement(string elementId)
    {
        var element = Canvas.Elements.FirstOrDefault(x => x.ElementId == elementId);

        if (element == null)
        {
            throw new ArgumentException($"Element with id ({elementId}) was not found.");
        }
        
        if (element is INodeModel)
        {
            Canvas.Connections
                .Where(x => x.SourceNodeId ==  elementId || x.TargetNodeId == elementId)
                .ReverseForEach(Disconnect);
        }

        MutationProvider.RemoveElement(element);
    }

    public IGraphElementModel? TryDuplicateElement(string elementId)
    {
        var element = Canvas.Elements.FirstOrDefault(x => x.ElementId == elementId)
            .NotNull($"Failed to duplicate element with id: {elementId} - not found.");

        if (element is not IPersistentElementModel p) return null;

        var data = p.Serialize();
        data.ElementId = Guid.NewGuid().ToString();
        
        return CreateElement(TemplateFactory.Create(data));
    }

    public void Connect(string sourceNode, string sourcePort, string destinationNode, string destinationPort)
    {
        var nodes = Canvas.Elements.OfType<INodeModel>();
        
        var sourcePortModel = nodes.FirstOrDefault(x => x.NodeId == sourceNode)?.TryFindPort(sourcePort)
            .NotNull($"Failed to find source port: {sourcePort}");
        var targetPortModel = nodes.FirstOrDefault(x => x.NodeId == destinationNode)?.TryFindPort(destinationPort)
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