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
    /// A Quad Primitive
    /// </summary>
    public class GLQuad : GLPrimitive
    {
        /// <summary>   Constructor. </summary>
        /// <param name="a_oEffect"> The shader program to use. </param>
        /// <param name="a_v2Pos">          The position of the quad (centre). </param>
        /// <param name="a_v2Size">         Size of the Quad. </param>
        /// <param name="a_oColor">         The color to apply to the quad. </param>
        /// <param name="a_szTexture">      (optional) the texture file. </param>
        public GLQuad(GLEffect a_oEffect, Vector3 a_v3Pos, Vector2 a_v2Size, Color4 a_oColor, string a_szTexture = "")
            : base()
        {
            // Setup Member Vars:
            m_oColor = a_oColor;
            m_v2Size = a_v2Size;
            m_v3Position = a_v3Pos;
            m_oEffect = a_oEffect;
            m_m4ModelMatrix = Matrix4.Identity;
            ///< @todo make quads scale better, so it can scale on X or Y...
            m_m4ModelMatrix = Matrix4.CreateScale(m_v2Size.X) * Matrix4.CreateTranslation(a_v3Pos);  // x and y should be the same, so scale by X

            if (a_szTexture != "")
            {
                // We can assuem we have been provided with a texture to load:
                //m_uiTextureID = Helpers.ResourceManager.Instance.LoadTexture(a_szTexture);
            }
            else
            {
                m_uiTextureID = 0; // set texture to none!
            }

            // calculate the Y scale to X, as we are using X for our scale in our matrix.
            float fYScale = 1;
            if (a_v2Size.X != 0)
            {
                fYScale = a_v2Size.Y / a_v2Size.X;
            }

            //setup our quads vertcies:
            m_aoVerticies = new GLVertex[4];
            m_aoVerticies[0] = new GLVertex(new Vector4(-0.5f, -0.5f * fYScale, 0.0f, 1.0f), a_oColor, new Vector2(0.0f, 1.0f));
            m_aoVerticies[1] = new GLVertex(new Vector4(0.5f, -0.5f * fYScale, 0.0f, 1.0f), a_oColor, new Vector2(1.0f, 1.0f));
            m_aoVerticies[2] = new GLVertex(new Vector4(-0.5f, 0.5f * fYScale, 0.0f, 1.0f), a_oColor, new Vector2(0.0f, 0.0f));
            m_aoVerticies[3] = new GLVertex(new Vector4(0.5f, 0.5f * fYScale, 0.0f, 1.0f), a_oColor, new Vector2(1.0f, 0.0f));

            // Setup Draw order. *this apears to have no effect under GL2.X*
            m_auiIndicies = new ushort[4];
            m_auiIndicies[0] = 0;
            m_auiIndicies[1] = 1;
            m_auiIndicies[2] = 2;
            m_auiIndicies[3] = 3;


            // tell OpenGL about our VBOs:
            GL.GenVertexArrays(1, out m_uiVextexArrayHandle);               // Generate Our Vertex Array and get the handle to it.
            GL.BindVertexArray(m_uiVextexArrayHandle);                      // Lets OpenGL that this is the current "active" vertex array.
            //#if DEBUG
            //    logger.Info("OpenGL Generate VAO: " + GL.GetError().ToString());
            //#endif

            GL.GenBuffers(1, out m_uiVertexBufferHandle);                   // Generate our Vertex Buffer Object and get the handle to it.
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_uiVertexBufferHandle);// Lets Open GL know that this is the current active buffer object.
            GL.BufferData<GLVertex>(BufferTarget.ArrayBuffer, new IntPtr(m_aoVerticies.Length * GLVertex.SizeInBytes()), m_aoVerticies, BufferUsageHint.StaticDraw); // tells OpenGL about the structure of the data.
            //#if DEBUG
            //    logger.Info("OpenGL Generate VBO: " + GL.GetError().ToString());
            //#endif

            GL.GenBuffers(1, out m_uiIndexBufferHandle);                    //Generate Our index Buffer and get handle to it.
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, m_uiIndexBufferHandle); // Lets Open GL know that this is the current active buffer object.
            GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(m_auiIndicies.Length * sizeof(ushort)), m_auiIndicies, BufferUsageHint.StaticDraw); // Tells OpenGL how the data is structured.
            //#if DEBUG
            //        logger.Info("OpenGL Generate EBO: " + GL.GetError().ToString());
            //#endif

            GL.BindBuffer(BufferTarget.ArrayBuffer, m_uiVertexBufferHandle);    // Switch back to our Buffer Object as the current buffer.
            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, GLVertex.SizeInBytes(), 0);  // Tells OpenGL about the first three doubles in the vbo, i.e the position of the vertex.
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, true, GLVertex.SizeInBytes(), Vector4.SizeInBytes); // tells OpenGL about the 4 half floats used to repesent color.
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, GLVertex.SizeInBytes(), (Vector4.SizeInBytes + Vector4.SizeInBytes)); // tells OpenGL about the 2 floats in the vertgexc used to repesent UV coords.
            //#if DEBUG
            //        logger.Info("OpenGL Create Vertes Attribute Pointers: " + GL.GetError().ToString());
            //#endif

            // Turn on the Vertex Attribs:
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

            // #if DEBUG
            //    logger.Info("OpenGL Create Quad Primitive: " + GL.GetError().ToString());
            //#endif
        }

        public override void UpdateVBOs()
        {
            GL.BindVertexArray(m_uiVextexArrayHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_uiVertexBufferHandle);// Lets Open GL know that this is the current active buffer object.
            GL.BufferData<GLVertex>(BufferTarget.ArrayBuffer, new IntPtr(m_aoVerticies.Length * GLVertex.SizeInBytes()), m_aoVerticies, BufferUsageHint.StaticDraw); // tells OpenGL about the structure of the data.
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, m_uiIndexBufferHandle); // Lets Open GL know that this is the current active buffer object.
            GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(m_auiIndicies.Length * sizeof(ushort)), m_auiIndicies, BufferUsageHint.StaticDraw); // Tells OpenGL how the data is structured.
        }

        public override void Render(ref Matrix4 a_m4Projection, ref Matrix4 a_m4View)
        {
            GL.BindVertexArray(m_uiVextexArrayHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, m_uiIndexBufferHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_uiVertexBufferHandle);

            m_oEffect.StartUsing(ref m_m4ModelMatrix);
            m_oEffect.StartUsing(ref a_m4Projection);
            m_oEffect.StartUsing(ref a_m4View);

            GL.BindTexture(TextureTarget.Texture2D, m_uiTextureID);

            GL.DrawElements(PrimitiveType.TriangleStrip, m_auiIndicies.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);
        }

        public override void Render(ref Matrix4 a_m4View)
        {
            GL.BindVertexArray(m_uiVextexArrayHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, m_uiIndexBufferHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_uiVertexBufferHandle);

            m_oEffect.StartUsing(ref m_m4ModelMatrix);
            m_oEffect.StartUsing(ref a_m4View);

            GL.BindTexture(TextureTarget.Texture2D, m_uiTextureID);

            GL.DrawElements(PrimitiveType.TriangleStrip, m_auiIndicies.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);
        }

        public override void Render()
        {
            GL.BindVertexArray(m_uiVextexArrayHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, m_uiIndexBufferHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_uiVertexBufferHandle);

            // recreate our matrix based on size and position.
            this.RecalculateModelMatrix();

            m_oEffect.StartUsing(ref m_m4ModelMatrix);

            GL.BindTexture(TextureTarget.Texture2D, m_uiTextureID);

            GL.DrawElements(PrimitiveType.TriangleStrip, m_auiIndicies.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);
        }
    }
}
