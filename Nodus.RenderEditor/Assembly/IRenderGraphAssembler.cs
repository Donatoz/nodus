using System;
using System.Linq;
using Nodus.NodeEditor.Meta;
using Nodus.RenderEditor.Models;
using Nodus.RenderEditor.Models.Contexts;
using Nodus.RenderEngine.OpenGL;
using Silk.NET.OpenGL;
using IRenderContext = Nodus.RenderEngine.Common.IRenderContext;

namespace Nodus.RenderEditor.Assembly;

public interface IRenderGraphAssembler
{
    IRenderContext CreateRenderContext(IRenderNodeModel root, GraphContext graph);
}

public class GlRenderGraphAssembler : IRenderGraphAssembler
{
    public IRenderContext CreateRenderContext(IRenderNodeModel root, GraphContext graph)
    {
        if (root.Context.Value is not IRenderMasterContext masterContext)
        {
            throw new Exception(
                $"Failed to create render context: root has to have a context of {typeof(IRenderMasterContext)} type.");
        }
        
        var descriptor = masterContext.GetDescriptor(graph);
        
        return new GlPrimitiveContext(Array.Empty<IGlShaderUniform>(), Array.Empty<IGlTextureDefinition>());
    }
}