using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using OpenTK.Graphics;
using Pulsar4X.WinForms;
using Pulsar4X.WinForms.Controls;
using OpenTK.Graphics.OpenGL;

namespace Pulsar4X.WinForms.Controls
{
    /// <summary>
    /// An Customised version of GLControle, Used as a base for the OpenGL Version Specifc dervied classes.
    /// </summary>
    abstract class GLCanvas : OpenTK.GLControl
    {
        /// <summary>
        /// used to determine if this control hase bee sucessfully loaded.
        /// </summary>
        protected bool m_bLoaded = false;

        public float m_fps = 0;


        public GLCanvas()
        {
            //GraphicsContext.CurrentContext.VSync = true; // this prevents us using 100% GPU/CPU.
            RegisterEventHandlers();
        }

        public GLCanvas(GraphicsMode a_oGraphicsMode)
            : base(a_oGraphicsMode)
        {
            //GraphicsContext.CurrentContext.VSync = true; // this prevents us using 100% GPU/CPU.
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
            //Application.Idle += Application_Idle;
        }

        public abstract void OnLoad(object sender, EventArgs e);
        public abstract void OnPaint(object sender, PaintEventArgs e);

        public void Application_Idle(object sender, EventArgs e)
        {
            if (m_bLoaded != true)
            {
                return;
            }

            this.Invalidate();

            //double dMilliseconds = ComputeTimeSlice();
            //Accumulate(dMilliseconds);
            // Animate(dMilliseconds);
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
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, a_iViewportWidth, a_iViewPortHeight, 0, -1, 1);                         // Top left corner pixel has coords (0,0), same as in winforms.
            GL.Viewport(a_iViewportPosX, a_iViewportPosY, a_iViewportWidth, a_iViewPortHeight); // Sets Position of the viewport relative to the world (as defined above) and it's size.
        }

        public void PositionViewPort(int a_iViewportPosX, int a_iViewportPosY)
        {
            throw new NotImplementedException();
        }

        public void ResizeViewPort(int a_iViewportWidth,    int a_iViewPortHeight)
        {
            throw new NotImplementedException();
        }

        public abstract void Render();

        //public abstract void PreRenderPlanet(float a_fRadius);

        public abstract void TestFunc(int a_itest);

    }
}
