#version 150
                                                                          
in vec3 VertexPosition;                                                             
in vec4 VertexColor;                                                                 
in vec2 UVCord;
                                                                      
uniform mat4 ProjectionMatrix;                                                        
uniform mat4 ViewMatrix;                                                              
uniform mat4 ModelMatrix;
                                                                
out vec4 PixelColour;                                                                    
out vec2 TexCoord;
                                                                       
void main()                                                                          
{                                                                                       
    gl_Position = ProjectionMatrix * ViewMatrix * ModelMatrix * vec4(VertexPosition, 1.0);              
    TexCoord = UVCord;                                                                   
    PixelColour = VertexColor;                                                           
};
