using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using WeifenLuo.WinFormsUI.Docking;
using Pulsar4X.UI.ViewModels;
using Pulsar4X.Entities;

#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif

using Pulsar4X.UI.GLUtilities;
using Pulsar4X.UI.SceenGraph;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Pulsar4X.UI.Handlers
{
    public class SystemListObject
    {
        /// <summary>
        /// type of entity for filtering. possibly future could use the StarSystemEntityType instead?
        /// </summary>
        public enum ListEntityType
        {
            Planets,
            Colonies,
            Contacts,
            TaskGroups,
            Waypoints,
            JumpPoint,
            SurveyPoints,
            Count,
        }

        public StarSystemEntity Entity { get; private set; }
        public ListEntityType EntityType { get; private set; }

        public SystemListObject(ListEntityType entityType, StarSystemEntity entity)
        {
            Entity = entity;
            EntityType = entityType;
        }
    }

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


        /// <summary>
        /// TG Logger:
        /// </summary>
#if LOG4NET_ENABLED
        public static readonly ILog logger = LogManager.GetLogger(typeof(TaskGroup));
#endif

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
        /// Dictionary of interesting locations for the SystemLocationList
        /// </summary>
        private Dictionary<Guid, SystemListObject> SystemLocationDict { get; set; }

        /// <summary>
        /// strings aren't unique unfortunately so I need to use Guid, but Guid does not translate into the display very well.
        /// </summary>
        private Dictionary<Guid, string> SystemLocationGuidDict { get; set; }

        /// <summary>
        /// PlottedMovesListbox orders vars. 
        /// </summary>
        private int SelectedOrderIndex = -1;
        private int PrevioslySelectedOrderIndex = -1;

        /// <summary>
        /// What taskgroups are at the location of CurrentTaskGroup?
        /// </summary>
        private BindingList<TaskGroupTN> OrgSelectedTGList { get; set; }


        /// <summary>
        /// Constructor for this handler.
        /// </summary>
        public TaskGroup()
        {
            m_oTaskGroupPanel = new Panels.TaskGroup_Panel();
            m_oRenameTaskGroupPanel = new Panels.ClassDes_RenameClass();

            /// <summary>
            /// create the location dictionary
            /// </summary>
            SystemLocationDict = new Dictionary<Guid, SystemListObject>();
            SystemLocationGuidDict = new Dictionary<Guid, string>();

            /// <summary>
            /// setup viewmodel:
            /// Bind TG Selection Combo Box.
            /// Bind faction Selection as well.
            /// </summary>
            VM = new ViewModels.TaskGroupViewModel();

            OrgSelectedTGList = new BindingList<TaskGroupTN>();

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
            m_oTaskGroupPanel.DisplayContactsCheckBox.CheckStateChanged += new EventHandler(DisplayCheckBox_CheckChanged);
            m_oTaskGroupPanel.DisplayTaskGroupsCheckBox.CheckStateChanged += new EventHandler(DisplayCheckBox_CheckChanged);
            m_oTaskGroupPanel.DisplayWaypointsCheckBox.CheckStateChanged += new EventHandler(DisplayCheckBox_CheckChanged);
            m_oTaskGroupPanel.OrderFilteringCheckBox.CheckStateChanged += new EventHandler(OrderFilteringCheckBox_CheckChanged);
            m_oTaskGroupPanel.DisplaySurveyLocationsCheckBox.CheckStateChanged += new EventHandler(DisplayCheckBox_CheckChanged);
            m_oTaskGroupPanel.ExcludeSurveyedCheckBox.CheckStateChanged += new EventHandler(DisplayCheckBox_CheckChanged);

            m_oTaskGroupPanel.NewTaskGroupButton.Click += new EventHandler(NewTaskGroupButton_Click);
            m_oTaskGroupPanel.RenameTaskGroupButton.Click += new EventHandler(RenameTaskGroupButton_Click);
            m_oTaskGroupPanel.SetSpeedButton.Click += new EventHandler(SetSpeedButton_Clicked);
            m_oTaskGroupPanel.MaxSpeedButton.Click += new EventHandler(MaxSpeedButton_Clicked);
            m_oTaskGroupPanel.AddMoveButton.Click += new EventHandler(AddMoveButton_Clicked);
            m_oTaskGroupPanel.RemoveButton.Click += new EventHandler(RemoveButton_Clicked);
            m_oTaskGroupPanel.RemoveAllButton.Click += new EventHandler(RemoveAllButton_Clicked);

            m_oTaskGroupPanel.CurrentTDRadioButton.CheckedChanged += new EventHandler(CurrentTDRadioButton_CheckChanged);
            m_oTaskGroupPanel.AllOrdersTDRadioButton.CheckedChanged += new EventHandler(AllOrdersTDRadioButton_CheckChanged);

            m_oTaskGroupPanel.AvailableActionsListBox.MouseDoubleClick += new MouseEventHandler(AddMoveButton_Clicked);
            m_oTaskGroupPanel.SystemLocationsListBox.MouseDoubleClick += new MouseEventHandler(AddMoveButton_Clicked);
            m_oTaskGroupPanel.PlottedMovesListBox.MouseDoubleClick += new MouseEventHandler(RemoveButton_Clicked);

            //m_oTaskGroupPanel.PlottedMovesListBox.SelectedIndexChanged += new EventHandler(PlottedMovesListBox_SelectedIndexChanged);
            m_oTaskGroupPanel.PlottedMovesListBox.MouseDown += new MouseEventHandler(PlottedMovesListBox_MouseDown);
            SelectedOrderIndex = m_oTaskGroupPanel.PlottedMovesListBox.SelectedIndex;

            #region Organization Tab
            m_oTaskGroupPanel.OrgSelectedTGComboBox.SelectedIndexChanged += new EventHandler(OrgSelectedTGComboBox_SelectedIndexChanged);
            m_oTaskGroupPanel.OrgMoveLeftButton.Click += new EventHandler(OrgMoveLeftButton_Click);
            m_oTaskGroupPanel.OrgMoveRightButton.Click += new EventHandler(OrgMoveRightButton_Click);
            #endregion

            #region Special Orders Tab
            SetupDefaultAndConditionalOrders();
            #endregion

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
        /// If any checkbox gets checked or unchecked clear the action list and build the system location list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayCheckBox_CheckChanged(object sender, EventArgs e)
        {
            BuildSystemLocationList();
            ClearActionList();
        }

        private void OrderFilteringCheckBox_CheckChanged(object sender, EventArgs e)
        {
            BuildActionList();
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
            String Title = String.Format("New Taskgroup #{0}", m_oCurrnetFaction.TaskGroups.Count);
            m_oCurrnetFaction.AddNewTaskGroup(Title, m_oCurrnetFaction.Capitol.Planet, m_oCurrnetFaction.Capitol.Planet.Position.System);

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

            if (CheckString == true)
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

            Order NewOrder = null;

            /// <summary>
            /// If AddMove is clicked with no system location it will bomb.
            /// </summary>
            if (PlaceIndex != -1)
            {
                List<Guid> GID = SystemLocationGuidDict.Keys.ToList();
                SystemListObject selected = SystemLocationDict[GID[PlaceIndex]];


                int ActionIndex = m_oTaskGroupPanel.AvailableActionsListBox.SelectedIndex;

                /// <summary>
                /// if AddMove is clicked with no selection action it will bomb.
                /// </summary>
                if (ActionIndex != -1)
                {
                    Constants.ShipTN.OrderType selected_ordertype = (Constants.ShipTN.OrderType)m_oTaskGroupPanel.AvailableActionsListBox.SelectedItem;

                    /// <summary>
                    /// Now figure out what the hell order this would be.
                    /// </summary>
                    var entity = selected.Entity;
                    var etype = selected.EntityType;
#warning handle secondary,tertiary, and order delays. also handle taskgroup split condition for move to contact orders if not already done so.
                    switch (etype)
                    {

                        case SystemListObject.ListEntityType.Contacts:
                            ShipTN shipcontact = (ShipTN)entity;//CurrentFaction.DetectedContactLists
                            NewOrder = new Order(selected_ordertype, -1, -1, 0, shipcontact.ShipsTaskGroup); //the task group? what if the TG splits?
                            shipcontact.TaskGroupsOrdered.Add(CurrentTaskGroup);
                            break;
                        case SystemListObject.ListEntityType.Planets:
                            SystemBody planet = (SystemBody)entity;
                            NewOrder = new Order(selected_ordertype, -1, -1, 0, planet);
                            break;
                        case SystemListObject.ListEntityType.JumpPoint:
                            JumpPoint jp = (JumpPoint)entity;
                            NewOrder = new Order(selected_ordertype, -1, -1, 0, jp);
                            break;
                        case SystemListObject.ListEntityType.Colonies:
                            Population popTargetOfOrder = (Population)entity;
                            NewOrder = new Order(selected_ordertype, -1, -1, 0, popTargetOfOrder);
                            break;
                        case SystemListObject.ListEntityType.TaskGroups:
                            TaskGroupTN TargetOfOrder = (TaskGroupTN)entity;
                            NewOrder = new Order(selected_ordertype, -1, -1, 0, TargetOfOrder);
                            TargetOfOrder.TaskGroupsOrdered.Add(CurrentTaskGroup);
                            break;
                        case SystemListObject.ListEntityType.Waypoints:
                            Waypoint waypoint = (Waypoint)entity;
                            NewOrder = new Order(selected_ordertype, -1, -1, 0, waypoint);
                            break;
                        case SystemListObject.ListEntityType.SurveyPoints:
                            SurveyPoint SPoint = (SurveyPoint)entity;
                            NewOrder = new Order(selected_ordertype, -1, -1, 0, SPoint);
                            break;
                    }
                    if (NewOrder != null)
                        CurrentTaskGroup.IssueOrder(NewOrder, SelectedOrderIndex);
                }
            }
            if (SelectedOrderIndex != -1 && SelectedOrderIndex < CurrentTaskGroup.TaskGroupOrders.Count)
                SelectedOrderIndex += 1;
            BuildPlottedMoveList();
            BuildSystemLocationList();
            CalculateTimeDistance();
        }

        /// <summary>
        /// Removes the selected order.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveButton_Clicked(object sender, EventArgs e)
        {
            int removeindex = SelectedOrderIndex;

            if (CurrentTaskGroup.TaskGroupOrders.Count != 0)
            {
                if (!(sender is Button) && PrevioslySelectedOrderIndex < CurrentTaskGroup.TaskGroupOrders.Count)
                {
                    MouseEventArgs mea = (MouseEventArgs)e;
                    var rect = m_oTaskGroupPanel.PlottedMovesListBox.GetItemRectangle(PrevioslySelectedOrderIndex);
                    if (rect.Contains(mea.Location))
                    {
                        removeindex = PrevioslySelectedOrderIndex;
                        SelectedOrderIndex = PrevioslySelectedOrderIndex;
                        //m_oTaskGroupPanel.PlottedMovesListBox.SelectedItem = PrevioslySelectedOrderIndex + 1;
                    }

                }

                if (removeindex == -1)
                    CurrentTaskGroup.RemoveOrder(CurrentTaskGroup.TaskGroupOrders.Last());
                //CurrentTaskGroup.TaskGroupOrders.Remove(CurrentTaskGroup.TaskGroupOrders.Last());
                else
                {
                    CurrentTaskGroup.RemoveOrder(removeindex);
                    //CurrentTaskGroup.TaskGroupOrders.RemoveAt(removeindex);
                    //int prevIndex = SelectedOrderIndex;
                    //SelectedOrderIndex = -1;
                    //m_oTaskGroupPanel.PlottedMovesListBox.SelectedIndex = prevIndex;
                }
                ClearActionList();
                BuildPlottedMoveList();
                CalculateTimeDistance();
                BuildActionList();
            }
        }

        /// <summary>
        /// removes all orders
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveAllButton_Clicked(object sender, EventArgs e)
        {
            foreach(Order TGO in CurrentTaskGroup.TaskGroupOrders)
            {
                CurrentTaskGroup.RemoveOrder(TGO);
            }
            //CurrentTaskGroup.TaskGroupOrders.Clear();
            ClearActionList();
            m_oTaskGroupPanel.PlottedMovesListBox.Items.Clear();
            CalculateTimeDistance();
            BuildActionList();
        }

        /// <summary>
        /// OrgSelectedTGListbox needs to be updated based on this.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OrgSelectedTGComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            BuildSelectedTGListBox();
        }

        /// <summary>
        /// Move the ship from the selected TG to the current one.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OrgMoveLeftButton_Click(object sender, EventArgs e)
        {
            if (CurrentTaskGroup != null  && m_oTaskGroupPanel.OrgSelectedTGListBox.SelectedIndex != -1 &&
                    m_oTaskGroupPanel.OrgSelectedTGComboBox.SelectedIndex != -1)
            {
                TaskGroupTN TaskGroupFrom = OrgSelectedTGList[m_oTaskGroupPanel.OrgSelectedTGComboBox.SelectedIndex];

                if (TaskGroupFrom.Ships.Count == 0)
                    return;

                ShipTN ShipToMove = TaskGroupFrom.Ships[m_oTaskGroupPanel.OrgSelectedTGListBox.SelectedIndex];

                TaskGroupFrom.TransferShipToTaskGroup(ShipToMove, CurrentTaskGroup);


                /// <summary>
                /// These are the following UI elements that will need to be updated in the event that a ship moves from one TG to another.
                /// </summary>
                m_oTaskGroupPanel.SetSpeedTextBox.Text = CurrentTaskGroup.CurrentSpeed.ToString();
                m_oTaskGroupPanel.MaxSpeedTextBox.Text = CurrentTaskGroup.MaxSpeed.ToString();

                RefreshShipCells();
                CalculateTimeDistance();
                BuildOrganizationTab();
            }        
        }

        /// <summary>
        /// Move the ship from the current TG to the selected one. the opposite of the above function.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OrgMoveRightButton_Click(object sender, EventArgs e)
        {
            if (CurrentTaskGroup != null && m_oTaskGroupPanel.OrgCurrentTGListBox.SelectedIndex != -1 && m_oTaskGroupPanel.OrgSelectedTGComboBox.SelectedIndex != -1 && 
                CurrentTaskGroup.Ships.Count != 0)
            {

                ShipTN ShipToMove = CurrentTaskGroup.Ships[m_oTaskGroupPanel.OrgCurrentTGListBox.SelectedIndex];
                TaskGroupTN TaskGroupTo = OrgSelectedTGList[m_oTaskGroupPanel.OrgSelectedTGComboBox.SelectedIndex];

                CurrentTaskGroup.TransferShipToTaskGroup(ShipToMove, TaskGroupTo);

                /// <summary>
                /// These are the following UI elements that will need to be updated in the event that a ship moves from one TG to another.
                /// </summary>
                m_oTaskGroupPanel.SetSpeedTextBox.Text = CurrentTaskGroup.CurrentSpeed.ToString();
                m_oTaskGroupPanel.MaxSpeedTextBox.Text = CurrentTaskGroup.MaxSpeed.ToString();

                RefreshShipCells();
                CalculateTimeDistance();
                BuildOrganizationTab();
            }
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
#if LOG4NET_ENABLED
                logger.Error("Something went wrong Creating Columns for Taskgroup summary screen...");
#endif
            }
        }

        /// <summary>
        /// Fill the combo boxes for default orders, conditional conditions, and conditional orders.
        /// Typecasting all these to ints produces shorter lines than going by the enum type.
        /// </summary>
        private void SetupDefaultAndConditionalOrders()
        {
            for (int defaultOrderIterator = 0; defaultOrderIterator < (int)Constants.ShipTN.DefaultOrders.TypeCount; defaultOrderIterator++)
            {
                m_oTaskGroupPanel.PrimaryDefaultOrderComboBox.Items.Add((Constants.ShipTN.DefaultOrders)defaultOrderIterator);
                m_oTaskGroupPanel.SecondaryDefaultOrderComboBox.Items.Add((Constants.ShipTN.DefaultOrders)defaultOrderIterator);
            }
            m_oTaskGroupPanel.PrimaryDefaultOrderComboBox.SelectedIndex = 0;
            m_oTaskGroupPanel.SecondaryDefaultOrderComboBox.SelectedIndex = 0;

            for (int condIterator = 0; condIterator < (int)Constants.ShipTN.Condition.TypeCount; condIterator++)
            {
                m_oTaskGroupPanel.ConditionalAConditionComboBox.Items.Add((Constants.ShipTN.Condition)condIterator);
                m_oTaskGroupPanel.ConditionalBConditionComboBox.Items.Add((Constants.ShipTN.Condition)condIterator);
            }
            m_oTaskGroupPanel.ConditionalAConditionComboBox.SelectedIndex = 0;
            m_oTaskGroupPanel.ConditionalBConditionComboBox.SelectedIndex = 0;

            for (int condOrderIterator = 0; condOrderIterator < (int)Constants.ShipTN.ConditionalOrders.TypeCount; condOrderIterator++)
            {
                m_oTaskGroupPanel.ConditionalAOrderComboBox.Items.Add((Constants.ShipTN.ConditionalOrders)condOrderIterator);
                m_oTaskGroupPanel.ConditionalBOrderComboBox.Items.Add((Constants.ShipTN.ConditionalOrders)condOrderIterator);
            }
            m_oTaskGroupPanel.ConditionalAOrderComboBox.SelectedIndex = 0;
            m_oTaskGroupPanel.ConditionalBOrderComboBox.SelectedIndex = 0;
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
#if LOG4NET_ENABLED
                    logger.Error("Something went wrong Creating Rows for Taskgroup summary screen...");
#endif
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
                    float AmmoPercent = (float)Math.Round(((float)m_oCurrnetTaskGroup.Ships[row].CurrentMagazineCapacity / (float)m_oCurrnetTaskGroup.Ships[row].ShipClass.TotalMagazineCapacity) * 100.0f);
                    Ammo = String.Format("{0}%", AmmoPercent);
                }
                else
                {
                    int missileCount = 0;
                    foreach (KeyValuePair<Pulsar4X.Entities.Components.OrdnanceDefTN, int> pair in m_oCurrnetTaskGroup.Ships[row].ShipOrdnance)
                    {
                        missileCount = missileCount + pair.Value;
                    }
                    float AmmoPercent = (float)Math.Round(((float)missileCount / (float)m_oCurrnetTaskGroup.Ships[row].ShipClass.LauncherCount) * 100.0f);
                    Ammo = String.Format("{0}%", AmmoPercent);
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
#if LOG4NET_ENABLED
                logger.Error("Something went wrong Refreshing Cells for Taskgroup Ship summary screen...");
#endif
            }
        }


        /// <summary>
        /// finds target system for updating systemlocationlist
        /// </summary>
        /// <returns></returns>
        private StarSystem GetTargetSystem()
        {
            StarSystem targetsys = CurrentTaskGroup.Position.System;
            BindingList<Order> borders = CurrentTaskGroup.TaskGroupOrders;
            
            List<Order> orders = CurrentTaskGroup.TaskGroupOrders.ToList();
            Constants.ShipTN.OrderType stdtrans = Constants.ShipTN.OrderType.StandardTransit;
            Constants.ShipTN.OrderType sqdtrans = Constants.ShipTN.OrderType.SquadronTransit;
            Constants.ShipTN.OrderType tndtrans = Constants.ShipTN.OrderType.TransitAndDivide;

            if (orders.Any(o => o.typeOf == stdtrans) || orders.Any(o => o.typeOf == sqdtrans) || orders.Any(o => o.typeOf == tndtrans))
            {
                int index;
                if (SelectedOrderIndex == -1)
                    index = orders.Count -1;
                else
                    index = SelectedOrderIndex -1;
                while (index >= 0)
                {
                    if (orders[index].typeOf == stdtrans || orders[index].typeOf == sqdtrans || orders[index].typeOf == tndtrans)
                    {
                        if (orders[index].jumpPoint.Connect != null)
                            targetsys = orders[index].jumpPoint.Connect.Position.System;
                        else
                            targetsys = null; //jumping into an unknown system.
                        index = -1;
                    }
                        
                    index--;
                }
            }
            return targetsys;
        }

        /// <summary>
        /// Build the Total System Location List here.
        /// </summary>
        private void BuildSystemLocationList()
        {
            //m_oTaskGroupPanel.SystemLocationsListBox.Items.Clear();
            StarSystem targetsystem = GetTargetSystem();

            SystemLocationDict.Clear();
            SystemLocationGuidDict.Clear();
            if (targetsystem != null)
            {
                AddJumpPointsToList(targetsystem);
                AddPlanetsToList(targetsystem);

                if (m_oTaskGroupPanel.DisplayContactsCheckBox.Checked == true)
                    AddContactsToList(targetsystem);

                if (m_oTaskGroupPanel.DisplayTaskGroupsCheckBox.Checked == true)
                    AddTaskGroupsToList(targetsystem);

                if (m_oTaskGroupPanel.DisplayWaypointsCheckBox.Checked == true)
                    AddWaypointsToList(targetsystem);

                if (m_oTaskGroupPanel.DisplaySurveyLocationsCheckBox.Checked == true)
                    AddSurveyPointsToList(targetsystem);
            }
            m_oTaskGroupPanel.SystemLocationsListBox.DataSource = SystemLocationGuidDict.Values.ToList();
        }

        /// <summary>
        /// Handles the mousedown event for the PlottedMoves Listbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlottedMovesListBox_MouseDown(object sender, MouseEventArgs e)
        {

            if (m_oTaskGroupPanel.PlottedMovesListBox.SelectedIndex != -1)
            {
                var rect = m_oTaskGroupPanel.PlottedMovesListBox.GetItemRectangle(m_oTaskGroupPanel.PlottedMovesListBox.SelectedIndex);
                if (rect.Contains(e.Location) && m_oTaskGroupPanel.PlottedMovesListBox.SelectedIndex != SelectedOrderIndex)
                {
                    m_oTaskGroupPanel.AddMoveButton.Text = "Insert Order";
                    SelectedOrderIndex = m_oTaskGroupPanel.PlottedMovesListBox.SelectedIndex;
                }
                else
                {
                    PrevioslySelectedOrderIndex = m_oTaskGroupPanel.PlottedMovesListBox.SelectedIndex;
                    m_oTaskGroupPanel.PlottedMovesListBox.SelectedIndex = -1;
                    SelectedOrderIndex = -1;
                    m_oTaskGroupPanel.AddMoveButton.Text = "Add Order";
                }                
            }
            BuildSystemLocationList();
        }

        /// <summary>
        /// Builds available orders here. Right now, moveTo is the only one worthwhile. also want to replace this with a proper string at some point.
        /// </summary>
        private void BuildActionList()
        {
            var currentSelectedAction = m_oTaskGroupPanel.AvailableActionsListBox.SelectedItem;
            ClearActionList();

            List<Guid> GID = SystemLocationGuidDict.Keys.ToList();

            if (m_oTaskGroupPanel.SystemLocationsListBox.SelectedIndex == -1)
                return;


            StarSystemEntity selectedEntity = SystemLocationDict[GID[m_oTaskGroupPanel.SystemLocationsListBox.SelectedIndex]].Entity;
            SystemListObject.ListEntityType entityType = SystemLocationDict[GID[m_oTaskGroupPanel.SystemLocationsListBox.SelectedIndex]].EntityType;

            List<Order> previousOrders = new List<Order>();
            int olistindex = m_oTaskGroupPanel.PlottedMovesListBox.SelectedIndex;

            if (CurrentTaskGroup.TaskGroupOrders.Count > 0 && olistindex >= 0)
            {
                previousOrders = CurrentTaskGroup.TaskGroupOrders.ToList().GetRange(0, olistindex);
            }
            foreach (var item in legalOrders(CurrentTaskGroup, selectedEntity, previousOrders))
            {
                m_oTaskGroupPanel.AvailableActionsListBox.Items.Add(item);
            }

            //set the selected action to be the previously selected action if it exsists.
            if (currentSelectedAction != null && m_oTaskGroupPanel.AvailableActionsListBox.Items.Contains(currentSelectedAction))
                m_oTaskGroupPanel.AvailableActionsListBox.SelectedItem = currentSelectedAction;
        }


        /// <summary>
        /// creates a list of orders by creating a union and intersection of other lists.
        /// </summary>
        /// <param name="thisTG"></param>
        /// <param name="targetEntity"></param>
        /// <param name="previousOrders"></param>
        /// <returns></returns>
        private List<Constants.ShipTN.OrderType> legalOrders(TaskGroupTN thisTG, StarSystemEntity targetEntity, List<Order> previousOrders)
        {
            List<Constants.ShipTN.OrderType> thisTGLegalOrders = new List<Constants.ShipTN.OrderType>();
            List<Constants.ShipTN.OrderType> additionalOrders = new List<Constants.ShipTN.OrderType>();
            List<Constants.ShipTN.OrderType> targetEntityLegalOrders = targetEntity.LegalOrders(CurrentTaskGroup.TaskGroupFaction);

            if (!m_oTaskGroupPanel.OrderFilteringCheckBox.Checked)
            {
                thisTGLegalOrders = Enum.GetValues(typeof(Constants.ShipTN.OrderType)).Cast<Constants.ShipTN.OrderType>().ToList();
            }
            else
            {
                thisTGLegalOrders = thisTG.LegalOrdersTG();
                foreach (var order in previousOrders)
                {
                    additionalOrders = additionalOrders.Union(order.EnablesTypeOf()).ToList();
                }
                thisTGLegalOrders = thisTGLegalOrders.Union(additionalOrders).ToList();
            }

            //it still needs to do an intersect with the targetEntityLegalOrders, regardless of filtering.
            thisTGLegalOrders = thisTGLegalOrders.Intersect(targetEntityLegalOrders).ToList();

            return thisTGLegalOrders;
        }


        /// <summary>
        /// Clears the action list.
        /// </summary>
        private void ClearActionList()
        {
            m_oTaskGroupPanel.AvailableActionsListBox.Items.Clear();
        }

        /// <summary>
        /// Build the list of TG orders here.
        /// </summary>
        private void BuildPlottedMoveList()
        {
            int prevIndex = SelectedOrderIndex;
            SelectedOrderIndex = -1;
            m_oTaskGroupPanel.PlottedMovesListBox.Items.Clear();
            foreach(Order order in CurrentTaskGroup.TaskGroupOrders)// (int loop = 0; loop < CurrentTaskGroup.TaskGroupOrders.Count; loop++)
            {
                m_oTaskGroupPanel.PlottedMovesListBox.Items.Add(order.Name);
            }

            if (prevIndex < CurrentTaskGroup.TaskGroupOrders.Count)
            {

                m_oTaskGroupPanel.PlottedMovesListBox.SelectedIndex = prevIndex;
                SelectedOrderIndex = prevIndex;
            }
        }

        /// <summary>
        /// Add every planet in the system that this TG is in to the list.
        /// Eventually jump orders will modify this. to be the system at the end of the order stack.
        /// </summary>
        private void AddPlanetsToList(StarSystem starsystem)
        {
            foreach (Star star in starsystem.Stars)
            {
                foreach (SystemBody planet in star.Planets)
                {
                    int PopCount = 0;
                    string keyName = "N/A";
                    foreach(Population CurrentPopulation in planet.Populations)
                    {
                        if (CurrentPopulation.Faction == CurrentFaction)
                        {
                            keyName = string.Format("{0} - {1}",CurrentPopulation.Name, CurrentPopulation.Species.Name);
                            if(CurrentFaction.Capitol == CurrentPopulation)
                                keyName = string.Format("{0}(Capitol)", keyName);

                            keyName = string.Format("{0}: {1:n2}m", keyName, CurrentPopulation.CivilianPopulation);

                            StarSystemEntity entObj = CurrentPopulation;
                            SystemListObject.ListEntityType entType = SystemListObject.ListEntityType.Colonies;
                            SystemListObject valueObj = new SystemListObject(entType, entObj);
                            SystemLocationGuidDict.Add(entObj.Id, keyName);
                            SystemLocationDict.Add(entObj.Id, valueObj);

                            PopCount++;
                        }
                    }

                    if (PopCount == 0)
                    {
                        //m_oTaskGroupPanel.SystemLocationsListBox.Items.Add(CurrentTaskGroup.Contact.Position.System.Stars[loop].Planets[loop2]);
                        keyName = planet.Name;//CurrentTaskGroup.Contact.Position.System.Stars[loop].Planets[loop2].Name;
                        StarSystemEntity entObj = planet;//CurrentTaskGroup.Contact.Position.System.Stars[loop].Planets[loop2];
                        SystemListObject.ListEntityType entType = SystemListObject.ListEntityType.Planets;
                        SystemListObject valueObj = new SystemListObject(entType, entObj);
                        SystemLocationGuidDict.Add(entObj.Id, keyName);
                        SystemLocationDict.Add(entObj.Id, valueObj);
                    }
                }
            }
        }

        /// <summary>
        /// Add jump points to the available locations list.
        /// </summary>
        /// <param name="starsystem"></param>
        private void AddJumpPointsToList(StarSystem starsystem)
        {
            /// <summary>
            /// If this starsystem has no survey results for this faction, or the results aren't complete or incomplete then do nothing.
            /// </summary>
            if (starsystem._SurveyResults.ContainsKey(CurrentFaction) == true)
            {
                /// <summary>
                /// Every JP is detected. add them all.
                /// </summary>
                if (starsystem._SurveyResults[CurrentFaction]._SurveyStatus == JPDetection.Status.Complete)
                {
                    foreach (JumpPoint jp in starsystem.JumpPoints)
                    {
                        StarSystemEntity entObj = jp;
                        SystemListObject.ListEntityType entType = SystemListObject.ListEntityType.JumpPoint;
                        SystemListObject valueObj = new SystemListObject(entType, entObj);
                        SystemLocationGuidDict.Add(entObj.Id, jp.Name);
                        SystemLocationDict.Add(entObj.Id, valueObj);
                    }
                }
                /// <summary>
                /// Only some of the JPs are detected, so list only those.
                /// </summary>
                else if (starsystem._SurveyResults[CurrentFaction]._SurveyStatus == JPDetection.Status.Incomplete)
                {
                    foreach (JumpPoint jp in starsystem._SurveyResults[CurrentFaction]._DetectedJPs)
                    {
                        StarSystemEntity entObj = jp;
                        SystemListObject.ListEntityType entType = SystemListObject.ListEntityType.JumpPoint;
                        SystemListObject valueObj = new SystemListObject(entType, entObj);
                        SystemLocationGuidDict.Add(entObj.Id, jp.Name);
                        SystemLocationDict.Add(entObj.Id, valueObj);
                    }
                }
            }
        }

        /// <summary>
        /// Adds any detected ships to the system location box.
        /// </summary>
        private void AddContactsToList(StarSystem starsystem)
        {

            if (CurrentFaction.DetectedContactLists.ContainsKey(starsystem) == true)
            {
                foreach (KeyValuePair<ShipTN, FactionContact> pair in CurrentFaction.DetectedContactLists[CurrentTaskGroup.Contact.Position.System].DetectedContacts)
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

                    //m_oTaskGroupPanel.SystemLocationsListBox.Items.Add(Entry);
                    SystemListObject.ListEntityType entType = SystemListObject.ListEntityType.Contacts;
                    SystemListObject valueObj = new SystemListObject(entType, pair.Key); //maybe this should be the value? though with the key I can *get* the value easly anyway.
                    SystemLocationGuidDict.Add(pair.Key.Id, Entry);
                    SystemLocationDict.Add(pair.Key.Id, valueObj);
                }

                foreach (KeyValuePair<Population, FactionContact> pair in CurrentFaction.DetectedContactLists[CurrentTaskGroup.Contact.Position.System].DetectedPopContacts)
                {
                    String TH = "";
                    if (pair.Value.thermal == true)
                    {
                        TH = String.Format("[Thermal {0}]", pair.Key.ThermalSignature);
                    }

                    String EM = "";
                    if (pair.Value.EM == true)
                    {
                        EM = String.Format("[EM {0}]", pair.Key.EMSignature);
                    }

                    String ACT = "";
                    if (pair.Value.active == true)
                    {
                        ACT = String.Format("[Active Ping]");
                    }

                    String Entry = String.Format("{0} {1}{2}{3}", pair.Key.Name, TH, EM, ACT);

                    //m_oTaskGroupPanel.SystemLocationsListBox.Items.Add(Entry);
                    SystemListObject.ListEntityType entType = SystemListObject.ListEntityType.Contacts;
                    SystemListObject valueObj = new SystemListObject(entType, pair.Key); //maybe this should be the value? though with the key I can *get* the value easly anyway.
                    SystemLocationGuidDict.Add(pair.Key.Id, Entry);
                    SystemLocationDict.Add(pair.Key.Id, valueObj);
                }
            }
        }

        /// <summary>
        /// Adds friendly taskgroups to the system location box
        /// </summary>
        private void AddTaskGroupsToList(StarSystem starsystem)
        {           
            foreach(SystemContact contact in starsystem.SystemContactList)
            {
                if (contact.SSEntity == StarSystemEntityType.TaskGroup)
                {
                    TaskGroupTN TaskGroup = contact.Entity as TaskGroupTN;
                    if (CurrentTaskGroup != TaskGroup && TaskGroup.TaskGroupFaction == CurrentFaction)
                    {
                        //m_oTaskGroupPanel.SystemLocationsListBox.Items.Add(CurrentTaskGroup.Contact.Position.System.SystemContactList[loop].TaskGroup);
                        string keyName = TaskGroup.Name;
                        StarSystemEntity entObj = TaskGroup;
                        SystemListObject valueObj = new SystemListObject(SystemListObject.ListEntityType.TaskGroups, entObj);
                        SystemLocationGuidDict.Add(entObj.Id, keyName);
                        SystemLocationDict.Add(entObj.Id, valueObj);
                    }
                }
            }
        }

        /// <summary>
        /// Adds user generated waypoints to the location list.
        /// </summary>
        private void AddWaypointsToList(StarSystem starsystem)
        {
            foreach (Waypoint waypoint in starsystem.Waypoints)
            {
                if (waypoint.FactionId == CurrentTaskGroup.TaskGroupFaction.FactionID)
                {   
                    string keyName = waypoint.Name;
                    StarSystemEntity entObj = waypoint;
                    SystemListObject valueObj = new SystemListObject(SystemListObject.ListEntityType.Waypoints, entObj);
                    SystemLocationGuidDict.Add(entObj.Id, keyName);
                    SystemLocationDict.Add(entObj.Id, valueObj);
                }
            }
        }

        /// <summary>
        /// Adds grav survey points to the location list.
        /// </summary>
        /// <param name="starsystem"></param>
        private void AddSurveyPointsToList(StarSystem starsystem)
        {
            int SPIndex = 1;
            bool DisplaySP = false;
            foreach (SurveyPoint SP in starsystem._SurveyPoints)
            {
                if (m_oTaskGroupPanel.ExcludeSurveyedCheckBox.Checked == true)
                {
                    /// <summary>
                    /// it is important to always check to see if the desired key is in the dictionary before using said dictionary to prevent null references, crashes, and so on.
                    /// </summary>
                    if (starsystem._SurveyResults.ContainsKey(CurrentFaction) == true)
                    {
                        if (starsystem._SurveyResults[CurrentFaction]._SurveyStatus == JPDetection.Status.Complete)
                        {
                            /// <summary>
                            /// Don't display any survey points, they are all surveyed, and surveyed points should be excluded.
                            /// </summary>
                            return;
                        }
                        else if (starsystem._SurveyResults[CurrentFaction]._SurveyStatus == JPDetection.Status.Incomplete)
                        {
                            if (starsystem._SurveyResults[CurrentFaction]._SurveyedPoints.Contains(SP) == false)
                            {
                                /// <summary>
                                /// Display only those sps not yet surveyed.
                                /// </summary>
                                DisplaySP = true;
                            }
                        }
                        else
                        {
                            /// <summary>
                            /// Display everything, as the survey status should be none in this case.
                            /// </summary>
                            DisplaySP = true;
                        }
                    }
                    else
                    {
                        /// <summary>
                        /// This faction has performed no survey, so add all points.
                        /// </summary>
                        DisplaySP = true;
                    }
                }
                else
                {
                    DisplaySP = true;
                }

                if (DisplaySP == true)
                {
                    string keyName = String.Format("Survey Location #{0}", SPIndex);
                    StarSystemEntity entObj = SP;
                    SystemListObject valueObj = new SystemListObject(SystemListObject.ListEntityType.SurveyPoints, entObj);
                    SystemLocationGuidDict.Add(entObj.Id, keyName);
                    SystemLocationDict.Add(entObj.Id, valueObj);
                    DisplaySP = false;
                }   
                SPIndex++;
            }
        }

        /// <summary>
        /// Time and distance or orders should be calculated here based on the radio button selection choices.
        /// Update: as with the lib taskgroup orders, this function should not be using parent position anymore. all the if blocks are as a result unnecessary.
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
                        dX = CurrentTaskGroup.Contact.Position.X - CurrentTaskGroup.TaskGroupOrders[0].target.Position.X;
                        dY = CurrentTaskGroup.Contact.Position.Y - CurrentTaskGroup.TaskGroupOrders[0].target.Position.Y;
                    }
                    else if (CurrentTaskGroup.TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Population)
                    {
                        dX = CurrentTaskGroup.Contact.Position.X - CurrentTaskGroup.TaskGroupOrders[0].target.Position.X;
                        dY = CurrentTaskGroup.Contact.Position.Y - CurrentTaskGroup.TaskGroupOrders[0].target.Position.Y;
                    }
                    else
                    {
                        dX = CurrentTaskGroup.Contact.Position.X - CurrentTaskGroup.TaskGroupOrders[0].target.Position.X;
                        dY = CurrentTaskGroup.Contact.Position.Y - CurrentTaskGroup.TaskGroupOrders[0].target.Position.Y;
                    }

                    dZ = Math.Sqrt((dX * dX) + (dY * dY));

                }
                else if (m_oTaskGroupPanel.AllOrdersTDRadioButton.Checked == true)
                {
                    if (CurrentTaskGroup.IsOrbiting == true)
                        CurrentTaskGroup.GetPositionFromOrbit();

                    double tX = CurrentTaskGroup.Contact.Position.X;
                    double tY = CurrentTaskGroup.Contact.Position.Y;

                    for (int loop = 0; loop < CurrentTaskGroup.TaskGroupOrders.Count; loop++)
                    {
                        if (CurrentTaskGroup.TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Body)
                        {
                            dX = CurrentTaskGroup.Contact.Position.X - CurrentTaskGroup.TaskGroupOrders[loop].target.Position.X;
                            dY = CurrentTaskGroup.Contact.Position.Y - CurrentTaskGroup.TaskGroupOrders[loop].target.Position.Y;
                        }
                        else if (CurrentTaskGroup.TaskGroupOrders[loop].target.SSEntity == StarSystemEntityType.Population)
                        {
                            dX = tX - CurrentTaskGroup.TaskGroupOrders[loop].target.Position.X;
                            dY = tY - CurrentTaskGroup.TaskGroupOrders[loop].target.Position.Y;
                        }
                        else
                        {
                            dX = tX - CurrentTaskGroup.TaskGroupOrders[loop].target.Position.X;
                            dY = tY - CurrentTaskGroup.TaskGroupOrders[loop].target.Position.Y;
                        }

                        dZ = dZ + Math.Sqrt((dX * dX) + (dY * dY));

                        if (CurrentTaskGroup.TaskGroupOrders[loop].typeOf != Constants.ShipTN.OrderType.StandardTransit &&
                            CurrentTaskGroup.TaskGroupOrders[loop].typeOf != Constants.ShipTN.OrderType.SquadronTransit &&
                            CurrentTaskGroup.TaskGroupOrders[loop].typeOf != Constants.ShipTN.OrderType.TransitAndDivide)
                        {
                            if (CurrentTaskGroup.TaskGroupOrders[0].target.SSEntity == StarSystemEntityType.Body)
                            {
                                tX = CurrentTaskGroup.TaskGroupOrders[loop].target.Position.X;
                                tY = CurrentTaskGroup.TaskGroupOrders[loop].target.Position.Y;
                            }
                            else if (CurrentTaskGroup.TaskGroupOrders[loop].target.SSEntity == StarSystemEntityType.Population)
                            {
                                tX = CurrentTaskGroup.TaskGroupOrders[loop].target.Position.X;
                                tY = CurrentTaskGroup.TaskGroupOrders[loop].target.Position.Y;
                            }
                            else
                            {
                                tX = CurrentTaskGroup.TaskGroupOrders[loop].target.Position.X;
                                tY = CurrentTaskGroup.TaskGroupOrders[loop].target.Position.Y;
                            }
                        }
                        else
                        {
                            /// <summary>
                            /// As the TG will be in a new system, set the jump point far end locations here.
                            /// </summary>

                            try
                            {
                                tX = CurrentTaskGroup.TaskGroupOrders[loop].jumpPoint.Connect.Position.X;
                                tY = CurrentTaskGroup.TaskGroupOrders[loop].jumpPoint.Connect.Position.Y;
                            }
                            catch
                            {
#if LOG4NET_ENABLED
                                logger.Error("No Jumppoint associated with jump point transit order in CalcTimeDistance in taskgroup.cs under handlers.");
#endif
                            }
                        }
                    }
                }

                if (dZ >= Constants.Units.MAX_KM_IN_AU)
                {
                    /// <summary>
                    /// DeltaZ is calculated in astronomic units, but we want km here. First dZ needs to be checked to make sure it won't overflow.
                    /// Count is the number of Max_KM_IN_AU present in dZ, so if dZ were ~144 AU, count would be around 10, meaning that there are around 20 B km in dZ.
                    /// </summary>
                    double Count = dZ / Constants.Units.MAX_KM_IN_AU;

#warning magic numbers in distance/time calculation for taskgroup
                    // What is this magic number 2.147483648?
                    // Please, if we're going to put number constants, at least explain what they mean.
                    ///< @todo Update this with our Constants.TimeInSeconds calculations to keep things consistent.
                    // 2.14783648 is 2^31 / 1B. or the signed 32 bit limit. count is the number of signed 32 bit int limit kms in dZ.
                    //multiplying and dividing by 100 gets a fraction of the distance past the Math.Floor operator, so we can have 1.25 B km for example.
                    double newDistance = Math.Floor(2.147483648 * Count * 100.0);
                    newDistance = newDistance / 100.0;

                    DistanceString = "Distance: " + newDistance.ToString() + "B km";

                    //if timeReq is bigger than this value then time shouldn't be calculated.
                    double maxTime = 2.147483648;

                    double timeReq = newDistance / (double)CurrentTaskGroup.CurrentSpeed;

                    TimeString = "ETA: N/A";

                    if (timeReq < maxTime)
                    {
                        //TimeReq was divided by 1B, correct that here.
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

                    double Distance = Math.Floor(dZ * Constants.Units.KmPerAu);

                    double TimeSeconds = Math.Floor(Distance / CurrentTaskGroup.CurrentSpeed);

                    double TimeMinutes = Math.Floor(TimeSeconds / 60.0);
                    TimeSeconds = TimeSeconds - (TimeMinutes * 60.0);


                    double TimeHours = Math.Floor(TimeMinutes / 60.0);
                    TimeMinutes = TimeMinutes - (TimeHours * 60.0);

                    DistanceString = "Distance: ";
                    TimeString = "ETA: " + TimeHours.ToString() + ":" + TimeMinutes.ToString() + ":" + TimeSeconds.ToString();

                    if (Distance > 1000000000.0)
                    {
                        Distance = Math.Floor(Distance / 10000000.0);
                        Distance = Distance / 100.0;

                        DistanceString = "Distance: " + Distance.ToString() + "B km";
                    }
                    else if (Distance > 1000000.0)
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


        #region Organization Tab
        
        /// <summary>
        /// Specific function to just update one list box and not the other.
        /// </summary>
        private void BuildSelectedTGListBox()
        {
            int TGIndex = m_oTaskGroupPanel.OrgSelectedTGComboBox.SelectedIndex;
            m_oTaskGroupPanel.OrgSelectedTGListBox.Items.Clear();
            if (TGIndex != -1)
            {
                foreach (ShipTN CurrentShip in OrgSelectedTGList[TGIndex].Ships)
                {
                    m_oTaskGroupPanel.OrgSelectedTGListBox.Items.Add(CurrentShip);
                }
            }
        }

        /// <summary>
        /// Function to just update the CurrentTGListBox.
        /// </summary>
        private void BuildCurrentTGListBox()
        {
            if (CurrentTaskGroup != null)
            {
                m_oTaskGroupPanel.OrgCurrentTGTextBox.Text = CurrentTaskGroup.Name;

                m_oTaskGroupPanel.OrgCurrentTGListBox.Items.Clear();
                foreach (ShipTN CurrentShip in CurrentTaskGroup.Ships)
                {
                    m_oTaskGroupPanel.OrgCurrentTGListBox.Items.Add(CurrentShip);
                }
            }
        }

        /// <summary>
        /// List the ships in each taskgroup for display.
        /// </summary>
        private void BuildOrganizationTab()
        {
            BuildCurrentTGListBox();
            BuildSelectedTGListBox();
        }

        /// <summary>
        /// What taskgroups are located near the current TG?
        /// </summary>
        private void BuildOrgSelectedTGList()
        {
            m_oTaskGroupPanel.OrgSelectedTGComboBox.Items.Clear();
            OrgSelectedTGList.Clear();
            /// <summary>
            /// CurrentTaskgroup is around a planet, so just get that planet's taskgroups in orbit.
            /// </summary>
            if (CurrentTaskGroup.IsOrbiting == true)
            {
                SystemBody CurPlanet = CurrentTaskGroup.OrbitingBody as SystemBody;
                foreach (TaskGroupTN CurTaskgroup in CurPlanet.TaskGroupsInOrbit)
                {
                    if (CurTaskgroup == CurrentTaskGroup)
                        continue;

                    if (CurTaskgroup.TaskGroupFaction == CurrentTaskGroup.TaskGroupFaction)
                    {
                        OrgSelectedTGList.Add(CurTaskgroup);
                        m_oTaskGroupPanel.OrgSelectedTGComboBox.Items.Add(CurTaskgroup);
                    }
                }
            }
            /// <summary>
            /// iterate through the system contact list looking for taskgroups of the same faction, and check their distance from CurrentTaskgroup.
            /// </summary>
            else
            {
                StarSystem CurSystem = CurrentTaskGroup.Position.System;

                foreach (SystemContact CurContact in CurSystem.SystemContactList)
                {
                    if (CurrentTaskGroup.Contact == CurContact)
                        continue;

                    if (CurContact.SSEntity == StarSystemEntityType.TaskGroup && CurContact.faction == CurrentTaskGroup.TaskGroupFaction)
                    {
                        float dist;
                        CurrentTaskGroup.Contact.DistTable.GetDistance(CurContact, out dist);

                        if (dist == 0.0)
                        {
                            OrgSelectedTGList.Add((CurContact.Entity as TaskGroupTN));
                            m_oTaskGroupPanel.OrgSelectedTGComboBox.Items.Add(CurContact.Name);
                        }
                    }
                }
            }


            /// <summary>
            /// Do this here and not in BuildOrgSelectedTGList. doing it there will cause the event handler to constantly call itself.
            /// </summary>
            if (m_oTaskGroupPanel.OrgSelectedTGComboBox.Items.Count != 0)
                m_oTaskGroupPanel.OrgSelectedTGComboBox.SelectedIndex = 0;
        }
        #endregion

        /// <summary>
        /// Refresh the TG page.
        /// </summary>
        private void RefreshTGPanel()
        {
            if (CurrentTaskGroup != null)
            {
                m_oTaskGroupPanel.TaskGroupLocationTextBox.Text = CurrentTaskGroup.Contact.Position.System.Name;
                m_oTaskGroupPanel.SetSpeedTextBox.Text = CurrentTaskGroup.CurrentSpeed.ToString();
                m_oTaskGroupPanel.MaxSpeedTextBox.Text = CurrentTaskGroup.MaxSpeed.ToString();

                RefreshShipCells();               
                ClearActionList();
                BuildPlottedMoveList();
                BuildSystemLocationList();
                CalculateTimeDistance();

                BuildOrgSelectedTGList();
                BuildOrganizationTab();
            }
        }

        #endregion
    }
}
