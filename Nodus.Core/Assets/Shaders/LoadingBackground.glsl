// Base shader taken from: https://www.shadertoy.com/view/XdVfzd
// Adopted for SKSL and Nodus framework

uniform float2 objectPosition;
uniform float2 objectSize;
uniform float time;

float remap(float value, float min1, float max1, float min2, float max2) {
  return min2 + (value - min1) * (max2 - min2) / (max1 - min1);
}

float smoothstep(float edge0, float edge1, float x) {
    float t = clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
    return t * t * (3.0 - 2.0 * t);
}

float mod(float x, float y) {
    return x - y * floor(x/y);
}

float rand(vec2 co) {
    return fract(cos(mod(dot(co.xy+0.12,vec2(12.9898,78.233)),3.14))*43758.5453);
}

vec3 rand3(vec2 co){
	float t=rand(co);
	float z=rand(co+t);
	return vec3(t,z,rand(co+z));
}

vec3 getpoint(vec2 id){
	vec3 r=rand3(id);
	vec3 ou=vec3(
		sin(time*r.x),
		cos(time*r.y),
		sin(time*r.z*10.)
	);
	return ou*0.25+0.5;
}

float line(vec2 uv, vec2 p, vec2 p2){
	vec2 n=uv-p;
	vec2 w=p2-p;
	float r=clamp(dot(n,w)/dot(w,w),0.,1.);
	r=length(n-w*r);
    return clamp(smoothstep(0.02,0.01,r)*(smoothstep(0.6,1.,1./distance(p,p2))),0.,.8);
}

half4 main(float2 fragCoord) {
    float2 localFrag = fragCoord - objectPosition;
    float2 uv = localFrag / objectSize;
    uv.y = 1. - uv.y;
    uv -= 0.5;
    uv.x *= (objectSize.x / objectSize.y);    

    float radialMask = clamp(smoothstep(0.3, 1, pow(length(uv * 1.5), 2) * 2), 0, 1);
    float ripple = pow(1 - clamp(abs((length(uv) - remap(fract(time / 6), 0, 1, -1, 1.5)) * 4), 0, 1), 1);
    
    uv *= 15;
    
    vec2 id=floor(uv);
    vec2 c=fract(uv);

    vec2 p[9];
	float col=0.;
    int i=0;
    for(float n=-1.;n<=1.;n++){
        for(float w=-1.;w<=1.;w++){
			vec2 nc=id+vec2(w,n);
			vec3 point=getpoint(nc);
			p[i]=point.xy+vec2(w,n);
			vec2 a=(p[i].xy-c)*23.;
			col+=pow(point.z/dot(a,a),2.2);
            i++;
		}
	}
	
	for(int i=0;i<9;i++){
		col+=pow(line(c,p[i].xy,p[4].xy),2.2);
	}
	
	col+=pow(line(c,p[1].xy,p[3].xy),2.2);
	col+=pow(line(c,p[1].xy,p[5].xy),2.2);
	col+=pow(line(c,p[7].xy,p[3].xy),2.2);
    col+=pow(line(c,p[7].xy,p[5].xy),2.2);
	
    col=pow(col,1./2.2);
    col*=col;
    col*=0.5;

    return half4(float3(col) * float3(0.05, 0.45, 0.2), length(col) * (radialMask + ripple * radialMask) * 0.25);
}