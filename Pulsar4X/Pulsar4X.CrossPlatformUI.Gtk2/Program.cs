using System;
using Eto.Forms;
using OpenTK;

namespace Pulsar4X.CrossPlatformUI.Gtk2
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
			//this MUST be the first line in the program or any text + the opengl window will cause it to segfault
			OpenTK.Toolkit.Init ();
			var platform = new Eto.GtkSharp.Platform ();
			platform.Add<GLSurface.IHandler>(() => new GtkGLSurfaceHandler());
			new Pulsar4XApplication(platform).Run();
        }
    }
}
