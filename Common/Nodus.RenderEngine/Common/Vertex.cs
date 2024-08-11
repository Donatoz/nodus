using System.Numerics;
using System.Runtime.InteropServices;

namespace Nodus.RenderEngine.Common;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public Vector3 Position { get; set; }
    public Vector3 Normal { get; set; }
    public Vector2 TexCoord { get; set; }

    public Vertex(Vector3 position, Vector3 normal, Vector2 texCoord)
    {
        Position = position;
        Normal = normal;
        TexCoord = texCoord;
    }
}