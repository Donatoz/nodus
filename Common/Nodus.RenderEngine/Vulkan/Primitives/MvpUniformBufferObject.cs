using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace Nodus.RenderEngine.Vulkan.Primitives;

[StructLayout(LayoutKind.Sequential)]
public struct MvpUniformBufferObject
{
    public Matrix4X4<float> Model;
    public Matrix4X4<float> View;
    public Matrix4X4<float> Projection;
}