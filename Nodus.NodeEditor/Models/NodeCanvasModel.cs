using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using DynamicData;
using Nodus.Core.Common;
using Nodus.Core.Entities;
using Nodus.Core.Extensions;
using Nodus.Core.Reactive;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Extensions;
using Nodus.NodeEditor.Meta;

namespace Nodus.NodeEditor.Models;

public interface INodeCanvasModel : IEntity, IDisposable
{
    IObservable<IEvent> EventStream { get; }
    
    IReactiveProperty<string> GraphName { get; }
    ICanvasOperatorModel Operator { get; }
    /// <summary>
    /// Graph representation of the node canvas.
    /// </summary>
    GraphContext Context { get; }
    
    IEnumerable<IGraphElementModel> Elements { get; }
    IEnumerable<Connection> Connections { get; }
    IObservable<IChangeSet<IGraphElementModel, string>> ElementStream { get; }
    IObservable<IChangeSet<Connection>> ConnectionStream { get; }
    
    void LoadGraph(NodeGraph graph);
    NodeGraph SerializeToGraph();
}

public interface INodeCanvasMutationProvider
{
    void AddElement(IGraphElementModel element);
    void RemoveElement(IGraphElementModel element);
    void AddConnection(Connection connection);
    void RemoveConnection(Connection connection);
}

public class NodeCanvasModel : Entity, INodeCanvasModel
{
    public override string EntityId { get; }
    public IReactiveProperty<string> GraphName => graphName;
    public IObservable<IEvent> EventStream => eventSubject;
    public IEnumerable<IGraphElementModel> Elements => elements.Items;
    public IEnumerable<Connection> Connections => connections.Items;
    public IObservable<IChangeSet<IGraphElementModel, string>> ElementStream => elements.Connect();
    public IObservable<IChangeSet<Connection>> ConnectionStream => connections.Connect();
    
    /// <summary>
    /// Returns a NEW graph context instance, meaning that with EACH getter call
    /// it will allocate new memory for the context cache.
    /// </summary>
    public GraphContext Context => new(Elements.OfType<INodeModel>(), Connections);

    public ICanvasOperatorModel Operator { get; }
    
    private readonly MutableReactiveProperty<string> graphName;
    private readonly Subject<IEvent> eventSubject;
    private readonly ISourceCache<IGraphElementModel, string> elements;
    private readonly ISourceList<Connection> connections;
    private readonly IFactory<IGraphElementData, IGraphElementTemplate> templateFactory;

    protected INodeCanvasMutationProvider MutationProvider { get; }
    protected virtual string DefaultGraphName => "New Graph";
    
    public NodeCanvasModel(INodeContextProvider contextProvider, 
        IFactory<IGraphElementTemplate, IGraphElementModel> elementFactory,
        IFactory<IGraphElementData, IGraphElementTemplate> templateFactory)
    {
        EntityId = Guid.NewGuid().ToString();
        graphName = new MutableReactiveProperty<string>(DefaultGraphName);
        eventSubject = new Subject<IEvent>();
        elements = new SourceCache<IGraphElementModel, string>(x => x.ElementId);
        connections = new SourceList<Connection>();
        this.templateFactory = templateFactory;

        MutationProvider = CreateMutationProvider();
        Operator = CreateOperator(contextProvider, elementFactory);

        this.AddComponent(new ValueContainer<INodeSearchModalModel>(new NodeSearchModalModel()));
    }

    protected virtual INodeCanvasMutationProvider CreateMutationProvider()
    {
        return new NodeCanvasMutationProvider(elements, connections);
    }

    protected virtual NodeCanvasOperatorModel CreateOperator(INodeContextProvider contextProvider, IFactory<IGraphElementTemplate, IGraphElementModel> elementFactory)
    {
        return new NodeCanvasOperatorModel(this, MutationProvider, elementFactory, contextProvider, templateFactory);
    }
    
    public virtual void LoadGraph(NodeGraph graph)
    {
        Elements.OfType<IDisposable>().DisposeAll();
        
        elements.Clear();
        connections.Clear();
        
        graphName.SetValue(graph.GraphName);
        
        graph.Elements.ForEach(x => Operator.CreateElement(templateFactory.Create(x)));
        graph.Connections.ForEach(x => Operator.Connect(x));
    }

    public virtual NodeGraph SerializeToGraph()
    {
        var elements = new List<IGraphElementData>();

        foreach (var element in Elements.OfType<IPersistentElementModel>())
        {
            var s = element.Serialize();
            eventSubject.RequestMutation(s);
            
            elements.Add(s);
        }
        
        return new NodeGraph(GraphName.Value, elements, Connections);
    }
    
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        if (!disposing) return;
        
        Elements.OfType<IDisposable>().DisposeAll();
        
        graphName.Dispose();
        eventSubject.Dispose();
        connections.Dispose();
        elements.Dispose();
    }

    private class NodeCanvasMutationProvider : INodeCanvasMutationProvider
    {
        private readonly ISourceCache<IGraphElementModel, string> elements;
        private readonly ISourceList<Connection> connections;
        
        public NodeCanvasMutationProvider(ISourceCache<IGraphElementModel, string> elements, ISourceList<Connection> connections)
        {
            this.elements = elements;
            this.connections = connections;
        }
        
        public void AddElement(IGraphElementModel element)
        {
            if (elements.Items.Any(x => x.ElementId == element.ElementId))
            {
                throw new Exception($"Element collision detected on: {element.ElementId}");
            }
            
            elements.AddOrUpdate(element);
        }

        public void RemoveElement(IGraphElementModel element)
        {
            elements.Remove(element);
            
            if (element is IDisposable d)
            {
                d.Dispose();
            }
        }

        public void AddConnection(Connection connection)
        {
            connections.Add(connection);
        }

        public void RemoveConnection(Connection connection)
        {
            connections.Remove(connection);
        }
    }
}