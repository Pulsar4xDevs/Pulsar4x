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
    public partial class MissileDesign : DockContent
    {
        /// <summary>
        /// Display for missile tech
        /// </summary>
        private DataGridView m_oTechDataGrid;
        public DataGridView TechDataGrid
        {
            get { return m_oTechDataGrid; }
        }

        public MissileDesign()
        {
            InitializeComponent();
        }
    }
}
