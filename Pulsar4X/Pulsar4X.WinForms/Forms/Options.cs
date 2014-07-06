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
    public partial class Options : Form
    {

        private bool m_bRestartRequired = false;

        public Options()
        {
            InitializeComponent();
        }

        private void ForecOpenGLVerCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            OpenGLVerComboBox.Enabled = ForecOpenGLVerCheckBox.Checked;
            OpenGLVerLabel.Enabled = ForecOpenGLVerCheckBox.Checked;
        }

        private void Options_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_bRestartRequired)
            {
                MessageBox.Show("A restart is required for changes to apply.", "Restart Required!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OpenGLVerComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ///< @todo save setting change
            m_bRestartRequired = true;
        }

        private void GLFontComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ///< @todo save setting change
            m_bRestartRequired = true;
        }
    }
}
