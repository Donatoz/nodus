using Nodus.Common;
using Nodus.RenderEngine.Vulkan.Convention;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Extensions;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Computing;

public interface IVkComputePipeline : IVkPipeline
{
}

public class VkComputePipeline : VkObject, IVkComputePipeline
{
    public Pipeline WrappedPipeline { get; }
    public PipelineLayout Layout { get; }
    public DescriptorSetLayout DescriptorSetLayout { get; private set; }

    private readonly IVkLogicalDevice device;
    private readonly IVkDescriptorSetFactory descriptorFactory;

    public unsafe VkComputePipeline(IVkContext vkContext, IVkLogicalDevice device, IVkShader computeShader, IVkDescriptorSetFactory? descriptorFactory = null) : base(vkContext)
    {
        this.device = device;
        this.descriptorFactory = descriptorFactory ?? new VkDescriptorSetFactory();
        
        var shaderStageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.ComputeBit,
            PName = (byte*)SilkMarshal.StringToPtr(ShaderConvention.EntryMethodName),
            Module = computeShader.WrappedModule
        };
        
        Layout = CreateLayout();

        var createInfo = new ComputePipelineCreateInfo
        {
            SType = StructureType.ComputePipelineCreateInfo,
            Layout = Layout,
            Stage = shaderStageInfo
        };

        Pipeline pipeline;
        Context.Api.CreateComputePipelines(device.WrappedDevice, default, 1, &createInfo, null, &pipeline)
            .TryThrow("Failed to create compute pipeline.");

        WrappedPipeline = pipeline;
        
        SilkMarshal.Free((nint)shaderStageInfo.PName);
    }

    private unsafe PipelineLayout CreateLayout()
    {
        using var bindings = descriptorFactory.CreateLayoutBindings().ToFixedArray();

        var setCreateInfo = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = bindings.Length,
            PBindings = bindings.Data
        };

        Context.Api.CreateDescriptorSetLayout(device.WrappedDevice, &setCreateInfo, null, out var setLayout)
            .TryThrow("Failed to create descriptor set layout.");
        
        DescriptorSetLayout = setLayout;
        
        var layoutInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PSetLayouts = &setLayout
        };

        Context.Api.CreatePipelineLayout(device.WrappedDevice, &layoutInfo, null, out var layout);
        
        return layout;
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            Context.Api.DestroyPipeline(device.WrappedDevice, WrappedPipeline, null);
            Context.Api.DestroyPipelineLayout(device.WrappedDevice, Layout, null);
            Context.Api.DestroyDescriptorSetLayout(device.WrappedDevice, DescriptorSetLayout, null);
        }
        
        base.Dispose(disposing);
    }
}