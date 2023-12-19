uniform float2 objectPosition;
uniform float2 objectSize;
uniform shader input;

half4 main(float2 fragCoord) {
    float2 localFrag = fragCoord - objectPosition;

    half4 c = sample(input, localFrag);
    float d = length(c);

    return half4(d, d, d, c.w);
}