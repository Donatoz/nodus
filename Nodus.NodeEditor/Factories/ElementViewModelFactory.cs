using Nodus.Core.Extensions;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.NodeEditor.Factories;

public class ElementViewModelFactory : GenericFactory<IGraphElementModel, ElementViewModel>
{
    public ElementViewModelFactory(IFactoryProvider<NodeCanvasViewModel> factoryProvider)
    {
        var nodeFactory = factoryProvider.GetFactory<INodeViewModelFactory>();
        var commentFactory = factoryProvider.GetFactory<ICommentViewModelFactory>();
        
        RegisterSubFactory(new SubFactory(typeof(INodeModel), x => nodeFactory.Create(x.MustBe<INodeModel>())));
        RegisterSubFactory(new SubFactory(typeof(ICommentModel), x => commentFactory.Create(x.MustBe<ICommentModel>())));
    }
}