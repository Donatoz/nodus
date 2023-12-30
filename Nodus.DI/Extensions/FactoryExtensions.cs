using Avalonia.Controls;
using Nodus.Core.Factories;
using Nodus.DI.Factories;

namespace Nodus.DI;

public static class FactoryExtensions
{
    public static void RegisterControlFactory<TControl, TCtx>(this ITypeCachedComponentFactoryProvider provider) where TControl : Control, new()
    {
        provider.RegisterFactory(typeof(IControlFactory<TControl, TCtx>), new ControlFactory<TControl, TCtx>());
    }
}