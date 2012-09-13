using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
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
    class GLFont : GLPrimitive
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

            } 
        }

        struct UVCoords
        {
            public Vector2 m_v2UVMin;
            public Vector2 m_v2UVMax;
        }

        /// <summary> The character map </summary>
        Dictionary<char, UVCoords> m_dicCharMap = new Dictionary<char, UVCoords>();

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
            string szTextureFile = "";
            string szBuffer;
            char cBuffer = ' ';

            // first load in XML file.
            XmlTextReader oXMLReader = new XmlTextReader("./Resources/Fonts/CooperBlackFont.xml");

            try
            {

                if (oXMLReader.ReadToNextSibling("Font"))
                {
                    szTextureFile = oXMLReader.GetAttribute("texture");
                }

                oXMLReader.ReadToDescendant("Character");

                do
                {
                    UVCoords oUVCoords = new UVCoords();

                    szBuffer = oXMLReader.GetAttribute("Umin");
                    float.TryParse(szBuffer, out oUVCoords.m_v2UVMin.X);

                    szBuffer = oXMLReader.GetAttribute("Vmin");
                    float.TryParse(szBuffer, out oUVCoords.m_v2UVMin.Y);

                    szBuffer = oXMLReader.GetAttribute("Umax");
                    float.TryParse(szBuffer, out oUVCoords.m_v2UVMax.X);

                    szBuffer = oXMLReader.GetAttribute("Vmax");
                    float.TryParse(szBuffer, out oUVCoords.m_v2UVMax.Y);

                    szBuffer = oXMLReader.GetAttribute("Char");
                    foreach (char c in szBuffer)
                    {
                        cBuffer = c;
                    }
                    m_dicCharMap.Add(cBuffer, oUVCoords);

                } while (oXMLReader.ReadToNextSibling("Character"));
            }
            catch
            {
                logger.Error("Error: faild to load Font Data file " + a_szFontDataFile);
            }

            // load fon texture.
            Pulsar4X.Helpers.ResourceManager.Instance.LoadTexture(szTextureFile);
        }

        public override void Render(ref Matrix4 a_m4Projection, ref Matrix4 a_m4View)
        {
            GL.BindVertexArray(m_uiVextexArrayHandle);

            m_oShaderProgram.StartUsing(ref m_m4ModelMatrix);

            OpenTKUtilities.Use2DTexture(m_uiTextureID);

            GL.DrawElements(BeginMode.TriangleStrip, m_auiIndicies.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);
        }

        public override void Render()
        {
            GL.BindVertexArray(m_uiVextexArrayHandle);

            // recreate our matrix based on size and position.
            this.RecalculateModelMatrix();

            m_oShaderProgram.StartUsing(ref m_m4ModelMatrix);

            OpenTKUtilities.Use2DTexture(m_uiTextureID);

            GL.DrawElements(BeginMode.TriangleStrip, m_auiIndicies.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);
        }
    }
}
