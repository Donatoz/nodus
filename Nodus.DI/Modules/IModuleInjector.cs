using Ninject;

namespace Nodus.DI.Modules;

public interface IModuleInjector
{
    void InjectModules(IKernel kernel, object context);
    void Repopulate();
}