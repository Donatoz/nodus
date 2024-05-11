namespace Nodus.RenderEngine.OpenGL.Assembly;

public interface IGlShaderVariableDefinition
{
    string Type { get; }
    string Name { get; }
}

public readonly struct GlShaderVariableDefinition : IGlShaderVariableDefinition
{
    public string Name { get; }
    public string Type { get; }

    public GlShaderVariableDefinition(string name, string type)
    {
        Name = name;
        Type = type;
    }
}