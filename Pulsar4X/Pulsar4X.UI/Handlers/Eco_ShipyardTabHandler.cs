using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Pulsar4X.Entities;
using System.ComponentModel;

#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif

/*
Shipyard work to be done: public accessors need to be created for these, and of course they have to all be implemented.
Likewise construction cycle work will need to be done for shipyards.
m_oShipyardListGroupBox
m_oShipyardTaskGroupBox
m_oShipyardCreateTaskGroupBox
m_oSYActivityGroupBox

m_oShipyardRequiredMaterialsListBox
m_oShipRequiredMaterialsListBox

m_oNewNameButton
m_oRefitDetailsButton

m_oAddTaskButton
m_oDefaultFleetButton

m_oSetActivityButton
m_oDeleteActivityButton
m_oPauseActivityButton
m_oRenameSYButton
m_oAutoRenameButton
m_oSMSYButton
m_oSYADeleteTaskButton
m_oSYAPauseTaskButton
m_oSYARaisePriorityButton
m_oSYALowerPriorityButton
m_oSYAScheduleButton
m_oSYARenameShipButton

m_oSYCBuildCostTextBox
m_oSYCCompletionDateTextBox
m_oSYTaskCostTextBox
m_oSYTaskCompletionDateTextBox


//m_oSYCTaskTypeComboBox //Alter the shipyard
//m_oSYTaskTypeComboBox  //Build/repair/refit/scrap ship



m_oCreateDefaultnameCheckBox


m_oAnnualBuildRateLabel

These are conditionally visible based on selected settings:
    Retool for new class:
m_oSYCShipClassLabel
m_oSYCNewShipClassComboBox
 
    Not visible during repair and scrap
//m_oSYNewClassComboBox  //Eligible classes

    Only visible during Construction, repair and refit dump ships back into their own TG
m_oSYTaskGroupComboBox   //Set the ships built to go into this taskgroup

    Not visible during construction
m_oRepairRefitScrapShipComboBox

    Visible during construction only
m_oSYShipNameTextBox

Text Label format for the SY Tasks tab: "Annual Ship Building Rate Per Slipway for 5000 ton ship: 111    Available Slipways: 1"

*/

namespace Pulsar4X.UI.Handlers
{
    public class Eco_ShipyardTabHandler
    {
        /// <summary>
        /// Shipyard tab Handler Logger:
        /// </summary>
#if LOG4NET_ENABLED
        public static readonly ILog logger = LogManager.GetLogger(typeof(Eco_ShipyardTabHandler));
#endif
        private static int MaxShipyardRows { get; set; }

        /// <summary>
        /// Initialize the Shipyard tab
        /// </summary>
        /// <param name="m_oSummaryPanel">The summary panel from the economics handler.</param>
        public static void BuildShipyardTab(Panels.Eco_Summary m_oSummaryPanel, Faction CurrentFaction, Population CurrentPopulation, BindingList<ShipClassTN> RetoolTargets)
        {
            MaxShipyardRows = 38;
            //m_oSummaryPanel.
            //Populate the datagrid
            //build the SYC Groupbox
            //build the create task groupbox
            //listen to the buttons
            BuildShipyardDataGrid(m_oSummaryPanel);
            BuildSYCGroupBox(m_oSummaryPanel, CurrentFaction, CurrentPopulation, RetoolTargets);
            BuildSYTaskGroupBox(m_oSummaryPanel, CurrentFaction, CurrentPopulation);
        }

        /// <summary>
        /// Refresh the shipyard tab.
        /// </summary>
        /// <param name="m_oSummaryPanel">The summary panel from the economics handler.</param>
        public static void RefreshShipyardTab(Panels.Eco_Summary m_oSummaryPanel, Faction CurrentFaction, Population CurrentPopulation, Installation.ShipyardInformation SYInfo
                                              , BindingList<ShipClassTN> RetoolTargets)
        {
            if (CurrentFaction != null && CurrentPopulation != null && SYInfo != null)
            {
                RefreshShipyardDataGrid(m_oSummaryPanel, CurrentFaction, CurrentPopulation);
                BuildSYCRequiredMinerals(m_oSummaryPanel, CurrentFaction, CurrentPopulation, SYInfo, RetoolTargets);
            }
        }

