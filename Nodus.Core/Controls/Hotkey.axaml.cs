using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;

namespace Nodus.Core.Controls;

public partial class Hotkey : UserControl
{
    public static readonly StyledProperty<string?> GestureProperty;

    public static readonly AttachedProperty<string?> MenuItemHotkeyProperty;

    public string? Gesture
    {
        get => GetValue(GestureProperty);
        set => SetValue(GestureProperty, value);
    }

    public string HotkeyLabel => Gesture?.ToString().ToUpper() ?? string.Empty;

    static Hotkey()
    {
        GestureProperty = AvaloniaProperty.Register<Hotkey, string?>(nameof(Gesture));
        MenuItemHotkeyProperty = AvaloniaProperty.RegisterAttached<Hotkey, MenuItem, string?>("MenuItemHotkey");
    }
    
    public static void SetMenuItemHotkey(AvaloniaObject element, string? gesture)
    {
        element.SetValue(MenuItemHotkeyProperty, gesture);

        if (element is MenuItem item && gesture != null)
        {
            if (item.Header is string s)
            {
                var h = new Hotkey { Gesture = gesture };
                var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center};
                panel.Children.Add(new TextBlock { Text = s, Margin = new Thickness(0, 0, 10 ,0), Classes = { "h5" }});
                panel.Children.Add(h);
                item.Header = panel;
            }

            try
            {
                item.HotKey = KeyGesture.Parse(gesture);
            }
            catch (ArgumentException e)
            {
                Trace.WriteLine(e.Message);
            }
        }
    }

    public static string? GetMenuItemHotkey(AvaloniaObject element)
    {
        return element.GetValue(MenuItemHotkeyProperty);
    }
    
    public Hotkey()
    {
        InitializeComponent();
    }
}