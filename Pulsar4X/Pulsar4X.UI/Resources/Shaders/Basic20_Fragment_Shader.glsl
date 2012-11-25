#version 120
                                                                                                                                       
uniform sampler2D TextureSampler; 
                                                       
varying vec4 PixelColour;                                                                    
varying vec2 TexCoord;
                                                                                                                                          
void main()                                                                            
{                                                                                    
    gl_FragColor = texture2D(TextureSampler, TexCoord) * PixelColour;              
}
