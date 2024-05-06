using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Rendering.Composition;
using Avalonia.Styling;
using Avalonia.VisualTree;
using DynamicData;
using Nodus.Core.Common;
using Nodus.Core.Entities;
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

    public static void SwitchClass(this StyledElement element, string className, bool isPresent)
    {
        if (isPresent)
        {
            element.Classes.Add(className);
        }
        else
        {
            element.Classes.Remove(className);
        }
    }

    public static void SwitchBetweenClasses(this StyledElement element, string activeClass, string inactiveClass,
        bool isActive)
    {
        element.Classes.Remove(isActive ? inactiveClass : activeClass);
        element.Classes.Add(isActive ? activeClass : inactiveClass);
    }

    public static void SwitchStyleVisibility(this StyledElement element, bool isVisible)
    {
        element.SwitchBetweenClasses("visible", "invisible", isVisible);
    }
    
    public static TControl CreateExtensionControl<TControl, TCtx>(this StyledElement container)
        where TCtx : class
        where TControl : StyledElement, new()
    {
        var control = new TControl();

        if (container.DataContext is IEntity e)
        {
            control.DataContext = e.TryGetGeneric<IContainer<TCtx>>()?.Value;
        }

        return control;
    }

    public static T? TryGetContext<T>(this StyledElement element) where T : class
    {
        return element.DataContext as T;
    }
}