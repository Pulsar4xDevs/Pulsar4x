using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModdingTools.JsonDataEditor
{
    public partial class LoadWindow : UserControl
    {
        public LoadWindow()
        {
            InitializeComponent();
        }

        private void saveAllButton_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                Data.SaveDataToDirectory(folderBrowserDialog1.SelectedPath);
            }
            //if (!Data.SaveData())
            //    MessageBox.Show("Error occured during saving. Data could be corrupted.");
            //else
            //    MessageBox.Show("Saved.");
        }

        private void loadFileButton_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                Data.loadDatafromDirectory(folderBrowserDialog1.SelectedPath);
            }
        }

        private void techButton_Click(object sender, EventArgs e)
        {
            Data.MainWindow.SetMode(WindowModes.TechWindow);
        }

        private void installationsButton_Click(object sender, EventArgs e)
        {
            Data.MainWindow.SetMode(WindowModes.InstallationsWindow);
        }

        private void ComponentButton_Click(object sender, EventArgs e)
        {
            Data.MainWindow.SetMode(WindowModes.ComponentsWindow);
        }
    }
}
