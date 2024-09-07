#version 460

layout (location = 0) in vec2 inPos;
layout (location = 1) in vec2 inUv;
layout (location = 2) in vec4 inColor;

layout (location = 0) out vec2 fragUv;
layout (location = 1) out vec4 fragColor;

layout (push_constant) uniform PushConstants {
    vec2 scale;
    vec2 translate;
} pushConstants;

void main() {
    gl_Position = vec4(inPos * pushConstants.scale + pushConstants.translate, 0.0, 1.0);

    fragUv = inUv;
    fragColor = inColor;
}