﻿using Eto.Forms;
using Eto.Serialization.Json;
using OpenTK;
using OpenTK.Graphics;
using Pulsar4X.ViewModel;
using System;

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
        private bool mouse_held = false;
        private bool continue_drag = false;
        private Vector2 mouse_held_position;
        private Vector2 mouse_released_position;
        private const float mouse_move_threshold = 20f;

        protected Splitter Body;

        protected RenderCanvas RenderCanvas;

        private UITimer timDraw;

		private OpenGLRenderer Renderer;

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
			RenderCanvas.MouseMove += WhenMouseMove;
            RenderCanvas.MouseDown += WhenMouseDown;
            RenderCanvas.MouseUp += WhenMouseUp;
            RenderCanvas.MouseWheel += WhenMouseWheel;
            RenderCanvas.MouseLeave += WhenMouseLeave;
        }

        private void WhenMouseLeave(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            mouse_held = false;
            continue_drag = false;
        }

        private void WhenMouseWheel(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            RenderVM.UpdateCameraZoom((int)e.Delta.Height);
        }

        private void WhenMouseUp(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            mouse_held = false;
            continue_drag = false;
        }

        private void WhenMouseDown(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            mouse_held = true;
            mouse_held_position.X = e.Location.X;
            mouse_held_position.Y = e.Location.Y;
        }

        private void WhenMouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            if(mouse_held)
            {
                Vector2 mouse_pos = new Vector2(e.Location.X, e.Location.Y);
                var delta = mouse_pos - mouse_held_position;
                if (delta.Length > mouse_move_threshold || continue_drag)
                {
                    continue_drag = true;
                    RenderVM.UpdateCameraPosition(delta);
                    mouse_held_position = mouse_pos;
                }
            }
        }

        private void Draw()
        {
            RenderVM.drawPending = true;
        }

		public void Initialize(object sender, EventArgs e) {
            RenderCanvas.MakeCurrent();
            var bounds = RenderCanvas.Bounds;
            Renderer.Initialize(bounds.X, bounds.Y, bounds.Width, bounds.Height);

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
            var bounds = RenderCanvas.Bounds;
			RenderVM.Resize(bounds.Width, bounds.Height);
            Renderer.Resize(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            RenderVM.drawPending = true;
        }

        public void Teardown(object sender, EventArgs e)
        {
			Renderer.Destroy();
        }
    }
}
