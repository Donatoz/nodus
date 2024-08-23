#version 460

layout (location = 0) in vec3 vertexColor;
layout (location = 2) in vec2 texCoord;

layout (location = 0) out vec4 outColor;

layout(binding = 1) uniform sampler2D texSampler;

void main() {
    outColor = texture(texSampler, texCoord);
}
