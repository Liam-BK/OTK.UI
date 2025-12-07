#version 330 core

uniform vec4 bounds;
out vec4 FragColor;

vec3 hsv2rgb(vec3 c)
{
    vec3 rgb = clamp(abs(mod(c.x * 6.0 + vec3(0.0, 4.0, 2.0), 6.0) - 3.0) - 1.0, 0.0, 1.0);
    return c.z * mix(vec3(1.0), rgb, c.y);
}

vec3 colorFromPolar(vec2 polar)
{
    float r = clamp(polar.x, 0.0, 1.0);
    float theta = polar.y;
    float h = theta / (2.0 * 3.14159265);
    float s = r;
    float v = 1.0;
    return hsv2rgb(vec3(h, s, v));
}

vec2 cartesianToPolar(vec2 p)
{
    float r = length(p);         
    float theta = atan(p.y, p.x);
    if(theta < 0.0) {
        theta += 6.2831853;
    }
    float radius = (bounds.z - bounds.x) * 0.5;
    return vec2(r / radius, theta);
}

void main()
{
    vec2 center = vec2((bounds.x + bounds.z) * 0.5, (bounds.y + bounds.w) * 0.5f);
    vec2 fragPos = gl_FragCoord.xy;
    vec2 pix = fragPos;
    pix -= center;
    float radius = (bounds.z - bounds.x) * 0.5;
    if(length(fragPos - center) <= radius){
        FragColor = vec4(colorFromPolar(cartesianToPolar(pix)), 1);
    }
    else{
        discard;
    }
}