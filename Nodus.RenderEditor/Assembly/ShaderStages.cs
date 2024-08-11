using Nodus.RenderEditor.Meta;
using Nodus.RenderEngine.Common;

namespace Nodus.RenderEditor.Assembly;

public interface IFragmentStageMeta : IRenderMetadata
{
    IShaderDefinition FragmentShader { get; }
}

public readonly struct FragmentStageMeta : IFragmentStageMeta
{
    public IShaderDefinition FragmentShader { get; }

    public FragmentStageMeta(IShaderDefinition fragmentShader)
    {
        FragmentShader = fragmentShader;
    }
}