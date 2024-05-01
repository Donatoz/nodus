using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Windows.Input;
using Avalonia.Input;
using DynamicData;
using DynamicData.Alias;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using Nodus.Core.Common;
using Nodus.Core.Entities;
using Nodus.Core.Extensions;
using Nodus.Core.Interaction;
using Nodus.Core.Reactive;
using Nodus.Core.Selection;
using Nodus.Core.ViewModels;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels.Events;
using Nodus.NodeEditor.Views;
using ReactiveUI;

namespace Nodus.NodeEditor.ViewModels;

public interface INodeCanvasViewModel : IEntity { }

public interface INodeCanvasOperatorViewModel
{
    void CreateElement(IGraphElementTemplate template);
    void CreateConnection(string sourceNode, string sourcePort, string targetNode, string targetPort);
    void RemoveConnection(ConnectionViewModel connection);
}

/// <summary>
/// Represents the view model for a node-based canvas.
/// </summary>
public class NodeCanvasViewModel : ReactiveViewModel, INodeCanvasOperatorViewModel, INodeCanvasViewModel
{
    /// <summary>
    /// Represents a selector for nodes.
    /// </summary>
    public ISelector<ElementViewModel> ElementSelector { get; }
    public NodeCanvasToolbarViewModel Toolbar { get; }
    public NodeContextContainerViewModel NodeContextContainer { get; }
    public BoundProperty<string> GraphName { get; }
    public ReadOnlyObservableCollection<ElementViewModel> Elements => elements;
    public ReadOnlyObservableCollection<ConnectionViewModel> Connections => connections;

    public ICommand RequestElementSelectionCommand { get; }
    public ICommand AddNodeCommand { get; }
    public ICommand AddCommentCommand { get; }
    public ICommand RemoveSelectedCommand { get; }

    private readonly NodeSearchModalViewModel? nodeSearchModal;
    private readonly CompositeDisposable disposables;
    private readonly IFactory<IGraphElementModel, ElementViewModel> elementsFactory;

    private readonly ReadOnlyObservableCollection<ElementViewModel> elements;
    private readonly ReadOnlyObservableCollection<ConnectionViewModel> connections;
    
    protected INodeCanvasModel Model { get; }
    protected INodeCanvasViewModelComponentFactory ComponentFactory { get; }

    /// <summary>
    /// Initialize a new instance of the NodeCanvasViewModel class.
    /// </summary>
    /// <param name="model">The INodeCanvasModel instance.</param>
    /// <param name="serviceProvider">Service provider</param>
    /// <param name="elementsFactoryProvider">Elements factory</param>
    /// <param name="componentFactory">VM components factory</param>
    public NodeCanvasViewModel(INodeCanvasModel model, IServiceProvider serviceProvider, INodeCanvasViewModelComponentFactory componentFactory,
        IFactory<IGraphElementModel, ElementViewModel> elementsFactory)
    {
        Model = model;
        ComponentFactory = componentFactory;
        this.elementsFactory = elementsFactory;
        disposables = new CompositeDisposable();
        
        ElementSelector = new Selector<ElementViewModel>();

        model.ElementStream
            .ObserveOn(RxApp.MainThreadScheduler)
            .Transform(CreateElement)
            .Bind(out elements)
            .Subscribe()
            .DisposeWith(disposables);

        model.ConnectionStream
            .ObserveOn(RxApp.MainThreadScheduler)
            .Transform(x => componentFactory.CreateConnection(x, elements.OfType<NodeViewModel>(), this))
            .Bind(out connections)
            .Subscribe()
            .DisposeWith(disposables);
        
        model.EventStream
            .OnEvent<MutationEvent<IGraphElementData>>(OnElementDataMutation)
            .DisposeWith(disposables);

        Elements
            .ToObservableChangeSet()
            .Subscribe(OnElementsChange)
            .DisposeWith(disposables);
        
        Toolbar = componentFactory.CreateToolbar(serviceProvider, model, this);

        var nodeSelectionStream = ElementSelector.SelectedStream
            .SelectMany(x => x)
            .Select(_ =>
            {
                var selectedNodes = ElementSelector.CurrentlySelected.OfType<NodeViewModel>();
                return selectedNodes.Count() == 1 ? selectedNodes.First() : null;
            });
        
        NodeContextContainer =
            componentFactory.CreateNodeContextContainer(() => model.Elements.OfType<INodeModel>(), nodeSelectionStream);

        RequestElementSelectionCommand = ReactiveCommand.Create<ElementViewModel?>(OnElementSelectionRequested);
        AddNodeCommand = ReactiveCommand.Create(CreateNewNode);
        RemoveSelectedCommand = ReactiveCommand.Create(RemoveCurrent);
        AddCommentCommand = ReactiveCommand.Create(CreateComment);

        GraphName = model.GraphName.ToBound();

        serviceProvider.GetRequiredService<IHotkeyBinder>()
            .BindHotkey(KeyGesture.Parse("Delete"), RemoveSelectedCommand)
            .DisposeWith(disposables);
        
        if (model.TryGetGeneric<IContainer<INodeSearchModalModel>>(out var c))
        {
            nodeSearchModal = componentFactory.CreateSearchModal(this, c.Value);
        }

        this.AddComponent(new DisposableContainer<ModalCanvasViewModel>(componentFactory.CreateModalCanvas()));
        this.AddComponent(new DisposableContainer<PopupContainerViewModel>(componentFactory.CreatePopupContainer()));
    }

