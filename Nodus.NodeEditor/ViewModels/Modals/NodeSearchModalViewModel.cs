using System;
using System.Collections.Generic;
using System.Linq;
using Nodus.Core.Reactive;
using Nodus.NodeEditor.Models;
using ReactiveUI;

namespace Nodus.NodeEditor.ViewModels;

public class NodeSearchModalViewModel : NodeEditorModalViewModel, IDisposable
{
    public MutableReactiveProperty<bool> KeepSearchContent { get; }
    internal string? SearchContent { get; set; }
    public BoundProperty<IEnumerable<NodeSearchModalItemViewModel>> AvailableNodes { get; }

    public NodeSearchModalViewModel(INodeCanvasOperatorViewModel canvasOperator, INodeSearchModalModel model) : base(canvasOperator)
    {
        AvailableNodes = model.AvailableNodes.ToBound(() => model.AvailableNodes.Value.Select(CreateItem));
        KeepSearchContent = new MutableReactiveProperty<bool>();
    }

    public void CreateNode(NodeTemplate template)
    {
        CanvasOperator.CreateNode(template);
    }

    protected virtual NodeSearchModalItemViewModel CreateItem(NodeTemplate template)
    {
        return new NodeSearchModalItemViewModel(template.Data.Title, template);
    }

    public void Dispose()
    {
        AvailableNodes.Dispose();
        KeepSearchContent.Dispose();
    }
}

public class NodeSearchModalItemViewModel
{
    public string Label { get; }
    public NodeTemplate Template { get; }

    public NodeSearchModalItemViewModel(string label, NodeTemplate template)
    {
        Label = label;
        Template = template;
    }
}