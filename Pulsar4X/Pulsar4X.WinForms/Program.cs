using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Pulsar4X.Stargen;
using log4net.Config;
using log4net;
using System.Threading;

namespace Pulsar4X.WinForms
{
    static class Program
    {
        public static readonly ILog logger = LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //AppDomain.CurrentDomain.FirstChanceException += new EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs>(CurrentDomain_FirstChanceException);
            //AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            // Create Splash Scrren:
            #if SPLASHSCREEN
                Forms.StartupSplashScreen.ShowSplashScreen();
                Forms.StartupSplashScreen.SetStatus("Loading Configuration...");
                Forms.StartupSplashScreen.Progress = 0.2;
            #endif

            XmlConfigurator.Configure();

            logger.Info("Program Started");

            #if SPLASHSCREEN
                Forms.StartupSplashScreen.SetStatus("Initializing data...");
                Forms.StartupSplashScreen.Progress = 0.3;
            #endif
            var ssf = new StarSystemFactory(true);
            GameState.Instance.StarSystems.Add(ssf.Create("Test"));
            GameState.Instance.StarSystems.Add(ssf.Create("Foo"));
            GameState.Instance.StarSystems.Add(ssf.Create("Bar"));

            #if SPLASHSCREEN
                Forms.StartupSplashScreen.SetStatus("Initialising Controls...");
                Forms.StartupSplashScreen.Progress = 0.45;
            #endif

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Pulsar4X.WinForms.Controls.UIController UIComponentControler = new Controls.UIController();

            #if SPLASHSCREEN
                Forms.StartupSplashScreen.SetStatus("Testing For OpenGL...");
                Forms.StartupSplashScreen.Progress = 0.7;
            #endif
            #if OPENGL
                bool bOpenTKInitOK = OpenTKUtilities.Instance.Initialise();  // Get the best possible version of OpenGL
                //bool bOpenTKInitOK = OpenTKUtilities.Instance.Initialise(OpenTKUtilities.GLVersion.OpenGL2X); // force GL2.0
                if (bOpenTKInitOK == false)
                {
                    // Log error with open TK:
                    logger.Warn("Error Initialising OpenTK and OpenGL. System and Glaaxy Maps May not work correctly!");
                }
            #endif

            // Starting:
            #if SPLASHSCREEN
                Forms.StartupSplashScreen.SetStatus("Starting...");
                Forms.StartupSplashScreen.Progress = 0.8;
            #endif
            // note that main form will close the splash screen on load!!
            Application.Run(Controls.UIController.g_aMainForm);

            logger.Info("Program Ended");
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Error("Unhandled Exception!", (Exception)e.ExceptionObject);
        }

        static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            logger.Error("First chance exception!", e.Exception);
        }
    }
}
