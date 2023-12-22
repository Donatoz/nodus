using Nodus.Core.Common;
using Nodus.NodeEditor.Meta;

namespace Nodus.NodeEditor.ViewModels.Events;

public readonly struct NodeVisualMutationEvent : IMutationEvent<NodeVisualData>
{
    public NodeViewModel ViewModel { get; }
    public NodeVisualData MutatedValue { get; }

    public NodeVisualMutationEvent(NodeViewModel viewModel, NodeVisualData mutatedValue)
    {
        ViewModel = viewModel;
        MutatedValue = mutatedValue;
    }
}