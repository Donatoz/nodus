using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

/// <summary>
/// Represents an OpenGL object.
/// </summary>
public class GlObject : RenderContextObject<uint, GL>
{
    public GlObject(GL context) : base(context)
    {
    }
}