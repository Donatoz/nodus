#version 330 core

in vec4 vertexColor;
in vec4 vertexPosition;

out vec4 FragColor;

float remap(float val, float from1, float to1, float from2, float to2) {
    return (val - from1) / (to1 - from1) * (to2 - from2) + from2;
}

void main()
{
    vec4 color = vertexPosition;
    
    color.x = remap(color.x, -1., 1., 0., 1.);
    color.y = remap(color.y, -1., 1., 0., 1.);
    
    FragColor = color;
}