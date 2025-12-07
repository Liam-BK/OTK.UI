#version 330 core

in vec2 texCoord;
out vec4 FragColor;

uniform vec3 colour;
uniform sampler2D sampler;
uniform bool useTexture;

void main()
{
    if (useTexture)
    {
        FragColor = texture(sampler, texCoord) * vec4(colour, 1.0);
    }
    else
    {
        FragColor = vec4(colour, 1.0);
    }
}