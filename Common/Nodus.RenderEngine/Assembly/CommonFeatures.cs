using Nodus.RenderEngine.OpenGL.Assembly;

namespace Nodus.RenderEngine.Assembly;

public record DeclarationFeature(ShaderVariableDefinition VariableDefinition, object? DeclarativeValue) : IShaderAssemblyFeature
{
    public ushort AssemblyPriority { get; init; }
}

public record OperationFeature(string LeftOperand, string Operation, object RightOperand) : IShaderAssemblyFeature
{
    public ushort AssemblyPriority { get; init; }
}

public record BodyFeature(string BodyName, ShaderObjectDefinition BodyDefinition, IShaderVariableDefinition[] Arguments, IList<IShaderAssemblyFeature> Children) : IShaderAssemblyFeature
{
    public ushort AssemblyPriority { get; init; }
}