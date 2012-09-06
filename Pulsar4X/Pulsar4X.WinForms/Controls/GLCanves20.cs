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
using OpenTK;
using log4net.Config;
using log4net;
using Pulsar4X.WinForms.GLUtilities;

namespace Pulsar4X.WinForms.Controls
{
    // cool GL 2.0 GLSL tutorial: http://www.lighthouse3d.com/tutorials/glsl-tutorial/

    /// <summary>
    /// A custom Version of the GLControl class which guarantees that OpenGL version 2.X is availble.
    /// </summary>
    public class GLCanves20 : GLCanvas
    {

        /// <summary> The shader program </summary>
       // private GLShader m_oShaderProgram;

        // for testing:
        GLQuad m_oQuad;
        GLCircle m_oCircle;
        System.Diagnostics.Stopwatch m_oSW = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch oSW2 = new System.Diagnostics.Stopwatch();
        double m_dAccumulator = 0;
        int m_iFrameCounter = 0;


        public GLCanves20()
            : base(new GraphicsMode(32, 24, 8, 4), 2, 0, GraphicsContextFlags.Debug)
        {
            #if DEBUG
                logger.Info("UI: Creating an OpenGL 2.1+ GLCanvas");
            #endif
        }


        public override void OnLoad(object sender, EventArgs e)
        {
            GraphicsContext.CurrentContext.VSync = true; // this prevents us using 100% GPU/CPU.
            m_bLoaded = true;           // So we know we have a valid Loaded OpenGL context.

            #if DEBUG
                logger.Info("OpenGL Pre State Config Error Check: " + GL.GetError().ToString());
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
            #if DEBUG
                logger.Info("OpenGL Post State Config Error Check: " + GL.GetError().ToString());
            #endif

            Program.logger.Info("UI: GLCanvas2.X Loaded Successfully, Open GL Version: " + GL.GetString(StringName.Version));
            #if DEBUG
                // Log out OpeGL specific stuff if debug build.
                logger.Info("UI: GLSL Version: " + GL.GetString(StringName.ShadingLanguageVersion));
                logger.Info("UI: Renderer: " + GL.GetString(StringName.Renderer));
                logger.Info("UI: Vender: " + GL.GetString(StringName.Vendor));
                logger.Info("UI: Extensions: " + GL.GetString(StringName.Extensions));
                logger.Info("OpenGL Error Check: " + GL.GetError().ToString());
            #endif

            // Setup Our View Port, this sets Our Projection and View Matricies.
            SetupViewPort(0, 0, this.Size.Width, this.Size.Height);

            m_oShaderProgram = new GLShader();
            m_oShaderProgram.SetProjectionMatrix(ref m_m4ProjectionMatrix);
            m_oShaderProgram.SetViewMatrix(ref m_m4ViewMatrix);

            m_oQuad = new GLQuad(m_oShaderProgram, new Vector3(512, 0, 0), new Vector2(128, 128), System.Drawing.Color.FromArgb(255, 255, 0, 0), "./Resources/Textures/DefaultIcon.png"); ///< @todo Proper Path when resources sorted.
            m_oCircle = new GLCircle(m_oShaderProgram, new Vector3(0, 0, 0), 512.0f, System.Drawing.Color.Green, "./Resources/Textures/DefaultTexture.png");

            m_oSW.Start();
        }

        public override void OnResize(object sender, EventArgs e)
        {
            // Dont need to do anything here as we are docked to our parent.
        }

        public override void OnSizeChange(object sender, EventArgs e)
        {
            if (m_bLoaded == true)
            {
                // When we are resized by our parent as pert of the docking, we will need to adjust our projection and view matricies 
                // to reflect the new viewing area:
                SetupViewPort(0, 0, this.Size.Width, this.Size.Height);  // Setup viewport again.
                m_oShaderProgram.SetProjectionMatrix(ref m_m4ProjectionMatrix);
                m_oShaderProgram.SetViewMatrix(ref m_m4ViewMatrix);
            }
        }

        public override void IncreaseZoomScaler()
        {
            m_fZoomScaler *= UIConstants.ZOOM_IN_FACTOR;
            m_m4ViewMatrix = Matrix4.Scale(m_fZoomScaler);
            m_oShaderProgram.SetViewMatrix(ref m_m4ViewMatrix);
            this.Invalidate();

        }

        public override void DecreaseZoomScaler()
        {
            m_fZoomScaler *= UIConstants.ZOOM_OUT_FACTOR;
            m_m4ViewMatrix = Matrix4.Scale(m_fZoomScaler);
            m_oShaderProgram.SetViewMatrix(ref m_m4ViewMatrix);
            this.Invalidate();
        }

        public override void Pan(ref Vector3 a_v3PanAmount)
        {
            m_m4ViewMatrix = m_m4ViewMatrix * Matrix4.CreateTranslation(a_v3PanAmount);
            m_oShaderProgram.SetViewMatrix(ref m_m4ViewMatrix);
            this.Invalidate();
        }

        public override void CenterOnZero()
        {
            m_m4ViewMatrix = Matrix4.Identity;
            m_m4ViewMatrix = Matrix4.Scale(m_fZoomScaler); // reset scaler.
            m_oShaderProgram.SetViewMatrix(ref m_m4ViewMatrix);
            this.Invalidate();
        }

        public override void CenterOn(ref Vector3 a_v3Location)
        {
            m_m4ViewMatrix = Matrix4.Identity;
            m_m4ViewMatrix = Matrix4.Scale(m_fZoomScaler); // reset scaler.
            m_m4ViewMatrix = m_m4ViewMatrix * Matrix4.CreateTranslation(a_v3Location);
            m_oShaderProgram.SetViewMatrix(ref m_m4ViewMatrix);
            this.Invalidate();
        }

        /// <summary>
        /// OpenGL 2.0 - use shaders and a 2D drawing method.
        /// </summary>
        public override void Render()
        {
            this.MakeCurrent();                            // Make this Canvas Current, just in case there is more then one canves open.

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); // Clear Back buffer of previous frame.

            //m_oCircle.Render(ref m_m4ProjectionMatrix, ref m_m4ViewMatrix);
           // m_oQuad.Render(ref m_m4ProjectionMatrix, ref m_m4ViewMatrix);       // render our quad.

            // call render on all items in the render list:
            foreach (GLUtilities.GLPrimitive oPrimative in m_loRenderList)
            {
                oPrimative.Render(ref m_m4ProjectionMatrix, ref m_m4ViewMatrix);
            }
           

            GraphicsContext.CurrentContext.SwapBuffers();
            //#if DEBUG
           //     logger.Info("Draw Colpeted, OpenGL error: " + GL.GetError().ToString());
           // #endif

            // used to work out frame rate:
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


        public override void TestFunc(int a_itest)
        {
            //m_oQuad.UpdatePos(a_itest, a_itest);
            //m_m4ViewMatrix = Matrix4.CreateTranslation(a_itest, a_itest, 0);
            float ftest = a_itest / 100.0f;
            //m_m4ViewMatrix = Matrix4.Scale(ftest, ftest, 0);
        }

    }
}
