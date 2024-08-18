#version 310 es

precision highp float;

in vec2 texCoord;

uniform float time;

uniform sampler2D mainTexture;
uniform sampler2D distortion;

out vec4 FragColor;

#define PI 3.14159

void main()
{
    vec2 uv = texCoord;
    
    vec2 speed = vec2(time, time) * .1;
    vec4 distortionColor = texture(distortion, uv + speed);
    
    float factor = sin(mod(time * 2., 2. * PI)) * .1;
    
    vec4 color = texture(mainTexture, mix(uv, distortionColor.xy, factor));
    
    FragColor = color;
}