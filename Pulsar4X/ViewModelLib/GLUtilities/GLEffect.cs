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

namespace Pulsar4X.ViewModel.GLUtilities
{
    public class GLEffect
    {
#if LOG4NET_ENABLED
        public static readonly ILog logger = LogManager.GetLogger(typeof(GLEffect));
#endif

        /// <summary> Used for getting the last OpenGL Error. </summary>
        protected ErrorCode m_eGLError;

        /// <summary>
        /// OpenGL Handle for this Shader.
        /// </summary>
        protected int m_iShaderProgramHandle;

        /// <summary>
        /// Stores the locations of the Projection, View and Model Matricies.
        /// </summary>
        protected int[] m_aiShaderMatrixLocations;


        public GLEffect() { }


        /// <summary>   Constructor. </summary>
        /// <param name="a_szVertShaderFile">   The Vertex shader file. </param>
        /// <param name="a_szFragShaderFile">   The fragment shader file. </param>
        //public GLEffect(string a_szVertShaderFile, string a_szFragShaderFile)
        //{
        //    // Load Shader source files:
        //    string szVertShaderSource = "";
        //    if (System.IO.File.Exists(a_szVertShaderFile))
        //    {
        //        System.IO.StreamReader oVertFile = new System.IO.StreamReader(a_szVertShaderFile);
        //        szVertShaderSource = oVertFile.ReadToEnd();
        //        oVertFile.Close();
        //    }
        //    else
        //    {
        //        logger.Error("Could not load vertex shader file: " + a_szVertShaderFile);
        //        // Set a default shader in the hopes that it will work. its better than nothing *shrug*
        //        switch (OpenTKUtilities.Instance.SupportedOpenGLVersion)
        //        {
        //            case OpenTKUtilities.GLVersion.OpenGL2X:
        //                szVertShaderSource = m_szVertexShaderVer120;
        //                break;
        //            default:
        //                szVertShaderSource = m_szVertexShaderVer150;
        //                break;
        //        }
        //    }

        //    string szFragShaderSource = "";
        //    if (System.IO.File.Exists(a_szFragShaderFile))
        //    {
        //        System.IO.StreamReader oFragFile = new System.IO.StreamReader(a_szFragShaderFile);
        //        szFragShaderSource = oFragFile.ReadToEnd();
        //        oFragFile.Close();
        //    }
        //    else
        //    {
        //        logger.Error("Could not load fragment shader file: " + a_szFragShaderFile);
        //        // Set a default shader in the hopes that it will work. its better than nothing *shrug*
        //        switch (OpenTKUtilities.Instance.SupportedOpenGLVersion)
        //        {
        //            case OpenTKUtilities.GLVersion.OpenGL2X:
        //                szFragShaderSource = m_szPixelShaderVer120;
        //                break;
        //            default:
        //                szFragShaderSource = m_szPixelShaderVer150;
        //                break;
        //        }
        //    }

        //    int iShaderError = 1;

        //    int iGLVertexShader = GL.CreateShader(ShaderType.VertexShader);    // Get a shader handle from open GL
        //    GL.ShaderSource(iGLVertexShader, szVertShaderSource);                  // Let OpenGL know about the source code for the shandle provided.
        //    GL.CompileShader(iGLVertexShader);                                 // Tell OpenGL to compile the shaders gened above.

        //    GL.GetShader(iGLVertexShader, ShaderParameter.CompileStatus, out iShaderError);
        //    if (iShaderError != 1)
        //    {
        //        logger.Error("Error " + iShaderError.ToString() + " Compiling Vertex Shader: " + GL.GetShaderInfoLog(iGLVertexShader)); // Log Result!
        //        iShaderError = 1;
        //    }

