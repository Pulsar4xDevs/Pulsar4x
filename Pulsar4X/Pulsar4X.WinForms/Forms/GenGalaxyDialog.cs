using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Pulsar4X.WinForms.ViewModels;
using Pulsar4X.Entities;
using Pulsar4X.Stargen;

namespace Pulsar4X.WinForms.Forms
{
    public partial class GenGalaxyDialog : Form
    {
        StarSystemFactory ssf = new StarSystemFactory(true);

        public GenGalaxyDialog()
        {
            InitializeComponent();
        }

        private void GenerateButton_Click(object sender, EventArgs e)
        {
            int iNoOfSystemsToGenerate = 1000;
            int.TryParse(NoOfSystemsTextBox.Text, out iNoOfSystemsToGenerate);
            int iNoOfSystemsDiv4 = iNoOfSystemsToGenerate / 4;

            GenProgressBar.Minimum = 0;
            GenProgressBar.Maximum = iNoOfSystemsToGenerate;

            for (int i = 0; i < iNoOfSystemsToGenerate; ++i)
            {
                GameState.Instance.StarSystems.Add(ssf.Create(GalaxyNameTextBox.Text + i.ToString()));
                GenProgressBar.Value = i;
            }

        }

    }
}
