using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using Ninject;
using Ninject.Parameters;
using Nodus.App.Views;
using Nodus.DI.Modules;
using Nodus.DI.Runtime;
using Nodus.NodeEditor.Services;
using Nodus.ViewModels;
using ReactiveUI;

namespace Nodus.App;

internal readonly struct AppElementProvider : IRuntimeElementProvider
{
    private readonly IKernel diKernel;
    
    public AppElementProvider(IKernel diKernel)
    {
        this.diKernel = diKernel;
    }
    
    public T GetRuntimeElement<T>()
    {
        return diKernel.Get<T>();
    }

    public T GetRuntimeElement<T>(params IParameter[] parameters)
    {
        return diKernel.Get<T>(parameters);
    }
}

internal readonly struct AppModuleLoader : IRuntimeModuleLoader
{
    private readonly IKernel diKernel;
    private readonly IModuleInjector injector;

    public AppModuleLoader(IKernel diKernel, IModuleInjector injector)
    {
        this.diKernel = diKernel;
        this.injector = injector;
    }
    
    public void LoadModulesFor(object context)
    {
        injector.InjectModules(diKernel, context);
    }

    public void Repopulate()
    {
        injector.Repopulate();
    }
}

public partial class App : Application
{
    private IKernel diKernel;
    private IServiceProvider services;
    
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
        var kernel = new StandardKernel();
        
        // Create primary service scope
        kernel.Bind<IServiceScope>().ToConstant(services.CreateScope()).InSingletonScope();
        // Expose the service scope as a service provider
        kernel.Bind<IServiceProvider>().ToMethod(x => x.Kernel.Get<IServiceScope>().ServiceProvider);
        // Bind runtime modules resolvers
        kernel.Bind<IRuntimeElementProvider>().ToConstant(new AppElementProvider(kernel)).InSingletonScope();
        kernel.Bind<IRuntimeModuleLoader>().ToConstant(new AppModuleLoader(kernel, new ModuleInjector())).InSingletonScope();

        return kernel;
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
        services = CreateServices();
        diKernel = CreateDIKernel();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = diKernel.Get<MainWindow>();
            desktop.MainWindow.DataContext = diKernel.Get<MainWindowViewModel>();

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