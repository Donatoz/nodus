using System;
using System.Collections.Generic;
using Nodus.Core.ViewModels;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.NodeEditor.Factories;

/// <summary>
/// Represents a factory for creating various view model components for a node canvas.
/// </summary>
public interface INodeCanvasViewModelComponentFactory
{
    /// <summary>
    /// Create a toolbar for the NodeCanvasViewModel.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve dependencies.</param>
    /// <param name="canvasModel">The NodeCanvasViewModel associated with the toolbar.</param>
    NodeCanvasToolbarViewModel CreateToolbar(IServiceProvider serviceProvider, INodeCanvasModel canvasModel, INodeCanvasViewModel viewModel);

    /// <summary>
    /// Create a ModalCanvasViewModel instance.
    /// </summary>
    ModalCanvasViewModel CreateModalCanvas();

    /// <summary>
    /// Create a search modal for nodes.
    /// </summary>
    /// <param name="canvasOperator">The canvas operator to perform operations on the node canvas.</param>
    /// <param name="model">The model representing the search modal.</param>
    NodeSearchModalViewModel CreateSearchModal(INodeCanvasOperatorViewModel canvasOperator, INodeSearchModalModel model);

    /// <summary>
    /// Create a connection between two nodes.
    /// </summary>
    /// <param name="model">The connection model.</param>
    /// <param name="nodes">The collection of nodes.</param>
    /// <param name="canvasOperator">The node canvas operator.</param>
    ConnectionViewModel CreateConnection(Connection model, IEnumerable<NodeViewModel> nodes, INodeCanvasOperatorViewModel canvasOperator);

    NodeContextContainerViewModel CreateNodeContextContainer(Func<IEnumerable<INodeModel>> nodesGetter,
        IObservable<NodeViewModel?> nodeChangeStream);

    PopupContainerViewModel CreatePopupContainer();
}

public class NodeCanvasViewModelComponentFactory : INodeCanvasViewModelComponentFactory
{
    public virtual NodeCanvasToolbarViewModel CreateToolbar(IServiceProvider serviceProvider, INodeCanvasModel canvasModel, INodeCanvasViewModel viewModel)
    {
        return new NodeCanvasToolbarViewModel(serviceProvider, canvasModel, viewModel);
    }

    public virtual ModalCanvasViewModel CreateModalCanvas()
    {
        return new ModalCanvasViewModel();
    }

    public virtual NodeSearchModalViewModel CreateSearchModal(INodeCanvasOperatorViewModel canvasOperator, INodeSearchModalModel model)
    {
        return new NodeSearchModalViewModel(canvasOperator, model);
    }

    public virtual ConnectionViewModel CreateConnection(Connection model, IEnumerable<NodeViewModel> nodes, INodeCanvasOperatorViewModel canvasOperator)
    {
        return new ConnectionViewModel(model, nodes, canvasOperator);
    }

    public virtual NodeContextContainerViewModel CreateNodeContextContainer(Func<IEnumerable<INodeModel>> nodesGetter, IObservable<NodeViewModel?> nodeChangeStream)
    {
        return new NodeContextContainerViewModel(nodesGetter, nodeChangeStream);
    }

    public virtual PopupContainerViewModel CreatePopupContainer()
    {
        return new PopupContainerViewModel();
    }
}