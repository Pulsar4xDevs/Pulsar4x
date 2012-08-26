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
    ///// <summary>
    ///// A customised version of OpenTK GLControl Class, it guarantees that OpenGL version 1.X is availble.
    ///// </summary>
    //class GLCanves10 : GLCanvas
    //{
    //    public GLCanves10() : base(new GraphicsMode(32, 24, 8, 0), 1, 5)
    //    {
    //    }


    //    public override void OnLoad(object sender, EventArgs e)
    //    {
    //        // The Following Enables OpenGL depth Test, this will prevent OpenGL from drawing 
    //        // anything which is obscured by by a previosly draw pixel.
    //        GL.Enable(EnableCap.DepthTest);


    //        m_bLoaded = true;           // So we know we have a valid Loaded OpenGL context.
    //    }
        
    //    public override void OnPaint(object sender, PaintEventArgs e)
    //    {
    //        if (!m_bLoaded)
    //        {
    //            return;
    //        }

    //        Render();
    //        this.Invalidate();
    //    }

    //    /// <summary>
    //    /// OpenGL 1.0 - use immediate mode 2D drawing method.
    //    /// </summary>
    //    public override void Render()
    //    {
    //        this.MakeCurrent();                                                         // Make this Canvas Current, just in case there is more then one canves open.
    //        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);  // Clear the back buffer.

    //        //GL.MatrixMode(MatrixMode.Modelview);                                        // Set the Matrix Mode.
    //        //GL.LoadIdentity();                                                          // Load Identitty matrix
    //        //GL.Translate(v2Speed.X, v2Speed.Y, 0);                                      // translate Object.
    //        //if (glGalaxyMap.Focused)                                                    // 
    //        //{
    //        //    GL.Color3(Color.Yellow);
    //        //}
    //        //else
    //        //{
    //        //    GL.Color3(Color.Blue);
    //        //}
    //        //GL.Begin(BeginMode.Triangles);
    //        //GL.Vertex2(100, 20);
    //        //GL.Vertex2(200, 20);
    //        //GL.Vertex2(200, 50);
    //        //GL.End();

    //        GraphicsContext.CurrentContext.SwapBuffers();
    //    }

    //    //public override void PreRenderPlanet(float a_fRadius)
    //    //{
    //    //}

    //    public override void TestFunc(int a_itest)
    //    {
    //    }
    //}
}
