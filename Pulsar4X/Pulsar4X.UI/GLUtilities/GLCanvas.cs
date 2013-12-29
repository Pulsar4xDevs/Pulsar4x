using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using Pulsar4X.UI;
using log4net.Config;
using log4net;

namespace Pulsar4X.UI.GLUtilities
{
    /// <summary>
    /// An Customised version of GLControl, Used as a base for the OpenGL Version Specifc dervied classes.
    /// </summary>
    public class GLCanvas : OpenTK.GLControl
    {
        #region Properties and Member Vars

        // logger:
        public static readonly ILog logger = LogManager.GetLogger(typeof(GLCanvas));

        /// <summary> Used for testing for OpenGL Errors. </summary>
        private ErrorCode m_eGLError;

        /// <summary>
        /// Our Projections/ViewMatricies.
        /// </summary>
        private Matrix4 m_m4ProjectionMatrix, m_m4ViewMatrix;

        /// <summary> The shader program/Effect used by default.</summary>
        private GLEffect m_oEffect;

        /// <summary>   Gets the default shader/Effect. </summary>
        /// <value> The default shader/Effect. </value>
        public GLEffect DefaultEffect
        {
            get
            {
                return m_oEffect;
            }
        }

        /// <summary>
        /// used to determine if this control hase bee sucessfully loaded.
        /// </summary>
        private bool m_bLoaded = false;

        /// <summary>
        /// If true then the GLCanvas has been loaded sucessfully.
        /// </summary>
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
        private float m_fZoomScaler = UIConstants.ZOOM_DEFAULT_SCALLER;

        /// <summary> The view offset, i.e. how much the view should be offset from 0, 0 </summary>
        private Vector3 m_v3ViewOffset = new Vector3(0.0f,0.0f,0.0f);

        /// <summary>
        /// The Current Sceen for the Canvas to Render.
        /// </summary>
        private SceenGraph.Sceen m_oSceenToRender;
        
        /// <summary>
        /// The Sceen for the Canvas to render.
        /// </summary>
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
        // for testing:
        System.Diagnostics.Stopwatch m_oSW = new System.Diagnostics.Stopwatch();
        double m_dAccumulator = 0;
        int m_iFrameCounter = 0;
        private float m_fps = 0;
        public float FPS
        {
            get
            {
                return m_fps;
            }
        }

        /// <summary> OpenGL Version String. </summary>
        public string OpenGLVersion { get; set; }
        /// <summary> OpenGL Major Version Number. </summary>
        public int OpenGLVersionMajor { get; set; }
        /// <summary> OpenGL Minor Version Number. </summary>
        public int OpenGLVersionMinor { get; set; }

        #endregion

        #region Constructors

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

        #endregion

        #region Init Methods

        /// <summary> Registers the event handlers. </summary>
        private void RegisterEventHandlers()
        {
            // Below we setup even handlers for this class:
            Load += new System.EventHandler(this.OnLoad);                           // Setup Load Event Handler
            Paint += new System.Windows.Forms.PaintEventHandler(this.OnPaint);      // Setup Paint Event Handler
            SizeChanged += new System.EventHandler(this.OnSizeChange);              // Setup Size Changed Enet Handler.
            Application.Idle += new EventHandler(Application_Idle);                 // make sure we render when UI is otherwise idle!
        }

        private void InitOpenGL30()
        {
            //this.Context.SwapInterval = 1; // this prevents us using 100% GPU/CPU.
            GraphicsContext.CurrentContext.SwapInterval = 1; // this prevents us using 100% GPU/CPU.
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

            m_oEffect = new GLUtilities.GLEffectBasic30("./Resources/Shaders/Basic30_Vertex_Shader.glsl", "./Resources/Shaders/Basic30_Fragment_Shader.glsl");

            // Setup Our View Port, this sets Our Projection and View Matricies.
            SetupViewPort(0, 0, this.Size.Width, this.Size.Height);

            m_oSW.Start();
        }

        private void InitOpenGL21()
        {
            //this.Context.SwapInterval = 1; // this prevents us using 100% GPU/CPU.
            GraphicsContext.CurrentContext.SwapInterval = 1; // this prevents us using 100% GPU/CPU
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

            logger.Info("UI: GLCanvas2.X Loaded Successfully, Open GL Version: " + GL.GetString(StringName.Version));
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

            m_oEffect = new GLUtilities.GLEffectBasic21("./Resources/Shaders/Basic20_Vertex_Shader.glsl", "./Resources/Shaders/Basic20_Fragment_Shader.glsl");

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
            // Work Out OpenGL Version:
            try
            {
                OpenGLVersion = GL.GetString(StringName.Version);
                OpenGLVersionMajor = int.Parse(OpenGLVersion[0].ToString());      // extracts the major verion number an converts it to a int.
                OpenGLVersionMinor = int.Parse(OpenGLVersion[2].ToString());      // same again for minor verion number.
               // OpenGLVersionMajor = 2; - uncomment to force GL 2.0 - for testing
#if DEBUG
                logger.Debug("Highest OpenGL Version Initialised is " + OpenGLVersion);
#endif
            }
            catch (System.NullReferenceException exp)
            {
                // Problem occured when trying to get open GL Verion, Logg and assume 2.0 so program exacution can continue:
                logger.Error("Error Getting OpenGL Version, Assuming version 2.0!");
                OpenGLVersion = "2.0";
                OpenGLVersionMajor = 2;
                OpenGLVersionMinor = 0;
            }


            if (OpenGLVersionMajor == 2)
            {
                InitOpenGL21();
            }
            else if (OpenGLVersionMajor == 3)
            {
                if (OpenGLVersionMinor >= 2)
                    InitOpenGL30();
                else
                    InitOpenGL21();
            }
            else if (OpenGLVersionMajor >= 4)
            {
                InitOpenGL30();
            }
            else
            {
                
                String MSG = String.Format("Unknown OpenGL Version {0}.{1}",OpenGLVersionMajor,OpenGLVersionMinor);
                logger.Error(MSG);
            }
        }

