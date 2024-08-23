using Nodus.RenderEngine.Common;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan;

public interface IVkShader : IVkUnmanagedHook
{
    ShaderModule WrappedModule { get; }
    ShaderSourceType Type { get; }

    void Update(IShaderDefinition definition);
}

public class VkShader : VkObject, IVkShader
{
    public ShaderModule WrappedModule { get; private set; }
    public ShaderSourceType Type { get; private set; }

    private readonly IVkLogicalDevice device;

    public VkShader(IVkContext vkContext, IVkLogicalDevice device, IShaderDefinition definition) : base(vkContext)
    {
        this.device = device;

        UpdateState(definition);
    }
    
    public unsafe void Update(IShaderDefinition definition)
    {
        Context.Api.DestroyShaderModule(device.WrappedDevice, WrappedModule, null);

        UpdateState(definition);
    }

    private void UpdateState(IShaderDefinition definition)
    {
        Type = definition.Type;
        WrappedModule = CreateModule(definition.Source);
    }
    
    private unsafe ShaderModule CreateModule(IShaderSource source)
    {
        var content = FetchBytesFromSource(source);

        var createInfo = new ShaderModuleCreateInfo
        {
            SType = StructureType.ShaderModuleCreateInfo,
            CodeSize = (nuint)content.Length
        };

        ShaderModule module;

        fixed (byte* p = content)
        {
            createInfo.PCode = (uint*)p;

            if (Context.Api.CreateShaderModule(device.WrappedDevice, in createInfo, null, out module) != Result.Success)
            {
                throw new Exception($"Failed to create shader module from source: {source}");
            }
        }

        return module;
    }

    private byte[] FetchBytesFromSource(IShaderSource source)
    {
        if (source is not IShaderByteSource s)
        {
            throw new Exception(
                $"Failed to create shader module from source: {source}. Only byte-sources are supported.");
        }

        return s.FetchBytes();
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            Context.Api.DestroyShaderModule(device.WrappedDevice, WrappedModule, null);
        }
        
        base.Dispose(disposing);
    }
}