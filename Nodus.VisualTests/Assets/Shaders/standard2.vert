#version 460

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec2 vTexCoord;

layout (location = 0) out vec3 vertexColor;
layout (location = 1) out vec3 vertexNormal;
layout (location = 2) out vec2 texCoord;

layout (set = 0, binding = 0) uniform UniformBufferObject {
    mat4 model;
    mat4 view;
    mat4 proj;
} ubo;

layout(set = 0, binding = 1) uniform sampler2DArray texSampler;

layout (set = 0, binding = 2) uniform FrameDataObject {
    float uvSize;
    float distortionAmount;
} frameData;

layout (push_constant) uniform PushConstants {
    float time;
} pc;

void main() {
    vec4 displaced = vec4(vPosition, 1.0);

    displaced += mix(vec4(0), texture(texSampler, vec3(vTexCoord + pc.time * 0.01, 0.0)) * vec4(vNormal, 1.0), frameData.distortionAmount);
    
    gl_Position = ubo.proj * ubo.view * ubo.model * displaced;
    vertexColor = vec3(1.0, 1.0, 1.0);

    vertexNormal = vNormal;
    texCoord = vTexCoord;
}