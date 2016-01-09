using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
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

m_oRepairRefitScrapLabel
m_oRepairRefitScrapClassComboBox

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

        /// <summary>
        /// Rows for the shipyard datagrid.
        /// </summary>
        private static int MaxShipyardRows { get; set; }

        private static int MaxShipyardTaskRows { get; set; }

        /// <summary>
        /// Initialize the Shipyard tab
        /// </summary>
        /// <param name="m_oSummaryPanel">The summary panel from the economics handler.</param>
        public static void BuildShipyardTab(Panels.Eco_Summary m_oSummaryPanel, Faction CurrentFaction, Population CurrentPopulation, BindingList<ShipClassTN> RetoolTargets)
        {
            MaxShipyardRows = 38;
            MaxShipyardTaskRows = 38;
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
        /// <param name="EligibleClassList">List of shipclasses that this shipyard can produce.</param>
        /// <param name="DamagedShipList">List of damaged ships in orbit.</param>
        /// <param name="ClassesInOrbit">List of shipclasses in orbit around CurrentPopulation.</param>
        /// <param name="ShipsOfClassInOrbit">List of ships in the selected shipclass in orbit around CurrentPopulation.</param>
        public static void RefreshShipyardTab(Panels.Eco_Summary m_oSummaryPanel, Faction CurrentFaction, Population CurrentPopulation, Installation.ShipyardInformation SYInfo,
                                              BindingList<ShipClassTN> RetoolTargets, ref BindingList<ShipClassTN> EligibleClassList, ref BindingList<ShipTN> DamagedShipList, 
                                              ref BindingList<ShipClassTN> ClassesInOrbit, ref BindingList<ShipTN> ShipsOfClassInOrbit)
        {
            /// <summary>
            /// Yeah, just going to constantly declare new variables to pass these along...
            /// </summary>
            ShipsOfClassInOrbit = new BindingList<ShipTN>();
            EligibleClassList = new BindingList<ShipClassTN>();
            DamagedShipList = new BindingList<ShipTN>();
            ClassesInOrbit = new BindingList<ShipClassTN>();

            if (CurrentFaction != null && CurrentPopulation != null && SYInfo != null)
            {
                RefreshShipyardDataGrid(m_oSummaryPanel, CurrentFaction, CurrentPopulation);
                RefreshSYCGroupBox(m_oSummaryPanel, CurrentFaction, CurrentPopulation, SYInfo, RetoolTargets);
                BuildSYCRequiredMinerals(m_oSummaryPanel, CurrentFaction, CurrentPopulation, SYInfo, RetoolTargets);
                RefreshSYTaskGroupBox(m_oSummaryPanel, CurrentFaction, CurrentPopulation, SYInfo, ref EligibleClassList, ref DamagedShipList, ref ClassesInOrbit,
                                                 ref ShipsOfClassInOrbit);
                BuildSYTRequiredMinerals(m_oSummaryPanel, CurrentFaction, CurrentPopulation, SYInfo, EligibleClassList, DamagedShipList, ClassesInOrbit,
                                                 ShipsOfClassInOrbit);

                String Entry = String.Format("Shipyard Complex Activity({0})", SYInfo.Name);
                m_oSummaryPanel.ShipyardTaskGroupBox.Text = Entry;

                Entry = String.Format("Create Task({0})", SYInfo.Name);
                m_oSummaryPanel.ShipyardCreateTaskGroupBox.Text = Entry;

                RefreshShipyardTasksTab(m_oSummaryPanel, CurrentFaction, CurrentPopulation);
            }
            else if (SYInfo == null)
            {
                m_oSummaryPanel.ShipyardDataGrid.ClearSelection();
                /// <summary>
                /// Do not display any rows at all as there is no shipyard for this world.
                /// </summary>
                for (int rowIterator = 0; rowIterator < m_oSummaryPanel.ShipyardDataGrid.Rows.Count; rowIterator++)
                {
                    m_oSummaryPanel.ShipyardDataGrid.Rows[rowIterator].Visible = false;
                }
            }
        }

        /// <summary>
        /// Need an updater function for this groupbox since the retool list can and will change.
        /// </summary>
        /// <param name="m_oSummaryPanel">Panel from economics</param>
        /// <param name="CurrentFaction">Current Faction</param>
        /// <param name="CurrentPopulation">Current Population</param>
        /// <param name="SYInfo">Shipyard information for the selected shipyard.</param>
        /// <param name="RetoolList">List of ships that this shipyard can be retooled to.</param>
        private static void RefreshSYCGroupBox(Panels.Eco_Summary m_oSummaryPanel, Faction CurrentFaction, Population CurrentPopulation, 
                                               Installation.ShipyardInformation SYInfo, BindingList<ShipClassTN> RetoolList)
        {
#warning this doesn't update when a new shipclass is added on its own. the econ page is "shared" by all factions so an event may not be possible there.
            if (RetoolList != null && CurrentFaction != null && SYInfo != null)
            {

                m_oSummaryPanel.NewShipClassComboBox.Items.Clear();
                RetoolList.Clear();
                foreach (ShipClassTN Ship in CurrentFaction.ShipDesigns)
                {
                    /// <summary>
                    /// Ships that are too big may not be in the retool list, and military ships may not be built at commercial yards.
                    /// Naval yards may build all classes of ships, but cap expansion for naval yards is very expensive.
                    /// </summary>
                    if (Ship.SizeTons <= SYInfo.Tonnage && !(Ship.IsMilitary == true && SYInfo.ShipyardType == Constants.ShipyardInfo.SYType.Commercial))
                    {
                        RetoolList.Add(Ship);
                    }
                }

                foreach (ShipClassTN Ship in RetoolList)
                {
                    m_oSummaryPanel.NewShipClassComboBox.Items.Add(Ship);
                }
                if (RetoolList.Count != 0)
                    m_oSummaryPanel.NewShipClassComboBox.SelectedIndex = 0;
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

                AddColumn("Yard", newPadding, m_oSummaryPanel.ShipyardTaskDataGrid, 0);
                AddColumn("TaskDescription", newPadding, m_oSummaryPanel.ShipyardTaskDataGrid, 0);
                AddColumn("Unit Name", newPadding, m_oSummaryPanel.ShipyardTaskDataGrid, 3);
                AddColumn("Progress", newPadding, m_oSummaryPanel.ShipyardTaskDataGrid, 3);
                AddColumn("Assigned Task Group", newPadding, m_oSummaryPanel.ShipyardTaskDataGrid, 3);
                AddColumn("Completion Date", newPadding, m_oSummaryPanel.ShipyardTaskDataGrid, 3);
                AddColumn("ABR", newPadding, m_oSummaryPanel.ShipyardTaskDataGrid, 3);
                AddColumn("Priority", newPadding, m_oSummaryPanel.ShipyardTaskDataGrid, 3);


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

                    using (DataGridViewRow row = new DataGridViewRow())
                    {
                        // setup row height. note that by default they are 22 pixels in height!
                        row.Height = 18;
                        row.Visible = false;
                        m_oSummaryPanel.ShipyardTaskDataGrid.Rows.Add(row);
                    }
                }


            }
            catch
            {
#if LOG4NET_ENABLED
                logger.Error("Something whent wrong Creating ShipyardDataGrid Columns for Eco_ShipyardTabHandler.cs");
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
            foreach (ShipClassTN Ship in RetoolList)
            {
                m_oSummaryPanel.NewShipClassComboBox.Items.Add(Ship);
            }
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
        }

        /// <summary>
        /// Refreshing the shipyard task groupbox will be necessary on player input.
        /// </summary>
        /// <param name="m_oSummaryPanel">The economics handler panel.</param>
        /// <param name="CurrentFaction">Selected faction from the economics handler.</param>
        /// <param name="CurrentPopulation">Selected population from the economics handler.</param>
        /// <param name="SYInfo">Shipyard information from the economics handler.</param>
        /// <param name="EligibleClassList">List of shipclasses that this shipyard can produce.</param>
        /// <param name="DamagedShipList">List of damaged ships in orbit.</param>
        /// <param name="ClassesInOrbit">List of shipclasses in orbit around CurrentPopulation.</param>
        /// <param name="ShipsOfClassInOrbit">List of ships in the selected shipclass in orbit around CurrentPopulation.</param>
        public static void RefreshSYTaskGroupBox(Panels.Eco_Summary m_oSummaryPanel, Faction CurrentFaction, Population CurrentPopulation, Installation.ShipyardInformation SYInfo,
                                                 ref BindingList<ShipClassTN> EligibleClassList, ref BindingList<ShipTN> DamagedShipList, ref BindingList<ShipClassTN> ClassesInOrbit,
                                                 ref BindingList<ShipTN>ShipsOfClassInOrbit)
        {
            if(m_oSummaryPanel.SYTaskTypeComboBox.SelectedIndex != -1)
            {
                Constants.ShipyardInfo.Task CurrentSYTask = (Constants.ShipyardInfo.Task)m_oSummaryPanel.SYTaskTypeComboBox.SelectedIndex;

                switch (CurrentSYTask)
                {
                    case Constants.ShipyardInfo.Task.Construction:                        
                        /// <summary>
                        /// Fill the taskgroups in orbit combo box.
                        /// </summary>
                        m_oSummaryPanel.SYTaskGroupComboBox.Items.Clear();
                        foreach (TaskGroupTN CurrentTaskGroup in CurrentPopulation.Planet.TaskGroupsInOrbit)
                        {
                            if(CurrentTaskGroup.TaskGroupFaction == CurrentFaction)
                                m_oSummaryPanel.SYTaskGroupComboBox.Items.Add(CurrentTaskGroup);
                        }
#warning later on look for Shipyard TG? and should shipyard TG be "special"?
                        if (m_oSummaryPanel.SYTaskGroupComboBox.Items.Count != 0)
                            m_oSummaryPanel.SYTaskGroupComboBox.SelectedIndex = 0;

                        if (SYInfo.AssignedClass != null)
                        {
                            m_oSummaryPanel.SYNewClassComboBox.Items.Clear();

                            GetEligibleClassList(CurrentFaction, SYInfo, ref EligibleClassList);

                            foreach (ShipClassTN CurrentClass in EligibleClassList)
                            {
                                m_oSummaryPanel.SYNewClassComboBox.Items.Add(CurrentClass);
                            }
                            if (m_oSummaryPanel.SYNewClassComboBox.Items.Count != 0)
                                m_oSummaryPanel.SYNewClassComboBox.SelectedIndex = 0;

                            int index = CurrentFaction.ShipDesigns.IndexOf(EligibleClassList[0]);
                            String Entry = String.Format("{0} {1}", CurrentFaction.ShipDesigns[index].Name,
                                                         (CurrentFaction.ShipDesigns[index].ShipsInClass.Count + CurrentFaction.ShipDesigns[index].ShipsUnderConstruction + 1));
                            m_oSummaryPanel.SYShipNameTextBox.Text = Entry;
                        }
                        break;
                    case Constants.ShipyardInfo.Task.Repair:
                        m_oSummaryPanel.RepairRefitScrapLabel.Text = "Repair";
                        GetDamagedShipList(CurrentFaction, CurrentPopulation, ref DamagedShipList);
                        m_oSummaryPanel.RepairRefitScrapShipComboBox.Items.Clear();
                        foreach (ShipTN CurrentShip in DamagedShipList)
                        {
                            m_oSummaryPanel.RepairRefitScrapShipComboBox.Items.Add(CurrentShip);
                        }
                        if (m_oSummaryPanel.RepairRefitScrapShipComboBox.Items.Count != 0)
                            m_oSummaryPanel.RepairRefitScrapShipComboBox.SelectedIndex = 0;
                        break;
                    case Constants.ShipyardInfo.Task.Refit:
                        m_oSummaryPanel.RepairRefitScrapLabel.Text = "Refit";
                        GetShipClassesInOrbit(CurrentFaction, CurrentPopulation, ref ClassesInOrbit);
                        m_oSummaryPanel.RepairRefitScrapClassComboBox.Items.Clear();
                        foreach (ShipClassTN CurrentClass in ClassesInOrbit)
                        {
                            m_oSummaryPanel.RepairRefitScrapClassComboBox.Items.Add(CurrentClass);
                        }
                        ShipClassTN CurrentRRSClass = null;
                        if (m_oSummaryPanel.RepairRefitScrapClassComboBox.Items.Count != 0)
                        {
                            m_oSummaryPanel.RepairRefitScrapClassComboBox.SelectedIndex = 0;
                            CurrentRRSClass = ClassesInOrbit[m_oSummaryPanel.RepairRefitScrapClassComboBox.SelectedIndex];
                        }

                        if (CurrentRRSClass != null)
                        {
                            GetShipsOfClassInOrbit(CurrentFaction, CurrentPopulation, CurrentRRSClass, ref ShipsOfClassInOrbit);
                            m_oSummaryPanel.RepairRefitScrapShipComboBox.Items.Clear();
                            foreach (ShipTN CurrentShip in ShipsOfClassInOrbit)
                            {
                                m_oSummaryPanel.RepairRefitScrapShipComboBox.Items.Add(CurrentShip);
                            }
                            if (m_oSummaryPanel.RepairRefitScrapShipComboBox.Items.Count != 0)
                                m_oSummaryPanel.RepairRefitScrapShipComboBox.SelectedIndex = 0;
                        }
                        break;
                    case Constants.ShipyardInfo.Task.Scrap:
                        m_oSummaryPanel.RepairRefitScrapLabel.Text = "Scrap";
                        GetShipClassesInOrbit(CurrentFaction, CurrentPopulation, ref ClassesInOrbit);
                        m_oSummaryPanel.RepairRefitScrapClassComboBox.Items.Clear();
                        foreach (ShipClassTN CurrentClass in ClassesInOrbit)
                        {
                            m_oSummaryPanel.RepairRefitScrapClassComboBox.Items.Add(CurrentClass);
                        }
                        CurrentRRSClass = null;
                        if (m_oSummaryPanel.RepairRefitScrapClassComboBox.Items.Count != 0)
                        {
                            m_oSummaryPanel.RepairRefitScrapClassComboBox.SelectedIndex = 0;
                            CurrentRRSClass = ClassesInOrbit[m_oSummaryPanel.RepairRefitScrapClassComboBox.SelectedIndex];
                        }

                        if(CurrentRRSClass != null)
                        {
                            GetShipsOfClassInOrbit(CurrentFaction, CurrentPopulation, CurrentRRSClass, ref ShipsOfClassInOrbit);
                            m_oSummaryPanel.RepairRefitScrapShipComboBox.Items.Clear();
                            foreach (ShipTN CurrentShip in ShipsOfClassInOrbit)
                            {
                                m_oSummaryPanel.RepairRefitScrapShipComboBox.Items.Add(CurrentShip);
                            }
                            if (m_oSummaryPanel.RepairRefitScrapShipComboBox.Items.Count != 0)
                                m_oSummaryPanel.RepairRefitScrapShipComboBox.SelectedIndex = 0;
                        }
                        break;
                }
            }
        }


        /// <summary>
        /// Update just the RRSShipComboBox since a full refresh will cause issues with constantly prompting more refreshes.
        /// </summary>
        /// <param name="m_oSummaryPanel">Current economics handler</param>
        /// <param name="CurrentFaction">Selected faction</param>
        /// <param name="CurrentPopulation">Selected population</param>
        /// <param name="ClassesInOrbit">List of shipclasses in orbit around CurrentPopulation.</param>
        /// <param name="ShipsOfClassInOrbit">List of ships in the selected shipclass in orbit around CurrentPopulation.</param>
        public static void RefreshRRSShipComboBox(Panels.Eco_Summary m_oSummaryPanel, Faction CurrentFaction, Population CurrentPopulation, BindingList<ShipClassTN> ClassesInOrbit,
                                                  ref BindingList<ShipTN> ShipsOfClassInOrbit)
        {
            if (m_oSummaryPanel.RepairRefitScrapClassComboBox.SelectedIndex != -1)
            {
                ShipClassTN CurrentRRSClass = ClassesInOrbit[m_oSummaryPanel.RepairRefitScrapClassComboBox.SelectedIndex];

                GetShipsOfClassInOrbit(CurrentFaction, CurrentPopulation, CurrentRRSClass, ref ShipsOfClassInOrbit);

                m_oSummaryPanel.RepairRefitScrapShipComboBox.Items.Clear();
                foreach (ShipTN CurrentShip in ShipsOfClassInOrbit)
                {
                    m_oSummaryPanel.RepairRefitScrapShipComboBox.Items.Add(CurrentShip);
                    if (m_oSummaryPanel.RepairRefitScrapShipComboBox.Items.Count != 0)
                    {
                        m_oSummaryPanel.RepairRefitScrapShipComboBox.SelectedIndex = 0;
                    }
                }
            }
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


                    String ProgString = String.Format("{0:N2}", (SYI.CurrentActivity.Progress * 100.0m));

                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator].Cells[7].Value = ProgString;
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

                    String ProgString = String.Format("{0:N2}",(SYI.CurrentActivity.Progress * 100.0m));

                    m_oSummaryPanel.ShipyardDataGrid.Rows[ShipyardIterator + row].Cells[7].Value = ProgString;
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
                    Installation.ShipyardInformation CostPrototyper = new Installation.ShipyardInformation(CurrentFaction, SYInfo.ShipyardType, 1);
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

                    CostPrototyper.SetShipyardActivity(CurrentFaction, Activity, RetoolTarget, NewCapLimit);

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

        /// <summary>
        /// Build the Shipyard task required minerals box.
        /// </summary>
        /// <param name="m_oSummaryPanel">Panel from the economics handler</param>
        /// <param name="CurrentFaction">Currently selected faction.</param>
        /// <param name="CurrentPopulation">Currently selected population</param>
        /// <param name="SYInfo">Currently selected shipyard on currently selected population belonging to currently selected faction</param>
        /// <param name="EligibleClassList">List of shipclasses that this shipyard can produce.</param>
        /// <param name="DamagedShipList">List of damaged ships in orbit.</param>
        /// <param name="ClassesInOrbit">List of shipclasses in orbit around CurrentPopulation.</param>
        /// <param name="ShipsOfClassInOrbit">List of ships in the selected shipclass in orbit around CurrentPopulation.</param> 
        public static void BuildSYTRequiredMinerals(Panels.Eco_Summary m_oSummaryPanel, Faction CurrentFaction, Population CurrentPopulation, Installation.ShipyardInformation SYInfo,
                                                    BindingList<ShipClassTN> EligibleClassList, BindingList<ShipTN> DamagedShipList, BindingList<ShipClassTN> ClassesInOrbit,
                                                    BindingList<ShipTN> ShipsOfClassInOrbit)
        {
            if (m_oSummaryPanel.SYTaskTypeComboBox.SelectedIndex != -1 && m_oSummaryPanel.SYTaskGroupComboBox.SelectedIndex != -1 && SYInfo != null &&
                (m_oSummaryPanel.SYNewClassComboBox.SelectedIndex != -1 || m_oSummaryPanel.RepairRefitScrapShipComboBox.SelectedIndex != -1))
            {
                m_oSummaryPanel.ShipRequiredMaterialsListBox.Items.Clear();

                Installation.ShipyardInformation CostPrototyper = new Installation.ShipyardInformation(CurrentFaction, SYInfo.ShipyardType,1);
                CostPrototyper.Tonnage = SYInfo.Tonnage;
                CostPrototyper.Slipways = SYInfo.Slipways;
                CostPrototyper.ModRate = SYInfo.ModRate;
                CostPrototyper.AssignedClass = SYInfo.AssignedClass;

                ShipTN CurrentShip = null;
                ShipClassTN ConstructRefit = null;
                TaskGroupTN TargetTG = null;
                int TGIndex = -1;

                /// <summary>
                /// I'm not storing a faction only list of taskgroups in orbit anywhere, so lets calculate that here.
                /// </summary>
                foreach (TaskGroupTN CurrentTaskGroup in CurrentPopulation.Planet.TaskGroupsInOrbit)
                {
                    if (CurrentTaskGroup.TaskGroupFaction == CurrentFaction)
                    {
                        if (TGIndex == m_oSummaryPanel.SYTaskGroupComboBox.SelectedIndex)
                        {
                            TargetTG = CurrentTaskGroup;
                            break;
                        }
                        TGIndex++;
                    }
                }

                /// <summary>
                /// No TG was found so create one, the shipyard will want a tg in any event.
                /// </summary>
                if (TGIndex == -1)
                {
                    CurrentFaction.AddNewTaskGroup("Shipyard TG", CurrentPopulation.Planet, CurrentPopulation.Planet.Position.System);

                    /// <summary>
                    /// Run this loop again as a different faction could have a taskgroup in orbit.
                    /// </summary>
                    foreach (TaskGroupTN CurrentTaskGroup in CurrentPopulation.Planet.TaskGroupsInOrbit)
                    {
                        if (CurrentTaskGroup.TaskGroupFaction == CurrentFaction)
                        {
                            TGIndex++;
                            if (TGIndex == m_oSummaryPanel.SYTaskGroupComboBox.SelectedIndex)
                            {
                                TargetTG = CurrentTaskGroup;
                                break;
                            }
                        }
                    }
                }

                Constants.ShipyardInfo.Task SYITask = (Constants.ShipyardInfo.Task)m_oSummaryPanel.SYTaskTypeComboBox.SelectedIndex;


                int BaseBuildRate = SYInfo.CalcShipBuildRate(CurrentFaction, CurrentPopulation);

                if ((int)SYITask != -1)
                {
                    switch (SYITask)
                    {
                        case Constants.ShipyardInfo.Task.Construction:
                            int newShipIndex = m_oSummaryPanel.SYNewClassComboBox.SelectedIndex;
                            if (newShipIndex != -1 && EligibleClassList.Count > newShipIndex)
                            {
                                ConstructRefit = EligibleClassList[newShipIndex];
                            }
                            break;
                        case Constants.ShipyardInfo.Task.Repair:
                            int CurrentShipIndex = m_oSummaryPanel.RepairRefitScrapShipComboBox.SelectedIndex;
                            if (CurrentShipIndex != -1 && DamagedShipList.Count > CurrentShipIndex)
                            {
                                CurrentShip = DamagedShipList[CurrentShipIndex];
                            }
                            break;
                        case Constants.ShipyardInfo.Task.Refit:
                            newShipIndex = m_oSummaryPanel.SYNewClassComboBox.SelectedIndex;
                            if (newShipIndex != -1 && EligibleClassList.Count > newShipIndex)
                            {
                                ConstructRefit = EligibleClassList[newShipIndex];
                            }

                            CurrentShipIndex = m_oSummaryPanel.RepairRefitScrapShipComboBox.SelectedIndex;
                            if (CurrentShipIndex != -1 && ShipsOfClassInOrbit.Count > CurrentShipIndex)
                            {
                                CurrentShip = ShipsOfClassInOrbit[CurrentShipIndex];
                            }
                            break;
                        case Constants.ShipyardInfo.Task.Scrap:
                            CurrentShipIndex = m_oSummaryPanel.RepairRefitScrapShipComboBox.SelectedIndex;
                            if (CurrentShipIndex != -1 && ShipsOfClassInOrbit.Count > CurrentShipIndex)
                            {
                                CurrentShip = ShipsOfClassInOrbit[CurrentShipIndex];
                            }
                            break;
                    }

                    /// <summary>
                    /// Faction swapping can cause some problems.
                    /// </summary>
                    if (CurrentShip == null && ConstructRefit == null)
                        return;

                    Installation.ShipyardInformation.ShipyardTask NewTask = new Installation.ShipyardInformation.ShipyardTask(CurrentShip, SYITask, TargetTG, BaseBuildRate, m_oSummaryPanel.SYShipNameTextBox.Text, ConstructRefit);
                    CostPrototyper.BuildingShips.Add(NewTask);

                    m_oSummaryPanel.SYTaskCostTextBox.Text = CostPrototyper.BuildingShips[0].Cost.ToString();
                    m_oSummaryPanel.SYTaskCompletionDateTextBox.Text = CostPrototyper.BuildingShips[0].CompletionDate.ToShortDateString();

                    for (int MineralIterator = 0; MineralIterator < Constants.Minerals.NO_OF_MINERIALS; MineralIterator++)
                    {

                        if (CostPrototyper.BuildingShips[0].minerialsCost[MineralIterator] != 0.0m)
                        {
                            string FormattedMineralTotal = CostPrototyper.BuildingShips[0].minerialsCost[MineralIterator].ToString("#,##0");

                            String CostString = String.Format("{0} {1} ({2})", (Constants.Minerals.MinerialNames)MineralIterator, FormattedMineralTotal, CurrentPopulation.Minerials[MineralIterator]);
                            m_oSummaryPanel.ShipRequiredMaterialsListBox.Items.Add(CostString);
                        }
                    }
                }
            }
            else
            {
                /// <summary>
                /// There is no cost to calculate so print this instead.
                /// </summary>
                m_oSummaryPanel.SYTaskCostTextBox.Text = "N/A";
                m_oSummaryPanel.SYTaskCompletionDateTextBox.Text = "N/A";
                m_oSummaryPanel.ShipRequiredMaterialsListBox.Items.Clear();
            }
        }


        /// <summary>
        /// Build the list of shipyard tasks at this population.
        /// </summary>
        /// <param name="m_oSummaryPanel"></param>
        /// <param name="CurrentFaction"></param>
        /// <param name="CurrentPopulation"></param>
        public static void RefreshShipyardTasksTab(Panels.Eco_Summary m_oSummaryPanel, Faction CurrentFaction, Population CurrentPopulation)
        {

            List<Installation.ShipyardInformation.ShipyardTask> SortedList = CurrentPopulation.ShipyardTasks.Keys.ToList().OrderBy(o => o.Priority).ToList();

            int row = 0;
            foreach (Installation.ShipyardInformation.ShipyardTask Task in SortedList)
            {
                m_oSummaryPanel.ShipyardTaskDataGrid.Rows[row].Visible = true;

                m_oSummaryPanel.ShipyardTaskDataGrid.Rows[row].Cells[0].Value = CurrentPopulation.ShipyardTasks[Task].Name;

                String Entry = "N/A";

                switch (Task.CurrentTask)
                {
                    case Constants.ShipyardInfo.Task.Construction:
                        Entry = String.Format("Build {0}", Task.ConstructRefitTarget);
                        break;
                    case Constants.ShipyardInfo.Task.Repair:
                        Entry = String.Format("Repair {0}", Task.CurrentShip);
                        break;
                    case Constants.ShipyardInfo.Task.Refit:
                        Entry = String.Format("Refit {0} to {1}", Task.CurrentShip, Task.ConstructRefitTarget);
                        break;
                    case Constants.ShipyardInfo.Task.Scrap:
                        Entry = String.Format("Scrap {0}", Task.CurrentShip);
                        break;
                }

                m_oSummaryPanel.ShipyardTaskDataGrid.Rows[row].Cells[1].Value = Entry;

                switch (Task.CurrentTask)
                {
                    case Constants.ShipyardInfo.Task.Construction:
                        Entry = String.Format("{0}", Task.Title);
                        break;
                    case Constants.ShipyardInfo.Task.Repair:
                        Entry = String.Format("{0}", Task.CurrentShip);
                        break;
                    case Constants.ShipyardInfo.Task.Refit:
                        Entry = String.Format("{0}", Task.CurrentShip);
                        break;
                    case Constants.ShipyardInfo.Task.Scrap:
                        Entry = String.Format("{0}", Task.CurrentShip);
                        break;
                }

                m_oSummaryPanel.ShipyardTaskDataGrid.Rows[row].Cells[2].Value = Entry;

                String ProgString = String.Format("{0:N2}", (Task.Progress * 100.0m));

                m_oSummaryPanel.ShipyardTaskDataGrid.Rows[row].Cells[3].Value = ProgString;
                m_oSummaryPanel.ShipyardTaskDataGrid.Rows[row].Cells[4].Value = Task.AssignedTaskGroup;
                m_oSummaryPanel.ShipyardTaskDataGrid.Rows[row].Cells[5].Value = Task.CompletionDate.ToShortDateString();
                m_oSummaryPanel.ShipyardTaskDataGrid.Rows[row].Cells[6].Value = Task.ABR;

                if (Task.IsPaused() == true)
                    m_oSummaryPanel.ShipyardTaskDataGrid.Rows[row].Cells[7].Value = "Paused";
                else
                    m_oSummaryPanel.ShipyardTaskDataGrid.Rows[row].Cells[7].Value = Task.Priority;


                row++;

                if (row == MaxShipyardTaskRows)
                {
                    using (DataGridViewRow NewRow = new DataGridViewRow())
                    {
                        // setup row height. note that by default they are 22 pixels in height!
                        NewRow.Height = 18;
                        NewRow.Visible = false;
                        m_oSummaryPanel.ShipyardTaskDataGrid.Rows.Add(NewRow);
                    }
                    MaxShipyardTaskRows++;
                }
            }

            /// <summary>
            /// Any rows that aren't being used should be set to invisible. They will still have data from previous ship tasks that I don't care to clear out since the user can't see the rows anyway.
            /// </summary>
            for (int rowIterator = row; row < MaxShipyardTaskRows; row++)
            {
                m_oSummaryPanel.ShipyardTaskDataGrid.Rows[row].Visible = false;
            }
        }


        /// <summary>
        /// Get a list of the shipclasses in orbit. this wll be needed to help prune repair/refit/scrap operation options.
        /// </summary>
        /// <param name="CurrentFaction">Current faction from the economics handler</param>
        /// <param name="CurrentPopulation">Current Population from the economics handler.</param>  
        /// <param name="ClassesInOrbit">List of shipclasses in orbit around CurrentPopulation.</param>        
        private static void GetShipClassesInOrbit(Faction CurrentFaction, Population CurrentPopulation, ref BindingList<ShipClassTN> ClassesInOrbit)
        {
            ClassesInOrbit.Clear();
            foreach (TaskGroupTN CurrentTaskGroup in CurrentPopulation.Planet.TaskGroupsInOrbit)
            {
                if (CurrentTaskGroup.TaskGroupFaction == CurrentFaction)
                {
                    foreach (ShipTN CurrentShip in CurrentTaskGroup.Ships)
                    {
                        if (ClassesInOrbit.Contains(CurrentShip.ShipClass) == false)
                            ClassesInOrbit.Add(CurrentShip.ShipClass);
                    }
                }
            }
        }
        
        /// <summary>
        /// Now I want a list of the ships of a specific class that are in orbit. these will potentially be targets for repair, refit, or scrap operations.
        /// </summary>
        /// <param name="CurrentFaction">Economics handler selected faction.</param>
        /// <param name="CurrentPopulation">Population from the economics handler.</param>
        /// <param name="CurrentShipClass">Shipclass selected via the RepairRefitScrapClassComboBox</param>
        /// <param name="ShipsOfClassInOrbit">List of ships in the selected shipclass in orbit around CurrentPopulation.</param>
        private static void GetShipsOfClassInOrbit(Faction CurrentFaction, Population CurrentPopulation, ShipClassTN CurrentShipClass, ref BindingList<ShipTN> ShipsOfClassInOrbit)
        {
            ShipsOfClassInOrbit.Clear();
            foreach (TaskGroupTN CurrentTaskGroup in CurrentPopulation.Planet.TaskGroupsInOrbit)
            {
                if (CurrentTaskGroup.TaskGroupFaction == CurrentFaction)
                {
                    foreach (ShipTN CurrentShip in CurrentTaskGroup.Ships)
                    {
                        if (CurrentShip.ShipClass == CurrentShipClass)
                            ShipsOfClassInOrbit.Add(CurrentShip);
                    }
                }
            }
        }

        /// <summary>
        /// This function produces a list of ships that have taken Armor or component damage. repair will need this.
        /// </summary>
        /// <param name="CurrentPopulation">Population selected by the economics handler.</param>
        /// <param name="DamagedShipsInOrbit">list of damaged ships this function will produce.</param>
        private static void GetDamagedShipList(Faction CurrentFaction, Population CurrentPopulation, ref BindingList<ShipTN> DamagedShipList)
        {
            DamagedShipList.Clear();
            foreach (TaskGroupTN CurrentTaskGroup in CurrentPopulation.Planet.TaskGroupsInOrbit)
            {
                if (CurrentTaskGroup.TaskGroupFaction == CurrentFaction)
                {
                    foreach (ShipTN CurrentShip in CurrentTaskGroup.Ships)
                    {
                        /// <summary>
                        /// Either a component is destroyed or the ship has taken armour damage.
                        /// </summary>
                        if (CurrentShip.DestroyedComponents.Count != 0 || CurrentShip.ShipArmor.isDamaged == true)
                            DamagedShipList.Add(CurrentShip);
                    }
                }
            }
        }

        /// <summary>
        /// This function gets the list of shipclasses this shipyard can build. In order to be considered eligible the class must be locked, and thus not alterable.
        /// eligible classes are those that would cost within 20% of cost to refit this shipclass towards.
        /// </summary>
        /// <param name="CurrentFaction">Current faction from the economics handler.</param>
        /// <param name="SYInfo">Currently selected shipyard.</param>
        /// <param name="EligibleClassList">List of shipclasses that this shipyard can produce.</param>
        private static void GetEligibleClassList(Faction CurrentFaction, Installation.ShipyardInformation SYInfo, ref BindingList<ShipClassTN> EligibleClassList)
        {
            if (SYInfo.AssignedClass == null)
            {
                return;
            }

            EligibleClassList.Clear();

            /// <summary>
            /// Shipyards may always build the ship that they are tooled for.
            /// </summary>
            EligibleClassList.Add(SYInfo.AssignedClass);

            /// <summary>
            /// If the total refit cost is less than this, the CurrentClass is eligible.
            /// </summary>
            decimal RefitThreshold = SYInfo.AssignedClass.BuildPointCost * 0.2m;

            /// <summary>
            /// component definition and count lists for the assigned class. for refit purposes we don't care about any specialized component functionality, just cost here.
            /// </summary>
            BindingList<ComponentDefTN> AssignedClassComponents = SYInfo.AssignedClass.ListOfComponentDefs;
            BindingList<short> AssignedClassComponentCounts = SYInfo.AssignedClass.ListOfComponentDefsCount;

            foreach (ShipClassTN CurrentClass in CurrentFaction.ShipDesigns)
            {
                /// <summary>
                /// Assigned class is already set to be built, and shouldn't ever be an "eligible class"
                /// </summary>
                if (CurrentClass == SYInfo.AssignedClass)
                    continue;

                /// <summary>
                /// Military ships are not buildable at commercial yards. Naval yards can build commercial ships however.
                /// </summary>
                if (CurrentClass.IsMilitary == true && SYInfo.ShipyardType == Constants.ShipyardInfo.SYType.Commercial)
                    continue;

                /// <summary>
                /// unlocked classes can be edited so I do not wish to have them included here.
                /// </summary>
                if(CurrentClass.IsLocked == true)
                {
                    decimal TotalRefitCost = SYInfo.AssignedClass.GetRefitCost(CurrentClass);

                    if (TotalRefitCost <= RefitThreshold)
                        EligibleClassList.Add(CurrentClass);
                }
            }
        }
    }
}
