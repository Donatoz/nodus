using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using DynamicData;
using Nodus.Core.Common;
using Nodus.Core.ViewModels;

namespace Nodus.Core.Extensions;

public static class ControlExtensions
{
    public static void TryBindToEvent<TEvt>(this Control control, Action<TEvt> callback, ICollection<IDisposable> lifetimeController) where TEvt : IEvent
    {
        if (control.DataContext is IReactiveViewModel vm)
        {
            lifetimeController.Add(vm.EventStream.Subscribe(Observer.Create<IEvent>(evt =>
            {
                if (evt is TEvt e)
                {
                    callback.Invoke(e);
                }
            })));
        }
    }

    public static bool HasVisualAncestorOrSelf(this Control c, Visual ancestor)
    {
        return c == ancestor || c.GetVisualAncestors().Any(x => x == ancestor);
    }

    public static void SwitchClass(this StyledElement control, string className, bool isPresent)
    {
        if (isPresent)
        {
            control.Classes.Add(className);
        }
        else
        {
            control.Classes.Remove(className);
        }
    }
}