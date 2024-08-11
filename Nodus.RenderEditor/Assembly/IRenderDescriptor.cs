using System.Collections.Generic;
using Nodus.RenderEngine.Common;

namespace Nodus.RenderEditor.Assembly;

public interface IRenderDescriptor
{
    IEnumerable<IShaderDefinition> CoreShaders { get; }
}

public readonly struct RenderDescriptor : IRenderDescriptor
{
    public IEnumerable<IShaderDefinition> CoreShaders { get; }

    public RenderDescriptor(IEnumerable<IShaderDefinition> coreShaders)
    {
        CoreShaders = coreShaders;
    }
}