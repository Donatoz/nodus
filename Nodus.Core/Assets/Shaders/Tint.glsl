uniform float2 objectPosition;
uniform float2 objectSize;
uniform shader input;

uniform float3 tintColor;

half4 main(float2 fragCoord) {
    float2 localFrag = fragCoord - objectPosition;

    half4 c = sample(input, localFrag);
    float3 finalCol = length(c) * tintColor;

    return half4(finalCol, c.w);
}