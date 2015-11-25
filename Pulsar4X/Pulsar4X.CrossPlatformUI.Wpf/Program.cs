using System;

namespace Pulsar4X.CrossPlatformUI.Wpf
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var platform = new Eto.Wpf.Platform();
            platform.Add<RenderCanvas.IHandler>(() => new WinGLSurfaceHandler());
            new Pulsar4XApplication(platform).Run();
        }
    }
}
