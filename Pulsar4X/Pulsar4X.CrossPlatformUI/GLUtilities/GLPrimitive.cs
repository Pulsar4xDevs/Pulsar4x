using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK;
#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif

namespace Pulsar4X.CrossPlatformUI.GLUtilities
{ 
    /// <summary>
    /// A class used for constructing and rendering an OpenGL primative (e.g. Quad, triangle, etc.).
    /// It includes the ability to apply a texture or color to the primative.
    /// </summary>
    public abstract class GLPrimitive
    {
        // Note: for AU/KM conversions: Constants.Units.KM_PER_AU
#if LOG4NET_ENABLED
        public static readonly ILog logger = LogManager.GetLogger(typeof(GLPrimitive));
#endif

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
                //UpdatePos((int)m_v3Position.X, (int)m_v3Position.Y);
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
        protected GLEffect m_oEffect;

        protected Color4 m_oColor;

        /// <summary>   
        /// Default constructor. 
        /// </summary>
        public GLPrimitive()
        {
        }

        public void SetRotation(float a_fRot)
        {
            m_m4ModelMatrix = Matrix4.CreateRotationZ(a_fRot);
        }

        public void UpdatePos(int x, int y)
        {
            m_m4ModelMatrix = Matrix4.CreateTranslation(x, y, 0);
        }

        public virtual void RecalculateModelMatrix()
        {
            m_m4ModelMatrix = Matrix4.CreateScale(m_v2Size.X) * Matrix4.CreateTranslation(m_v3Position);
        }

        public virtual void SetSize(Vector2 a_v2Size)
        {
            m_v2Size = a_v2Size;
            RecalculateModelMatrix();
        }

        public abstract void Render(ref Matrix4 a_m4Projection, ref Matrix4 a_m4View);
        public abstract void Render(ref Matrix4 a_m4View);
        public abstract void Render();
        public abstract void UpdateVBOs();
    }
}
