using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Nodus.Core;

public class InitializedBitmap : Bitmap
{
    public InitializedBitmap(Uri uri) : base(AssetLoader.Open(uri))
    {
    }
}