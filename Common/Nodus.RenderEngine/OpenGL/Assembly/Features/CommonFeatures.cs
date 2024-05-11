using Nodus.Core.Extensions;
using Nodus.RenderEngine.Assembly;

namespace Nodus.RenderEngine.OpenGL.Assembly;

public record GlVersionFeature : IGlShaderAssemblyFeature
{
    public uint AssemblyPriority => 0;
    public ushort Version { get; }
    public GlShaderVersionType Type { get; }
    
    public GlVersionFeature(ushort version, GlShaderVersionType type)
    {
        Version = version;
        Type = type;
    }

    public IShaderAssemblyToken GetToken(IShaderAssemblyContext context)
    {
        return new GenericShaderAssemblyToken(d =>
        {
            d.SourceBuilder.AppendLine($"#version {Version} {VersionTypeToString()}");

            if (Type == GlShaderVersionType.Es)
            {
                d.SourceBuilder.AppendLine("precision mediump float;");
            }
        });
    }

    private string VersionTypeToString()
    {
        return Type == GlShaderVersionType.Core ? "core" : "es";
    }

    public enum GlShaderVersionType
    {
        Core,
        Es
    }
}

public record GlUniformFeature(string UniformName, string UniformType)
    : SingleLineFeature($"uniform {UniformType} {UniformName};"), IGlShaderAssemblyFeature
{
    public uint AssemblyPriority => 11;
}

public record GlVaryingFeature : IGlShaderAssemblyFeature
{
    public uint AssemblyPriority => 10;
    public string VaryingName { get; }
    public bool IsInput { get; }
    public string Type { get; }
    
    public GlVaryingFeature(bool isInput, string name, string type)
    {
        IsInput = isInput;
        VaryingName = name;
        Type = type;
    }

    public IShaderAssemblyToken GetToken(IShaderAssemblyContext context)
    {
        return new GenericShaderAssemblyToken(d =>
        {
            var version = context.GetVersion().NotNull("Varying feature must precess the version feature.");
            if (version.Type != GlVersionFeature.GlShaderVersionType.Es && IsInput)
            {
                var layoutLocation = context.FeaturesBefore(this).OfType<GlVaryingFeature>().Count(x => x.IsInput);
                d.SourceBuilder.Append($"layout (location = {layoutLocation}) ");
            }
            
            d.SourceBuilder.Append((IsInput ? "in" : "out") + $" {Type} {VaryingName};");
            d.SourceBuilder.AppendLine();
        });
    }
}