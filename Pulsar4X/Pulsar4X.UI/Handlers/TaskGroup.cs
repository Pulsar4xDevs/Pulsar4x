using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using WeifenLuo.WinFormsUI.Docking;
using Pulsar4X.UI.ViewModels;
using Pulsar4X.Entities;
using Pulsar4X.Stargen;
using log4net.Config;
using log4net;
using Pulsar4X.UI.GLUtilities;
using Pulsar4X.UI.SceenGraph;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Pulsar4X.UI.Handlers
{
    public class TaskGroup
    {
        public enum StratCells
        {
            Name,
            ShipClass,
            Fuel,
            Ammo,
            Shields,
            Thermal,
            MaintSup,
            MaintClock,
            Crew,
            Morale,
            Grade,
            Training,
            Count

        }

        public enum SystemLocationListType
        {
            Planets,
            Contacts,
            TaskGroups,
            Waypoints,
            Count
        }

        /// <summary>
        /// TG Logger:
        /// </summary>
        public static readonly ILog logger = LogManager.GetLogger(typeof(TaskGroup));

        /// <summary>
        /// Panel for taskgroup related stuff. Opengl shouldn't be used here I don't think, but I'm not sure. Included everything from SystemMap.cs anyway.
        /// </summary>
        Panels.TaskGroup_Panel m_oTaskGroupPanel;
        Panels.ClassDes_RenameClass m_oRenameTaskGroupPanel;

        /// <summary>
        /// Misspelling intentional to keep this in line with systemMap's misspelling.
        /// </summary>
        private Pulsar4X.Entities.TaskGroupTN m_oCurrnetTaskGroup;
        public Pulsar4X.Entities.TaskGroupTN CurrentTaskGroup
        {
            get { return m_oCurrnetTaskGroup; }
            set
            {
                if (value != m_oCurrnetTaskGroup)
                {
                    m_oCurrnetTaskGroup = value;
                    RefreshTGPanel();
                }
            }
        }

        /// <summary>
        /// Current faction
        /// </summary>
        private Pulsar4X.Entities.Faction m_oCurrnetFaction;
        public Pulsar4X.Entities.Faction CurrentFaction
        {
            get { return m_oCurrnetFaction; }
            set
            {
                if (value != m_oCurrnetFaction)
                {
                    m_oCurrnetFaction = value;
                    m_oCurrnetTaskGroup = m_oCurrnetFaction.TaskGroups[0];
                    RefreshTGPanel();
                }
            }

        }

        /// <summary>
        /// The view model this handler uses.
        /// </summary>
        public ViewModels.TaskGroupViewModel VM { get; set; }

        /// <summary>
        /// Index of interesting locations for the SystemLocationList
        /// </summary>
        private BindingList<int> SystemLocationListIndices { get; set; }

        /// <summary>
        /// What those indices really mean
        /// </summary>
        private BindingList<SystemLocationListType> SystemLocationListTypes { get; set; }


        /// <summary>
        /// This reference I need to taskgroup renames to appear on the systemmap.
        /// </summary>
        public Pulsar4X.UI.Handlers.SystemMap SystemMapReference { get; set; }



        /// <summary>
        /// Constructor for this handler.
        /// </summary>
        public TaskGroup()
        {
            m_oTaskGroupPanel = new Panels.TaskGroup_Panel();
            m_oRenameTaskGroupPanel = new Panels.ClassDes_RenameClass();

            /// <summary>
            /// setup viewmodel:
            /// Bind TG Selection Combo Box.
            /// Bind faction Selection as well.
            /// </summary>
            VM = new ViewModels.TaskGroupViewModel();

            SystemLocationListIndices = new BindingList<int>();
            SystemLocationListTypes = new BindingList<SystemLocationListType>();

            /// <summary>
            /// Set up the faction bindings. FactionSelectionComboBox is in the TaskGroup_Panel.designer.cs file.
            /// </summary>
            m_oTaskGroupPanel.FactionSelectionComboBox.Bind(c => c.DataSource, VM, d => d.Factions);
            m_oTaskGroupPanel.FactionSelectionComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentFaction, DataSourceUpdateMode.OnPropertyChanged);
            m_oTaskGroupPanel.FactionSelectionComboBox.DisplayMember = "Name";
            VM.FactionChanged += (s, args) => CurrentFaction = VM.CurrentFaction;
            CurrentFaction = VM.CurrentFaction;
            m_oTaskGroupPanel.FactionSelectionComboBox.SelectedIndexChanged += (s, args) => m_oTaskGroupPanel.FactionSelectionComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oTaskGroupPanel.FactionSelectionComboBox.SelectedIndexChanged += new EventHandler(FactionSelectComboBox_SelectedIndexChanged);


            /// <summary>
            /// Bind the TaskGroup to the appropriate combo box.
            /// </summary>
            m_oTaskGroupPanel.TaskGroupSelectionComboBox.Bind(c => c.DataSource, VM, d => d.TaskGroups);
            m_oTaskGroupPanel.TaskGroupSelectionComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentTaskGroup, DataSourceUpdateMode.OnPropertyChanged);
            m_oTaskGroupPanel.TaskGroupSelectionComboBox.DisplayMember = "Name";
            VM.TaskGroupChanged += (s, args) => CurrentTaskGroup = VM.CurrentTaskGroup;
            CurrentTaskGroup = VM.CurrentTaskGroup;
            m_oTaskGroupPanel.TaskGroupSelectionComboBox.SelectedIndexChanged += (s, args) => m_oTaskGroupPanel.TaskGroupSelectionComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oTaskGroupPanel.TaskGroupSelectionComboBox.SelectedIndexChanged += new EventHandler(TaskGroupSelectComboBox_SelectedIndexChanged);

            /// <summary>
            /// Setup TG Data Grid:
            /// </summary>
            m_oTaskGroupPanel.TaskGroupDataGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            m_oTaskGroupPanel.TaskGroupDataGrid.RowHeadersVisible = false;
            m_oTaskGroupPanel.TaskGroupDataGrid.AutoGenerateColumns = false;
            SetupShipDataGrid();

            m_oTaskGroupPanel.SystemLocationsListBox.SelectedIndexChanged += new EventHandler(SystemLocationListBox_SelectedIndexChanged);
            m_oTaskGroupPanel.DisplayContactsCheckBox.CheckStateChanged += new EventHandler(DisplayContactsCheckBox_CheckChanged);
            m_oTaskGroupPanel.DisplayTaskGroupsCheckBox.CheckStateChanged += new EventHandler(DisplayTaskGroupsCheckBox_CheckChanged);
            m_oTaskGroupPanel.DisplayWaypointsCheckBox.CheckStateChanged += new EventHandler(DisplayWaypointsCheckBox_CheckChanged);

            m_oTaskGroupPanel.NewTaskGroupButton.Click += new EventHandler(NewTaskGroupButton_Click);
            m_oTaskGroupPanel.RenameTaskGroupButton.Click += new EventHandler(RenameTaskGroupButton_Click);
            m_oTaskGroupPanel.SetSpeedButton.Click += new EventHandler(SetSpeedButton_Clicked);
            m_oTaskGroupPanel.MaxSpeedButton.Click += new EventHandler(MaxSpeedButton_Clicked);
            m_oTaskGroupPanel.AddMoveButton.Click += new EventHandler(AddMoveButton_Clicked);
            m_oTaskGroupPanel.RemoveButton.Click += new EventHandler(RemoveButton_Clicked);
            m_oTaskGroupPanel.RemoveAllButton.Click += new EventHandler(RemoveAllButton_Clicked);

            m_oTaskGroupPanel.CurrentTDRadioButton.CheckedChanged += new EventHandler(CurrentTDRadioButton_CheckChanged);
            m_oTaskGroupPanel.AllOrdersTDRadioButton.CheckedChanged += new EventHandler(AllOrdersTDRadioButton_CheckChanged);

            /// <summary>
            /// Rename Class Button Handlers:
            /// </summary>
            m_oRenameTaskGroupPanel.NewClassNameLabel.Text = "Please enter a new taskgroup name";
            m_oRenameTaskGroupPanel.OKButton.Click += new EventHandler(OKButton_Click);
            m_oRenameTaskGroupPanel.CancelRenameButton.Click += new EventHandler(CancelRenameButton_Click);
            m_oRenameTaskGroupPanel.RenameClassTextBox.KeyPress += new KeyPressEventHandler(RenameClassTextBox_KeyPress);

            RefreshTGPanel();
        }

        /// <summary>
        /// Handle Faction Changes here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FactionSelectComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshTGPanel();
        }

        /// <summary>
        /// Handle TaskGroup Changes here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TaskGroupSelectComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshTGPanel();
        }

        /// <summary>
        /// If the contacts checkbox is changed:
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayContactsCheckBox_CheckChanged(object sender, EventArgs e)
        {
            BuildSystemLocationList();
            ClearActionList();
        }

        /// <summary>
        /// If Taskgroups checkbox is changed:
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayTaskGroupsCheckBox_CheckChanged(object sender, EventArgs e)
        {
            BuildSystemLocationList();
            ClearActionList();
        }

        /// <summary>
        /// If Waypoints checkbox is changed:
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayWaypointsCheckBox_CheckChanged(object sender, EventArgs e)
        {
            BuildSystemLocationList();
            ClearActionList();
        }

        /// <summary>
        /// The TimeDistance radio buttons are controlled here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentTDRadioButton_CheckChanged(object sender, EventArgs e)
        {
            CalculateTimeDistance();
        }

        /// <summary>
        /// And here as well for all orders.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AllOrdersTDRadioButton_CheckChanged(object sender, EventArgs e)
        {
            CalculateTimeDistance();
        }

        /// <summary>
        /// If a location is chosen build the action list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SystemLocationListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            BuildActionList();
        }

        /// <summary>
        /// Create a new taskgroup.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewTaskGroupButton_Click(object sender, EventArgs e)
        {
            String Title = String.Format("New Taskgroup #{0}",m_oCurrnetFaction.TaskGroups.Count);
            m_oCurrnetFaction.AddNewTaskGroup(Title, m_oCurrnetFaction.Capitol, m_oCurrnetFaction.Capitol.Primary.StarSystem);

            m_oTaskGroupPanel.TaskGroupSelectionComboBox.SelectedIndex = (m_oTaskGroupPanel.TaskGroupSelectionComboBox.Items.Count - 1);
        }

        #region TG Rename Panel
        /// <summary>
        /// Renames the current taskgroup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RenameTaskGroupButton_Click(object sender, EventArgs e)
        {
            if (m_oCurrnetTaskGroup != null)
            {

                m_oRenameTaskGroupPanel.RenameClassTextBox.Text = m_oCurrnetTaskGroup.Name;

                Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
                m_oRenameTaskGroupPanel.ShowDialog();
                m_oRenameTaskGroupPanel.RenameClassTextBox.Focus();
                Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
            } 
        }

        /// <summary>
        /// Actually change the name
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OKButton_Click(object sender, EventArgs e)
        {
            RenameTaskGroup();

            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oRenameTaskGroupPanel.Hide();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        /// <summary>
        /// Same as above, only on enter pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RenameClassTextBox_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                RenameTaskGroup();
                

                Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
                m_oRenameTaskGroupPanel.Hide();
                Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
            }
        }

        /// <summary>
        /// Disregard the rename.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelRenameButton_Click(object sender, EventArgs e)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oRenameTaskGroupPanel.Hide();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        /// <summary>
        /// Handle name changes here
        /// </summary>
        private void RenameTaskGroup()
        {
            m_oCurrnetTaskGroup.Name = m_oRenameTaskGroupPanel.RenameClassTextBox.Text;
        }

        #endregion

        /// <summary>
        /// Sets the speed of the taskgroup to its user entered value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetSpeedButton_Clicked(object sender, EventArgs e)
        {
            string newSpeed = m_oTaskGroupPanel.SetSpeedTextBox.Text;
            int value;
            bool CheckString = int.TryParse(newSpeed, out value);

            if(CheckString == true)
                CurrentTaskGroup.SetSpeed(value);

            CalculateTimeDistance();
        }

        /// <summary>
        /// Sets the speed of the taskgroup to its maximum.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MaxSpeedButton_Clicked(object sender, EventArgs e)
        {
            CurrentTaskGroup.SetSpeed(CurrentTaskGroup.MaxSpeed);
            m_oTaskGroupPanel.SetSpeedTextBox.Text = CurrentTaskGroup.MaxSpeed.ToString();

            CalculateTimeDistance();
        }

        /// <summary>
        /// Adds the selected order to the task group's list of orders. This function is going to get giant.
        /// Not handled: Order filtering, delays, secondary and tertiary orders.
        /// As for adding new things here, look at the logic for how they were added to the SystemLocationListBox to derive how to get to them for this code.
        /// Don't forget filtering of destinations by survey status.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddMoveButton_Clicked(object sender, EventArgs e)
        {
            /// <summary>
            /// Planets, Contacts, TG, WP
            /// </summary>
            int PlaceIndex = m_oTaskGroupPanel.SystemLocationsListBox.SelectedIndex;


            int newIndex;
            int Counter;
            Orders NewOrder;

            if (PlaceIndex != -1)
            {
                int ActionIndex = m_oTaskGroupPanel.AvailableActionsListBox.SelectedIndex;

                if (ActionIndex != -1)
                {
                    /// <summary>
                    /// Now figure out what the hell order this would be.
                    /// </summary>

                    for (int loop = 0; loop < SystemLocationListIndices.Count; loop++)
                    {
                        if (PlaceIndex < SystemLocationListIndices[loop])
                        {
                            if (loop == 0)
                            {
                                newIndex = PlaceIndex;
                            }
                            else
                            {
                                newIndex = PlaceIndex - SystemLocationListIndices[loop-1];
                            }

                            /// <summary>
                            /// Which type is this? 
                            /// </summary>
                            switch (SystemLocationListTypes[loop-1])
                            {
                                #region Planets
                                case SystemLocationListType.Planets :
                                    /// <summary>
                                    /// This is currently the planets section.
                                    /// </summary>
                                    Counter = 0;
                                    for (int loop2 = 0; loop2 < CurrentTaskGroup.Contact.CurrentSystem.Stars.Count; loop2++)
                                    {
                                        Counter = Counter + CurrentTaskGroup.Contact.CurrentSystem.Stars[loop2].Planets.Count;

                                        if (newIndex < Counter)
                                        {
                                            int PlanetIndex = newIndex - (Counter - CurrentTaskGroup.Contact.CurrentSystem.Stars[loop2].Planets.Count);

                                            NewOrder = new Orders((Constants.ShipTN.OrderType)ActionIndex, -1, -1, 0, CurrentTaskGroup.Contact.CurrentSystem.Stars[loop2].Planets[PlanetIndex]);
                                            CurrentTaskGroup.IssueOrder(NewOrder);
                                            break;
                                        }
                                    }


                                break;
                                #endregion

                                #region Contacts
                                case SystemLocationListType.Contacts:
                                /// <summary>
                                /// This is Contacts
                                /// </summary>
                                Counter = 0;

                                if(CurrentFaction.DetectedContactLists.ContainsKey(CurrentTaskGroup.Contact.CurrentSystem) == true)
                                {
                                    foreach (KeyValuePair<ShipTN, FactionContact> pair in CurrentFaction.DetectedContactLists[CurrentTaskGroup.Contact.CurrentSystem].DetectedContacts)
                                    {
                                        if (Counter == newIndex)
                                        {
                                            Counter = 0;
                                            NewOrder = new Orders((Constants.ShipTN.OrderType)ActionIndex, -1, -1, 0, pair.Key.ShipsTaskGroup);
                                            CurrentTaskGroup.IssueOrder(NewOrder);
                                            pair.Key.TaskGroupsOrdered.Add(CurrentTaskGroup);
                                            break;
                                        }

                                        Counter++;
                                    }
                                }

                                if (Counter != 0)
                                {
                                    logger.Error("Contact selected for AddMoveButton_Clicked was not in the contact list somehow. Taskgroup.cs under handlers.");
                                }
                                
                                break;
                                #endregion

                                #region TaskGroups
                                case SystemLocationListType.TaskGroups:
                                /// <summary>
                                /// This is Taskgroups.
                                /// </summary>
                                Counter = 0;
                                TaskGroupTN TargetOfOrder = null;

                                for (int loop2 = 0; loop2 < CurrentTaskGroup.Contact.CurrentSystem.SystemContactList.Count; loop2++)
                                {
                                    if (CurrentTaskGroup.Contact.CurrentSystem.SystemContactList[loop2].SSEntity == StarSystemEntityType.TaskGroup)
                                    {
                                        if (CurrentTaskGroup.Contact.CurrentSystem.SystemContactList[loop2].TaskGroup != CurrentTaskGroup &&
                                            CurrentTaskGroup.Contact.CurrentSystem.SystemContactList[loop2].TaskGroup.TaskGroupFaction == CurrentFaction)
                                        {
                                            if (Counter == newIndex)
                                            {
                                                TargetOfOrder = CurrentTaskGroup.Contact.CurrentSystem.SystemContactList[loop2].TaskGroup;
                                                break;
                                            }

                                            Counter++;
                                        }
                                    }
                                }

                                if (TargetOfOrder != null)
                                {
                                    NewOrder = new Orders((Constants.ShipTN.OrderType)ActionIndex, -1, -1, 0, TargetOfOrder);
                                    CurrentTaskGroup.IssueOrder(NewOrder);
                                    TargetOfOrder.TaskGroupsOrdered.Add(CurrentTaskGroup);
                                }
                                else
                                {
                                    /// <summary>
                                    /// Error condition here. How would this even happen? and yet I know that it will.
                                    /// </summary>
                                    logger.Error("Unknown taskgroup selected as order target in AddMoveButton_Clicked in TaskGroup.cs under Handlers.");
                                }
                                break;
                                #endregion

                                #region Waypoints
                                case SystemLocationListType.Waypoints:
                                /// <summary>
                                /// This is waypoints.
                                /// </summary>

                                if (CurrentTaskGroup.Contact.CurrentSystem.Waypoints.Count <= newIndex)
                                {
                                    logger.Error("Index out of range Selection for waypoint order target int AddMoveButton_Clicked in TaskGroup.cs under Handlers.");
                                }


                                NewOrder = new Orders((Constants.ShipTN.OrderType)ActionIndex, -1, -1, 0, CurrentTaskGroup.Contact.CurrentSystem.Waypoints[newIndex]);
                                CurrentTaskGroup.IssueOrder(NewOrder);
                                break;
                                #endregion

                                #region Count
                                case SystemLocationListType.Count :
                                /// <summary>
                                /// Do nothing, this is here to mark the end of the list.
                                /// </summary>
                                break;
                                #endregion
                            }

                            break;
                        }
                    }
                }
            }

            BuildPlottedMoveList();
            CalculateTimeDistance();
        }

        /// <summary>
        /// Removes the top order.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveButton_Clicked(object sender, EventArgs e)
        {
            if (CurrentTaskGroup.TaskGroupOrders.Count != 0)
            {
                CurrentTaskGroup.TaskGroupOrders.Remove(CurrentTaskGroup.TaskGroupOrders.Last());
                ClearActionList();
                BuildPlottedMoveList();
                CalculateTimeDistance();
            }
        }

        /// <summary>
        /// removes all orders
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveAllButton_Clicked(object sender, EventArgs e)
        {
            CurrentTaskGroup.TaskGroupOrders.Clear();
            ClearActionList();
            m_oTaskGroupPanel.PlottedMovesListBox.Items.Clear();
            CalculateTimeDistance();
        }

        /// <summary>
        /// Shows the TG Panel
        /// </summary>
        /// <param name="a_oDockPanel"> The target Docking Panel. </param>
        public void ShowAllPanels(DockPanel a_oDockPanel)
        {
            ShowViewPortPanel(a_oDockPanel);

            RefreshTGPanel();
        }

        /// <summary>
        /// Shows the View Port Panel.
        /// </summary>
        /// <param name="a_oDockPanel"> The target Docking Panel. </param>
        public void ShowViewPortPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oTaskGroupPanel.Show(a_oDockPanel, DockState.Document);
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }


        #region PrivateMethods

        /// <summary>
        /// Creates the ship information section. There is also a tactical data condition where this info is swapped out with other info, that is not yet handled.
        /// </summary>
        private void SetupShipDataGrid()
        {
            try
            {
                /// <summary>
                /// Add columns:
                /// </summary>
                Padding newPadding = new Padding(2, 0, 2, 0);
                AddColumn("Ship Name", newPadding);
                AddColumn("Class", newPadding);
                AddColumn("Fuel", newPadding);
                AddColumn("Ammo", newPadding);
                AddColumn("Shields", newPadding);
                AddColumn("Thermal Sig.", newPadding);
                AddColumn("Maint Supplies", newPadding);
                AddColumn("Maint Clock", newPadding);
                AddColumn("Crew (mths)", newPadding);
                AddColumn("Morale", newPadding);
                AddColumn("Grade Bonus", newPadding);
                AddColumn("TF Training", newPadding);
            }
            catch
            {
                logger.Error("Something went wrong Creating Columns for Taskgroup summary screen...");
            }
        }

        private void RefreshShipCells()
        {
            m_oTaskGroupPanel.TaskGroupDataGrid.Rows.Clear();
            if (m_oCurrnetTaskGroup != null && m_oTaskGroupPanel.TaskGroupDataGrid.Columns.Count != 0)
            {
                try
                {
                    /// <summary>
                    /// Add Rows:
                    /// </summary>
                    for (int loop = 0; loop < m_oCurrnetTaskGroup.Ships.Count; loop++)
                    {
                        using (DataGridViewRow row = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            row.Height = 18;
                            m_oTaskGroupPanel.TaskGroupDataGrid.Rows.Add(row);

                            PopulateRow(loop);
                        }
                    }
                }
                catch
                {
                    logger.Error("Something went wrong Creating Rows for Taskgroup summary screen...");
                }
            }
        }

        /// <summary>
        /// Just a space saver here to avoid copy pasting a lot.
        /// </summary>
        /// <param name="Header">Text of column header.</param>
        /// <param name="newPadding">Padding in use, not sure what this is or why its necessary. Cargo culting it is.</param>
        private void AddColumn(String Header, Padding newPadding)
        {
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.HeaderText = Header;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                col.DefaultCellStyle.Padding = newPadding;
                if (col != null)
                {
                    m_oTaskGroupPanel.TaskGroupDataGrid.Columns.Add(col);
                }
            }
        }

        /// <summary>
        /// Fills in the rows of the display.
        /// </summary>
        /// <param name="row">ship index and also row.</param>
        private void PopulateRow(int row)
        {
            try
            {
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Name].Value = m_oCurrnetTaskGroup.Ships[row].Name;
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.ShipClass].Value = m_oCurrnetTaskGroup.Ships[row].ShipClass.Name;

                float fuelPercent = (m_oCurrnetTaskGroup.Ships[row].CurrentFuel / m_oCurrnetTaskGroup.Ships[row].ShipClass.TotalFuelCapacity) * 100.0f;
                fuelPercent = (float)Math.Floor(fuelPercent);
            
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Fuel].Value = fuelPercent.ToString() + "%";

                String Ammo;
                if (m_oCurrnetTaskGroup.Ships[row].ShipClass.TotalMagazineCapacity == 0)
                {
                    Ammo = "N/A";
                }
                else if (m_oCurrnetTaskGroup.Ships[row].ShipClass.ShipMagazineDef.Count != 0)
                {
                    float AmmoPercent = (m_oCurrnetTaskGroup.Ships[row].CurrentMagazineCapacity / m_oCurrnetTaskGroup.Ships[row].ShipClass.TotalMagazineCapacity) * 100.0f;
                    Ammo = AmmoPercent.ToString();
                }
                else
                {
                    int missileCount = 0;
                    foreach (KeyValuePair<Pulsar4X.Entities.Components.OrdnanceDefTN, int> pair in m_oCurrnetTaskGroup.Ships[row].ShipOrdnance)
                    {
                        missileCount = missileCount + pair.Value;
                    }
                    float AmmoPercent = missileCount / m_oCurrnetTaskGroup.Ships[row].ShipClass.LauncherCount;
                    Ammo = AmmoPercent.ToString();
                }
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Ammo].Value = Ammo;

                String Shield;
                if (m_oCurrnetTaskGroup.Ships[row].ShipClass.TotalShieldPool == 0.0f)
                {
                    Shield = "N/A";
                }
                else
                {
                    Shield = m_oCurrnetTaskGroup.Ships[row].CurrentShieldPool.ToString() + "/" + m_oCurrnetTaskGroup.Ships[row].ShipClass.TotalShieldPool.ToString();
                }
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Shields].Value = Shield;

                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Thermal].Value = m_oCurrnetTaskGroup.Ships[row].CurrentThermalSignature.ToString();
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.MaintSup].Value = m_oCurrnetTaskGroup.Ships[row].CurrentMSP.ToString() + "/" + m_oCurrnetTaskGroup.Ships[row].CurrentMSPCapacity.ToString();
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.MaintClock].Value = m_oCurrnetTaskGroup.Ships[row].MaintenanceClock.ToString();
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Crew].Value = m_oCurrnetTaskGroup.Ships[row].CurrentDeployment.ToString();
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Morale].Value = m_oCurrnetTaskGroup.Ships[row].Morale.ToString();
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Grade].Value = m_oCurrnetTaskGroup.Ships[row].ShipGrade.ToString();
                m_oTaskGroupPanel.TaskGroupDataGrid.Rows[row].Cells[(int)StratCells.Training].Value = m_oCurrnetTaskGroup.Ships[row].TFTraining.ToString();
            }
            catch
            {
                logger.Error("Something went wrong Refreshing Cells for Taskgroup Ship summary screen...");
            }
        }

        /// <summary>
        /// Build the Total System Location List here.
        /// </summary>
        private void BuildSystemLocationList()
        {
            m_oTaskGroupPanel.SystemLocationsListBox.Items.Clear();
            SystemLocationListIndices.Clear();
            SystemLocationListTypes.Clear();

            AddPlanetsToList();

            if (m_oTaskGroupPanel.DisplayContactsCheckBox.Checked == true)
                AddContactsToList();

            if (m_oTaskGroupPanel.DisplayTaskGroupsCheckBox.Checked == true)
                AddTaskGroupsToList();

            if (m_oTaskGroupPanel.DisplayWaypointsCheckBox.Checked == true)
                AddWaypointsToList();

            SystemLocationListIndices.Add(m_oTaskGroupPanel.SystemLocationsListBox.Items.Count);
            SystemLocationListTypes.Add(SystemLocationListType.Count);
        }

        /// <summary>
        /// Builds available orders here. Right now, moveTo is the only one worthwhile. also want to replace this with a proper string at some point.
        /// </summary>
        private void BuildActionList()
        {
            ClearActionList();
            m_oTaskGroupPanel.AvailableActionsListBox.Items.Add(Constants.ShipTN.OrderType.MoveTo);
        }

        /// <summary>
        /// Clears the action list.
        /// </summary>
        private void ClearActionList()
        {
            m_oTaskGroupPanel.AvailableActionsListBox.Items.Clear();
            m_oTaskGroupPanel.PlottedMovesListBox.ClearSelected();
        }

        /// <summary>
        /// Build the list of TG orders here.
        /// </summary>
        private void BuildPlottedMoveList()
        {
            m_oTaskGroupPanel.PlottedMovesListBox.Items.Clear();

            for (int loop = 0; loop < CurrentTaskGroup.TaskGroupOrders.Count; loop++)
            {
                m_oTaskGroupPanel.PlottedMovesListBox.Items.Add(CurrentTaskGroup.TaskGroupOrders[loop].Name);
            }
        }

        /// <summary>
        /// Add every planet in the system that this TG is in to the list.
        /// Eventually jump orders will modify this. to be the system at the end of the order stack.
        /// </summary>
        private void AddPlanetsToList()
        {
            SystemLocationListIndices.Add(m_oTaskGroupPanel.SystemLocationsListBox.Items.Count);
            SystemLocationListTypes.Add(SystemLocationListType.Planets);


            for (int loop = 0; loop < CurrentTaskGroup.Contact.CurrentSystem.Stars.Count; loop++)
            {
                for (int loop2 = 0; loop2 < CurrentTaskGroup.Contact.CurrentSystem.Stars[loop].Planets.Count; loop2++)
                {
                    m_oTaskGroupPanel.SystemLocationsListBox.Items.Add(CurrentTaskGroup.Contact.CurrentSystem.Stars[loop].Planets[loop2]);
                }
            }
        }

        /// <summary>
        /// Adds any detected ships to the system location box.
        /// </summary>
        private void AddContactsToList()
        {
            SystemLocationListIndices.Add(m_oTaskGroupPanel.SystemLocationsListBox.Items.Count);
            SystemLocationListTypes.Add(SystemLocationListType.Contacts);

            if (CurrentFaction.DetectedContactLists.ContainsKey(CurrentTaskGroup.Contact.CurrentSystem) == true)
            {
                foreach (KeyValuePair<ShipTN, FactionContact> pair in CurrentFaction.DetectedContactLists[CurrentTaskGroup.Contact.CurrentSystem].DetectedContacts)
                {
                        String TH = "";
                        if (pair.Value.thermal == true)
                        {
                            TH = String.Format("[Thermal {0}]", pair.Key.CurrentThermalSignature);
                        }

                        String EM = "";
                        if (pair.Value.EM == true)
                        {
                            EM = String.Format("[EM {0}]", pair.Key.CurrentEMSignature);
                        }

                        String ACT = "";
                        if (pair.Value.active == true)
                        {
                            ACT = String.Format("[ACT {0}]", pair.Key.TotalCrossSection);
                        }

                        String Entry = String.Format("{0} {1}{2}{3}", pair.Key.Name, TH, EM, ACT);

                        m_oTaskGroupPanel.SystemLocationsListBox.Items.Add(Entry);
                }
            }
        }

        /// <summary>
        /// Adds friendly taskgroups to the system location box
        /// </summary>
        private void AddTaskGroupsToList()
        {
            SystemLocationListIndices.Add(m_oTaskGroupPanel.SystemLocationsListBox.Items.Count);
            SystemLocationListTypes.Add(SystemLocationListType.TaskGroups);


            for (int loop = 0; loop < CurrentTaskGroup.Contact.CurrentSystem.SystemContactList.Count; loop++)
            {
                if (CurrentTaskGroup.Contact.CurrentSystem.SystemContactList[loop].SSEntity == StarSystemEntityType.TaskGroup)
                {
                    if (CurrentTaskGroup != CurrentTaskGroup.Contact.CurrentSystem.SystemContactList[loop].TaskGroup &&
                        CurrentTaskGroup.Contact.CurrentSystem.SystemContactList[loop].TaskGroup.TaskGroupFaction == CurrentFaction)
                    {
                        m_oTaskGroupPanel.SystemLocationsListBox.Items.Add(CurrentTaskGroup.Contact.CurrentSystem.SystemContactList[loop].TaskGroup);
                    }
                }
            }
        }

        /// <summary>
        /// Adds user generated waypoints to the location list.
        /// </summary>
        private void AddWaypointsToList()
        {
            SystemLocationListIndices.Add(m_oTaskGroupPanel.SystemLocationsListBox.Items.Count);
            SystemLocationListTypes.Add(SystemLocationListType.Waypoints);

            for (int loop = 0; loop < CurrentTaskGroup.Contact.CurrentSystem.Waypoints.Count; loop++)
            {
                if( CurrentTaskGroup.Contact.CurrentSystem.Waypoints[loop].FactionId == CurrentTaskGroup.TaskGroupFaction.FactionID)
                    m_oTaskGroupPanel.SystemLocationsListBox.Items.Add(CurrentTaskGroup.Contact.CurrentSystem.Waypoints[loop]);
            }
        }

        /// <summary>
        /// Time and distance or orders should be calculated here based on the radio button selection choices.
        /// </summary>
        private void CalculateTimeDistance()
        {
            m_oTaskGroupPanel.TimeDistanceTextBox.Clear();

            if (CurrentTaskGroup.TaskGroupOrders.Count != 0)
            {

                String DistanceString = "N/A";
                String TimeString = "N/A";
                double dX = 0.0;
                double dY = 0.0;
                double dZ = 0.0;

                if (m_oTaskGroupPanel.CurrentTDRadioButton.Checked == true)
                {
                    if (CurrentTaskGroup.TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Body)
                    {
                        dX = CurrentTaskGroup.Contact.XSystem - (CurrentTaskGroup.TaskGroupOrders[0].target.XSystem + CurrentTaskGroup.TaskGroupOrders[0].body.Primary.XSystem);
                        dY = CurrentTaskGroup.Contact.YSystem - (CurrentTaskGroup.TaskGroupOrders[0].target.YSystem + CurrentTaskGroup.TaskGroupOrders[0].body.Primary.YSystem);
                    }
                    else if (CurrentTaskGroup.TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Population)
                    {
                        dX = CurrentTaskGroup.Contact.XSystem - (CurrentTaskGroup.TaskGroupOrders[0].target.XSystem + CurrentTaskGroup.TaskGroupOrders[0].pop.Planet.Primary.XSystem);
                        dY = CurrentTaskGroup.Contact.YSystem - (CurrentTaskGroup.TaskGroupOrders[0].target.YSystem + CurrentTaskGroup.TaskGroupOrders[0].pop.Planet.Primary.YSystem);
                    }
                    else
                    {
                        dX = CurrentTaskGroup.Contact.XSystem - CurrentTaskGroup.TaskGroupOrders[0].target.XSystem;
                        dY = CurrentTaskGroup.Contact.YSystem - CurrentTaskGroup.TaskGroupOrders[0].target.YSystem;
                    }

                    dZ = Math.Sqrt((dX * dX) + (dY * dY));

                }
                else if (m_oTaskGroupPanel.AllOrdersTDRadioButton.Checked == true)
                {
                    if (CurrentTaskGroup.IsOrbiting == true)
                        CurrentTaskGroup.GetPositionFromOrbit();

                    double tX = CurrentTaskGroup.Contact.XSystem;
                    double tY = CurrentTaskGroup.Contact.YSystem;

                    for (int loop = 0; loop < CurrentTaskGroup.TaskGroupOrders.Count; loop++)
                    {
                        if (CurrentTaskGroup.TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Body)
                        {
                            dX = CurrentTaskGroup.Contact.XSystem - (CurrentTaskGroup.TaskGroupOrders[loop].target.XSystem + CurrentTaskGroup.TaskGroupOrders[loop].body.Primary.XSystem);
                            dY = CurrentTaskGroup.Contact.YSystem - (CurrentTaskGroup.TaskGroupOrders[loop].target.YSystem + CurrentTaskGroup.TaskGroupOrders[loop].body.Primary.YSystem);
                        }
                        else if (CurrentTaskGroup.TaskGroupOrders[loop].target.SSEntity == StarSystemEntityType.Population)
                        {
                            dX = tX - (CurrentTaskGroup.TaskGroupOrders[loop].target.XSystem + CurrentTaskGroup.TaskGroupOrders[loop].pop.Planet.Primary.XSystem);
                            dY = tY - (CurrentTaskGroup.TaskGroupOrders[loop].target.YSystem + CurrentTaskGroup.TaskGroupOrders[loop].pop.Planet.Primary.YSystem);
                        }
                        else
                        {
                            dX = tX - CurrentTaskGroup.TaskGroupOrders[loop].target.XSystem;
                            dY = tY - CurrentTaskGroup.TaskGroupOrders[loop].target.YSystem;
                        }

                        dZ = dZ + Math.Sqrt((dX * dX) + (dY * dY));

                        if (CurrentTaskGroup.TaskGroupOrders[loop].typeOf != Constants.ShipTN.OrderType.StandardTransit &&
                            CurrentTaskGroup.TaskGroupOrders[loop].typeOf != Constants.ShipTN.OrderType.SquadronTransit &&
                            CurrentTaskGroup.TaskGroupOrders[loop].typeOf != Constants.ShipTN.OrderType.TransitAndDivide)
                        {
                            if (CurrentTaskGroup.TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Body)
                            {
                                tX = (CurrentTaskGroup.TaskGroupOrders[loop].target.XSystem + CurrentTaskGroup.TaskGroupOrders[loop].body.Primary.XSystem);
                                tY = (CurrentTaskGroup.TaskGroupOrders[loop].target.YSystem + CurrentTaskGroup.TaskGroupOrders[loop].body.Primary.YSystem);
                            }
                            else if (CurrentTaskGroup.TaskGroupOrders[loop].target.SSEntity == StarSystemEntityType.Population)
                            {
                                tX =(CurrentTaskGroup.TaskGroupOrders[loop].target.XSystem + CurrentTaskGroup.TaskGroupOrders[loop].pop.Planet.Primary.XSystem);
                                tY =(CurrentTaskGroup.TaskGroupOrders[loop].target.YSystem + CurrentTaskGroup.TaskGroupOrders[loop].pop.Planet.Primary.YSystem);
                            }
                            else
                            {
                                tX = CurrentTaskGroup.TaskGroupOrders[loop].target.XSystem;
                                tY = CurrentTaskGroup.TaskGroupOrders[loop].target.YSystem;
                            }
                        }
                        else
                        {
                            /// <summary>
                            /// As the TG will be in a new system, set the jump point far end locations here.
                            /// </summary>

                            try
                            {
                                tX = CurrentTaskGroup.TaskGroupOrders[loop].jumpPoint.Connect.XSystem;
                                tY = CurrentTaskGroup.TaskGroupOrders[loop].jumpPoint.Connect.YSystem;
                            }
                            catch
                            {
                                logger.Error("No Jumppoint associated with jump point transit order in CalcTimeDistance in taskgroup.cs under handlers.");
                            }
                        }
                    }
                }

                if (dZ >= Constants.Units.MAX_KM_IN_AU)
                {
                    double Count = dZ / Constants.Units.MAX_KM_IN_AU;

                    double newDistance = Math.Floor(2.147483648 * Count * 100.0);
                    newDistance = newDistance / 100.0;

                    DistanceString = "Distance: " + newDistance.ToString() + "B km";

                    double maxTime = 2.147483648;

                    double timeReq = newDistance / (double)CurrentTaskGroup.CurrentSpeed;

                    TimeString = "ETA: N/A";

                    if (timeReq < maxTime)
                    {
                        double TimeSeconds = Math.Floor(timeReq * 1000000000.0);
                        double TimeMinutes = Math.Floor(TimeSeconds / 60.0);
                        TimeSeconds = TimeSeconds - (TimeMinutes * 60.0);

                        double TimeHours = Math.Floor(TimeMinutes / 60.0);
                        TimeMinutes = TimeMinutes - (TimeHours * 60.0);

                        TimeString = "ETA: " + TimeHours.ToString() + ":" + TimeMinutes.ToString() + ":" + TimeSeconds.ToString();
                    }

                }
                else
                {
                    /// <summary>
                    /// This is the easy case, no worries about overflows here. I hope.
                    /// </summary>
                        
                    double Distance = Math.Floor(dZ * Constants.Units.KM_PER_AU);

                    double TimeSeconds = Math.Floor(Distance / CurrentTaskGroup.CurrentSpeed);

                    double TimeMinutes = Math.Floor(TimeSeconds / 60.0);
                    TimeSeconds = TimeSeconds - (TimeMinutes * 60.0);


                    double TimeHours = Math.Floor(TimeMinutes / 60.0);
                    TimeMinutes = TimeMinutes - (TimeHours * 60.0);

                    DistanceString = "Distance: ";
                    TimeString = "ETA: " + TimeHours.ToString() + ":" + TimeMinutes.ToString() + ":" + TimeSeconds.ToString();

                    if(Distance > 1000000000.0)
                    {
                        Distance = Math.Floor(Distance / 10000000.0);
                        Distance = Distance / 100.0;

                        DistanceString = "Distance: " + Distance.ToString() + "B km";
                    }
                    else if(Distance > 1000000.0)
                    {
                        Distance = Math.Floor(Distance / 10000.0);
                        Distance = Distance / 100.0;

                        DistanceString = "Distance: " + Distance.ToString() + "M km";
                    }
                    else if (Distance > 1000.0)
                    {
                        Distance = Math.Floor(Distance / 10.0);
                        Distance = Distance / 100.0;

                        DistanceString = "Distance: " + Distance.ToString() + "K km";
                    }
                    else
                    {
                        Distance = Math.Floor(Distance);

                        DistanceString = "Distance: " + Distance.ToString() + "km";
                    }
                }
                m_oTaskGroupPanel.TimeDistanceTextBox.Text = DistanceString + " " + TimeString;
            }
            else
            {
                m_oTaskGroupPanel.TimeDistanceTextBox.Text = "No Orders";
            }
        }

        /// <summary>
        /// Refresh the TG page.
        /// </summary>
        private void RefreshTGPanel()
        {
            if (CurrentTaskGroup != null)
            {
                m_oTaskGroupPanel.TaskGroupLocationTextBox.Text = CurrentTaskGroup.Contact.CurrentSystem.Name;
                m_oTaskGroupPanel.SetSpeedTextBox.Text = CurrentTaskGroup.CurrentSpeed.ToString();
                m_oTaskGroupPanel.MaxSpeedTextBox.Text = CurrentTaskGroup.MaxSpeed.ToString();

                RefreshShipCells();

                BuildSystemLocationList();
                ClearActionList();

                BuildPlottedMoveList();

                CalculateTimeDistance();
            }
        }

        #endregion
    }
}
