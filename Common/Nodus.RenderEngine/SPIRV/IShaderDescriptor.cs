using Nodus.Core.Extensions;
using Nodus.RenderEngine.Common;
using Silk.NET.Core.Native;
using Silk.NET.SPIRV.Reflect;
using DescriptorType = Silk.NET.Vulkan.DescriptorType;
using Format = Silk.NET.Vulkan.Format;

namespace Nodus.RenderEngine.SPIRV;

public interface IShaderDescriptor
{
    IShaderMeta Describe(IShaderDefinition shader);
}

public interface IShaderMeta
{
    IShaderDefinition Definition { get; }
    ShaderInputVariable[] InputVariables { get; }
    ShaderDescriptorBinding[] DescriptorBindings { get; }
    ShaderPushConstantBlock[] PushConstantBlocks { get; }
}

public record ShaderMeta(
    IShaderDefinition Definition,
    ShaderInputVariable[] InputVariables,
    ShaderDescriptorBinding[] DescriptorBindings,
    ShaderPushConstantBlock[] PushConstantBlocks) : IShaderMeta;

public class ShaderDescriptor : IShaderDescriptor
{
    public static ShaderDescriptor Default { get; } = new(new ShaderReflectionProvider(Reflect.GetApi()));
    
    private readonly IShaderReflectionProvider reflectionProvider;

    public ShaderDescriptor(IShaderReflectionProvider reflectionProvider)
    {
        this.reflectionProvider = reflectionProvider;
    }
    
    public unsafe IShaderMeta Describe(IShaderDefinition shader)
    {
        var source = shader.Source.MustBe<IShaderByteSource>();
        var bytes = source.FetchBytes();

        ReflectShaderModule module;

        fixed (byte* pSrc = bytes)
        {
            reflectionProvider.Api.CreateShaderModule((nuint)bytes.Length, pSrc, &module);
        }
        
        var result = DescribeImpl(ref module, shader);
        
        reflectionProvider.Api.DestroyShaderModule(ref module);
        
        return result;
    }

    protected virtual unsafe IShaderMeta DescribeImpl(ref ReflectShaderModule module, IShaderDefinition shader)
    {
        var inVars = new ShaderInputVariable[module.InputVariableCount];
        var descBindings = new ShaderDescriptorBinding[module.DescriptorBindingCount];
        var pushConstBlocks = new ShaderPushConstantBlock[module.PushConstantBlockCount];

        for (var i = 0; i < inVars.Length; i++)
        {
            var v = module.InputVariables[i];
            inVars[i] = new ShaderInputVariable(SilkMarshal.PtrToString((nint)v->Name) ?? string.Empty, 
                v->TypeDescription->Op, (Format)(int)v->Format, v->Location);
        }

        for (var i = 0; i < descBindings.Length; i++)
        {
            var d = module.DescriptorBindings[i];
            descBindings[i] = new ShaderDescriptorBinding(SilkMarshal.PtrToString((nint)d.Name) ?? string.Empty,
                (DescriptorType)(int)d.DescriptorType, d.Binding, d.Count);
        }

        for (var i = 0; i < pushConstBlocks.Length; i++)
        {
            var p = module.PushConstantBlocks[i];
            
            pushConstBlocks[i] = new ShaderPushConstantBlock(SilkMarshal.PtrToString((nint)p.Name) ?? string.Empty, 
                p.TypeDescription->Op, p.Offset, p.Size);
        }
        
        return new ShaderMeta(shader, inVars, descBindings, pushConstBlocks);
    }
}