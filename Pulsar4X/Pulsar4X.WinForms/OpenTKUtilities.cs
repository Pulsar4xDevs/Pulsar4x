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
        /// List of Textures already loaded.
        /// </summary>
        List<TextureData> m_lLoadedTextures = new List<TextureData>();

        /// <summary>
        /// Used to determine our currently active texture;
        /// </summary>
        uint m_uiActiveTexture = 0;

        /// <summary>
        /// Used to determine our currently active shader.
        /// </summary>
        int m_iActiveShader = 0;

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
        /// The supported open gl version 
        /// </summary>
        GLVersion m_eSupportedOpenGLVersion;
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
        /// Instance of this class/singelton
        /// </summary>
        private static readonly OpenTKUtilities m_oInstance = new OpenTKUtilities(); 


        private OpenTKUtilities() { }

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

        /// <summary>
        /// Initialises the OpenTK Framework, Specificaly OpenGL.
        /// </summary>
        /// <returns>True If Successfull, false otherwise.</returns>
        public bool Initialise(GLVersion a_eGLVersion = GLVersion.Unknown)
        {
            // create a OpenTK Controll and query it for the GLContext version number:
            OpenTK.GLControl oTest = new GLControl(new GraphicsMode(32,24,8,4), 4, 0, GraphicsContextFlags.Default);
            Form oOpenGLVersionCheck = new Form();
            oOpenGLVersionCheck.Controls.Add(oTest);
            oOpenGLVersionCheck.Show();
            oOpenGLVersionCheck.Hide();
            
            string szOpenGLVersion = GL.GetString(StringName.Version);
            int iMajor = int.Parse(szOpenGLVersion[0].ToString());      // extracts the major verion number an converts it to a int.
            int iMinor = int.Parse(szOpenGLVersion[2].ToString());      // same again for minor verion number.

            #if DEBUG
                Program.logger.Debug("Highest OpenGL Version Initialised is " + szOpenGLVersion);  
            #endif

            if (iMajor == 1)
            {
                m_eSupportedOpenGLVersion = GLVersion.OpenGL1X;
            }
            else if (iMajor == 3 && iMinor < 2)
            {
                m_eSupportedOpenGLVersion = GLVersion.OpenGL2X;
            }
            else if (iMajor == 3 && iMinor >= 2)
            {
                m_eSupportedOpenGLVersion = GLVersion.OpenGL3X;
            }
            else if (iMajor == 4)
            {
                m_eSupportedOpenGLVersion = GLVersion.OpenGL4X;
            }


            if (a_eGLVersion != GLVersion.Unknown)
            {
                m_eSupportedOpenGLVersion = a_eGLVersion;
                #if DEBUG
                    Program.logger.Warn("OpenGL Version Autodetect has been overridden to " + a_eGLVersion.ToString());
                #endif
            }

            return true;
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
        /// Loads in a texture file from disk and returns the openGL texture ID.
        /// </summary>
        /// <param name="a_szTextureFile">Path to the Texture file to be loaded.</param>
        /// <returns>0 if error, else an OpenGL Texture ID</returns>
        static public uint LoadTexture(string a_szTextureFile)
        {
            // First check if the file exists:
            if (!System.IO.File.Exists(a_szTextureFile))
            {
                return 0; // retun 0 if invalid file.
            }


            // Second Chak if we have already loaded the file:
            TextureData oTexture = m_oInstance.m_lLoadedTextures.Find(
                delegate(TextureData oTextureData)
                {
                    return oTextureData.m_szTextureFile == a_szTextureFile;
                }
            );
            if (oTexture != null)
            {
                // then we have already loaded this texture.
                oTexture.m_iUseCount++;             // Increase count of how oftern it has been used,
                return oTexture.m_uiTextureID;      // and return it.
            }

            // looks like the file is valid and we have not loaded it before
            // Create New Texture Data:
            TextureData oNewTexture = new TextureData();
            oNewTexture.m_szTextureFile = a_szTextureFile;
            oNewTexture.m_iUseCount = 1;

            // load the file into a bitmap
            System.Drawing.Bitmap oTextureBitmap = new System.Drawing.Bitmap(a_szTextureFile);

            // Get the data out of the bit map
            System.Drawing.Imaging.BitmapData oRawTextureData = oTextureBitmap.LockBits(new Rectangle(0, 0, oTextureBitmap.Width, oTextureBitmap.Height),
                                                                                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                                                                    System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            // Code to transfer Texture Data to GPU
                
            // Generate Texture Handle
            GL.GenTextures(1, out oNewTexture.m_uiTextureID);
            // Tell openGL that this is a 2d texture:
            GL.BindTexture(TextureTarget.Texture2D, oNewTexture.m_uiTextureID);

            // Configure Text Paramaters:
            //GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)TextureEnvMode.Modulate);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Nearest);

            // Load data by telling OpenGL to build mipmaps out of bitmap data
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, oTextureBitmap.Width, oTextureBitmap.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, oRawTextureData.Scan0);

            Program.logger.Info("OpenGL Loading Texture " + a_szTextureFile + ": " + GL.GetError().ToString());

            // Now that we have provided the data to OpenGL we can free the texture from system Ram.
            oTextureBitmap.UnlockBits(oRawTextureData);

            return oNewTexture.m_uiTextureID;
        }


        /// <summary>
        /// Unloads the Specified Texture if it is no longer in use.
        /// </summary>
        /// <param name="a_uiTextureID">The Texture to be unloaded.</param>
        static public void UnloadTexture(uint a_uiTextureID)
        {
            // find the texture data.
            int oTextureIndex = m_oInstance.m_lLoadedTextures.FindIndex(
                delegate(TextureData oTextureData)
                {
                    return oTextureData.m_uiTextureID == a_uiTextureID;
                }
            );
            if (oTextureIndex < 0)
            {
                // then there is a valid Texture for this ID:
                TextureData oTexture = m_oInstance.m_lLoadedTextures.ElementAt(oTextureIndex);
                oTexture.m_iUseCount--;             // Decrease count of how oftern it has been used,

                // test to see if it is no longer used:
                if (oTexture.m_iUseCount <= 0)
                {
                    GL.DeleteTexture(oTexture.m_uiTextureID);   // Delete the texture from OpenGL.

                    m_oInstance.m_lLoadedTextures.RemoveAt(oTextureIndex); // delete the data from the list.
                } 
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
