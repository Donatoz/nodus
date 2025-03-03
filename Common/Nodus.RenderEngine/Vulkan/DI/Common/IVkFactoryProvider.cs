using Nodus.DI.Factories;
using Nodus.RenderEngine.Vulkan.Convention;
using Serilog;

namespace Nodus.RenderEngine.Vulkan.DI;

public interface IVkFactoryProvider
{
    IFactory<string, ILogger> LoggerFactory { get; }
}

public sealed class VkFactoryProvider : IVkFactoryProvider
{
    public IFactory<string, ILogger> LoggerFactory { get; }

    public VkFactoryProvider()
    {
        LoggerFactory = new VkLoggerFactory(() => new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] " +
                                             $"[{{SourceContext}}{{{LogProperties.CallerNameProperty}}}]" +
                                             " {Message:lj}{NewLine}{Exception}")
        );
    }
}