using System;
using Eto.Forms;

namespace Pulsar4X.CrossPlatformUI.Gtk2
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
			var platform = new Eto.GtkSharp.Platform ();
			platform.Add<GLSurface.IHandler>(() => new GtkGLSurfaceHandler());
			new Application(platform).Run(new MainForm());
        }
    }
}
