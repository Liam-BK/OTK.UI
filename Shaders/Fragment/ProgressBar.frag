#version 330 core

in vec2 texCoord;
out vec4 FragColor;

uniform vec3 colour;
uniform vec3 fillColour;
uniform sampler2D sampler;
uniform sampler2D sampler1;
uniform vec4 bounds;
uniform float fillAmount;
uniform bool useTexture;
uniform bool useFillTexture;

void main()
{
    if(gl_FragCoord.x <= bounds.x + (bounds.z - bounds.x) * fillAmount){
        if(useFillTexture){
            FragColor = texture(sampler1, texCoord) * vec4(fillColour, 1.0);
        }
        else{
            FragColor = vec4(fillColour, 1.0);
        }
    }
    else{
        if (useTexture)
        {
            FragColor = texture(sampler, texCoord) * vec4(colour, 1.0);
        }
        else
        {
            FragColor = vec4(colour, 1.0);
        }
    }
}