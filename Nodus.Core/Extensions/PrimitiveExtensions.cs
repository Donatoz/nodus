namespace Nodus.Core.Extensions;

public static class PrimitiveExtensions
{
    public static bool IsNullOrEmpty(this string? s)
    {
        return s?.Trim().Length == 0;
    }
}