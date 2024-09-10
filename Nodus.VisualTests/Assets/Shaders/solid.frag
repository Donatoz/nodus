#version 460

layout (location = 0) in vec3 vertexColor;
layout (location = 2) in vec2 texCoord;

layout (location = 0) out vec4 outColor;

layout(binding = 1) uniform sampler2DArray texSampler;

void main() {
    outColor = texture(texSampler, vec3(texCoord, 1.0));
}
