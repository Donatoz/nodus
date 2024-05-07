using System.Numerics;
using System.Runtime.InteropServices;

namespace Nodus.RenderEngine.Common;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public Vector3 Position { get; set; }
    public Vector2 TexCoord { get; set; }

    public Vertex(Vector3 position, Vector2 texCoord)
    {
        Position = position;
        TexCoord = texCoord;
    }
}