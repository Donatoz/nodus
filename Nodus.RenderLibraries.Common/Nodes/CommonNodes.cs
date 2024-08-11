using System.Numerics;
using Nodus.NodeEditor.Extensions;
using Nodus.NodeEditor.Models;
using Nodus.RenderEditor.Assembly;
using Nodus.RenderEditor.Meta;

namespace Nodus.RenderLibraries.Common;

[NodeTemplatesContainer]
public class CommonNodes
{
    public const string VertexAssemblyNodeContextId = "VertexAssembly";
    public const string FragmentAssemblyNodeContextId = "FragmentAssembly";
    public const string QuadMasterNodeContextId = "QuadMaster";
    public const string Vector4NodeContextId = "Vector4";
    
    [NodeTemplateProvider]
    public static IEnumerable<NodeTemplate> GetDefaultTemplates()
    {
        yield return new NodeTemplateBuilder("Vertex Assembly", "Describes the vertex stage")
            .WithInputTypedPort<Vector3>("Position")
            .WithInputTypedPort<Vector2>("TexCoord")
            .WithOutputTypedPort<IRenderMetadata>("Vertex")
            .WithGroup(RenderNodeGroups.RenderGroup)
            .Build();
        
        yield return new NodeTemplateBuilder("Fragment Assembly", "Describes the fragment stage")
            .WithInputTypedPort<Vector4>("Color")
            .WithOutputTypedPort<IFragmentStageMeta>("Fragment")
            .WithGroup(RenderNodeGroups.RenderGroup)
            .WithContextId(FragmentAssemblyNodeContextId)
            .Build();
        
        yield return new NodeTemplateBuilder("Quad Master", "Renders a quad")
            .WithInputTypedPort<IRenderMetadata>("Vertex")
            .WithInputTypedPort<IFragmentStageMeta>("Fragment")
            .WithGroup(RenderNodeGroups.RenderGroup)
            .WithContextId(QuadMasterNodeContextId)
            .Build();
    }

    private static readonly Type[] VectorConstantTypes = { typeof(Vector2), typeof(Vector3), typeof(Vector4) };

    [NodeTemplateProvider]
    public static IEnumerable<NodeTemplate> GetConstantTemplates()
    {
        yield return new NodeTemplateBuilder("Float", "Outputs a float")
            .WithOutputTypedPort<float>("float")
            .WithGroup(RenderNodeGroups.ConstGroup)
            .Build();
        
        yield return new NodeTemplateBuilder("Relay", "Just a relay")
            .WithInputTypedPort<Vector4>("in")
            .WithOutputTypedPort<Vector4>("out")
            .WithGroup(RenderNodeGroups.ConstGroup)
            .Build();
        
        for (var i = 2; i <= 4; i++)
        {
            yield return new NodeTemplateBuilder($"Vector{i}", $"Outputs a {i}D vector")
                .WithOutputTypedPort($"Vec{i}", VectorConstantTypes[i - 2])
                .WithGroup(RenderNodeGroups.ConstGroup)
                .WithContextId(CommonNodes.Vector4NodeContextId)
                .Build();
        }
    }
}