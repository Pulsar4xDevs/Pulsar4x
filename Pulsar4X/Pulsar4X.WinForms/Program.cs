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

            XmlConfigurator.Configure();

            logger.Info("Program Started");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Pulsar4X.WinForms.Controls.UIController UIComponentControler = new Controls.UIController();
            Application.Run(Controls.UIController.g_aMainForm);


            logger.Info("Program Ended");
        }
    }
}
