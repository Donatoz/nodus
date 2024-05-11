#version 310 es

precision mediump float;

in vec2 texCoord;
in vec4 vertexPosition;

uniform float time;

uniform sampler2D mainTexture;
uniform sampler2D distortion;

out vec4 FragColor;

void main()
{
    vec2 uv = texCoord;
    
    vec2 speed = vec2(time, time) * .005;
    vec4 distortionColor = texture(distortion, uv + speed);
    
    float factor = sin(time / 6.) * .1;
    
    FragColor = texture(mainTexture, mix(uv, distortionColor.xy, factor));
}