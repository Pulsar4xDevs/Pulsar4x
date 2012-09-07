using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics;
using Pulsar4X.WinForms;
using System.Drawing;
using System.Windows.Forms;
using Pulsar4X.WinForms.Controls;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using log4net.Config;
using log4net;
using OpenTK.Platform;
using Config = OpenTK.Configuration;
using Utilities = OpenTK.Platform.Utilities;

namespace Pulsar4X.WinForms
{
    /// <summary>
    /// Used to initilise, configure, etc. OpenTK components.
    /// </summary>
    public sealed class OpenTKUtilities
    {
        public static readonly ILog logger = LogManager.GetLogger(typeof(OpenTKUtilities));

        /// <summary>
        /// A Structure used to store data about a texture.
        /// </summary>
        class TextureData
        {
            public int m_iUseCount = 0;
            public uint m_uiTextureID = 0;
            public string m_szTextureFile = "null";
        }

        /// <summary>
        /// Values that repesent OpenGL Versions.
        /// </summary>
        public enum GLVersion
        {
            Unknown = 0,
            OpenGL1X,
            OpenGL2X,
            OpenGL3X,
            OpenGL4X
        };

        /// <summary>
        /// List of Textures already loaded.
        /// </summary>
        //List<TextureData> m_lLoadedTextures = new List<TextureData>();

        /// <summary>
        /// Used to determine our currently active texture;
        /// </summary>
        uint m_uiActiveTexture = 0;

        /// <summary>
        /// Used to determine our currently active shader.
        /// </summary>
        int m_iActiveShader = 0;

        /// <summary> 
        /// The supported openGL version 
        /// </summary>
        GLVersion m_eSupportedOpenGLVersion;

        /// <summary>
        /// Instance of this class/singelton
        /// </summary>
        private static readonly OpenTKUtilities m_oInstance = new OpenTKUtilities(); 



        /// <summary>
        /// Returns the instance of the OpenTKUtilities class.
        /// </summary>
        public static OpenTKUtilities Instance
        {
            get
            {
                return m_oInstance;
            }
        }

        /// <summary>   Gets or sets the supported open gl version. </summary>
        /// <value> The supported openGL version. </value>
        public GLVersion SupportedOpenGLVersion
        {
            get
            {
                return m_eSupportedOpenGLVersion;
            }
            set
            {
                m_eSupportedOpenGLVersion = value;
            }
        }



        /// <summary>
        /// Constructor that prevents a default instance of this class from being created.
        /// </summary>
        private OpenTKUtilities() { }

        /// <summary>   Initialises the OpenTK Framework, Specificaly OpenGL. </summary>
        /// <param name="a_eGLVersion"> (optional) the e gl version. </param>
        /// <returns> True If Successfull, false otherwise. </returns>
        public bool Initialise(GLVersion a_eGLVersion = GLVersion.Unknown)
        {
            bool bSuccess = false;  // used to shpow success or failure of Init.

            // create a OpenTK Controll and query it for the GLContext version number:
            OpenTK.GLControl oTest = new GLControl(new GraphicsMode(32,24,8,4), 4, 0, GraphicsContextFlags.Default);
            // We need to "show" a for before OpenTK actuall initilise the GLContext (which we need to happen to get a version number!).
            // the following creates a form that we can show but will never be seen by the end user!
            Form oOpenGLVersionCheck = new Form();
            oOpenGLVersionCheck.Controls.Add(oTest);
            oOpenGLVersionCheck.StartPosition = FormStartPosition.CenterScreen;
            oOpenGLVersionCheck.Size = new Size(0, 0);
            oOpenGLVersionCheck.ShowInTaskbar = false;
            oOpenGLVersionCheck.FormBorderStyle = FormBorderStyle.None;
            oOpenGLVersionCheck.TopMost = false;
            oOpenGLVersionCheck.Show();
            oOpenGLVersionCheck.Hide();
            
            string szOpenGLVersion = GL.GetString(StringName.Version);
            int iMajor = int.Parse(szOpenGLVersion[0].ToString());      // extracts the major verion number an converts it to a int.
            int iMinor = int.Parse(szOpenGLVersion[2].ToString());      // same again for minor verion number.

            #if DEBUG
                logger.Debug("Highest OpenGL Version Initialised is " + szOpenGLVersion);  
            #endif

            if (iMajor == 1)
            {
                m_eSupportedOpenGLVersion = GLVersion.OpenGL1X;
            }
            else if (iMajor == 2)
            {
                m_eSupportedOpenGLVersion = GLVersion.OpenGL2X;
                bSuccess = true;
            }
            else if (iMajor <= 3 && iMinor < 2)
            {
                m_eSupportedOpenGLVersion = GLVersion.OpenGL2X;
                bSuccess = true;
            }
            else if (iMajor <= 3 && iMinor >= 2)
            {
                m_eSupportedOpenGLVersion = GLVersion.OpenGL3X;
                bSuccess = true;
            }
            else if (iMajor == 4)
            {
                m_eSupportedOpenGLVersion = GLVersion.OpenGL4X;
                bSuccess = true;
            }
            else
            {
                logger.Error("OpenGL Version Autodetect Could Not work out which version of OpenGL is on this system");
                logger.Error("OpenGL Version Major is: " + iMajor.ToString() + "OpenGL Minor is: " + iMinor.ToString());
                bSuccess = false;
            }

            // override the Detected veriosn if a one was specified.
            if (a_eGLVersion != GLVersion.Unknown)
            {
                m_eSupportedOpenGLVersion = a_eGLVersion;
                logger.Warn("OpenGL Version Autodetect has been overridden to " + a_eGLVersion.ToString());
                bSuccess = true;
            }

            return bSuccess;
        }


        /// <summary>
        /// Creates and returns a new GLCanvas, the exact version is determined by the OpenGL supproted by the system.
        /// OpenGL 2.x = GLCanvas20
        /// OpenGL 3.x = GLCanvas30
        /// </summary>
        /// <returns>A Valid GLCanvas Control</returns>
        public GLCanvas CreateGLCanvas()
        {
            switch (m_eSupportedOpenGLVersion)
            {
                case GLVersion.OpenGL1X:
                    // Not Supported
                    throw new NotSupportedException();
                case GLVersion.OpenGL2X:
                    return new GLCanves20();
                case GLVersion.OpenGL3X:
                    return new GLCanvas30();
                case GLVersion.OpenGL4X:
                    // not implimented yet, retrun a version 3 instead:
                    return new GLCanvas30();
                default:
                    // Not Supported
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Will bind the specified texture for use if it is not already the current active texture.
        /// This save calles to the GPU if the texture is already set as the avctive texture.
        /// </summary>
        /// <param name="a_uiTextureID">The texture to set.</param>
        static public void Use2DTexture(uint a_uiTextureID)
        {
            if (m_oInstance.m_uiActiveTexture != a_uiTextureID)
            {
                m_oInstance.m_uiActiveTexture = a_uiTextureID;
                GL.BindTexture(TextureTarget.Texture2D, a_uiTextureID);
            }
        }

        /// <summary>
        /// Will Bind the specified Shader Program for use by OpenGL if it is not already the active Program.
        /// This will save Calls to the GPU if the shader program is already set as active.
        /// </summary>
        /// <param name="a_uiShaderProgram">The ShaderProgram to set</param>
        static public void UseShaderProgram(int a_iShaderProgram)
        {
            if (m_oInstance.m_iActiveShader != a_iShaderProgram)
            {
                m_oInstance.m_iActiveShader = a_iShaderProgram;
                GL.UseProgram(a_iShaderProgram);
            }
        }
    }
}
