using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Pulsar4X.WinForms.Forms
{
    public partial class AboutBox : Form
    {
        public AboutBox()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            this.Hide();
            this.Close();
        }

        private void AboutBox_Load(object sender, EventArgs e)
        {
            AboutTextBox.Text = UIConstants.ABOUT_BOX_TEXT;
        }
    }
}
