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
    public partial class ClassDes_Properties : DockContent
    {
        #region Properties

        private PropertyGrid m_oClassPropertyGrid;

        /// <summary>
        /// Shows all the class data and allows modification of that data where aproprate.
        /// </summary>
        public PropertyGrid ClassPropertyGrid
        {
            get
            {
                return m_oClassPropertyGrid;
            }
        }

        #endregion

        public ClassDes_Properties()
        {
            InitializeComponent();

            this.AutoHidePortion = 0.2f;
            this.HideOnClose = true;
            this.Text = "Class Properties";
            this.TabText = "Class Properties";
            this.ToolTipText = "Class Properties";

            // setup property grid.
            m_oClassPropertyGrid = new PropertyGrid();
            m_oClassPropertyGrid.Dock = DockStyle.Fill;
            this.Controls.Add(m_oClassPropertyGrid);
        }
    }
}
