using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK;
#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif

namespace Pulsar4X.CrossPlatformUI.GLUtilities
{
    /// <summary>
    /// Most Basic type of GL effect/Shaders, Works with openGL 2.X
    /// </summary>
    public class GLEffectBasic21 : GLEffect
    {
        /// <summary>   Constructor. </summary>
        /// <param name="a_szVertShaderFile">   The Vertex shader file. </param>
        /// <param name="a_szFragShaderFile">   The fragment shader file. </param>
        public GLEffectBasic21(string a_szVertShaderFile, string a_szFragShaderFile)
        {
            // Load Shader source files:
            string szVertShaderSource = "";
            if (System.IO.File.Exists(a_szVertShaderFile))
            {
                System.IO.StreamReader oVertFile = new System.IO.StreamReader(a_szVertShaderFile);
                szVertShaderSource = oVertFile.ReadToEnd();
                oVertFile.Close();
            }

            string szFragShaderSource = "";
            if (System.IO.File.Exists(a_szFragShaderFile))
            {
                System.IO.StreamReader oFragFile = new System.IO.StreamReader(a_szFragShaderFile);
                szFragShaderSource = oFragFile.ReadToEnd();
                oFragFile.Close();
            }

            int iShaderError = 1;
            string szShaderError;

            int iGLVertexShader = GL.CreateShader(ShaderType.VertexShader);    // Get a shader handle from open GL
            GL.ShaderSource(iGLVertexShader, szVertShaderSource);                  // Let OpenGL know about the source code for the shandle provided.
            GL.CompileShader(iGLVertexShader);                                 // Tell OpenGL to compile the shaders gened above.

            GL.GetShader(iGLVertexShader, ShaderParameter.CompileStatus, out iShaderError);
            GL.GetShaderInfoLog(iGLVertexShader, out szShaderError);
            if (iShaderError != 1)
            {
#if LOG4NET_ENABLED
                logger.Error("Error " + iShaderError.ToString() + " Compiling Vertex Shader: " + szShaderError); // Log Result!
#endif
                iShaderError = 1;
            }

            int iGLPixelShader = GL.CreateShader(ShaderType.FragmentShader);    // Get a shader handle from open GL
            GL.ShaderSource(iGLPixelShader, szFragShaderSource);                     // Let OpenGL know about the source code for the shandle provided.
            GL.CompileShader(iGLPixelShader);                                   // Tell OpenGL to compile the shaders gened above.

            GL.GetShader(iGLPixelShader, ShaderParameter.CompileStatus, out iShaderError);
            GL.GetShaderInfoLog(iGLPixelShader, out szShaderError);
            if (iShaderError != 1)
            {
#if LOG4NET_ENABLED
                logger.Error("Error " + iShaderError.ToString() + " Compiling Fragment/Pixel Shader: " + szShaderError); // Log Result!
#endif
                iShaderError = 1;
            }

            m_iShaderProgramHandle = GL.CreateProgram();                        // Tell OpenGL to creat a handle for a complete shader program (composed of the above two shaders).
            GL.AttachShader(m_iShaderProgramHandle, iGLVertexShader);           // Attache our Vertex shader to the program.
            GL.AttachShader(m_iShaderProgramHandle, iGLPixelShader);            // Attache our Pixel (fragment) shader to our program.

            // Note the below 4 function calls bind our vertex components in C# to our OpenGL shader.
            GL.BindAttribLocation(m_iShaderProgramHandle, 0, "VertexPosition"); // Binds the vertex position Variable in the shader program to the index 0.
            GL.BindAttribLocation(m_iShaderProgramHandle, 1, "VertexColour");   // Binds the vertex color Variable in the shader program to the index 1.
            GL.BindAttribLocation(m_iShaderProgramHandle, 2, "UVCord");         // Binds the vertex UC coords Variable in the shader program to the index 2.
            GL.LinkProgram(m_iShaderProgramHandle);                             // Compiles the Shader into a complete program ready to be run on the GPU. (think linker stage in normal compiling).

            GL.GetProgram(m_iShaderProgramHandle, GetProgramParameterName.ValidateStatus, out iShaderError);
            GL.GetShaderInfoLog(iGLPixelShader, out szShaderError);
            if (iShaderError != 1)
            {
#if LOG4NET_ENABLED
                logger.Error("Error " + iShaderError.ToString() + " Creating Shader Program: " + szShaderError); // Log Result!
#endif
                iShaderError = 1;
            }

#if LOG4NET_ENABLED
            logger.Info("OpenGL Pre Bind Matricies to Shader Code: " + GL.GetError().ToString());
#endif
            // The Following Bind our Projection, view (camera) and model Matricies in c# to the corosponding vars in the shader program
            // it is what allows us to update a matrix in c# and have the GPU do all the calculations for Transformations on next render.
            m_aiShaderMatrixLocations = new int[3];     // create memory.
            m_aiShaderMatrixLocations[0] = GL.GetUniformLocation(m_iShaderProgramHandle, "ProjectionMatrix");
            m_aiShaderMatrixLocations[1] = GL.GetUniformLocation(m_iShaderProgramHandle, "ViewMatrix");
            m_aiShaderMatrixLocations[2] = GL.GetUniformLocation(m_iShaderProgramHandle, "ModelMatrix");

            m_eGLError = GL.GetError();
            if (m_eGLError != ErrorCode.NoError)
            {
#if LOG4NET_ENABLED
                logger.Info("OpenGL Bind Matricies to Shader Code: " + m_eGLError.ToString());
#endif
            }
            // This tells OpenGL to delete the shader objects. 
            // Note that OpenGL wont delete them until all shader programs currently useing them are deleted also.
            // Deleteing them now lets OpenGL know that we don't want to use tem again in a different shader (if we do we will need to re compile them).
            // Allowing OpenGL to clean up after us. i.e. we do it now so we don't forget later ;)
            GL.DeleteShader(iGLVertexShader);
            GL.DeleteShader(iGLPixelShader);
        }
    }
}
