using System.Diagnostics;
using System.Numerics;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.Extensions;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.RenderEditor.Assembly;
using Nodus.RenderEditor.Models;
using Nodus.RenderEditor.Models.Contexts;
using Nodus.RenderEngine.Assembly;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.OpenGL.Assembly;

namespace Nodus.RenderLibraries.Common.Contexts;

public interface IShaderStageContext : INodeContext
{
}

public class FragmentStageContext : NodeContextBase<IRenderNodeModel>, IShaderStageContext
{
    private IRenderPortModel? inPort;

    private readonly ModularShaderAssembler shaderAssembler;

    public FragmentStageContext()
    {
        shaderAssembler = new ModularShaderAssembler();
    }
    
    public override void Bind(INodeModel node)
    {
        base.Bind(node);

        inPort = Node!.Ports.Value.OfType<IRenderPortModel>()
            .FirstOrDefault(x => x.Type == PortType.Input && x.ValueType.Value == typeof(Vector4))
            .NotNull("Fragment stage context must have at least one vec4 input port.");
        
        TryBindFirstOutPort(ctx => GetStageMeta(ctx));
    }

    private FragmentStageMeta GetStageMeta(GraphContext ctx)
    {
        if (inPort == null || Node == null) return default;
        
        shaderAssembler.ClearFeatures();
        var transpiler = new GlslFeatureTranspiler();

        ctx.GetSortedPredecessors(Node, inPort)
            .Select(x => x.Context.Value)
            .OfType<IShaderAssemblyFeatureContext>()
            .Select(x => x.GetFeature(ctx))
            .ForEach(x => shaderAssembler.AddFeature(x));
        
        var source = shaderAssembler.AssembleShader(transpiler).EvaluateToSource();
        
        return new FragmentStageMeta(new ShaderDefinition(new ShaderStaticSource(source), ShaderSourceType.Fragment));
    }

    public override void Deserialize(NodeContextData data) {}
    public override NodeContextData? Serialize() => null;
}