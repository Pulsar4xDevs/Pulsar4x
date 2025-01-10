using System;
using System.Reflection;
using ImGuiSDL2CS;
using System.Threading;
using System.Threading.Tasks;
using Pulsar4X.Client.CrashReports;

namespace Pulsar4X.SDL2UI
{
    public class Program
    {
        static SDL2Window? Instance;
        [STAThread]
        public static async Task Main(string[] args)
        {
            // Webhook URL to the #crash-reports channel
            var crashLogger = new DiscordCrashLogger("https://discord.com/api/webhooks/1313608706172125305/pE4jhTyUviwomqfmZcJB-QWayFBwgTVR_o_6SSO_q91c1TI0QKTKNmuBgJl1o0Q7S7Vy");

            try
            {
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                Instance = new PulsarMainWindow();
                Instance.Run();
                Instance.Dispose();
            }
            catch (Exception e)
            {
                // Log the crash
                await crashLogger.LogCrashAsync(e, $"Git Hash: {AssemblyInfo.GetGitHash()}");
                
                // Throw again to allow the local debugger to handle the exception
                throw;
            }
            
        }
    }
}
