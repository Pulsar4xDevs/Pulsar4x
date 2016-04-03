using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Xml;

namespace Pulsar4X.CrossPlatformUI.GLUtilities
{
    /// <summary>
    /// Class to manager the Loading and Saving of UI resources, Textures, Shaders, Sounds, Data Files, etc.
    /// </summary>
    public class ResourceManager
    {

        private static int DEFAULT_STRING_TEXTURE_WIDTH_PER_CHAR = 128;
        private static int DEFAULT_STRING_TEXTURE_HEIGHT_PER_CHAR = 128;
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


        /// <summary> The dic of Loaded textures </summary>
        Dictionary<string, TextureData> m_dicTextures = new Dictionary<string, TextureData>();


        /// <summary>   
        /// A Structure used to store data about a OpenGL Font. 
        /// </summary>
        public class GLFontData
        {
            public struct UVCoords
            {
                public Vector2 m_v2UVMin;
                public Vector2 m_v2UVMax;
            }

            /// <summary> The character map </summary>
            public Dictionary<char, UVCoords> m_dicCharMap = new Dictionary<char, UVCoords>();

            public string m_szDataFile = "null";
            public uint m_uiUseCount = 0;
            public uint m_uiTextureID = 0;

            public GLFontData() { }
        }


        /// <summary> The dic of Loaded GLFonts </summary>
        Dictionary<string, GLFontData> m_dicGLFonts = new Dictionary<string, GLFontData>();


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
            if (m_dicTextures.TryGetValue(a_szTextureFile, out oTexture))
            {
                oTexture.m_uiUseCount++;
                return oTexture.m_uiTextureID;
            }

            // Second check if the file exists:
            if (!System.IO.File.Exists(a_szTextureFile))
            {
#if LOG4NET_ENABLED
                logger.Error("Could not find texture file: " + a_szTextureFile);
#endif
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
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);

            // Load data by telling OpenGL to build mipmaps out of bitmap data
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, oTextureBitmap.Width, oTextureBitmap.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, oRawTextureData.Scan0);

#if LOG4NET_ENABLED
            logger.Info("OpenGL Loading Texture " + a_szTextureFile + ": " + GL.GetError().ToString());
#endif

            // Now that we have provided the data to OpenGL we can free the texture from system Ram.
            oTextureBitmap.UnlockBits(oRawTextureData);

            // add our new texture to the dic:
            m_dicTextures.Add(a_szTextureFile, oNewTexture);

