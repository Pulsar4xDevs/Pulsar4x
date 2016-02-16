using Eto.Drawing;
using Eto.Wpf;
using Eto.Wpf.Forms;
using OpenTK.Graphics;
using System;
using Keys = Eto.Forms.Keys;
using MouseButtons = Eto.Forms.MouseButtons;
using MouseEventArgs = Eto.Forms.MouseEventArgs;

namespace Pulsar4X.CrossPlatformUI.Wpf
{
    public class WinGLSurfaceHandler : WpfFrameworkElement<WpfGLSurface, RenderCanvas, RenderCanvas.ICallback>, RenderCanvas.IHandler
    {
        private GraphicsMode mode;
        private int major;
        private int minor;
        private GraphicsContextFlags flags;

        protected override void Initialize()
        {
            var c = new WpfGLSurface(mode, major, minor, flags, Widget);
            c.glc.Paint += (sender, args) => base.Callback.OnDrawNow(Widget, args);
            c.glc.Resize += (sender, args) => base.Callback.OnResize(Widget, args);
            c.glc.Load += (sender, args) => base.Callback.OnInitialized(Widget, args);
            c.glc.Disposed += (sender, args) => base.Callback.OnShuttingDown(Widget, args);
            c.glc.MouseMove += WhenMouseMove;
            c.glc.MouseDown += WhenMouseDown;
            c.glc.MouseUp += WhenMouseUp;
            c.glc.MouseWheel += WhenMouseWheel;
            c.glc.MouseLeave += WhenMouseLeave;
            this.Control = c;

            base.Initialize();
        }

        private void WhenMouseLeave(object sender, EventArgs e)
        {
            Callback.OnMouseLeave(Widget, new MouseEventArgs(MouseButtons.None, Keys.None, new PointF(0, 0)));
        }

        private void WhenMouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Callback.OnMouseWheel(Widget, new MouseEventArgs(MouseButtons.None, Keys.None, new PointF(e.Location.X, e.Location.Y), new SizeF(e.Delta/120, e.Delta/120)));
        }

        private void WhenMouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            MouseButtons button;
            Enum.TryParse<MouseButtons>(e.Button.ToString(), out button);
            Callback.OnMouseUp(Widget, new MouseEventArgs(button, Keys.None, new PointF(e.Location.X, e.Location.Y)));
        }

        private void WhenMouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            MouseButtons button;
            Enum.TryParse<MouseButtons>(e.Button.ToString(), out button);
            Callback.OnMouseDown(Widget, new MouseEventArgs(button, Keys.None, new PointF(e.Location.X, e.Location.Y)));
        }

        private void WhenMouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Callback.OnMouseMove(Widget, new MouseEventArgs(MouseButtons.None, Keys.None, new PointF(e.Location.X, e.Location.Y)));
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
            set { this.Control.SetSize(value.ToWpf()); }
        }

        public bool IsInitialized
        {
            get { return Control.IsInitialized; }
        }

        public void MakeCurrent()
        {
            this.Control.MakeCurrent();
        }

        public void Refresh()
        {
            this.Control.glc.Refresh();
        }

        public void SwapBuffers()
        {
            this.Control.SwapBuffers();
        }

        public override void AttachEvent(string id)
        {
            switch (id)
            {
                case RenderCanvas.GLInitializedEvent:
                    this.Control.Initialized += (sender, args) => Callback.OnInitialized(this.Widget, args);
                    break;

                case RenderCanvas.GLShuttingDownEvent:
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