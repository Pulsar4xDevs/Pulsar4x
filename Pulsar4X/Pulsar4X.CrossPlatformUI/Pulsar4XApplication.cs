using Eto;
using Eto.Forms;
using System;
using System.Globalization;

namespace Pulsar4X.CrossPlatformUI
{
    public class Pulsar4XApplication : Application
    {
        public Pulsar4XApplication(Platform platform)
			: base(platform)
		{
            Name = "Pulsar4X";
            Style = "application";
        }

        protected override void OnInitialized(EventArgs e)
        {
            //Sets CultureInfo to en-US for the whole application;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CreateSpecificCulture("en-US");

            MainForm = new MainForm();
            base.OnInitialized(e);

            MainForm.Closed += (sender, args) => { Quit(); };
            MainForm.Show();
        }

    }
}
