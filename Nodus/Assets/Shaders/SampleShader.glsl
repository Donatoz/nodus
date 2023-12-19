// Simple UV shader

uniform float2 objectPosition;
uniform float2 objectSize;
uniform float time;
uniform shader input;

half4 main(float2 fragCoord) {
    float2 localFrag = fragCoord - objectPosition;
    float2 uv = localFrag / objectSize;
    uv.y = 1. - uv.y;
    
    uv += sin(time) / 5.;

    half4 c = sample(input, localFrag);

    return half4(c.xyz * uv.xyy, c.w);
}