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
    public partial class Individual_Unit_Details_Panel : DockContent
    {
        /// <summary>
        /// Display for Ship Armor
        /// </summary>
        private DataGridView m_oArmorDisplayDataGrid;
        public DataGridView ArmorDisplayDataGrid
        {
            get { return m_oArmorDisplayDataGrid; }
        }


        public Individual_Unit_Details_Panel()
        {
            InitializeComponent();

            this.HideOnClose = true;
            this.Text = "Individual Unit Details";
            this.TabText = "Individual Unit Details";
            this.ToolTipText = "Individual unit information";

            m_oArmorDisplayDataGrid = new DataGridView();
            m_oArmorDisplayDataGrid.Dock = DockStyle.Fill;
            m_oArmorDisplayDataGrid.AllowUserToAddRows = false;
            m_oArmorDisplayDataGrid.AllowUserToDeleteRows = false;
            m_oArmorDisplayDataGrid.AllowUserToOrderColumns = false;
            m_oArmorDisplayDataGrid.AllowUserToResizeColumns = false;
            m_oArmorDisplayDataGrid.AllowUserToResizeRows = false;
            m_oArmorDisplayDataGrid.Enabled = true;
            m_oArmorDisplayDataGrid.ReadOnly = true;
            m_oArmorDisplayDataGrid.ColumnHeadersVisible = false;
            m_oArmorDisplayDataGrid.RowHeadersVisible = false;
            m_oArmorDisplayDataGrid.ClearSelection();
            this.m_oArmorGroupBox.Controls.Add(m_oArmorDisplayDataGrid);
        }
    }
}
