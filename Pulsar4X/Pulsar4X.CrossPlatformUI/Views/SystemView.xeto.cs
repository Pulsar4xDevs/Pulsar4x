using Eto.Drawing;
using Eto.Forms;
using Eto.Serialization.Xaml;
using OpenTK;
using OpenTK.Graphics;
using Pulsar4X.ViewModel;
using Pulsar4X.ViewModel.SystemView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

//#TODO Move the actual rendering stuff out into a partial and separate it from the VMs
//I might also want to make windows render with DirectX so that it plays nicer with wpf, 
//so we'll have to see if we can abstract that too
namespace Pulsar4X.CrossPlatformUI.Views {
	public class SystemView : Panel {
		protected Panel RenderCanvasLocation;
		//protected RenderCanvas RenderCanvas;
		private UITimer timDraw;

		protected DropDown systems;

		protected SystemVM CurrentSystem;
		public RenderVM RenderVM { get; set; }

		private bool mouse_held = false;
		private bool continue_drag = false;
		private Vector2 mouse_held_position;
		private Vector2 mouse_released_position;
		private const float mouse_move_threshold = 20f;
		
		private OpenGLRenderer Renderer;
        protected SystemMap_DrawableView SysMapDrawable;

		public SystemView(StarSystemVM viewmodel) {
			
			DataContext = viewmodel;			
			XamlReader.Load(this);

            #region OpenGL stuff
            //intend to make opengl vs drawable system view an option. an openGL pannel could be a bit fancier. 

            //RenderVM = new RenderVM(GameVM.CurrentAuthToken);
            //Renderer = new OpenGLRenderer(RenderVM);
            //RenderCanvas = new RenderCanvas(GraphicsMode.Default, 3, 3, GraphicsContextFlags.Default);
            //systems.BindDataContext(s => s.DataStore, (GameVM g) => g.StarSystems);
            //systems.ItemTextBinding = Binding.Property((SystemVM vm) => vm.Name);
            //systems.ItemKeyBinding = Binding.Property((SystemVM vm) => vm.ID).Convert((Guid ID) => ID.ToString());

            //direct binding - might need to be replaced later
            //systems.Bind(s => s.SelectedValue, RenderVM, (RenderVM rvm) => rvm.ActiveSystem);

            // @Hack: so far the only way I've found to make the RenderCanvas properly update the
            //        size information when the window is resized is to completely detach it from
            //        the window, and then re-attach it, before calling Resize.
            //
            //        There's nothing 'right' about doing it this way, except that it works.
            //   SizeChanged += (sender, args) =>
            //   {
            //       RenderCanvasLocation.Remove(RenderCanvas);
            //       RenderCanvasLocation.Content = RenderCanvas;
            //       Resize(sender, args);
            //   };

            //RenderCanvas.GLInitalized += Initialize;
            //RenderCanvas.GLDrawNow += DrawNow;
            //RenderCanvas.GLShuttingDown += Teardown;
            ////RenderCanvas.GLResize += Resize; // replaced by the @Hack above ^^^
            //RenderCanvas.MouseMove += WhenMouseMove;
            //RenderCanvas.MouseDown += WhenMouseDown;
            //RenderCanvas.MouseUp += WhenMouseUp;
            //RenderCanvas.MouseWheel += WhenMouseWheel;
            //RenderCanvas.MouseLeave += WhenMouseLeave;

            //RenderCanvasLocation.Content = RenderCanvas;
            //RenderCanvasLocation.Content = SysMapDrawable;
            //SysMapDrawable = new SystemMap_DrawableView(GameVM.StarSystemViewModel.SelectedSystemVM);
            #endregion


            systems.DataContext = viewmodel.StarSystems;
            systems.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            systems.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
            
            SysMapDrawable.SetViewmodel(viewmodel.SelectedSystemVM);
            
        }

		private void WhenMouseLeave(object sender, MouseEventArgs e) {
			e.Handled = true;
			mouse_held = false;
			continue_drag = false;
		}

		private void WhenMouseWheel(object sender, MouseEventArgs e) {
			e.Handled = true;
			RenderVM.UpdateCameraZoom((int)e.Delta.Height);
		}

		private void WhenMouseUp(object sender, MouseEventArgs e) {
			e.Handled = true;
			mouse_held = false;
			continue_drag = false;
		}

		private void WhenMouseDown(object sender, MouseEventArgs e) {
			e.Handled = true;
			mouse_held = true;
			mouse_held_position.X = e.Location.X;
			mouse_held_position.Y = e.Location.Y;
		}

		private void WhenMouseMove(object sender, MouseEventArgs e) {
			e.Handled = true;
			if (mouse_held) {
				Vector2 mouse_pos = new Vector2(e.Location.X, e.Location.Y);
				var delta = mouse_pos - mouse_held_position;
				if (delta.Length > mouse_move_threshold || continue_drag) {
					continue_drag = true;
					RenderVM.UpdateCameraPosition(delta);
					mouse_held_position = mouse_pos;
				}
			}
		}

		private void Draw() {
			RenderVM.drawPending = true;
		}

		public void Initialize(object sender, EventArgs e) {
			//RenderCanvas.MakeCurrent();
			//var bounds = RenderCanvas.Bounds;
			//Renderer.Initialize(bounds.X, bounds.Y, bounds.Width, bounds.Height);

			////we need this to run on its own because we cant have rendering blocked by the
			////the rest of the system or waiting for an advance time command
			//timDraw = new UITimer { Interval = 0.013 }; // Every Millisecond.
			//timDraw.Elapsed += timDraw_Elapsed;
			//timDraw.Start();
		}

        private void timDraw_Elapsed(object sender, EventArgs e)
        {
            SysMapDrawable.Invalidate();
        }

        //private void timDraw_Elapsed(object sender, EventArgs e) {
        //	if (!RenderVM.drawPending || !RenderCanvas.IsInitialized) {
        //		return;
        //	}

        //RenderCanvas.MakeCurrent();

        //Renderer.Draw(RenderVM);

        //RenderCanvas.SwapBuffers();

        //RenderVM.drawPending = false;
        //}

        public void DrawNow(object sender, EventArgs e) {
			Draw();
			
		}

		public void Resize(object sender, EventArgs e) {
			//RenderCanvas.MakeCurrent();
			//var bounds = RenderCanvas.Bounds;
			//RenderVM.Resize(bounds.Width, bounds.Height);
			//Renderer.Resize(bounds.X, bounds.Y, bounds.Width, bounds.Height);
			//RenderVM.drawPending = true;
		}

		public void Teardown(object sender, EventArgs e) {
			Renderer.Destroy();
		}
	}
}
