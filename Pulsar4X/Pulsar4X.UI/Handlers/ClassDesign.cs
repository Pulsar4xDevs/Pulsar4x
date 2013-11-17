using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;
using WeifenLuo.WinFormsUI.Docking;
using Pulsar4X.UI.ViewModels;
using Pulsar4X.Entities;
using Pulsar4X.Stargen;
using log4net.Config;
using log4net;
using Newtonsoft.Json;
using Pulsar4X.Entities.Components;


namespace Pulsar4X.UI.Handlers
{
    public class ClassDesign
    {

        /// <summary>
        /// Current faction, need this to update display under all circumstances I think.
        /// </summary>
        private Faction _CurrnetFaction;
        public Faction CurrentFaction
        {
            get { return _CurrnetFaction; }
            set
            {

                if (_CurrnetFaction != value)
                {
                    _CurrnetFaction = value;
                    if (_CurrnetFaction.ShipDesigns.Count != 0)
                    {
                        _CurrnetShipClass = _CurrnetFaction.ShipDesigns[0];
                        UpdateDisplay();
                    }
                }

            }
        }

        /// <summary>
        /// Current shipclass, need this to update display under all circumstances I think.
        /// </summary>
        private ShipClassTN _CurrnetShipClass;
        public ShipClassTN CurrentShipClass
        {
            get { return _CurrnetShipClass; }
            set
            {
                if (_CurrnetShipClass != value)
                {
                    _CurrnetShipClass = value;

                    if (_CurrnetShipClass != null)
                    {
                        UpdateDisplay();
                    }
                }
            }
        }

        /// <summary>
        /// Class Design Logger:
        /// </summary>
        public static readonly ILog logger = LogManager.GetLogger(typeof(TaskGroup));

        //Panels.ClassDes_DesignAndInfo m_oDesignAndInformationPanel;
        Panels.ClassDes_Options m_oOptionsPanel;
        //Panels.ClassDes_Properties m_oClassPropertiesPanel;

        ClassDesignViewModel VM;

        /// <summary>
        /// amount to add/subtract from a design for a specified component.
        /// </summary>
        private int ComponentAmt { get; set; }

        /// <summary>
        /// Component columns for the datagrid that displays components in the faction list.
        /// </summary>
        public enum ComponentCell
        {
            Name,
            RatingType,
            Rating,
            Cost,
            Size,
            Crew,
            Materials,
            CType,
            CIndex,
            TypeCount
        }

        /// <summary>
        /// how many components are in the component list data grid?
        /// </summary>
        private int TotalComponents { get; set; }

        /// <summary>
        /// Location of each "grouping" of components.
        /// </summary>
        private BindingList<int> CompLocation { get; set; }

        /// <summary>
        /// Each Group type of components.
        /// </summary>
        public enum ComponentGroup
        {
            Basic,
            Engines,
            FireControl,
            Beam,
            Missiles,
            Magazines,
            Ground,
            Reactors,
            Actives,
            Passives,
            Shields,
            Industry,
            TypeCount
        }

        public ClassDesign()
        {

            ComponentAmt = 1;

            TotalComponents = 0;

            CompLocation = new BindingList<int>();

            // create panels:
            //m_oClassPropertiesPanel = new Panels.ClassDes_Properties();
            //m_oDesignAndInformationPanel = new Panels.ClassDes_DesignAndInfo();
            m_oOptionsPanel = new Panels.ClassDes_Options();

            //m_oClassPropertiesPanel.ClassPropertyGrid.PropertySort = PropertySort.CategorizedAlphabetical;

            // creat ViewModel.
            VM = new ClassDesignViewModel();

            /// <summary>
            /// setup bindings:
            /// </summary>
            m_oOptionsPanel.FactionComboBox.Bind(c => c.DataSource, VM, d => d.Factions);
            m_oOptionsPanel.FactionComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentFaction, DataSourceUpdateMode.OnPropertyChanged);
            m_oOptionsPanel.FactionComboBox.DisplayMember = "Name";
            VM.FactionChanged += (s, args) => CurrentFaction = VM.CurrentFaction;
            CurrentFaction = VM.CurrentFaction;

            m_oOptionsPanel.FactionComboBox.SelectedIndexChanged += (s, args) => m_oOptionsPanel.FactionComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oOptionsPanel.FactionComboBox.SelectedIndexChanged += new EventHandler(FactionComboBox_SelectedIndexChanged);

            m_oOptionsPanel.ClassComboBox.Bind(c => c.DataSource, VM, d => d.ShipDesigns);
            m_oOptionsPanel.ClassComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentShipClass, DataSourceUpdateMode.OnPropertyChanged);
            m_oOptionsPanel.ClassComboBox.DisplayMember = "Name";
            VM.ShipClassChanged += (s, args) => CurrentShipClass = VM.CurrentShipClass;
            CurrentShipClass = VM.CurrentShipClass;
            m_oOptionsPanel.ClassComboBox.SelectedIndexChanged += (s, args) => m_oOptionsPanel.ClassComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oOptionsPanel.ClassComboBox.SelectedIndexChanged += new EventHandler(ClassComboBox_SelectedIndexChanged);

            //if (VM.CurrentShipClass != null)
            //{
            //    m_oClassPropertiesPanel.ClassPropertyGrid.SelectedObject = VM.CurrentShipClass;
            //}


            // Setup Events:
            m_oOptionsPanel.NewButton.Click += new EventHandler(NewButton_Click);

            m_oOptionsPanel.SizeInTonsCheckBox.CheckStateChanged += new EventHandler(SizeInTonsCheckBox_CheckedStateChanged);
            m_oOptionsPanel.GroupComponentsCheckBox.CheckStateChanged += new EventHandler(GroupComponentsCheckBox_CheckStateChanged);

            m_oOptionsPanel.OneRadioButton.CheckedChanged += new EventHandler(AMTRadioButton_CheckedChanged);
            m_oOptionsPanel.FiveRadioButton.CheckedChanged += new EventHandler(AMTRadioButton_CheckedChanged);
            m_oOptionsPanel.TenRadioButton.CheckedChanged += new EventHandler(AMTRadioButton_CheckedChanged);
            m_oOptionsPanel.HundredRadioButton.CheckedChanged += new EventHandler(AMTRadioButton_CheckedChanged);

            if (CurrentFaction != null)
            {
                if(CurrentFaction.ShipDesigns.Count != 0)
                    CurrentShipClass = CurrentFaction.ShipDesigns[0];
            }

            m_oOptionsPanel.ComponentDataGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            m_oOptionsPanel.ComponentDataGrid.RowHeadersVisible = false;
            m_oOptionsPanel.ComponentDataGrid.AutoGenerateColumns = false;
            SetupComponentDataGrid();

