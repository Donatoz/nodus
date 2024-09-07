#version 460

layout (location = 0) in vec2 fragUv;
layout (location = 1) in vec4 fragColor;

layout(binding = 1) uniform sampler2D font;

layout(location = 0) out vec4 outColor;

void main() {
    vec4 fontColor = texture(font, fragUv);
    
    outColor = fontColor * fragColor;
}