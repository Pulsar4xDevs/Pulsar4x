using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics;
using Pulsar4X.WinForms;
using Pulsar4X.WinForms.Controls;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using log4net.Config;
using log4net;

namespace Pulsar4X.WinForms.GLUtilities
{
    class GLCircle : GLPrimitive
    {


        /// <summary>
        /// 
        /// </summary>
        /// <param name="a_oShaderProgram"></param>
        /// <param name="a_v3Pos"></param>
        /// <param name="a_fRadus"></param>
        /// <param name="a_oColor"></param>
        public GLCircle(GLShader a_oShaderProgram, Vector3 a_v3Pos, float a_fRadus, System.Drawing.Color a_oColor)
            : base()
        {
            // calculate the number of verts for the circle:
            int iNumOfVerts = (int)(a_fRadus * OpenTK.MathHelper.PiOver3); 
            // create some working vars:
            double dX, dY, dAngle;

            //create our Vertex and index arrays:
            m_aoVerticies = new GLVertex[iNumOfVerts];
            m_auiIndicies = new ushort[iNumOfVerts + 1]; // make this one longer so it can loop back around to the begining!!

            for (int i = 0; i < iNumOfVerts; ++i)
            {
                dAngle = i * (MathHelper.TwoPi / iNumOfVerts);
                dX = Math.Cos(dAngle) * a_fRadus;
                dY = Math.Sin(dAngle) * a_fRadus;

                m_aoVerticies[i].m_v3Position.X = dX;
                m_aoVerticies[i].m_v3Position.Y = dY;
                m_aoVerticies[i].m_v3Position.Z = 0;
                m_aoVerticies[i].SetColor(a_oColor);
                m_auiIndicies[i] = (ushort)i;
            }

            // set last index:
            m_auiIndicies[iNumOfVerts] = 0;

            // Setup Matrix:
            m_m4ModelMatrix = Matrix4.CreateTranslation(a_v3Pos);

            // Set our shader program:
            m_oShaderProgram = a_oShaderProgram;
            m_uiTextureID = 0;


            // tell Opgl about our VBOs:
            GL.GenVertexArrays(1, out m_uiVextexArrayHandle);               // Generate Our Vertex Array and get the handle to it.
            GL.BindVertexArray(m_uiVextexArrayHandle);                      // Lets OpenGL that this is the current "active" vertex array.
            #if DEBUG
                Program.logger.Info("OpenGL Generate VAO: " + GL.GetError().ToString());
            #endif

            GL.GenBuffers(1, out m_uiVertexBufferHandle);                   // Generate our Vertex Buffer Object and get the handle to it.
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_uiVertexBufferHandle);// Lets Open GL know that this is the current active buffer object.
            GL.BufferData<GLVertex>(BufferTarget.ArrayBuffer, new IntPtr(m_aoVerticies.Length * GLVertex.SizeInBytes()), m_aoVerticies, BufferUsageHint.StaticDraw); // tells OpenGL about the structure of the data.
            #if DEBUG
                Program.logger.Info("OpenGL Generate VBO: " + GL.GetError().ToString());
            #endif

            GL.GenBuffers(1, out m_uiIndexBufferHandle);                    //Generate Our index Buffer and get handle to it.
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, m_uiIndexBufferHandle); // Lets Open GL know that this is the current active buffer object.
            GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(m_auiIndicies.Length * sizeof(ushort)), m_auiIndicies, BufferUsageHint.StaticDraw); // Tells OpenGL how the data is structured.
            #if DEBUG
                Program.logger.Info("OpenGL Generate EBO: " + GL.GetError().ToString());
            #endif

            GL.BindBuffer(BufferTarget.ArrayBuffer, m_uiVertexBufferHandle);    // Switch back to our Buffer Object as the current buffer.
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Double, false, GLVertex.SizeInBytes(), 0);  // Tells OpenGL about the first three doubles in the vbo, i.e the position of the vertex.
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.HalfFloat, true, GLVertex.SizeInBytes(), Vector3d.SizeInBytes); // tells OpenGL about the 4 half floats used to repesent color.
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, GLVertex.SizeInBytes(), (Vector3d.SizeInBytes + Vector4h.SizeInBytes)); // tells OpenGL about the 2 floats in the vertgexc used to repesent UV coords.
            #if DEBUG
                Program.logger.Info("OpenGL Create Vertes Attribute Pointers: " + GL.GetError().ToString());
            #endif

            // Turn on the Vertex Attribs:
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);
        }

        public override void Render(ref Matrix4 a_m4Projection, ref Matrix4 a_m4View)
        {
            //GL.EnableClientState(ArrayCap.VertexArray);
            //GL.EnableClientState(ArrayCap.TextureCoordArray);
            //GL.EnableClientState(ArrayCap.IndexArray);
            GL.BindVertexArray(m_uiVextexArrayHandle);
            //GL.BindBuffer(BufferTarget.ArrayBuffer, m_uiVertexBufferHandle);
            //m_oShaderProgram.StartUsing(ref a_m4Projection, ref a_m4View, ref m_m4ModelMatrix);
            m_oShaderProgram.StartUsing(ref m_m4ModelMatrix);

            //GL.ActiveTexture(TextureUnit.Texture0);
            //GL.BindTexture(TextureTarget.Texture2D, m_uiTextureID);
            //OpenTKUtilities.Use2DTexture(m_uiTextureID);
            //GL.DrawRangeElements(BeginMode.Triangles, 0, 3, m_auiIndicies.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);
            GL.DrawElements(BeginMode.LineStrip, m_auiIndicies.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);
        }

    }
}
