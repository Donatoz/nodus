# OpenGL Shader Assembly

This directory contains the necessary data types and interfaces for building
a GLSL shader. A GL shader assembler takes as input GL shader assembly features,
extracts a token from each feature and composes a shader assembly from the received
tokens.

### Example

The example below shows a process of creating a simple vertex shader.
```csharp
var asm = new GlShaderAssembler(new GlVersionFeature(300, GlVersionFeature.GlShaderVersionType.Core));
        
asm.AddUniform(new GlUniformFeature("myUniform", "vec2"));
asm.AddUniform(new GlUniformFeature("mySecondUniform", "float"));
asm.AddVarying(new GlVaryingFeature(true, "someInput", "float"));
asm.AddVarying(new GlVaryingFeature(false, "someOutput", "float"));
```

The result of such assembly will be the next shader:
```glsl
#version 300 core
layout (location = 0) in float someInput;
out float someOutput;
uniform vec2 myUniform;
uniform float mySecondUniform;

```