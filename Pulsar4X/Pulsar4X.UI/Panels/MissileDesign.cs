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
    public partial class MissileDesign : Form
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

            m_oTechDataGrid = new DataGridView();
            m_oTechDataGrid.Dock = DockStyle.Fill;
            m_oTechDataGrid.AllowUserToAddRows = false;
            m_oTechDataGrid.AllowUserToDeleteRows = false;
            m_oTechDataGrid.AllowUserToOrderColumns = false;
            m_oTechDataGrid.AllowUserToResizeColumns = false;
            m_oTechDataGrid.AllowUserToResizeRows = false;
            m_oTechDataGrid.ReadOnly = true;
            m_oTechDataGrid.Enabled = true;
            m_oTechDataGrid.Font = m_oInfoLabel.Font;
            this.m_oInfoGroupBox.Controls.Add(m_oTechDataGrid);
        }
    }
}