        //    int iGLPixelShader = GL.CreateShader(ShaderType.FragmentShader);    // Get a shader handle from open GL
        //    GL.ShaderSource(iGLPixelShader, szFragShaderSource);                     // Let OpenGL know about the source code for the shandle provided.
        //    GL.CompileShader(iGLPixelShader);                                   // Tell OpenGL to compile the shaders gened above.

        //    GL.GetShader(iGLPixelShader, ShaderParameter.CompileStatus, out iShaderError);
        //    if (iShaderError != 1)
        //    {
        //        logger.Error("Error " + iShaderError.ToString() + " Compiling Fragment/Pixel Shader: " + GL.GetShaderInfoLog(iGLPixelShader)); // Log Result!
        //        iShaderError = 1;
        //    }

        //    m_iShaderProgramHandle = GL.CreateProgram();                        // Tell OpenGL to creat a handle for a complete shader program (composed of the above two shaders).
        //    GL.AttachShader(m_iShaderProgramHandle, iGLVertexShader);           // Attache our Vertex shader to the program.
        //    GL.AttachShader(m_iShaderProgramHandle, iGLPixelShader);            // Attache our Pixel (fragment) shader to our program.
        //    // Note the below 4 function calls bind our vertex components in C# to our OpenGL shader.
        //    GL.BindAttribLocation(m_iShaderProgramHandle, 0, "VertexPosition"); // Binds the vertex position Variable in the shader program to the index 0.
        //    GL.BindAttribLocation(m_iShaderProgramHandle, 1, "VertexColour");   // Binds the vertex color Variable in the shader program to the index 1.
        //    GL.BindAttribLocation(m_iShaderProgramHandle, 2, "UVCord");         // Binds the vertex UC coords Variable in the shader program to the index 2.
        //    if (OpenTKUtilities.Instance.SupportedOpenGLVersion != OpenTKUtilities.GLVersion.OpenGL2X)
        //    {
        //        GL.BindFragDataLocation(m_iShaderProgramHandle, 0, "FragColor");    // Binds the Pixel (fragment) color Variable to the index 3, only for GL3.0 or greater!!
        //    }
        //    GL.LinkProgram(m_iShaderProgramHandle);                             // Compiles the Shader into a complete program ready to be run on the GPU. (think linker stage in normal compiling).

        //    GL.GetProgram(m_iShaderProgramHandle, ProgramParameter.ValidateStatus, out iShaderError);
        //    if (iShaderError != 1 || iShaderError != 0)
        //    {
        //        logger.Error("Error " + iShaderError.ToString() + " Creating Shader Program: " + GL.GetShaderInfoLog(m_iShaderProgramHandle)); // Log Result!
        //        iShaderError = 1;
        //    }

        //    // The Following Bind our Projection, view (camera) and model Matricies in c# to the corosponding vars in the shader program
        //    // it is what allows us to update a matrix in c# and have the GPU do all the calculations for Transformations on next render.
        //    m_aiShaderMatrixLocations = new int[3];     // create memory.
        //    m_aiShaderMatrixLocations[0] = GL.GetUniformLocation(m_iShaderProgramHandle, "ProjectionMatrix");
        //    m_aiShaderMatrixLocations[1] = GL.GetUniformLocation(m_iShaderProgramHandle, "ViewMatrix");
        //    m_aiShaderMatrixLocations[2] = GL.GetUniformLocation(m_iShaderProgramHandle, "ModelMatrix");
        //    ErrorCode m_eGLError = GL.GetError();
        //    if (m_eGLError != ErrorCode.NoError)
        //    {
        //        logger.Info("OpenGL Bind Matricies to Shader Code: " + m_eGLError.ToString());
        //    }   
        //    // This tells OpenGL to delete the shader objects. 
        //    // Note that OpenGL wont delete them until all shader programs currently useing them are deleted also.
        //    // Deleteing them now lets OpenGL know that we don't want to use tem again in a different shader (if we do we will need to re compile them).
        //    // Allowing OpenGL to clean up after us. i.e. we do it now so we don't forget later ;)
        //    GL.DeleteShader(iGLVertexShader);
        //    GL.DeleteShader(iGLPixelShader);
        //}

