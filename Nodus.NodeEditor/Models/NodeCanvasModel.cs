using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
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
    IReactiveProperty<IEnumerable<INodeModel>> Nodes { get; }
    IReactiveProperty<IEnumerable<Connection>> Connections { get; }
    ICanvasOperatorModel Operator { get; }
    /// <summary>
    /// Graph representation of the node canvas.
    /// </summary>
    GraphContext Context { get; }

    void LoadGraph(NodeGraph graph);
    NodeGraph SerializeToGraph();
}

public interface INodeCanvasMutationProvider
{
    void AddNode(INodeModel node);
    void RemoveNode(INodeModel node);
    void AddConnection(Connection connection);
    void RemoveConnection(Connection connection);
}

public class NodeCanvasModel : Entity, INodeCanvasModel
{
    public override string EntityId { get; }
    public IReactiveProperty<string> GraphName => graphName;
    public IObservable<IEvent> EventStream => eventSubject;
    public IReactiveProperty<IEnumerable<INodeModel>> Nodes => nodes;
    public IReactiveProperty<IEnumerable<Connection>> Connections => connections;
    /// <summary>
    /// Returns a NEW graph context instance, meaning that with EACH getter call
    /// it will allocate new memory for the context cache.
    /// </summary>
    public GraphContext Context => new(Nodes.Value, Connections.Value);

    public ICanvasOperatorModel Operator { get; }
    
    private readonly MutableReactiveProperty<string> graphName;
    private readonly Subject<IEvent> eventSubject;
    private readonly MutableReactiveProperty<IList<INodeModel>> nodes;
    private readonly MutableReactiveProperty<IList<Connection>> connections;

    protected INodeCanvasMutationProvider MutationProvider { get; }
    protected INodeContextProvider NodeContextProvider { get; }
    protected virtual string DefaultGraphName => "New Graph";
    
    public NodeCanvasModel(IComponentFactoryProvider<INodeCanvasModel> componentFactoryProvider, INodeContextProvider contextProvider)
    {
        EntityId = Guid.NewGuid().ToString();
        graphName = new MutableReactiveProperty<string>(DefaultGraphName);
        eventSubject = new Subject<IEvent>();
        nodes = new MutableReactiveProperty<IList<INodeModel>>(new List<INodeModel>());
        connections = new MutableReactiveProperty<IList<Connection>>(new List<Connection>());

        NodeContextProvider = contextProvider;
        MutationProvider = CreateMutationProvider();
        Operator = CreateOperator(componentFactoryProvider, contextProvider);

        this.AddComponent(new ValueContainer<INodeSearchModalModel>(new NodeSearchModalModel()));
    }

    protected virtual INodeCanvasMutationProvider CreateMutationProvider()
    {
        return new NodeCanvasMutationProvider(nodes, connections);
    }

    protected virtual NodeCanvasOperatorModel CreateOperator(IComponentFactoryProvider<INodeCanvasModel> componentFactoryProvider, INodeContextProvider contextProvider)
    {
        return new NodeCanvasOperatorModel(this, MutationProvider, componentFactoryProvider, contextProvider);
    }
    
    public virtual void LoadGraph(NodeGraph graph)
    {
        nodes.ClearAndInvalidate();
        connections.ClearAndInvalidate();
        
        graphName.SetValue(graph.GraphName);
        
        graph.Nodes.ForEach(x => Operator.CreateNode(new NodeTemplate(x)));
        graph.Connections.ForEach(x => Operator.Connect(x));
    }

    public virtual NodeGraph SerializeToGraph()
    {
        var nodes = new List<NodeData>();

        foreach (var node in Nodes.Value)
        {
            var s = node.Serialize();
            eventSubject.RequestMutation(s);
            
            nodes.Add(s);
        }
        
        return new NodeGraph(GraphName.Value,
            nodes,
            Connections.Value);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        
        nodes.Value.DisposeAll();
            
        graphName.Dispose();
        eventSubject.Dispose();
        nodes.Dispose();
        connections.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private class NodeCanvasMutationProvider : INodeCanvasMutationProvider
    {
        private readonly MutableReactiveProperty<IList<INodeModel>> nodes;
        private readonly MutableReactiveProperty<IList<Connection>> connections;
        
        public NodeCanvasMutationProvider(MutableReactiveProperty<IList<INodeModel>> nodes,
            MutableReactiveProperty<IList<Connection>> connections)
        {
            this.nodes = nodes;
            this.connections = connections;
        }
        
        public void AddNode(INodeModel node)
        {
            nodes.AddAndInvalidate(node);
        }

        public void RemoveNode(INodeModel node)
        {
            nodes.RemoveAndInvalidate(node);
        }

        public void AddConnection(Connection connection)
        {
            connections.AddAndInvalidate(connection);
        }

        public void RemoveConnection(Connection connection)
        {
            connections.RemoveAndInvalidate(connection);
        }
    }
}