        #endregion

        #region EventHandlers

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

            this.Invalidate();
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

        #endregion

        #region Public Methods

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

        /// <summary>
        /// Sets up the OpenGL viewport/camera.
        /// </summary>
        /// <param name="a_iViewportPosX">The X-axis Position of the Viewport relative to the world</param>
        /// <param name="a_iViewportPosY">The Y-axis Position of the Viewport Relative to the world</param>
        /// <param name="a_iViewportWidth">The Width of the Viewport</param>
        /// <param name="a_iViewPortHeight">The Height of the Viewport</param>
        public void SetupViewPort(  int a_iViewportPosX,    int a_iViewportPosY, 
                                            int a_iViewportWidth,    int a_iViewPortHeight)
        {
            GL.Viewport(a_iViewportPosX, a_iViewportPosY, a_iViewportWidth, a_iViewPortHeight);
            //float aspectRatio = a_iViewportWidth / (float)(a_iViewPortHeight); // Calculate Aspect Ratio.

            // Setup our Projection Matrix, This defines how the 2D image seen on screen is created from our 3d world.
            // this will give us a 2d perspective looking Down onto the X,Y plane with 0,0 being the centre of the screen (by default)
            m_m4ProjectionMatrix = Matrix4.CreateOrthographic(a_iViewportWidth, a_iViewPortHeight, -10, 10);

            // Setup our Model View Matrix i.e. the position and faceing of our camera. We are setting it up to look at (0,0,0) from (0,3,5) with positive y being up.
            m_m4ViewMatrix = Matrix4.Scale(m_fZoomScaler) * Matrix4.CreateTranslation(m_v3ViewOffset);
            if (m_bLoaded && m_oEffect != null)
            {
                m_oEffect.SetProjectionMatrix(ref m_m4ProjectionMatrix);
                m_oEffect.SetViewMatrix(ref m_m4ViewMatrix);
            }
        }

        public Vector3 ConvertScreenCoordsToWorldCoords(Vector3 a_v3ScreenCoords)
        {
            return a_v3ScreenCoords / m_fZoomScaler;
        }

        public Vector3 ConvertScreenCoordsToWorldCoords(Vector3 a_v3ScreenCoords, float a_fZoomScaler)
        {
            return a_v3ScreenCoords / a_fZoomScaler;
        }

        public Vector3 ConvertWorldCoordsToScreenCoords(Vector3 a_v3WorldCoords)
        {
            return a_v3WorldCoords * m_fZoomScaler;
        }

        public Vector3 ConvertWorldCoordsToScreenCoords(Vector3 a_v3WorldCoords, float a_fZoomScaler)
        {
            return a_v3WorldCoords * a_fZoomScaler;
        }

        /// <summary>
        /// Increases the zoom factor, Zooming In.
        /// </summary>
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

        /// <summary>
        /// Decreases the Zoom factor, zooming oout.
        /// </summary>
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

        /// <summary>
        /// Pans by the amount specified.
        /// </summary>
        /// <param name="a_v3PanAmount"> Ammount to Pan. </param>
        public void Pan(ref Vector3 a_v3PanAmount)
        {
            m_v3ViewOffset = m_v3ViewOffset + a_v3PanAmount;
            RecalculateViewMatrix();
            SceenToRender.ViewOffset = m_v3ViewOffset;
            this.Invalidate();
        }

        /// <summary>
        /// Centres on coords (0, 0, 0).
        /// </summary>
        public void CenterOnZero()
        {
            m_v3ViewOffset = Vector3.Zero;
            RecalculateViewMatrix();
            SceenToRender.ViewOffset = m_v3ViewOffset;
            this.Invalidate();
        }

        /// <summary>
        /// Centers on the Coords provided.
        /// </summary>
        /// <param name="a_v3Location"> Coords to Centre on. </param>
        public void CenterOn(ref Vector3 a_v3Location)
        {
            m_v3ViewOffset = a_v3Location;                  // set offset.
            RecalculateViewMatrix();
            SceenToRender.ViewOffset = m_v3ViewOffset;
            this.Invalidate();
        }

        /// <summary>
        /// Renders the current Sceen.
        /// </summary>
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

        #endregion

        #region Private Methods

        /// <summary>
        /// Recalculates the View Matrix.
        /// </summary>
        /// <param name="a_fOldZoomScaler"> Old Zoom  Scaler. </param>
        private void RecalculateViewMatrix(float a_fOldZoomScaler = -1.0f)
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
            if (m_bLoaded && m_oEffect != null)
            {
                m_oEffect.SetViewMatrix(ref m_m4ViewMatrix);
            }
        }

        #endregion
    }
}
