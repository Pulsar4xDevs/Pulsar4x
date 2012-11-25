using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Pulsar4X.UI.Forms
{
    public partial class MainForm : Form
    {
        DockPanel m_oDockPanel;

        public MainForm()
        {
            InitializeComponent();

            // Create and Add docking Panel:
            m_oDockPanel = new DockPanel();
            m_oDockPanel.DocumentStyle = DocumentStyle.DockingWindow;
            m_oDockPanel.Dock = DockStyle.Fill;

            // set Mono only stuff:
            if (Helpers.UIController.Instance.IsRunningOnMono)
            {
                m_oDockPanel.SupportDeeplyNestedContent = false;
                m_oDockPanel.AllowEndUserDocking = false;
                m_oDockPanel.AllowEndUserNestedDocking = false;
            }

            m_oToolStripContainer.ContentPanel.Controls.Add(m_oDockPanel);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Setup Events:
            exitToolStripMenuItem.Click += new EventHandler(exitToolStripMenuItem_Click);
            systemInformationToolStripMenuItem.Click += new EventHandler(systemInformationToolStripMenuItem_Click);
            aboutToolStripMenuItem.Click += new EventHandler(aboutToolStripMenuItem_Click);
            systemMapToolStripMenuItem.Click += new EventHandler(systemMapToolStripMenuItem_Click);

            // Set up the proportion of the windows used by docing panels at diufferent locations:
            m_oDockPanel.DockBottomPortion = 0.2f;
            m_oDockPanel.DockLeftPortion = 0.2f;
            m_oDockPanel.DockRightPortion = 0.2f;
        }

        #region MenuAndToolStripEvents

        void systemMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Helpers.UIController.Instance.SystemMap.ShowAllPanels(m_oDockPanel);
        }

        void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dialogs.AboutBox frmAboutBox = new Dialogs.AboutBox();
            frmAboutBox.ShowDialog();
        }

        void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        void systemInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Helpers.UIController.Instance.SystemGenAndDisplay.ShowAllPanels(m_oDockPanel);
        }

        #endregion
    }
}
