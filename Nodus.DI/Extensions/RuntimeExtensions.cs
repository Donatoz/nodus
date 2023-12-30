using Nodus.DI.Runtime;

namespace Nodus.DI;

public static class RuntimeExtensions
{
    public static void LoadModulesForType<T>(this IRuntimeModuleLoader loader)
    {
        loader.LoadModulesFor(typeof(T));
    }
}