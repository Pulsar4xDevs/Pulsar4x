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

        /// <summary>
        /// Economic summary page datagrid.
        /// </summary>
        private DataGridView m_oSummaryDataGrid;

        public DataGridView SummaryDataGrid
        {
            get
            {
                return m_oSummaryDataGrid;
            }
        }

        /// <summary>
        /// Installation list datagrid.
        /// </summary>
        private DataGridView m_oBuildDataGrid;

        public DataGridView BuildDataGrid
        {
            get
            {
                return m_oBuildDataGrid;
            }
        }

        /// <summary>
        /// Construction in progress datagrid.
        /// </summary>
        private DataGridView m_oConstructionDataGrid;

        public DataGridView ConstructionDataGrid
        {
            get
            {
                return m_oConstructionDataGrid;
            }
        }

        /// <summary>
        /// Mineral storage and production datagrid
        /// </summary>
        private DataGridView m_oMiningDataGrid;

        public DataGridView MiningDataGrid
        {
            get
            {
                return m_oMiningDataGrid;
            }
        }

        /// <summary>
        /// Ship maintenance datagrid.
        /// </summary>
        private DataGridView m_oMaintenanceDataGrid;

        public DataGridView MaintenanceDataGrid
        {
            get
            {
                return m_oMaintenanceDataGrid;
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
            m_oSummaryDataGrid.AllowUserToResizeColumns = false;
            m_oSummaryDataGrid.AllowUserToResizeRows = false;
            m_oSummaryDataGrid.MultiSelect = false;
            m_oSummaryDataGrid.ReadOnly = true;
            m_oSummaryDataGrid.Enabled = true;
            m_oSummaryGroupBox.Controls.Add(m_oSummaryDataGrid);

            m_oBuildDataGrid = new DataGridView();
            m_oBuildDataGrid.Dock = DockStyle.Fill;
            m_oBuildDataGrid.AllowUserToAddRows = false;
            m_oBuildDataGrid.AllowUserToDeleteRows = false;
            m_oBuildDataGrid.AllowUserToOrderColumns = false;
            m_oBuildDataGrid.AllowUserToResizeColumns = false;
            m_oBuildDataGrid.AllowUserToResizeRows = false;
            m_oBuildDataGrid.MultiSelect = false;
            m_oBuildDataGrid.ReadOnly = true;
            m_oBuildDataGrid.Enabled = true;
            m_oInstallationGroupBox.Controls.Add(m_oBuildDataGrid);

            m_oConstructionDataGrid = new DataGridView();
            m_oConstructionDataGrid.Dock = DockStyle.Fill;
            m_oConstructionDataGrid.AllowUserToAddRows = false;
            m_oConstructionDataGrid.AllowUserToDeleteRows = false;
            m_oConstructionDataGrid.AllowUserToOrderColumns = false;
            m_oConstructionDataGrid.AllowUserToResizeColumns = false;
            m_oConstructionDataGrid.AllowUserToResizeRows = false;
            m_oConstructionDataGrid.MultiSelect = false;
            m_oConstructionDataGrid.ReadOnly = true;
            m_oConstructionDataGrid.Enabled = true;
            m_oConstructionDataGrid.Visible = false;
            m_oIndustrialAllocationGroupBox.Controls.Add(m_oConstructionDataGrid);

            m_oMiningDataGrid = new DataGridView();
            m_oMiningDataGrid.Dock = DockStyle.Fill;
            m_oMiningDataGrid.AllowUserToAddRows = false;
            m_oMiningDataGrid.AllowUserToDeleteRows = false;
            m_oMiningDataGrid.AllowUserToOrderColumns = false;
            m_oMiningDataGrid.AllowUserToResizeColumns = false;
            m_oMiningDataGrid.AllowUserToResizeRows = false;
            m_oMiningDataGrid.MultiSelect = false;
            m_oMiningDataGrid.ReadOnly = true;
            m_oMiningDataGrid.Enabled = true;
            m_oMineralProductionGroupBox.Controls.Add(m_oMiningDataGrid);

            m_oMaintenanceDataGrid = new DataGridView();
            m_oMaintenanceDataGrid.Dock = DockStyle.Fill;
            m_oMaintenanceDataGrid.AllowUserToAddRows = false;
            m_oMaintenanceDataGrid.AllowUserToDeleteRows = false;
            m_oMaintenanceDataGrid.AllowUserToOrderColumns = false;
            m_oMaintenanceDataGrid.AllowUserToResizeColumns = false;
            m_oMaintenanceDataGrid.AllowUserToResizeRows = false;
            m_oMaintenanceDataGrid.MultiSelect = false;
            m_oMaintenanceDataGrid.ReadOnly = true;
            m_oMaintenanceDataGrid.Enabled = true;
            m_oMaintFacilityGroupBox.Controls.Add(m_oMaintenanceDataGrid);
        }
    }
}
