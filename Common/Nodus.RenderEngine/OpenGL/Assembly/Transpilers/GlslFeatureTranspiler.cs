using Nodus.Core.Extensions;
using Nodus.RenderEngine.Assembly;

namespace Nodus.RenderEngine.OpenGL.Assembly;

public class GlslFeatureTranspiler : IShaderFeatureTranspiler
{
    public IShaderAssemblyToken CreateToken(IShaderAssemblyFeature feature, IShaderAssemblyContext context)
    {
        return feature switch
        {
            GlVersionFeature v => TranspileVersion(v),
            DeclarationFeature d => TranspileDeclaration(d, context),
            OperationFeature o => TranspileOperation(o),
            BodyFeature b => TranspileBody(b, context),
            _ => throw new ArgumentException($"Failed to transpile feature ({feature}): feature is not supported by the transpiler.")
        };
    }

    private IShaderAssemblyToken TranspileVersion(GlVersionFeature version)
    {
        return new GenericShaderAssemblyToken(d =>
            d.SourceBuilder.AppendLine($"#version {version.Version} {version.VersionTypeToString()}"));
    }

    public IShaderAssemblyToken TranspileDeclaration(DeclarationFeature declaration, IShaderAssemblyContext context)
    {
        return new GenericShaderAssemblyToken(d =>
        {
            d.SourceBuilder.Append($"{declaration.VariableDefinition.Type} {declaration.VariableDefinition.Name}");
            
            if (declaration.DeclarativeValue != null)
            {
                var def = GlslObjectTranspiler.ObjectToSource(declaration.DeclarativeValue);
                var val = def.IsPrimitive ? def.ObjectValue : $"{def.ObjectType}({def.ObjectValue})";
                d.SourceBuilder.Append($" = {val}");
            }

            d.SourceBuilder.Append(';').Append(Environment.NewLine);
        });
    }

    public IShaderAssemblyToken TranspileOperation(OperationFeature operation)
    {
        return new GenericShaderAssemblyToken(d =>
        {
            d.SourceBuilder.AppendLine($"{operation.LeftOperand} {operation.Operation} {GlslObjectTranspiler.ObjectToSource(operation.RightOperand).ObjectValue};");
        });
    }

    public IShaderAssemblyToken TranspileBody(BodyFeature body, IShaderAssemblyContext context)
    {
        return new GenericShaderAssemblyToken(d =>
        {
            d.SourceBuilder.AppendLine(
                $"{body.BodyDefinition.ObjectType} {body.BodyName}({string.Join(", ", body.Arguments.Select(x => $"{x.Type} {x.Name}"))}) {{");

            body.Children.Select(x => CreateToken(x, context)).ForEach(x => x.AlterDraft(d));

            d.SourceBuilder.AppendLine("}");
        });
    }
}