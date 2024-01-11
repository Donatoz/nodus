using Ninject.Parameters;

namespace Nodus.DI.Runtime;

public interface IRuntimeInjector
{
    void Inject(object target, params IParameter[] parameters);
}