namespace Nodus.RenderEngine.Assembly;

public interface IShaderVariableDefinition
{
    string Type { get; }
    string Name { get; }
}

public readonly struct ShaderVariableDefinition : IShaderVariableDefinition
{
    public string Name { get; }
    public string Type { get; }

    public ShaderVariableDefinition(string name, string type)
    {
        Name = name;
        Type = type;
    }
}