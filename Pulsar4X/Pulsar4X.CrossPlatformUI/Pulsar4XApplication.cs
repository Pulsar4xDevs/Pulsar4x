using Eto;
using Eto.Forms;
using System;

namespace Pulsar4X.CrossPlatformUI
{
    public class Pulsar4XApplication : Application
    {
        public new MainForm MainForm;

        public Pulsar4XApplication(Platform platform)
			: base(platform)
		{
            Name = "Pulsar4X";
            Style = "application";
        }

        protected override void OnInitialized(EventArgs e)
        {
            MainForm = new MainForm();
            base.OnInitialized(e);

            MainForm.Show();
        }
    }
}
