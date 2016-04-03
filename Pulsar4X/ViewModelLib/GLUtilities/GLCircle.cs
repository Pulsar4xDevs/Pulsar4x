using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Pulsar4X.ViewModel;
using Pulsar4X.ViewModel.SystemView;
#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif


namespace Pulsar4X.ViewModel.GLUtilities
{

    /// <summary>   
    /// Circle Primitive.
    /// </summary>
    class GLOrbitCircle : GLPrimitive
    {
        /// <summary>   Constructor. </summary>
        /// <param name="a_oEffect"> The Shader program to use when rendering. </param>
        /// <param name="a_v3Pos">          The Position of the circle. </param>
        /// <param name="a_fRadus">         The Radius of the circle. </param>
        /// <param name="a_oColor">         The color of the circle. </param>
        /// <param name="a_szTexture">      (optional) the texture file. </param>
        public GLOrbitCircle(GLEffect a_oEffect, Vector3 a_v3Pos, float a_fRadus, Color4 a_oColor, string a_szTexture = "")
            : base()
        {
            

            // Save some stuff to member vars:
            m_v3Position = a_v3Pos;
            m_v2Size.X = a_fRadus;

            // calculate the number of verts, min is 90 for a good looking circle, max is 360 for performace reasons.
            int iNumOfVerts = (int)(a_fRadus * MathHelper.PiOver4);
            if (iNumOfVerts < 90)
            {
                iNumOfVerts = 90;
            }
            else if (iNumOfVerts > 360)
            {
                iNumOfVerts = 360;
            }

            // create some working vars:
            double dAngle;
            float fX, fY;

            //create our Vertex and index arrays:
            m_aoVerticies = new GLVertex[iNumOfVerts];
            m_auiIndicies = new ushort[iNumOfVerts + 1]; // make this one longer so it can loop back around to the begining!!

            for (int i = 0; i < iNumOfVerts; ++i)
            {
                dAngle = i * (MathHelper.TwoPi / iNumOfVerts);
                fX = (float)Math.Cos(dAngle) * 1;
                fY = (float)Math.Sin(dAngle) * 1;

                m_aoVerticies[i].m_v4Position.X = fX;
                m_aoVerticies[i].m_v4Position.Y = fY;
                m_aoVerticies[i].m_v4Position.Z = 0;
                m_aoVerticies[i].SetColor(a_oColor);
                m_auiIndicies[i] = (ushort)i;
            }

            // set last index:
            m_auiIndicies[iNumOfVerts] = 0;

            // Setup Matrix:
            m_m4ModelMatrix = Matrix4.CreateScale(a_fRadus * 2) * Matrix4.CreateTranslation(a_v3Pos);

            // Set our shader program:
            m_oEffect = a_oEffect;

            // Load texture if specified:
            if (a_szTexture != "")
            {
                // We can assuem we have been provided with a texture to load:
                //m_uiTextureID = Helpers.ResourceManager.Instance.LoadTexture(a_szTexture);
            }
            else
            {
                m_uiTextureID = 0; // set texture to none!
            }


            // tell Opgl about our VBOs:
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
            //    logger.Info("OpenGL Generate EBO: " + GL.GetError().ToString());
            //#endif

            GL.BindBuffer(BufferTarget.ArrayBuffer, m_uiVertexBufferHandle);    // Switch back to our Buffer Object as the current buffer.
            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, GLVertex.SizeInBytes(), 0);  // Tells OpenGL about the first three doubles in the vbo, i.e the position of the vertex.
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, true, GLVertex.SizeInBytes(), Vector4.SizeInBytes); // tells OpenGL about the floats used to repesent color.
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, GLVertex.SizeInBytes(), (Vector4.SizeInBytes + Vector4.SizeInBytes)); // tells OpenGL about the 2 floats in the vertgexc used to repesent UV coords.
            //#if DEBUG
            //    logger.Info("OpenGL Create Vertes Attribute Pointers: " + GL.GetError().ToString());
            //#endif

            // Turn on the Vertex Attribs:
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

            // #if DEBUG
            //     logger.Info("OpenGL Create Circle Primitive: " + GL.GetError().ToString());
            //#endif
        }

        public GLOrbitCircle(GLEffect a_oEffect, Vector3 a_v3Pos, SystemObjectRenderInfo renderInfo,  Color4 a_oColor, string a_szTexture = "")
            : base()
        {
            // Save some stuff to member vars:
            //double dKMperAUdevby10 = (Pulsar4X.Constants.Units.KM_PER_AU / 10); // we scale everthing down by 10 to avoid float buffer overflows.
            m_v3Position = a_v3Pos;
            m_v2Size.X = 1;

            // set verts to 360, looks good at any zoom.
            int iNumOfVerts = 360;
            
            //create our Vertex and index arrays:
            m_aoVerticies = new GLVertex[iNumOfVerts];
            m_auiIndicies = new ushort[iNumOfVerts + 1]; // make this one longer so it can loop back around to the begining!!

            for (int i = 0; i < iNumOfVerts; ++i)
            {
                double dAngle = i * (MathHelper.TwoPi / iNumOfVerts);

                double x, y;
                a_oOrbitEntity.Orbit.GetPosition(dAngle, out x, out y);

                m_aoVerticies[i].m_v4Position.X = (float)x;
                m_aoVerticies[i].m_v4Position.Y = (float)y;
                m_aoVerticies[i].m_v4Position.Z = 0;
                m_aoVerticies[i].SetColor(a_oColor);
                m_auiIndicies[i] = (ushort)i;
            }

            // set last index:
            m_auiIndicies[iNumOfVerts] = 0;

            // Setup Matrix:
            m_m4ModelMatrix = Matrix4.Identity * Matrix4.CreateTranslation(a_v3Pos);

            // Set our shader program:
            m_oEffect = a_oEffect;

            // Load texture if specified:
            if (a_szTexture != "")
            {
                // We can assuem we have been provided with a texture to load:
                //m_uiTextureID = Helpers.ResourceManager.Instance.LoadTexture(a_szTexture);
            }
            else
            {
                m_uiTextureID = 0; // set texture to none!
            }


            // tell Opgl about our VBOs:
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
            //    logger.Info("OpenGL Generate EBO: " + GL.GetError().ToString());
            //#endif

            GL.BindBuffer(BufferTarget.ArrayBuffer, m_uiVertexBufferHandle);    // Switch back to our Buffer Object as the current buffer.
            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, GLVertex.SizeInBytes(), 0);  // Tells OpenGL about the first three doubles in the vbo, i.e the position of the vertex.
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, true, GLVertex.SizeInBytes(), Vector4.SizeInBytes); // tells OpenGL about the 4 floats used to repesent color.
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, GLVertex.SizeInBytes(), (Vector4.SizeInBytes + Vector4.SizeInBytes)); // tells OpenGL about the 2 floats in the vertgexc used to repesent UV coords.
            //#if DEBUG
            //    logger.Info("OpenGL Create Vertes Attribute Pointers: " + GL.GetError().ToString());
            //#endif

            // Turn on the Vertex Attribs:
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

            // #if DEBUG
            //     logger.Info("OpenGL Create Circle Primitive: " + GL.GetError().ToString());
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

            GL.DrawElements(PrimitiveType.LineStrip, m_auiIndicies.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);
        }

        public override void Render(ref Matrix4 a_m4View)
        {
            GL.BindVertexArray(m_uiVextexArrayHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, m_uiIndexBufferHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_uiVertexBufferHandle);

            m_oEffect.StartUsing(ref m_m4ModelMatrix);
            m_oEffect.StartUsing(ref a_m4View);

            GL.BindTexture(TextureTarget.Texture2D, m_uiTextureID);

            GL.DrawElements(PrimitiveType.LineStrip, m_auiIndicies.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);
        }

        public override void Render()
        {
            GL.BindVertexArray(m_uiVextexArrayHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, m_uiIndexBufferHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_uiVertexBufferHandle);

            m_oEffect.StartUsing(ref m_m4ModelMatrix);

            GL.BindTexture(TextureTarget.Texture2D, m_uiTextureID);

            GL.DrawElements(PrimitiveType.LineStrip, m_auiIndicies.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);
        }
    }
}
