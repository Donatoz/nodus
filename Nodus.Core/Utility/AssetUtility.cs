using System;
using System.IO;
using Avalonia.Platform;

namespace Nodus.Core.Utility;

public static class AssetUtility
{
    private static void ValidateUri(Uri uri)
    {
        if (!AssetLoader.Exists(uri))
        {
            throw new ArgumentException($"Asset ({uri.PathAndQuery}) was not found.");
        }
    }
    
    public static string ReadAsset(Uri uri)
    {
        ValidateUri(uri);

        using var stream = AssetLoader.Open(uri);
        using var reader = new StreamReader(stream);
        
        return reader.ReadToEnd();
    }

    public static byte[] ReadAssetBytes(Uri uri)
    {
        ValidateUri(uri);

        using var stream = AssetLoader.Open(uri);
        using var memStream = new MemoryStream();
        
        stream.CopyTo(memStream);

        return memStream.ToArray();
    }
}