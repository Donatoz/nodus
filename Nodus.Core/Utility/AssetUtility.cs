using System;
using System.IO;
using Avalonia.Platform;

namespace Nodus.Core.Utility;

public static class AssetUtility
{
    public static string TryReadAsset(Uri uri)
    {
        if (!AssetLoader.Exists(uri))
        {
            throw new ArgumentException($"Asset ({uri.PathAndQuery}) was not found.");
        }

        using var stream = AssetLoader.Open(uri);
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}