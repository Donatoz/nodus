using Nodus.Core.Extensions;
using Nodus.DI.Factories;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.Vulkan.Convention;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Memory;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Sync;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Computing;

public interface IVkComputeDispatcherContext
{
    IShaderDefinition ComputeShader { get; }
    IVkPhysicalDevice PhysicalDevice { get; }
    VkQueueInfo QueueInfo { get; }
}

public readonly struct VkComputeDispatcherContext(
    IShaderDefinition computeShader, 
    IVkPhysicalDevice physicalDevice, 
    VkQueueInfo queueInfo) : IVkComputeDispatcherContext
{
    public IShaderDefinition ComputeShader { get; } = computeShader;
    public IVkPhysicalDevice PhysicalDevice { get; } = physicalDevice;
    public VkQueueInfo QueueInfo { get; } = queueInfo;
}

public class VkComputeDispatcher : IDisposable
{
    private readonly IVkContext context;
    private readonly IVkLogicalDevice device;
    private readonly IVkComputePipeline computePipeline;
    private readonly IVkMemoryLease storageBufferMemory;
    private readonly IVkBoundBuffer storageBuffer;
    private readonly IVkAllocatedBuffer<float> outputBuffer;
    private readonly IVkShader shader;
    private readonly IVkDescriptorPool descriptorPool;
    private readonly IVkCommandPool commandPool;
    private readonly Queue queue;
    private readonly IVkFence inFlightFence;
    private readonly IVkPhysicalDevice physicalDevice;
    
    private readonly float[] inputData;
    private readonly ulong storageBufferSize;
    
    public VkComputeDispatcher(IVkContext context, IVkLogicalDevice device, IVkComputeDispatcherContext dispatcherContext)
    {
        dispatcherContext.QueueInfo.ThrowIfIncomplete();

        this.context = context;
        this.device = device;
        physicalDevice = dispatcherContext.PhysicalDevice;
        shader = new VkShader(context, device, dispatcherContext.ComputeShader);
        commandPool = new VkCommandPool(context, device, dispatcherContext.QueueInfo, 1,
            CommandPoolCreateFlags.ResetCommandBufferBit);
        queue = device.Queues[dispatcherContext.QueueInfo.ComputeFamily!.Value];
        inFlightFence = new VkFence(context, device, true);
        
        inputData = new float[1024];
        for (var i = 0; i < inputData.Length; i++)
        {
            inputData[i] = i;
        }
        
        storageBufferSize = (ulong)(sizeof(float) * inputData.Length);
        storageBufferMemory =
            context.RenderServices.MemoryLessor.LeaseMemory(MemoryGroups.ComputeStorageMemory, storageBufferSize);
        
        storageBuffer = new VkBoundBuffer(context, new VkBufferContext(
            storageBufferSize,
            BufferUsageFlags.VertexBufferBit | BufferUsageFlags.StorageBufferBit | BufferUsageFlags.TransferDstBit,
            SharingMode.Exclusive));
        outputBuffer = new VkAllocatedBuffer<float>(context,
            new VkAllocatedBufferContext((uint)storageBufferSize, BufferUsageFlags.StorageBufferBit | BufferUsageFlags.TransferSrcBit, SharingMode.Exclusive,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit));
        outputBuffer.Allocate();
        
        storageBuffer.BindToMemory(storageBufferMemory);

        using var stagingBuffer = new VkAllocatedBuffer<float>(context,
            new VkAllocatedBufferContext((uint)storageBufferSize, BufferUsageFlags.TransferSrcBit, SharingMode.Exclusive,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit));
        stagingBuffer.Allocate();
        stagingBuffer.UpdateData(inputData);
        
        stagingBuffer.CmdCopyTo(storageBuffer, this.context, commandPool.GetBuffer(0), queue);

        computePipeline = new VkComputePipeline(context, device, shader, CreateDescriptorsFactory());

        var writeTokens = new IVkDescriptorWriteToken[]
        {
            new VkStorageBufferWriteToken(0, 0, storageBuffer.WrappedBuffer, 0, storageBufferSize),
            new VkStorageBufferWriteToken(1, 0, outputBuffer.WrappedBuffer, 0, storageBufferSize)
        };

        descriptorPool = new VkDescriptorPool(context, device, new VkDescriptorPoolContext(
            [new VkDescriptorInfo { Type = DescriptorType.StorageBuffer, Count = 1 }],
            new VkDescriptorWriter(writeTokens)), 1, computePipeline.DescriptorSetLayouts[0]);
        
        descriptorPool.UpdateSets();
        writeTokens.OfType<IDisposable>().DisposeAll();
    }

    private IVkDescriptorSetFactory CreateDescriptorsFactory()
    {
        return new VkDescriptorSetFactory
        {
            BindingsFactory = () =>
            [
                new DescriptorSetLayoutBinding
                {
                    Binding = 0,
                    DescriptorCount = 1,
                    DescriptorType = DescriptorType.StorageBuffer,
                    StageFlags = ShaderStageFlags.ComputeBit
                },
                new DescriptorSetLayoutBinding
                {
                    Binding = 1,
                    DescriptorCount = 1,
                    DescriptorType = DescriptorType.StorageBuffer,
                    StageFlags = ShaderStageFlags.ComputeBit
                }
            ]
        };
    }

    public unsafe void Dispatch()
    {
        inFlightFence.Await();
        inFlightFence.Reset();

        var cmdBuffer = commandPool.GetBuffer(0);
        
        RecordCommands(cmdBuffer);

        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdBuffer
        };

        context.Api.QueueSubmit(queue, 1, &submitInfo, inFlightFence.WrappedFence);
        
        inFlightFence.Await();

        using var stgBuffer = new VkAllocatedBuffer<float>(context,
            new VkAllocatedBufferContext((uint)storageBufferSize, BufferUsageFlags.TransferDstBit, SharingMode.Exclusive,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit));
        
        stgBuffer.Allocate();
        outputBuffer.CmdCopyTo(stgBuffer, context, cmdBuffer, queue);
        
        stgBuffer.MapToHost();
        var data = stgBuffer.GetMappedData((uint)storageBufferSize / sizeof(float)).ToArray();
        
        Console.WriteLine($"Length: {data.Length}");
        Console.WriteLine(string.Join(",", data));
    }

    public void RecordCommands(CommandBuffer cmdBuffer)
    {
        var descriptorSet = descriptorPool.GetSet(0);
        
        context.Api.BeginCommandBuffer(cmdBuffer, new CommandBufferBeginInfo{SType = StructureType.CommandBufferBeginInfo})
            .TryThrow("Failed to begin cmd buffer.");
        
        context.Api.CmdBindPipeline(cmdBuffer, PipelineBindPoint.Compute, computePipeline.WrappedPipeline);
        context.Api.CmdBindDescriptorSets(cmdBuffer, PipelineBindPoint.Compute, computePipeline.Layout, 0, 1, in descriptorSet, 0, 0);
        
        context.Api.CmdDispatch(cmdBuffer, (uint)inputData.Length / 256, 1, 1);

        context.Api.EndCommandBuffer(cmdBuffer)
            .TryThrow("Failed to end cmd buffer.");
    }
    
    public void Dispose()
    {
        computePipeline.Dispose();
        descriptorPool.Dispose();
        
        storageBuffer.Dispose();
        storageBufferMemory.Dispose();
        outputBuffer.Dispose();
        
        shader.Dispose();
        commandPool.Dispose();
        inFlightFence.Dispose();
    }
}