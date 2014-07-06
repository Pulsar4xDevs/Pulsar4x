using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using OpenTK;

namespace Pulsar4X.UI.Panels
{
    public partial class SysMap_ViewPort : DockContent
    {

        #region Properties
        public Button AdvanceTime5S
        {
            get { return m_oAdvanceTime5S; }
        }

        public Button AdvanceTime30S
        {
            get { return m_oAdvanceTime30S; }
        }
        public Button AdvanceTime2M
        {
            get { return m_oAdvanceTime2M; }
        }

        public Button AdvanceTime5M
        {
            get { return m_oAdvanceTime5M; }
        }

        public Button AdvanceTime20M
        {
            get { return m_oAdvanceTime20M; }
        }

        public Button AdvanceTime1H
        {
            get { return m_oAdvanceTime1H; }
        }
        public Button AdvanceTime3H
        {
            get { return m_oAdvanceTime3H; }
        }

        public Button AdvanceTime8H
        {
            get { return m_oAdvanceTime8H; }
        }

        public Button AdvanceTime1D
        {
            get { return m_oAdvanceTime1D; }
        }
        public Button AdvanceTime5D
        {
            get { return m_oAdvanceTime5D; }
        }

        public Button AdvanceTime30D
        {
            get { return m_oAdvanceTime30D; }
        }

        public Button StartSim
        {
            get { return m_oStartSim; }
        }
        #endregion

        public SysMap_ViewPort()
        {
            InitializeComponent();
            this.HideOnClose = true;
            this.Text = "System Map";
            this.TabText = "System Map View";
            this.ToolTipText = "System Map View Port";
        }
    }
}
