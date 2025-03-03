using Nodus.DI.Factories;
using Nodus.RenderEngine.Vulkan.Convention;
using Serilog;

namespace Nodus.RenderEngine.Vulkan.DI;

public class VkLoggerFactory : IFactory<string, ILogger>
{
    private readonly Func<LoggerConfiguration> configurationProvider;

    public VkLoggerFactory(Func<LoggerConfiguration> configurationProvider)
    {
        this.configurationProvider = configurationProvider;
    }
    
    public ILogger Create(string name)
    {
        return configurationProvider.Invoke().CreateLogger().ForContext(LogProperties.CallerNameProperty, name);
    }
}