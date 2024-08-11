using Nodus.Core.Extensions;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

/// <summary>
/// Represents a material used for rendering objects in OpenGL context renderers.
/// </summary>
public interface IGlMaterial : IDisposable
{
    /// <summary>
    /// The render priority of the material.
    /// </summary>
    uint RenderPriority { get; }
    string MaterialId { get; }

    /// <summary>
    /// Use the material bound shader program.
    /// </summary>
    void Use();

    /// <summary>
    /// Update the definition of the material, making the material to update its shader program.
    /// </summary>
    /// <param name="definition">The new material definition.</param>
    void UpdateDefinition(IGlMaterialDefinition definition);

    /// <summary>
    /// Apply a uniform value to the material shader program.
    /// </summary>
    /// <param name="uniform">The shader uniform to apply.</param>
    void ApplyUniform(IGlShaderUniform uniform);
}

public class GlMaterial : IGlMaterial
{
    public uint RenderPriority { get; }
    public string MaterialId => definition.MaterialId;

    private GlShaderProgram? program;
    private IGlMaterialDefinition definition = null!;
    
    private readonly GL gl;
    
    public GlMaterial(GL context, IGlMaterialDefinition definition)
    {
        gl = context;
        RenderPriority = definition.RenderPriority;
        
        UpdateDefinition(definition);
    }

    public void Use()
    {
        program!.Use();

        definition.Textures.ForEach(x => x.TryBind());
        definition.Uniforms.OfType<IGlShaderUniform>().ForEach(program.ApplyUniform);
        
        gl.TryThrowNextError($"Failed to use material: {MaterialId}");
    }

    public void UpdateDefinition(IGlMaterialDefinition newDefinition)
    {
        definition = newDefinition;
        UpdateProgram();
    }

    private void UpdateProgram()
    {
        program?.Dispose();
        
        var shaders = definition.Shaders
            .Select(x => new GlShader(gl, x.Source, x.Type.ToShaderType()) as IGlShader).ToArray();
        program = new GlShaderProgram(gl, shaders);

        shaders.DisposeAll();
    }

    public void ApplyUniform(IGlShaderUniform uniform)
    {
        program!.ApplyUniform(uniform);
    }

    public void Dispose()
    {
        program!.Dispose();
    }
}