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

//#TODO Move the actual rendering stuff out into a partial and separate it from the VMs
//I might also want to make windows render with DirectX so that it plays nicer with wpf, 
//so we'll have to see if we can abstract that too
namespace Pulsar4X.CrossPlatformUI.Views
{
    public class SystemView : Panel
    {
        protected ListBox Systems;
        protected SystemVM CurrentSystem;
        protected RenderVM RenderVM;

        protected Splitter Body;

        protected RenderCanvas RenderCanvas;

        private UITimer timDraw;

		private OpenGLRenderer Renderer;

        private SystemViewModel svm;

        public SystemView(GameVM GameVM)
        {
            RenderVM = new RenderVM();
            Renderer = new OpenGLRenderer(RenderVM);
            DataContext = GameVM;
            RenderCanvas = new RenderCanvas(GraphicsMode.Default, 3, 3, GraphicsContextFlags.Default);
            JsonReader.Load(this);

            Systems.BindDataContext(s => s.DataStore, (GameVM g) => g.StarSystems);
            Systems.ItemTextBinding = Binding.Property((SystemVM vm) => vm.Name);
            Systems.ItemKeyBinding = Binding.Property((SystemVM vm) => vm.ID).Convert((Guid ID) => ID.ToString());
            
            //direct binding - might need to be replaced later
            Systems.Bind(s => s.SelectedValue, RenderVM, (RenderVM rvm) => rvm.ActiveSystem);

            Body.Panel2 = RenderCanvas;
            RenderCanvas.GLInitalized += Initialize;
            RenderCanvas.GLDrawNow += DrawNow;
            RenderCanvas.GLShuttingDown += Teardown;
            RenderCanvas.GLResize += Resize;
			RenderCanvas.MouseMove += Gl_context_MouseMove;
        }

        void Gl_context_MouseMove (object sender, MouseEventArgs e)
        {
			Console.WriteLine (e.Location);
        }

        private void Draw()
        {
            RenderVM.drawPending = true;
        }

		public void Initialize(object sender, EventArgs e) {
            RenderCanvas.MakeCurrent();
            Renderer.Initialize(RenderCanvas.GLSize.Width, RenderCanvas.GLSize.Height);

            //we need this to run on its own because we cant have rendering blocked by the
            //the rest of the system or waiting for an advance time command
            timDraw = new UITimer { Interval = 0.013 }; // Every Millisecond.
			timDraw.Elapsed += timDraw_Elapsed;
			timDraw.Start();
		}

		private void timDraw_Elapsed(object sender, EventArgs e)
		{
			if (!RenderVM.drawPending || !RenderCanvas.IsInitialized)
			{
				return;
			}

			RenderCanvas.MakeCurrent();

			Renderer.Draw(RenderVM);

			RenderCanvas.SwapBuffers();

			RenderVM.drawPending = false;
		}

        public void DrawNow(object sender, EventArgs e)
        {
            Draw();
        }

        public void Resize(object sender, EventArgs e)
        {
			RenderCanvas.MakeCurrent();
			Renderer.Resize(RenderCanvas.GLSize.Width, RenderCanvas.GLSize.Height);
        }

        public void Teardown(object sender, EventArgs e)
        {
			Renderer.Destroy();
        }
    }

    struct SystemViewModel
    {
        public ObservableCollection<SystemVM> systems;
        public RenderVM render_data;

        public SystemViewModel(GameVM g, RenderVM r)
        {
            systems = g.StarSystems;
            render_data = r;
        }
    }
}
