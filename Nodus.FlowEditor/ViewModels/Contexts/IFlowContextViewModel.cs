using System;
using System.Collections.Generic;
using Nodus.Core.ObjectDescription;
using Nodus.Core.Reactive;
using Nodus.NodeEditor.ViewModels;

namespace FlowEditor.ViewModels.Contexts;

public interface IFlowContextViewModel : INodeContextViewModel, IDisposable
{
    IReactiveProperty<IEnumerable<PropertyEditorViewModel>> Editors { get; }
}