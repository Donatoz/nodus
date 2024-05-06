using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public class GlObject : RenderContextObject<uint, GL>
{
    public GlObject(GL context) : base(context)
    {
    }
}