#version 460

layout (location = 0) in vec3 vertexColor;
layout (location = 2) in vec2 texCoord;

layout (location = 0) out vec4 outColor;

layout(binding = 1) uniform sampler2DArray texSampler;

layout (binding = 2) uniform FrameDataObject {
    float uvSize;
    float distortionAmount;
} frameData;

layout (push_constant) uniform PushConstants {
    float time;
} pc;

#define PI 3.14159

void main() {
    float time = pc.time;
    vec2 uv = texCoord * frameData.uvSize;
    
    vec2 speed = vec2(time, time) * .1;
    vec4 distortionColor = texture(texSampler, vec3(uv + speed, 0.0));

    float factor = sin(mod(time * 2., 2. * PI)) * .1;

    vec4 color = texture(texSampler, vec3(mix(uv, distortionColor.xy, factor), 1.0));
    
    outColor = color;
}
