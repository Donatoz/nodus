using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Nodus.Core.Common;
using Nodus.Core.Extensions;
using Nodus.Core.Reactive;
using Nodus.NodeEditor.Extensions;
using Nodus.NodeEditor.Meta;

namespace Nodus.NodeEditor.Models;

public interface INodeCanvasModel : IObservable<IEvent>
{
    IObservable<IEvent> EventStream { get; }
    
    IReactiveProperty<IEnumerable<INodeModel>> Nodes { get; }
    IReactiveProperty<IEnumerable<Connection>> Connections { get; }
    INodeSearchModalModel SearchModal { get; }
    ICanvasOperatorModel Operator { get; }
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

public class NodeCanvasModel : INodeCanvasModel
{
    private readonly MutableReactiveProperty<string> graphName;
    private readonly Subject<IEvent> eventSubject;
    private readonly MutableReactiveProperty<IList<INodeModel>> nodes;
    private readonly MutableReactiveProperty<IList<Connection>> connections;
    private readonly NodeSearchModalModel searchModal;

    public IReactiveProperty<string> GraphName => graphName;
    public IObservable<IEvent> EventStream => eventSubject;
    public IReactiveProperty<IEnumerable<INodeModel>> Nodes => nodes;
    public IReactiveProperty<IEnumerable<Connection>> Connections => connections;
    public GraphContext Context => new(Nodes.Value, Connections.Value);

    public INodeSearchModalModel SearchModal => searchModal;
    public ICanvasOperatorModel Operator { get; }

    protected INodeCanvasMutationProvider MutationProvider { get; }
    protected virtual string DefaultGraphName => "New Graph";
    
    public NodeCanvasModel()
    {
        graphName = new MutableReactiveProperty<string>(DefaultGraphName);
        eventSubject = new Subject<IEvent>();
        nodes = new MutableReactiveProperty<IList<INodeModel>>(new List<INodeModel>());
        connections = new MutableReactiveProperty<IList<Connection>>(new List<Connection>());
        searchModal = new NodeSearchModalModel();
        
        MutationProvider = CreateMutationProvider();
        Operator = CreateOperator();
    }

    protected virtual INodeCanvasMutationProvider CreateMutationProvider()
    {
        return new NodeCanvasMutationProvider(nodes, connections);
    }

    protected virtual NodeCanvasOperatorModel CreateOperator()
    {
        return new NodeCanvasOperatorModel(this, MutationProvider);
    }
    
    public virtual void LoadGraph(NodeGraph graph)
    {
        nodes.Value.Clear();
        connections.Value.Clear();
        
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

    public IDisposable Subscribe(IObserver<IEvent> observer)
    {
        throw new NotImplementedException();
    }
}