            m_oOptionsPanel.RefreshTechButton.Click += new EventHandler(RefreshTechButton_Click);

            UpdateDisplay();

        }

        #region event handlers
        /// <summary>
        ///  BuildComponentDataGrid is an intensive function that should only be run under two circumstances. Obs Comb and salvaged comp to be handled later.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FactionComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            TotalComponents = 0;
            BuildComponentDataGrid();
        }

        private void RefreshTechButton_Click(object sender, EventArgs e)
        {
            BuildComponentDataGrid();
        }

        private  void ClassComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            //if (VM.CurrentShipClass != null)
            //{
            //    m_oClassPropertiesPanel.ClassPropertyGrid.SelectedObject = VM.CurrentShipClass;
            //}
        }

        /// <summary>
        /// New ship class button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewButton_Click(object sender, EventArgs e)
        {
            ShipClassTN oNewShipClass = new ShipClassTN("New Class",VM.CurrentFaction);
            VM.ShipDesigns.Add(oNewShipClass);
            m_oOptionsPanel.ClassComboBox.SelectedItem = oNewShipClass;
        }

        /// <summary>
        /// Checkbox controlling for HS vs tonnage display
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SizeInTonsCheckBox_CheckedStateChanged(object sender, EventArgs e)
        {
            if (CurrentShipClass != null)
            {
                BuildPassiveDefences();
            }
        }

        /// <summary>
        /// Should components be grouped by type in the list box or just printed out as is?
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GroupComponentsCheckBox_CheckStateChanged(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        /// <summary>
        /// Handle how many components the user wants to add or subtract from a design.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AMTRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (m_oOptionsPanel.OneRadioButton.Checked == true)
                ComponentAmt = 1;
            else if (m_oOptionsPanel.FiveRadioButton.Checked == true)
                ComponentAmt = 5;
            else if (m_oOptionsPanel.TenRadioButton.Checked == true)
                ComponentAmt = 10;
            else if (m_oOptionsPanel.HundredRadioButton.Checked == true)
                ComponentAmt = 100;
        }
        #endregion


        #region PublicMethods

        public void ShowAllPanels(DockPanel a_oDockPanel)
        {
            //ShowPropertiesPanel(a_oDockPanel);
            //ShowDesignAndInfoPanel(a_oDockPanel);
            ShowOptionsPanel(a_oDockPanel);
        }

        public void ShowPropertiesPanel(DockPanel a_oDockPanel)
        {
            //Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            //m_oClassPropertiesPanel.Show(a_oDockPanel, DockState.DockRight);
            //Helpers.UIController.Instance.SuspendAutoPanelDisplay = false; 
        }

        public void ActivatePropertiesPanel()
        {
            //Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            //m_oClassPropertiesPanel.Activate();
            //Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void ShowDesignAndInfoPanel(DockPanel a_oDockPanel)
        {
            //Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            //m_oDesignAndInformationPanel.Show(a_oDockPanel, DockState.Document);
            //Helpers.UIController.Instance.SuspendAutoPanelDisplay = false; 
        }

        public void ActivateDesignAndInfoPanel()
        {
            //Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            //m_oDesignAndInformationPanel.Activate();
            //Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void ShowOptionsPanel(DockPanel a_oDockPanel)
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oOptionsPanel.Show(a_oDockPanel, DockState.Document);
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void ActivateOptionsPanel()
        {
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = true;
            m_oOptionsPanel.Activate();
            Helpers.UIController.Instance.SuspendAutoPanelDisplay = false;
        }

        public void SMOn()
        {
            // todo
        }

        public void SMOff()
        {
            // todo
        }

        /// <summary>
        /// Updates the overall display for class design.
        /// </summary>
        public void UpdateDisplay()
        {
            if (CurrentShipClass != null)
            {
                BuildMisc();
                BuildPowerSystems();
                BuildPassiveDefences();
                BuildCrewAccomPanel();

                BuildDesignTab();
            }
        }

        #endregion


        #region Private methods
        /// <summary>
        /// print the current class summary to the appropriate window.
        /// </summary>
        private void BuildMisc()
        {
            m_oOptionsPanel.ClassSummaryTextBox.Text = CurrentShipClass.Summary;
            m_oOptionsPanel.BPCostTextBox.Text = Math.Round(CurrentShipClass.BuildPointCost).ToString();
        }

        /// <summary>
        /// Power systems panel.
        /// </summary>
        private void BuildPowerSystems()
        {
            m_oOptionsPanel.EnginePowerTextBox.Text = CurrentShipClass.MaxEnginePower.ToString();
            m_oOptionsPanel.MaxSpeedTextBox.Text = CurrentShipClass.MaxSpeed.ToString();
            m_oOptionsPanel.ReactorPowerTextBox.Text = CurrentShipClass.TotalPowerGeneration.ToString();
            m_oOptionsPanel.RequiredPowerTextBox.Text = CurrentShipClass.TotalPowerRequirement.ToString();
        }

        /// <summary>
        /// Passive defences panel
        /// </summary>
        private void BuildPassiveDefences()
        {
            m_oOptionsPanel.ArmorRatingTextBox.Text = CurrentShipClass.ShipArmorDef.depth.ToString();

            if(m_oOptionsPanel.SizeInTonsCheckBox.Checked == true)
               m_oOptionsPanel.ExactClassSizeTextBox.Text = CurrentShipClass.SizeTons.ToString();
            else
               m_oOptionsPanel.ExactClassSizeTextBox.Text = CurrentShipClass.SizeHS.ToString();

            m_oOptionsPanel.ArmorAreaTextBox.Text = (CurrentShipClass.ShipArmorDef.area / 4.0f).ToString();
            m_oOptionsPanel.ArmorStrengthTextBox.Text = Math.Round((CurrentShipClass.ShipArmorDef.area / 16.0) * (double)CurrentShipClass.ShipArmorDef.depth).ToString();
            m_oOptionsPanel.ArmorColumnsTextBox.Text = CurrentShipClass.ShipArmorDef.cNum.ToString();
            m_oOptionsPanel.ShieldStrengthTextBox.Text = CurrentShipClass.TotalShieldPool.ToString();

            if (CurrentShipClass.ShipShieldDef != null)
            {
                if (CurrentShipClass.ShipShieldDef.shieldGen == CurrentShipClass.ShipShieldDef.shieldPool)
                {
                    m_oOptionsPanel.ShieldRechargeTextBox.Text = "300";
                }
                else
                {
                    float shield = (float)Math.Floor((CurrentShipClass.ShipShieldDef.shieldPool / CurrentShipClass.ShipShieldDef.shieldGen) * 300.0f);
                    m_oOptionsPanel.ShieldRechargeTextBox.Text = shield.ToString();
                }
            }
            else
            {
                m_oOptionsPanel.ShieldRechargeTextBox.Text = "0";
            }

            m_oOptionsPanel.InternalHTKTextBox.Text = CurrentShipClass.TotalHTK.ToString();
        }

        /// <summary>
        /// Crew accomodations panel
        /// </summary>
        private void BuildCrewAccomPanel()
        {
            m_oOptionsPanel.DeploymentTimeTextBox.Text = CurrentShipClass.MaxDeploymentTime.ToString();
            m_oOptionsPanel.TonsPerManTextBox.Text = String.Format("{0:N3}",CurrentShipClass.TonsPerMan);
            m_oOptionsPanel.CapPerHSTextBox.Text = String.Format("{0:N2}",CurrentShipClass.CapPerHS);
            m_oOptionsPanel.AccomHSReqTextBox.Text = String.Format("{0:N4}",CurrentShipClass.AccomHSRequirement);
            m_oOptionsPanel.AccomHSAvailTextBox.Text = String.Format("{0:N1}",CurrentShipClass.AccomHSAvailable);
            m_oOptionsPanel.CrewBerthsTextBox.Text = CurrentShipClass.TotalRequiredCrew.ToString();
            m_oOptionsPanel.SpareBerthsTextBox.Text = CurrentShipClass.SpareCrewQuarters.ToString();
            m_oOptionsPanel.CryoBerthsTextBox.Text = CurrentShipClass.SpareCryoBerths.ToString();
        }

        /// <summary>
        /// Builds the design tab. Really wishing I'd done these as a dictionary originally.
        /// Not implemented: CIWS,ECCM,Turrets, ECM,Cloak, Jump Engines, Maintenance Storage Bays,Hangar,Boat Bay,Troop Bay,Drop Pod,Orbital Hab,Rec Facilities,
        /// Geo Sensors,Grav Sensors, 
        /// </summary>
        private void BuildDesignTab()
        {
            String Entry;
            m_oOptionsPanel.BriefSummaryTextBox.Text = CurrentShipClass.Summary;

            m_oOptionsPanel.ComponentsListBox.Items.Clear();
            if (m_oOptionsPanel.GroupComponentsCheckBox.Checked == true)
            {

                if (CurrentShipClass.ShipBFCDef.Count != 0 || CurrentShipClass.ShipBeamDef.Count != 0 || CurrentShipClass.ShipReactorDef.Count != 0 ||
                    CurrentShipClass.ShipMLaunchDef.Count != 0 || CurrentShipClass.ShipMagazineDef.Count != 0 || CurrentShipClass.ShipMFCDef.Count != 0)
                {
                    Entry = "Weapons and Fire Control:";
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                    for (int loop = 0; loop < CurrentShipClass.ShipBeamDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipBeamCount[loop], CurrentShipClass.ShipBeamDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipMLaunchDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipMLaunchCount[loop], CurrentShipClass.ShipMLaunchDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipReactorDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipReactorCount[loop], CurrentShipClass.ShipReactorDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipMagazineDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipMagazineCount[loop], CurrentShipClass.ShipMagazineDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipBFCDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipBFCCount[loop], CurrentShipClass.ShipBFCDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipMFCDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipMFCCount[loop], CurrentShipClass.ShipMFCDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }
                }

                Entry = "";
                m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                Entry = "Defences:";
                m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                Entry = String.Format("{0}x {1} Armour", CurrentShipClass.ShipArmorDef.size, CurrentShipClass.ShipArmorDef.Name);
                m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                if (CurrentShipClass.ShipShieldDef != null)
                {
                    Entry = String.Format("{0}x {1}", CurrentShipClass.ShipShieldCount, CurrentShipClass.ShipShieldDef.Name);
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                }

                Entry = "";
                m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                if (CurrentShipClass.ShipEngineDef != null)
                {
                    Entry = "Engines:";
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                    Entry = String.Format("{0}x {1}", CurrentShipClass.ShipEngineCount, CurrentShipClass.ShipEngineDef.Name);
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                }

                Entry = "";
                m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                if (CurrentShipClass.ShipCargoDef.Count != 0 || CurrentShipClass.ShipColonyDef.Count != 0 || CurrentShipClass.ShipCHSDef.Count != 0)
                {
                    Entry = "Special Functions:";
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                    for (int loop = 0; loop < CurrentShipClass.ShipCargoDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipCargoCount[loop], CurrentShipClass.ShipCargoDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipColonyDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipColonyCount[loop], CurrentShipClass.ShipColonyDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipCHSDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipCHSCount[loop], CurrentShipClass.ShipCHSDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }
                }

                Entry = "";
                m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                if (CurrentShipClass.ShipASensorDef.Count != 0 || CurrentShipClass.ShipPSensorDef.Count != 0)
                {
                    Entry = "Sensors:";
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                    for (int loop = 0; loop < CurrentShipClass.ShipASensorDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipASensorCount[loop], CurrentShipClass.ShipASensorDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }

                    for (int loop = 0; loop < CurrentShipClass.ShipPSensorDef.Count; loop++)
                    {
                        Entry = String.Format("{0}x {1}", CurrentShipClass.ShipPSensorCount[loop], CurrentShipClass.ShipPSensorDef[loop].Name);
                        m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                    }
                }

                Entry = "";
                m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                Entry = "General:";
                m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);

                for (int loop = 0; loop < CurrentShipClass.CrewQuarters.Count; loop++)
                {
                    Entry = String.Format("{0}x {1}", CurrentShipClass.CrewQuartersCount[loop], CurrentShipClass.CrewQuarters[loop].Name);
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                }

                for (int loop = 0; loop < CurrentShipClass.FuelTanks.Count; loop++)
                {
                    Entry = String.Format("{0}x {1}", CurrentShipClass.FuelTanksCount[loop], CurrentShipClass.FuelTanks[loop].Name);
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                }

                for (int loop = 0; loop < CurrentShipClass.EngineeringBays.Count; loop++)
                {
                    Entry = String.Format("{0}x {1}", CurrentShipClass.EngineeringBaysCount[loop], CurrentShipClass.EngineeringBays[loop].Name);
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                }

                for (int loop = 0; loop < CurrentShipClass.OtherComponents.Count; loop++)
                {
                    Entry = String.Format("{0}x {1}", CurrentShipClass.OtherComponentsCount[loop], CurrentShipClass.OtherComponents[loop].Name);
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                }
            }
            else
            {
                Entry = String.Format("{0}x {1} Armour", CurrentShipClass.ShipArmorDef.size, CurrentShipClass.ShipArmorDef.Name);
                m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                for (int loop = 0; loop < CurrentShipClass.ListOfComponentDefs.Count; loop++)
                {
                    Entry = String.Format("{0}x {1}",CurrentShipClass.ListOfComponentDefsCount[loop], CurrentShipClass.ListOfComponentDefs[loop].Name);
                    m_oOptionsPanel.ComponentsListBox.Items.Add(Entry);
                }
            }
        }

        /// <summary>
        /// Creates the columns for the component data grid.
        /// </summary>
        private void SetupComponentDataGrid()
        {
            m_oOptionsPanel.ComponentDataGrid.Columns.Clear();
            try
            {
                Padding newPadding = new Padding(2, 0, 2, 0);
                AddColumn("Name", newPadding);
                AddColumn("Rating Type", newPadding);
                AddColumn("Rating", newPadding);
                AddColumn("Cost", newPadding);
                AddColumn("Size", newPadding);
                AddColumn("Crew", newPadding);
                AddColumn("Materials (exc Duranium)", newPadding);

                AddColumn("CType", newPadding);
                AddColumn("CIndex", newPadding);

                m_oOptionsPanel.ComponentDataGrid.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

                foreach (DataGridViewColumn Column in m_oOptionsPanel.ComponentDataGrid.Columns)
                {
                    Column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }

                //m_oOptionsPanel.ComponentDataGrid.Columns[m_oOptionsPanel.ComponentDataGrid.Columns.Count - 1].Visible = false;
                //m_oOptionsPanel.ComponentDataGrid.Columns[m_oOptionsPanel.ComponentDataGrid.Columns.Count - 2].Visible = false;


            }
            catch
            {
                logger.Error("Something went wrong Creating Columns for Class Design ComponentGrid screen...");
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
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                col.DefaultCellStyle.Padding = newPadding;
                if (col != null)
                {
                    m_oOptionsPanel.ComponentDataGrid.Columns.Add(col);
                }
            }
        }

        /// <summary>
        /// Listing of all components that can be added and statistics about them.
        /// </summary>
        private void BuildComponentDataGrid()
        {
            int row = 0;
            String Entry = "N/A";
            String Entry2 = "N/A";
            ComponentDefListTN List = _CurrnetFaction.ComponentList;

            if (TotalComponents == 0)
            {

                m_oOptionsPanel.ComponentDataGrid.Rows.Clear();

                try
                {

                    #region Basic Systems
                    using (DataGridViewRow NewRow = new DataGridViewRow())
                    {
                        /// <summary>
                        /// setup row height. note that by default they are 22 pixels in height!
                        /// </summary>
                        NewRow.Height = 18;
                        m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                        DataGridViewCellStyle style = new DataGridViewCellStyle();
                        style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                        m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;

                        m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Basic Systems";
                        CompLocation.Add(row);
                        row++;
                    }

                    for (int loop = 0; loop < List.CrewQuarters.Count; loop++)
                    {
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = List.CrewQuarters[loop].Name;
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = "Life Support";
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = (List.CrewQuarters[loop].size * 50.0f).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = List.CrewQuarters[loop].cost.ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (List.CrewQuarters[loop].size * 50.0f).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = List.CrewQuarters[loop].crew;

                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)List.CrewQuarters[loop].componentType).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = loop;

                            row++;
                            TotalComponents = TotalComponents + 1;
                        }
                    }

                    for (int loop = 0; loop < List.FuelStorage.Count; loop++)
                    {
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = List.FuelStorage[loop].Name;
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = "Litres of Fuel";
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = (List.FuelStorage[loop].size * 50000.0f).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = List.FuelStorage[loop].cost.ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (List.FuelStorage[loop].size * 50.0f).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = List.FuelStorage[loop].crew;

                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)List.FuelStorage[loop].componentType).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = loop;

                            row++;
                            TotalComponents = TotalComponents + 1;
                        }
                    }

                    for (int loop = 0; loop < List.EngineeringSpaces.Count; loop++)
                    {
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = List.EngineeringSpaces[loop].Name;
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = "Failure Rate";
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = List.EngineeringSpaces[loop].size;
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = List.EngineeringSpaces[loop].cost.ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (List.EngineeringSpaces[loop].size * 50.0f).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = List.EngineeringSpaces[loop].crew;

                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)List.EngineeringSpaces[loop].componentType).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = loop;

                            row++;
                            TotalComponents = TotalComponents + 1;
                        }
                    }

                    for (int loop = 0; loop < List.OtherComponents.Count; loop++)
                    {
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = List.OtherComponents[loop].Name;
                            switch (List.OtherComponents[loop].componentType)
                            {
                                case ComponentTypeTN.Bridge:
                                    Entry = "CommandControl";
                                    Entry2 = "1";
                                    break;
                                case ComponentTypeTN.MaintenanceBay:
                                    Entry = "MaintStorage";
                                    Entry2 = "1000";
                                    break;
                                case ComponentTypeTN.OrbitalHabitat:
                                    Entry = "Worker Capacity";
                                    Entry2 = "50000";
                                    break;
                                case ComponentTypeTN.RecFacility:
                                    Entry = "Crew Recreation";
                                    Entry2 = "0";
                                    break;
                                default:
                                    Entry = "UNK Component";
                                    Entry2 = "*********UNK*********";
                                    break;
                            }
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = Entry;
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = Entry2;
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = List.OtherComponents[loop].cost.ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (List.OtherComponents[loop].size * 50.0f).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = List.OtherComponents[loop].crew;

                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)List.OtherComponents[loop].componentType).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = loop;

                            row++;
                            TotalComponents = TotalComponents + 1;
                        }
                    }
                    #endregion

                    #region Engines
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                            DataGridViewCellStyle style = new DataGridViewCellStyle();
                            style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Engines";
                            CompLocation.Add(row);

                            row++;
                        }



                        for (int loop = 0; loop < List.Engines.Count; loop++)
                        {
                            using (DataGridViewRow NewRow = new DataGridViewRow())
                            {
                                /// <summary>
                                /// setup row height. note that by default they are 22 pixels in height!
                                /// </summary>
                                NewRow.Height = 18;
                                m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = List.Engines[loop].Name;
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = "Engine Power";
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = List.Engines[loop].enginePower.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = List.Engines[loop].cost.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (List.Engines[loop].size * 50.0f).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = List.Engines[loop].crew;

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)List.Engines[loop].componentType).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = loop;

                                row++;
                                TotalComponents = TotalComponents + 1;
                            }
                        }
                    #endregion

                    #region Fire Control
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                            DataGridViewCellStyle style = new DataGridViewCellStyle();
                            style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Fire Control";

                            CompLocation.Add(row);

                            row++;
                        }



                        for (int loop = 0; loop < List.BeamFireControlDef.Count; loop++)
                        {
                            using (DataGridViewRow NewRow = new DataGridViewRow())
                            {
                                /// <summary>
                                /// setup row height. note that by default they are 22 pixels in height!
                                /// </summary>
                                NewRow.Height = 18;
                                m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = List.BeamFireControlDef[loop].Name;
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = "50% Acc. Distance";
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = List.BeamFireControlDef[loop].range.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = List.BeamFireControlDef[loop].cost.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (List.BeamFireControlDef[loop].size * 50.0f).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = List.BeamFireControlDef[loop].crew;

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)List.BeamFireControlDef[loop].componentType).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = loop;

                                row++;
                                TotalComponents = TotalComponents + 1;
                            }
                        }

                        for (int loop = 0; loop < List.MissileFireControlDef.Count; loop++)
                        {
                            using (DataGridViewRow NewRow = new DataGridViewRow())
                            {
                                /// <summary>
                                /// setup row height. note that by default they are 22 pixels in height!
                                /// </summary>
                                NewRow.Height = 18;
                                m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = List.MissileFireControlDef[loop].Name;
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = "Range km";


                                if (List.MissileFireControlDef[loop].maxRange >= 100000)
                                {
                                    float RNG = (float)(List.MissileFireControlDef[loop].maxRange) / 100000.0f;
                                    Entry = String.Format("{0:N1}B", RNG);
                                }
                                else if (List.MissileFireControlDef[loop].maxRange >= 100)
                                {
                                    float RNG = (float)(List.MissileFireControlDef[loop].maxRange) / 100.0f;
                                    Entry = String.Format("{0:N1}M", RNG);
                                }
                                else
                                {
                                    float RNG = (float)List.MissileFireControlDef[loop].maxRange;
                                    Entry = String.Format("{0:N1}K", RNG);
                                }

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = Entry;
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = List.MissileFireControlDef[loop].cost.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (List.MissileFireControlDef[loop].size * 50.0f).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = List.MissileFireControlDef[loop].crew;

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)List.MissileFireControlDef[loop].componentType).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = loop;

                                row++;
                                TotalComponents = TotalComponents + 1;
                            }
                        }
                    #endregion

                    #region Energy Weapons / CIWS(not yet implemented) / Turrets(not yet implemented)
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                            DataGridViewCellStyle style = new DataGridViewCellStyle();
                            style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Energy Weapons";

                            CompLocation.Add(row);

                            row++;
                        }

                        for (int loop = 0; loop < List.BeamWeaponDef.Count; loop++)
                        {
                            using (DataGridViewRow NewRow = new DataGridViewRow())
                            {
                                /// <summary>
                                /// setup row height. note that by default they are 22 pixels in height!
                                /// </summary>
                                NewRow.Height = 18;
                                m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                                switch (List.BeamWeaponDef[loop].componentType)
                                {
                                    case ComponentTypeTN.Laser:
                                    case ComponentTypeTN.AdvLaser:
                                    case ComponentTypeTN.Plasma:
                                    case ComponentTypeTN.AdvPlasma:
                                    case ComponentTypeTN.Rail:
                                    case ComponentTypeTN.AdvRail:
                                    case ComponentTypeTN.Particle:
                                    case ComponentTypeTN.AdvParticle:
                                        Entry = "Damage";
                                        Entry2 = List.BeamWeaponDef[loop].damage[0].ToString();
                                        break;

                                    case ComponentTypeTN.Meson:
                                    case ComponentTypeTN.Microwave:
                                        Entry = "Range";
                                        Entry2 = (List.BeamWeaponDef[loop].range / 10000.0f).ToString();
                                        break;

                                    case ComponentTypeTN.Gauss:
                                        Entry = "Rate of Fire";
                                        Entry2 = List.BeamWeaponDef[loop].shotCount.ToString();
                                        break;
                                }

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = List.BeamWeaponDef[loop].Name;
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = Entry;
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = Entry2;
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = List.BeamWeaponDef[loop].cost.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (List.BeamWeaponDef[loop].size * 50.0f).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = List.BeamWeaponDef[loop].crew;

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)List.BeamWeaponDef[loop].componentType).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = loop;

                                row++;
                                TotalComponents = TotalComponents + 1;
                            }
                        }

                        /// <summary>
                        /// CIWS is marked down as a gauss cannon for rating type and rating, but CIWS has 2x guns, so shotcount*2 is their rate of fire.
                        /// </summary>
                    #endregion

                    #region Missile/Torpedo Launchers (Plasma torpedos not yet implemented)
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                            DataGridViewCellStyle style = new DataGridViewCellStyle();
                            style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Missile/Torpedo Launchers";

                            CompLocation.Add(row);

                            row++;
                        }

                        for (int loop = 0; loop < List.MLauncherDef.Count; loop++)
                        {
                            using (DataGridViewRow NewRow = new DataGridViewRow())
                            {
                                /// <summary>
                                /// setup row height. note that by default they are 22 pixels in height!
                                /// </summary>
                                NewRow.Height = 18;
                                m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = List.MLauncherDef[loop].Name;
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = "Max Missile Size";
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = List.MLauncherDef[loop].launchMaxSize.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = List.MLauncherDef[loop].cost.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (List.MLauncherDef[loop].size * 50.0f).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = List.MLauncherDef[loop].crew;

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)List.MLauncherDef[loop].componentType).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = loop;

                                row++;
                                TotalComponents = TotalComponents + 1;
                            }
                        }
                    #endregion

                    #region Magazines
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                            DataGridViewCellStyle style = new DataGridViewCellStyle();
                            style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Magazines";

                            CompLocation.Add(row);

                            row++;
                        }

                        for (int loop = 0; loop < List.MagazineDef.Count; loop++)
                        {
                            using (DataGridViewRow NewRow = new DataGridViewRow())
                            {
                                /// <summary>
                                /// setup row height. note that by default they are 22 pixels in height!
                                /// </summary>
                                NewRow.Height = 18;
                                m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = List.MagazineDef[loop].Name;
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = "Ordnance Storage";
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = List.MagazineDef[loop].capacity.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = List.MagazineDef[loop].cost.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (List.MagazineDef[loop].size * 50.0f).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = List.MagazineDef[loop].crew;

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)List.MagazineDef[loop].componentType).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = loop;

                                row++;
                                TotalComponents = TotalComponents + 1;
                            }
                        }
                    #endregion

                    #region Planetary Combat (Troop bays Not yet implemented)
                    
                    using (DataGridViewRow NewRow = new DataGridViewRow())
                    {
                        /// <summary>
                        /// setup row height. note that by default they are 22 pixels in height!
                        /// </summary>
                        NewRow.Height = 18;
                        m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                        DataGridViewCellStyle style = new DataGridViewCellStyle();
                        style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                        m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                        m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Planetary Combat";
                    
                        CompLocation.Add(row);
                    
                        row++;
                    }
                
                    #endregion

                    #region Power Plants
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                            DataGridViewCellStyle style = new DataGridViewCellStyle();
                            style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Power Plants";

                            CompLocation.Add(row);

                            row++;
                        }

                        for (int loop = 0; loop < List.ReactorDef.Count; loop++)
                        {
                            using (DataGridViewRow NewRow = new DataGridViewRow())
                            {
                                /// <summary>
                                /// setup row height. note that by default they are 22 pixels in height!
                                /// </summary>
                                NewRow.Height = 18;
                                m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = List.ReactorDef[loop].Name;
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = "Power Produced";
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = List.ReactorDef[loop].powerGen.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = List.ReactorDef[loop].cost.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (List.ReactorDef[loop].size * 50.0f).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = List.ReactorDef[loop].crew;

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)List.ReactorDef[loop].componentType).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = loop;

                                row++;
                                TotalComponents = TotalComponents + 1;
                            }
                        }
                    #endregion

                    #region Active Sensors
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                            DataGridViewCellStyle style = new DataGridViewCellStyle();
                            style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Active Sensors";

                            CompLocation.Add(row);

                            row++;
                        }

                        for (int loop = 0; loop < List.ActiveSensorDef.Count; loop++)
                        {
                            using (DataGridViewRow NewRow = new DataGridViewRow())
                            {
                                /// <summary>
                                /// setup row height. note that by default they are 22 pixels in height!
                                /// </summary>
                                NewRow.Height = 18;
                                m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = List.ActiveSensorDef[loop].Name;
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = "Range km";

                                if (List.ActiveSensorDef[loop].maxRange >= 100000)
                                {
                                    float RNG = (float)(List.ActiveSensorDef[loop].maxRange) / 100000.0f;
                                    Entry = String.Format("{0:N1}B", RNG);
                                }
                                else if (List.ActiveSensorDef[loop].maxRange >= 100)
                                {
                                    float RNG = (float)(List.ActiveSensorDef[loop].maxRange) / 100.0f;
                                    Entry = String.Format("{0:N1}M", RNG);
                                }
                                else
                                {
                                    float RNG = (float)List.ActiveSensorDef[loop].maxRange;
                                    Entry = String.Format("{0:N1}K", RNG);
                                }

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = Entry;
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = List.ActiveSensorDef[loop].cost.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (List.ActiveSensorDef[loop].size * 50.0f).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = List.ActiveSensorDef[loop].crew;

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)List.ActiveSensorDef[loop].componentType).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = loop;

                                row++;
                                TotalComponents = TotalComponents + 1;
                            }
                        }
                    #endregion

                    #region Passive Sensors (Geo/Grav not yet implemented)
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                            DataGridViewCellStyle style = new DataGridViewCellStyle();
                            style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Passive Sensors";

                            CompLocation.Add(row);

                            row++;
                        }

                        for (int loop = 0; loop < List.PassiveSensorDef.Count; loop++)
                        {
                            using (DataGridViewRow NewRow = new DataGridViewRow())
                            {
                                /// <summary>
                                /// setup row height. note that by default they are 22 pixels in height!
                                /// </summary>
                                NewRow.Height = 18;
                                m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = List.PassiveSensorDef[loop].Name;
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = "Sensor Strength";
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = List.PassiveSensorDef[loop].rating.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = List.PassiveSensorDef[loop].cost.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (List.PassiveSensorDef[loop].size * 50.0f).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = List.PassiveSensorDef[loop].crew;

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)List.PassiveSensorDef[loop].componentType).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = loop;

                                row++;
                                TotalComponents = TotalComponents + 1;
                            }
                        }
                    #endregion

                    #region Shields / Electronic Warfare (Cloak not implemented,ecm/eccm not implemented)
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                            DataGridViewCellStyle style = new DataGridViewCellStyle();
                            style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Shields / Electronic Warfare";

                            CompLocation.Add(row);

                            row++;
                        }

                        for (int loop = 0; loop < List.ShieldDef.Count; loop++)
                        {
                            using (DataGridViewRow NewRow = new DataGridViewRow())
                            {
                                /// <summary>
                                /// setup row height. note that by default they are 22 pixels in height!
                                /// </summary>
                                NewRow.Height = 18;
                                m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = List.ShieldDef[loop].Name;
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = "Shield Strength";
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = List.ShieldDef[loop].shieldPool.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = List.ShieldDef[loop].cost.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (List.ShieldDef[loop].size * 50.0f).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = List.ShieldDef[loop].crew;

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)List.ShieldDef[loop].componentType).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = loop;

                                row++;
                                TotalComponents = TotalComponents + 1;
                            }
                        }
                    #endregion

                    #region Transportation and Industry (Hangar, Maint Module, Terraformer, Sorium Harvester, Tractor, Salvage, Luxury, construction module not yet implemented)
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                            DataGridViewCellStyle style = new DataGridViewCellStyle();
                            style.Font = new System.Drawing.Font(m_oOptionsPanel.ComponentDataGrid.Font, System.Drawing.FontStyle.Bold);
                            m_oOptionsPanel.ComponentDataGrid.Rows[row].DefaultCellStyle = style;


                            m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = "Transportation and Industry";

                            CompLocation.Add(row);

                            row++;
                        }

                        for (int loop = 0; loop < List.CargoHandleSystemDef.Count; loop++)
                        {
                            using (DataGridViewRow NewRow = new DataGridViewRow())
                            {
                                /// <summary>
                                /// setup row height. note that by default they are 22 pixels in height!
                                /// </summary>
                                NewRow.Height = 18;
                                m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = List.CargoHandleSystemDef[loop].Name;
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = "Cargo Handling Multiplier";
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = List.CargoHandleSystemDef[loop].tractorMultiplier.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = List.CargoHandleSystemDef[loop].cost.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (List.CargoHandleSystemDef[loop].size * 50.0f).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = List.CargoHandleSystemDef[loop].crew;

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)List.CargoHandleSystemDef[loop].componentType).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = loop;

                                row++;
                                TotalComponents = TotalComponents + 1;
                            }
                        }

                        for (int loop = 0; loop < List.CargoHoldDef.Count; loop++)
                        {
                            using (DataGridViewRow NewRow = new DataGridViewRow())
                            {
                                /// <summary>
                                /// setup row height. note that by default they are 22 pixels in height!
                                /// </summary>
                                NewRow.Height = 18;
                                m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = List.CargoHoldDef[loop].Name;
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = "Cargo Capacity";
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = List.CargoHoldDef[loop].cargoCapacity.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = List.CargoHoldDef[loop].cost.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (List.CargoHoldDef[loop].size * 50.0f).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = List.CargoHoldDef[loop].crew;

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)List.CargoHoldDef[loop].componentType).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = loop;

                                row++;
                                TotalComponents = TotalComponents + 1;
                            }
                        }

                        for (int loop = 0; loop < List.ColonyBayDef.Count; loop++)
                        {
                            using (DataGridViewRow NewRow = new DataGridViewRow())
                            {
                                /// <summary>
                                /// setup row height. note that by default they are 22 pixels in height!
                                /// </summary>
                                NewRow.Height = 18;
                                m_oOptionsPanel.ComponentDataGrid.Rows.Add(NewRow);

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Name].Value = List.ColonyBayDef[loop].Name;
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.RatingType].Value = "Colonist Capacity";
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Rating].Value = List.ColonyBayDef[loop].cryoBerths.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Cost].Value = List.ColonyBayDef[loop].cost.ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Size].Value = (List.ColonyBayDef[loop].size * 50.0f).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Crew].Value = List.ColonyBayDef[loop].crew;

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CType].Value = ((int)List.ColonyBayDef[loop].componentType).ToString();
                                m_oOptionsPanel.ComponentDataGrid.Rows[row].Cells[(int)ComponentCell.CIndex].Value = loop;

                                row++;
                                TotalComponents = TotalComponents + 1;
                            }
                        }
                    #endregion

                }
                catch
                {
                    logger.Error("Something went wrong Creating Rows for Class Design ComponentGrid screen...");
                }
            }
            else if( TotalComponents != List.TotalComponents)
            {
                //rowCount = 1;
                //m_oOptionsPanel.ComponentDataGrid.Rows.Insert(int rowIndex, int rowCount);

                #region Basic Component Addition
                /// <summary>
                /// A basic Component was added.
                /// </summary>
                if (CompLocation[(int)ComponentGroup.Engines] != (List.CrewQuarters.Count + List.FuelStorage.Count + List.EngineeringSpaces.Count + List.OtherComponents.Count + 1))
                {
                    int CQCount = 0, FSCount = 0, ESCount = 0, OCCount = 0;
                    int rowLine = 1;

                    /// <summary>
                    /// Count the crew quarters rows:
                    /// </summary>
                    while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == "0")
                    {
                        rowLine++;
                    }

                    CQCount = rowLine - 1;

                    int AddedRows = List.CrewQuarters.Count - CQCount;

                    /// <summary>
                    /// Increment all the component locations past the current one(Engine) by added rows count.
                    /// </summary>
                    for (int loop = (int)ComponentGroup.Engines; loop < (int)ComponentGroup.TypeCount; loop++)
                    {
                        CompLocation[loop] = CompLocation[loop] + AddedRows;
                    }

                    /// <summary>
                    /// insert and fill in the rows where appropriate.
                    /// </summary>
                    for (int loop = CQCount; loop <= (List.CrewQuarters.Count - 1); loop++)
                    {
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Insert(rowLine, NewRow);

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Name].Value = List.CrewQuarters[loop].Name;
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.RatingType].Value = "Life Support";
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Rating].Value = (List.CrewQuarters[loop].size * 50.0f).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Cost].Value = List.CrewQuarters[loop].cost.ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Size].Value = (List.CrewQuarters[loop].size * 50.0f).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Crew].Value = List.CrewQuarters[loop].crew;

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value = ((int)List.CrewQuarters[loop].componentType).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CIndex].Value = loop;

                            TotalComponents = TotalComponents + 1;
                        }
                        rowLine++;
                    }

                    /// <summary>
                    /// Fuel Storage Section.
                    /// </summary>
                    int CQEnd = rowLine;

                    while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == "1")
                    {
                        rowLine++;
                    }

                    FSCount = rowLine - CQEnd;

                    AddedRows = List.FuelStorage.Count - FSCount;

                    /// <summary>
                    /// Increment all the component locations past the current one(Engine) by added rows count.
                    /// </summary>
                    for (int loop = (int)ComponentGroup.Engines; loop < (int)ComponentGroup.TypeCount; loop++)
                    {
                        CompLocation[loop] = CompLocation[loop] + AddedRows;
                    }

                    /// <summary>
                    /// insert and fill in the rows where appropriate.
                    /// </summary>
                    for (int loop = FSCount; loop <= (List.FuelStorage.Count - 1); loop++)
                    {
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Insert(rowLine, NewRow);

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Name].Value = List.FuelStorage[loop].Name;
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.RatingType].Value = "Litres of Fuel";
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Rating].Value = (List.FuelStorage[loop].size * 50000.0f).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Cost].Value = List.FuelStorage[loop].cost.ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Size].Value = (List.FuelStorage[loop].size * 50.0f).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Crew].Value = List.FuelStorage[loop].crew;

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value = ((int)List.FuelStorage[loop].componentType).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CIndex].Value = loop;

                            TotalComponents = TotalComponents + 1;
                        }
                        rowLine++;
                    }

                    /// <summary>
                    /// Engineering spaces Section
                    /// </summary>
                    int FSEnd = rowLine;

                    while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == "2")
                    {
                        rowLine++;
                    }

                    ESCount = rowLine - FSEnd;

                    AddedRows = List.EngineeringSpaces.Count - ESCount;

                    /// <summary>
                    /// Increment all the component locations past the current one(Engine) by added rows count.
                    /// </summary>
                    for (int loop = (int)ComponentGroup.Engines; loop < (int)ComponentGroup.TypeCount; loop++)
                    {
                        CompLocation[loop] = CompLocation[loop] + AddedRows;
                    }

                    /// <summary>
                    /// insert and fill in the rows where appropriate.
                    /// </summary>
                    for (int loop = ESCount; loop <= (List.EngineeringSpaces.Count - 1); loop++)
                    {
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Insert(rowLine, NewRow);

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Name].Value = List.EngineeringSpaces[loop].Name;
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.RatingType].Value = "Failure Rate";
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Rating].Value = List.EngineeringSpaces[loop].size;
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Cost].Value = List.EngineeringSpaces[loop].cost.ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Size].Value = (List.EngineeringSpaces[loop].size * 50.0f).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Crew].Value = List.EngineeringSpaces[loop].crew;

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value = ((int)List.EngineeringSpaces[loop].componentType).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CIndex].Value = loop;

                            TotalComponents = TotalComponents + 1;
                        }
                        rowLine++;
                    }

                    int out1,out2;

                    int.TryParse((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value, out out1);
                    int.TryParse((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value, out out2);

                    while ( out1 >= 3 && out2 <= 8 )
                    {
                        rowLine++;

                        int.TryParse((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value,out out1);
                        int.TryParse((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value,out out2);
                    }

                    OCCount = rowLine - ESCount;
                }
                #endregion

                #region Engine Addition
                /// <summary>
                /// An engine was added to the component list.
                /// </summary>
                if (CompLocation[(int)ComponentGroup.FireControl] != (List.Engines.Count + CompLocation[(int)ComponentGroup.Engines] + 1 ))
                {
                    int rowLine = CompLocation[(int)ComponentGroup.Engines] + 1;
                    int EngineCount = 0;

                    /// <summary>
                    /// Count the rows that are already filled with component type 10 or "Engine"
                    /// </summary>
                    while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == "10")
                    {
                        rowLine++;
                    }
                    EngineCount = (rowLine - CompLocation[(int)ComponentGroup.Engines] - 1);

                    int AddedRows = List.Engines.Count - EngineCount;

                    /// <summary>
                    /// Increment all the component locations past the current one(Engine) by added rows count.
                    /// </summary>
                    for (int loop = (int)ComponentGroup.FireControl; loop < (int)ComponentGroup.TypeCount; loop++)
                    {
                        CompLocation[loop] = CompLocation[loop] + AddedRows;
                    }

                    /// <summary>
                    /// insert and fill in the rows where appropriate.
                    /// </summary>
                    for (int loop = EngineCount; loop <= (List.Engines.Count-1); loop++)
                    {
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Insert(rowLine,NewRow);

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Name].Value = List.Engines[loop].Name;
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.RatingType].Value = "Engine Power";
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Rating].Value = List.Engines[loop].enginePower.ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Cost].Value = List.Engines[loop].cost.ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Size].Value = (List.Engines[loop].size * 50.0f).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Crew].Value = List.Engines[loop].crew;

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value = ((int)List.Engines[loop].componentType).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CIndex].Value = loop;

                            TotalComponents = TotalComponents + 1;
                        }
                        rowLine++;
                    }

                }
                #endregion

                #region Fire Control Addition
                if (CompLocation[(int)ComponentGroup.Beam] != (List.BeamFireControlDef.Count + List.MissileFireControlDef.Count + CompLocation[(int)ComponentGroup.FireControl] + 1))
                {
                    int rowLine = CompLocation[(int)ComponentGroup.FireControl] + 1;
                    int BFCCount = 0;

                    /// <summary>
                    /// Count the rows that are already filled with component type 16 or "BFC"
                    /// </summary>
                    while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == "16")
                    {
                        rowLine++;
                    }
                    BFCCount = (rowLine - CompLocation[(int)ComponentGroup.FireControl] - 1);

                    int AddedRows = List.BeamFireControlDef.Count - BFCCount;

                    /// <summary>
                    /// Increment all the component locations past the current one(FireControl) by added rows count.
                    /// </summary>
                    for (int loop = (int)ComponentGroup.Beam; loop < (int)ComponentGroup.TypeCount; loop++)
                    {
                        CompLocation[loop] = CompLocation[loop] + AddedRows;
                    }

                    /// <summary>
                    /// insert and fill in the rows where appropriate.
                    /// </summary>
                    for (int loop = BFCCount; loop <= (List.BeamFireControlDef.Count - 1); loop++)
                    {
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Insert(rowLine, NewRow);

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Name].Value = List.BeamFireControlDef[loop].Name;
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.RatingType].Value = "50% Acc. Distance";
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Rating].Value = List.BeamFireControlDef[loop].range.ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Cost].Value = List.BeamFireControlDef[loop].cost.ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Size].Value = (List.BeamFireControlDef[loop].size * 50.0f).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Crew].Value = List.BeamFireControlDef[loop].crew;

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value = ((int)List.BeamFireControlDef[loop].componentType).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CIndex].Value = loop;

                            TotalComponents = TotalComponents + 1;
                        }
                        rowLine++;
                    }



                    /// <summary>
                    /// Missile fire control Section.
                    /// </summary>
                    int BFCEnd = rowLine;
                    int MFCCount = 0;

                    /// <summary>
                    /// Count the rows that are already filled with component type 33 or "MFC"
                    /// </summary>
                    while ((string)m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value == "33")
                    {
                        rowLine++;
                    }
                    MFCCount = (rowLine - BFCEnd);

                    AddedRows = List.MissileFireControlDef.Count - MFCCount;

                    /// <summary>
                    /// Increment all the component locations past the current one(FireControl) by added rows count.
                    /// </summary>
                    for (int loop = (int)ComponentGroup.Beam; loop < (int)ComponentGroup.TypeCount; loop++)
                    {
                        CompLocation[loop] = CompLocation[loop] + AddedRows;
                    }

                    /// <summary>
                    /// insert and fill in the rows where appropriate.
                    /// </summary>
                    for (int loop = MFCCount; loop <= (List.MissileFireControlDef.Count - 1); loop++)
                    {
                        using (DataGridViewRow NewRow = new DataGridViewRow())
                        {
                            /// <summary>
                            /// setup row height. note that by default they are 22 pixels in height!
                            /// </summary>
                            NewRow.Height = 18;
                            m_oOptionsPanel.ComponentDataGrid.Rows.Insert(rowLine, NewRow);

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Name].Value = List.MissileFireControlDef[loop].Name;
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.RatingType].Value = "Range km";


                            if (List.MissileFireControlDef[loop].maxRange >= 100000)
                            {
                                float RNG = (float)(List.MissileFireControlDef[loop].maxRange) / 100000.0f;
                                Entry = String.Format("{0:N1}B", RNG);
                            }
                            else if (List.MissileFireControlDef[loop].maxRange >= 100)
                            {
                                float RNG = (float)(List.MissileFireControlDef[loop].maxRange) / 100.0f;
                                Entry = String.Format("{0:N1}M", RNG);
                            }
                            else
                            {
                                float RNG = (float)List.MissileFireControlDef[loop].maxRange;
                                Entry = String.Format("{0:N1}K", RNG);
                            }

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Rating].Value = Entry;
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Cost].Value = List.MissileFireControlDef[loop].cost.ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Size].Value = (List.MissileFireControlDef[loop].size * 50.0f).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Crew].Value = List.MissileFireControlDef[loop].crew;

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.Materials].Value = "Not Yet Implemented";

                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CType].Value = ((int)List.MissileFireControlDef[loop].componentType).ToString();
                            m_oOptionsPanel.ComponentDataGrid.Rows[rowLine].Cells[(int)ComponentCell.CIndex].Value = loop;

                            TotalComponents = TotalComponents + 1;
                        }
                        rowLine++;
                    }

                }
                #endregion
            }
        }
        #endregion
    }
}
