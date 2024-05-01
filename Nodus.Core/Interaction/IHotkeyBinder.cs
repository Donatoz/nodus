using System;
using System.Reactive.Disposables;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI;

namespace Nodus.Core.Interaction;

public interface IHotkeyBinder
{
    IDisposable BindHotkey(KeyGesture gesture, Action context);
}

public class WindowHotkeyBinder : IHotkeyBinder
{
    private readonly Window window;
    
    public WindowHotkeyBinder(Window window)
    {
        this.window = window;
    }
    
    public IDisposable BindHotkey(KeyGesture gesture, Action context)
    {
        var binding = new KeyBinding { Gesture = gesture, Command = ReactiveCommand.Create(context) };
        window.KeyBindings.Add(binding);
        
        return Disposable.Create(() => window.KeyBindings.Remove(binding));
    }
}

public static class HotkeyBinderExtensions
{
    public static IDisposable BindHotkey(this IHotkeyBinder binder, KeyGesture gesture, ICommand command)
    {
        return binder.BindHotkey(gesture, () => command.Execute(null));
    }
}