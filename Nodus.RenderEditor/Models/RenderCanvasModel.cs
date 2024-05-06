using Nodus.DI.Factories;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.RenderEditor.Models;

public class RenderCanvasModel : NodeCanvasModel
{
    public RenderCanvasModel(INodeContextProvider contextProvider, 
        IFactory<IGraphElementTemplate, IGraphElementModel> elementFactory, 
        IFactory<IGraphElementData, IGraphElementTemplate> templateFactory) : base(contextProvider, elementFactory, templateFactory)
    {
    }
    
    
}