using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics;
using Pulsar4X.WinForms;
using Pulsar4X.WinForms.Controls;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using log4net.Config;
using log4net;

namespace Pulsar4X.WinForms.GLUtilities
{
    /// <summary>
    /// An OpenGL shader program.
    /// </summary>
    class GLShader
    {
        /// <summary>
        /// OpenGL Handle for this Shader.
        /// </summary>
        private int m_iShaderProgramHandle;

        /// <summary>
        /// Stores the locations of the Projection, View and Model Matricies.
        /// </summary>
        private int[] m_aiShaderMatrixLocations;

        public GLShader()
        {
            string szVertexShader = @"
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
};";

            string szPixelShader = @"
#version 150
                                                                             
precision highp float;
                                                                  
uniform sampler2D TextureSampler; 
                                                       
in vec4 PixelColour;                                                                    
in vec2 TexCoord;
                                                                        
out vec4 FragColor; 
                                                                     
void main()                                                                            
{                                                                                    
    FragColor = texture2D(TextureSampler, TexCoord) * PixelColour;                   
};";

            int iGLVertexShader = GL.CreateShader(ShaderType.VertexShader);    // Get a shader handle from open GL
            GL.ShaderSource(iGLVertexShader, szVertexShader);                  // Let OpenGL know about the source code for the shandle provided.
            GL.CompileShader(iGLVertexShader);                                 // Tell OpenGL to compile the shaders gened above.
            
            Program.logger.Info("Compiling Vertex Shader: " + GL.GetShaderInfoLog(iGLVertexShader)); // Log Result!

            int iGLPixelShader = GL.CreateShader(ShaderType.FragmentShader);    // Get a shader handle from open GL
            GL.ShaderSource(iGLPixelShader, szPixelShader);                     // Let OpenGL know about the source code for the shandle provided.
            GL.CompileShader(iGLPixelShader);                                   // Tell OpenGL to compile the shaders gened above.

            Program.logger.Info("Compiling Vertex Shader: " + GL.GetShaderInfoLog(iGLPixelShader)); // Log Result!

            m_iShaderProgramHandle = GL.CreateProgram();                        // Tell OpenGL to creat a handle for a complete shader program (composed of the above two shaders).
            GL.AttachShader(m_iShaderProgramHandle, iGLVertexShader);           // Attache our Vertex shader to the program.
            GL.AttachShader(m_iShaderProgramHandle, iGLPixelShader);            // Attache our Pixel (fragment) shader to our program.
            // Note the below 4 function calls bind our vertex components in C# to our OpenGL shader.
            GL.BindAttribLocation(m_iShaderProgramHandle, 0, "VertexPosition"); // Binds the vertex position Variable in the shader program to the index 0.
            GL.BindAttribLocation(m_iShaderProgramHandle, 1, "VertexColour");   // Binds the vertex color Variable in the shader program to the index 1.
            GL.BindAttribLocation(m_iShaderProgramHandle, 2, "UVCord");         // Binds the vertex UC coords Variable in the shader program to the index 2.
            GL.BindFragDataLocation(m_iShaderProgramHandle, 3, "FragColor");    // Binds the Pixel (fragment) color Variable to the index 3.

            GL.LinkProgram(m_iShaderProgramHandle);                             // Compiles the Shader into a complete program ready to be run on the GPU. (think linker stage in normal compiling).
            int temp = 1;
            Program.logger.Info("Creating Shader Program: " + GL.GetProgramInfoLog(m_iShaderProgramHandle)); // Log Result!
            GL.GetProgram(m_iShaderProgramHandle, ProgramParameter.ValidateStatus, out temp);
            Program.logger.Info("Validating shader: " + temp.ToString());

            // The Following Bind our Projection, view (camera) and model Matricies in c# to the corosponding vars in the shader program
            // it is what allows us to update a matrix in c# and have the GPU do all the calculations for Transformations on next render.
            m_aiShaderMatrixLocations = new int[3];     // create memory.
            m_aiShaderMatrixLocations[0] = GL.GetUniformLocation(m_iShaderProgramHandle, "ProjectionMatrix");
            m_aiShaderMatrixLocations[1] = GL.GetUniformLocation(m_iShaderProgramHandle, "ViewMatrix");
            m_aiShaderMatrixLocations[2] = GL.GetUniformLocation(m_iShaderProgramHandle, "ModelMatrix");

            Program.logger.Info("OpenGL Bind Matricies to Shader Code: " + GL.GetError().ToString());
            // This tells OpenGL to delete the shader objects. 
            // Note that OpenGL wont delete them until all shader programs currently useing them are deleted also.
            // Deleteing them now lets OpenGL know that we don't want to use tem again in a different shader (if we do we will need to re compile them).
            // Allowing OpenGL to clean up after us. i.e. we do it now so we don't forget later ;)
            GL.DeleteShader(iGLVertexShader);
            GL.DeleteShader(iGLPixelShader);
        }


        public GLShader(string a_szVertexShader, string a_szPixelShader)
        {
            throw new NotImplementedException();
        }


        ~GLShader()
        {
            // lets try and clean up after ourselves!!
            //GL.DeleteShader(m_iShaderProgramHandle);
        }


        public void StartUsing(ref Matrix4 a_m4Projection, ref Matrix4 a_m4View, ref Matrix4 a_m4Model)
        {
            GL.UseProgram(m_iShaderProgramHandle);
            //GL.UniformMatrix4(m_aiShaderMatrixLocations[0], 1, false, ref a_m4Projection.Row0.X);
            //GL.UniformMatrix4(m_aiShaderMatrixLocations[1], 1, false, ref a_m4View.Row0.X);
            //GL.UniformMatrix4(m_aiShaderMatrixLocations[2], 1, false, ref a_m4Model.Row0.X);

            GL.UniformMatrix4(m_aiShaderMatrixLocations[0], false, ref a_m4Projection);
            GL.UniformMatrix4(m_aiShaderMatrixLocations[1], false, ref a_m4View);
            GL.UniformMatrix4(m_aiShaderMatrixLocations[2], false, ref a_m4Model);
        }

        public void StopUsing()
        {
            GL.UseProgram( 0 );
        }
    }
}
