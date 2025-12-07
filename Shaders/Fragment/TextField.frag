#version 330 core

in vec2 texCoord;
out vec4 FragColor;

uniform vec3 colour;
uniform sampler2D sampler;
uniform bool useTexture;
uniform bool caretVisible;
uniform float caretPos;
uniform float leftSelect;
uniform float rightSelect;
uniform float caretMargin;
uniform vec4 bounds;
uniform vec3 highlightColour;

void main()
{
    vec2 fragPos = gl_FragCoord.xy;
    if (useTexture)
    {
        if(caretVisible){
            if(abs(fragPos.x - caretPos) <= 1 && fragPos.y <= bounds.w - caretMargin && fragPos.y >= bounds.y + caretMargin){
                FragColor = vec4(0, 0, 0, 1);
            }
            else{
                if(fragPos.x >= leftSelect && fragPos.x < rightSelect&& fragPos.y <= bounds.w - caretMargin && fragPos.y >= bounds.y + caretMargin){
                    FragColor = texture(sampler, texCoord) * vec4(highlightColour, 1.0);
                }
                else{
                    FragColor = texture(sampler, texCoord) * vec4(colour, 1.0);
                }     
            }
        }
        else{
            if(fragPos.x >= leftSelect && fragPos.x < rightSelect&& fragPos.y <= bounds.w - caretMargin && fragPos.y >= bounds.y + caretMargin){
                FragColor = texture(sampler, texCoord) * vec4(highlightColour, 1.0);
            }
            else{
                FragColor = texture(sampler, texCoord) * vec4(colour, 1.0);
            }
        }
    }
    else
    {
        if(caretVisible){
            if(abs(fragPos.x - caretPos) <= 1 && fragPos.y <= bounds.w - caretMargin && fragPos.y >= bounds.y + caretMargin){
                FragColor = vec4(0, 0, 0, 1);
            }
            else{
                if(fragPos.x >= leftSelect && fragPos.x < rightSelect&& fragPos.y <= bounds.w - caretMargin && fragPos.y >= bounds.y + caretMargin){
                    FragColor = vec4(colour, 1.0) * vec4(highlightColour, 1.0);
                }
                else{
                    FragColor = vec4(colour, 1.0);
                } 
            }
        }
        else{
            if(fragPos.x >= leftSelect && fragPos.x < rightSelect&& fragPos.y <= bounds.w - caretMargin && fragPos.y >= bounds.y + caretMargin){
                FragColor = vec4(colour, 1.0) * vec4(highlightColour, 1.0);
            }
            else{
                FragColor = vec4(colour, 1.0);
            }
        }
    }
}