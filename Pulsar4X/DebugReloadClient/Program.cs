using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MyvarReload;
using Pulsar4X.SDL2UI;

namespace DebugReloadClient
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var filpth = Path.GetFullPath("../../../Pulsar4X.ImGuiNetUI/bin/Debug/net47/Pulsar4X.ImGuiNetUI.exe");
            var ass = new ReloadAssembly(filpth);

            dynamic pmw = ass.NewType<PulsarMainWindow>();
            

            pmw.Run();
            pmw.Dispose();
            

        }
    }
}