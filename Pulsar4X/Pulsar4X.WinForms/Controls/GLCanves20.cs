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
        /// <summary>
        /// Our Projections/ViewMatricies.
        /// </summary>
        Matrix4 m_m4ProjectionMatrix, m_m4ViewMatrix;

        private GLShader m_oShaderProgram;
        private GLShader m_oShaderProgram2;



        // for testing:
        GLPrimitive m_oQuad;
        GLCircle m_oCircle;
        System.Diagnostics.Stopwatch m_oSW = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch oSW2 = new System.Diagnostics.Stopwatch();
        double m_dAccumulator = 0;
        int m_iFrameCounter = 0;



        public GLCanves20()
            : base(new GraphicsMode(32, 24, 8, 4), 2, 0, GraphicsContextFlags.Debug)
        {
            #if DEBUG
                Program.logger.Info("UI: Creating an OpenGL 2.1+ GLCanvas");
            #endif

        }


        public override void OnLoad(object sender, EventArgs e)
        {
            GraphicsContext.CurrentContext.VSync = true; // this prevents us using 100% GPU/CPU.
            m_bLoaded = true;           // So we know we have a valid Loaded OpenGL context.

            #if DEBUG
                Program.logger.Info("OpenGL Pre State Config Error Check: " + GL.GetError().ToString());
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
            GL.ClearColor(System.Drawing.Color.MidnightBlue);
            GL.ClearDepth(1.0);
            GL.ClearStencil(0);
            GL.Enable(EnableCap.VertexArray);
            #if DEBUG
                Program.logger.Info("OpenGL Post State Config Error Check: " + GL.GetError().ToString());
            #endif

            Program.logger.Info("UI: GLCanvas2.X Loaded Successfully, Open GL Version: " + GL.GetString(StringName.Version));
            #if DEBUG
                // Log out OpeGL specific stuff if debug build.
                Program.logger.Info("UI: GLSL Version: " + GL.GetString(StringName.ShadingLanguageVersion));
                Program.logger.Info("UI: Renderer: " + GL.GetString(StringName.Renderer));
                Program.logger.Info("UI: Vender: " + GL.GetString(StringName.Vendor));
                Program.logger.Info("UI: Extensions: " + GL.GetString(StringName.Extensions));
                Program.logger.Info("OpenGL Error Check: " + GL.GetError().ToString());
            #endif

            // Setup Our View Port, this sets Our Projection and View Matricies.
            SetupViewPort(0, 0, this.Size.Width, this.Size.Height);
            // Program.logger.Info("OpenGL post View Setup: " + GL.GetError().ToString());

            m_oShaderProgram = new GLShader();
            //m_oShaderProgram2 = new GLUtilities.GLShader("DefaultVertShader.txt", "SolidColorFragShader.txt");
            m_oQuad = new GLPrimitive(m_oShaderProgram);
            m_oShaderProgram.SetProjectionMatrix(ref m_m4ProjectionMatrix);
            m_oShaderProgram.SetViewMatrix(ref m_m4ViewMatrix);
            //m_oShaderProgram2.SetProjectionMatrix(ref m_m4ProjectionMatrix);
            //m_oShaderProgram2.SetViewMatrix(ref m_m4ViewMatrix);


            Vector3 Pos = new Vector3(220, 220, 0);
            m_oCircle = new GLCircle(m_oShaderProgram, Pos, 120.0f, System.Drawing.Color.Green);

            m_oSW.Start();
        }


        public override void OnPaint(object sender, PaintEventArgs e)
        {
            if (!m_bLoaded)
            {
                return;
            }

            Render();
            //DrawSphere();
            this.Invalidate();
        }

        public override void OnResize(object sender, EventArgs e)
        {
            this.Size = this.Parent.Size;               // Set this controls size to be the same as the parent. This is assuemd to be safe.
            SetupViewPort(0, 0, this.Size.Height, this.Size.Width);  // Setup viewport again.
            this.Invalidate();                                       // Force redraw.
        }

        public override void OnSizeChange(object sender, EventArgs e)
        {
            if (m_bLoaded == true)
            {
                //this.Size = this.Parent.Size;               // Set this controls size to be the same as the parent. This is assuemd to be safe.
                SetupViewPort(0, 0, this.Size.Height, this.Size.Width);  // Setup viewport again.
                m_oShaderProgram.SetProjectionMatrix(ref m_m4ProjectionMatrix);
                m_oShaderProgram.SetViewMatrix(ref m_m4ViewMatrix);
                //this.Invalidate();                                       // Force redraw.
            }
        }

        public override void SetupViewPort(int a_iViewportPosX, int a_iViewportPosY,
                                            int a_iViewportWidth, int a_iViewPortHeight)
        {
            GL.Viewport(a_iViewportPosX, a_iViewportPosY, a_iViewportWidth, a_iViewPortHeight);
            //float aspectRatio = a_iViewportWidth / (float)(a_iViewPortHeight); // Calculate Aspect Ratio.

            // Setup our Projection Matrix, This defines how the 2D image seen on screen is created from our 3d world.
            //m_m4ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0, a_iViewportWidth, a_iViewPortHeight, 0, 0.1f, 100.0f);
            m_m4ProjectionMatrix = new Matrix4(new Vector4((2.0f / a_iViewportWidth), 0, 0, 0),
                                                new Vector4(0, (2.0f / a_iViewPortHeight), 0, 0),
                                                new Vector4(0, 0, 1, 0),
                                                new Vector4(-1, -1, 1, 1));

            // Setup our Model View Matrix i.e. the position and faceing of our camera. We are setting it up to look at (0,0,0) from (0,3,5) with positive y being up.
            m_m4ViewMatrix = Matrix4.Identity;
        }

        /// <summary>
        /// OpenGL 3.0 - use shaders and a 3D drawing method.
        /// </summary>
        public override void Render()
        {
            this.MakeCurrent();                            // Make this Canvas Current, just in case there is more then one canves open.

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); // Clear Back buffer of previous frame.

            m_oQuad.Render(ref m_m4ProjectionMatrix, ref m_m4ViewMatrix);       // render our quad.
            m_oCircle.Render(ref m_m4ProjectionMatrix, ref m_m4ViewMatrix);
            //m_oShader.StopUsing();
            GraphicsContext.CurrentContext.SwapBuffers();
            //#if DEBUG
           //     Program.logger.Info("Draw Colpeted, OpenGL error: " + GL.GetError().ToString());
           // #endif

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
