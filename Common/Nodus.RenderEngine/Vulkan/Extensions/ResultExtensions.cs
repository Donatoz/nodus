using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Extensions;


public static class ResultExtensions
{
    public static void TryThrow(this Result result, string onErrorMessage)
    {
        if (result != Result.Success)
        {
            throw new VulkanException($"{result}: {onErrorMessage}");
        }
    }
}