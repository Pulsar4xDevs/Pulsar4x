using Eto;
using Eto.Forms;
using Pulsar4X.ViewModel;
using System;

namespace Pulsar4X.CrossPlatformUI
{
    public class Pulsar4XApplication : Application
    {
        private GameVM Game;

        public Pulsar4XApplication(Platform platform)
			: base(platform)
		{
            this.Name = "Pulsar4X";
            this.Style = "application";
        }

        protected override void OnInitialized(EventArgs e)
        {
            Game = new GameVM();
            MainForm = new MainForm(Game);
            base.OnInitialized(e);

            MainForm.Show();
        }
    }
}
