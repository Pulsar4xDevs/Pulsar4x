using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Pulsar4X.Stargen;
using log4net.Config;
using log4net;

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
            AppDomain.CurrentDomain.FirstChanceException += new EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs>(CurrentDomain_FirstChanceException);

            XmlConfigurator.Configure();

            logger.Info("Program Started");
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Pulsar4X.WinForms.Controls.UIController UIComponentControler = new Controls.UIController();
            //bool bOpenTKInitOK = OpenTKUtilities.Instance.Initialise();  // Get the best possible version of OpenGL
            bool bOpenTKInitOK = OpenTKUtilities.Instance.Initialise(OpenTKUtilities.GLVersion.OpenGL2X); // force GL2.0
            if (bOpenTKInitOK == false)
            {
                // Log error with open TK:
                logger.Warn("Error Initialising OpenTK and OpenGL. System and Glaaxy Maps May not work correctly!");
            }

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
