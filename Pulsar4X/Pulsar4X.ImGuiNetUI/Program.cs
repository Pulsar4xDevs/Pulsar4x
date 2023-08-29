using System;
using ImGuiSDL2CS;
using System.Threading;

namespace Pulsar4X.SDL2UI
{
    public class Program
    {
        static SDL2Window Instance;
        [STAThread]
        public static void Main()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            Instance = new PulsarMainWindow();
            Instance.Run();
            Instance.Dispose();
        }
    }
}
