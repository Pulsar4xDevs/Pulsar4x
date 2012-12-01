using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Pulsar4X.Stargen;
using log4net.Config;
using log4net;

namespace Pulsar4X.UI
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
            XmlConfigurator.Configure(); // Enables Log4net based on App.config file.
            logger.Info("Program Started");

            // gen star system:
            var ssf = new StarSystemFactory(true);
            GameState.Instance.StarSystems.Add(ssf.Create("Test"));
            GameState.Instance.StarSystems.Add(ssf.Create("Foo"));
            GameState.Instance.StarSystems.Add(ssf.Create("Bar"));

            Application.EnableVisualStyles();

            // Init our UI Controller:
            Helpers.UIController.Instance.Initialise();
            //Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Forms.MainForm());

            logger.Info("Program Ended");
        }
    }
}
