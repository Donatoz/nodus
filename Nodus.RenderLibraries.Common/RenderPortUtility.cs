using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.RenderLibraries.Common;

public static class RenderPortUtility
{
    public static TypedPortData Port<T>(string header, PortType type)
    {
        return new TypedPortData(header, type, type == PortType.Input ? PortCapacity.Single : PortCapacity.Multiple, typeof(T));
    }
}