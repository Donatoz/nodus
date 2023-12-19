using System;
using Avalonia;

namespace Nodus.Core.Extensions;

public static class GeometryExtensions
{
    public static Point ClampInside(this Rect rect, Point p)
    {
        return rect.Intersect(new Rect(p, rect.Size)).Position;
    }
}