using System;
using FlowEditor.Models.Primitives;
using Nodus.Core.Reactive;
using Nodus.NodeEditor.ViewModels;

namespace FlowEditor.ViewModels.Contexts;

public interface IFlowContextViewModel : INodeContextViewModel, IDisposable
{
    IReactiveProperty<DescriptionProvider?> DescribedContext { get; }
}