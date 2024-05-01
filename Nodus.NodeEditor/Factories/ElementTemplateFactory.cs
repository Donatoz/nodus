using Nodus.Core.Extensions;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Factories;

public class ElementTemplateFactory : GenericFactory<IGraphElementData, IGraphElementTemplate>
{
    public ElementTemplateFactory()
    {
        RegisterSubFactory(new SubFactory(typeof(NodeData), x => new NodeTemplate(x.MustBe<NodeData>())));
        RegisterSubFactory(new SubFactory(typeof(CommentData), x => new ElementTemplate<CommentData>(x.MustBe<CommentData>())));
    }
}