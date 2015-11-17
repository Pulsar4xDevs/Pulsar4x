using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Json;
using Pulsar4X.ViewModel;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class SystemView : Panel
    {
        protected ListBox Systems;
        protected SystemVM CurrentSystem;

        protected Splitter Body;

        protected GLSurface gl_context;

        private UITimer timDraw;

        private bool drawPending = false;

        public SystemView(GameVM GameVM)
        {
            DataContext = GameVM;
            gl_context = new GLSurface();
            JsonReader.Load(this);
            Systems.BindDataContext(c => c.DataStore, (GameVM c) => c.StarSystems);
            Systems.ItemTextBinding = Binding.Property((SystemVM vm) => vm.Name);
            Systems.ItemKeyBinding = Binding.Property((SystemVM vm) => vm.ID).Convert((Guid ID) => ID.ToString());
            Systems.SelectedIndexChanged += loadSystem;
            //var panel = new TableLayout();
            //panel.Rows.Add(new TableRow(new TableCell(GlContext)));
            Body.Panel2 = gl_context;
            gl_context.GLInitalized += InitializeCanvas;
            gl_context.GLDrawNow += DrawNow;
            gl_context.GLShuttingDown += TeardownCanvas;
            gl_context.GLResize += Resize;
        }

        private void Draw()
        {
            drawPending = true;
        }

        private void timDraw_Elapsed(object sender, EventArgs e)
        {
            if (!drawPending || !gl_context.IsInitialized)
            {
                return;
            }

            gl_context.MakeCurrent();

            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0F);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.Flush();
            gl_context.SwapBuffers();

            drawPending = false;
        }

        public void loadSystem(object sender, EventArgs e)
        {
            CurrentSystem = (SystemVM)((ListBox)sender).SelectedValue;
            Draw();
        }

        public void InitializeCanvas(object sender, EventArgs e)
        {
            gl_context.MakeCurrent();

            GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            GL.ClearColor(0.0F, 0.0F, 0.0F, 1.0F);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.ShadeModel(ShadingModel.Smooth);
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
            GL.Enable(EnableCap.DepthTest);

            timDraw = new UITimer { Interval = 0.013 }; // Every Millisecond.
            timDraw.Elapsed += timDraw_Elapsed;
            timDraw.Start();
        }

        public void DrawNow(object sender, EventArgs e)
        {
            Draw();
        }

        public void Resize(object sender, EventArgs e)
        {
            Draw();
        }

        public void TeardownCanvas(object sender, EventArgs e)
        {
        }
    }
}
