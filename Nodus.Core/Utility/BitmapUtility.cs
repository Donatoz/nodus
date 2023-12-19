namespace Nodus.Core.Utility;

public static class BitmapUtility
{
    public static int GetStride(int width, int bitsPerPixel)
    {
        return (width * bitsPerPixel + 7) / 8;
    }
}