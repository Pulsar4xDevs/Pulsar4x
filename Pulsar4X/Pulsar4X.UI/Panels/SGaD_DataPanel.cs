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
    public partial class SGaD_DataPanel : DockContent
    {

        private SplitContainer m_oSplitContainer1;
        /// <summary>
        /// Returs the Planets Data Grid View
        /// </summary>
        public DataGridView PlanetsDataGrid
        {
            get
            {
                return m_oPlanetsDataGridView;
            }
        }
        /// <summary>
        /// Returns the Star Data Grid View
        /// </summary>
        public DataGridView StarDataGrid
        {
            get
            {
                return m_oStarDataGridView;
            }
        }

        public SGaD_DataPanel()
        {
            InitializeComponent();

            // Create Controls:
            m_oSplitContainer1 = new SplitContainer();
            m_oPlanetsDataGridView = new DataGridView();
            m_oStarDataGridView = new DataGridView();

            // Add controls to panel:
            this.Controls.Add(m_oSplitContainer1);
            m_oSplitContainer1.Panel1.Controls.Add(m_oStarDataGridView);
            m_oSplitContainer1.Panel2.Controls.Add(m_oPlanetsDataGridView);

            // Configure controls:
            m_oSplitContainer1.Dock = DockStyle.Fill;
            m_oSplitContainer1.Orientation = Orientation.Horizontal;
            m_oSplitContainer1.Panel1MinSize = 108;
            m_oStarDataGridView.Dock = DockStyle.Fill;
            m_oPlanetsDataGridView.Dock = DockStyle.Fill;
        }

        public void SetSplitterDistance(int a_iDist)
        {
            m_oSplitContainer1.SplitterDistance = a_iDist;
        }
    }
}
