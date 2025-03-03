using Silk.NET.SPIRV;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.SPIRV;

public readonly record struct ShaderInputVariable(string Name, Op OpType, Format Format, uint Location);
public readonly record struct ShaderDescriptorBinding(string Name, DescriptorType Type, uint Binding, uint Count);
public readonly record struct ShaderPushConstantBlock(string Name, Op OpType, uint Offset, uint Size);