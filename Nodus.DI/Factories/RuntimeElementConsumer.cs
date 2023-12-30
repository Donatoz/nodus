using Nodus.DI.Runtime;

namespace Nodus.DI.Factories;

public abstract class RuntimeElementConsumer
{
    protected IRuntimeElementProvider ElementProvider { get; }
    
    protected RuntimeElementConsumer(IRuntimeElementProvider elementProvider)
    {
        ElementProvider = elementProvider;
    }
}