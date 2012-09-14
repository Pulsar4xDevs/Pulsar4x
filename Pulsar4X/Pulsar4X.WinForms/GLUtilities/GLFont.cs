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
                m_szText = value;
            } 
        }


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
            //"./Resources/Fonts/CooperBlackFont.xml"
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
