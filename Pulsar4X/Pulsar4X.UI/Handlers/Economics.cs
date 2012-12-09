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
            Padding newPadding = new Padding(2, 0, 2, 0);
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.HeaderText = "Item";
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                col.DefaultCellStyle.Padding = newPadding;
                m_oSummaryPanel.SummaryDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.HeaderText = "Amount";
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                col.DefaultCellStyle.Padding = newPadding;
                m_oSummaryPanel.SummaryDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.HeaderText = "Installation";
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                col.DefaultCellStyle.Padding = newPadding;
                m_oSummaryPanel.SummaryDataGrid.Columns.Add(col);
            }
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.HeaderText = "Number or Level";
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                col.DefaultCellStyle.Padding = newPadding;
                m_oSummaryPanel.SummaryDataGrid.Columns.Add(col);
            }

            // Add Rows:
            for (int i = 0; i < 33; ++i)
            {
                using (DataGridViewRow row = new DataGridViewRow())
                {
                    // setup row height. note that by default they are 22 pixels in height!
                    row.Height = 18;
                    m_oSummaryPanel.SummaryDataGrid.Rows.Add(row);
                }
            }

            // Setup item Colomn:
            m_oSummaryPanel.SummaryDataGrid.Rows[1].Cells[0].Value = "Species";
            m_oSummaryPanel.SummaryDataGrid.Rows[7].Cells[0].Value = "Population";
            m_oSummaryPanel.SummaryDataGrid.Rows[8].Cells[0].Value = "   Agriculture and Enviromental (5.0%)";
            m_oSummaryPanel.SummaryDataGrid.Rows[9].Cells[0].Value = "   Service Industries (75.0%)";
            m_oSummaryPanel.SummaryDataGrid.Rows[10].Cells[0].Value = "   Manufacturing (20.0%)";
            m_oSummaryPanel.SummaryDataGrid.Rows[11].Cells[0].Value = "Anual Growth Rate";

            m_oSummaryPanel.SummaryDataGrid.Rows[14].Cells[0].Value = "Current Infrastructure";

            m_oSummaryPanel.SummaryDataGrid.Rows[31].Cells[0].Value = "Tectonics";

            // Setup Installation Colomn
            m_oSummaryPanel.SummaryDataGrid.Rows[0].Cells[2].Value = "Military Academy";
            m_oSummaryPanel.SummaryDataGrid.Rows[1].Cells[2].Value = "Deep Space Tracking Station";
            m_oSummaryPanel.SummaryDataGrid.Rows[2].Cells[2].Value = "Maintenance Facility Maximum Ship Size";

            m_oSummaryPanel.SummaryDataGrid.Rows[4].Cells[2].Value = "Shipyards / Slipways";
            m_oSummaryPanel.SummaryDataGrid.Rows[5].Cells[2].Value = "Maintenance Facilities";
            m_oSummaryPanel.SummaryDataGrid.Rows[6].Cells[2].Value = "Construction Factories";
            m_oSummaryPanel.SummaryDataGrid.Rows[7].Cells[2].Value = "Ordnance Factories";
            m_oSummaryPanel.SummaryDataGrid.Rows[8].Cells[2].Value = "Fighter Factories";
            m_oSummaryPanel.SummaryDataGrid.Rows[9].Cells[2].Value = "Fuel Refineries";
            m_oSummaryPanel.SummaryDataGrid.Rows[10].Cells[2].Value = "Mines";
            m_oSummaryPanel.SummaryDataGrid.Rows[11].Cells[2].Value = "Automated Mines";
            m_oSummaryPanel.SummaryDataGrid.Rows[12].Cells[2].Value = "Research Labs";
            m_oSummaryPanel.SummaryDataGrid.Rows[13].Cells[2].Value = "Ground Force Training Facilities";
            m_oSummaryPanel.SummaryDataGrid.Rows[14].Cells[2].Value = "Financial Centre";
            m_oSummaryPanel.SummaryDataGrid.Rows[15].Cells[2].Value = "Mass Driver";
            m_oSummaryPanel.SummaryDataGrid.Rows[16].Cells[2].Value = "Sector Command";
            m_oSummaryPanel.SummaryDataGrid.Rows[17].Cells[2].Value = "Spaceport";
            m_oSummaryPanel.SummaryDataGrid.Rows[18].Cells[2].Value = "Terraforming Installation";

            m_oSummaryPanel.SummaryDataGrid.Rows[20].Cells[2].Value = "Fuel Reserves";
            m_oSummaryPanel.SummaryDataGrid.Rows[21].Cells[2].Value = "Maintenance Supplies";
            m_oSummaryPanel.SummaryDataGrid.Rows[23].Cells[2].Value = "EM Signature of Colony";
            m_oSummaryPanel.SummaryDataGrid.Rows[25].Cells[2].Value = "Economic Production Modifier";
            m_oSummaryPanel.SummaryDataGrid.Rows[26].Cells[2].Value = "Manufacturing Efficiency Modifier";
            m_oSummaryPanel.SummaryDataGrid.Rows[27].Cells[2].Value = "Political Status Production Modifier";
            m_oSummaryPanel.SummaryDataGrid.Rows[28].Cells[2].Value = "Political Status Wealth/Trade Modifier";
            m_oSummaryPanel.SummaryDataGrid.Rows[29].Cells[2].Value = "Political Status Modifier";
        }

        public void RefreshSummaryCells()
        {
            m_oSummaryPanel.SummaryDataGrid.Rows[1].Cells[1].Value = VM.CurrentFaction.Species.Name;
            m_oSummaryPanel.SummaryDataGrid.Rows[7].Cells[1].Value = VM.CurrentPopulation.CivilianPopulation.ToString() + "m";
            m_oSummaryPanel.SummaryDataGrid.Rows[8].Cells[1].Value = VM.CurrentPopulation.PopulationWorkingInAgriAndEnviro.ToString() + "m";
            m_oSummaryPanel.SummaryDataGrid.Rows[9].Cells[1].Value = VM.CurrentPopulation.PopulationWorkingInServiceIndustries.ToString() + "m";
            m_oSummaryPanel.SummaryDataGrid.Rows[10].Cells[1].Value = VM.CurrentPopulation.PopulationWorkingInManufacturing.ToString() + "m";
            m_oSummaryPanel.SummaryDataGrid.Rows[11].Cells[1].Value = VM.CurrentPopulation.PopulationGrowthRate.ToString() + "%";

            m_oSummaryPanel.SummaryDataGrid.Rows[14].Cells[1].Value = VM.CurrentPopulation.Installations[(int)Installation.InstallationType.Infrastructure].Number.ToString();

            //m_oSummaryPanel.SummaryDataGrid.Rows[31].Cells[1].Value = VM.CurrentPopulation.Planet.;  - No tetonics???
            m_oSummaryPanel.SummaryDataGrid.Rows[0].Cells[3].Value = "Level " + VM.CurrentPopulation.Installations[(int)Installation.InstallationType.MilitaryAcademy].Number.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[1].Cells[3].Value = "Level " + VM.CurrentPopulation.Installations[(int)Installation.InstallationType.DeepSpaceTrackingStation].Number.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[2].Cells[3].Value = (VM.CurrentPopulation.Installations[(int)Installation.InstallationType.MaintenanceFacility].Number * 200).ToString() + " tons";

            int iShipyards = (int)(VM.CurrentPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].Number + VM.CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].Number);
            m_oSummaryPanel.SummaryDataGrid.Rows[4].Cells[3].Value =  iShipyards.ToString() + " / ";
            m_oSummaryPanel.SummaryDataGrid.Rows[5].Cells[3].Value = VM.CurrentPopulation.Installations[(int)Installation.InstallationType.MaintenanceFacility].Number.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[6].Cells[3].Value = VM.CurrentPopulation.Installations[(int)Installation.InstallationType.ConstructionFactory].Number.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[7].Cells[3].Value = VM.CurrentPopulation.Installations[(int)Installation.InstallationType.OrdnanceFactory].Number.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[8].Cells[3].Value = VM.CurrentPopulation.Installations[(int)Installation.InstallationType.FighterFactory].Number.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[9].Cells[3].Value = VM.CurrentPopulation.Installations[(int)Installation.InstallationType.FuelRefinery].Number.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[10].Cells[3].Value = VM.CurrentPopulation.Installations[(int)Installation.InstallationType.Mine].Number.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[11].Cells[3].Value = VM.CurrentPopulation.Installations[(int)Installation.InstallationType.AutomatedMine].Number.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[12].Cells[3].Value = VM.CurrentPopulation.Installations[(int)Installation.InstallationType.ResearchLab].Number.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[13].Cells[3].Value = VM.CurrentPopulation.Installations[(int)Installation.InstallationType.GroundForceTrainingFacility].Number.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[14].Cells[3].Value = VM.CurrentPopulation.Installations[(int)Installation.InstallationType.FinancialCentre].Number.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[15].Cells[3].Value = VM.CurrentPopulation.Installations[(int)Installation.InstallationType.MassDriver].Number.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[16].Cells[3].Value = VM.CurrentPopulation.Installations[(int)Installation.InstallationType.SectorCommand].Number.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[17].Cells[3].Value = VM.CurrentPopulation.Installations[(int)Installation.InstallationType.Spaceport].Number.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[18].Cells[3].Value = VM.CurrentPopulation.Installations[(int)Installation.InstallationType.TerraformingInstallation].Number.ToString();

            m_oSummaryPanel.SummaryDataGrid.Rows[20].Cells[3].Value = VM.CurrentPopulation.FuelStockpile.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[21].Cells[3].Value = VM.CurrentPopulation.MaintenanceSupplies.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[23].Cells[3].Value = VM.CurrentPopulation.EMSignature.ToString();
            m_oSummaryPanel.SummaryDataGrid.Rows[25].Cells[3].Value = (VM.CurrentPopulation.ModifierEconomicProduction * 100).ToString() + "%";
            m_oSummaryPanel.SummaryDataGrid.Rows[26].Cells[3].Value = (VM.CurrentPopulation.ModifierManfacturing * 100).ToString() + "%";
            m_oSummaryPanel.SummaryDataGrid.Rows[27].Cells[3].Value = (VM.CurrentPopulation.ModifierProduction * 100).ToString() + "%";
            m_oSummaryPanel.SummaryDataGrid.Rows[28].Cells[3].Value = (VM.CurrentPopulation.ModifierWealthAndTrade * 100).ToString() + "%";
            m_oSummaryPanel.SummaryDataGrid.Rows[29].Cells[3].Value = (VM.CurrentPopulation.ModifierPoliticalStability * 100).ToString() + "%";
        }

        #endregion

    }
}
