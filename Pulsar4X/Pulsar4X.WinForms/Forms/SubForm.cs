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
    public partial class SubForm : Form
    {
        public SubForm()
        {
            InitializeComponent();
        }

        public Panel GetMainPanel()
        {
            return MainPanel;
        }

        private void SubForm_Load(object sender, EventArgs e)
        {

        }


    }
}
