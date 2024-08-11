using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.RenderEditor.Assembly;

namespace Nodus.RenderEditor.Models.Contexts;

public interface IRenderMasterContext : INodeContext
{
    IRenderDescriptor GetDescriptor(GraphContext context);
}