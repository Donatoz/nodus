using Avalonia.Controls;

namespace Nodus.Core.Factories;

public interface IControlFactory<out TControl, in TCtx> where TControl : Control
{
    TControl Create(TCtx context);
}

public class ControlFactory<TControl, TCtx> : IControlFactory<TControl, TCtx> where TControl : Control, new()
{
    public TControl Create(TCtx context)
    {
        return new TControl { DataContext = context };
    }
}