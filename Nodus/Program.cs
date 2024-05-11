using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Collections.Generic;
using Avalonia.Logging;
using Avalonia.OpenGL;
using Avalonia.Win32;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;

namespace Nodus;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current.Register<FontAwesomeIconProvider>();
        
        return AppBuilder.Configure<App.App>()
            .UsePlatformDetect()
            .WithInterFont()
            .With(new AngleOptions {
                GlProfiles = new List<GlVersion>
                {
                    new(GlProfileType.OpenGLES, 3, 1)
                }
            })
            .LogToTrace()
            .UseReactiveUI();
    }
}