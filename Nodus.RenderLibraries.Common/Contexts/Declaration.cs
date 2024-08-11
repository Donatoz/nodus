using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.RenderEditor.Models;
using Nodus.RenderEditor.Models.Contexts;
using Nodus.RenderEngine.Assembly;

namespace Nodus.RenderLibraries.Common.Contexts;

public class VectorContext : NodeContextBase<IRenderNodeModel>, IShaderAssemblyFeatureContext
{
    public IShaderAssemblyFeature GetFeature(GraphContext graph)
    {
        return new DeclarationFeature(new ShaderVariableDefinition("v_01", "vec4"), null);
    }

    public override void Deserialize(NodeContextData data) {}

    public override NodeContextData? Serialize() => null;
}