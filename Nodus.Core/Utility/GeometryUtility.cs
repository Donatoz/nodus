using System.Collections.Generic;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace Nodus.Core.Utility;

public static class GeometryUtility
{
    public static (Path path, LineSegment line) CreateSingleLinePath()
    {
        var (path, segments) = CreateEmptyPath();
        var line = new LineSegment();
        segments.Add(line);
        
        return (path, line);
    }

    public static (Path path, IList<PathSegment> segments) CreatePolyLinePath(int segmentsCount)
    {
        var (path, segments) = CreateEmptyPath();

        for (var i = 0; i < segmentsCount; i++)
        {
            segments.Add(new LineSegment());
        }
        
        return (path, segments);
    }

    public static (PathGeometry geometry, IList<PathSegment> segments) CreatePolyLineGeometry(int segmentsCount)
    {
        var (geo, segments) = CreatePathGeometry();

        for (var i = 0; i < segmentsCount; i++)
        {
            segments.Add(new LineSegment());
        }

        return (geo, segments);
    }

    public static (Path, PathSegments) CreateEmptyPath()
    {
        var path = new Path();

        var (geometry, segments) = CreatePathGeometry();

        path.Data = geometry;
        
        return (path, segments);
    }

    public static (PathGeometry geometry, PathSegments segments) CreatePathGeometry()
    {
        var segments = new PathSegments();
        var figure = new PathFigure
        {
            Segments = segments
        };
        figure.IsClosed = false;
        var figures = new PathFigures { figure };

        return (new PathGeometry { Figures = figures }, segments);
    }
}