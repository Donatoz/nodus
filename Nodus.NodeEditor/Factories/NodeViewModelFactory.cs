using System.Diagnostics;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.NodeEditor.Factories;

public static class NodeViewModelFactory
{
    public static NodeViewModel Create(INodeModel model)
    {
        return new NodeViewModel(model);
    }
}