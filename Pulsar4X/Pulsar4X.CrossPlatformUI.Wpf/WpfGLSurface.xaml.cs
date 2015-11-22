using System;
using OpenTK;
using OpenTK.Graphics;
using Eto;
namespace Pulsar4X.CrossPlatformUI.Wpf
{
    /// <summary>
    /// Interaction logic for WpfUserControl.xaml
    /// </summary>
    public partial class WpfGLSurface : System.Windows.Controls.UserControl
    {
        public event EventHandler ShuttingDown = delegate { };
        public GLControl glc;
        readonly GraphicsMode graphicsMode;
        readonly int major;
        readonly int minor;
        readonly GraphicsContextFlags flags;

        /// <summary>
        /// Constructs a new instance with the specified GraphicsMode.
        /// </summary>
        /// <param name="mode">The OpenTK.Graphics.GraphicsMode of the control.</param>
        /// <param name="major">The major version for the OpenGL GraphicsContext.</param>
        /// <param name="minor">The minor version for the OpenGL GraphicsContext.</param>
        /// <param name="flags">The GraphicsContextFlags for the OpenGL GraphicsContext.</param>
        public WpfGLSurface(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags, RenderCanvas Widget)
        {
            if (mode == null)
                throw new ArgumentNullException("mode");

            // SDL does not currently support embedding
            // on external windows. If Open.Toolkit is not yet
            // initialized, we'll try to request a native backend
            // that supports embedding.
            // Most people are using GLControl through the
            // WinForms designer in Visual Studio. This approach
            // works perfectly in that case.
            Toolkit.Init(new ToolkitOptions
            {
                Backend = PlatformBackend.PreferNative
            });


            this.graphicsMode = mode;
            this.major = major;
            this.minor = minor;
            this.flags = flags;

            this.
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
