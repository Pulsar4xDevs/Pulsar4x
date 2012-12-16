using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Pulsar4X.UI.Panels
{
    public partial class ClassDes_DesignAndInfo : DockContent
    {
        #region Properties



        #endregion

        public ClassDes_DesignAndInfo()
        {
            InitializeComponent();

            this.HideOnClose = true;
            this.Text = "Design and Info";
            this.TabText = "Design and Info";
            this.ToolTipText = "Class Design and Information";
        }
    }
}
