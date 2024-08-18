using System.Numerics;
using System.Runtime.InteropServices;
using Nodus.RenderEngine.Common;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Utility;

public static class VertexUtility
{
    public static unsafe VertexInputBindingDescription GetVertexBindingDescription()
    {
        return new VertexInputBindingDescription
        {
            Binding = 0,
            Stride = (uint)sizeof(Vertex),
            InputRate = VertexInputRate.Vertex
        };
    }

    public static VertexInputAttributeDescription[] GetVertexAttributeDescriptions()
    {
        return
        [
            new VertexInputAttributeDescription
            {
                Binding = 0,
                Location = 0,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.Position))
            },
            new VertexInputAttributeDescription
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.Normal))
            },
            new VertexInputAttributeDescription
            {
                Binding = 0,
                Location = 2,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.TexCoord))
            }
        ];
    }
}