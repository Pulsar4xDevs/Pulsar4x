using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net.Config;
using log4net;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace Pulsar4X.Helpers
{
    /// <summary>
    /// Class to manager the Loading and Saving of UI resources, Textures, Shaders, Sounds, Data Files, etc.
    /// </summary>
    class ResourceManager
    {
        public static readonly ILog logger = LogManager.GetLogger(typeof(ResourceManager));

        /// <summary>
        /// Instance of the singelton class.
        /// </summary>
        private static readonly ResourceManager m_oInstance = new ResourceManager();

        /// <summary>
        /// Get ResourceManager Instance.
        /// </summary>
        public static ResourceManager Instance
        {
            get
            {
                return m_oInstance;
            }
        }


        /// <summary>
        /// A Structure used to store data about a texture.
        /// </summary>
        class TextureData
        {
            public uint m_uiUseCount = 0;
            public uint m_uiTextureID = 0;
            public string m_szTextureFile = "null";
        }

        Dictionary<string, TextureData> m_dicTextures = new Dictionary<string, TextureData>();

        /// <summary>
        /// Default Constructor.
        /// </summary>
        private ResourceManager()
        {
            
        }

        public uint LoadTexture(string a_szTextureFile)
        {
            // first Check if we have already loaded the file:
            TextureData oTexture;
            bool test = m_dicTextures.TryGetValue(a_szTextureFile, out oTexture);
            if (test)
            {
                oTexture.m_uiUseCount++;
                return oTexture.m_uiTextureID;
            }

            // Second check if the file exists:
            if (!System.IO.File.Exists(a_szTextureFile))
            {
                logger.Error("Could not find texture file: " + a_szTextureFile);
                return 0; // retun 0 if invalid file.
            }

            // looks like the file is valid and we have not loaded it before
            // Create New Texture Data:
            TextureData oNewTexture = new TextureData();
            oNewTexture.m_szTextureFile = a_szTextureFile;
            oNewTexture.m_uiUseCount = 1;

            // load the file into a bitmap
            System.Drawing.Bitmap oTextureBitmap = new System.Drawing.Bitmap(a_szTextureFile);

            // Get the data out of the bit map
            System.Drawing.Imaging.BitmapData oRawTextureData = oTextureBitmap.LockBits(new Rectangle(0, 0, oTextureBitmap.Width, oTextureBitmap.Height),
                                                                                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                                                                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

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
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, oTextureBitmap.Width, oTextureBitmap.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, oRawTextureData.Scan0);

            logger.Info("OpenGL Loading Texture " + a_szTextureFile + ": " + GL.GetError().ToString());

            // Now that we have provided the data to OpenGL we can free the texture from system Ram.
            oTextureBitmap.UnlockBits(oRawTextureData);

            // add our new texture to the dic:
            m_dicTextures.Add(a_szTextureFile, oNewTexture);

            return oNewTexture.m_uiTextureID;
        }


        /// <summary>
        /// Unloads the Specified Texture if it is no longer in use.
        /// </summary>
        /// <param name="a_uiTextureID">The Texture to be unloaded.</param>
        public void UnloadTexture(uint a_uiTextureID)
        {
            // find the texture data.
            //int oTextureIndex = m_lLoadedTextures.FindIndex(
            //    delegate(TextureData oTextureData)
            //    {
            //        return oTextureData.m_uiTextureID == a_uiTextureID;
            //    }
            //);
            //if (oTextureIndex < 0)
            //{
            //    // then there is a valid Texture for this ID:
            //    TextureData oTexture = m_lLoadedTextures.ElementAt(oTextureIndex);
            //    oTexture.m_iUseCount--;             // Decrease count of how oftern it has been used,

            //    // test to see if it is no longer used:
            //    if (oTexture.m_iUseCount <= 0)
            //    {
            //        GL.DeleteTexture(oTexture.m_uiTextureID);   // Delete the texture from OpenGL.

            //        m_lLoadedTextures.RemoveAt(oTextureIndex); // delete the data from the list.
            //    }
            //}
        }
    }
}
