using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Pulsar4X.WinForms
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Pulsar4X.WinForms.Controls.UIController UIComponentControler = new Controls.UIController();
            Application.Run(Controls.UIController.g_aMainForm);
        }
    }
}
