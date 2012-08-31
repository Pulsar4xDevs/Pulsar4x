using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using Pulsar4X.WinForms;
using Pulsar4X.WinForms.Controls;
using log4net.Config;
using log4net;

namespace Pulsar4X.WinForms.Controls
{
    /// <summary>
    /// An Customised version of GLControle, Used as a base for the OpenGL Version Specifc dervied classes.
    /// </summary>
    public abstract class GLCanvas : OpenTK.GLControl
    {

        public static readonly ILog logger = LogManager.GetLogger(typeof(GLCanvas));

        /// <summary>
        /// Our Projections/ViewMatricies.
        /// </summary>
        protected Matrix4 m_m4ProjectionMatrix, m_m4ViewMatrix;

        /// <summary>
        /// used to determine if this control hase bee sucessfully loaded.
        /// </summary>
        protected bool m_bLoaded = false;

        /// <summary> 
        /// The zoom scaler, make this smaller to zoom out, larger to zoom in.
        /// </summary>
        protected float m_fZoomScaler = UIConstants.ZOOM_DEFAULT_SCALLER;

        /// <summary>   Gets or sets the zoom factor. Make this smaller to zoom out, larger to zoom in.</summary>
        /// <value> The zoom factor. </value>
        public float ZoomFactor
        {
            get
            {
                return m_fZoomScaler;
            }
            set
            {
                m_fZoomScaler = value;
            }
        }

        protected float m_fps = 0;
        public float FPS
        {
            get
            {
                return m_fps;
            }
        }


        public GLCanvas()
        {
            RegisterEventHandlers();
        }

        public GLCanvas(GraphicsMode a_oGraphicsMode)
            : base(a_oGraphicsMode)
        {
            RegisterEventHandlers();
        }

        public GLCanvas(GraphicsMode a_oGraphicsMode, int a_iMajor, int a_iMinor, GraphicsContextFlags a_eFlags = GraphicsContextFlags.Default)
            : base(a_oGraphicsMode, a_iMajor, a_iMinor, a_eFlags)
        {
            RegisterEventHandlers();
        }

        private void RegisterEventHandlers()
        {
            // Below we setup even handlers for this class:
            Load += new System.EventHandler(this.OnLoad);                           // Setep Load Event Handler
            Resize += new System.EventHandler(this.OnResize);                       // Setep Resize Event Handler
            Paint += new System.Windows.Forms.PaintEventHandler(this.OnPaint);      // Setep Paint Event Handler
            SizeChanged += new System.EventHandler(this.OnSizeChange);
            //Application.Idle += Application_Idle;
        }

        public abstract void OnLoad(object sender, EventArgs e);
        public abstract void OnSizeChange(object sender, EventArgs e);

        public void OnPaint(object sender, PaintEventArgs e)
        {
            if (!m_bLoaded)
            {
                return;
            }

            Render();
            this.Invalidate();
        }

        public void Application_Idle(object sender, EventArgs e)
        {
            if (m_bLoaded != true)
            {
                return;
            }

            this.Invalidate();
        }

        public virtual void OnResize(object sender, EventArgs e)
        {
            this.Size = this.Parent.Size;               // Set this controls size to be the same as the parent. This is assuemd to be safe.
            SetupViewPort(0, 0, this.Size.Height, this.Size.Width);  // Setup viewport again.
            this.Invalidate();                                       // Force redraw.
        }

        
        /// <summary>
        /// Sets up the OpenGL viewport/camera.
        /// </summary>
        /// <param name="a_iViewportPosX">The X-axis Position of the Viewport relative to the world</param>
        /// <param name="a_iViewportPosY">The Y-axis Position of the Viewport Relative to the world</param>
        /// <param name="a_iViewportWidth">The Width of the Viewport</param>
        /// <param name="a_iViewPortHeight">The Height of the Viewport</param>
        public virtual void SetupViewPort(  int a_iViewportPosX,    int a_iViewportPosY, 
                                            int a_iViewportWidth,    int a_iViewPortHeight)
        {
            GL.Viewport(a_iViewportPosX, a_iViewportPosY, a_iViewportWidth, a_iViewPortHeight);
            //float aspectRatio = a_iViewportWidth / (float)(a_iViewPortHeight); // Calculate Aspect Ratio.

            // Setup our Projection Matrix, This defines how the 2D image seen on screen is created from our 3d world.
            // this will give us a 2d perspective looking Down onto the X,Y plane with 0,0 being the centre of the screen (by default)
            m_m4ProjectionMatrix = Matrix4.CreateOrthographic(a_iViewportWidth, a_iViewPortHeight, -10, 10);
            
            // This will setup a projection where 0,0 is in the bottom left of the screen and we are looking at the X,Y plane from above (i think, i might be below).
            //Matrix4 m_m4ProjectionMatrix2 = new Matrix4(new Vector4((2.0f / a_iViewportWidth), 0, 0, 0),
            //                                    new Vector4(0, (2.0f / a_iViewPortHeight), 0, 0),
            //                                    new Vector4(0, 0, 1, 0),
            //                                    new Vector4(-1, -1, 1, 1));

            // Setup our Model View Matrix i.e. the position and faceing of our camera. We are setting it up to look at (0,0,0) from (0,3,5) with positive y being up.
            m_m4ViewMatrix = Matrix4.Scale(m_fZoomScaler);
        }

        public abstract void IncreaseZoomScaler();

        public abstract void DecreaseZoomScaler();

        public void PositionViewPort(int a_iViewportPosX, int a_iViewportPosY)
        {
            throw new NotImplementedException();
        }

        public void ResizeViewPort(int a_iViewportWidth,    int a_iViewPortHeight)
        {
            throw new NotImplementedException();
        }

        public abstract void Render();

        public abstract void TestFunc(int a_itest);

    }
}