            return oNewTexture.m_uiTextureID;
        }

        [Obsolete("GenStringTexture is Obsolete, use GLFont class instead")]
        public uint GenStringTexture(string a_szString, out Vector2 a_v2Size)
        {
            a_v2Size = Vector2.Zero;

            // first Check if the string is valid:
            if (a_szString == null)
            {
                return 0;
            }

            // next we see if this string has been gend already:
            TextureData oTexture;
            if (m_dicTextures.TryGetValue(a_szString, out oTexture))
            {
                oTexture.m_uiUseCount++;
                return oTexture.m_uiTextureID;
            }

            // looks like the string is valid and we have not gend it before
            // Create New Texture Data:
            TextureData oNewTexture = new TextureData();
            oNewTexture.m_szTextureFile = a_szString;
            oNewTexture.m_uiUseCount = 1;

            // create working Vars:
            a_v2Size.X = a_szString.Length * DEFAULT_STRING_TEXTURE_WIDTH_PER_CHAR; // Calcs the texture width based on no of chars.
            a_v2Size.Y = DEFAULT_STRING_TEXTURE_HEIGHT_PER_CHAR;
            Bitmap oStringBitmap = new Bitmap((int)a_v2Size.X, (int)a_v2Size.Y);

            // render using System.Drawing:
            using (Graphics gfx = Graphics.FromImage(oStringBitmap))
            {
                gfx.Clear(Color.Transparent);
                Font oFont = new Font(new FontFamily("Arial"), 128.0f, FontStyle.Regular, GraphicsUnit.Pixel);
                SolidBrush oBrush = new SolidBrush(Color.White);
                gfx.DrawString(a_szString, oFont, oBrush, 0, 0);

            }

            // lock bits for editing:
            System.Drawing.Imaging.BitmapData oBitmapData = oStringBitmap.LockBits(new Rectangle(0, 0, oStringBitmap.Width, oStringBitmap.Height),
                                                                                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                                                                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);


            // Generate Texture Handle
            GL.GenTextures(1, out oNewTexture.m_uiTextureID);
            // Tell openGL that this is a 2d texture:
            GL.BindTexture(TextureTarget.Texture2D, oNewTexture.m_uiTextureID);

            // Configure Text Paramaters:
            //GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)TextureEnvMode.Modulate);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Nearest);

            // Load data by telling OpenGL to build mipmaps out of bitmap data
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, oStringBitmap.Width, oStringBitmap.Height,
                          0, PixelFormat.Bgra, PixelType.UnsignedByte, oBitmapData.Scan0);

            // unlock bitmap.
            oStringBitmap.UnlockBits(oBitmapData);

            // add our new texture to the dic:
            m_dicTextures.Add(a_szString, oNewTexture);

            // return the texture id.
            return oNewTexture.m_uiTextureID;
        }

        public GLFontData LoadGLFont(string a_szFontDataFile)
        {
            // safty checks:
            if (a_szFontDataFile == null)
            {
                return null;
            }
            else if (a_szFontDataFile == "")
            {
                return null;
            }

            // next we see if this string has been gend already:
            GLFontData oFontData;
            if (m_dicGLFonts.TryGetValue(a_szFontDataFile, out oFontData))
            {
                oFontData.m_uiUseCount++;
                return oFontData;
            }
            oFontData = new GLFontData();
            
            // We have not loaded the font before so, set up new one:
            oFontData.m_szDataFile = a_szFontDataFile;      // Set Data File path.
            oFontData.m_uiUseCount = 1;

            // Create some Working Vars and Buffers:
            string szTextureFile = "";
            string szBuffer;
            char cBuffer = ' ';
            
            // first load in XML file.
            XmlTextReader oXMLReader = new XmlTextReader(a_szFontDataFile);

            try
            {
                // Get Texture path:
                if (oXMLReader.ReadToNextSibling("Font")) // works on windows.
                {
                    szTextureFile = oXMLReader.GetAttribute("texture");
                }
                else if (oXMLReader.ReadToDescendant("Font")) // works on linux!
                {
                    szTextureFile = oXMLReader.GetAttribute("texture");
                }
                else
                {
#if LOG4NET_ENABLED
                    logger.Error("Couldn't find texture path for " + a_szFontDataFile);
#endif
                }

                // Move to first Charecter element
                oXMLReader.ReadToDescendant("Character");

                // Loop through all Char elements and get out UV coords for each charcter, save them to the Dic.
                do
                {
                    /// <summary>
                    /// All of the System.Globalization stuff is due ot the fact that the default float.TryParse behaves differently on different computers. thanks microsoft.
                    /// </summary>

                    GLFontData.UVCoords oUVCoords = new GLFontData.UVCoords();

                    System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");

                    float minX = 0.0f, minY = 0.0f, maxX = 0.0f, maxY = 0.0f;

                    szBuffer = oXMLReader.GetAttribute("Umin");
                    bool r1 = float.TryParse(szBuffer, System.Globalization.NumberStyles.AllowDecimalPoint, culture, out minX);

                    //bool r1 = float.TryParse(szBuffer, out minX);

                    szBuffer = oXMLReader.GetAttribute("Umax");
                    bool r2 = float.TryParse(szBuffer, System.Globalization.NumberStyles.AllowDecimalPoint, culture, out maxX);

                    //bool r2 = float.TryParse(szBuffer, out maxX);


                    szBuffer = oXMLReader.GetAttribute("Vmin");
                    bool r3 = float.TryParse(szBuffer, System.Globalization.NumberStyles.AllowDecimalPoint, culture, out minY);

                    //bool r3 = float.TryParse(szBuffer, out minY);


                    szBuffer = oXMLReader.GetAttribute("Vmax");
                    bool r4 = float.TryParse(szBuffer, System.Globalization.NumberStyles.AllowDecimalPoint, culture, out maxY);

                    //bool r4 = float.TryParse(szBuffer, out maxY);

                    oUVCoords.m_v2UVMin.X = minX;
                    oUVCoords.m_v2UVMax.X = maxX;
                    oUVCoords.m_v2UVMin.Y = minY;
                    oUVCoords.m_v2UVMax.Y = maxY;


                    szBuffer = oXMLReader.GetAttribute("Char");
                    foreach (char c in szBuffer)
                    {
                        cBuffer = c;
                    }
                    oFontData.m_dicCharMap.Add(cBuffer, oUVCoords);

                    if (r1 == false || r2 == false || r3 == false || r4 == false)
                    {
#if LOG4NET_ENABLED
                        logger.Info("ResourceManager.cs Char: " + szBuffer + " Coordinates: " + oUVCoords.m_v2UVMin.X + "/" + oUVCoords.m_v2UVMin.Y + "," + oUVCoords.m_v2UVMax.X + "/" + oUVCoords.m_v2UVMax.Y +
                                    "|" + r1 + " " + r2 + " " + r3 + " " + r4);
#endif
                    }

                } while (oXMLReader.ReadToNextSibling("Character"));  // Move to Next Charcter Element

#if LOG4NET_ENABLED
                logger.Info("Loaded GLFont Data File " + a_szFontDataFile);
#endif
            }
            catch (XmlException e)
            {
                // XML Error occured, catch and log.
#if LOG4NET_ENABLED
                logger.Error("Error: faild to load Font Data file " + a_szFontDataFile);
                logger.Error("Font Exception: " + e.Message);
                logger.Info("You May have an unsupported Charcter in thoe font data file, inclundg <, >, &");
#endif
            }
            finally
            {
                // Close the XMl file.
                oXMLReader.Close();
            }

            // load font texture.
            oFontData.m_uiTextureID = ResourceManager.Instance.LoadTexture(szTextureFile);

            // Add to list of loaded fonts:
            m_dicGLFonts.Add(oFontData.m_szDataFile, oFontData);

            return oFontData;
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

        public void UnloadGLFont(string a_szFontDataFile)
        {
        }
    }
}
