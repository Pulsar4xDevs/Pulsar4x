/*
using Eto.Drawing;
using Eto.Forms;
using Eto.GtkSharp;
using Eto.GtkSharp.Forms;
using Eto;
using OpenTK.Graphics;
using OpenTK;

namespace Pulsar4X.CrossPlatformUI.Mac
{
	public class MacGLSurfaceHandler : GtkControl<MacGLSurface, GLSurface, GLSurface.ICallback>, GLSurface.IHandler
	{
		private GraphicsMode mode;
		private int major;
		private int minor;
		private GraphicsContextFlags flags;

		protected override void Initialize()
		{
			Toolkit.Init();
			var c = new MacGLSurface(mode, major, minor, flags);
			c.RenderFrame += (sender, args) => base.Callback.OnDrawNow(Widget, args);
			c.Initialized += (sender, args) => base.Callback.OnInitialized(Widget, args);
			c.ShuttingDown += (sender, args) => base.Callback.OnShuttingDown(Widget, args);
			this.Control = c;

			base.Initialize();
		}

		public void CreateWithParams(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags)
		{
			this.mode = mode;
			this.major = major;
			this.minor = minor;
			this.flags = flags;
		}

		public Size GLSize
		{
			get { return Control.GLSize; }
			set { this.Control.GLSize = value; }
		}

		public bool IsInitialized
		{
			get { return Control.IsInitialized; }
		}

		public void MakeCurrent()
		{
			this.Control.MakeCurrent();
		}

		public void SwapBuffers()
		{
			this.Control.SwapBuffers();
		}

		public override Color BackgroundColor
		{
			get
			{
				return this.BackgroundColor;
			}
			set
			{
				this.BackgroundColor = value;
			}
		}
	}
}

*/