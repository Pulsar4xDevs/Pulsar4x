using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Pulsar4X.UI.ViewModels;
using Pulsar4X.Entities;
using Pulsar4X.Stargen;

namespace Pulsar4X.UI.Dialogs
{
    public partial class GenGalaxyDialog : Form
    {
        StarSystemFactory ssf = new StarSystemFactory(true);

        System.Diagnostics.Stopwatch m_oSW = new System.Diagnostics.Stopwatch();

        public GenGalaxyDialog()
        {
            InitializeComponent();
        }

        private void GenerateButton_Click(object sender, EventArgs e)
        {
            m_oSW.Start();

            int iNoOfSystemsToGenerate = 1000;
            int.TryParse(NoOfSystemsTextBox.Text, out iNoOfSystemsToGenerate);
            int iNoOfSystemsDiv4 = iNoOfSystemsToGenerate / 4;

            GenProgressBar.Minimum = 0;
            GenProgressBar.Maximum = iNoOfSystemsToGenerate;

            for (int i = 0; i < iNoOfSystemsToGenerate; ++i)
            {
                ssf.Create(GalaxyNameTextBox.Text + i.ToString());
                GameState.Instance.StarSystemCurrentIndex++;
                GenProgressBar.Value = i;
            }

            m_oSW.Stop();
            Timelabel.Text = m_oSW.Elapsed.Hours.ToString() + ":" + m_oSW.Elapsed.Minutes.ToString() + ":" + m_oSW.Elapsed.Seconds.ToString() + ":" + m_oSW.Elapsed.Milliseconds.ToString();
        }

        private void GenGalaxyDialog_Load(object sender, EventArgs e)
        {

        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
