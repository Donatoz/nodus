using Nodus.Core.Common;
using Nodus.NodeEditor.Meta;

namespace Nodus.NodeEditor.ViewModels.Events;

public readonly struct ElementVisualMutationEvent : IMutationEvent<VisualGraphElementData>
{
    public ElementViewModel ViewModel { get; }
    public VisualGraphElementData MutatedValue { get; }

    public ElementVisualMutationEvent(ElementViewModel viewModel, VisualGraphElementData mutatedValue)
    {
        ViewModel = viewModel;
        MutatedValue = mutatedValue;
    }
}