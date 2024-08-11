namespace Nodus.RenderEngine.Assembly;

public readonly struct ShaderObjectDefinition
{
    public string ObjectType { get; }
    public string ObjectValue { get; }
    public bool IsPrimitive { get; }

    public ShaderObjectDefinition(string objectType, bool isPrimitive, string? objectValue = null)
    {
        ObjectType = objectType;
        IsPrimitive = isPrimitive;
        ObjectValue = objectValue ?? string.Empty;
    }
}