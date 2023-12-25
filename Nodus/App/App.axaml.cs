using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using Ninject;
using Ninject.Modules;
using Nodus.App.Views;
using Nodus.NodeEditor.DI;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Services;
using Nodus.ViewModels;
using ReactiveUI;

namespace Nodus.App;

public partial class App : Application
{
    private readonly IKernel diKernel;
    private readonly IServiceProvider services;
    
    public App()
    {
        services = CreateServices();
        diKernel = CreateDIKernel();
    }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Create outer-level kernel that handles object dependencies.
    /// </summary>
    /// <returns></returns>
    private IKernel CreateDIKernel()
    {
        var kernel = new StandardKernel(GetKernelModules().ToArray());
        
        // Create primary service scope
        kernel.Bind<IServiceScope>().ToConstant(services.CreateScope()).InSingletonScope();
        // Expose the service scope as a service provider
        kernel.Bind<IServiceProvider>().ToMethod(x => x.Kernel.Get<IServiceScope>().ServiceProvider);

        return kernel;
    }

    private IEnumerable<INinjectModule> GetKernelModules()
    {
        yield return new NodeCanvasDIModule();
    }

    /// <summary>
    /// Create inner-level service provider for standalone service objects.
    /// </summary>
    /// <returns></returns>
    private IServiceProvider CreateServices()
    {
        var container = new ServiceCollection();

        container.AddScoped<IStorageProvider>(_ => (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.StorageProvider);
        container.AddTransient<INodeCanvasSerializationService>(p => new LocalNodeCanvasSerializationService(p.GetRequiredService<IStorageProvider>()));

        return container.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = diKernel.Get<MainWindowViewModel>()
            };

            desktop.WhenAnyValue(x => x.MainWindow)
                .Subscribe(UpdateWindowServicesScope);
            
            UpdateWindowServicesScope(desktop.MainWindow);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void UpdateWindowServicesScope(Window? window)
    {
        diKernel.Rebind<IServiceScope>().ToConstant(services.CreateScope()).InSingletonScope();
    }
}