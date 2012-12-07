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
            AddCellsToSummaryDataGrid();

            // Setup Pops tree View
            //m_oPopulationsPanel
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

        private void AddCellsToSummaryDataGrid()
        {
        }

        #endregion

    }
}
