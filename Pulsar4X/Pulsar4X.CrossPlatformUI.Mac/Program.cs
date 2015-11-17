using System;
using Eto.Forms;

namespace Pulsar4X.CrossPlatformUI.Mac
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            new Application(Eto.Platforms.Mac).Run(new MainForm());
        }
    }
}
