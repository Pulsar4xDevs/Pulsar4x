using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.WinForms;
using Pulsar4X.WinForms.Controls;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using log4net.Config;
using log4net;

namespace Pulsar4X.WinForms.GLUtilities
{
   
    /// <summary> 
    /// A Font for use in OpenGL, will Render is current "Text".
    /// </summary>
    class GLFont
    {
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
                UpdatePositions();
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
                UpdateSize();
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
        /// <param name="a_oShaderProgram"> The shader program. </param>
        /// <param name="a_v3Pos">          The position of the first character. </param>
        /// <param name="a_v2Size">         Size of the first character. </param>
        /// <param name="a_oColor">         The color. </param>
        /// <param name="a_szFontDataFile"> (optional) the font data file. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public GLFont(GLShader a_oShaderProgram, Vector3 a_v3Pos, Vector2 a_v2Size, System.Drawing.Color a_oColor, string a_szFontDataFile = "")
        {
            // load in data:
            m_oFontData = Helpers.ResourceManager.Instance.LoadGLFont(a_szFontDataFile);

            m_v3Position = a_v3Pos;
            m_v2Size = a_v2Size;

            // Creat some working vars:
            Vector3 v3CharPos = new Vector3();

            // create quads and position them accordingly:
            for (uint i = 0; i < c_uiMaxNumberOfChars; ++i)
            {
                // Create offseted position:
                v3CharPos.X = a_v3Pos.X + i * a_v2Size.X;
                v3CharPos.Y = a_v3Pos.Y;
                // Create Quad and set its texture:
                GLQuad oQuad = new GLQuad(a_oShaderProgram, v3CharPos, a_v2Size, a_oColor);
                oQuad.TextureID = m_oFontData.m_uiTextureID;

                // add quad to the list:
                m_lQuads.Add(oQuad);
            }
        }

        public void Render(ref Matrix4 a_m4Projection, ref Matrix4 a_m4View)
        {
            foreach (GLQuad oQuad in m_lQuads)
            {
                oQuad.Render(ref a_m4Projection, ref a_m4View);
            }
        }

        public void Render()
        {
            foreach (GLQuad oQuad in m_lQuads)
            {
                oQuad.Render();
            }
        }

        private void UpdatePositions()
        {
            Vector3 v3CharPos = new Vector3();
            int i = 0;

            foreach (GLQuad oQuad in m_lQuads)
            {
                ++i;
                v3CharPos.X = m_v3Position.X + i * m_v2Size.X;
                v3CharPos.Y = m_v3Position.Y;
                oQuad.Position = v3CharPos;
            }
        }

        private void UpdateSize()
        {
            foreach (GLQuad oQuad in m_lQuads)
            {
                oQuad.Size = m_v2Size;
            }
        }

        private void UpdateUVCoords()
        {
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
                }
            }
        }
    }
}
