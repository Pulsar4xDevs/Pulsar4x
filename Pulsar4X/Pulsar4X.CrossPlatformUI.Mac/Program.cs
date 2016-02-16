using System;
using Eto.Forms;

namespace Pulsar4X.CrossPlatformUI.Mac
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
			var platform = new Eto.Mac.Platform ();
			platform.Add<GLSurface.IHandler>(() => new MacGLSurfaceHandler());
			new Application(platform).Run(new MainForm());
        }
    }
}
