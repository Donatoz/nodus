using System.Diagnostics;
using System.Reflection;
using Ninject;
using Ninject.Modules;
using Nodus.Core.Extensions;

namespace Nodus.DI.Modules;

public interface IModuleInjector
{
    void InjectModules(IKernel kernel, object context);
    void Repopulate();
}

public class ModuleInjector : IModuleInjector
{
    private readonly IDictionary<object, IEnumerable<NinjectModule>> moduleFactoryBindings;
    
    public ModuleInjector()
    {
        moduleFactoryBindings = new Dictionary<object, IEnumerable<NinjectModule>>();
        
        Repopulate();
    }

    public void Repopulate()
    {
        var boundTypeGroups = AppDomain.CurrentDomain.GetAsmTypesWithAttribute<ModuleInjectionEntryAttribute>()
            .Select(x => new
            {
                Attribute = x.GetCustomAttribute<ModuleInjectionEntryAttribute>(),
                Type = x
            }).GroupBy(x => x.Attribute!.ContextualBinding);

        boundTypeGroups.ForEach(x =>
        {
            moduleFactoryBindings[x.Key] = x.OrderBy(x => x.Attribute.InjectionPriority)
                .Select(y => Activator.CreateInstance(y.Type) as NinjectModule)
                .Where(y => y != null).Cast<NinjectModule>();
        });
    }
    
    public void InjectModules(IKernel kernel, object context)
    {
        if (!moduleFactoryBindings.ContainsKey(context))
        {
            throw new ArgumentException($"Context ({context}) has no DI modules bindings.");
        }
        
        kernel.Load(moduleFactoryBindings[context]);
    }
}