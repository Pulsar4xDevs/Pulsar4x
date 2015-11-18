using Eto.Drawing;
using Eto.Forms;
using Eto.GtkSharp;
using Eto.GtkSharp.Forms;
using Eto;
using OpenTK.Graphics;
using OpenTK;

namespace Pulsar4X.CrossPlatformUI.Gtk2
{
	public class GtkGLSurfaceHandler : GtkControl<GtkGLSurface, GLSurface, GLSurface.ICallback>, GLSurface.IHandler
	{
		private GraphicsMode mode;
		private int major;
		private int minor;
		private GraphicsContextFlags flags;

		protected override void Initialize()
		{
			var c = new GtkGLSurface(mode, major, minor, flags, Widget);
			c.glc.Paint += (sender, args) => base.Callback.OnDrawNow(Widget, args);
			c.glc.Resize += (sender, args) => base.Callback.OnResize(Widget, args);
			c.glc.Load += (sender, args) => base.Callback.OnInitialized(Widget, args);
			c.glc.Disposed += (sender, args) => base.Callback.OnShuttingDown(Widget, args);
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
			get { return this.Control.GLSize; }
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

		public override void AttachEvent(string id)
		{
			switch (id)
			{
			case GLSurface.GLInitializedEvent:
				this.Control.Initialized += (sender, args) => Callback.OnInitialized(this.Widget, args);
				break;

			case GLSurface.GLShuttingDownEvent:
				this.Control.ShuttingDown += (sender, args) => Callback.OnShuttingDown(this.Widget, args);
				break;

			default:
				base.AttachEvent(id);
				break;
			}
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

