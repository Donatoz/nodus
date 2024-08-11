using System;
using System.Text;
using Avalonia.Media;

namespace Nodus.Core.Extensions;

public static class PrimitiveExtensions
{
    public static bool IsNullOrEmpty(this string? s)
    {
        return s?.Trim().Length == 0;
    }

    public static Color Lerp(this Color from, Color to, float t)
    {
        var r = (byte)(from.R + (to.R - from.R) * t);
        var g = (byte)(from.G + (to.G - from.G) * t);
        var b = (byte)(from.B + (to.B - from.B) * t);
        var a = (byte)(from.A + (to.A - from.A) * t);

        return Color.FromArgb(a, r, g, b);
    }
    
    public static string AddIndents(this string input, int indentStride = 3)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var indentedString = new StringBuilder();
        var level = 0;

        foreach (var line in input.Split(Environment.NewLine))
        {

            if (line.EndsWith('}'))
            {
                level--;
            }
            
            if (level > 0)
            {
                indentedString.Append(new string(' ', indentStride * level));
            }
            
            if (line.EndsWith('{'))
            {
                level++;
            }
            
            indentedString.Append(line).Append(Environment.NewLine);
        }

        return indentedString.ToString();
    }
}