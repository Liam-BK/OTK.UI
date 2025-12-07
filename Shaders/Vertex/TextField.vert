#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in float aType;

uniform mat4 model;
uniform mat4 projection;

uniform float margin;
uniform float uvMargin;

const vec2 multipliers[13] = vec2[13](
    vec2(0, 0),

    vec2(1, 1),
    vec2(1, 0),
    vec2(0, 1),

    vec2(-1, 1),
    vec2(-1, 0),
    vec2(0, 1),

    vec2(1, -1),
    vec2(1, 0),
    vec2(0, -1),
    
    vec2(-1, -1),
    vec2(-1, 0),
    vec2(0, -1)
);

out vec2 texCoord;

void main()
{
    vec4 pos = model * vec4(aPosition, 1.0);
    vec2 multiplier = multipliers[int(aType + 0.5)];
    pos.x += multiplier.x * margin;
    pos.y += multiplier.y * margin;
    pos = projection * pos;
    gl_Position = pos;
    vec2 tex = aTexCoord + multiplier * uvMargin;
    texCoord = tex;
}