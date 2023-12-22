using System;

namespace Nodus.Core.Extensions;

public static class ServiceExtensions
{
    public static T GetRequiredService<T>(this IServiceProvider provider) where T : class
    {
        return (provider.GetService(typeof(T)).NotNull($"Failed to retrieve service of type: {typeof(T)}") as T)!;
    }
}