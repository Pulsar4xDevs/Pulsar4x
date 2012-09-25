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
    /// An Customised version of GLControl, Used as a base for the OpenGL Version Specifc dervied classes.
    /// </summary>
    public class GLCanvas : OpenTK.GLControl
    {

        public static readonly ILog logger = LogManager.GetLogger(typeof(GLCanvas));

        // for testing:
        System.Diagnostics.Stopwatch m_oSW = new System.Diagnostics.Stopwatch();
        double m_dAccumulator = 0;
        int m_iFrameCounter = 0;


        /// <summary> Used for testing for OpenGL Errors. </summary>
        protected ErrorCode m_eGLError;

        public delegate void InputHandlers(KeyEventArgs k, MouseEventArgs m);

        public InputHandlers InputHandler;

        /// <summary>
        /// Our Projections/ViewMatricies.
        /// </summary>
        protected Matrix4 m_m4ProjectionMatrix, m_m4ViewMatrix;

        /// <summary> The shader program used by default.</summary>
        protected GLUtilities.GLShader m_oShaderProgram;

        /// <summary>   Gets the default shader. </summary>
        /// <value> The default shader. </value>
        public GLUtilities.GLShader DefaultShader
        {
            get
            {
                return m_oShaderProgram;
            }
        }

        /// <summary>
        /// used to determine if this control hase bee sucessfully loaded.
        /// </summary>
        private bool m_bLoaded = false;

        public bool Loaded
        {
            get
            {
                return m_bLoaded;
            }
            set
            {
                m_bLoaded = value;
            }
        }

        /// <summary> 
        /// The zoom scaler, make this smaller to zoom out, larger to zoom in.
        /// </summary>
        protected float m_fZoomScaler = UIConstants.ZOOM_DEFAULT_SCALLER;

        /// <summary> The view offset, i.e. how much the view should be offset from 0, 0 </summary>
        protected Vector3 m_v3ViewOffset = new Vector3(0, 0, 0);

        /// <summary>
        /// Keeps tract of the start location when calculation Panning.
        /// </summary>
        Vector3 m_v3PanStartLocation;

        /// <summary>
        /// The Current Sceen for the Canvas to Render.
        /// </summary>
        protected SceenGraph.Sceen m_oSceenToRender;
        
        public SceenGraph.Sceen SceenToRender 
        {
            get
            {
                return m_oSceenToRender;
            }
            set
            {
                // first save the view setting to the sceen:
                if (m_oSceenToRender != null)
                {
                    m_oSceenToRender.ViewOffset = m_v3ViewOffset;
                    m_oSceenToRender.ZoomSclaer = m_fZoomScaler;
                }

                // Now set our new sceen and view settings.
                m_oSceenToRender = value;
                m_fZoomScaler = m_oSceenToRender.ZoomSclaer;
                m_v3ViewOffset = m_oSceenToRender.ViewOffset;
                RecalculateViewMatrix();
            }
        }

        /// <summary> Gets or sets the zoom factor. </summary>
        /// <value> The zoom factor. </value>
        public float ZoomFactor
        {
            get
            {
                return m_fZoomScaler;
            }
            set
            {
                float OldZoomScale = m_fZoomScaler;
                m_fZoomScaler = value;
                // update view matrix:
                RecalculateViewMatrix(OldZoomScale);
                // update sceen:
                m_oSceenToRender.ZoomSclaer = m_fZoomScaler;
            }
        }


        ///< @todo FPS counter,  For testing will need to be either deleted or cleaned up at some point
        protected float m_fps = 0;
        public float FPS
        {
            get
            {
                return m_fps;
            }
        }


        /// <summary>   Default constructor. </summary>
        public GLCanvas()
        {
            RegisterEventHandlers();
        }

        /// <summary> Constructor. </summary>
        /// <param name="a_oGraphicsMode">  The openGL graphics mode. </param>
        public GLCanvas(GraphicsMode a_oGraphicsMode)
            : base(a_oGraphicsMode)
        {
            RegisterEventHandlers();
        }


        /// <summary>   Constructor. </summary>
        /// <remarks>   Gregory.nott, 9/7/2012. </remarks>
        /// <param name="a_oGraphicsMode">  The openGL graphics mode. </param>
        /// <param name="a_iMajor">  The Major part of the openGL Version (1, 2, 3 or 4). </param>
        /// <param name="a_iMinor">  The minor part of the openGL version.</param>
        /// <param name="a_eFlags">  (optional) Any OpenGL Flags, e.g. Debug or Normal</param>
        public GLCanvas(GraphicsMode a_oGraphicsMode, int a_iMajor, int a_iMinor, GraphicsContextFlags a_eFlags = GraphicsContextFlags.Default)
            : base(a_oGraphicsMode, a_iMajor, a_iMinor, a_eFlags)
        {
            RegisterEventHandlers();
        }

        /// <summary>
        /// Creates the GLCanvas as an OpenGL 2.0 context
        /// </summary>
        /// <param name="a_version20"> Pass through a float to force this constructor.</param>
        public GLCanvas(float a_version20)
            : base(new GraphicsMode(32, 24, 8, 4), 2, 0, GraphicsContextFlags.Default)
        {
            RegisterEventHandlers();
            #if DEBUG
                logger.Info("UI: Creating an OpenGL 2.0+ GLCanvas");
            #endif
        }

        /// <summary>
        /// Creates the GLCanvas as an OpenGL 3.2 context.
        /// </summary>
        /// <param name="a_version30">Pass through a int to force this constructor.</param>
        public GLCanvas(int a_version30)
            : base(new GraphicsMode(32, 24, 8, 4), 3, 2, GraphicsContextFlags.Default)
        {
            RegisterEventHandlers();
            #if DEBUG
                logger.Info("UI: Creating an OpenGL 3.2+ GLCanvas");
            #endif
        }

        /// <summary> Registers the event handlers. </summary>
        private void RegisterEventHandlers()
        {
            // Below we setup even handlers for this class:
            Load += new System.EventHandler(this.OnLoad);                           // Setup Load Event Handler
            Paint += new System.Windows.Forms.PaintEventHandler(this.OnPaint);      // Setup Paint Event Handler
            SizeChanged += new System.EventHandler(this.OnSizeChange);              // Setup Size Changed Enet Handler.
            MouseMove += new MouseEventHandler(OnMouseMove);                        // Setup Mouse Move Event handler
            MouseDown += new MouseEventHandler(OnMouseDown);                        // Setup Mouse Down Event handler.
            MouseUp += new MouseEventHandler(OnMouseUp);                            // Setup Mouse Down Event handler.
            //MouseHover += new EventHandler(GLCanvas_OnMouseHover);
            KeyDown += new KeyEventHandler(OnKeyDown);
            //MouseUp += new MouseEventHandler(OnMouseUp);
            Application.Idle += new EventHandler(Application_Idle);
        }

        private void InitOpenGL30()
        {
            this.Context.SwapInterval = 1; // this prevents us using 100% GPU/CPU.
            //GraphicsContext.CurrentContext.VSync = true; // this prevents us using 100% GPU/CPU.
            Loaded = true;           // So we know we have a valid Loaded OpenGL context.

            #if DEBUG
                m_eGLError = GL.GetError();
                if (m_eGLError != ErrorCode.NoError)
                {
                    logger.Info("OpenGL Pre State Config Error Check: " + m_eGLError.ToString());
                }   
            #endif
            //GL.ShadeModel(ShadingModel.Smooth);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.ReadBuffer(ReadBufferMode.Back);
            GL.DrawBuffer(DrawBufferMode.Back);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthMask(true);
            GL.Disable(EnableCap.StencilTest);
            GL.StencilMask(0xFFFFFFFF);
            GL.StencilFunc(StencilFunction.Equal, 0x00000000, 0x00000001);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);
            GL.ClearColor(System.Drawing.Color.MidnightBlue);
            GL.ClearDepth(1.0);
            GL.ClearStencil(0);
            //GL.Enable(EnableCap.VertexArray);
            m_eGLError = GL.GetError();
            if (m_eGLError != ErrorCode.NoError)
            {
                logger.Info("OpenGL Post State Config Error Check: " + m_eGLError.ToString());
            } 
                
            logger.Info("UI: GLCanvas3.X Loaded Successfully, Open GL Version: " + GL.GetString(StringName.Version));
            // Log out OpeGL specific stuff if debug build.
            logger.Info("UI: GLSL Version: " + GL.GetString(StringName.ShadingLanguageVersion));
            logger.Info("UI: Renderer: " + GL.GetString(StringName.Renderer));
            logger.Info("UI: Vender: " + GL.GetString(StringName.Vendor));
            logger.Info("UI: Extensions: " + GL.GetString(StringName.Extensions));
            m_eGLError = GL.GetError();
            if (m_eGLError != ErrorCode.NoError)
            {
                logger.Info("OpenGL Error Check, InvalidEnum or NoError Expected: " + m_eGLError.ToString());
            }    

            m_oShaderProgram = new GLUtilities.GLShader();

            // Setup Our View Port, this sets Our Projection and View Matricies.
            SetupViewPort(0, 0, this.Size.Width, this.Size.Height);

            m_oSW.Start();
        }

        private void InitOpenGL20()
        {
            this.Context.SwapInterval = 1; // this prevents us using 100% GPU/CPU.
            //GraphicsContext.CurrentContext.VSync = true; // this prevents us using 100% GPU/CPU.
            Loaded = true;           // So we know we have a valid Loaded OpenGL context.

            #if DEBUG
                m_eGLError = GL.GetError();
                if (m_eGLError != ErrorCode.NoError)
                {
                    logger.Info("OpenGL Pre State Config Error Check: " + m_eGLError.ToString());
                }   
            #endif
            //GL.ShadeModel(ShadingModel.Smooth);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.ReadBuffer(ReadBufferMode.Back);
            GL.DrawBuffer(DrawBufferMode.Back);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthMask(true);
            GL.Disable(EnableCap.StencilTest);
            GL.StencilMask(0xFFFFFFFF);
            GL.StencilFunc(StencilFunction.Equal, 0x00000000, 0x00000001);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);
            GL.ClearColor(System.Drawing.Color.MidnightBlue);
            GL.ClearDepth(1.0);
            GL.ClearStencil(0);
            GL.Enable(EnableCap.VertexArray);
            m_eGLError = GL.GetError();
            if (m_eGLError != ErrorCode.NoError)
            {
                logger.Info("OpenGL Post State Config Error Check: " + m_eGLError.ToString());
            }  

            Program.logger.Info("UI: GLCanvas2.X Loaded Successfully, Open GL Version: " + GL.GetString(StringName.Version));
                // Log out OpeGL specific stuff if debug build.
            logger.Info("UI: GLSL Version: " + GL.GetString(StringName.ShadingLanguageVersion));
            logger.Info("UI: Renderer: " + GL.GetString(StringName.Renderer));
            logger.Info("UI: Vender: " + GL.GetString(StringName.Vendor));
            logger.Info("UI: Extensions: " + GL.GetString(StringName.Extensions));
            m_eGLError = GL.GetError();
            if (m_eGLError != ErrorCode.NoError)
            {
                logger.Info("OpenGL Error Check, InvalidEnum or NoError Expected: " + m_eGLError.ToString());
            }
            
            m_oShaderProgram = new GLUtilities.GLShader();

            // Setup Our View Port, this sets Our Projection and View Matricies.
            SetupViewPort(0, 0, this.Size.Width, this.Size.Height);

            m_oSW.Start();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // GLCanvas
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.Name = "GLCanvas";
            this.ResumeLayout(false);
        }

        public void OnLoad(object sender, EventArgs e)
        {
            if (OpenTKUtilities.Instance.SupportedOpenGLVersion == OpenTKUtilities.GLVersion.OpenGL2X)
            {
                InitOpenGL20();
            }
            else if (OpenTKUtilities.Instance.SupportedOpenGLVersion == OpenTKUtilities.GLVersion.OpenGL3X
                    || OpenTKUtilities.Instance.SupportedOpenGLVersion == OpenTKUtilities.GLVersion.OpenGL4X)
            {
                InitOpenGL30();
            }
        }

        /// <summary> Executes the size change action. </summary>
        /// <remarks> Must be overloaded by the inherited classes. 
        /// 		  This is needed to update the view and projection matricies whenever the form size changes.
        /// 		  If these matricies are not updates then the view will be cut off and not draw for the whole screen. </remarks>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        public void OnSizeChange(object sender, EventArgs e)
        {
            if (m_bLoaded == true)
            {
                // When we are resized by our parent as pert of the docking, we will need to adjust our projection and view matricies 
                // to reflect the new viewing area:
                SetupViewPort(0, 0, this.Size.Width, this.Size.Height);  // Setup viewport again.
            }
        }

        /// <summary>   Paints this window, Calles the Render() functio to make sure our sceen is rendered. </summary>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        public void OnPaint(object sender, PaintEventArgs e)
        {
            if (!m_bLoaded)
            {
                return;
            }

            Render();
        }

        /// <summary>   Event handler. Called by Application for idle events. Keeps the Canvas rendering evan when nothing is happening! </summary>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        public void Application_Idle(object sender, EventArgs e)
        {
            if (m_bLoaded != true)
            {
                return;
            }

            this.Invalidate();
        }


        /// <summary>   Executes the mouse move action. i.e. Panning </summary>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Vector3 v3PanEndLocation;
                v3PanEndLocation.X = e.Location.X;
                v3PanEndLocation.Y = e.Location.Y;
                v3PanEndLocation.Z = 0.0f;

                Vector3 v3PanAmount = (v3PanEndLocation - m_v3PanStartLocation);

                v3PanAmount.Y = -v3PanAmount.Y; // we flip Y to make the panning go in the right direction.
                this.Pan(ref v3PanAmount);

                m_v3PanStartLocation.X = e.Location.X;
                m_v3PanStartLocation.Y = e.Location.Y;
                m_v3PanStartLocation.Z = 0.0f;
            }
        }


        /// <summary>   Executes the mouse down action. i.e. Start panning </summary>
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            // An left mouse down, start pan.
            if (e.Button.Equals(System.Windows.Forms.MouseButtons.Right))
            {
                Cursor.Current = Cursors.NoMove2D;
                m_v3PanStartLocation.X = e.Location.X;
                m_v3PanStartLocation.Y = e.Location.Y;
                m_v3PanStartLocation.Z = 0.0f;
            }
            else if (e.Button.Equals(System.Windows.Forms.MouseButtons.Middle))
            {
                // on middle or mouse wheel button, centre!
                this.CenterOnZero();
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            // reset cursor:
            Cursor.Current = Cursors.Default;
        }

        private void GLCanvas_OnMouseHover(object sender, EventArgs e)
        {
            // get mouse position in control coords:
            Point oCursorPosition = PointToClient(Cursor.Position);

            // Convert to be world coords:
            Vector3 v3CurPosWorldCorrds = new Vector3((Size.Width / 2) - oCursorPosition.X, (Size.Height / 2) - oCursorPosition.Y, 0);
            v3CurPosWorldCorrds = v3CurPosWorldCorrds / ZoomFactor;

           // Guid oEntity = m_oCurrentSceen.GetElementAtCoords(v3CurPosWorldCorrds);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            InputHandler(e, null);
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

            // Setup our Model View Matrix i.e. the position and faceing of our camera. We are setting it up to look at (0,0,0) from (0,3,5) with positive y being up.
            m_m4ViewMatrix = Matrix4.Scale(m_fZoomScaler) * Matrix4.CreateTranslation(m_v3ViewOffset);
            if (m_bLoaded && m_oShaderProgram != null)
            {
                m_oShaderProgram.SetProjectionMatrix(ref m_m4ProjectionMatrix);
                m_oShaderProgram.SetViewMatrix(ref m_m4ViewMatrix);
            }
        }

        public virtual Vector3 ConvertScreenCoordsToWorldCoords(Vector3 a_v3ScreenCoords)
        {
            return a_v3ScreenCoords / m_fZoomScaler;
        }

        public virtual Vector3 ConvertScreenCoordsToWorldCoords(Vector3 a_v3ScreenCoords, float a_fZoomScaler)
        {
            return a_v3ScreenCoords / a_fZoomScaler;
        }

        public virtual Vector3 ConvertWorldCoordsToScreenCoords(Vector3 a_v3WorldCoords)
        {
            return a_v3WorldCoords * m_fZoomScaler;
        }

        public virtual Vector3 ConvertWorldCoordsToScreenCoords(Vector3 a_v3WorldCoords, float a_fZoomScaler)
        {
            return a_v3WorldCoords * a_fZoomScaler;
        }

        public virtual void RecalculateViewMatrix(float a_fOldZoomScaler = -1.0f)
        {
            // When we are provided a valid old zoom we will convert the View offset to take into account the change in position.
            // this only needs to happen if the zoomscale has changed.
            if (a_fOldZoomScaler > 0.0f)
            {
                // first get current world position of camera:
                Vector3 v3WorldPos = ConvertScreenCoordsToWorldCoords(m_v3ViewOffset, a_fOldZoomScaler);
                // and convert it to new screen coords at new zoom scale:
                m_v3ViewOffset = ConvertWorldCoordsToScreenCoords(v3WorldPos);
            }

            // create new view matrix and apply to shader (if loaded properly and shader is valid!)
            m_m4ViewMatrix = Matrix4.Scale(m_fZoomScaler) * Matrix4.CreateTranslation(m_v3ViewOffset);
            if (m_bLoaded && m_oShaderProgram != null)
            {
                m_oShaderProgram.SetViewMatrix(ref m_m4ViewMatrix);
            }
        }

        public void IncreaseZoomScaler()
        {
            float fOldZoomSxaler = m_fZoomScaler;
            m_fZoomScaler *= UIConstants.ZOOM_IN_FACTOR;

            if (m_fZoomScaler > UIConstants.ZOOM_MAXINUM_VALUE)
            {
                m_fZoomScaler = UIConstants.ZOOM_MAXINUM_VALUE;
            }

            RecalculateViewMatrix(fOldZoomSxaler);
            SceenToRender.ZoomSclaer = m_fZoomScaler;
            SceenToRender.Refresh();                    // called so sceen elements can adjust to new zoom value.
            this.Invalidate();
        }

        public void DecreaseZoomScaler()
        {
            float fOldZoomSxaler = m_fZoomScaler;
            m_fZoomScaler *= UIConstants.ZOOM_OUT_FACTOR;

            if (m_fZoomScaler < UIConstants.ZOOM_MINIMUM_VALUE)
            {
                m_fZoomScaler = UIConstants.ZOOM_MINIMUM_VALUE;
            }

            RecalculateViewMatrix(fOldZoomSxaler);
            SceenToRender.ZoomSclaer = m_fZoomScaler;
            SceenToRender.Refresh();                    // called so sceen elements can adjust to new zoom value.
            this.Invalidate();
        }

        public void Pan(ref Vector3 a_v3PanAmount)
        {
            m_v3ViewOffset = m_v3ViewOffset + a_v3PanAmount;
            RecalculateViewMatrix();
            SceenToRender.ViewOffset = m_v3ViewOffset;
            this.Invalidate();
        }

        public void CenterOnZero()
        {
            m_v3ViewOffset = Vector3.Zero;  // sero out offset.
            RecalculateViewMatrix();
            SceenToRender.ViewOffset = m_v3ViewOffset;
            this.Invalidate();
        }

        public void CenterOn(ref Vector3 a_v3Location)
        {
            m_v3ViewOffset = a_v3Location;                  // set offset.
            RecalculateViewMatrix();
            SceenToRender.ViewOffset = m_v3ViewOffset;
            this.Invalidate();
        }

        public void Render()
        {
            this.MakeCurrent();                            // Make this Canvas Current, just in case there is more then one canves open.

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); // Clear Back buffer of previous frame.

            // call render on the sceen:
            if (SceenToRender != null)
            {
                SceenToRender.Render();
            }

            GraphicsContext.CurrentContext.SwapBuffers();

            // Calc FPS:
            m_oSW.Stop();
            m_dAccumulator += m_oSW.Elapsed.TotalMilliseconds;
            m_iFrameCounter++;
            if (m_dAccumulator > 2000)
            {
                m_fps = (float)m_dAccumulator / (float)m_iFrameCounter;
                m_dAccumulator -= 2000;
                m_iFrameCounter = 0;
            }
            m_oSW.Reset();
            m_oSW.Start();
        }
    }
}