        /// <summary>
        /// Populate the Datagrid
        /// </summary>
        /// <param name="m_oSummaryPanel">The summary panel from the economics handler.</param>
        private static void BuildShipyardDataGrid(Panels.Eco_Summary m_oSummaryPanel)
        {
            try
            {
                // Add coloums:
                Padding newPadding = new Padding(2, 0, 2, 0);
                AddColumn("Name", newPadding, m_oSummaryPanel.ShipyardDataGrid, 0);
                AddColumn("Ty", newPadding, m_oSummaryPanel.ShipyardDataGrid, 3);
                AddColumn("Total Slipways", newPadding, m_oSummaryPanel.ShipyardDataGrid, 3);
                AddColumn("Capacity per Slipway", newPadding, m_oSummaryPanel.ShipyardDataGrid, 3);
                AddColumn("Available Slipways", newPadding, m_oSummaryPanel.ShipyardDataGrid,3);
                AddColumn("Assigned Class", newPadding, m_oSummaryPanel.ShipyardDataGrid, 3);
                AddColumn("Current Complex Activity", newPadding, m_oSummaryPanel.ShipyardDataGrid, 3);
                AddColumn("Progress", newPadding, m_oSummaryPanel.ShipyardDataGrid, 3);
                AddColumn("Completion Date", newPadding, m_oSummaryPanel.ShipyardDataGrid, 3);
                AddColumn("Mod Rate", newPadding, m_oSummaryPanel.ShipyardDataGrid, 3);

                // Add Rows:
                for (int i = 0; i < MaxShipyardRows; ++i)
                {
                    using (DataGridViewRow row = new DataGridViewRow())
                    {
                        // setup row height. note that by default they are 22 pixels in height!
                        row.Height = 18;
                        row.Visible = false;
                        m_oSummaryPanel.ShipyardDataGrid.Rows.Add(row);
                    }
                }
            }
            catch
            {
#if LOG4NET_ENABLED
                logger.Error("Something whent wrong Creating ShipyardDataGrid Columns for Economics summary screen...");
#endif
            }
        }