    private void OnElementSelectionRequested(ElementViewModel? element)
    {
        if (element == null)
        {
            ElementSelector.DeselectAll();
        }
        else
        {
            ElementSelector.Select(element);
        }
    }

    private void RemoveCurrent()
    {
        ElementSelector.CurrentlySelected.ForEach(RemoveElement);
    }

    private void OnElementsChange(IChangeSet<ElementViewModel> changes)
    {
        if (changes.Removes > 0)
        {
            changes
                .Where(x => x is { Reason: ListChangeReason.Remove or ListChangeReason.Clear, Item.Current: ISelectable })
                .ForEach(x =>
                {
                    if (ElementSelector.CurrentlySelected.Contains(x.Item.Current))
                    {
                        ElementSelector.Deselect(x.Item.Current);
                    }

                    if (x.Item.Current is IDisposable d)
                    {
                        d.Dispose();
                    }
                });
        }
    }

    private ElementViewModel CreateElement(IGraphElementModel model)
    {
        var element = elementsFactory.Create(model);

        element.EventStream
            .OfType<ElementDeleteRequest>()
            .Subscribe(x => RemoveElement(x.Element))
            .DisposeWith(disposables);

        return element;
    }
    
    private void OnElementDataMutation(MutationEvent<IGraphElementData> evt)
    {
        evt.MutatedValue.VisualData ??= new VisualGraphElementData();

        Trace.WriteLine($"---------- Mutated: {evt.MutatedValue}");
        Trace.WriteLine($"---------- Id: {evt.MutatedValue.ElementId}");
        elements.ForEach(x => Trace.WriteLine($"---------------- {x.ElementId}"));
        
        var elementVm = elements.FirstOrDefault(x => x.ElementId == evt.MutatedValue.ElementId)
            .NotNull($"Failed to find mutated element view model: {evt.MutatedValue.ElementId}");
        
        RaiseEvent(new ElementVisualMutationEvent(elementVm, evt.MutatedValue.VisualData));
    }

    protected void RemoveElement(ElementViewModel element)
    {
        Model.Operator.RemoveElement(element.ElementId);
    }
    
    private void CreateNewNode()
    {
        if (nodeSearchModal == null) return;
        
        this.TryGetGeneric<IContainer<ModalCanvasViewModel>>()?.Value.OpenModal(nodeSearchModal);
    }

    private void CreateComment()
    {
        Model.Operator.CreateElement(new ElementTemplate<CommentData>(new CommentData("New Comment")));
    }

    public void CreateElement(IGraphElementTemplate template)
    {
        Model.Operator.CreateElement(template);
    }

    public void CreateConnection(string sourceNode, string sourcePort, string targetNode, string targetPort)
    {
        Model.Operator.Connect(sourceNode, sourcePort, targetNode, targetPort);
    }
    
    public void RemoveConnection(ConnectionViewModel connection)
    {
        Model.Operator.Disconnect(connection.Data);
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        if (!disposing) return;
        
        nodeSearchModal?.Dispose();
        NodeContextContainer.Dispose();
        ElementSelector.Dispose();
        disposables.Dispose();
    }
}