using Ninject.Parameters;

namespace Nodus.DI.Runtime;

public interface IRuntimeElementProvider
{
    T GetRuntimeElement<T>();
    T GetRuntimeElement<T>(params IParameter[] parameters);
}