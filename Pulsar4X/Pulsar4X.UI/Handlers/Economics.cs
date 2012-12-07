using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Pulsar4X.UI.ViewModels;
using Pulsar4X.Entities;

namespace Pulsar4X.UI.Handlers
{
    public class Economics
    {
        /// <summary>
        /// Panel that contains the list of populated systems.
        /// </summary>
        Panels.Eco_Populations m_oPopulationsPanel;

        /// <summary>
        /// Panel that contains the currently selected population summary.
        /// </summary>
        Panels.Eco_Summary m_oSummaryPanel;

        public EconomicsViewModel VM { get; set; }

        public Economics()
        {
            // create panels:
            m_oPopulationsPanel = new Panels.Eco_Populations();
            m_oSummaryPanel = new Panels.Eco_Summary();

            // Create Viewmodel:
            VM = new EconomicsViewModel();

            // create Bindings:
            m_oPopulationsPanel.FactionSelectionComboBox.Bind(c => c.DataSource, VM, d => d.Factions);
            m_oPopulationsPanel.FactionSelectionComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentFaction, DataSourceUpdateMode.OnPropertyChanged);
            m_oPopulationsPanel.FactionSelectionComboBox.DisplayMember = "Name";
            m_oPopulationsPanel.FactionSelectionComboBox.SelectedIndexChanged += (s, args) => m_oPopulationsPanel.FactionSelectionComboBox.DataBindings["SelectedItem"].WriteValue();

            // Setup Summary Data Grid:
            m_oSummaryPanel.SummaryDataGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            m_oSummaryPanel.SummaryDataGrid.RowHeadersVisible = false;
            m_oSummaryPanel.SummaryDataGrid.AutoGenerateColumns = false;
            SetupSummaryDataGrid();
            RefreshSummaryCells();

            // Setup Pops List box
            m_oPopulationsPanel.PopulationsListBox.Bind(c => c.DataSource, VM, d => d.Populations);
            m_oPopulationsPanel.PopulationsListBox.Bind(c => c.SelectedItem, VM, d => d.CurrentFaction, DataSourceUpdateMode.OnPropertyChanged);
            m_oPopulationsPanel.PopulationsListBox.DisplayMember = "Name";
            //VM.PopulationChanged += (s, args) => CurrentStarSystem = VM.CurrentStarSystem;
             
            // setup Event handlers:

        }

        #region EventHandlers
        #endregion

        #region PublicMethods

        public void ShowAllPanels(DockPanel a_oDockPanel)
        {
            ShowSummaryPanel(a_oDockPanel);
            ShowPopulationsPanel(a_oDockPanel);
        }

        public void ShowSummaryPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oSummaryPanel.Show(a_oDockPanel, DockState.Document);
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void ActivateSummaryPanel()
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oSummaryPanel.Activate();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void ShowPopulationsPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oPopulationsPanel.Show(a_oDockPanel, DockState.DockLeft);
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void ActivatePopulationsPanel()
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oPopulationsPanel.Activate();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        #endregion


        #region PrivateMethods

        private void SetupSummaryDataGrid()
        {
            // Add coloums:
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.HeaderText = "Item";
                m_oSummaryPanel.SummaryDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.HeaderText = "Amount";
                m_oSummaryPanel.SummaryDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.HeaderText = "Installation";
                m_oSummaryPanel.SummaryDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.HeaderText = "Number or Level";
                m_oSummaryPanel.SummaryDataGrid.Columns.Add(col);
            }

            // Add Rows:
            for (int i = 0; i < 35; ++i)
            {
                using (DataGridViewRow row = new DataGridViewRow())
                {
                    m_oSummaryPanel.SummaryDataGrid.Rows.Add(row);
                }
            }

            // Setup item Colomn:
            m_oSummaryPanel.SummaryDataGrid.Rows[1].Cells[0].Value = "Species";
            m_oSummaryPanel.SummaryDataGrid.Rows[7].Cells[0].Value = "Population";
            m_oSummaryPanel.SummaryDataGrid.Rows[31].Cells[0].Value = "Tectonics";



            // Setup Installation Colomn
            m_oSummaryPanel.SummaryDataGrid.Rows[17].Cells[2].Value = "Fuel Reserves";
        }

        public void RefreshSummaryCells()
        {
            m_oSummaryPanel.SummaryDataGrid.Rows[1].Cells[1].Value = VM.CurrentFaction.Species.Name;
            m_oSummaryPanel.SummaryDataGrid.Rows[7].Cells[1].Value = VM.CurrentPopulation.CivilianPopulation.ToString();
            //m_oSummaryPanel.SummaryDataGrid.Rows[31].Cells[1].Value = VM.CurrentPopulation.Planet.;  - No tetonics???
            m_oSummaryPanel.SummaryDataGrid.Rows[17].Cells[3].Value = VM.CurrentPopulation.FuelStockpile.ToString();
        }

        #endregion

    }
}
