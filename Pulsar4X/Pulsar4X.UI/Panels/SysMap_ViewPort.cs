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
