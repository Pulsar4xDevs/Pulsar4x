using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif
namespace Pulsar4X.ViewModel.GLUtilities
{

    /// <summary> 
    /// A Font for use in OpenGL, will Render is current "Text".
    /// </summary>
    public class GLFont
    {
#if LOG4NET_ENABLED
        /// <summary>
        /// GLFONT Logger:
        /// </summary>
        public static readonly ILog logger = LogManager.GetLogger(typeof(GLFont));
#endif

        /// <summary> The text to render </summary>
        private string m_szText;

        /// <summary>   
        /// Gets or sets the text to render. 
        /// </summary>
        public string Text
        {
            get
            {
                return m_szText;
            }
            set
            {
                m_szText = value;
                UpdateUVCoords();
            }
        }

        private Vector3 m_v3Position;

        public Vector3 Position
        {
            get
            {
                return m_v3Position;
            }
            set
            {
                m_v3Position = value;
                UpdatePositionAndSize();
            }
        }

        private Vector2 m_v2Size;

        public Vector2 Size
        {
            get
            {
                return m_v2Size;
            }
            set
            {
                m_v2Size = value;
                UpdatePositionAndSize();
            }
        }

        /// <summary>
        /// The Maximum number of Chars that the GLFont Class will Print
        /// </summary>
        private const uint c_uiMaxNumberOfChars = 40;

        /// <summary>
        /// List of the quads used for drawing the charcters.
        /// </summary>
        List<GLQuad> m_lQuads = new List<GLQuad>();

        /// <summary> Information describing the font </summary>
        Helpers.ResourceManager.GLFontData m_oFontData;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        /// <param name="a_oEffect"> The shader program. </param>
        /// <param name="a_v3Pos">          The position of the first character. </param>
        /// <param name="a_v2Size">         Size of the first character. </param>
        /// <param name="a_oColor">         The color. </param>
        /// <param name="a_szFontDataFile"> (optional) the font data file. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public GLFont(GLEffect a_oEffect, Vector3 a_v3Pos, Vector2 a_v2Size, Color4 a_oColor, string a_szFontDataFile, string a_szText = "")
        {
            // load in data:
            m_oFontData = Helpers.ResourceManager.Instance.LoadGLFont(a_szFontDataFile);

            m_v3Position = a_v3Pos;
            m_v2Size = a_v2Size;
            m_szText = a_szText;

            // Creat some working vars:
            Vector3 v3CharPos = new Vector3();

            // create quads and position them accordingly:
            for (uint i = 0; i < c_uiMaxNumberOfChars; ++i)
            {
                // Create offseted position:
                v3CharPos.X = a_v3Pos.X + i * a_v2Size.X;
                v3CharPos.Y = a_v3Pos.Y;
                // Create Quad and set its texture:
                GLQuad oQuad = new GLQuad(a_oEffect, v3CharPos, a_v2Size, a_oColor);
                oQuad.TextureID = m_oFontData.m_uiTextureID;

                // add quad to the list:
                m_lQuads.Add(oQuad);
            }

            UpdateUVCoords();
        }

        public void Render(ref Matrix4 a_m4Projection, ref Matrix4 a_m4View)
        {
            if (m_szText == null)
            {
                // Do nothing if we have no valid text.
                return;
            }

            int iCharsToDraw = m_szText.Length;
            if (iCharsToDraw > c_uiMaxNumberOfChars)
            {
                iCharsToDraw = (int)c_uiMaxNumberOfChars;
            }

            for (int i = 0; i < iCharsToDraw; ++i)
            {
                m_lQuads[i].Render(ref a_m4Projection, ref a_m4View);
            }
        }

        public void Render()
        {
            if (m_szText == null)
            {
                // Do nothing if we have no valid text.
                return;
            }

            int iCharsToDraw = m_szText.Length;
            if (iCharsToDraw > c_uiMaxNumberOfChars)
            {
                iCharsToDraw = (int)c_uiMaxNumberOfChars;
            }

            for (int i = 0; i < iCharsToDraw; ++i)
            {
                m_lQuads[i].Render();
            }
        }

        private void UpdatePositionAndSize()
        {
            Vector3 v3CharPos = new Vector3();
            int i = 0;

            foreach (GLQuad oQuad in m_lQuads)
            {
                ++i;
                oQuad.Size = m_v2Size;
                v3CharPos.X = m_v3Position.X + i * m_v2Size.X;
                v3CharPos.Y = m_v3Position.Y;
                oQuad.Position = v3CharPos;
            }
        }

        private void UpdateUVCoords()
        {
            if (m_szText == null)
            {
                // Do nothing if we have no valid text.
                return;
            }

            char[] caText = m_szText.ToCharArray();
            int iCharsToDraw = caText.Length;
            if (iCharsToDraw > c_uiMaxNumberOfChars)
            {
                iCharsToDraw = (int)c_uiMaxNumberOfChars;
            }

            Helpers.ResourceManager.GLFontData.UVCoords oGLUVCoords;

            for (int i = 0; i < iCharsToDraw; ++i)
            {
                if (m_oFontData.m_dicCharMap.TryGetValue(caText[i], out oGLUVCoords))
                {
                    // noew set UV coords as required:
                    m_lQuads[i].Verticies[0].m_v2UV.X = oGLUVCoords.m_v2UVMin.X;    // 0, 1
                    m_lQuads[i].Verticies[0].m_v2UV.Y = oGLUVCoords.m_v2UVMax.Y;    // 0, 1
                    m_lQuads[i].Verticies[1].m_v2UV = oGLUVCoords.m_v2UVMax;    // 1, 1
                    m_lQuads[i].Verticies[2].m_v2UV = oGLUVCoords.m_v2UVMin;    // 0, 0
                    m_lQuads[i].Verticies[3].m_v2UV.X = oGLUVCoords.m_v2UVMax.X;    // 1 , 0
                    m_lQuads[i].Verticies[3].m_v2UV.Y = oGLUVCoords.m_v2UVMin.Y;    // 1 , 0
                    m_lQuads[i].UpdateVBOs();
                }
                else
                {
                    m_oFontData.m_dicCharMap.TryGetValue(' ', out oGLUVCoords); // get value for space!
                    m_lQuads[i].Verticies[0].m_v2UV.X = oGLUVCoords.m_v2UVMin.X;    // 0, 1
                    m_lQuads[i].Verticies[0].m_v2UV.Y = oGLUVCoords.m_v2UVMax.Y;    // 0, 1
                    m_lQuads[i].Verticies[1].m_v2UV = oGLUVCoords.m_v2UVMax;    // 1, 1
                    m_lQuads[i].Verticies[2].m_v2UV = oGLUVCoords.m_v2UVMin;    // 0, 0
                    m_lQuads[i].Verticies[3].m_v2UV.X = oGLUVCoords.m_v2UVMax.X;    // 1 , 0
                    m_lQuads[i].Verticies[3].m_v2UV.Y = oGLUVCoords.m_v2UVMin.Y;    // 1 , 0
                    m_lQuads[i].UpdateVBOs();
                }
            }
        }
    }
}
