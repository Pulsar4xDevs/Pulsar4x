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
    public partial class Eco_Summary : DockContent
    {
        #region Properties

        DataGridView m_oSummaryDataGrid;

        public DataGridView SummaryDataGrid
        {
            get
            {
                return m_oSummaryDataGrid;
            }
        }

        #endregion

        public Eco_Summary()
        {
            InitializeComponent();

            this.HideOnClose = true;
            this.Text = "Summary";
            this.TabText = "Summary";
            this.ToolTipText = "Colony Summary";

            // create and add out data grid:
            m_oSummaryDataGrid = new DataGridView();
            m_oSummaryDataGrid.Dock = DockStyle.Fill;
            m_oSummaryDataGrid.AllowUserToAddRows = false;
            m_oSummaryDataGrid.AllowUserToDeleteRows = false;
            m_oSummaryDataGrid.AllowUserToOrderColumns = false;
            m_oSummaryDataGrid.AllowUserToResizeColumns = true;
            m_oSummaryDataGrid.AllowUserToResizeRows = true;
            m_oSummaryDataGrid.ReadOnly = true;
            m_oSummaryDataGrid.Enabled = false;
            this.Controls.Add(m_oSummaryDataGrid);
        }
    }
}
