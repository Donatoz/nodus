using System;
using System.Collections.Generic;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.RenderEditor.ViewModels.Contexts;

public class RenderContextContainerViewModel : NodeContextContainerViewModel
{
    public RenderContextContainerViewModel(Func<IEnumerable<INodeModel>> nodesGetter, IObservable<NodeViewModel?> nodeChangeStream) : base(nodesGetter, nodeChangeStream)
    {
    }
}