        ///// <summary>   
        ///// Creates the default shader for openGL 2.1 or higher
        ///// </summary>
        //void CreateDefaultVer120()
        //{
        //    int iShaderError = 1;

        //    int iGLVertexShader = GL.CreateShader(ShaderType.VertexShader);    // Get a shader handle from open GL
        //    GL.ShaderSource(iGLVertexShader, m_szVertexShaderVer120);                  // Let OpenGL know about the source code for the shandle provided.
        //    GL.CompileShader(iGLVertexShader);                                 // Tell OpenGL to compile the shaders gened above.

        //    GL.GetShader(iGLVertexShader, ShaderParameter.CompileStatus, out iShaderError);
        //    if (iShaderError != 1)
        //    {
        //        logger.Error("Error " + iShaderError.ToString() + " Compiling Vertex Shader: " + GL.GetShaderInfoLog(iGLVertexShader)); // Log Result!
        //        iShaderError = 1;
        //    }

        //    int iGLPixelShader = GL.CreateShader(ShaderType.FragmentShader);    // Get a shader handle from open GL
        //    GL.ShaderSource(iGLPixelShader, m_szPixelShaderVer120);                     // Let OpenGL know about the source code for the shandle provided.
        //    GL.CompileShader(iGLPixelShader);                                   // Tell OpenGL to compile the shaders gened above.

        //    GL.GetShader(iGLPixelShader, ShaderParameter.CompileStatus, out iShaderError);
        //    if (iShaderError != 1)
        //    {
        //        logger.Error("Error " + iShaderError.ToString() + " Compiling Fragment/Pixel Shader: " + GL.GetShaderInfoLog(iGLPixelShader)); // Log Result!
        //        iShaderError = 1;
        //    }

        //    m_iShaderProgramHandle = GL.CreateProgram();                        // Tell OpenGL to creat a handle for a complete shader program (composed of the above two shaders).
        //    GL.AttachShader(m_iShaderProgramHandle, iGLVertexShader);           // Attache our Vertex shader to the program.
        //    GL.AttachShader(m_iShaderProgramHandle, iGLPixelShader);            // Attache our Pixel (fragment) shader to our program.

        //    // Note the below 4 function calls bind our vertex components in C# to our OpenGL shader.
        //    GL.BindAttribLocation(m_iShaderProgramHandle, 0, "VertexPosition"); // Binds the vertex position Variable in the shader program to the index 0.
        //    GL.BindAttribLocation(m_iShaderProgramHandle, 1, "VertexColour");   // Binds the vertex color Variable in the shader program to the index 1.
        //    GL.BindAttribLocation(m_iShaderProgramHandle, 2, "UVCord");         // Binds the vertex UC coords Variable in the shader program to the index 2.
        //    GL.LinkProgram(m_iShaderProgramHandle);                             // Compiles the Shader into a complete program ready to be run on the GPU. (think linker stage in normal compiling).

        //    GL.GetProgram(m_iShaderProgramHandle, ProgramParameter.ValidateStatus, out iShaderError);
        //    if (iShaderError != 1)
        //    {
        //        logger.Error("Error " + iShaderError.ToString() + " Creating Shader Program: " + GL.GetShaderInfoLog(m_iShaderProgramHandle)); // Log Result!
        //        iShaderError = 1;
        //    }

