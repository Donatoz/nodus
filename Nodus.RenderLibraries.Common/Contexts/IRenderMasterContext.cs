using System.Diagnostics;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.RenderEditor.Assembly;
using Nodus.RenderEditor.Models;
using Nodus.RenderEditor.Models.Contexts;
using Nodus.RenderEngine.Avalonia;
using Nodus.RenderEngine.Common;

namespace Nodus.RenderLibraries.Common.Contexts;

public class QuadMasterContext : NodeContextBase<IRenderNodeModel>, IRenderMasterContext
{
    private IRenderPortModel? inputPort;
    
    public override void Bind(INodeModel node)
    {
        base.Bind(node);
        
        inputPort = Node!.Ports.Value.OfType<IRenderPortModel>()
            .FirstOrDefault(x => x.Type == PortType.Input && x.ValueType.Value == typeof(IFragmentStageMeta))
            .NotNull("Quad master context must have at least one IFragmentStageMeta input port.");
    }

    public IRenderDescriptor GetDescriptor(GraphContext context)
    {
        var fragMeta = ResolvePortValue(inputPort.NotNull(), context).MustBe<IFragmentStageMeta>();
        
        return new RenderDescriptor(new[]
        {
            new ShaderDefinition(new ShaderUriSource(new Uri("avares://Nodus.RenderEngine.Avalonia/Assets/Shaders/example.vert")), ShaderSourceType.Vertex),
            fragMeta.FragmentShader
        });
    }

    public override void Deserialize(NodeContextData data) {}

    public override NodeContextData? Serialize() => null;
}