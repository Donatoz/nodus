using Nodus.Core.Extensions;
using Nodus.RenderEngine.Assembly;
using Nodus.RenderEngine.Common;

namespace Nodus.RenderEngine.OpenGL.Assembly;

public interface IGlShaderAssembly : IShaderAssembly
{
    GlShader EvaluateToShader();
}

public readonly struct GlShaderAssemblyContext : IShaderAssemblyContext
{
    public IReadOnlyCollection<IShaderAssemblyFeature> EffectiveFeatures { get; }

    public GlShaderAssemblyContext(IReadOnlyCollection<IShaderAssemblyFeature> effectiveFeatures)
    {
        EffectiveFeatures = effectiveFeatures;
    }
}


public class GlShaderAssembler : IShaderAssembler
{
    private readonly GlVersionFeature version;
    private readonly IList<GlUniformFeature> uniforms;
    private readonly IList<GlVaryingFeature> varyings;
    private readonly IList<IGlShaderBodyFeature> bodies;
    
    public GlShaderAssembler(GlVersionFeature version)
    {
        this.version = version;

        uniforms = new List<GlUniformFeature>();
        varyings = new List<GlVaryingFeature>();
        bodies = new List<IGlShaderBodyFeature>();
    }

    public void AddUniform(GlUniformFeature feature)
    {
        if (uniforms.Any(x => x.UniformName == feature.UniformName))
        {
            throw new ArgumentException($"Failed to add uniform feature ({feature.UniformName}): uniform already exists.");
        }
        
        uniforms.Add(feature);
    }

    public void AddVarying(GlVaryingFeature feature)
    {
        if (varyings.Any(x => x.VaryingName == feature.VaryingName))
        {
            throw new ArgumentException($"Failed to add varying feature ({feature.VaryingName}): varying variable already exists.");
        }
        
        varyings.Add(feature);
    }

    public void AddBody(IGlShaderBodyFeature bodyFeature)
    {
        if (bodies.Any(x => x.BodyName == bodyFeature.BodyName))
        {
            throw new ArgumentException($"Failed to add body feature ({bodyFeature.BodyName}): body already exists.");
        }
        
        bodies.Add(bodyFeature);
    }
    
    public IShaderAssembly AssembleShader(ShaderSourceType type)
    {
        var effectiveFeatures = new List<IGlShaderAssemblyFeature>
        {
            version
        };

        var context = new GlShaderAssemblyContext(effectiveFeatures);
        
        effectiveFeatures.AddRange(uniforms);
        effectiveFeatures.AddRange(varyings);
        effectiveFeatures.AddRange(bodies);
        
        return new ShaderAssembly(effectiveFeatures
            .OrderBy(x => x.AssemblyPriority)
            .Select(x => x.GetToken(context)));
    }
}