using System;
using Eto.Forms;

namespace Pulsar4X.CrossPlatformUI.Wpf
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var platform = new Eto.Wpf.Platform();
            platform.Add<GLSurface.IHandler>(() => new WinGLSurfaceHandler());
            new Application(platform).Run(new MainForm());
        }
    }
}
