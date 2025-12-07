#version 330 core

in vec2 texCoord;
out vec4 FragColor;

uniform vec3 colour;
uniform vec3 hoverColour;
uniform int currentIndex;
uniform vec4 bounds;
uniform int segments;
uniform float deadZone;
uniform sampler2D sampler;
uniform bool useTexture;

void main()
{
    if (useTexture)
    {
        vec4 assessedColour = texture(sampler, texCoord);
        if(assessedColour.w < 0.05){
            discard;
        }
        else{
            float arcDelta = radians(360.0 / segments);
            vec2 segVec = vec2(sin(arcDelta * currentIndex), cos(arcDelta * currentIndex));
            vec2 center = vec2((bounds.x + bounds.z) * 0.5, (bounds.y + bounds.w) * 0.5);
            vec2 fragPos = gl_FragCoord.xy;
            vec2 fragVec = normalize(fragPos - center);
            float deadZoneSqrd = deadZone * deadZone;
            vec4 applicableColour = acos(dot(fragVec, segVec)) > arcDelta * 0.5 || dot(fragVec, segVec) < 0 || length(fragPos - center) < deadZone ? vec4(colour, 1.0) : vec4(hoverColour, 1.0);
            FragColor = assessedColour * applicableColour;
        }
    }
    else
    {
        FragColor = vec4(colour, 1.0);
    }
}