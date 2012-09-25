//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Drawing;
//using System.Windows.Forms;
//using OpenTK.Graphics;
//using Pulsar4X.WinForms;
//using Pulsar4X.WinForms.Controls;
//using OpenTK.Graphics.OpenGL;
//using OpenTK;
//using log4net.Config;
//using log4net;
//using Pulsar4X.WinForms.GLUtilities;

//namespace Pulsar4X.WinForms.Controls
//{
//    // cool GL 2.0 GLSL tutorial: http://www.lighthouse3d.com/tutorials/glsl-tutorial/

//    /// <summary>
//    /// A custom Version of the GLControl class which guarantees that OpenGL version 2.X is availble.
//    /// </summary>
//    public class GLCanves20 : GLCanvas
//    {

//        /// <summary> The shader program </summary>
//        // private GLShader m_oShaderProgram;

//        // for testing:
//        System.Diagnostics.Stopwatch m_oSW = new System.Diagnostics.Stopwatch();
//        double m_dAccumulator = 0;
//        int m_iFrameCounter = 0;


//        public GLCanves20()
//            : base(new GraphicsMode(32, 24, 8, 4), 2, 0, GraphicsContextFlags.Debug)
//        {
//#if DEBUG
//            logger.Info("UI: Creating an OpenGL 2.1+ GLCanvas");
//#endif
//        }


//        public override void OnLoad(object sender, EventArgs e)
//        {
//            GraphicsContext.CurrentContext.VSync = true; // this prevents us using 100% GPU/CPU.
//            m_bLoaded = true;           // So we know we have a valid Loaded OpenGL context.

//#if DEBUG
//            m_eGLError = GL.GetError();
//            if (m_eGLError != ErrorCode.NoError)
//            {
//                logger.Info("OpenGL Pre State Config Error Check: " + m_eGLError.ToString());
//            }
//#endif
//            //GL.ShadeModel(ShadingModel.Smooth);
//            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
//            GL.ReadBuffer(ReadBufferMode.Back);
//            GL.DrawBuffer(DrawBufferMode.Back);
//            GL.DepthFunc(DepthFunction.Lequal);
//            GL.DepthMask(true);
//            GL.Disable(EnableCap.StencilTest);
//            GL.StencilMask(0xFFFFFFFF);
//            GL.StencilFunc(StencilFunction.Equal, 0x00000000, 0x00000001);
//            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
//            GL.FrontFace(FrontFaceDirection.Ccw);
//            GL.CullFace(CullFaceMode.Back);
//            GL.Enable(EnableCap.CullFace);
//            GL.Enable(EnableCap.DepthTest);
//            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
//            GL.Enable(EnableCap.Blend);
//            GL.ClearColor(System.Drawing.Color.MidnightBlue);
//            GL.ClearDepth(1.0);
//            GL.ClearStencil(0);
//            GL.Enable(EnableCap.VertexArray);
//            m_eGLError = GL.GetError();
//            if (m_eGLError != ErrorCode.NoError)
//            {
//                logger.Info("OpenGL Post State Config Error Check: " + m_eGLError.ToString());
//            }

//            Program.logger.Info("UI: GLCanvas2.X Loaded Successfully, Open GL Version: " + GL.GetString(StringName.Version));
//            // Log out OpeGL specific stuff if debug build.
//            logger.Info("UI: GLSL Version: " + GL.GetString(StringName.ShadingLanguageVersion));
//            logger.Info("UI: Renderer: " + GL.GetString(StringName.Renderer));
//            logger.Info("UI: Vender: " + GL.GetString(StringName.Vendor));
//            logger.Info("UI: Extensions: " + GL.GetString(StringName.Extensions));
//            m_eGLError = GL.GetError();
//            if (m_eGLError != ErrorCode.NoError)
//            {
//                logger.Info("OpenGL Error Check, InvalidEnum or NoError Expected: " + m_eGLError.ToString());
//            }

//            m_oShaderProgram = new GLShader();

//            // Setup Our View Port, this sets Our Projection and View Matricies.
//            SetupViewPort(0, 0, this.Size.Width, this.Size.Height);

//            m_oSW.Start();
//        }

