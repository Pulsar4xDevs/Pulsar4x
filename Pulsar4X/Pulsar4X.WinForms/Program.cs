using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Pulsar4X.Stargen;

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

            var test = Constants.Gasses.H.Name;
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Pulsar4X.WinForms.Controls.UIController UIComponentControler = new Controls.UIController();
            Application.Run(Controls.UIController.g_aMainForm);

            

            double MinAge = 1.0E9;
            double MaxAge = 6.0E9;

            var ssf = new StarSystemFactory(MinAge, MaxAge, true);
            var ss = ssf.Create("Proxima");

        }
    }
}
