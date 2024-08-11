using Nodus.DI.Runtime;
using Nodus.NodeEditor.Models;
using Nodus.RenderLibraries.Common.Contexts;

namespace Nodus.RenderLibraries.Common;

public static class CommonRenderLibrary
{
    public static void Register(INodeContextProvider provider, IRuntimeElementProvider elementProvider)
    {
        provider.RegisterFactory<FragmentStageContext>(CommonNodes.FragmentAssemblyNodeContextId, elementProvider);
        provider.RegisterFactory<QuadMasterContext>(CommonNodes.QuadMasterNodeContextId, elementProvider);
        provider.RegisterFactory<VectorContext>(CommonNodes.Vector4NodeContextId, elementProvider);
    }
}