using System;
using Nodus.Core.Utility;
using Nodus.RenderEngine.Common;

namespace Nodus.RenderEngine.Avalonia;

public readonly struct TextureUriSource : ITextureSource
{
    private readonly byte[] bytes;
    
    public TextureUriSource(Uri uri)
    {
        bytes = AssetUtility.ReadAssetBytes(uri);
    }

    public byte[] FetchBytes() => bytes;
}