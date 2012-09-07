using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Pulsar4X
{
    public partial class MainForm : Form
    {
        Pulsar4X.WinForms.Controls.DraggableTabControl m_MainTabControl = new Pulsar4X.WinForms.Controls.DraggableTabControl();

        public MainForm()
        {
            InitializeComponent();
            m_MainTabControl.Name = "DraggableTabControl";
            m_MainTabControl.Dock = DockStyle.Fill;
        }

        public Panel GetMainPanel()
        {
            return MainPanel;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Load in the dragable tabs here, setup ref. list to all tabs?
            //m_MainTabControl.Size = MainPanel.Size;
            m_MainTabControl.MinimumSize = new System.Drawing.Size(1008, 706);
            m_MainTabControl.TabPages.Add(Pulsar4X.WinForms.Controls.UIController.g_aTabPages[3]);
            m_MainTabControl.TabPages.Add(Pulsar4X.WinForms.Controls.UIController.g_aTabPages[0]);
           //m_MainTabControl.TabPages.Add(Pulsar4X.WinForms.Controls.UIController.g_aTabPages[9]);

            MainPanel.Controls.Add(m_MainTabControl);
            
            // close splash screen:
            Pulsar4X.WinForms.Forms.StartupSplashScreen.SetStatus("Starting...");
            Pulsar4X.WinForms.Forms.StartupSplashScreen.Progress = 0.9;
            Pulsar4X.WinForms.Forms.StartupSplashScreen.CloseForm();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            Size newSize = this.Size;
            newSize.Width -= 42;            // Adjust size width to keet boarders
            newSize.Height -= 63;           // Adjust size height to keep boarders.
            MainPanel.Size = newSize;       // Set new MainPanel Size

            foreach (Control control in MainPanel.Controls)
            {
                control.Size = newSize; // this will edit the size of any sub forms. there will generall only be the one DraggableTabControl.
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void systemMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show System Map.
            WinForms.Controls.UIController.ShowTab(WinForms.UIConstants.UITabs.SYSTEM_MAP_INDEX);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Pulsar4X.WinForms.Forms.AboutBox AboutBox = new WinForms.Forms.AboutBox();
            AboutBox.Show();
        }

        private void systemInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show System information tab.
            WinForms.Controls.UIController.ShowTab(WinForms.UIConstants.UITabs.SYSTEM_GENERATION_AND_DISPLAY_INDEX);
        }


    }
}
