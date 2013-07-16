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
        public Button AdvanceTime5Seconds
        {
            get { return m_oAdvanceTime5Seconds; }
        }

        public Button AdvanceTime10Seconds
        {
            get { return m_oAdvanceTime10Seconds; }
        }
        public Button AdvanceTime100Seconds
        {
            get { return m_oAdvanceTime100Seconds; }
        }

        public Button AdvanceTime1000Seconds
        {
            get { return m_oAdvanceTime1000Seconds; }
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
