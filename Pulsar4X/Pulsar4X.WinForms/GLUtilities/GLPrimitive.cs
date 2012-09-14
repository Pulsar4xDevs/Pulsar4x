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
    public abstract class GLPrimitive
    {
        // Note: for AU/KM conversions: Constants.Units.KM_PER_AU
        public static readonly ILog logger = LogManager.GetLogger(typeof(GLPrimitive));

        /// <summary>
        /// Array of verticies that make up this primative.
        /// </summary>
        protected GLVertex[] m_aoVerticies;

        public GLVertex[] Verticies
        {
            get
            {
                return m_aoVerticies;
            }
        }

        /// <summary>
        /// An array of Indexes into the m_aoVerticies array. this tells the GPU in which order to draw the vertexes.
        /// this also allows us to reuse verticies, saving on the amount if memory we need to transfer to the GPU.
        /// </summary>
        protected ushort[] m_auiIndicies;

        /// <summary>
        /// Size of this Object in x and y.
        /// </summary>
        protected OpenTK.Vector2 m_v2Size;

        public OpenTK.Vector2 Size
        {
            get
            {
                return m_v2Size;
            }
            set
            {
                SetSize(value);
            }
        }


        /// <summary>
        /// The Position of the object in world coords.
        /// </summary>
        protected OpenTK.Vector3 m_v3Position;

        public OpenTK.Vector3 Position
        {
            get
            {
                return m_v3Position;
            }
            set
            {
                m_v3Position = value;
            }
        }

        /// <summary>
        /// Handle to the Vertex array that will be passed to OpenGL/GPU.
        /// </summary>
        protected uint m_uiVextexArrayHandle;

        /// <summary>
        /// Handle to the Vertex Buffer, i.e. the actual Vertex Data being sent to the GPU. This is part of the VertexArray.
        /// </summary>
        protected uint m_uiVertexBufferHandle;

        /// <summary>
        /// Handle to the Index Buffer, it is used to specify which Verticies in the Vertex Buiffer get written int what order. 
        /// It is part of the VertexArray.
        /// </summary>
        protected uint m_uiIndexBufferHandle;

        /// <summary>
        /// Translates this Model Relative to the rest of the world.
        /// </summary>
        protected OpenTK.Matrix4 m_m4ModelMatrix = new Matrix4();

        /// <summary>
        /// OpenGL texture ID.
        /// </summary>
        protected uint m_uiTextureID;

        /// <summary> Gets or sets the identifier of the texture. </summary>
        ///
        /// <value> The identifier of the texture. </value>
        public uint TextureID
        {
            get
            {
                return m_uiTextureID;
            }
            set
            {
                m_uiTextureID = value;
            }
        }

        /// <summary> 
        /// A handle to the shader program to be used by this Primitive. 
        /// </summary>
        protected GLShader m_oShaderProgram;

        /// <summary>   
        /// Default constructor. 
        /// </summary>
        public GLPrimitive()
        {
        }

        #region OldQuadTestCode
        /// <summary>
        /// Creates a generic "quad" Primative!!
        /// </summary>
        //public GLPrimitive(GLShader a_oShaderProgram)
        //{
        //    //setup our quads vertcies:
        //    m_v2Size.X = 128;
        //    m_v2Size.Y = 128;

        //    m_aoVerticies = new GLVertex[4];
        //    m_aoVerticies[0].m_v3Position.X = -0.5f * m_v2Size.X;
        //    m_aoVerticies[0].m_v3Position.Y = -0.5f * m_v2Size.Y;
        //    m_aoVerticies[0].m_v3Position.Z = -1.0f;
        //    m_aoVerticies[0].m_v4Color.Y = (OpenTK.Half)2048;
        //    m_aoVerticies[0].m_v2UV.X = 0.0f;
        //    m_aoVerticies[0].m_v2UV.Y = 1.0f;

        //    m_aoVerticies[1].m_v3Position.X = 0.5f * m_v2Size.X;
        //    m_aoVerticies[1].m_v3Position.Y = -0.5f * m_v2Size.Y;
        //    m_aoVerticies[1].m_v3Position.Z = -1.0f;
        //    m_aoVerticies[1].m_v4Color.Y = (OpenTK.Half)2048;
        //    m_aoVerticies[1].m_v2UV.X = 1.0f;
        //    m_aoVerticies[1].m_v2UV.Y = 1.0f;

        //    m_aoVerticies[2].m_v3Position.X = -0.5f * m_v2Size.X;
        //    m_aoVerticies[2].m_v3Position.Y = 0.5f * m_v2Size.Y;
        //    m_aoVerticies[2].m_v3Position.Z = -1.0f;
        //    m_aoVerticies[2].m_v4Color.Y = (OpenTK.Half)2048;
        //    m_aoVerticies[2].m_v2UV.X = 0.0f;
        //    m_aoVerticies[2].m_v2UV.Y = 0.0f;

        //    m_aoVerticies[3].m_v3Position.X = 0.5f * m_v2Size.X;
        //    m_aoVerticies[3].m_v3Position.Y = 0.5f * m_v2Size.Y;
        //    m_aoVerticies[3].m_v3Position.Z = -1.0f;
        //    m_aoVerticies[3].m_v4Color.Y = (OpenTK.Half)1024;
        //    m_aoVerticies[3].m_v2UV.X = 1.0f;
        //    m_aoVerticies[3].m_v2UV.Y = 0.0f;

        //    m_auiIndicies = new ushort[4];
        //    m_auiIndicies[0] = 0;
        //    m_auiIndicies[1] = 1;
        //    m_auiIndicies[2] = 2;
        //    m_auiIndicies[3] = 3;

        //    //m_m4ModelView = new Matrix4d();
        //    m_m4ModelMatrix = Matrix4.Identity;
        //    //m_m4ModelView = Matrix4.Scale(1);
        //    m_m4ModelMatrix = Matrix4.CreateTranslation(new Vector3(400, 400, 0));

        //    // Load Our Texture
        //    m_uiTextureID = OpenTKUtilities.LoadTexture("test.png"); ///< @todo Proper Path when resources sorted.

        //    // Create our shader:
        //    m_oShaderProgram = a_oShaderProgram;

        //    // tell Opgl about our VBOs:
        //    GL.GenVertexArrays(1, out m_uiVextexArrayHandle);               // Generate Our Vertex Array and get the handle to it.
        //    GL.BindVertexArray(m_uiVextexArrayHandle);                      // Lets OpenGL that this is the current "active" vertex array.
        //    #if DEBUG
        //        logger.Info("OpenGL Generate VAO: " + GL.GetError().ToString());
        //    #endif

        //    GL.GenBuffers(1, out m_uiVertexBufferHandle);                   // Generate our Vertex Buffer Object and get the handle to it.
        //    GL.BindBuffer(BufferTarget.ArrayBuffer, m_uiVertexBufferHandle);// Lets Open GL know that this is the current active buffer object.
        //    GL.BufferData<GLVertex>(BufferTarget.ArrayBuffer, new IntPtr(m_aoVerticies.Length * GLVertex.SizeInBytes()), m_aoVerticies, BufferUsageHint.StaticDraw); // tells OpenGL about the structure of the data.
        //    #if DEBUG
        //        logger.Info("OpenGL Generate VBO: " + GL.GetError().ToString());
        //    #endif

        //    GL.GenBuffers(1, out m_uiIndexBufferHandle);                    //Generate Our index Buffer and get handle to it.
        //    GL.BindBuffer(BufferTarget.ElementArrayBuffer, m_uiIndexBufferHandle); // Lets Open GL know that this is the current active buffer object.
        //    GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(m_auiIndicies.Length * sizeof(ushort)), m_auiIndicies, BufferUsageHint.StaticDraw); // Tells OpenGL how the data is structured.
        //    #if DEBUG
        //        logger.Info("OpenGL Generate EBO: " + GL.GetError().ToString());
        //    #endif

        //    GL.BindBuffer(BufferTarget.ArrayBuffer, m_uiVertexBufferHandle);    // Switch back to our Buffer Object as the current buffer.
        //    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Double, false, GLVertex.SizeInBytes(), 0);  // Tells OpenGL about the first three doubles in the vbo, i.e the position of the vertex.
        //    GL.VertexAttribPointer(1, 4, VertexAttribPointerType.HalfFloat, true, GLVertex.SizeInBytes(), Vector3d.SizeInBytes); // tells OpenGL about the 4 half floats used to repesent color.
        //    GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, GLVertex.SizeInBytes(), (Vector3d.SizeInBytes + Vector4h.SizeInBytes)); // tells OpenGL about the 2 floats in the vertgexc used to repesent UV coords.
        //    #if DEBUG
        //        logger.Info("OpenGL Create Vertes Attribute Pointers: " + GL.GetError().ToString());
        //    #endif
            
        //    // Turn on the Vertex Attribs:
        //    GL.EnableVertexAttribArray(0);
        //    GL.EnableVertexAttribArray(1);
        //    GL.EnableVertexAttribArray(2);
        //}
        #endregion

        public void SetRotation(float a_fRot)
        {
            m_m4ModelMatrix = Matrix4.CreateRotationZ(a_fRot);
        }

        public void UpdatePos(int x, int y)
        {
            m_m4ModelMatrix = Matrix4.CreateTranslation(x, y, 0);
        }


        public virtual void Render(ref Matrix4 a_m4Projection, ref Matrix4 a_m4View)
        {
            GL.BindVertexArray(m_uiVextexArrayHandle);

            m_oShaderProgram.StartUsing(ref m_m4ModelMatrix);
            
            //GL.ActiveTexture(TextureUnit.Texture0);
            OpenTKUtilities.Use2DTexture(m_uiTextureID);

            GL.DrawElements(BeginMode.TriangleStrip, m_auiIndicies.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);
        }

        public virtual void Render()
        {
            GL.BindVertexArray(m_uiVextexArrayHandle);

            m_oShaderProgram.StartUsing(ref m_m4ModelMatrix);

            //GL.ActiveTexture(TextureUnit.Texture0);
            OpenTKUtilities.Use2DTexture(m_uiTextureID);

            GL.DrawElements(BeginMode.TriangleStrip, m_auiIndicies.Length, DrawElementsType.UnsignedShort, IntPtr.Zero);
        }

        public virtual void RecalculateModelMatrix()
        {
            m_m4ModelMatrix = Matrix4.Scale(m_v2Size.X) * Matrix4.CreateTranslation(m_v3Position);
        }

        public virtual void SetSize(Vector2 a_v2Size)
        {
            m_v2Size = a_v2Size;
            RecalculateModelMatrix();
        }
    }
}
