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
    /// <summary>
    /// A class used for constructing and rendering an OpenGL primative (e.g. Quad, triangle, etc.).
    /// It includes the ability to apply a texture or color to the primative.
    /// </summary>
    class GLPrimitive
    {
        // Note: for AU/KM conversions: Constants.Units.KM_PER_AU

        /// <summary>
        /// Array of verticies that make up this primative.
        /// </summary>
        private GLVertex[] m_aoVerticies;

        /// <summary>
        /// An array of Indexes into the m_aoVerticies array. this tells the GPU in which order to draw the vertexes.
        /// this also allows us to reuse verticies, saving on the amount if memory we need to transfer to the GPU.
        /// </summary>
        public ushort[] m_auiIndicies;

        /// <summary>
        /// Size of this Object in x and y.
        /// </summary>
        private OpenTK.Vector2 m_v2Size;

        /// <summary>
        /// Handle to the Vertex array that will be passed to OpenGL/GPU.
        /// </summary>
        public uint m_uiVextexArrayHandle;

        /// <summary>
        /// Handle to the Vertex Buffer, i.e. the actual Vertex Data being sent to the GPU. This is part of the VertexArray.
        /// </summary>
        private uint m_uiVertexBufferHandle;

        /// <summary>
        /// Handle to the Index Buffer, it is used to specify which Verticies in the Vertex Buiffer get written int what order. 
        /// It is part of the VertexArray.
        /// </summary>
        private uint m_uiIndexBufferHandle;

        /// <summary>
        /// Translates this Model Relative to the rest of the world.
        /// </summary>
        public OpenTK.Matrix4 m_m4ModelMatrix = new Matrix4();

        /// <summary>
        /// OpenGL texture ID.
        /// </summary>
        public uint m_uiTextureID;

        private GLShader m_oShaderProgram;

        /// <summary>
        /// Creates a generic "quad" Primative!!
        /// </summary>
        public GLPrimitive()
        {
            //setup our quads vertcies:
            m_v2Size.X = 128;
            m_v2Size.Y = 128;

            m_aoVerticies = new GLVertex[4];
            m_aoVerticies[0].m_v3Position.X = -0.5f * m_v2Size.X;
            m_aoVerticies[0].m_v3Position.Y = -0.5f * m_v2Size.Y;
            m_aoVerticies[0].m_v3Position.Z = -1.0f;
            m_aoVerticies[0].m_v4Color.X = (OpenTK.Half)2048;
            m_aoVerticies[0].m_v2UV.X = 0.0f;
            m_aoVerticies[0].m_v2UV.Y = 1.0f;

            m_aoVerticies[1].m_v3Position.X = 0.5f * m_v2Size.X;
            m_aoVerticies[1].m_v3Position.Y = -0.5f * m_v2Size.Y;
            m_aoVerticies[1].m_v3Position.Z = -1.0f;
            m_aoVerticies[1].m_v4Color.Y = (OpenTK.Half)2048;
            m_aoVerticies[1].m_v2UV.X = 1.0f;
            m_aoVerticies[1].m_v2UV.Y = 1.0f;

            m_aoVerticies[2].m_v3Position.X = -0.5f * m_v2Size.X;
            m_aoVerticies[2].m_v3Position.Y = 0.5f * m_v2Size.Y;
            m_aoVerticies[2].m_v3Position.Z = -1.0f;
            m_aoVerticies[2].m_v4Color.Z = (OpenTK.Half)2048;
            m_aoVerticies[2].m_v2UV.X = 0.0f;
            m_aoVerticies[2].m_v2UV.Y = 0.0f;

            m_aoVerticies[3].m_v3Position.X = 0.5f * m_v2Size.X;
            m_aoVerticies[3].m_v3Position.Y = 0.5f * m_v2Size.Y;
            m_aoVerticies[3].m_v3Position.Z = -1.0f;
            m_aoVerticies[3].m_v4Color.W = (OpenTK.Half)2048;
            m_aoVerticies[3].m_v2UV.X = 1.0f;
            m_aoVerticies[3].m_v2UV.Y = 0.0f;

            m_auiIndicies = new ushort[6];
            m_auiIndicies[0] = 0;
            m_auiIndicies[1] = 1;
            m_auiIndicies[2] = 2;
            m_auiIndicies[3] = 2;
            m_auiIndicies[4] = 1;
            m_auiIndicies[5] = 3;

            //m_m4ModelView = new Matrix4d();
            m_m4ModelMatrix = Matrix4.Identity;
            //m_m4ModelView = Matrix4.Scale(1);
            m_m4ModelMatrix = Matrix4.CreateTranslation(new Vector3(400, 400, -1));

            // Load Our Texture
            m_uiTextureID = OpenTKUtilities.LoadTexture("test.png");

            // Create our shader:
            m_oShaderProgram = new GLShader();

            // tell Opgl about our VBOs:
            GL.GenVertexArrays(1, out m_uiVextexArrayHandle);               // Generate Our Vertex Array and get the handle to it.
            GL.BindVertexArray(m_uiVextexArrayHandle);                      // Lets OpenGL that this is the current "active" vertex array.
            #if DEBUG
                Program.logger.Info("OpenGL Generate VAO: " + GL.GetError().ToString());
            #endif

            GL.GenBuffers(1, out m_uiVertexBufferHandle);                   // Generate our Vertex Buffer Object and get the handle to it.
            Program.logger.Info("OpenGL Gen m_uiVertexBufferHandle Code: " + GL.GetError().ToString());
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_uiVertexBufferHandle);// Lets Open GL know that this is the current active buffer object.
            Program.logger.Info("OpenGL Gen m_uiVertexBufferHandle Code: " + GL.GetError().ToString());
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


        public void SetRotation(float a_fRot)
        {
            m_m4ModelMatrix = m_m4ModelMatrix * Matrix4.CreateRotationZ(a_fRot);
        }

        public void UpdatePos(int x, int y)
        {
            m_m4ModelMatrix = m_m4ModelMatrix * Matrix4.CreateTranslation(x, y, 0);
        }


        public void Render(ref Matrix4 a_m4Projection, ref Matrix4 a_m4View)
        {
            //GL.EnableClientState(ArrayCap.VertexArray);
           // GL.EnableClientState(ArrayCap.TextureCoordArray);
            //GL.EnableClientState(ArrayCap.IndexArray);
            GL.BindVertexArray(m_uiVextexArrayHandle);
            //GL.BindBuffer(BufferTarget.ArrayBuffer, m_uiVertexBufferHandle);
            m_oShaderProgram.StartUsing(ref a_m4Projection, ref a_m4View, ref m_m4ModelMatrix);
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, m_uiTextureID);
            GL.DrawRangeElements(BeginMode.Triangles, 0, 3, m_auiIndicies.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);
            //GL.DrawElements(BeginMode.Triangles, m_auiIndicies.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);
        }
    }
}