        /// <summary>
        /// This builds the Shipyard complex groupbox, which controls how the shipyards themselves are modified. Shipyard tasks(such as ship building) are handled elsewhere.
        /// </summary>
        /// <param name="m_oSummaryPanel">The summary panel from the economics handler.</param>
        private static void BuildSYCGroupBox(Panels.Eco_Summary m_oSummaryPanel, Faction CurrentFaction, Population CurrentPopulation, BindingList<ShipClassTN> RetoolList)
        {
            m_oSummaryPanel.SYCTaskTypeComboBox.Items.Clear();
            m_oSummaryPanel.SYCTaskTypeComboBox.DataSource = Constants.ShipyardInfo.ShipyardTasks;
            m_oSummaryPanel.SYCTaskTypeComboBox.SelectedIndex = 0;

#warning this doesn't update when a new shipclass is added on its own. the econ page is "shared" by all factions so an event may not be possible there.
            m_oSummaryPanel.NewShipClassComboBox.Items.Clear();
            m_oSummaryPanel.NewShipClassComboBox.DataSource = RetoolList;
            if (RetoolList.Count != 0)
                m_oSummaryPanel.NewShipClassComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// This builds the Shipyard tasks groupbox, which controls the building/repairing/refitting/scrapping of ships. said ships must be in orbit and taskgroups connected to them may not
        /// move while this process is underway.
        /// </summary>
        /// <param name="m_oSummaryPanel">The summary panel from the economics handler.</param>
        private static void BuildSYTaskGroupBox(Panels.Eco_Summary m_oSummaryPanel, Faction CurrentFaction, Population CurrentPopulation)
        {
            m_oSummaryPanel.SYTaskTypeComboBox.Items.Clear();
            m_oSummaryPanel.SYTaskTypeComboBox.DataSource = Constants.ShipyardInfo.ShipyardTaskType;
            m_oSummaryPanel.SYTaskTypeComboBox.SelectedIndex = 0;


            // Do Eligible classes here:
            //m_oSYNewClassComboBox
        }


        /// <summary>
        /// Just a space saver here to avoid copy pasting a lot. this is copied from taskgroup
        /// </summary>
        /// <param name="Header">Text of column header.</param>
        /// <param name="newPadding">Padding in use, not sure what this is or why its necessary. Cargo culting it is.</param>
        /// <param name="CellControl">Alignment control. 0 = both left, 1 = header left, cells center, 2 = header center, cells left, 3 = both center.</param>
        private static void AddColumn(String Header, Padding newPadding, DataGridView TheDataGrid, int CellControl = -1)
        {
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.HeaderText = Header;
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

                if (CellControl == 0)
                {
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }
                else if (CellControl == 1)
                {
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                else if (CellControl == 2)
                {
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }
                else if (CellControl == 3)
                {
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                col.DefaultCellStyle.Padding = newPadding;

                if (col != null)
                {
                    TheDataGrid.Columns.Add(col);
                }
            }
        }

        /// <summary>
        /// Populate the rows of the Shipyard datagrid with the various shipyard complex data items.
        /// </summary>
        /// <param name="m_oSummaryPanel"></param>
        /// <param name="CurrentFaction">Faction Currently selected</param>
        /// <param name="CurrentPopulation">Population Currently selected</param>
        private static void RefreshShipyardDataGrid(Panels.Eco_Summary m_oSummaryPanel, Faction CurrentFaction, Population CurrentPopulation)
        {
            if (m_oSummaryPanel.ShipyardDataGrid.Rows.Count != 0)
            {

                int row = 0;

                /// <summary>
                /// Populate the Naval shipyard info.
                /// </summary>
                int NavalYards = (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].Number);
                for (int ShipyardIterator = 0; ShipyardIterator < NavalYards; ShipyardIterator++)
                {
                    m_oSummaryPanel.ShipyardDataGrid.Rows[row].Visible = true;
                    row++;

                    if (row == MaxShipyardRows)
                    {
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            // setup row height. note that by default they are 22 pixels in height!
                            NewRow.Height = 18;
                            NewRow.Visible = false;
                            m_oSummaryPanel.ShipyardDataGrid.Rows.Add(NewRow);
                        }
                        MaxShipyardRows++;
                    }

                    Installation.ShipyardInformation SYI = CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].SYInfo[ShipyardIterator];
                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator].Cells[0].Value = SYI.Name;
                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator].Cells[1].Value = "N";
                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator].Cells[2].Value = SYI.Slipways;
                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator].Cells[3].Value = SYI.Tonnage;

                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator].Cells[4].Value = (SYI.Slipways - SYI.BuildingShips.Count());

                    if (SYI.AssignedClass != null)
                    {
                        m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator].Cells[5].Value = SYI.AssignedClass.Name;
                    }
                    else
                    {
                        m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator].Cells[5].Value = "No Assigned Class";
                    }

                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator].Cells[6].Value = Constants.ShipyardInfo.ShipyardTasks[(int)SYI.CurrentActivity.Activity];
                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator].Cells[7].Value = SYI.CurrentActivity.Progress;
                    if (SYI.CurrentActivity.Activity == Constants.ShipyardInfo.ShipyardActivity.NoActivity)
                    {
                        m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator].Cells[8].Value = "N/A";
                    }
                    else
                    {
                        float YearsOfProduction = (float)SYI.CurrentActivity.CostOfActivity / SYI.CalcAnnualSYProduction();
                        if (YearsOfProduction < Constants.Colony.TimerYearMax)
                        {
                            m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator].Cells[8].Value = SYI.CurrentActivity.CompletionDate;
                        }
                        else
                        {
                            m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator].Cells[8].Value = "N/A";
                        }
                    }

                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator].Cells[9].Value = SYI.ModRate;


                }

                /// <summary>
                /// Populate the commercial yard information.
                /// </summary>
                int CommercialYards = (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].Number);
                for (int ShipyardIterator = 0; ShipyardIterator < CommercialYards; ShipyardIterator++)
                {
                    m_oSummaryPanel.ShipyardDataGrid.Rows[row].Visible = true;
                    

                    if (row == MaxShipyardRows)
                    {
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            // setup row height. note that by default they are 22 pixels in height!
                            NewRow.Height = 18;
                            NewRow.Visible = false;
                            m_oSummaryPanel.ShipyardDataGrid.Rows.Add(NewRow);
                        }
                        MaxShipyardRows++;
                    }

                    Installation.ShipyardInformation SYI = CurrentPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].SYInfo[ShipyardIterator];
                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator + row].Cells[0].Value = SYI.Name;
                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator + row].Cells[1].Value = "C";
                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator + row].Cells[2].Value = SYI.Slipways;
                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator + row].Cells[3].Value = SYI.Tonnage;

                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator + row].Cells[4].Value = (SYI.Slipways - SYI.BuildingShips.Count());

                    if (SYI.AssignedClass != null)
                    {
                        m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator + row].Cells[5].Value = SYI.AssignedClass.Name;
                    }
                    else
                    {
                        m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator + row].Cells[5].Value = "No Assigned Class";
                    }

                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator + row].Cells[6].Value = Constants.ShipyardInfo.ShipyardTasks[(int)SYI.CurrentActivity.Activity];
                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator + row].Cells[7].Value = SYI.CurrentActivity.Progress;
                    if (SYI.CurrentActivity.Activity == Constants.ShipyardInfo.ShipyardActivity.NoActivity)
                    {
                        m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator + row].Cells[8].Value = "N/A";
                    }
                    else
                    {
                        m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator + row].Cells[8].Value = SYI.CurrentActivity.CompletionDate;
                    }

                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator + row].Cells[9].Value = SYI.ModRate;

                    row++;
                }

                /// <summary>
                /// Do not display any rows after this.
                /// </summary>
                for (int rowIterator = row; rowIterator < MaxShipyardRows; rowIterator++)
                {
                    m_oSummaryPanel.ShipyardDataGrid.Rows[rowIterator].Visible = false;
                }
            }
        }

        /// <summary>
        /// Every task will cost resources, and this program will populate the required resources listbox.
        /// </summary>
        /// <param name="m_oSummaryPanel">Panel the economics handler will pass to us</param>
        /// <param name="CurrentFaction">Current Faction</param>
        /// <param name="CurrentPopulation">Currently selected population</param>
        /// <param name="SYInfo">Current Shipyard</param>
        /// <param name="Retool">Retool target if any</param>
        /// <param name="CapLimit">Cap expansion limit if any</param>
        public static void BuildSYCRequiredMinerals(Panels.Eco_Summary m_oSummaryPanel, Faction CurrentFaction, Population CurrentPopulation, Installation.ShipyardInformation SYInfo,
                                                    BindingList<ShipClassTN> PotentialRetoolTargets)
        {
            if (m_oSummaryPanel.SYCTaskTypeComboBox.SelectedIndex != -1  && SYInfo != null)
            {
                m_oSummaryPanel.SYCRequiredMaterialsListBox.Items.Clear();
                Constants.ShipyardInfo.ShipyardActivity Activity = (Constants.ShipyardInfo.ShipyardActivity)m_oSummaryPanel.SYCTaskTypeComboBox.SelectedIndex;

                if (Activity != Constants.ShipyardInfo.ShipyardActivity.CapExpansion && Activity != Constants.ShipyardInfo.ShipyardActivity.NoActivity)
                {
                    Installation.ShipyardInformation CostPrototyper = new Installation.ShipyardInformation(SYInfo.ShipyardType);
                    CostPrototyper.Tonnage = SYInfo.Tonnage;
                    CostPrototyper.Slipways = SYInfo.Slipways;
                    CostPrototyper.ModRate = SYInfo.ModRate;
                    CostPrototyper.AssignedClass = SYInfo.AssignedClass;
                    ShipClassTN RetoolTarget = null;
                    if (PotentialRetoolTargets.Count != 0 && m_oSummaryPanel.NewShipClassComboBox.SelectedIndex != -1)
                    {
                        RetoolTarget = PotentialRetoolTargets[m_oSummaryPanel.NewShipClassComboBox.SelectedIndex];
                    }
                    int NewCapLimit = -1;
                    bool r = Int32.TryParse(m_oSummaryPanel.ExpandCapUntilXTextBox.Text, out NewCapLimit);

                    CostPrototyper.SetShipyardActivity(Activity, RetoolTarget, NewCapLimit);

                    for (int MineralIterator = 0; MineralIterator < Constants.Minerals.NO_OF_MINERIALS; MineralIterator++)
                    {

                        if (CostPrototyper.CurrentActivity.minerialsCost[MineralIterator] != 0.0m)
                        {
                            string FormattedMineralTotal = CostPrototyper.CurrentActivity.minerialsCost[MineralIterator].ToString("#,##0");

                            String CostString = String.Format("{0} {1} ({2})", (Constants.Minerals.MinerialNames)MineralIterator, FormattedMineralTotal, CurrentPopulation.Minerials[MineralIterator]);
                            m_oSummaryPanel.SYCRequiredMaterialsListBox.Items.Add(CostString);
                        }
                    }

                    m_oSummaryPanel.SYCBuildCostTextBox.Text = CostPrototyper.CurrentActivity.CostOfActivity.ToString();

                    float YearsOfProduction = (float)CostPrototyper.CurrentActivity.CostOfActivity / CostPrototyper.CalcAnnualSYProduction();
                    if (YearsOfProduction < Constants.Colony.TimerYearMax)
                    {
                        m_oSummaryPanel.SYCCompletionDateTextBox.Text = CostPrototyper.CurrentActivity.CompletionDate.ToShortDateString();
                    }
                    else
                    {
                        m_oSummaryPanel.SYCCompletionDateTextBox.Text = "N/A";
                    }

                    if ((Activity == Constants.ShipyardInfo.ShipyardActivity.Retool && RetoolTarget == null) ||
                        (Activity == Constants.ShipyardInfo.ShipyardActivity.CapExpansionUntilX && (r == false || (NewCapLimit <= SYInfo.Tonnage))))
                    {
                        m_oSummaryPanel.SYCCompletionDateTextBox.Text = "N/A";
                        m_oSummaryPanel.SYCRequiredMaterialsListBox.Items.Clear();
                    }

                    /// <summary>
                    /// This retool is free. or not necessary.
                    /// </summary>
                    if (Activity == Constants.ShipyardInfo.ShipyardActivity.Retool && ((RetoolTarget != null && SYInfo.AssignedClass == null) || RetoolTarget == SYInfo.AssignedClass))
                    {
                        m_oSummaryPanel.SYCBuildCostTextBox.Text = "N/A";
                        m_oSummaryPanel.SYCCompletionDateTextBox.Text = "N/A";
                        m_oSummaryPanel.SYCRequiredMaterialsListBox.Items.Clear();
                    }
                }
                else
                {
                    m_oSummaryPanel.SYCBuildCostTextBox.Text = "N/A";
                    m_oSummaryPanel.SYCCompletionDateTextBox.Text = "N/A";
                    m_oSummaryPanel.SYCRequiredMaterialsListBox.Items.Clear();
                }
            }
        }
    }
}