        //    logger.Info("OpenGL Pre Bind Matricies to Shader Code: " + GL.GetError().ToString());
        //    // The Following Bind our Projection, view (camera) and model Matricies in c# to the corosponding vars in the shader program
        //    // it is what allows us to update a matrix in c# and have the GPU do all the calculations for Transformations on next render.
        //    m_aiShaderMatrixLocations = new int[3];     // create memory.
        //    m_aiShaderMatrixLocations[0] = GL.GetUniformLocation(m_iShaderProgramHandle, "ProjectionMatrix");
        //    m_aiShaderMatrixLocations[1] = GL.GetUniformLocation(m_iShaderProgramHandle, "ViewMatrix");
        //    m_aiShaderMatrixLocations[2] = GL.GetUniformLocation(m_iShaderProgramHandle, "ModelMatrix");
        //    m_eGLError = GL.GetError();
        //    if (m_eGLError != ErrorCode.NoError)
        //    {
        //        logger.Info("OpenGL Bind Matricies to Shader Code: " + m_eGLError.ToString());
        //    }   
        //    // This tells OpenGL to delete the shader objects. 
        //    // Note that OpenGL wont delete them until all shader programs currently useing them are deleted also.
        //    // Deleteing them now lets OpenGL know that we don't want to use tem again in a different shader (if we do we will need to re compile them).
        //    // Allowing OpenGL to clean up after us. i.e. we do it now so we don't forget later ;)
        //    GL.DeleteShader(iGLVertexShader);
        //    GL.DeleteShader(iGLPixelShader);
        //}

        ///// <summary>   
        ///// Creates the default Shader for openGL 3.2 or higher.
        ///// </summary>
        //void CreateDefaultVer150()
        //{
        //    int iShaderError = 1;

        //    int iGLVertexShader = GL.CreateShader(ShaderType.VertexShader);    // Get a shader handle from open GL
        //    GL.ShaderSource(iGLVertexShader, m_szVertexShaderVer150);          // Let OpenGL know about the source code for the shandle provided.
        //    GL.CompileShader(iGLVertexShader);                                 // Tell OpenGL to compile the shaders gened above.

        //    GL.GetShader(iGLVertexShader, ShaderParameter.CompileStatus, out iShaderError);
        //    if (iShaderError != 1)
        //    {
        //        logger.Error("Error " + iShaderError.ToString() + " Compiling Vertex Shader: " + GL.GetShaderInfoLog(iGLVertexShader)); // Log Result!
        //        iShaderError = 1;
        //    }

        //    int iGLPixelShader = GL.CreateShader(ShaderType.FragmentShader);    // Get a shader handle from open GL
        //    GL.ShaderSource(iGLPixelShader, m_szPixelShaderVer150);                     // Let OpenGL know about the source code for the shandle provided.
        //    GL.CompileShader(iGLPixelShader);                                   // Tell OpenGL to compile the shaders gened above.

        //    GL.GetShader(iGLPixelShader, ShaderParameter.CompileStatus, out iShaderError);
        //    if (iShaderError != 1)
        //    {
        //        logger.Error("Error " + iShaderError.ToString() + " Compiling Fragment/Pixel Shader: " + GL.GetShaderInfoLog(iGLPixelShader)); // Log Result!
        //        iShaderError = 1;
        //    }

        //    m_iShaderProgramHandle = GL.CreateProgram();                        // Tell OpenGL to creat a handle for a complete shader program (composed of the above two shaders).
        //    GL.AttachShader(m_iShaderProgramHandle, iGLVertexShader);           // Attache our Vertex shader to the program.
        //    GL.AttachShader(m_iShaderProgramHandle, iGLPixelShader);            // Attache our Pixel (fragment) shader to our program.
        //    // Note the below 4 function calls bind our vertex components in C# to our OpenGL shader.
        //    GL.BindAttribLocation(m_iShaderProgramHandle, 0, "VertexPosition"); // Binds the vertex position Variable in the shader program to the index 0.
        //    GL.BindAttribLocation(m_iShaderProgramHandle, 1, "VertexColour");   // Binds the vertex color Variable in the shader program to the index 1.
        //    GL.BindAttribLocation(m_iShaderProgramHandle, 2, "UVCord");         // Binds the vertex UC coords Variable in the shader program to the index 2.
        //    GL.BindFragDataLocation(m_iShaderProgramHandle, 0, "FragColor");    // Binds the Pixel (fragment) color Variable to the index 3.
        //    GL.LinkProgram(m_iShaderProgramHandle);                             // Compiles the Shader into a complete program ready to be run on the GPU. (think linker stage in normal compiling).

