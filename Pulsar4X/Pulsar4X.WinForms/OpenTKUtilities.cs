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

namespace Pulsar4X.WinForms
{
    /// <summary>
    /// Used to initilise, configure, etc. OpenTK components.
    /// </summary>
    public sealed class OpenTKUtilities
    {
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
        public bool Initialise()
        {
            throw new NotImplementedException();

            //return false; // return false as this has not yet been implemented.
        }


        /// <summary>
        /// Loads in a texture file from disk and returns the openGL texture ID.
        /// </summary>
        /// <param name="a_szTextureFile">Path to the Texture file to be loaded.</param>
        /// <returns>0 if error, else an OpenGL Texture ID</returns>
        static public uint LoadTexture(string a_szTextureFile)
        {
            uint uiTextureHandle = 0;
            // First chaek if the file exists:
            if (System.IO.File.Exists(a_szTextureFile))
            {
                // load the file into a bitmap
                System.Drawing.Bitmap oTextureBitmap = new System.Drawing.Bitmap(a_szTextureFile);

                // Get the data out of the bit map
                System.Drawing.Imaging.BitmapData oTextureData = oTextureBitmap.LockBits(new Rectangle(0, 0, oTextureBitmap.Width, oTextureBitmap.Height),
                                                                                        System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                                                                        System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                // Code to transfer Texture Data to GPU
                
                // Generate Texture Handle
                GL.GenTextures(1, out uiTextureHandle);
                // Tell openGL that this is a 2d texture:
                GL.BindTexture(TextureTarget.Texture2D, uiTextureHandle);

                // Configure Text Paramaters:
                //GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)TextureEnvMode.Modulate);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Nearest);

                // Load data by telling OpenGL to build mipmaps out of bitmap data
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, oTextureBitmap.Width, oTextureBitmap.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, oTextureData.Scan0);

                Program.logger.Info("OpenGL Loading Texture: " + GL.GetError().ToString());

                // Now that we have provided the data to OpenGL we can free the texture from system Ram.
                oTextureBitmap.UnlockBits(oTextureData);
            }

            return uiTextureHandle;
        }
    }
}
