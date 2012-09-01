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
            // Create Splash Scrren:
            Forms.StartupSplashScreen.ShowSplashScreen();
            Forms.StartupSplashScreen.SetStatus("Testing..."); // Update Splash Scrren Status

            // for testing splash screen:
            Thread.Sleep(2500);

            AppDomain.CurrentDomain.FirstChanceException += new EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs>(CurrentDomain_FirstChanceException);

            Forms.StartupSplashScreen.SetStatus("Loading Configuration...");
            Forms.StartupSplashScreen.Progress = 0.2;
            XmlConfigurator.Configure();

            logger.Info("Program Started");

            Forms.StartupSplashScreen.SetStatus("Initialising Controls...");
            Forms.StartupSplashScreen.Progress = 0.4;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Pulsar4X.WinForms.Controls.UIController UIComponentControler = new Controls.UIController();
            Forms.StartupSplashScreen.SetStatus("Testing For OpenGL...");
            Forms.StartupSplashScreen.Progress = 0.6;
            bool bOpenTKInitOK = OpenTKUtilities.Instance.Initialise();  // Get the best possible version of OpenGL
            //bool bOpenTKInitOK = OpenTKUtilities.Instance.Initialise(OpenTKUtilities.GLVersion.OpenGL2X); // force GL2.0
            if (bOpenTKInitOK == false)
            {
                // Log error with open TK:
                logger.Warn("Error Initialising OpenTK and OpenGL. System and Glaaxy Maps May not work correctly!");
            }

            Forms.StartupSplashScreen.SetStatus("Testing...");
            Forms.StartupSplashScreen.Progress = 0.8;
            // for testing splash screen:
            Thread.Sleep(2500);
            // Close splash screen:
            Forms.StartupSplashScreen.SetStatus("Starting...");
            Forms.StartupSplashScreen.Progress = 1.0;
            Forms.StartupSplashScreen.CloseForm();
            Application.Run(Controls.UIController.g_aMainForm);
            
            /*
            StarSystemFactory ssf = new StarSystemFactory(true);
            ssf.Create("Test");
            */

            logger.Info("Program Ended");
        }

        static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            logger.Error("First chance exception!", e.Exception);
        }
    }
}
