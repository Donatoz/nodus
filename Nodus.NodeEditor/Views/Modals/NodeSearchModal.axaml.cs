using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Nodus.Core.Controls;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.NodeEditor.Views.Modals;

public partial class NodeSearchModal : Modal
{
    internal static readonly StyledProperty<IEnumerable<NodeSearchModalItemViewModel>> NodesProperty;

    internal IEnumerable<NodeSearchModalItemViewModel> Nodes
    {
        get => GetValue(NodesProperty);
        set => SetValue(NodesProperty, value);
    }

    
    static NodeSearchModal()
    {
        NodesProperty = AvaloniaProperty.Register<NodeSearchModal, IEnumerable<NodeSearchModalItemViewModel>>(nameof(Nodes));
    }
    
    public NodeSearchModal()
    {
        InitializeComponent();
        
        SearchBox.AddHandler(TextBox.TextChangedEvent, OnSearchTextChanged);
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        
    }

    private void OnNodeSelected(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Captured is Control {DataContext: NodeSearchModalItemViewModel item} && DataContext is NodeSearchModalViewModel vm)
        {
            vm.CreateNode(item.Template);
        }
        
        RaiseEvent(new ModalStateEventArgs(false) {RoutedEvent = ModalStateChangedEvent});
    }

    private void OnItemInitialized(object? sender, EventArgs e)
    {
        if (sender is not Border { DataContext: NodeSearchModalItemViewModel vm } b) return;

        if (vm.Template.Data.Group != null)
        {
            b.Classes.Add(NodeGroups.NodeGroupPrefix + vm.Template.Data.Group);
        }
    }
}