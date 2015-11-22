using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using Eto;
using Pulsar4X.ViewModel;

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
