namespace Nodus.Core.Extensions;

public static class NumericExtensions
{
    public static float Remap(this float value, float from1, float to1, float from2, float to2) {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    public static byte Lerp(this byte a, byte b, float t)
    {
        return (byte) (a + (b - a) * t);
    }
}