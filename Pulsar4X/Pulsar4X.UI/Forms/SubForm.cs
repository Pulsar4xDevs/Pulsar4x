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
    public partial class SubForm : Form
    {
        DockPanel m_oDockPanel;

        public SubForm()
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

            this.Controls.Add(m_oDockPanel);
        }
    }
}
