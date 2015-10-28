using System;
using Eto.Forms;

namespace Pulsar4X.CrossPlatformUI.Desktop
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            new Application(Eto.Platform.Detect).Run(new MainForm());
        }
    }
}
