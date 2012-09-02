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

        private void SubForm_Resize(object sender, EventArgs e)
        {
           // Size newSize = this.Size;
            //newSize.Width -= 42;            // Adjust size width to keet boarders
           // newSize.Height -= 63;           // Adjust size height to keep boarders.
            //MainPanel.Size = newSize;       // Set new MainPanel Size

            foreach (Control control in MainPanel.Controls)
            {
                control.Size = MainPanel.Size; // this will edit the size of any sub forms. there will generall only be the one DraggableTabControl.
            }
        }
    }
}
