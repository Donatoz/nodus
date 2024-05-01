using Nodus.Core.Extensions;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Factories;

public class ElementFactory : GenericFactory<IGraphElementTemplate, IGraphElementModel>
{
    private readonly INodeContextProvider nodeContextProvider;
    private readonly IFactoryProvider<INodeCanvasModel> canvasFactoryProvider;

    public ElementFactory(INodeContextProvider nodeContextProvider, IFactoryProvider<INodeCanvasModel> canvasFactoryProvider)
    {
        this.nodeContextProvider = nodeContextProvider;
        this.canvasFactoryProvider = canvasFactoryProvider;
        
        RegisterSubFactory(new SubFactory(typeof(IGraphElementTemplate<NodeData>), CreateNode));
        RegisterSubFactory(new SubFactory(typeof(IGraphElementTemplate<CommentData>), CreateComment));
    }

    private IGraphElementModel CreateNode(IGraphElementTemplate template)
    {
        var nodeFactory = canvasFactoryProvider.GetFactory<INodeModelFactory>();
        var portFactory = canvasFactoryProvider.GetFactory<IPortModelFactory>();

        var nodeTemplate = template.MustBe<NodeTemplate>();

        return nodeFactory.CreateNode(
            nodeTemplate.WithContext(nodeContextProvider.TryGetContextFactory(nodeTemplate.Data.ContextId ?? string.Empty)),
            portFactory);
    }

    private IGraphElementModel CreateComment(IGraphElementTemplate template)
    {
        var commentFactory = canvasFactoryProvider.GetFactory<ICommentModelFactory>();

        return commentFactory.CreateComment(template.MustBe<IGraphElementTemplate<CommentData>>().Data);
    }
}