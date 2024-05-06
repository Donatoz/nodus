using System.Numerics;
using System.Runtime.InteropServices;

namespace Nodus.RenderEngine.Common;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public Vector3 Position { get; set; }
    public Vector4 Color { get; set; }

    public Vertex(Vector3 position, Vector4 color)
    {
        Position = position;
        Color = color;
    }
}