#version 150
                                                                             
precision highp float;
                                                                  
uniform sampler2D TextureSampler; 
uniform sampler2D TextureSampler2;
                                                       
in vec4 PixelColour;                                                                    
in vec2 TexCoord;
                                                                        
out vec4 FragColor; 
                                                                     
void main()                                                                            
{                                                                                    
    FragColor = texture2D(TextureSampler, TexCoord) * PixelColour;     
};