        //    GL.GetProgram(m_iShaderProgramHandle, ProgramParameter.ValidateStatus, out iShaderError);
        //    if (iShaderError != 1)
        //    {
        //        logger.Error("Error " + iShaderError.ToString() + " Creating Shader Program: " + GL.GetShaderInfoLog(m_iShaderProgramHandle)); // Log Result!
        //        iShaderError = 1;
        //    }

        //    // The Following Bind our Projection, view (camera) and model Matricies in c# to the corosponding vars in the shader program
        //    // it is what allows us to update a matrix in c# and have the GPU do all the calculations for Transformations on next render.
        //    m_aiShaderMatrixLocations = new int[3];     // create memory.
        //    m_aiShaderMatrixLocations[0] = GL.GetUniformLocation(m_iShaderProgramHandle, "ProjectionMatrix");
        //    m_aiShaderMatrixLocations[1] = GL.GetUniformLocation(m_iShaderProgramHandle, "ViewMatrix");
        //    m_aiShaderMatrixLocations[2] = GL.GetUniformLocation(m_iShaderProgramHandle, "ModelMatrix");

        //    m_eGLError = GL.GetError();
        //    if (m_eGLError != ErrorCode.NoError)
        //    {
        //        logger.Info("OpenGL Bind Matricies to Shader Code: " + m_eGLError.ToString());
        //    }   
        //    // This tells OpenGL to delete the shader objects. 
        //    // Note that OpenGL wont delete them until all shader programs currently useing them are deleted also.
        //    // Deleteing them now lets OpenGL know that we don't want to use tem again in a different shader (if we do we will need to re compile them).
        //    // Allowing OpenGL to clean up after us. i.e. we do it now so we don't forget later ;)
        //    GL.DeleteShader(iGLVertexShader);
        //    GL.DeleteShader(iGLPixelShader);
        //}


        public void SetProjectionMatrix(ref Matrix4 a_m4Projection)
        {
            GL.UseProgram(m_iShaderProgramHandle);
            GL.UniformMatrix4(m_aiShaderMatrixLocations[0], false, ref a_m4Projection);
        }

        public void SetViewMatrix(ref Matrix4 a_m4View)
        {
            GL.UseProgram(m_iShaderProgramHandle);
            GL.UniformMatrix4(m_aiShaderMatrixLocations[1], false, ref a_m4View);
        }

        public void SetModelMatrix(ref Matrix4 a_m4Model)
        {
            GL.UseProgram(m_iShaderProgramHandle);
            GL.UniformMatrix4(m_aiShaderMatrixLocations[2], false, ref a_m4Model);
        }

        public void StartUsing(ref Matrix4 a_m4Model)
        {
            GL.UseProgram(m_iShaderProgramHandle);
            GL.UniformMatrix4(m_aiShaderMatrixLocations[2], false, ref a_m4Model);
        }

        public void StartUsing(ref Matrix4 a_m4Projection, ref Matrix4 a_m4View, ref Matrix4 a_m4Model)
        {
            GL.UseProgram(m_iShaderProgramHandle);

            GL.UniformMatrix4(m_aiShaderMatrixLocations[0], false, ref a_m4Projection);
            GL.UniformMatrix4(m_aiShaderMatrixLocations[1], false, ref a_m4View);
            GL.UniformMatrix4(m_aiShaderMatrixLocations[2], false, ref a_m4Model);
        }

        public void StartUsing()
        {
            GL.UseProgram(m_iShaderProgramHandle);
        }

        public void StopUsing()
        {
            GL.UseProgram(0);
        }
    }
}
