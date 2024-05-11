using DynamicData;
using Nodus.RenderEngine.Assembly;

namespace Nodus.RenderEngine.OpenGL.Assembly;

public static class GlShaderAssemblyExtensions
{
    public static GlVersionFeature? GetVersion(this IShaderAssemblyContext ctx)
    {
        return ctx.EffectiveFeatures.OfType<GlVersionFeature>().FirstOrDefault();
    }

    public static IEnumerable<IShaderAssemblyFeature> FeaturesBefore(this IShaderAssemblyContext ctx, IShaderAssemblyFeature feature)
    {
        return ctx.EffectiveFeatures.Take(ctx.EffectiveFeatures.IndexOf(feature));
    }
}