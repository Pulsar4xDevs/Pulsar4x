#version 120
                                                                          
attribute vec3 VertexPosition;                                                             
attribute vec4 VertexColor;                                                                 
attribute vec2 UVCord;
                                                                      
uniform mat4 ProjectionMatrix;                                                        
uniform mat4 ViewMatrix;                                                              
uniform mat4 ModelMatrix;
                                                                
varying vec4 PixelColour;                                                                    
varying vec2 TexCoord;
                                                                       
void main()                                                                          
{                                                                                       
    gl_Position = ProjectionMatrix * ViewMatrix * ModelMatrix * vec4(VertexPosition, 1.0);              
    TexCoord = UVCord;                                                                   
    PixelColour = VertexColor;                                                           
}
