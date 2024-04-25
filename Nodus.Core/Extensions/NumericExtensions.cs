using System;

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
    
    public static bool IsNumber(this object o)
    {
        return Type.GetTypeCode(o.GetType()) switch
        {
            TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 or TypeCode.Int16
                or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Decimal or TypeCode.Double or TypeCode.Single => true,
            _ => false
        };
    }
}