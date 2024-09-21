using Nodus.RenderEngine.Vulkan.Sync;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Nodus.RenderEngine.Vulkan.Extensions;

public static class SyncExtensions
{
    public static unsafe void AwaitAll(this IEnumerable<Semaphore> semaphores, IVkContext context, ulong timeout = ulong.MaxValue)
    {
        var semaphoresArr = semaphores as Semaphore[] ?? semaphores.ToArray();
        
        fixed (Semaphore* p = semaphoresArr)
        {
            var waitInfo = new SemaphoreWaitInfo
            {
                SType = StructureType.SemaphoreWaitInfo,
                SemaphoreCount = (uint)semaphoresArr.Length,
                PSemaphores = p
            };

            context.Api.WaitSemaphores(context.RenderServices.Devices.LogicalDevice.WrappedDevice, waitInfo, ulong.MaxValue);
        }
    }

    public static IVkTask AsTask(this IVkFence fence, IVkContext context, PipelineStageFlags waitStage = PipelineStageFlags.None)
    {
        return new VkTask(waitStage, (sm, _) =>
        {
            sm?.AwaitAll(context);
            fence.Await();
            return VkTaskResult.Success;
        });
    }
}