//        public override void OnSizeChange(object sender, EventArgs e)
//        {
//            if (m_bLoaded == true)
//            {
//                // When we are resized by our parent as pert of the docking, we will need to adjust our projection and view matricies 
//                // to reflect the new viewing area:
//                SetupViewPort(0, 0, this.Size.Width, this.Size.Height);  // Setup viewport again.
//            }
//        }

//        public override void IncreaseZoomScaler()
//        {
//            float fOldZoomSxaler = m_fZoomScaler;
//            m_fZoomScaler *= UIConstants.ZOOM_IN_FACTOR;

//            if (m_fZoomScaler > UIConstants.ZOOM_MAXINUM_VALUE)
//            {
//                m_fZoomScaler = UIConstants.ZOOM_MAXINUM_VALUE;
//            }

//            RecalculateViewMatrix(fOldZoomSxaler);
//            SceenToRender.ZoomSclaer = m_fZoomScaler;
//            SceenToRender.Refresh();                    // called so sceen elements can adjust to new zoom value.
//            this.Invalidate();
//        }

//        public override void DecreaseZoomScaler()
//        {
//            float fOldZoomSxaler = m_fZoomScaler;
//            m_fZoomScaler *= UIConstants.ZOOM_OUT_FACTOR;

//            if (m_fZoomScaler < UIConstants.ZOOM_MINIMUM_VALUE)
//            {
//                m_fZoomScaler = UIConstants.ZOOM_MINIMUM_VALUE;
//            }

//            RecalculateViewMatrix(fOldZoomSxaler);
//            SceenToRender.ZoomSclaer = m_fZoomScaler;
//            SceenToRender.Refresh();                    // called so sceen elements can adjust to new zoom value.
//            this.Invalidate();
//        }

//        public override void Pan(ref Vector3 a_v3PanAmount)
//        {
//            m_v3ViewOffset = m_v3ViewOffset + a_v3PanAmount;
//            RecalculateViewMatrix();
//            SceenToRender.ViewOffset = m_v3ViewOffset;
//            this.Invalidate();
//        }

//        public override void CenterOnZero()
//        {
//            m_v3ViewOffset = Vector3.Zero;  // sero out offset.
//            RecalculateViewMatrix();
//            SceenToRender.ViewOffset = m_v3ViewOffset;
//            this.Invalidate();
//        }

//        public override void CenterOn(ref Vector3 a_v3Location)
//        {
//            m_v3ViewOffset = a_v3Location;                  // set offset.
//            RecalculateViewMatrix();
//            SceenToRender.ViewOffset = m_v3ViewOffset;
//            this.Invalidate();
//        }

//        /// <summary>
//        /// OpenGL 2.0 - use shaders and a 2D drawing method.
//        /// </summary>
//        public override void Render()
//        {
//            this.MakeCurrent();                            // Make this Canvas Current, just in case there is more then one canves open.

//            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); // Clear Back buffer of previous frame.

//            // call render on all items in the render list:
//            //foreach (GLUtilities.GLPrimitive oPrimative in m_loRenderList)
//            //{
//            //    oPrimative.Render(ref m_m4ProjectionMatrix, ref m_m4ViewMatrix);
//            //}
//            // call render on the sceen:
//            SceenToRender.Render();

//            GraphicsContext.CurrentContext.SwapBuffers();
//            //#if DEBUG
//            //     logger.Info("Draw Colpeted, OpenGL error: " + GL.GetError().ToString());
//            // #endif

//            // used to work out frame rate:
//            m_oSW.Stop();
//            m_dAccumulator += m_oSW.Elapsed.TotalMilliseconds;
//            m_iFrameCounter++;
//            if (m_dAccumulator > 2000)
//            {
//                m_fps = (float)m_dAccumulator / (float)m_iFrameCounter;
//                m_dAccumulator -= 2000;
//                m_iFrameCounter = 0;
//            }
//            m_oSW.Reset();
//            m_oSW.Start();
//        }


//        public override void TestFunc(int a_itest)
//        {
//            //m_oQuad.UpdatePos(a_itest, a_itest);
//            //m_m4ViewMatrix = Matrix4.CreateTranslation(a_itest, a_itest, 0);
//            float ftest = a_itest / 100.0f;
//            //m_m4ViewMatrix = Matrix4.Scale(ftest, ftest, 0);
//        }

//    }
//}
