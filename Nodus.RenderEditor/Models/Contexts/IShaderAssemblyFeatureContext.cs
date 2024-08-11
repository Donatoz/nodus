using Nodus.NodeEditor.Meta;
using Nodus.RenderEngine.Assembly;

namespace Nodus.RenderEditor.Models.Contexts;

public interface IShaderAssemblyFeatureContext
{
    IShaderAssemblyFeature GetFeature(GraphContext graph);
}