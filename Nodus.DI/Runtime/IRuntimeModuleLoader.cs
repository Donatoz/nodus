namespace Nodus.DI.Runtime;

public interface IRuntimeModuleLoader
{
    void LoadModulesFor(object context);
    void Repopulate();
}