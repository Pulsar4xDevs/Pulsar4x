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
            if (!Data.SaveData())
                MessageBox.Show("Error occured during saving. Data could be corrupted.");
            else
                MessageBox.Show("Saved.");
        }

        private void loadFileButton_Click(object sender, EventArgs e)
        {
            if(openFileDialog.ShowDialog() == DialogResult.OK)
            {
                Data.LoadFile(openFileDialog.FileName);
            }
        }
    }
}
