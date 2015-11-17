using System;
using OpenTK;
using OpenTK.Graphics;
using Eto;
namespace Pulsar4X.CrossPlatformUI.Wpf
{
    /// <summary>
    /// Interaction logic for WpfUserControl.xaml
    /// </summary>
    public partial class WpfUserControl : System.Windows.Controls.UserControl
    {
        public event EventHandler ShuttingDown = delegate { };
        private GraphicsMode mode;
        private int major;
        private int minor;
        private GraphicsContextFlags flags;
        public GLControl glc;
        private GLSurface Widget;

        public WpfUserControl(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags, GLSurface Widget)
        {
            // TODO: Complete member initialization
            this.mode = mode;
            this.major = major;
            this.minor = minor;
            this.flags = flags;
            this.Widget = Widget;
            InitializeComponent();
            glc = new GLControl(mode, major, minor, flags);
            glc.Load += (sender, args) => {
                Widget.OnGLInitalized(args);
            };
            glControl.Child = glc;
        }

        public Eto.Drawing.Size GLSize
        {
            get
            {
                return glc.Size.ToEto();
            }
            set
            {
                glc.Size = value.ToSD();
            }
        }

        internal void MakeCurrent()
        {
            glc.MakeCurrent();
        }

        internal void SwapBuffers()
        {
            glc.SwapBuffers();
        }
    }
}
