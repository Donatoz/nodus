using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Rendering.Composition;
using Nodus.Core.Controls;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using ReactiveUI;

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

    protected override void OnInitialized()
    {
        base.OnInitialized();

        Container.AttachedToVisualTree += OnContainerAttached;
    }

    private void OnContainerAttached(object _, VisualTreeAttachmentEventArgs __)
    {
        var v = ElementComposition.GetElementVisual(Container);
        
        if (v == null) return;

        var c = v.Compositor;

        var anim = c.CreateVector2KeyFrameAnimation();
        anim.Target = "Size";
        anim.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
        anim.Duration = TimeSpan.FromMilliseconds(100);
        var col = c.CreateImplicitAnimationCollection();
        col["Size"] = anim;

        v.ImplicitAnimations = col;
        
        Trace.WriteLine($"Added anims");
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is NodeSearchModalViewModel { SearchContent: not null } vm)
        {
            SearchBox.Text = vm.SearchContent;
            FilterNodes(vm.SearchContent);
        }
    }

    private void OnConfirm()
    {
        var firstVisible = NodesContainer.GetRealizedContainers().FirstOrDefault(x => x.IsVisible);
        
        if (firstVisible == null) return;

        if (DataContext is NodeSearchModalViewModel vm && firstVisible.DataContext is NodeSearchModalItemViewModel item)
        {
            vm.CreateNode(item.Template);
        }
        
        RaiseEvent(new ModalStateEventArgs(false) {RoutedEvent = ModalStateChangedEvent});
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (SearchBox.Text == null) return;

        if (DataContext is NodeSearchModalViewModel { KeepSearchContent.Value: true } vm)
        {
            vm.SearchContent = SearchBox.Text;
        }

        // Stupid hack to confirm the search
        if (SearchBox.Text.EndsWith(Environment.NewLine))
        {
            OnConfirm();
            return;
        }

        var text = SearchBox.Text.Trim().ToLower();
        
        FilterNodes(text);
    }

    private void FilterNodes(string filter)
    {
        //TODO: Optimize this
        foreach (var item in NodesContainer.Items.OfType<NodeSearchModalItemViewModel>())
        {
            var c = NodesContainer.ContainerFromItem(item);
            if (c == null) continue;

            c.IsVisible = item.Label.ToLower().Contains(filter);
        }
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