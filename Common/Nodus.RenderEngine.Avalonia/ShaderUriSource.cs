using System;
using Nodus.Core.Utility;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.OpenGL;

namespace Nodus.RenderEngine.Avalonia;

public readonly struct ShaderUriSource : IShaderSource
{
    private readonly string contents;
    
    public ShaderUriSource(Uri uri)
    {
        contents = AssetUtility.TryReadAsset(uri);
    }

    public string FetchSource() => contents;
}