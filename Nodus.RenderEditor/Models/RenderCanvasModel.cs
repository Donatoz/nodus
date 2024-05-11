using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using Nodus.Core.Extensions;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.RenderEditor.Meta;

namespace Nodus.RenderEditor.Models;

public interface IRenderCanvasModel : INodeCanvasModel
{
    IEnumerable<IRenderCanvasVariableModel> Variables { get; }
    IObservable<IChangeSet<IRenderCanvasVariableModel>> VariableStream { get; }

    void AddVariable(string name, Type type, object? initialVal = null);
}

public class RenderCanvasModel : NodeCanvasModel, IRenderCanvasModel
{
    public IEnumerable<IRenderCanvasVariableModel> Variables => variables.Items;
    public IObservable<IChangeSet<IRenderCanvasVariableModel>> VariableStream => variables.Connect();

    private readonly ISourceList<IRenderCanvasVariableModel> variables;
    
    public RenderCanvasModel(INodeContextProvider contextProvider, 
        IFactory<IGraphElementTemplate, IGraphElementModel> elementFactory, 
        IFactory<IGraphElementData, IGraphElementTemplate> templateFactory) : base(contextProvider, elementFactory, templateFactory)
    {
        variables = new SourceList<IRenderCanvasVariableModel>();
    }

    public override NodeGraph SerializeToGraph()
    {
        var baseGraph = base.SerializeToGraph();

        return new RenderGraph(baseGraph, Variables.Select(x => x.Serialize()).ToArray());
    }

    public override void LoadGraph(NodeGraph graph)
    {
        base.LoadGraph(graph);
        
        if (graph is not RenderGraph r) return;

        r.GraphVariables.ForEach(x => AddVariable(x.Name, x.Type, x.Value));
    }

    public void AddVariable(string name, Type type, object? initialVal = null)
    {
        variables.Add(new RenderCanvasVariableModel(name, type, initialVal));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        variables.Dispose();
    }
}