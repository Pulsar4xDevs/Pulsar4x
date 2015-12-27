using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Pulsar4X.UI.ViewModels;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using Pulsar4X.Helpers.GameMath;

#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif

namespace Pulsar4X.UI.Handlers
{
    public class Economics
    {
        /// <summary>
        /// Lifted directly from the taskgroup code
        /// </summary>
        public class BuildListObject
        {
            /// <summary>
            /// type of entity for filtering.
            /// </summary>
            public enum ListEntityType
            {
                Installation,
                Component,
                Missile,
                Fighter,
                PDC_Build,
                PDC_Prefab,
                PDC_Assemble,
                PDC_Refit,
                MaintenanceSupplies,
                Count
            }

            public GameEntity Entity { get; private set; }
            public ListEntityType EntityType { get; private set; }

            public BuildListObject(ListEntityType entityType, GameEntity entity)
            {
                Entity = entity;
                EntityType = entityType;
            }
        }

        /// <summary>
        /// Economics Logger:
        /// </summary>
#if LOG4NET_ENABLED
        public static readonly ILog logger = LogManager.GetLogger(typeof(Economics));
#endif

        /// <summary>
        /// Currently selected faction/empire
        /// </summary>
        private Faction m_oCurrnetFaction;
        public Faction CurrentFaction
        {
            get { return m_oCurrnetFaction; }
            set
            {
                if (value != m_oCurrnetFaction)
                {
                    m_oCurrnetFaction = value;
                    if (m_oCurrnetFaction.Populations.Count != 0)
                        m_oCurrnetPopulation = m_oCurrnetFaction.Populations[0];
                    RefreshPanels();
                }
            }
        }

        /// <summary>
        /// Which planetary population is selected.
        /// </summary>
        private Population m_oCurrnetPopulation;
        public Population CurrentPopulation
        {
            get { return m_oCurrnetPopulation; }
            set
            {
                if (value != m_oCurrnetPopulation)
                {
                    m_oCurrnetPopulation = value;

                    if (m_oCurrnetPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].Number >= 1.0f)
                    {
                        CurrentSYInfo = m_oCurrnetPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].SYInfo[0];
                    }
                    else if (m_oCurrnetPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].Number >= 1.0f)
                    {
                        CurrentSYInfo = m_oCurrnetPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].SYInfo[0];
                    }
                    else
                    {
                        CurrentSYInfo = null;
                    }

                    RefreshPanels();
                }
            }
        }

        /// <summary>
        /// This dictionary stores the populations currently in the tree view, and the strings they are keyed as.
        /// </summary>
        private Dictionary<String, Population> TreeViewDictionary { get; set; }

        /// <summary>
        /// Panel that contains the currently selected population summary. The summary panel is going to hold all the various panels now.
        /// </summary>
        Panels.Eco_Summary m_oSummaryPanel;

        /// <summary>
        /// For shipyard activity changes when there is a current activity.
        /// </summary>
        Panels.ConfirmActivity m_oConfirmActivityPanel;

        public EconomicsViewModel VM { get; set; }

        /// <summary>
        /// And here is how I'll be passing the DateTimeModifier correctly.
        /// </summary>
        public Pulsar4X.UI.Forms.MainForm MainFormReference { get; set; }

        /// <summary>
        /// When time is advanced things can move, so I need this to update the system map display if it is currently in use.
        /// </summary>
        public Pulsar4X.UI.Handlers.SystemMap SystemMapReference { get; set; }

        /// <summary>
        /// If the row goes beyond this something needs to be done.
        /// </summary>
        private int BuildTabMaxRows = 50;

        /// <summary>
        /// This is the max row count for the construction build queue data grid.
        /// </summary>
        private int ConstructionTabMaxRows = 50;

        /// <summary>
        /// Dictionary of buildings and their GUID
        /// </summary>
        private Dictionary<Guid, BuildListObject> BuildLocationDict { get; set; }

        /// <summary>
        /// GUID to displayed string dictionary
        /// </summary>
        private Dictionary<Guid, string> BuildLocationDisplayDict { get; set; }

        /// <summary>
        /// Currently Selected Shipyard
        /// </summary>
        private Installation.ShipyardInformation CurrentSYInfo { get; set; }

        /// <summary>
        /// What shipclasses can the current SY retool to?
        /// </summary>
        private BindingList<ShipClassTN> PotentialRetoolTargets { get; set; }

        /// <summary>
        /// Ships in orbit that are in need of repairs.
        /// </summary>
        public BindingList<ShipTN> DamagedShipList { get; set; }

        /// <summary>
        /// List of classes that can be built at this shipyard.
        /// </summary>
        public BindingList<ShipClassTN> EligibleClassList { get; set; }

        /// <summary>
        /// List of classes that can be the target of a repair/refit/scrap operation.
        /// </summary>
        public BindingList<ShipClassTN> ClassesInOrbit { get; set; }

        /// <summary>
        /// List of each ship assigned with a shipclass in orbit to be a potential target for a repair/refit/scrap operation. 
        /// </summary>
        public BindingList<ShipTN> ShipsOfClassInOrbit { get; set; }


        public Economics()
        {
            //Create the summary panel.
            m_oSummaryPanel = new Panels.Eco_Summary();
            m_oConfirmActivityPanel = new Panels.ConfirmActivity();

            // Create Viewmodel:
            VM = new EconomicsViewModel();

            /// <summary>
            /// Create the tree view dictionary obviously.
            /// </summary>
            TreeViewDictionary = new Dictionary<string, Population>();

            /// <summary>
            /// Create the dictionary for the list of construction orders. lifted straight from the TG code.
            /// </summary>
            BuildLocationDict = new Dictionary<Guid, BuildListObject>();
            BuildLocationDisplayDict = new Dictionary<Guid, string>();

            // create Bindings:
            m_oSummaryPanel.FactionComboBox.Bind(c => c.DataSource, VM, d => d.Factions);
            m_oSummaryPanel.FactionComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentFaction, DataSourceUpdateMode.OnPropertyChanged);
            m_oSummaryPanel.FactionComboBox.DisplayMember = "Name";
            VM.FactionChanged += (s, args) => CurrentFaction = VM.CurrentFaction;
            CurrentFaction = VM.CurrentFaction;
            m_oSummaryPanel.FactionComboBox.SelectedIndexChanged += (s, args) => m_oSummaryPanel.FactionComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oSummaryPanel.FactionComboBox.SelectedIndexChanged += new EventHandler(FactionComboBox_SelectedIndexChanged);

            VM.PopulationChanged += (s, args) => CurrentPopulation = VM.CurrentPopulation;
            CurrentPopulation = VM.CurrentPopulation;

            /// <summary>
            /// Checkboxes:
            /// </summary>
            m_oSummaryPanel.GroupByFunctionCheckBox.CheckedChanged += new EventHandler(GroupByFunctionCheckBox_CheckedChanged);
            m_oSummaryPanel.HideCMCCheckBox.CheckedChanged += new EventHandler(HideCMCCheckBox_CheckedChanged);

            /// <summary>
            /// Tree view
            /// </summary>
            m_oSummaryPanel.PopulationTreeView.AfterSelect += new TreeViewEventHandler(PopulationTreeView_Input);


            /// <summary>
            /// Time Advancement Buttons:
            /// </summary>
            m_oSummaryPanel.FiveSecondsButton.Click += new EventHandler(FiveSecondsButton_Click);
            m_oSummaryPanel.ThirtySecondsButton.Click += new EventHandler(ThirtySecondsButton_Click);
            m_oSummaryPanel.FiveMinutesButton.Click += new EventHandler(FiveMinutesButton_Click);
            m_oSummaryPanel.TwentyMinutesButton.Click += new EventHandler(TwentyMinutesButton_Click);
            m_oSummaryPanel.OneHourButton.Click += new EventHandler(OneHourButton_Click);
            m_oSummaryPanel.ThreeHoursButton.Click += new EventHandler(ThreeHoursButton_Click);
            m_oSummaryPanel.EightHoursButton.Click += new EventHandler(EightHoursButton_Click);
            m_oSummaryPanel.OneDayButton.Click += new EventHandler(OneDayButton_Click);
            m_oSummaryPanel.FiveDaysButton.Click += new EventHandler(FiveDaysButton_Click);
            m_oSummaryPanel.ThirtyDaysButton.Click += new EventHandler(ThirtyDaysButton_Click);

            // Setup Summary Data Grid:
            m_oSummaryPanel.SummaryDataGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            m_oSummaryPanel.SummaryDataGrid.RowHeadersVisible = false;
            m_oSummaryPanel.SummaryDataGrid.AutoGenerateColumns = false;
            m_oSummaryPanel.BuildDataGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            m_oSummaryPanel.BuildDataGrid.RowHeadersVisible = false;
            m_oSummaryPanel.BuildDataGrid.AutoGenerateColumns = false;
            m_oSummaryPanel.ConstructionDataGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            m_oSummaryPanel.ConstructionDataGrid.RowHeadersVisible = false;
            m_oSummaryPanel.ConstructionDataGrid.AutoGenerateColumns = false;
            m_oSummaryPanel.ConstructionDataGrid.ColumnHeadersHeight = 34;
            SetupSummaryDataGrid();
            RefreshSummaryCells();

            #region Industrial Tab
            m_oSummaryPanel.StockpileButton.Click += new EventHandler(StockpileButton_Click);
            StockpileButton_Click(null, null);

            m_oSummaryPanel.InstallationTypeComboBox.SelectedIndexChanged += new EventHandler(InstallationTypeComboBox_SelectedIndexChanged);
            BuildConstructionComboBox();

            m_oSummaryPanel.BuildDataGrid.SelectionChanged += new EventHandler(BuildDataGrid_SelectionChanged);

            m_oSummaryPanel.ConstructionDataGrid.SelectionChanged += new EventHandler(ConstructionDataGrid_SelectionChanged);

            m_oSummaryPanel.CreateBuildProjButton.Click += new EventHandler(CreateBuildProjButton_Click);
            m_oSummaryPanel.ModifyBuildProjButton.Click += new EventHandler(ModifyBuildProjButton_Click);
            m_oSummaryPanel.CancelBuildProjButton.Click += new EventHandler(CancelBuildProjButton_Click);
            m_oSummaryPanel.PauseBuildProjButton.Click += new EventHandler(PauseBuildProjButton_Click);
            m_oSummaryPanel.StartRefiningButton.Click += new EventHandler(StartRefiningButton_Click);
            m_oSummaryPanel.StopRefiningButton.Click += new EventHandler(StopRefiningButton_Click);
            #endregion

            #region Mining Tab
            m_oSummaryPanel.MiningDataGrid.RowHeadersVisible = false;
            m_oSummaryPanel.MiningDataGrid.ColumnHeadersHeight = 34;
            m_oSummaryPanel.MaintenanceDataGrid.RowHeadersVisible = false;
            SetupMiningTab();
            #endregion

            #region shipyard Tab
            if (m_oCurrnetPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].Number >= 1.0f)
            {
                CurrentSYInfo = m_oCurrnetPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].SYInfo[0];
            }
            else if (m_oCurrnetPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].Number >= 1.0f)
            {
                CurrentSYInfo = m_oCurrnetPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].SYInfo[0];
            }

            PotentialRetoolTargets = new BindingList<ShipClassTN>();
            if (CurrentSYInfo != null)
            {
                foreach (ShipClassTN ShipClass in CurrentFaction.ShipDesigns)
                {
                    if (ShipClass.SizeTons <= CurrentSYInfo.Tonnage)
                    {
                        PotentialRetoolTargets.Add(ShipClass);
                    }
                }
            }

            m_oSummaryPanel.SYCTaskTypeComboBox.SelectedIndexChanged += new EventHandler(SYCTaskTypeComboBox_SelectedIndexChanged);
            m_oSummaryPanel.SYTaskTypeComboBox.SelectedIndexChanged += new EventHandler(SYTaskTypeComboBox_SelectedIndexChanged);
            m_oSummaryPanel.SetActivityButton.Click += new EventHandler(SetActivityButton_Click);
            m_oSummaryPanel.ShipyardDataGrid.SelectionChanged += new EventHandler(ShipyardDataGrid_SelectionChanged);
            m_oSummaryPanel.ExpandCapUntilXTextBox.TextChanged += new EventHandler(ExpandCapUntilXTextBox_TextChanged);
            m_oSummaryPanel.NewShipClassComboBox.SelectedIndexChanged += new EventHandler(NewShipClassComboBox_SelectedIndexChanged);
            m_oSummaryPanel.RepairRefitScrapClassComboBox.SelectedIndexChanged += new EventHandler(RepairRefitScrapClassComboBox_SelectedIndexChanged);
            m_oSummaryPanel.AddTaskButton.Click += new EventHandler(AddTaskButton_Click);

            m_oSummaryPanel.ShipyardDataGrid.RowHeadersVisible = false;
            m_oSummaryPanel.ShipyardDataGrid.ColumnHeadersHeight = 34;
            m_oSummaryPanel.ShipyardTaskDataGrid.RowHeadersVisible = false;
            Eco_ShipyardTabHandler.BuildShipyardTab(m_oSummaryPanel,m_oCurrnetFaction,m_oCurrnetPopulation,PotentialRetoolTargets);

            m_oConfirmActivityPanel.YesButton.Click += new EventHandler(ConfirmYesButton_Click);
            m_oConfirmActivityPanel.NoButton.Click += new EventHandler(ConfirmNoButton_Click);

            if(CurrentSYInfo != null)
            {
                String Entry = String.Format("Shipyard {0} already has a task in progress. Are you sure you want to cancel the existing activity?",CurrentSYInfo.Name);
                m_oConfirmActivityPanel.ShipyardNamePromptLabel.Text = Entry;
            }

            ShipsOfClassInOrbit = new BindingList<ShipTN>();
            EligibleClassList = new BindingList<ShipClassTN>();
            DamagedShipList = new BindingList<ShipTN>();
            ClassesInOrbit = new BindingList<ShipClassTN>();
            #endregion

            #region Shipyard Tasks Tab
            m_oSummaryPanel.SYAPauseTaskButton.Click += new EventHandler(SYAPauseTaskButton_Click);
            m_oSummaryPanel.SYALowerPriorityButton.Click += new EventHandler(SYALowerPriorityButton_Click);
            m_oSummaryPanel.SYARaisePriorityButton.Click += new EventHandler(SYARaisePriorityButton_Click);
            #endregion

            #region Terraforming Tab
            m_oSummaryPanel.TerraformingSaveAtmButton.Click += new EventHandler(TerraformingSaveAtmButton_Click);

            foreach (WeightedValue<AtmosphericGas> Gas in AtmosphericGas.AtmosphericGases)
            {
                m_oSummaryPanel.TerraformingGasComboBox.Items.Add(Gas.Value.Name);
            }
            m_oSummaryPanel.TerraformingGasComboBox.SelectedIndex = 0;
            #endregion

            // Setup Pop Tree view. I do not know if I can bind this one, so I'll wind up doing it by hand.
            RefreshPanels();

            // setup Event handlers:

        }

        #region EventHandlers

        /// <summary>
        /// Handle Faction Changes here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FactionComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshPanels();
        }

        /// <summary>
        /// if a new category of items to build is selected update that.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstallationTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshIndustryTab();
        }

        /// <summary>
        /// Handle Group by function checkbox check changed. this changes the treeview to be sorted or unsorted according to functionality
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GroupByFunctionCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            BuildTreeView();
        }

        /// <summary>
        /// Hides the civilian mining complexes from the tree view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HideCMCCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            BuildTreeView();
        }

        /// <summary>
        /// Tree view input handling
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PopulationTreeView_Input(object sender, EventArgs e)
        {
            if (m_oSummaryPanel.PopulationTreeView.SelectedNode != null)
            {
                if (TreeViewDictionary.ContainsKey(m_oSummaryPanel.PopulationTreeView.SelectedNode.Name) == true)
                {
                    m_oCurrnetPopulation = TreeViewDictionary[m_oSummaryPanel.PopulationTreeView.SelectedNode.Name];

                    if (m_oCurrnetPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].Number >= 1.0f)
                    {
                        CurrentSYInfo = m_oCurrnetPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].SYInfo[0];
                    }
                    else if (m_oCurrnetPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].Number >= 1.0f)
                    {
                        CurrentSYInfo = m_oCurrnetPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].SYInfo[0];
                    }
                    else
                    {
                        CurrentSYInfo = null;
                    }

                    RefreshPanels(true);
                }
            }

        }

        /// <summary>
        /// If a Shipyard Complex tasktype is swapped, the UI needs to change to reflect this.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SYCTaskTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_oSummaryPanel.SYCTaskTypeComboBox.SelectedIndex != -1)
            {
                if ((Constants.ShipyardInfo.ShipyardActivity)m_oSummaryPanel.SYCTaskTypeComboBox.SelectedIndex == Constants.ShipyardInfo.ShipyardActivity.Retool)
                {
                    m_oSummaryPanel.SYCShipClassLabel.Text = "Ship Class";
                    m_oSummaryPanel.SYCShipClassLabel.Visible = true;
                    m_oSummaryPanel.NewShipClassComboBox.Visible = true;
                    m_oSummaryPanel.ExpandCapUntilXTextBox.Visible = false;
                }
                else if ((Constants.ShipyardInfo.ShipyardActivity)m_oSummaryPanel.SYCTaskTypeComboBox.SelectedIndex == Constants.ShipyardInfo.ShipyardActivity.CapExpansionUntilX)
                {
                    m_oSummaryPanel.SYCShipClassLabel.Text = "Cap Limit";
                    m_oSummaryPanel.SYCShipClassLabel.Visible = true;
                    m_oSummaryPanel.NewShipClassComboBox.Visible = false;
                    m_oSummaryPanel.ExpandCapUntilXTextBox.Visible = true;
                }
                else
                {
                    m_oSummaryPanel.SYCShipClassLabel.Text = "Ship Class";
                    m_oSummaryPanel.SYCShipClassLabel.Visible = false;
                    m_oSummaryPanel.NewShipClassComboBox.Visible = false;
                    m_oSummaryPanel.ExpandCapUntilXTextBox.Visible = false;
                }

                Eco_ShipyardTabHandler.BuildSYCRequiredMinerals(m_oSummaryPanel, CurrentFaction, CurrentPopulation, CurrentSYInfo, PotentialRetoolTargets);
            }
        }

        /// <summary>
        /// If the shipyard task type is changed the UI will need to change to reflect this.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SYTaskTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_oSummaryPanel.SYTaskTypeComboBox.SelectedIndex != -1)
            {
                switch ((Constants.ShipyardInfo.Task)m_oSummaryPanel.SYTaskTypeComboBox.SelectedIndex)
                {
                    case Constants.ShipyardInfo.Task.Construction:
                        m_oSummaryPanel.NewClassLabel.Visible = true;
                        m_oSummaryPanel.SYNewClassComboBox.Visible = true;
                        m_oSummaryPanel.SYShipNameTextBox.Visible = true;
                        m_oSummaryPanel.RepairRefitScrapShipComboBox.Visible = false;
                        m_oSummaryPanel.TaskGroupLabel.Visible = true;
                        m_oSummaryPanel.SYTaskGroupComboBox.Visible = true;
                        m_oSummaryPanel.RepairRefitScrapLabel.Visible = false;
                        m_oSummaryPanel.RepairRefitScrapClassComboBox.Visible = false;
                        break;
                    case Constants.ShipyardInfo.Task.Repair:
                        m_oSummaryPanel.NewClassLabel.Visible = false;
                        m_oSummaryPanel.SYNewClassComboBox.Visible = false;
                        m_oSummaryPanel.SYShipNameTextBox.Visible = false;
                        m_oSummaryPanel.RepairRefitScrapShipComboBox.Visible = true;
                        m_oSummaryPanel.TaskGroupLabel.Visible = false;
                        m_oSummaryPanel.SYTaskGroupComboBox.Visible = false;
                        m_oSummaryPanel.RepairRefitScrapLabel.Visible = true;
                        m_oSummaryPanel.RepairRefitScrapClassComboBox.Visible = false;
                        
                        break;
                    case Constants.ShipyardInfo.Task.Refit:
                        m_oSummaryPanel.NewClassLabel.Visible = true;
                        m_oSummaryPanel.SYNewClassComboBox.Visible = true;
                        m_oSummaryPanel.SYShipNameTextBox.Visible = false;
                        m_oSummaryPanel.RepairRefitScrapShipComboBox.Visible = true;
                        m_oSummaryPanel.TaskGroupLabel.Visible = false;
                        m_oSummaryPanel.SYTaskGroupComboBox.Visible = false;
                        m_oSummaryPanel.RepairRefitScrapLabel.Visible = true;
                        m_oSummaryPanel.RepairRefitScrapClassComboBox.Visible = true;
                        break;
                    case Constants.ShipyardInfo.Task.Scrap:
                        m_oSummaryPanel.NewClassLabel.Visible = false;
                        m_oSummaryPanel.SYNewClassComboBox.Visible = false;
                        m_oSummaryPanel.SYShipNameTextBox.Visible = false;
                        m_oSummaryPanel.RepairRefitScrapShipComboBox.Visible = true;
                        m_oSummaryPanel.TaskGroupLabel.Visible = false;
                        m_oSummaryPanel.SYTaskGroupComboBox.Visible = false;
                        m_oSummaryPanel.RepairRefitScrapLabel.Visible = true;
                        m_oSummaryPanel.RepairRefitScrapClassComboBox.Visible = true;
                        break;
                }

                /// <summary>
                /// So. I want the eco_SY tab handler to be able to populate these lists as needed.
                /// So. they have to be refs.
                /// But they are properties.
                /// So kludge.
                /// </summary>
                BindingList<ShipClassTN> ECL = EligibleClassList;
                BindingList<ShipClassTN> CIO = ClassesInOrbit;
                BindingList<ShipTN> DSL = DamagedShipList;
                BindingList<ShipTN> SCO = ShipsOfClassInOrbit;
                Eco_ShipyardTabHandler.RefreshSYTaskGroupBox(m_oSummaryPanel, CurrentFaction, CurrentPopulation, CurrentSYInfo, ref ECL, ref DSL,
                                                          ref CIO, ref SCO);
                EligibleClassList = ECL;
                ClassesInOrbit = CIO;
                DamagedShipList = DSL;
                ShipsOfClassInOrbit = SCO;
            }
        }

        /// <summary>
        /// Update the RRS ship combo box based on the change in RRS class combobox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RepairRefitScrapClassComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            /// <summary>
            /// Ayep.
            /// </summary>
            BindingList<ShipTN> SCO = ShipsOfClassInOrbit;
            Eco_ShipyardTabHandler.RefreshRRSShipComboBox(m_oSummaryPanel, CurrentFaction, CurrentPopulation, ClassesInOrbit, ref SCO);
            ShipsOfClassInOrbit = SCO;
        }

        /// <summary>
        /// Set the selected activity for the currently selected shipyard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetActivityButton_Click(object sender, EventArgs e)
        {
            if (m_oSummaryPanel.SYCTaskTypeComboBox.SelectedIndex != -1)
            {
                if (CurrentSYInfo.CurrentActivity.Activity == Constants.ShipyardInfo.ShipyardActivity.NoActivity)
                {
                    if ((Constants.ShipyardInfo.ShipyardActivity)m_oSummaryPanel.SYCTaskTypeComboBox.SelectedIndex == Constants.ShipyardInfo.ShipyardActivity.Retool)
                    {
                        if (PotentialRetoolTargets.Count != 0 && m_oSummaryPanel.NewShipClassComboBox.SelectedIndex != -1)
                        {             
                            ShipClassTN RetoolTarget = PotentialRetoolTargets[m_oSummaryPanel.NewShipClassComboBox.SelectedIndex];
                            CurrentSYInfo.SetShipyardActivity(CurrentFaction, Constants.ShipyardInfo.ShipyardActivity.Retool, RetoolTarget);    
                        }
                    }
                    else if ((Constants.ShipyardInfo.ShipyardActivity)m_oSummaryPanel.SYCTaskTypeComboBox.SelectedIndex == Constants.ShipyardInfo.ShipyardActivity.CapExpansionUntilX)
                    {
                        int NewCapLimit;
                        bool r = Int32.TryParse(m_oSummaryPanel.ExpandCapUntilXTextBox.Text, out NewCapLimit);
                        if (r == true && NewCapLimit > CurrentSYInfo.Tonnage)
                        {
                            CurrentSYInfo.SetShipyardActivity(CurrentFaction, (Constants.ShipyardInfo.ShipyardActivity)m_oSummaryPanel.SYCTaskTypeComboBox.SelectedIndex, null, NewCapLimit);
                        }
                    }
                    else
                    {
                        CurrentSYInfo.SetShipyardActivity(CurrentFaction, (Constants.ShipyardInfo.ShipyardActivity)m_oSummaryPanel.SYCTaskTypeComboBox.SelectedIndex);
                    }
                }
                else
                {
                    //pop up prompt about overwriting current activity
                    m_oConfirmActivityPanel.ShowDialog();

                    if (m_oConfirmActivityPanel.UserEntry == Panels.ConfirmActivity.UserSelection.Yes)
                    {
                        if ((Constants.ShipyardInfo.ShipyardActivity)m_oSummaryPanel.SYCTaskTypeComboBox.SelectedIndex == Constants.ShipyardInfo.ShipyardActivity.Retool)
                        {
                            if (PotentialRetoolTargets.Count != 0 && m_oSummaryPanel.NewShipClassComboBox.SelectedIndex != -1)
                            {
                                ShipClassTN RetoolTarget = PotentialRetoolTargets[m_oSummaryPanel.NewShipClassComboBox.SelectedIndex];
                                CurrentSYInfo.SetShipyardActivity(CurrentFaction, Constants.ShipyardInfo.ShipyardActivity.Retool, RetoolTarget);
                            }
                        }
                        else if ((Constants.ShipyardInfo.ShipyardActivity)m_oSummaryPanel.SYCTaskTypeComboBox.SelectedIndex == Constants.ShipyardInfo.ShipyardActivity.CapExpansionUntilX)
                        {
                            int NewCapLimit;
                            bool r = Int32.TryParse(m_oSummaryPanel.ExpandCapUntilXTextBox.Text, out NewCapLimit);
                            if (r == true && NewCapLimit > CurrentSYInfo.Tonnage)
                            {
                                CurrentSYInfo.SetShipyardActivity(CurrentFaction, (Constants.ShipyardInfo.ShipyardActivity)m_oSummaryPanel.SYCTaskTypeComboBox.SelectedIndex, null, NewCapLimit);
                            }
                        }
                        else
                        {
                            CurrentSYInfo.SetShipyardActivity(CurrentFaction, (Constants.ShipyardInfo.ShipyardActivity)m_oSummaryPanel.SYCTaskTypeComboBox.SelectedIndex);
                        }
                    }
                }


                /// <summary>
                /// So. I want the eco_SY tab handler to be able to populate these lists as needed.
                /// So. they have to be refs.
                /// But they are properties.
                /// So kludge.
                /// </summary>
                BindingList<ShipClassTN> ECL = EligibleClassList;
                BindingList<ShipClassTN> CIO = ClassesInOrbit;
                BindingList<ShipTN> DSL = DamagedShipList;
                BindingList<ShipTN> SCO = ShipsOfClassInOrbit;
                Eco_ShipyardTabHandler.RefreshShipyardTab(m_oSummaryPanel, CurrentFaction, CurrentPopulation, CurrentSYInfo, PotentialRetoolTargets, ref ECL, ref DSL,
                                                          ref CIO, ref SCO);
                EligibleClassList = ECL;
                ClassesInOrbit = CIO;
                DamagedShipList = DSL;
                ShipsOfClassInOrbit = SCO;
            }
        }

        /// <summary>
        /// Yes change the activity.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfirmYesButton_Click(object sender, EventArgs e)
        {
            m_oConfirmActivityPanel.UserEntry = Panels.ConfirmActivity.UserSelection.Yes;
            m_oConfirmActivityPanel.Hide();
        }

        /// <summary>
        /// Don't change activity.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfirmNoButton_Click(object sender, EventArgs e)
        {
            m_oConfirmActivityPanel.UserEntry = Panels.ConfirmActivity.UserSelection.No;
            m_oConfirmActivityPanel.Hide();
        }

        /// <summary>
        /// What is the currently selected shipyard?
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShipyardDataGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (m_oSummaryPanel.ShipyardDataGrid.CurrentCell != null)
            {
                if (m_oSummaryPanel.ShipyardDataGrid.CurrentCell.RowIndex != -1)
                {
                    int index = m_oSummaryPanel.ShipyardDataGrid.CurrentCell.RowIndex;

                    if (index > (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].Number) +
                               (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].Number))
                        return;


                    if(index < (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].Number))
                    {
                        CurrentSYInfo = CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].SYInfo[index];
                    }
                    else
                    {
                        index = index - (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].Number);
                        CurrentSYInfo = CurrentPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].SYInfo[index];
                    }

                    PotentialRetoolTargets.Clear();
                    foreach (ShipClassTN ShipClass in CurrentFaction.ShipDesigns)
                    {
                        if (ShipClass.SizeTons <= CurrentSYInfo.Tonnage)
                        {
                            PotentialRetoolTargets.Add(ShipClass);
                        }
                    }

                    if (CurrentSYInfo != null)
                    {
                        String Entry = String.Format("Shipyard Complex Activity({0})",CurrentSYInfo.Name);
                        m_oSummaryPanel.ShipyardTaskGroupBox.Text = Entry;

                        Entry = String.Format("Create Task({0})", CurrentSYInfo.Name);
                        m_oSummaryPanel.ShipyardCreateTaskGroupBox.Text = Entry;
                    }

                    /// <summary>
                    /// So. I want the eco_SY tab handler to be able to populate these lists as needed.
                    /// So. they have to be refs.
                    /// But they are properties.
                    /// So kludge.
                    /// </summary>
                    BindingList<ShipClassTN> ECL = EligibleClassList;
                    BindingList<ShipClassTN> CIO = ClassesInOrbit;
                    BindingList<ShipTN> DSL = DamagedShipList;
                    BindingList<ShipTN> SCO = ShipsOfClassInOrbit;
                    Eco_ShipyardTabHandler.RefreshShipyardTab(m_oSummaryPanel, CurrentFaction, CurrentPopulation, CurrentSYInfo, PotentialRetoolTargets, ref ECL, ref DSL,
                                                              ref CIO, ref SCO);
                    EligibleClassList = ECL;
                    ClassesInOrbit = CIO;
                    DamagedShipList = DSL;
                    ShipsOfClassInOrbit = SCO;
                }
            }
        }

        /// <summary>
        /// Make sure the cost of capacity expansion can be updated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExpandCapUntilXTextBox_TextChanged(object sender, EventArgs e)
        {
            Constants.ShipyardInfo.ShipyardActivity Activity = (Constants.ShipyardInfo.ShipyardActivity)m_oSummaryPanel.SYCTaskTypeComboBox.SelectedIndex;
            if (Activity == Constants.ShipyardInfo.ShipyardActivity.CapExpansionUntilX)
            {
                /// <summary>
                /// So. I want the eco_SY tab handler to be able to populate these lists as needed.
                /// So. they have to be refs.
                /// But they are properties.
                /// So kludge.
                /// </summary>
                BindingList<ShipClassTN> ECL = EligibleClassList;
                BindingList<ShipClassTN> CIO = ClassesInOrbit;
                BindingList<ShipTN> DSL = DamagedShipList;
                BindingList<ShipTN> SCO = ShipsOfClassInOrbit;
                Eco_ShipyardTabHandler.RefreshShipyardTab(m_oSummaryPanel, CurrentFaction, CurrentPopulation, CurrentSYInfo, PotentialRetoolTargets, ref ECL, ref DSL,
                                                          ref CIO, ref SCO);
                EligibleClassList = ECL;
                ClassesInOrbit = CIO;
                DamagedShipList = DSL;
                ShipsOfClassInOrbit = SCO;
            }
        }

        /// <summary>
        /// Cost to retool needs to change to reflect the newly selected shipclass.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewShipClassComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Eco_ShipyardTabHandler.BuildSYCRequiredMinerals(m_oSummaryPanel, CurrentFaction, CurrentPopulation, CurrentSYInfo, PotentialRetoolTargets);
        }

        /// <summary>
        /// Build, repair, refit, or scrap a ship at the selected shipyard. This will put the specified ship's taskgroup into the shipyard, which should prevent it from carrying out orders.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddTaskButton_Click(object sender, EventArgs e)
        {
            /// <summary>
            /// This index should never ever be -1.
            /// </summary>
            if (m_oSummaryPanel.SYTaskTypeComboBox.SelectedIndex != -1 && m_oSummaryPanel.SYTaskGroupComboBox.SelectedIndex != -1 && CurrentSYInfo != null && CurrentPopulation != null && 
                CurrentFaction != null && (m_oSummaryPanel.SYNewClassComboBox.SelectedIndex != -1 || m_oSummaryPanel.RepairRefitScrapShipComboBox.SelectedIndex != -1))
            {
                m_oSummaryPanel.ShipRequiredMaterialsListBox.Items.Clear();

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
                        TGIndex++;
                        if (TGIndex == m_oSummaryPanel.SYTaskGroupComboBox.SelectedIndex)
                        {
                            TargetTG = CurrentTaskGroup;
                            break;
                        }  
                    }
                }

                /// <summary>
                /// Ok, if there is no TG then one needs to be created here.
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

                int BaseBuildRate = CurrentSYInfo.CalcShipBuildRate(CurrentFaction, CurrentPopulation);

                if ((int)SYITask != -1)
                {
                    switch ((Constants.ShipyardInfo.Task)SYITask)
                    {
                        case Constants.ShipyardInfo.Task.Construction:
                            int newShipIndex = m_oSummaryPanel.SYNewClassComboBox.SelectedIndex;
                            if (newShipIndex != -1)
                            {
                                ConstructRefit = EligibleClassList[newShipIndex];
                            }
                            break;
                        case Constants.ShipyardInfo.Task.Repair:
                            int CurrentShipIndex = m_oSummaryPanel.RepairRefitScrapShipComboBox.SelectedIndex;
                            if (CurrentShipIndex != -1)
                            {
                                CurrentShip = DamagedShipList[CurrentShipIndex];
                            }
                            break;
                        case Constants.ShipyardInfo.Task.Refit:
                            newShipIndex = m_oSummaryPanel.SYNewClassComboBox.SelectedIndex;
                            if (newShipIndex != -1)
                            {
                                ConstructRefit = EligibleClassList[newShipIndex];
                            }

                            CurrentShipIndex = m_oSummaryPanel.RepairRefitScrapShipComboBox.SelectedIndex;
                            if (CurrentShipIndex != -1)
                            {
                                CurrentShip = ShipsOfClassInOrbit[CurrentShipIndex];
                            }
                            break;
                        case Constants.ShipyardInfo.Task.Scrap:
                            CurrentShipIndex = m_oSummaryPanel.RepairRefitScrapShipComboBox.SelectedIndex;
                            if (CurrentShipIndex != -1)
                            {
                                CurrentShip = ShipsOfClassInOrbit[CurrentShipIndex];
                            }
                            break;
                    }

                    /// <summary>
                    /// if a slipway is available build the new ship.
                    /// </summary>
                    if (CurrentSYInfo.Slipways > CurrentSYInfo.BuildingShips.Count)
                    {
                        Installation.ShipyardInformation.ShipyardTask NewTask = new Installation.ShipyardInformation.ShipyardTask(CurrentShip, SYITask, TargetTG, BaseBuildRate, m_oSummaryPanel.SYShipNameTextBox.Text, ConstructRefit);
                        CurrentSYInfo.BuildingShips.Add(NewTask);
                        CurrentPopulation.ShipyardTasks.Add(NewTask, CurrentSYInfo);

                        /// <summary>
                        /// Cost display for the new order.
                        /// </summary>
                        m_oSummaryPanel.SYTaskCostTextBox.Text = CurrentSYInfo.BuildingShips[0].Cost.ToString();
                        m_oSummaryPanel.SYTaskCompletionDateTextBox.Text = CurrentSYInfo.BuildingShips[0].CompletionDate.ToShortDateString();

                        for (int MineralIterator = 0; MineralIterator < Constants.Minerals.NO_OF_MINERIALS; MineralIterator++)
                        {

                            if (CurrentSYInfo.BuildingShips[0].minerialsCost[MineralIterator] != 0.0m)
                            {
                                string FormattedMineralTotal = CurrentSYInfo.BuildingShips[0].minerialsCost[MineralIterator].ToString("#,##0");

                                String CostString = String.Format("{0} {1} ({2})", (Constants.Minerals.MinerialNames)MineralIterator, FormattedMineralTotal, CurrentPopulation.Minerials[MineralIterator]);
                                m_oSummaryPanel.ShipRequiredMaterialsListBox.Items.Add(CostString);
                            }
                        }

                        /// <summary>
                        /// So. I want the eco_SY tab handler to be able to populate these lists as needed.
                        /// So. they have to be refs.
                        /// But they are properties.
                        /// So kludge.
                        /// </summary>
                        BindingList<ShipClassTN> ECL = EligibleClassList;
                        BindingList<ShipClassTN> CIO = ClassesInOrbit;
                        BindingList<ShipTN> DSL = DamagedShipList;
                        BindingList<ShipTN> SCO = ShipsOfClassInOrbit;
                        Eco_ShipyardTabHandler.RefreshShipyardTab(m_oSummaryPanel, CurrentFaction, CurrentPopulation, CurrentSYInfo, PotentialRetoolTargets, ref ECL, ref DSL,
                                                                  ref CIO, ref SCO);
                        EligibleClassList = ECL;
                        ClassesInOrbit = CIO;
                        DamagedShipList = DSL;
                        ShipsOfClassInOrbit = SCO;
                    }
                }
            }
        }
        #region Shipyard Tasks Tab event handlers
        /// <summary>
        /// Pause the currently selected task.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SYAPauseTaskButton_Click(object sender, EventArgs e)
        {
            if (m_oSummaryPanel.ShipyardTaskDataGrid.CurrentCell != null)
            {
                if (m_oSummaryPanel.ShipyardTaskDataGrid.CurrentCell.RowIndex != -1)
                {
                    int index = m_oSummaryPanel.ShipyardTaskDataGrid.CurrentCell.RowIndex;

                    if(CurrentSYInfo.BuildingShips[index].IsPaused() == true)
                        CurrentSYInfo.BuildingShips[index].UnPause();
                    else
                        CurrentSYInfo.BuildingShips[index].Pause();


                    Eco_ShipyardTabHandler.RefreshShipyardTasksTab(m_oSummaryPanel, CurrentFaction, CurrentPopulation);
                }
            }
        }

        /// <summary>
        /// Decrease the priority of the selected task.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SYALowerPriorityButton_Click(object sender, EventArgs e)
        {
            if (m_oSummaryPanel.ShipyardTaskDataGrid.CurrentCell != null)
            {
                if (m_oSummaryPanel.ShipyardTaskDataGrid.CurrentCell.RowIndex != -1)
                {
                    int index = m_oSummaryPanel.ShipyardTaskDataGrid.CurrentCell.RowIndex;

                    CurrentSYInfo.BuildingShips[index].DecrementPriority();

                    Eco_ShipyardTabHandler.RefreshShipyardTasksTab(m_oSummaryPanel, CurrentFaction, CurrentPopulation);
                }
            }
        }

        /// <summary>
        /// Increase the priority of the selected task.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SYARaisePriorityButton_Click(object sender, EventArgs e)
        {
            if (m_oSummaryPanel.ShipyardTaskDataGrid.CurrentCell != null)
            {
                if (m_oSummaryPanel.ShipyardTaskDataGrid.CurrentCell.RowIndex != -1)
                {
                    int index = m_oSummaryPanel.ShipyardTaskDataGrid.CurrentCell.RowIndex;

                    CurrentSYInfo.BuildingShips[index].IncrementPriority();

                    Eco_ShipyardTabHandler.RefreshShipyardTasksTab(m_oSummaryPanel, CurrentFaction, CurrentPopulation);
                }
            }
        }
        #endregion

        #region Terraforming Tab event handlers
        /// <summary>
        /// confirm the terraforming orders from the terrafroming tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TerraformingSaveAtmButton_Click(object sender, EventArgs e)
        {
            /// <summary>
            /// True = add gas, false = subtract gas.
            /// </summary>
            if (m_oSummaryPanel.TerraformingAddGasCheckBox.Checked == true)
                CurrentPopulation._GasAddSubtract = true;
            else
                CurrentPopulation._GasAddSubtract = false;

            float getAtm;
            bool r1 = float.TryParse(m_oSummaryPanel.TerraformingMaxGasTextBox.Text, out getAtm);

            if (r1 == true)
            {
                CurrentPopulation._GasAmt = getAtm;
            }

            if (m_oSummaryPanel.TerraformingGasComboBox.SelectedIndex != -1)
            {
                int Count = 0;
                foreach (WeightedValue<AtmosphericGas> Gas in AtmosphericGas.AtmosphericGases)
                {
                    if (Count == m_oSummaryPanel.TerraformingGasComboBox.SelectedIndex)
                    {
                        CurrentPopulation._GasToAdd = Gas.Value;
                        break;
                    }
                    Count++;
                }
            }
        }
        #endregion
        #endregion

        #region PublicMethods

        public void ShowAllPanels(DockPanel a_oDockPanel)
        {
            ShowSummaryPanel(a_oDockPanel);
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

        /// <summary>
        /// The system map needs a way to refresh the economics panel based on the passage of time there, so a way to call SoftRefresh publically is needed.
        /// </summary>
        public void RefreshDisplay()
        {
            SoftRefresh();
        }

        #endregion


        #region PrivateMethods

        #region Time Advancement Buttons
        /// <summary>
        /// Function to advance time for all buttons. this is all lifted from the system map time code.
        /// </summary>
        /// <param name="deltaSeconds"></param>
        private void AdvanceTime(int deltaSeconds)
        {
            int elapsed = GameState.SE.SubpulseHandler(GameState.Instance.Factions, GameState.RNG, deltaSeconds);

            TimeSpan TS = new TimeSpan(0, 0, elapsed);
            GameState.Instance.GameDateTime = GameState.Instance.GameDateTime.Add(TS);

            int Seconds = GameState.Instance.GameDateTime.Second + (GameState.Instance.GameDateTime.Minute * 60) + (GameState.Instance.GameDateTime.Hour * 3600) +
                           (GameState.Instance.GameDateTime.DayOfYear * 86400) - 86400;

            /// <summary>
            /// Put the date time on the main form.
            /// </summary>
            MainFormReference.Text = "Pulsar4X - " + GameState.Instance.GameDateTime.ToString();

            /// <summary>
            /// update planet/taskgroup/other positions as needed.
            /// </summary>
            SystemMapReference.RefreshStarSystem();

            SoftRefresh();
        }


        /// <summary>
        /// Advance simulation time by 5 seconds
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FiveSecondsButton_Click(object sender, EventArgs e)
        {
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.FiveSeconds);
            }
        }

        /// <summary>
        /// Advance simulation time by 30 seconds
        /// </summary>
        private void ThirtySecondsButton_Click(object sender, EventArgs e)
        {
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.ThirtySeconds);
            }
        }

        /// <summary>
        /// Advance simulation time by 5 minutes
        /// </summary>
        private void FiveMinutesButton_Click(object sender, EventArgs e)
        {
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.FiveMinutes);
            }
        }

        /// <summary>
        /// Advance simulation time by 20 minutes
        /// </summary>
        private void TwentyMinutesButton_Click(object sender, EventArgs e)
        {
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.TwentyMinutes);
            }
        }

        /// <summary>
        /// Advance simulation time by 1 hour
        /// </summary>
        private void OneHourButton_Click(object sender, EventArgs e)
        {
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.Hour);
            }
        }

        /// <summary>
        /// Advance simulation time by 3 hours
        /// </summary>
        private void ThreeHoursButton_Click(object sender, EventArgs e)
        {
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.ThreeHours);
            }
        }

        /// <summary>
        /// Advance simulation time by 8 hours
        /// </summary>
        private void EightHoursButton_Click(object sender, EventArgs e)
        {
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.EightHours);
            }
        }

        /// <summary>
        /// Advance simulation time by 1 day
        /// </summary>
        private void OneDayButton_Click(object sender, EventArgs e)
        {
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.Day);
            }
        }

        /// <summary>
        /// Advance simulation time by 5 days
        /// </summary>
        private void FiveDaysButton_Click(object sender, EventArgs e)
        {
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.FiveDays);
            }
        }

        /// <summary>
        /// Advance simulation time by 30 days
        /// </summary>
        private void ThirtyDaysButton_Click(object sender, EventArgs e)
        {
            if (GameState.SE.SimCreated == true)
            {
                AdvanceTime((int)Constants.TimeInSeconds.Month);
            }
        }
        #endregion

        #region IndustrialTab
        /// <summary>
        /// Stockpile swap button functionality
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StockpileButton_Click(object sender, EventArgs e)
        {
            if (m_oSummaryPanel.ConstructionDataGrid.Visible == false)
            {
                m_oSummaryPanel.ConstructionDataGrid.Visible = true;
                m_oSummaryPanel.ShipComponentGroupBox.Visible = false;
                m_oSummaryPanel.PlanetMissileGroupBox.Visible = false;
                m_oSummaryPanel.PlanetPDCGroupBox.Visible = false;
                m_oSummaryPanel.PlanetFighterGroupBox.Visible = false;
            }
            else
            {
                m_oSummaryPanel.ConstructionDataGrid.Visible = false;
                m_oSummaryPanel.ShipComponentGroupBox.Visible = true;
                m_oSummaryPanel.PlanetMissileGroupBox.Visible = true;
                m_oSummaryPanel.PlanetPDCGroupBox.Visible = true;
                m_oSummaryPanel.PlanetFighterGroupBox.Visible = true;
            }

        }

        /// <summary>
        /// If the user selects a different item in the build list handle this event here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BuildDataGrid_SelectionChanged(object sender, EventArgs e)
        {
            BuildCostListBox();
        }

        /// <summary>
        /// If the user selects another construction project update the display with information about that project for the modify project button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConstructionDataGrid_SelectionChanged(object sender, EventArgs e)
        {
#warning when fighters are implemented This, and the next 4 functions need to be updated. Update to the update, make sure UpdateBuildTexts can handle fighters.
            UpdateBuildTexts();
        }

        /// <summary>
        /// These following button presses perform different tasks to the currently selected population's build queue.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateBuildProjButton_Click(object sender, EventArgs e)
        {
            if (CurrentPopulation != null)
            {
                if (m_oSummaryPanel.BuildDataGrid.CurrentCell != null)
                {
                    float NumToBuild = -1.0f;
                    float PercentCapacity = -1.0f;
                    bool r1 = float.TryParse(m_oSummaryPanel.ItemNumberTextBox.Text, out NumToBuild);
                    bool r2 = float.TryParse(m_oSummaryPanel.ItemPercentTextBox.Text, out PercentCapacity);

                    if (m_oSummaryPanel.BuildDataGrid.CurrentCell.RowIndex != -1 && r1 == true && r2 == true)
                    {
                        List<Guid> GID = BuildLocationDisplayDict.Keys.ToList();
                        switch (BuildLocationDict[GID[m_oSummaryPanel.BuildDataGrid.CurrentCell.RowIndex]].EntityType)
                        {
                            case BuildListObject.ListEntityType.Installation:
                                Installation Install = BuildLocationDict[GID[m_oSummaryPanel.BuildDataGrid.CurrentCell.RowIndex]].Entity as Installation;
                                CurrentPopulation.BuildQueueAddInstallation(Install, NumToBuild, PercentCapacity);
                                break;
                            case BuildListObject.ListEntityType.Component:
                                ComponentDefTN Component = BuildLocationDict[GID[m_oSummaryPanel.BuildDataGrid.CurrentCell.RowIndex]].Entity as ComponentDefTN;
                                CurrentPopulation.BuildQueueAddComponent(Component, NumToBuild, PercentCapacity);
                                break;
                            case BuildListObject.ListEntityType.Missile:
                                OrdnanceDefTN Missile = BuildLocationDict[GID[m_oSummaryPanel.BuildDataGrid.CurrentCell.RowIndex]].Entity as OrdnanceDefTN;
                                CurrentPopulation.BuildQueueAddMissile(Missile, NumToBuild, PercentCapacity);
                                break;
                            case BuildListObject.ListEntityType.Fighter:
#warning fighter and PDC not done here.
                                break;
                            case BuildListObject.ListEntityType.PDC_Build:
                                break;
                            case BuildListObject.ListEntityType.PDC_Prefab:
                                break;
                            case BuildListObject.ListEntityType.PDC_Assemble:
                                break;
                            case BuildListObject.ListEntityType.PDC_Refit:
                                break;
                            case BuildListObject.ListEntityType.MaintenanceSupplies:
                                CurrentPopulation.BuildQueueAddMSP(NumToBuild, PercentCapacity);
                                break;
                        }
                    }
                }
            }
            Build_BuildQueue();
        }
        /// <summary>
        /// Need to find current selection for these next 3.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ModifyBuildProjButton_Click(object sender, EventArgs e)
        {
            if (m_oSummaryPanel.ConstructionDataGrid.CurrentCell != null)
            {
                if (m_oSummaryPanel.ConstructionDataGrid.CurrentCell.RowIndex != -1)
                {
                    float NumToBuild = -1.0f;
                    float PercentCapacity = -1.0f;
                    bool r1 = float.TryParse(m_oSummaryPanel.ItemNumberTextBox.Text, out NumToBuild);
                    bool r2 = float.TryParse(m_oSummaryPanel.ItemPercentTextBox.Text, out PercentCapacity);

                    int index = m_oSummaryPanel.ConstructionDataGrid.CurrentCell.RowIndex;
                    if (index > 0 && index <= CurrentPopulation.ConstructionBuildQueue.Count) // 1 to Count is CBQ Item
                    {
                        int RealIndex = index - 1;
                        CurrentPopulation.ConstructionBuildQueue[RealIndex].numToBuild = NumToBuild;
                        CurrentPopulation.ConstructionBuildQueue[RealIndex].buildCapacity = PercentCapacity;
                    }
                    else if (index > (CurrentPopulation.ConstructionBuildQueue.Count + 2) &&
                             index < ((CurrentPopulation.MissileBuildQueue.Count + CurrentPopulation.ConstructionBuildQueue.Count) + 3)) //Count + 2 to MBQ + CBQ = MBQ Item
                    {
                        int RealIndex = index - (CurrentPopulation.ConstructionBuildQueue.Count + 3);
                        CurrentPopulation.MissileBuildQueue[RealIndex].numToBuild = NumToBuild;
                        CurrentPopulation.MissileBuildQueue[RealIndex].buildCapacity = PercentCapacity;
                    }
                }
            }

            /// <summary>
            /// Update the display.
            /// </summary>
            Build_BuildQueue();
        }

        /// <summary>
        /// Cancels the selected build project.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelBuildProjButton_Click(object sender, EventArgs e)
        {
            if (m_oSummaryPanel.ConstructionDataGrid.CurrentCell != null)
            {
                if (m_oSummaryPanel.ConstructionDataGrid.CurrentCell.RowIndex != -1)
                {
                    int index = m_oSummaryPanel.ConstructionDataGrid.CurrentCell.RowIndex;
                    if (index > 0 && index <= CurrentPopulation.ConstructionBuildQueue.Count) // 1 to Count is CBQ Item
                    {
                        int RealIndex = index - 1;
                        CurrentPopulation.ConstructionBuildQueue.RemoveAt(RealIndex);
                    }
                    else if (index > (CurrentPopulation.ConstructionBuildQueue.Count + 2) &&
                             index < ((CurrentPopulation.MissileBuildQueue.Count + CurrentPopulation.ConstructionBuildQueue.Count) + 3)) //Count + 2 to MBQ + CBQ = MBQ Item
                    {
                        int RealIndex = index - (CurrentPopulation.ConstructionBuildQueue.Count + 3);
                        CurrentPopulation.MissileBuildQueue.RemoveAt(RealIndex);
                    }
                }
            }

            /// <summary>
            /// Update the display.
            /// </summary>
            Build_BuildQueue();
        }

        /// <summary>
        /// Pauses the currently selected build project.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PauseBuildProjButton_Click(object sender, EventArgs e)
        {
            if (m_oSummaryPanel.ConstructionDataGrid.CurrentCell != null)
            {
                if (m_oSummaryPanel.ConstructionDataGrid.CurrentCell.RowIndex != -1)
                {
                    int index = m_oSummaryPanel.ConstructionDataGrid.CurrentCell.RowIndex;
                    if (index > 0 && index <= CurrentPopulation.ConstructionBuildQueue.Count) // 1 to Count is CBQ Item
                    {
                        int RealIndex = index - 1;

                        if (CurrentPopulation.ConstructionBuildQueue[RealIndex].inProduction == true)
                            CurrentPopulation.ConstructionBuildQueue[RealIndex].inProduction = false;
                        else
                            CurrentPopulation.ConstructionBuildQueue[RealIndex].inProduction = true;
                    }
                    else if (index > (CurrentPopulation.ConstructionBuildQueue.Count + 2) &&
                             index < ((CurrentPopulation.MissileBuildQueue.Count + CurrentPopulation.ConstructionBuildQueue.Count) + 3)) //Count + 2 to MBQ + CBQ = MBQ Item
                    {
                        int RealIndex = index - (CurrentPopulation.ConstructionBuildQueue.Count + 3);

                        if (CurrentPopulation.MissileBuildQueue[RealIndex].inProduction == true)
                            CurrentPopulation.MissileBuildQueue[RealIndex].inProduction = false;
                        else
                            CurrentPopulation.MissileBuildQueue[RealIndex].inProduction = true;
                    }
                }
            }

            /// <summary>
            /// Update the display.
            /// </summary>
            Build_BuildQueue();
        }

        /// <summary>
        /// Start fuel production at this colony
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartRefiningButton_Click(object sender, EventArgs e)
        {
            if (CurrentPopulation != null)
                CurrentPopulation.IsRefining = true;

            m_oSummaryPanel.StartRefiningButton.Enabled = false;
            m_oSummaryPanel.StopRefiningButton.Enabled = true;
        }

        /// <summary>
        /// Stop fuel production at this colony.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopRefiningButton_Click(object sender, EventArgs e)
        {
            if (CurrentPopulation != null)
                CurrentPopulation.IsRefining = false;

            m_oSummaryPanel.StartRefiningButton.Enabled = true;
            m_oSummaryPanel.StopRefiningButton.Enabled = false;
        }
        #endregion


        /// <summary>
        /// Refresh all the various panels that make up this display.
        /// </summary>
        private void RefreshPanels(bool skipTree=false)
        {
            if (m_oCurrnetFaction != null)
            {
                m_oSummaryPanel.ShipyardDataGrid.ClearSelection();

                /// <summary>
                /// reset the construction type combo box selection to 0.
                /// </summary>
                if (m_oSummaryPanel.InstallationTypeComboBox.Items.Count != 0)
                    m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex = 0;


                /// <summary>
                /// Build the population lists.
                /// </summary>
                if(skipTree == false)
                    BuildTreeView();

                /// <summary>
                /// Summary Tab:
                /// </summary>
                RefreshSummaryCells();

                /// <summary>
                /// Industry Tab:
                /// </summary>
                RefreshIndustryTab();

                /// <summary>
                /// Mining Tab:
                /// </summary>
                RefreshMiningTab();

                /// <summary>
                /// Shipyard Tab:
                /// </summary>

                /// <summary>
                /// This needs to be cleared properly.
                /// </summary>
                ClearCB(m_oSummaryPanel.SYNewClassComboBox);
                ClearCB(m_oSummaryPanel.SYTaskGroupComboBox);
                ClearCB(m_oSummaryPanel.RepairRefitScrapClassComboBox);
                ClearCB(m_oSummaryPanel.RepairRefitScrapShipComboBox);
                ClearCB(m_oSummaryPanel.NewShipClassComboBox);
                m_oSummaryPanel.SYShipNameTextBox.Clear();

                
                /// <summary>
                /// So. I want the eco_SY tab handler to be able to populate these lists as needed.
                /// So. they have to be refs.
                /// But they are properties.
                /// So kludge.
                /// </summary>
                
                BindingList<ShipClassTN> ECL = EligibleClassList;
                BindingList<ShipClassTN> CIO = ClassesInOrbit;
                BindingList<ShipTN> DSL = DamagedShipList;
                BindingList<ShipTN> SCO = ShipsOfClassInOrbit;
                Eco_ShipyardTabHandler.RefreshShipyardTab(m_oSummaryPanel, CurrentFaction, CurrentPopulation, CurrentSYInfo, PotentialRetoolTargets, ref ECL, ref DSL,
                                                          ref CIO, ref SCO);
                EligibleClassList = ECL;
                ClassesInOrbit = CIO;
                DamagedShipList = DSL;
                ShipsOfClassInOrbit = SCO;

                /// <summary>
                /// Terraforming Tab:
                /// </summary>
                BuildTerraformingTab();
            }
        }

        /// <summary>
        /// clear the specified combo box.
        /// </summary>
        /// <param name="BoxToClear"></param>
        private void ClearCB(ComboBox BoxToClear)
        {
            BoxToClear.Items.Clear();
            BoxToClear.Text = "";
            BoxToClear.SelectedIndex = -1;
        }

        /// <summary>
        /// Soft refresh does not refresh the entire page, and only updates items that may change as a result of time advancement.
        /// </summary>
        private void SoftRefresh()
        {
            if (m_oCurrnetFaction != null)
            {
                /// <summary>
                /// Summary Tab:
                /// </summary>
                RefreshSummaryCells();

                /// <summary>
                /// Refresh the construction queue:
                /// </summary>
                Build_BuildQueue();

                /// <summary>
                /// Update stockpiles.
                /// </summary>
                BuildStockListBoxes();

                /// <summary>
                /// Industry Tab:
                /// </summary>
                BuildConstructionLabel();
                BuildRefiningLabel();
                UpdateBuildTexts();

                /// <summary>
                /// Mining Tab:
                /// </summary>
                RefreshMiningTab();

                /// <summary>
                /// Shipyard Tab:
                /// </summary>

                /// <summary>
                /// So. I want the eco_SY tab handler to be able to populate these lists as needed.
                /// So. they have to be refs.
                /// But they are properties.
                /// So kludge.
                /// </summary>
                BindingList<ShipClassTN> ECL = EligibleClassList;
                BindingList<ShipClassTN> CIO = ClassesInOrbit;
                BindingList<ShipTN> DSL = DamagedShipList;
                BindingList<ShipTN> SCO = ShipsOfClassInOrbit;
                Eco_ShipyardTabHandler.RefreshShipyardTab(m_oSummaryPanel, CurrentFaction, CurrentPopulation, CurrentSYInfo, PotentialRetoolTargets, ref ECL, ref DSL,
                                                          ref CIO, ref SCO);
                EligibleClassList = ECL;
                ClassesInOrbit = CIO;
                DamagedShipList = DSL;
                ShipsOfClassInOrbit = SCO;

                /// <summary>
                /// Terraforming Tab:
                /// </summary>
                BuildTerraformingTab();
            }
        }

        /// <summary>
        /// Just a space saver here to avoid copy pasting a lot. this is copied from taskgroup
        /// </summary>
        /// <param name="Header">Text of column header.</param>
        /// <param name="newPadding">Padding in use, not sure what this is or why its necessary. Cargo culting it is.</param>
        /// <param name="CellControl">Alignment control. 0 = both left, 1 = header left, cells center, 2 = header center, cells left, 3 = both center.</param>
        private void AddColumn(String Header, Padding newPadding, DataGridView TheDataGrid, int CellControl = -1)
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

        #region General F2 page
        /// <summary>
        /// Build the tree view box of populations.
        /// </summary>
        private void BuildTreeView()
        {
            TreeViewDictionary.Clear();

            Dictionary<StarSystem, float> SystemPopulation = new Dictionary<StarSystem, float>();

            m_oSummaryPanel.PopulationTreeView.Nodes.Clear();

            m_oSummaryPanel.PopulationTreeView.Nodes.Add("Populated Systems");

            if (m_oSummaryPanel.GroupByFunctionCheckBox.Checked == true)
            {
                m_oSummaryPanel.PopulationTreeView.Nodes.Add("Automated Mining Colonies");

                if (m_oSummaryPanel.HideCMCCheckBox.Checked == false)
                    m_oSummaryPanel.PopulationTreeView.Nodes.Add("Civilian Mining Colonies");

                m_oSummaryPanel.PopulationTreeView.Nodes.Add("Listening Posts");
                m_oSummaryPanel.PopulationTreeView.Nodes.Add("Archeological Digs");
                m_oSummaryPanel.PopulationTreeView.Nodes.Add("Terraforming Sites");
                m_oSummaryPanel.PopulationTreeView.Nodes.Add("Other Colonies");

                foreach (Population Pop in m_oCurrnetFaction.Populations)
                {
                    StarSystem CurrentSystem = Pop.Planet.Position.System;

                    /// <summary>
                    /// What type of colony is this, and should it be placed into the tree view(no if CMC and CMC are hidden)
                    /// <summary>
                    String Class = "";

                    /// <summary>
                    /// What key will be put into the display tree?
                    /// </summary>
                    String Entry = "";

                    /// <summary>
                    /// Which node display should be used?
                    /// </summary>
                    int DisplayIndex = -1;

                    /// <summary>
                    /// Populated colony, can do basically anything
                    /// </summary>
                    if (Pop.CivilianPopulation > 0)
                    {
                        if (SystemPopulation.ContainsKey(CurrentSystem) == false)
                        {
                            SystemPopulation.Add(CurrentSystem, Pop.CivilianPopulation);
                        }
                        else
                        {
                            SystemPopulation[CurrentSystem] = SystemPopulation[CurrentSystem] + Pop.CivilianPopulation;
                        }

                        if (m_oCurrnetFaction.Capitol == Pop)
                            Class = String.Format("(Capitol)");

                        Class = String.Format("{0}: {1:n2}m",Class, Pop.CivilianPopulation);

                        Entry = String.Format("{0} - {1}{2}", Pop.Name, Pop.Species.Name, Class);

#warning DisplayIndex here is kludgy, do a find on the appropriate section? if so alter the adds to include a key string in addition to a text string
                        DisplayIndex = 0;
                    }
                    /// <summary>
                    /// Automining colony will only mine, but may have CMCs listening posts, terraforming gear and ruins
                    /// </summary>
                    else if (Pop.Installations[(int)Installation.InstallationType.AutomatedMine].Number >= 1)
                    {
                        int mines = (int)Math.Floor(Pop.Installations[(int)Installation.InstallationType.AutomatedMine].Number);
                        Class = String.Format(": {0}x Auto Mines", mines);

                        Entry = String.Format("{0} - {1}{2}", Pop.Name, Pop.Species.Name, Class);

                        DisplayIndex = 1;
                    }
                    /// <summary>
                    /// CMCs. don't print this one if they should be hidden(by user input request). will also have a DSTS(or should I roll that into the CMC?), and may have terraforming and ruins)
                    /// </summary.
                    else if (Pop.Installations[(int)Installation.InstallationType.CivilianMiningComplex].Number >= 1 && m_oSummaryPanel.HideCMCCheckBox.Checked == false)
                    {
                        int mines = (int)Math.Floor(Pop.Installations[(int)Installation.InstallationType.CivilianMiningComplex].Number);
                        Class = String.Format(": {0}x Civ Mines", mines);

                        Entry = String.Format("{0} - {1}{2}", Pop.Name, Pop.Species.Name, Class);

                        DisplayIndex = 2;

                    }
                    /// <summary>
                    /// Listening Post. will have DSTS, and maybe terraforming or ruins.
                    /// </summary>
                    else if (Pop.Installations[(int)Installation.InstallationType.DeepSpaceTrackingStation].Number >= 1)
                    {
                        int DSTS = (int)Math.Floor(Pop.Installations[(int)Installation.InstallationType.DeepSpaceTrackingStation].Number);
                        Class = String.Format(": {0}x DSTS", DSTS);

                        Entry = String.Format("{0} - {1}{2}", Pop.Name, Pop.Species.Name, Class);

                        DisplayIndex = 2;
                        if (m_oSummaryPanel.HideCMCCheckBox.Checked == false)
                            DisplayIndex = 3;
                    }

                    /// <summary>
                    /// Archeological Dig. will have ruins, and may have orbital terraforming.
                    /// </summary>
                    else if (Pop.Planet.PlanetaryRuins.RuinSize != Ruins.RSize.NoRuins)
                    {
                        Class = String.Format(" Archeological Dig");

                        Entry = String.Format("{0} - {1}{2}", Pop.Name, Pop.Species.Name, Class);

                        DisplayIndex = 3;
                        if (m_oSummaryPanel.HideCMCCheckBox.Checked == false)
                            DisplayIndex = 4;
                    }
                    /// <summary>
                    /// Orbital Terraforming modules. a planet with ships in orbit that will terraform it.
                    /// </summary>
                    else if (Pop._OrbitalTerraformModules >= 1)
                    {
                        Class = String.Format(": {0:n1}x Orbital Terraform", Pop._OrbitalTerraformModules);

                        Entry = String.Format("{0} - {1}{2}", Pop.Name, Pop.Species.Name, Class);

                        DisplayIndex = 4;
                        if (m_oSummaryPanel.HideCMCCheckBox.Checked == false)
                            DisplayIndex = 5;
                    }
                    else
                    {

                        /// <summary>
                        /// If none of the above are true, then the colony is simply dropped into the other colonies category.
                        /// </summary>

                        Entry = String.Format("{0} - {1}", Pop.Name, Pop.Species.Name);

                        DisplayIndex = 5;
                        if (m_oSummaryPanel.HideCMCCheckBox.Checked == false)
                            DisplayIndex = 6;
                    }

                    /// <summary>
                    /// Every displayIndex node needs to have the current system in it added somewhere, so I'll do it here.
                    /// </summary>
                    if (m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes.ContainsKey(CurrentSystem.Name) == false)
                    {
                        m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes.Add(CurrentSystem.Name, CurrentSystem.Name);
                    }

                    int CurrentSystemIndex = m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes.IndexOfKey(CurrentSystem.Name);
                    m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes[CurrentSystemIndex].Nodes.Add(Entry, Entry);

                    TreeViewDictionary.Add(Entry, Pop);
                }
            }
            else
            {
                foreach (Population Pop in m_oCurrnetFaction.Populations)
                {
                    StarSystem CurrentSystem = Pop.Planet.Position.System;

                    if (m_oSummaryPanel.PopulationTreeView.Nodes[0].Nodes.ContainsKey(CurrentSystem.Name) == false)
                    {
                        m_oSummaryPanel.PopulationTreeView.Nodes[0].Nodes.Add(CurrentSystem.Name, CurrentSystem.Name);
                    }

                    /// <summary>
                    /// What type of colony is this, and should it be placed into the tree view(no if CMC and CMC are hidden)
                    /// <summary>
                    String Class = "";
                    bool CMCPrintControl = true;

                    /// <summary>
                    /// Populated colony, can do basically anything
                    /// </summary>
                    if (Pop.CivilianPopulation > 0)
                    {
                        if (SystemPopulation.ContainsKey(CurrentSystem) == false)
                        {
                            SystemPopulation.Add(CurrentSystem, Pop.CivilianPopulation);
                        }
                        else
                        {
                            SystemPopulation[CurrentSystem] = SystemPopulation[CurrentSystem] + Pop.CivilianPopulation;
                        }

                        if (m_oCurrnetFaction.Capitol == Pop)
                            Class = String.Format("(Capitol)");

                        Class = String.Format("{0}: {1:n2}m", Class, Pop.CivilianPopulation);
                    }
                    /// <summary>
                    /// Automining colony will only mine, but may have CMCs listening posts, terraforming gear and ruins
                    /// </summary>
                    else if (Pop.Installations[(int)Installation.InstallationType.AutomatedMine].Number >= 1)
                    {
                        int mines = (int)Math.Floor(Pop.Installations[(int)Installation.InstallationType.AutomatedMine].Number);
                        Class = String.Format(": {0}x Auto Mines", mines);
                    }
                    /// <summary>
                    /// CMCs. don't print this one if they should be hidden(by user input request). will also have a DSTS(or should I roll that into the CMC?), and may have terraforming and ruins)
                    /// </summary.
                    else if (Pop.Installations[(int)Installation.InstallationType.CivilianMiningComplex].Number >= 1)
                    {
                        int mines = (int)Math.Floor(Pop.Installations[(int)Installation.InstallationType.CivilianMiningComplex].Number);
                        Class = String.Format(": {0}x Civ Mines", mines);

                        if (m_oSummaryPanel.HideCMCCheckBox.Checked == true)
                            CMCPrintControl = false;
                    }
                    /// <summary>
                    /// Listening Post. will have DSTS, and maybe terraforming or ruins.
                    /// </summary>
                    else if (Pop.Installations[(int)Installation.InstallationType.DeepSpaceTrackingStation].Number >= 1)
                    {
                        int DSTS = (int)Math.Floor(Pop.Installations[(int)Installation.InstallationType.DeepSpaceTrackingStation].Number);
                        Class = String.Format(": {0}x DSTS", DSTS);
                    }

                    /// <summary>
                    /// Archeological Dig. will have ruins, and may have orbital terraforming.
                    /// </summary>
                    else if (Pop.Planet.PlanetaryRuins.RuinSize != Ruins.RSize.NoRuins)
                    {
                        Class = String.Format(" Archeological Dig");
                    }
                    /// <summary>
                    /// Orbital Terraforming modules. a planet with ships in orbit that will terraform it.
                    /// </summary>
                    else if (Pop._OrbitalTerraformModules >= 1)
                    {
                        Class = String.Format(": {0:n1}x Orbital Terraform", Pop._OrbitalTerraformModules);
                    }

                    /// <summary>
                    /// If none of the above are true, then the colony is simply dropped into the other colonies category, though that isn't important here.
                    /// Don't print if this is a CMC and CMC are hidden.
                    /// </summary>

                    if (CMCPrintControl == true)
                    {
                        String Entry = String.Format("{0} - {1}{2}", Pop.Name, Pop.Species.Name, Class);

                        int CurrentSystemIndex = m_oSummaryPanel.PopulationTreeView.Nodes[0].Nodes.IndexOfKey(CurrentSystem.Name);
                        m_oSummaryPanel.PopulationTreeView.Nodes[0].Nodes[CurrentSystemIndex].Nodes.Add(Entry, Entry);

                        TreeViewDictionary.Add(Entry, Pop);
                    }
                }
            }

            foreach (KeyValuePair<StarSystem, float> pair in SystemPopulation)
            {
#warning if Populated systems isn't the 0th node location in sorted view this will break so change this to search for populated system if that is done.
                int key = m_oSummaryPanel.PopulationTreeView.Nodes[0].Nodes.IndexOfKey(pair.Key.Name);

                String Entry = String.Format("{0}({1:n1}m)", pair.Key.Name, pair.Value);

                m_oSummaryPanel.PopulationTreeView.Nodes[0].Nodes[key].Text = Entry;
            }


            /// <summary>
            /// Expand all the nodes here so that they are all printed.
            /// </summary>
            m_oSummaryPanel.PopulationTreeView.Nodes[0].ExpandAll();
        }
        #endregion

        #region Industrial Summary
        private void SetupSummaryDataGrid()
        {
            try
            {
                // Add coloums:
                Padding newPadding = new Padding(2, 0, 2, 0);
                AddColumn("Item", newPadding, m_oSummaryPanel.SummaryDataGrid, 2);
                AddColumn("Amount", newPadding, m_oSummaryPanel.SummaryDataGrid, 3);
                AddColumn("Installation", newPadding, m_oSummaryPanel.SummaryDataGrid, 2);
                AddColumn("Number or Level", newPadding, m_oSummaryPanel.SummaryDataGrid, 3);

                AddColumn("Item", newPadding, m_oSummaryPanel.BuildDataGrid);

                AddColumn("Project", newPadding, m_oSummaryPanel.ConstructionDataGrid, 0);
                AddColumn("Amount Remaining", newPadding, m_oSummaryPanel.ConstructionDataGrid, 3);
                AddColumn("% of Capacity", newPadding, m_oSummaryPanel.ConstructionDataGrid, 3);
                AddColumn("Production Rate", newPadding, m_oSummaryPanel.ConstructionDataGrid, 3);
                AddColumn("Cost Per Item", newPadding, m_oSummaryPanel.ConstructionDataGrid, 3);
                AddColumn("Estimated Completion Date", newPadding, m_oSummaryPanel.ConstructionDataGrid, 3);
                AddColumn("Pause / Queue", newPadding, m_oSummaryPanel.ConstructionDataGrid, 3);

                // Add Rows:
                for (int i = 0; i < 38; ++i)
                {
                    using (DataGridViewRow row = new DataGridViewRow())
                    {
                        // setup row height. note that by default they are 22 pixels in height!
                        row.Height = 18;
                        m_oSummaryPanel.SummaryDataGrid.Rows.Add(row);
                    }
                }

                for (int RowIterator = 0; RowIterator < BuildTabMaxRows; RowIterator++)
                {
                    using (DataGridViewRow row = new DataGridViewRow())
                    {
                        // setup row height. note that by default they are 22 pixels in height!
                        row.Height = 17;
                        m_oSummaryPanel.BuildDataGrid.Rows.Add(row);
                    }

                    using (DataGridViewRow row = new DataGridViewRow())
                    {
                        // setup row height. note that by default they are 22 pixels in height!
                        row.Height = 17;
                        m_oSummaryPanel.ConstructionDataGrid.Rows.Add(row);
                    }
                }

                /// <summary>
                /// General Colony Information
                /// </summary>
                m_oSummaryPanel.SummaryDataGrid.Rows[0].Cells[0].Value = "Political Status";
                m_oSummaryPanel.SummaryDataGrid.Rows[1].Cells[0].Value = "Species";
                m_oSummaryPanel.SummaryDataGrid.Rows[2].Cells[0].Value = "Planetary Suitability(colony cost)";
                m_oSummaryPanel.SummaryDataGrid.Rows[3].Cells[0].Value = "Administration Level Required";

                /// <summary>
                /// Wealth = Population * wealth tech
                /// </summary>
                m_oSummaryPanel.SummaryDataGrid.Rows[5].Cells[0].Value = "Annual Wealth Creation";

                /// <summary>
                /// Population Breakdown
                /// </summary>
                m_oSummaryPanel.SummaryDataGrid.Rows[7].Cells[0].Value = "Population";
                m_oSummaryPanel.SummaryDataGrid.Rows[8].Cells[0].Value = "   Agriculture and Enviromental (5.0%)";
                m_oSummaryPanel.SummaryDataGrid.Rows[9].Cells[0].Value = "   Service Industries (75.0%)";
                m_oSummaryPanel.SummaryDataGrid.Rows[10].Cells[0].Value = "   Manufacturing (20.0%)";
                m_oSummaryPanel.SummaryDataGrid.Rows[11].Cells[0].Value = "Anual Growth Rate";

                /// <summary>
                /// Infrastructure information.
                /// </summary>
                m_oSummaryPanel.SummaryDataGrid.Rows[13].Cells[0].Value = "Infrastructure Required per Million Population";
                m_oSummaryPanel.SummaryDataGrid.Rows[14].Cells[0].Value = "Current Infrastructure";
                m_oSummaryPanel.SummaryDataGrid.Rows[15].Cells[0].Value = "Population supported by Infrastructure";

                /// <summary>
                /// Manufacturing Sector Population Usage
                /// </summary>
                m_oSummaryPanel.SummaryDataGrid.Rows[17].Cells[0].Value = "Manufacturing Sector Breakdown";
                m_oSummaryPanel.SummaryDataGrid.Rows[18].Cells[0].Value = "Shipyard Workers";
                m_oSummaryPanel.SummaryDataGrid.Rows[19].Cells[0].Value = "Maintenance Workers";
                m_oSummaryPanel.SummaryDataGrid.Rows[20].Cells[0].Value = "Construction Workers";
                m_oSummaryPanel.SummaryDataGrid.Rows[21].Cells[0].Value = "Ordnance Factory Workers";
                m_oSummaryPanel.SummaryDataGrid.Rows[22].Cells[0].Value = "Fighter Factory Workers";
                m_oSummaryPanel.SummaryDataGrid.Rows[23].Cells[0].Value = "Fuel Refinery Workers";
                m_oSummaryPanel.SummaryDataGrid.Rows[24].Cells[0].Value = "Financial Centre Workers";
                m_oSummaryPanel.SummaryDataGrid.Rows[25].Cells[0].Value = "Mine Workers";
                m_oSummaryPanel.SummaryDataGrid.Rows[26].Cells[0].Value = "Terraformers";
                m_oSummaryPanel.SummaryDataGrid.Rows[27].Cells[0].Value = "Scientists";
                m_oSummaryPanel.SummaryDataGrid.Rows[28].Cells[0].Value = "Available Workers";

                /// <summary>
                /// Protection
                /// </summary>
                m_oSummaryPanel.SummaryDataGrid.Rows[30].Cells[0].Value = "Requested Protection Level";
                m_oSummaryPanel.SummaryDataGrid.Rows[31].Cells[0].Value = "Actual Protection Level";

                /// <summary>
                /// Planetary Geology
                /// </summary>
                m_oSummaryPanel.SummaryDataGrid.Rows[33].Cells[0].Value = "Tectonics";
                m_oSummaryPanel.SummaryDataGrid.Rows[34].Cells[0].Value = "Geological Team Survey Completed";

                /// <summary>
                /// Ruins
                /// </summary>
                m_oSummaryPanel.SummaryDataGrid.Rows[0].Cells[2].Value = "Abandoned Installations";
                m_oSummaryPanel.SummaryDataGrid.Rows[0].Cells[3].Value = "??";
                m_oSummaryPanel.SummaryDataGrid.Rows[1].Cells[2].Value = "(Unknown Race - Xenologist Team Required)";

                /// <summary>
                /// Global Colony Installations
                /// </summary>
                m_oSummaryPanel.SummaryDataGrid.Rows[3].Cells[2].Value = "Sector Command HQ";
                m_oSummaryPanel.SummaryDataGrid.Rows[4].Cells[2].Value = "Commerical Spaceport";
                m_oSummaryPanel.SummaryDataGrid.Rows[5].Cells[2].Value = "Military Academy";
                m_oSummaryPanel.SummaryDataGrid.Rows[6].Cells[2].Value = "Deep Space Tracking Station";
                m_oSummaryPanel.SummaryDataGrid.Rows[7].Cells[2].Value = "Annual Genetic Conversion Rate";
                m_oSummaryPanel.SummaryDataGrid.Rows[8].Cells[2].Value = "Maintenance Facility Maximum Ship Size";
                m_oSummaryPanel.SummaryDataGrid.Rows[9].Cells[2].Value = "Mass Driver Capacity";

                /// <summary>
                /// Industrial Colony Installations
                /// </summary>
                m_oSummaryPanel.SummaryDataGrid.Rows[11].Cells[2].Value = "Shipyards / Slipways";
                m_oSummaryPanel.SummaryDataGrid.Rows[12].Cells[2].Value = "Maintenance Facilities";
                m_oSummaryPanel.SummaryDataGrid.Rows[13].Cells[2].Value = "Construction Factories";
                m_oSummaryPanel.SummaryDataGrid.Rows[14].Cells[2].Value = "Conventional Industry";
                m_oSummaryPanel.SummaryDataGrid.Rows[15].Cells[2].Value = "Ordnance Factories";
                m_oSummaryPanel.SummaryDataGrid.Rows[16].Cells[2].Value = "Fighter Factories";
                m_oSummaryPanel.SummaryDataGrid.Rows[17].Cells[2].Value = "Fuel Refineries";
                m_oSummaryPanel.SummaryDataGrid.Rows[18].Cells[2].Value = "Mines";
                m_oSummaryPanel.SummaryDataGrid.Rows[19].Cells[2].Value = "Automated Mines";
                m_oSummaryPanel.SummaryDataGrid.Rows[20].Cells[2].Value = "Mass Drivers";
                m_oSummaryPanel.SummaryDataGrid.Rows[21].Cells[2].Value = "Terraforming Installation";
                m_oSummaryPanel.SummaryDataGrid.Rows[22].Cells[2].Value = "Research Labs";
                m_oSummaryPanel.SummaryDataGrid.Rows[23].Cells[2].Value = "Gene Modification Centers";
                m_oSummaryPanel.SummaryDataGrid.Rows[24].Cells[2].Value = "Financial Centre";
                m_oSummaryPanel.SummaryDataGrid.Rows[25].Cells[2].Value = "Ground Force Training Facilities";

                /// <summary>
                /// Colony Supplies
                /// </summary>
                m_oSummaryPanel.SummaryDataGrid.Rows[27].Cells[2].Value = "Fuel Reserves";
                m_oSummaryPanel.SummaryDataGrid.Rows[28].Cells[2].Value = "Maintenance Supplies";

                /// <summary>
                /// Colony Detection Characteristics
                /// </summary>
                m_oSummaryPanel.SummaryDataGrid.Rows[30].Cells[2].Value = "Thermal Signature of Colony";
                m_oSummaryPanel.SummaryDataGrid.Rows[31].Cells[2].Value = "EM Signature of Colony";

                /// <summary>
                /// Colony Modifiers
                /// </summary>
                m_oSummaryPanel.SummaryDataGrid.Rows[33].Cells[2].Value = "Economic Production Modifier";
                m_oSummaryPanel.SummaryDataGrid.Rows[34].Cells[2].Value = "Manufacturing Efficiency Modifier";
                m_oSummaryPanel.SummaryDataGrid.Rows[35].Cells[2].Value = "Political Status Production Modifier";
                m_oSummaryPanel.SummaryDataGrid.Rows[36].Cells[2].Value = "Political Status Wealth/Trade Modifier";
                m_oSummaryPanel.SummaryDataGrid.Rows[37].Cells[2].Value = "Political Status Modifier";
            }
            catch
            {
#if LOG4NET_ENABLED
                logger.Error("Something whent wrong Creating Colums for Economics summary screen...");
#endif
            }
        }

        public void RefreshSummaryCells()
        {
            try
            {
                if (CurrentPopulation != null && m_oSummaryPanel.SummaryDataGrid.Rows.Count != 0)
                {

                    /// <summary>
                    /// Items should not be displayed if the population in question does not have them. Adjust variables control this.
                    /// </summary>
                    int Adjust1 = 0, Adjust2 = 0;

                    float TotalWorkerReq = 0.0f;

                    m_oSummaryPanel.SummaryDataGrid.ClearSelection();

                    /// <summary>
                    /// General Colony Information
                    /// </summary>
                    m_oSummaryPanel.SummaryDataGrid.Rows[0].Cells[1].Value = CurrentPopulation.PoliticalPopStatus.ToString() + " Population";
                    m_oSummaryPanel.SummaryDataGrid.Rows[1].Cells[1].Value = CurrentPopulation.Species.Name;

                    // need planetary hab rating vs species tolerance
                    double ColCost = CurrentPopulation.Species.GetTNHabRating(CurrentPopulation.Planet);

                    m_oSummaryPanel.SummaryDataGrid.Rows[2].Cells[1].Value = ColCost.ToString();
                    m_oSummaryPanel.SummaryDataGrid.Rows[3].Cells[1].Value = CurrentPopulation.AdminRating;

                    /// <summary>
                    /// Wealth Creation
                    /// </summary>
                    int Expand = CurrentFaction.FactionTechLevel[(int)Faction.FactionTechnology.ExpandCivilianEconomy];
                    if (Expand > 12)
                        Expand = 12;
                    double Wealth = CurrentPopulation.CivilianPopulation * Expand * 20.0;
                    String WealthStr = String.Format("{0:N0}", Wealth);
                    m_oSummaryPanel.SummaryDataGrid.Rows[5].Cells[1].Value = WealthStr.ToString();

                    /// <summary>
                    /// Population Breakdown
                    /// </summary>
                    String Entry = String.Format("{0:N2}m", CurrentPopulation.CivilianPopulation);
                    m_oSummaryPanel.SummaryDataGrid.Rows[7].Cells[1].Value = Entry;

                    if (CurrentPopulation.CivilianPopulation != 0.0f)
                    {
                        Entry = String.Format("   Agriculture and Enviromental ({0:N2}%)", CurrentPopulation.PopulationWorkingInAgriAndEnviro / CurrentPopulation.CivilianPopulation);
                        m_oSummaryPanel.SummaryDataGrid.Rows[8].Cells[0].Value = Entry;
                        Entry = String.Format("   Service Industries ({0:N2}%)", CurrentPopulation.PopulationWorkingInServiceIndustries / CurrentPopulation.CivilianPopulation);
                        m_oSummaryPanel.SummaryDataGrid.Rows[9].Cells[0].Value = Entry;
                        Entry = String.Format("   Manufacturing ({0:N2}%)", CurrentPopulation.PopulationWorkingInManufacturing / CurrentPopulation.CivilianPopulation);
                        m_oSummaryPanel.SummaryDataGrid.Rows[10].Cells[0].Value = Entry;
                        m_oSummaryPanel.SummaryDataGrid.Rows[11].Cells[0].Value = "Annual Growth Rate";

                        Entry = String.Format("{0:N2}m", CurrentPopulation.PopulationWorkingInAgriAndEnviro);
                        m_oSummaryPanel.SummaryDataGrid.Rows[8].Cells[1].Value = Entry;
                        Entry = String.Format("{0:N2}m", CurrentPopulation.PopulationWorkingInServiceIndustries);
                        m_oSummaryPanel.SummaryDataGrid.Rows[9].Cells[1].Value = Entry;
                        Entry = String.Format("{0:N2}m", CurrentPopulation.PopulationWorkingInManufacturing);
                        m_oSummaryPanel.SummaryDataGrid.Rows[10].Cells[1].Value = Entry;
                        Entry = String.Format("{0:N2}%", CurrentPopulation.PopulationGrowthRate);
                        m_oSummaryPanel.SummaryDataGrid.Rows[11].Cells[1].Value = Entry;

                        Adjust1 = 5;
                    }
                    /// <summary>
                    /// Infrastructure information.
                    /// </summary>
                    m_oSummaryPanel.SummaryDataGrid.Rows[8 + Adjust1].Cells[1].Value = (ColCost * 200.0).ToString();
                    m_oSummaryPanel.SummaryDataGrid.Rows[9 + Adjust1].Cells[1].Value = CurrentPopulation.Installations[(int)Installation.InstallationType.Infrastructure].Number.ToString();

                    if (ColCost != 0.0f)
                        m_oSummaryPanel.SummaryDataGrid.Rows[10 + Adjust1].Cells[1].Value = (CurrentPopulation.Installations[(int)Installation.InstallationType.Infrastructure].Number / (ColCost * 200.0)).ToString();
                    else
                        m_oSummaryPanel.SummaryDataGrid.Rows[10 + Adjust1].Cells[1].Value = "No Maximum";

                    if (CurrentPopulation.CivilianPopulation != 0.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[0].Value = "Manufacturing Sector Breakdown";

                        Adjust1++;
                    }

                    /// <summary>
                    /// Manufacturing Sector Population Usage
                    /// </summary>

                    int iShipyards = (int)(CurrentPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].Number + CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].Number);

#warning Magic numbers here for worker calculations
                    float ShipyardWorkers = 1000000.0f * iShipyards;

                    int iSlipways = 0;
                    for (int CSYIterator = 0; CSYIterator < (int)CurrentPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].Number; CSYIterator++)
                    {
                        int slips = CurrentPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].SYInfo[CSYIterator].Slipways;
                        int tons = CurrentPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].SYInfo[CSYIterator].Tonnage;
                        iSlipways = iSlipways + slips;

                        /// <summary>
                        /// Manpower requirement = 1,000,000 + num_slipways * capacity_per_slipway_in_tons * 100 / DIVISOR.  DIVISOR is 1 for military yards and 10 for commercial yards.  Thus, the flat 1,000,000 manpower required is not reduced for commercial yards, only the capacity-based component.
                        /// </summary>
                        ShipyardWorkers = ShipyardWorkers + (slips * tons * 100 / 10);
                    }

                    for (int NSYIterator = 0; NSYIterator < (int)CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].Number; NSYIterator++)
                    {
                        int slips = CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].SYInfo[NSYIterator].Slipways;
                        int tons = CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].SYInfo[NSYIterator].Tonnage;
                        iSlipways = iSlipways + CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].SYInfo[NSYIterator].Slipways;

                        /// <summary>
                        /// Manpower requirement = 1,000,000 + num_slipways * capacity_per_slipway_in_tons * 100 / DIVISOR.  DIVISOR is 1 for military yards and 10 for commercial yards.  Thus, the flat 1,000,000 manpower required is not reduced for commercial yards, only the capacity-based component.
                        /// </summary>
                        ShipyardWorkers = ShipyardWorkers + (slips * tons * 100 / 1);
                    }

                    /// <summary>
                    /// Ruins
                    /// </summary>
                    if (CurrentPopulation.Planet.PlanetaryRuins.RuinSize != Ruins.RSize.NoRuins)
                    {
#warning handle Ruins here
                        m_oSummaryPanel.SummaryDataGrid.Rows[0].Cells[2].Value = "Abandoned Installations";
                        m_oSummaryPanel.SummaryDataGrid.Rows[0].Cells[3].Value = "??";
                        m_oSummaryPanel.SummaryDataGrid.Rows[1].Cells[2].Value = "(Unknown Race - Xenologist Team Required)";
                        m_oSummaryPanel.SummaryDataGrid.Rows[1].Cells[3].Value = "";
                        Adjust2 = 3;
                    }

                    /// <summary>
                    /// Sector Command
                    /// </summary>
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.SectorCommand].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Sector Command HQ";
                        float radius = (float)Math.Floor(Math.Sqrt((double)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.SectorCommand].Number)));
                        Entry = String.Format("Level {0} (Radius {1})", Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.SectorCommand].Number), radius);
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Entry;

                        Adjust2++;
                    }

                    /// <summary>
                    /// Commercial Spaceport
                    /// </summary
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.Spaceport].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Commerical Spaceport";
                        Entry = String.Format("Level {0}", Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.Spaceport].Number));
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Entry;

                        Adjust2++;
                    }

                    /// <summary>
                    /// Military Academy
                    /// </summary
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.MilitaryAcademy].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Military Academy";
                        Entry = String.Format("Level {0}", Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.MilitaryAcademy].Number));
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Entry;

                        Adjust2++;
                    }

                    /// <summary>
                    /// Deep Space Tracking Station
                    /// </summary
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.DeepSpaceTrackingStation].Number >= 1.0f)
                    {
                        int DSTS = CurrentFaction.FactionTechLevel[(int)Faction.FactionTechnology.DSTSSensorStrength];
                        if (DSTS > Constants.Colony.DeepSpaceMax)
                            DSTS = Constants.Colony.DeepSpaceMax;

#warning if EM strength differs from Thermal, handle that here in the UI.
                        int Strength = (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.DeepSpaceTrackingStation].Number) * Constants.Colony.ThermalDeepSpaceStrength[DSTS];
                        Entry = String.Format("Deep Space Tracking Station - Strength {0}", Strength);
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = Entry;
                        Entry = String.Format("Level {0}", Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.DeepSpaceTrackingStation].Number));
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Entry;

                        Adjust2++;
                    }

                    /// <summary>
                    /// Genetic Modification Centres
                    /// </summary
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.GeneticModificationCentre].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Annual Genetic Conversion Rate";

                        float rate = 0.25f * (float)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.GeneticModificationCentre].Number);
                        Entry = String.Format("{0:N2}m", rate);
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Entry;

                        Adjust2++;
                    }

                    /// <summary>
                    /// Maintenance Facilities
                    /// </summary
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.MaintenanceFacility].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Maintenance Facility Maximum Ship Size";

                        float fac = 200 * (float)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.MaintenanceFacility].Number);
                        Entry = String.Format("{0:n} tons", fac);
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Entry;

                        Adjust2++;
                    }

                    /// <summary>
                    /// Mass Drivers
                    /// </summary
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.MassDriver].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Mass Driver Capacity";

                        float fac = 5000 * (float)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.MassDriver].Number);
                        Entry = String.Format("{0:n} tons per year", fac);
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Entry;

                        Adjust2++;
                    }

                    /// <summary>
                    /// Don't want to print this space if there is nothing above it.
                    /// </summary>
                    if (Adjust2 != 0)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "";
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = "";

                        Adjust2++;
                    }

                    /// <summary>
                    /// Shipyards
                    /// </summary>
                    if (iShipyards != 0)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[0].Value = "Shipyard Workers";
                        Entry = String.Format("{0:N2}m", (ShipyardWorkers / 1000000.0f));
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[1].Value = Entry;
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Shipyards / Slipways";
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = iShipyards.ToString() + " / " + iSlipways.ToString();

                        TotalWorkerReq = TotalWorkerReq + (ShipyardWorkers / 1000000.0f);

                        Adjust1++;
                        Adjust2++;
                    }

                    /// <summary>
                    /// Maintenance Facility Workers. This is separate from Maintenance factories above as shipyards needed to be calculated first for offset adjustment.
                    /// </summary
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.MaintenanceFacility].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[0].Value = "Maintenance Workers";

                        float workers = 0.05f * (float)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.MaintenanceFacility].Number);
                        Entry = String.Format("{0:N2}m", workers);
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[1].Value = Entry;

                        TotalWorkerReq = TotalWorkerReq + workers;

                        Adjust1++;
                    }

                    /// <summary>
                    /// Construction Factories
                    /// </summary>
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.ConstructionFactory].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[0].Value = "Construction Workers";
                        float workers = 0.05f * (float)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.ConstructionFactory].Number);

                        /// <summary>
                        /// Conventional Industry worker adjustment.
                        /// </summary>
                        if (CurrentPopulation.Installations[(int)Installation.InstallationType.ConventionalIndustry].Number >= 1.0f)
                        {
                            workers = workers + (0.05f * (float)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.ConventionalIndustry].Number));
                        }

                        TotalWorkerReq = TotalWorkerReq + workers;

                        Entry = String.Format("{0:N2}m", workers);
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[1].Value = Entry;

                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Construction Factories";
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.ConstructionFactory].Number).ToString();

                        Adjust1++;
                        Adjust2++;
                    }

                    /// <summary>
                    /// Conventional Industry
                    /// </summary>
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.ConventionalIndustry].Number >= 1.0f)
                    {
                        if (CurrentPopulation.Installations[(int)Installation.InstallationType.ConstructionFactory].Number < 1.0f)
                        {
                            m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[0].Value = "Construction Workers";
                            float workers = 0.05f * (float)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.ConventionalIndustry].Number);
                            Entry = String.Format("{0:N2}m", workers);
                            m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[1].Value = Entry;

                            TotalWorkerReq = TotalWorkerReq + workers;

                            Adjust1++;
                        }

                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Conventional Industry";
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.ConventionalIndustry].Number).ToString();

                        Adjust2++;
                    }


                    /// <summary>
                    /// Ordnance Factories
                    /// </summary>
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.OrdnanceFactory].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[0].Value = "Ordnance Factory Workers";
                        float workers = 0.05f * (float)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.OrdnanceFactory].Number);

                        TotalWorkerReq = TotalWorkerReq + workers;

                        Entry = String.Format("{0:N2}m", workers);
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[1].Value = Entry;

                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Ordnance Factories";
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.OrdnanceFactory].Number).ToString();

                        Adjust1++;
                        Adjust2++;
                    }

                    /// <summary>
                    /// Fighter Factories
                    /// </summary>
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.FighterFactory].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[0].Value = "Fighter Factory Workers";
                        float workers = 0.05f * (float)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.FighterFactory].Number);

                        TotalWorkerReq = TotalWorkerReq + workers;

                        Entry = String.Format("{0:N2}m", workers);
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[1].Value = Entry;

                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Fighter Factories";
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.FighterFactory].Number).ToString();

                        Adjust1++;
                        Adjust2++;
                    }

                    /// <summary>
                    /// Refineries
                    /// </summary>
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.FuelRefinery].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[0].Value = "Fuel Refinery Workers";
                        float workers = 0.05f * (float)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.FuelRefinery].Number);

                        TotalWorkerReq = TotalWorkerReq + workers;

                        Entry = String.Format("{0:N2}m", workers);
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[1].Value = Entry;

                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Ordnance Factories";
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.FuelRefinery].Number).ToString();

                        Adjust1++;
                        Adjust2++;
                    }

                    /// <summary>
                    /// Financial Centre Workers
                    /// </summary>
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.FinancialCentre].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[0].Value = "Financial Centre Workers";

                        float workers = 0.05f * (float)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.FinancialCentre].Number);

                        TotalWorkerReq = TotalWorkerReq + workers;

                        Entry = String.Format("{0:N2}m", workers);
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[1].Value = Entry;

                        Adjust1++;
                    }

                    /// <summary>
                    /// mines
                    /// </summary>
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.Mine].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[0].Value = "Mine Workers";
                        float workers = 0.05f * (float)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.Mine].Number);

                        TotalWorkerReq = TotalWorkerReq + workers;

                        Entry = String.Format("{0:N2}m", workers);
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[1].Value = Entry;

                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Mines";
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.Mine].Number).ToString();

                        Adjust1++;
                        Adjust2++;
                    }

                    /// <summary>
                    /// automines
                    /// </summary>
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.AutomatedMine].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Automated Mines";
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.AutomatedMine].Number).ToString();

                        Adjust2++;
                    }

                    /// <summary>
                    /// Mass Drivers
                    /// </summary>
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.MassDriver].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Mass Drivers";
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.MassDriver].Number).ToString();

                        Adjust2++;
                    }

                    /// <summary>
                    /// Terraformers
                    /// </summary>
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.TerraformingInstallation].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[0].Value = "Terraformers";
                        float workers = 0.25f * (float)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.TerraformingInstallation].Number);

                        TotalWorkerReq = TotalWorkerReq + workers;

                        Entry = String.Format("{0:N2}m", workers);
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[1].Value = Entry;

                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Terraforming Installation";
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.TerraformingInstallation].Number).ToString();

                        Adjust1++;
                        Adjust2++;
                    }

                    /// <summary>
                    /// Research labs
                    /// </summary>
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.ResearchLab].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[0].Value = "Scientists";
                        float workers = 1.0f * (float)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.ResearchLab].Number);

                        TotalWorkerReq = TotalWorkerReq + workers;

                        Entry = String.Format("{0:N2}m", workers);
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[1].Value = Entry;

                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Research Labs";
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.ResearchLab].Number).ToString();

                        Adjust1++;
                        Adjust2++;
                    }

                    /// <summary>
                    /// Available workers
                    /// </summary>
                    if (CurrentPopulation.CivilianPopulation != 0.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[0].Value = "Available Workers";
                        Entry = String.Format("{0:N2}", (CurrentPopulation.PopulationWorkingInManufacturing - TotalWorkerReq));
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[1].Value = Entry;

                        Adjust1++;
                    }

                    /// <summary>
                    /// Protection
                    /// </summary>
                    if (CurrentPopulation.CivilianPopulation >= 10.0f)
                    {

                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[0].Value = "";
                        m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[1].Value = "";
                        m_oSummaryPanel.SummaryDataGrid.Rows[13 + Adjust1].Cells[0].Value = "Requested Protection Level";
                        Entry = String.Format("{0:N0}", Math.Round(CurrentPopulation.CivilianPopulation / 5.5f));
                        m_oSummaryPanel.SummaryDataGrid.Rows[13 + Adjust1].Cells[1].Value = Entry;
                        m_oSummaryPanel.SummaryDataGrid.Rows[14 + Adjust1].Cells[0].Value = "Actual Protection Level";
                        m_oSummaryPanel.SummaryDataGrid.Rows[14 + Adjust1].Cells[1].Value = CurrentPopulation.Planet.Position.System.GetProtectionLevel(CurrentFaction).ToString();
                        Adjust1 = Adjust1 + 3;
                    }

                    /// <summary>
                    /// Planetary Geology
                    /// </summary>
                    m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[0].Value = "";
                    m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[1].Value = "";
                    m_oSummaryPanel.SummaryDataGrid.Rows[13 + Adjust1].Cells[0].Value = "Tectonics";
                    m_oSummaryPanel.SummaryDataGrid.Rows[13 + Adjust1].Cells[1].Value = null; //CurrentPopulation.SystemBody.PlanetaryTectonics;
                    if (CurrentPopulation.Planet.GeoSurveyList.ContainsKey(CurrentFaction) == true)
                    {
                        Entry = "Completed";
                    }
                    else
                    {
                        Entry = "No";
                    }
                    m_oSummaryPanel.SummaryDataGrid.Rows[14 + Adjust1].Cells[0].Value = "Geological Team Survey Completed";
                    m_oSummaryPanel.SummaryDataGrid.Rows[14 + Adjust1].Cells[1].Value = Entry;

                    for (int rowIterator = (15 + Adjust1); rowIterator < 38; rowIterator++)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[rowIterator].Cells[0].Value = "";
                        m_oSummaryPanel.SummaryDataGrid.Rows[rowIterator].Cells[1].Value = "";
                    }

                    /// <summary>
                    /// Genetic Modification Centers
                    /// </summary>
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.GeneticModificationCentre].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Genetic Modification Centers";
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.GeneticModificationCentre].Number).ToString();

                        Adjust2++;
                    }

                    /// <summary>
                    /// Financial Centres
                    /// </summary>
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.FinancialCentre].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Financial Centre";
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.FinancialCentre].Number).ToString();

                        Adjust2++;
                    }

                    /// <summary>
                    /// Ground Forces Training Facilities
                    /// </summary>
                    if (CurrentPopulation.Installations[(int)Installation.InstallationType.GroundForceTrainingFacility].Number >= 1.0f)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Ground Force Training Facilities";
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.GroundForceTrainingFacility].Number).ToString();

                        Adjust2++;
                    }

                    /// <summary>
                    /// Don't want to print this space if there is nothing above it.
                    /// </summary>
                    if (Adjust2 != 0 && (string)m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value == "")
                    {
                        Adjust2++;
                    }
                    else if (Adjust2 != 0)// && (string)m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value != "")
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "";
                        m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value = "";

                        Adjust2++;
                    }
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Fuel Available";
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2++].Cells[3].Value = CurrentPopulation.FuelStockpile.ToString();
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Maintenance Supplies Available";
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2++].Cells[3].Value = CurrentPopulation.MaintenanceSupplies.ToString();
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "";
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2++].Cells[3].Value = "";

                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Thermal Signature of Colony";
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2++].Cells[3].Value = CurrentPopulation.ThermalSignature.ToString();
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "EM Signature of Colony";
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2++].Cells[3].Value = CurrentPopulation.EMSignature.ToString();
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "";
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2++].Cells[3].Value = "";
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Economic Production Modifier";
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2++].Cells[3].Value = (CurrentPopulation.ModifierEconomicProduction * 100).ToString() + "%";
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Manufacturing Efficiency Modifier";
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2++].Cells[3].Value = (CurrentPopulation.ModifierManfacturing * 100).ToString() + "%";
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Political Status Production Modifier";
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2++].Cells[3].Value = (CurrentPopulation.ModifierProduction * 100).ToString() + "%";
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Political Status Wealth/Trade Modifier";
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2++].Cells[3].Value = (CurrentPopulation.ModifierWealthAndTrade * 100).ToString() + "%";
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value = "Political Status Modifier";
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2++].Cells[3].Value = (CurrentPopulation.ModifierPoliticalStability * 100).ToString() + "%";

                    for (int rowIterator = Adjust2; rowIterator < 38; rowIterator++)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Rows[rowIterator].Cells[2].Value = "";
                        m_oSummaryPanel.SummaryDataGrid.Rows[rowIterator].Cells[3].Value = "";
                    }
                }
            }
            catch
            {
#if LOG4NET_ENABLED
                String LoggerEntry = String.Format("Ran into an error in RefreshSummaryCells.");
                logger.Error(LoggerEntry);
#endif
            }
        }
        #endregion

        #region Industrial Tab
        /// <summary>
        /// Puts all the strings in the Installation combo box.
        /// </summary>
        private void BuildConstructionComboBox()
        {

            m_oSummaryPanel.InstallationTypeComboBox.Items.Clear();
            foreach (String Const in UIConstants.EconomicsPage.ConstructionTypes)
            {
                m_oSummaryPanel.InstallationTypeComboBox.Items.Add(Const);
            }

            m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex = 0;

            int row = 0;
            for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
            {
                m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
            }

        }

        /// <summary>
        /// Refresh industrial tab updates the display for the various industrial tab items.
        /// </summary>
        private void RefreshIndustryTab()
        {
#warning Add an entry for Unused Construction, Unused Fighter Capacity, and Unused Ordnance Capacity here.
            /// <summary>
            /// Clear the dictionary for the build list grid
            /// </summary>
            BuildLocationDict.Clear();
            BuildLocationDisplayDict.Clear();

            /// <summary>
            /// Check if there is a valid selected index.
            /// </summary>
            if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex != -1)
            {
                #region Installations
                /// <summary>
                /// Check the selected index against the construction ID for the build grid.
                /// </summary>
                if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.Installations)
                {
                    int row = 0;
                    /// <summary>
                    /// Loop through every installation.
                    /// </summary>
                    foreach (Installation Install in CurrentFaction.InstallationTypes)
                    {
                        /// <summary>
                        /// can this installation be built?
                        /// </summary>
                        if (Install.IsBuildable(CurrentFaction, CurrentPopulation) == true)
                        {
                            /// <summary>
                            /// do we not need to add a row?
                            /// </summary>
                            if (row < BuildTabMaxRows)
                            {
                                /// <summary>
                                /// add to the build location dictionary and print to the build data grid.
                                /// </summary>
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Installation, Install);
                                BuildLocationDisplayDict.Add(Install.Id, Install.Name);
                                BuildLocationDict.Add(Install.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Install.Name;
                                row++;

                            }
                            else
                            {
                                /// <summary>
                                /// create a row.
                                /// </summary>
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Installation, Install);
                                BuildLocationDisplayDict.Add(Install.Id, Install.Name);
                                BuildLocationDict.Add(Install.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Install.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }

                    }

                    /// <summary>
                    /// Set all unused rows to not visible.
                    /// </summary>
                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
                }
                #endregion
                #region Missiles
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.Missiles)
                {
                    int row = 0;
                    foreach (OrdnanceDefTN Missile in CurrentFaction.ComponentList.MissileDef)
                    {
                        if (Missile.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Missile, Missile);
                                BuildLocationDisplayDict.Add(Missile.Id, Missile.Name);
                                BuildLocationDict.Add(Missile.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Missile.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Missile, Missile);
                                BuildLocationDisplayDict.Add(Missile.Id, Missile.Name);
                                BuildLocationDict.Add(Missile.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Missile.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
                }
                #endregion
                #region Fighters
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.Fighters)
                {
                    int row = 0;
                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
#warning do fighter list here.
                }
                #endregion
                #region Basic Components
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.BasicComponents)
                {
                    int row = 0;
                    foreach (GeneralComponentDefTN Crew in CurrentFaction.ComponentList.CrewQuarters)
                    {
                        if (Crew.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Crew);
                                BuildLocationDisplayDict.Add(Crew.Id, Crew.Name);
                                BuildLocationDict.Add(Crew.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Crew.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Crew);
                                BuildLocationDisplayDict.Add(Crew.Id, Crew.Name);
                                BuildLocationDict.Add(Crew.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Crew.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    foreach (GeneralComponentDefTN Fuel in CurrentFaction.ComponentList.FuelStorage)
                    {
                        if (Fuel.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Fuel);
                                BuildLocationDisplayDict.Add(Fuel.Id, Fuel.Name);
                                BuildLocationDict.Add(Fuel.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Fuel.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Fuel);
                                BuildLocationDisplayDict.Add(Fuel.Id, Fuel.Name);
                                BuildLocationDict.Add(Fuel.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Fuel.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    foreach (GeneralComponentDefTN EBay in CurrentFaction.ComponentList.EngineeringSpaces)
                    {
                        if (EBay.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, EBay);
                                BuildLocationDisplayDict.Add(EBay.Id, EBay.Name);
                                BuildLocationDict.Add(EBay.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = EBay.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, EBay);
                                BuildLocationDisplayDict.Add(EBay.Id, EBay.Name);
                                BuildLocationDict.Add(EBay.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = EBay.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    foreach (GeneralComponentDefTN Other in CurrentFaction.ComponentList.OtherComponents)
                    {
                        if (Other.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Other);
                                BuildLocationDisplayDict.Add(Other.Id, Other.Name);
                                BuildLocationDict.Add(Other.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Other.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Other);
                                BuildLocationDisplayDict.Add(Other.Id, Other.Name);
                                BuildLocationDict.Add(Other.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Other.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
                }
                #endregion
                #region Electronics and Shields
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.ElectronicShieldComponents)
                {
                    int row = 0;
                    foreach (ShieldDefTN Shield in CurrentFaction.ComponentList.ShieldDef)
                    {
                        if (Shield.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Shield);
                                BuildLocationDisplayDict.Add(Shield.Id, Shield.Name);
                                BuildLocationDict.Add(Shield.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Shield.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Shield);
                                BuildLocationDisplayDict.Add(Shield.Id, Shield.Name);
                                BuildLocationDict.Add(Shield.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Shield.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
                }
                #endregion
                #region Engines and Jump Engines
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.EngineComponents)
                {
                    int row = 0;

                    /// <summary>
                    /// loop through each engine.
                    /// </summary>
                    foreach (EngineDefTN Engine in CurrentFaction.ComponentList.Engines)
                    {
                        /// <summary>
                        /// Don't display obsolete engines.
                        /// </summary>
                        if (Engine.isObsolete == false)
                        {
                            /// <summary>
                            /// if current row is less than the current max row count set the row to visible and put the name of the engine component into the cell.
                            /// </summary>
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Engine);
                                BuildLocationDisplayDict.Add(Engine.Id, Engine.Name);
                                BuildLocationDict.Add(Engine.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Engine.Name;
                                row++;

                            }
                            /// <summary>
                            /// add a new row, and increment the current max row count.
                            /// </summary>
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Engine);
                                BuildLocationDisplayDict.Add(Engine.Id, Engine.Name);
                                BuildLocationDict.Add(Engine.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Engine.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    /// <summary>
                    /// Iterate through the Jump engines.
                    foreach (JumpEngineDefTN JumpEngine in CurrentFaction.ComponentList.JumpEngineDef)
                    {
                        /// <summary>
                        /// No obsolete components.
                        /// </summary>
                        if (JumpEngine.isObsolete == false)
                        {
                            /// <summary>
                            /// if current row is less than the current max row count set the row to visible and put the name of the engine component into the cell.
                            /// </summary>
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, JumpEngine);
                                BuildLocationDisplayDict.Add(JumpEngine.Id, JumpEngine.Name);
                                BuildLocationDict.Add(JumpEngine.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = JumpEngine.Name;
                                row++;

                            }
                        }
                        /// <summary>
                        /// add a new row, and increment the current max row count.
                        /// </summary>
                        else
                        {
                            using (DataGridViewRow Row = new DataGridViewRow())
                            {
                                // setup row height. note that by default they are 22 pixels in height!
                                Row.Height = 17;
                                m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                            }// make new rows and add items.

                            BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, JumpEngine);
                            BuildLocationDisplayDict.Add(JumpEngine.Id, JumpEngine.Name);
                            BuildLocationDict.Add(JumpEngine.Id, Temp);

                            m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                            m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = JumpEngine.Name;
                            row++;
                            BuildTabMaxRows++;
                        }
                    }

                    /// <summary>
                    /// set all other rows to not visible
                    /// </summary>
                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
                }
                #endregion
                #region Sensors and Fire Controls
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.SensorsFCComponents)
                {
                    int row = 0;
                    foreach (PassiveSensorDefTN Passive in CurrentFaction.ComponentList.PassiveSensorDef)
                    {
                        if (Passive.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Passive);
                                BuildLocationDisplayDict.Add(Passive.Id, Passive.Name);
                                BuildLocationDict.Add(Passive.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Passive.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Passive);
                                BuildLocationDisplayDict.Add(Passive.Id, Passive.Name);
                                BuildLocationDict.Add(Passive.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Passive.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    foreach (SurveySensorDefTN Survey in CurrentFaction.ComponentList.SurveySensorDef)
                    {
                        if (Survey.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Survey);
                                BuildLocationDisplayDict.Add(Survey.Id, Survey.Name);
                                BuildLocationDict.Add(Survey.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Survey.Name;
                                row++;
                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Survey);
                                BuildLocationDisplayDict.Add(Survey.Id, Survey.Name);
                                BuildLocationDict.Add(Survey.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Survey.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                        
                    }

                    foreach (ActiveSensorDefTN Active in CurrentFaction.ComponentList.ActiveSensorDef)
                    {
                        if (Active.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Active);
                                BuildLocationDisplayDict.Add(Active.Id, Active.Name);
                                BuildLocationDict.Add(Active.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Active.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Active);
                                BuildLocationDisplayDict.Add(Active.Id, Active.Name);
                                BuildLocationDict.Add(Active.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Active.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }
                    foreach (BeamFireControlDefTN BFC in CurrentFaction.ComponentList.BeamFireControlDef)
                    {
                        if (BFC.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, BFC);
                                BuildLocationDisplayDict.Add(BFC.Id, BFC.Name);
                                BuildLocationDict.Add(BFC.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = BFC.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, BFC);
                                BuildLocationDisplayDict.Add(BFC.Id, BFC.Name);
                                BuildLocationDict.Add(BFC.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = BFC.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    foreach (ActiveSensorDefTN MFC in CurrentFaction.ComponentList.MissileFireControlDef)
                    {
                        if (MFC.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, MFC);
                                BuildLocationDisplayDict.Add(MFC.Id, MFC.Name);
                                BuildLocationDict.Add(MFC.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = MFC.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, MFC);
                                BuildLocationDisplayDict.Add(MFC.Id, MFC.Name);
                                BuildLocationDict.Add(MFC.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = MFC.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
                }
                #endregion
                #region Transport and Industry
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.TransportIndustryComponents)
                {
                    int row = 0;
                    foreach (CargoDefTN Hold in CurrentFaction.ComponentList.CargoHoldDef)
                    {
                        if (Hold.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Hold);
                                BuildLocationDisplayDict.Add(Hold.Id, Hold.Name);
                                BuildLocationDict.Add(Hold.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Hold.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Hold);
                                BuildLocationDisplayDict.Add(Hold.Id, Hold.Name);
                                BuildLocationDict.Add(Hold.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Hold.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    foreach (ColonyDefTN Bay in CurrentFaction.ComponentList.ColonyBayDef)
                    {
                        if (Bay.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Bay);
                                BuildLocationDisplayDict.Add(Bay.Id, Bay.Name);
                                BuildLocationDict.Add(Bay.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Bay.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Bay);
                                BuildLocationDisplayDict.Add(Bay.Id, Bay.Name);
                                BuildLocationDict.Add(Bay.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Bay.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    foreach (CargoHandlingDefTN CHS in CurrentFaction.ComponentList.CargoHandleSystemDef)
                    {
                        if (CHS.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, CHS);
                                BuildLocationDisplayDict.Add(CHS.Id, CHS.Name);
                                BuildLocationDict.Add(CHS.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = CHS.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, CHS);
                                BuildLocationDisplayDict.Add(CHS.Id, CHS.Name);
                                BuildLocationDict.Add(CHS.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = CHS.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
                }
                #endregion
                #region Weapon and Support Components
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.WeaponsSupportComponents)
                {
                    int row = 0;
                    foreach (BeamDefTN Beam in CurrentFaction.ComponentList.BeamWeaponDef)
                    {
                        if (Beam.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Beam);
                                BuildLocationDisplayDict.Add(Beam.Id, Beam.Name);
                                BuildLocationDict.Add(Beam.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Beam.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Beam);
                                BuildLocationDisplayDict.Add(Beam.Id, Beam.Name);
                                BuildLocationDict.Add(Beam.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Beam.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    foreach (TurretDefTN Turret in CurrentFaction.ComponentList.TurretDef)
                    {
                        if (Turret.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Turret);
                                BuildLocationDisplayDict.Add(Turret.Id, Turret.Name);
                                BuildLocationDict.Add(Turret.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Turret.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Turret);
                                BuildLocationDisplayDict.Add(Turret.Id, Turret.Name);
                                BuildLocationDict.Add(Turret.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Turret.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    foreach (CIWSDefTN CIWS in CurrentFaction.ComponentList.CIWSDef)
                    {
                        if (CIWS.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, CIWS);
                                BuildLocationDisplayDict.Add(CIWS.Id, CIWS.Name);
                                BuildLocationDict.Add(CIWS.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = CIWS.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, CIWS);
                                BuildLocationDisplayDict.Add(CIWS.Id, CIWS.Name);
                                BuildLocationDict.Add(CIWS.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = CIWS.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    foreach (MissileLauncherDefTN Tube in CurrentFaction.ComponentList.MLauncherDef)
                    {
                        if (Tube.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Tube);
                                BuildLocationDisplayDict.Add(Tube.Id, Tube.Name);
                                BuildLocationDict.Add(Tube.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Tube.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Tube);
                                BuildLocationDisplayDict.Add(Tube.Id, Tube.Name);
                                BuildLocationDict.Add(Tube.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Tube.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    foreach (ReactorDefTN Reactor in CurrentFaction.ComponentList.ReactorDef)
                    {
                        if (Reactor.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Reactor);
                                BuildLocationDisplayDict.Add(Reactor.Id, Reactor.Name);
                                BuildLocationDict.Add(Reactor.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Reactor.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Reactor);
                                BuildLocationDisplayDict.Add(Reactor.Id, Reactor.Name);
                                BuildLocationDict.Add(Reactor.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Reactor.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    foreach (MagazineDefTN Mag in CurrentFaction.ComponentList.MagazineDef)
                    {
                        if (Mag.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Mag);
                                BuildLocationDisplayDict.Add(Mag.Id, Mag.Name);
                                BuildLocationDict.Add(Mag.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Mag.Name;
                                row++;

                            }
                            else
                            {
                                using (DataGridViewRow Row = new DataGridViewRow())
                                {
                                    // setup row height. note that by default they are 22 pixels in height!
                                    Row.Height = 17;
                                    m_oSummaryPanel.BuildDataGrid.Rows.Add(Row);
                                }// make new rows and add items.

                                BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.Component, Mag);
                                BuildLocationDisplayDict.Add(Mag.Id, Mag.Name);
                                BuildLocationDict.Add(Mag.Id, Temp);

                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Mag.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
                        }
                    }

                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
                }
                #endregion
                #region Build PDC
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.BuildPDCOrbitalHabitat)
                {
                    int row = 0;
                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
#warning do Industrial PDC orbhab build here
                }
                #endregion
                #region Prefab PDC
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.PrefabPDC)
                {
                    int row = 0;
                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
#warning do Industrial PDC Prefab here
                }
                #endregion
                #region Assemble PDC
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.AssemblePDC)
                {
                    int row = 0;
                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
#warning do Industrial PDC assembly here
                }
                #endregion
                #region Refit PDC
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.RefitPDC)
                {
                    int row = 0;
                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
#warning do Industrial PDC refit here
                }
                #endregion
                #region Maintenance Supplies
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.MaintenanceSupplies)
                {
                    /// <summary>
                    /// Maintenance supplies don't have a game entity, so this is a kludge to make that work within the confines of the existing build location dictionary.
                    /// </summary>
                    GameEntity Placeholder = new GameEntity();
                    Placeholder.Id = Guid.NewGuid();
                    BuildListObject Temp = new BuildListObject(BuildListObject.ListEntityType.MaintenanceSupplies, Placeholder);
                    BuildLocationDisplayDict.Add(Placeholder.Id, "Maintenance Supplies");
                    BuildLocationDict.Add(Placeholder.Id, Temp);

                    m_oSummaryPanel.BuildDataGrid.Rows[0].Visible = true;
                    m_oSummaryPanel.BuildDataGrid.Rows[0].Cells[0].Value = "Maintenance Supplies";

                    for (int RowIterator = 1; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
                }
                #endregion
            }

            BuildCostListBox();

            BuildStockListBoxes();

            Build_BuildQueue();

            BuildConstructionLabel();
            BuildRefiningLabel();

            UpdateBuildTexts();

            if (CurrentPopulation != null)
            {
                if (CurrentPopulation.IsRefining == false)
                {
                    m_oSummaryPanel.StopRefiningButton.Enabled = false;
                    m_oSummaryPanel.StartRefiningButton.Enabled = true;
                }
                else
                {
                    m_oSummaryPanel.StopRefiningButton.Enabled = true;
                    m_oSummaryPanel.StartRefiningButton.Enabled = false;
                }
            }
        }

        /// <summary>
        /// This listbox handles the cost of the selected item to be built.
        /// </summary>
        public void BuildCostListBox()
        {

            m_oSummaryPanel.InstallationCostListBox.Items.Clear();

            if (m_oSummaryPanel.BuildDataGrid.CurrentCell != null)
            {
                if (m_oSummaryPanel.BuildDataGrid.CurrentCell.RowIndex != -1)
                {
                    List<Guid> GID = BuildLocationDisplayDict.Keys.ToList();
                    String CostString = "N/A";
                    switch (BuildLocationDict[GID[m_oSummaryPanel.BuildDataGrid.CurrentCell.RowIndex]].EntityType)
                    {
                        case BuildListObject.ListEntityType.Installation:
                            Installation Install = BuildLocationDict[GID[m_oSummaryPanel.BuildDataGrid.CurrentCell.RowIndex]].Entity as Installation;
                            CostString = String.Format("Cost: {0:N2}", Install.Cost);
                            m_oSummaryPanel.InstallationCostListBox.Items.Add(CostString);
                            for (int MineralIterator = 0; MineralIterator < Constants.Minerals.NO_OF_MINERIALS; MineralIterator++)
                            {
                                if (Install.MinerialsCost[MineralIterator] != 0)
                                {
                                    string FormattedMineralTotal = CurrentPopulation.Minerials[MineralIterator].ToString("#,##0");

                                    CostString = String.Format("{0:N4} x {1} ({2})", Install.MinerialsCost[MineralIterator], (Constants.Minerals.MinerialNames)MineralIterator, FormattedMineralTotal);
                                    m_oSummaryPanel.InstallationCostListBox.Items.Add(CostString);
                                }
                            }
                            break;
                        case BuildListObject.ListEntityType.Missile:
                            OrdnanceDefTN Missile = BuildLocationDict[GID[m_oSummaryPanel.BuildDataGrid.CurrentCell.RowIndex]].Entity as OrdnanceDefTN;
                            CostString = String.Format("Cost: {0:N2}", Missile.cost);
                            m_oSummaryPanel.InstallationCostListBox.Items.Add(CostString);
                            for (int MineralIterator = 0; MineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; MineralIterator++)
                            {
                                if (Missile.minerialsCost[MineralIterator] != 0.0m)
                                {
                                    string FormattedMineralTotal = CurrentPopulation.Minerials[MineralIterator].ToString("#,##0");
                                    CostString = String.Format("{0:N4} x {1} ({2})", Missile.minerialsCost[MineralIterator], (Constants.Minerals.MinerialNames)MineralIterator, FormattedMineralTotal);
                                    m_oSummaryPanel.InstallationCostListBox.Items.Add(CostString);
                                }
                            }
                            if (Missile.fuelCost != 0.0f)
                            {
                                float fuel = 0.0f;
                                string FormattedFuelTotal = "";
                                if (CurrentPopulation.FuelStockpile > 1000000)
                                {
                                    fuel = CurrentPopulation.FuelStockpile / 1000000.0f;
                                    FormattedFuelTotal = String.Format("{0:N2}m", fuel);
                                }
                                else if (CurrentPopulation.FuelStockpile > 100000)
                                {
                                    fuel = CurrentPopulation.FuelStockpile / 100000.0f;
                                    FormattedFuelTotal = String.Format("{0:N2}k", fuel);
                                }
                                else
                                {
                                    FormattedFuelTotal = CurrentPopulation.FuelStockpile.ToString("#,##0");
                                }
                                CostString = String.Format("Fuel x{0} ({1})\n", Math.Floor(Missile.fuelCost), FormattedFuelTotal);
                                m_oSummaryPanel.InstallationCostListBox.Items.Add(CostString);
                            }
                            break;
                        case BuildListObject.ListEntityType.Fighter:
                            break;
                        case BuildListObject.ListEntityType.Component:
                            ComponentDefTN Component = BuildLocationDict[GID[m_oSummaryPanel.BuildDataGrid.CurrentCell.RowIndex]].Entity as ComponentDefTN;
                            CostString = String.Format("Cost: {0:N2}", Component.cost);
                            m_oSummaryPanel.InstallationCostListBox.Items.Add(CostString);
                            for (int MineralIterator = 0; MineralIterator < Constants.Minerals.NO_OF_MINERIALS; MineralIterator++)
                            {
                                if (Component.minerialsCost[MineralIterator] != 0)
                                {
                                    string FormattedMineralTotal = CurrentPopulation.Minerials[MineralIterator].ToString("#,##0");

                                    CostString = String.Format("{0:N4} x {1} ({2})", Component.minerialsCost[MineralIterator], (Constants.Minerals.MinerialNames)MineralIterator, FormattedMineralTotal);
                                    m_oSummaryPanel.InstallationCostListBox.Items.Add(CostString);
                                }
                            }
                            break;
                        case BuildListObject.ListEntityType.PDC_Build:
                            break;
                        case BuildListObject.ListEntityType.PDC_Prefab:
                            break;
                        case BuildListObject.ListEntityType.PDC_Assemble:
                            break;
                        case BuildListObject.ListEntityType.PDC_Refit:
                            break;
                        case BuildListObject.ListEntityType.MaintenanceSupplies:
                            CostString = String.Format("Cost: {0:N2}", Constants.Colony.MaintenanceSupplyCost);
                            m_oSummaryPanel.InstallationCostListBox.Items.Add(CostString);
                            for (int MineralIterator = 0; MineralIterator < Constants.Minerals.NO_OF_MINERIALS; MineralIterator++)
                            {
                                if (Constants.Colony.MaintenanceMineralCost[MineralIterator] != 0.0m)
                                {
                                    string FormattedMineralTotal = CurrentPopulation.Minerials[MineralIterator].ToString("#,##0");

                                    CostString = String.Format("{0:N4} x {1} ({2})", Constants.Colony.MaintenanceMineralCost[MineralIterator],
                                                                                     (Constants.Minerals.MinerialNames)MineralIterator,
                                                                                     FormattedMineralTotal);
                                    m_oSummaryPanel.InstallationCostListBox.Items.Add(CostString);
                                }
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// BuildStockListBoxes builds the Missile and component stockpile listboxes, and will later build the PDC and fighter listboxes.
        /// </summary>
        private void BuildStockListBoxes()
        {
            m_oSummaryPanel.ShipCompListBox.Items.Clear();
            m_oSummaryPanel.MissileStockListBox.Items.Clear();

            /// <summary>
            /// Must have done this one before learning of dictionaries.
            /// </summary>
            for (int componentIterator = 0; componentIterator < CurrentPopulation.ComponentStockpile.Count; componentIterator++)
            {
                ComponentDefTN CurrentComponent = CurrentPopulation.ComponentStockpile[componentIterator];
                float ComponentCount = CurrentPopulation.ComponentStockpileCount[componentIterator];
                String Entry = String.Format("{0:N4}x {1}", ComponentCount, CurrentComponent);
                m_oSummaryPanel.ShipCompListBox.Items.Add(Entry);
            }

            foreach (KeyValuePair<OrdnanceDefTN, float> MissilePair in CurrentPopulation.MissileStockpile)
            {
                String Entry = String.Format("{0:N4}x {1}", MissilePair.Value, MissilePair.Key.Name);
                m_oSummaryPanel.MissileStockListBox.Items.Add(Entry);
            }

#warning do PDC and fighter listboxes here.
        }

        /// <summary>
        /// This will determine estimated completion dates as well as other things.
        /// </summary>
        private void Build_BuildQueue()
        {
#warning fighters and PDCs not yet handled here.
            try
            {
                if (CurrentPopulation != null && m_oSummaryPanel.ConstructionDataGrid.Rows.Count != 0)
                {
                    float BuildPercentage = 0.0f;

                    m_oSummaryPanel.ConstructionDataGrid.Rows[0].Cells[0].Value = "";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[0].Cells[1].Value = "";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[0].Cells[2].Value = "";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[0].Cells[3].Value = "C Fact";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[0].Cells[4].Value = "";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[0].Cells[5].Value = "";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[0].Cells[6].Value = "";
                    int CurrentRow = 1;
                    int QueueNum = 1;

                    foreach (ConstructionBuildQueueItem CBQ in CurrentPopulation.ConstructionBuildQueue)
                    {
                        if (CurrentRow == ConstructionTabMaxRows)
                        {
                            using (DataGridViewRow row = new DataGridViewRow())
                            {
                                // setup row height. note that by default they are 22 pixels in height!
                                row.Height = 17;
                                m_oSummaryPanel.ConstructionDataGrid.Rows.Add(row);
                            }

                            ConstructionTabMaxRows++;
                        }

                        switch (CBQ.buildType)
                        {
                            case ConstructionBuildQueueItem.CBType.PlanetaryInstallation:
                                m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[0].Value = CBQ.installationBuild.Name;
                                break;
                            case ConstructionBuildQueueItem.CBType.ShipComponent:
                                m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[0].Value = CBQ.componentBuild.Name;
                                break;
                            case ConstructionBuildQueueItem.CBType.MaintenanceSupplies:
                                m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[0].Value = "Maintenance Supplies";
                                break;
                        }

                        m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[1].Value = String.Format("{0:N2}", CBQ.numToBuild);
                        m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[2].Value = String.Format("{0:N2}", CBQ.buildCapacity);

                        float DevotedToThis = (CBQ.buildCapacity / 100.0f) * CurrentPopulation.CalcTotalIndustry();

                        m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[3].Value = String.Format("{0:N2}", DevotedToThis);
                        m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[4].Value = String.Format("{0:N2}", CBQ.costPerItem);


                        if ((BuildPercentage + CBQ.buildCapacity) <= 100.0f)
                        {
                            /// <summary>
                            /// nothing is calculated here, but this logic determines how date should be presented.
                            /// </summary>
                            BuildPercentage = BuildPercentage + CBQ.buildCapacity;
                            float BPRequirement = (float)Math.Floor(CBQ.numToBuild) * (float)CBQ.costPerItem;
                            float YearsOfProduction = (BPRequirement / DevotedToThis);

                            if (DevotedToThis == 0.0f || YearsOfProduction > Constants.Colony.TimerYearMax)
                            {
                                m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[5].Value = "-";
                            }
                            else
                            {
                                m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[5].Value = CBQ.completionDate.ToShortDateString();
                            }

                            //this item is being built
                            if (CBQ.inProduction == true)
                            {
                                m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[6].Value = "No";
                            }
                            else
                            {
                                m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[6].Value = "Paused";
                            }
                        }
                        else
                        {
                            //this item is in the queue.
                            m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[5].Value = "-";
                            m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[6].Value = String.Format("Queue-{0}", QueueNum);
                            QueueNum++;
                        }

                        CurrentRow++;
                    }

                    if (CurrentRow == ConstructionTabMaxRows)
                    {
                        using (DataGridViewRow row = new DataGridViewRow())
                        {
                            // setup row height. note that by default they are 22 pixels in height!
                            row.Height = 17;
                            m_oSummaryPanel.ConstructionDataGrid.Rows.Add(row);
                        }

                        ConstructionTabMaxRows++;
                    }

                    m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[0].Value = "";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[1].Value = "";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[2].Value = "";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[3].Value = "";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[4].Value = "";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[5].Value = "";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[6].Value = "";
                    CurrentRow++;

                    if (CurrentRow == ConstructionTabMaxRows)
                    {
                        using (DataGridViewRow row = new DataGridViewRow())
                        {
                            // setup row height. note that by default they are 22 pixels in height!
                            row.Height = 17;
                            m_oSummaryPanel.ConstructionDataGrid.Rows.Add(row);
                        }

                        ConstructionTabMaxRows++;
                    }

                    m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[0].Value = "";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[1].Value = "";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[2].Value = "";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[3].Value = "O Fact";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[4].Value = "";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[5].Value = "";
                    m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[6].Value = "";
                    CurrentRow++;
                    QueueNum = 1;
                    BuildPercentage = 0.0f;

                    foreach (MissileBuildQueueItem MBQ in CurrentPopulation.MissileBuildQueue)
                    {
                        if (CurrentRow == ConstructionTabMaxRows)
                        {
                            using (DataGridViewRow row = new DataGridViewRow())
                            {
                                // setup row height. note that by default they are 22 pixels in height!
                                row.Height = 17;
                                m_oSummaryPanel.ConstructionDataGrid.Rows.Add(row);
                            }

                            ConstructionTabMaxRows++;
                        }


                        m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[0].Value = MBQ.ordnanceDef.Name;
                        m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[1].Value = String.Format("{0:N2}", MBQ.numToBuild);
                        m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[2].Value = String.Format("{0:N2}", MBQ.buildCapacity);

                        float DevotedToThis = (MBQ.buildCapacity / 100.0f) * CurrentPopulation.CalcTotalOrdnanceIndustry();

                        m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[3].Value = String.Format("{0:N2}", DevotedToThis);
                        m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[4].Value = String.Format("{0:N2}", MBQ.costPerItem);


                        if ((BuildPercentage + MBQ.buildCapacity) <= 100.0f)
                        {
                            BuildPercentage = BuildPercentage + MBQ.buildCapacity;

                            /// <summary>
                            /// nothing is calculated here, but this logic determines how date should be presented.
                            /// </summary>
                            /// 
                            float BPRequirement = (float)Math.Floor(MBQ.numToBuild) * (float)MBQ.costPerItem;
                            float YearsOfProduction = (BPRequirement / DevotedToThis);

                            if (DevotedToThis == 0.0f || YearsOfProduction > Constants.Colony.TimerYearMax)
                            {
                                m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[5].Value = "-";
                            }
                            else
                            {
                                m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[5].Value = MBQ.completionDate.ToShortDateString();
                            }

                            /// <summary>
                            /// this item is being built
                            /// </summary>
                            if (MBQ.inProduction == true)
                            {
                                m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[6].Value = "No";
                            }
                            else
                            {
                                m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[6].Value = "Paused";
                            }
                        }
                        else
                        {
                            //this item is in the queue.
                            m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[5].Value = "-";
                            m_oSummaryPanel.ConstructionDataGrid.Rows[CurrentRow].Cells[6].Value = String.Format("Queue-{0}", QueueNum);
                            QueueNum++;
                        }
                        CurrentRow++;
                    }

                    for (int RowIterator = CurrentRow; RowIterator < ConstructionTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.ConstructionDataGrid.Rows[RowIterator].Cells[0].Value = "";
                        m_oSummaryPanel.ConstructionDataGrid.Rows[RowIterator].Cells[1].Value = "";
                        m_oSummaryPanel.ConstructionDataGrid.Rows[RowIterator].Cells[2].Value = "";
                        m_oSummaryPanel.ConstructionDataGrid.Rows[RowIterator].Cells[3].Value = "";
                        m_oSummaryPanel.ConstructionDataGrid.Rows[RowIterator].Cells[4].Value = "";
                        m_oSummaryPanel.ConstructionDataGrid.Rows[RowIterator].Cells[5].Value = "";
                        m_oSummaryPanel.ConstructionDataGrid.Rows[RowIterator].Cells[6].Value = "";
                    }
                }
            }
            catch
            {
#if LOG4NET_ENABLED
                logger.Error("Error building the build queue.");
#endif
            }
        }

        /// <summary>
        /// Build the industrial tab label containing production totals.
        /// </summary>
        private void BuildConstructionLabel()
        {
            if (CurrentPopulation != null)
            {
                float CFProd = CurrentPopulation.CalcTotalIndustry();
                float OFProd = CurrentPopulation.CalcTotalOrdnanceIndustry();
                float FFProd = CurrentPopulation.CalcTotalFighterIndustry();
                int CF = (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.ConstructionFactory].Number);
                int EB = 0;
                int CI = (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.ConventionalIndustry].Number);
                int OF = (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.OrdnanceFactory].Number); ;
                int FF = (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.FighterFactory].Number); ;
                String CLabel = String.Format("Construction: {0:N1} ({1}/{2}/{3})                    Ordnance Production: {4:N1} ({5})                    Fighter Production: {6:N1} ({7})",
                                               CFProd, CF, EB, CI, OFProd, OF, FFProd, FF);
                m_oSummaryPanel.ConstructionLabel.Text = CLabel;
            }
        }

        /// <summary>
        /// Build the refining label to print various statistics about refining on the current population.
        /// </summary>
        private void BuildRefiningLabel()
        {
            if (CurrentPopulation != null)
            {
                float RFProd = CurrentPopulation.CalcTotalRefining();
                int CI = (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.ConventionalIndustry].Number);
                int RF = (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.FuelRefinery].Number);

                String FormattedProduction = ((int)Math.Floor(RFProd)).ToString("#,##0");
                String FormattedStockpile = ((int)Math.Floor(CurrentPopulation.FuelStockpile)).ToString("#,##0");

                m_oSummaryPanel.RefineryLabel.Text = String.Format("Industry:{0}     Refineries:{1}", CI, RF);
                m_oSummaryPanel.FuelProductionLabel.Text = String.Format("Annual Production:{0} Litres", FormattedProduction);
                m_oSummaryPanel.FuelStockpileLabel.Text = String.Format("Fuel Reserves:{0} Litres", FormattedStockpile);
            }
        }

        /// <summary>
        /// Update the number and percent textboxes depending on What constructionDataGrid item is selected.
        /// </summary>
        private void UpdateBuildTexts()
        {
            if (m_oSummaryPanel.ConstructionDataGrid.CurrentCell != null)
            {
                if (m_oSummaryPanel.ConstructionDataGrid.CurrentCell.RowIndex != -1)
                {
                    int index = m_oSummaryPanel.ConstructionDataGrid.CurrentCell.RowIndex;
                    if (index > 0 && index <= CurrentPopulation.ConstructionBuildQueue.Count) // 1 to Count is CBQ Item
                    {
                        int RealIndex = index - 1;
                        BuildIndustrialProjectGroupBoxText(RealIndex);

                    }
                    else if (index > (CurrentPopulation.ConstructionBuildQueue.Count + 2) &&
                             index < ((CurrentPopulation.MissileBuildQueue.Count + CurrentPopulation.ConstructionBuildQueue.Count) + 3)) //Count + 2 to MBQ + CBQ = MBQ Item
                    {
                        int RealIndex = index - (CurrentPopulation.ConstructionBuildQueue.Count + 3);
                        BuildIndustrialProjectGroupBoxText(RealIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Build the number and percentage textboxes so that the user has current data without having to manually refresh this.
        /// </summary>
        /// <param name="RealIndex"></param>
        private void BuildIndustrialProjectGroupBoxText(int RealIndex)
        {
            if (CurrentPopulation != null && RealIndex != -1)
            {
                m_oSummaryPanel.ItemNumberTextBox.Text = CurrentPopulation.ConstructionBuildQueue[RealIndex].numToBuild.ToString();
                m_oSummaryPanel.ItemPercentTextBox.Text = CurrentPopulation.ConstructionBuildQueue[RealIndex].buildCapacity.ToString();
            }
            else
            {
                m_oSummaryPanel.ItemNumberTextBox.Text = "0";
                m_oSummaryPanel.ItemPercentTextBox.Text = "100";
            }
        }
        #endregion

        #region Mining Tab
        /// <summary>
        /// Initialize the mining tab columns.
        /// </summary>
        private void SetupMiningTab()
        {
            try
            {
                Padding newPadding = new Padding(2, 0, 2, 0);
                AddColumn("Mineral", newPadding, m_oSummaryPanel.MiningDataGrid, 0);
                AddColumn("Quantity", newPadding, m_oSummaryPanel.MiningDataGrid, 3);
                AddColumn("Access.", newPadding, m_oSummaryPanel.MiningDataGrid, 3);
                AddColumn("Annual Production", newPadding, m_oSummaryPanel.MiningDataGrid, 3);
                AddColumn("Years to Depletion", newPadding, m_oSummaryPanel.MiningDataGrid, 3);
                AddColumn("Stockpile", newPadding, m_oSummaryPanel.MiningDataGrid, 3);
                AddColumn("Recent SP +/-", newPadding, m_oSummaryPanel.MiningDataGrid, 3);
                AddColumn("Mass Driver +/-", newPadding, m_oSummaryPanel.MiningDataGrid, 3);
                AddColumn("Stockpile plus Production", newPadding, m_oSummaryPanel.MiningDataGrid, 3);
                AddColumn("Projected Usage", newPadding, m_oSummaryPanel.MiningDataGrid, 3);
                AddColumn("Reserve Level (Dbl-Clk to Set)", newPadding, m_oSummaryPanel.MiningDataGrid, 3);

                // Add Rows:
                for (int i = 0; i <= (int)Constants.Minerals.MinerialNames.MinerialCount; ++i)
                {
                    using (DataGridViewRow row = new DataGridViewRow())
                    {
                        // setup row height. note that by default they are 22 pixels in height!
                        row.Height = 18;
                        m_oSummaryPanel.MiningDataGrid.Rows.Add(row);
                    }
                }

                /// <summary>
                /// populate mineral names.
                /// </summary>
                for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
                {
                    m_oSummaryPanel.MiningDataGrid.Rows[mineralIterator].Cells[0].Value = String.Format("{0}", (Constants.Minerals.MinerialNames)mineralIterator);
                }
                m_oSummaryPanel.MiningDataGrid.Rows[(int)Constants.Minerals.MinerialNames.MinerialCount].Cells[0].Value = "Total";
            }
            catch
            {
#if LOG4NET_ENABLED
                logger.Error("Something whent wrong Creating Columns for the mining tab in the economics screen...");
#endif
            }
        }

        /// <summary>
        /// Displays the mining data for the current population.
        /// </summary>
        private void RefreshMiningTab()
        {
            if (m_oCurrnetPopulation != null && m_oSummaryPanel.MiningDataGrid.Rows.Count != 0)
            {
                //access annual production and years to depletion are "-" if 0.
                int mineralReserveTotal = 0;
                int mineralStockTotal = 0;
                float Accessibility = 0;
                int Production = 0;
                int YearsToDepletion = 0;
                for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
                {

                    /// <summary>
                    /// Planetary reserves.
                    /// </summary>
                    mineralReserveTotal = mineralReserveTotal + (int)Math.Floor(m_oCurrnetPopulation.Planet.MinerialReserves[mineralIterator]);
                    if (m_oCurrnetPopulation.Planet.MinerialReserves[mineralIterator] != 0.0f)
                        m_oSummaryPanel.MiningDataGrid.Rows[mineralIterator].Cells[1].Value = String.Format("{0}", Math.Floor(m_oCurrnetPopulation.Planet.MinerialReserves[mineralIterator]));
                    else
                        m_oSummaryPanel.MiningDataGrid.Rows[mineralIterator].Cells[1].Value = "-";

                    /// <summary>
                    /// Planetary Accessibility
                    /// </summary>
                    Accessibility = Accessibility + m_oCurrnetPopulation.Planet.MinerialAccessibility[mineralIterator];
                    if (m_oCurrnetPopulation.Planet.MinerialAccessibility[mineralIterator] != 0.0f)
                        m_oSummaryPanel.MiningDataGrid.Rows[mineralIterator].Cells[2].Value = String.Format("{0}", m_oCurrnetPopulation.Planet.MinerialAccessibility[mineralIterator]);
                    else
                        m_oSummaryPanel.MiningDataGrid.Rows[mineralIterator].Cells[2].Value = "-";

                    /// <summary>
                    /// 3 is annual production
                    /// </summary>
                    int AnnualProd = (int)Math.Floor(m_oCurrnetPopulation.CalcTotalMining() * m_oCurrnetPopulation.Planet.MinerialAccessibility[mineralIterator]);
                    Production = Production + AnnualProd;
                    if (AnnualProd != 0)
                        m_oSummaryPanel.MiningDataGrid.Rows[mineralIterator].Cells[3].Value = String.Format("{0}", AnnualProd);
                    else
                        m_oSummaryPanel.MiningDataGrid.Rows[mineralIterator].Cells[3].Value = "-";

                    /// <summary>
                    /// 4 is YTD. reserves / mining
                    /// </summary>
                    int YTD = (int)(Math.Floor(m_oCurrnetPopulation.Planet.MinerialReserves[mineralIterator]) / (Math.Floor(m_oCurrnetPopulation.CalcTotalMining() * m_oCurrnetPopulation.Planet.MinerialAccessibility[mineralIterator])));
                    if (YTD > YearsToDepletion)
                        YearsToDepletion = YTD;
                    if (YTD != 0)
                        m_oSummaryPanel.MiningDataGrid.Rows[mineralIterator].Cells[4].Value = String.Format("{0}", YTD);
                    else
                        m_oSummaryPanel.MiningDataGrid.Rows[mineralIterator].Cells[4].Value = "-";

                    /// <summary>
                    /// Population mineral stockpile
                    /// </summary>
                    mineralStockTotal = mineralStockTotal + (int)Math.Floor(m_oCurrnetPopulation.Minerials[mineralIterator]);
                    m_oSummaryPanel.MiningDataGrid.Rows[mineralIterator].Cells[5].Value = String.Format("{0}", Math.Floor(m_oCurrnetPopulation.Minerials[mineralIterator]));
                }
                if (mineralReserveTotal != 0)
                    m_oSummaryPanel.MiningDataGrid.Rows[(int)Constants.Minerals.MinerialNames.MinerialCount].Cells[1].Value = String.Format("{0}", mineralReserveTotal);
                else
                    m_oSummaryPanel.MiningDataGrid.Rows[(int)Constants.Minerals.MinerialNames.MinerialCount].Cells[1].Value = "-";

                if (Accessibility != 0.0f)
                    m_oSummaryPanel.MiningDataGrid.Rows[(int)Constants.Minerals.MinerialNames.MinerialCount].Cells[2].Value = String.Format("{0:N2}", (Accessibility / 11.0f));
                else
                    m_oSummaryPanel.MiningDataGrid.Rows[(int)Constants.Minerals.MinerialNames.MinerialCount].Cells[2].Value = "-";

                if (Production != 0)
                    m_oSummaryPanel.MiningDataGrid.Rows[(int)Constants.Minerals.MinerialNames.MinerialCount].Cells[3].Value = String.Format("{0}", Production);
                else
                    m_oSummaryPanel.MiningDataGrid.Rows[(int)Constants.Minerals.MinerialNames.MinerialCount].Cells[3].Value = "-";

                if (YearsToDepletion != 0)
                    m_oSummaryPanel.MiningDataGrid.Rows[(int)Constants.Minerals.MinerialNames.MinerialCount].Cells[4].Value = String.Format("{0}", YearsToDepletion);
                else
                    m_oSummaryPanel.MiningDataGrid.Rows[(int)Constants.Minerals.MinerialNames.MinerialCount].Cells[4].Value = "-";

                m_oSummaryPanel.MiningDataGrid.Rows[(int)Constants.Minerals.MinerialNames.MinerialCount].Cells[5].Value = String.Format("{0}", mineralStockTotal);
            }

            BuildMiningLabel();
        }

        /// <summary>
        /// Builds the label informing the user how many mines and how much production comes from those mines under ideal circumstances.
        /// </summary>
        private void BuildMiningLabel()
        {
            if (CurrentPopulation != null)
            {
                int Mines = (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.Mine].Number) +
                            (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.AutomatedMine].Number) +
                            (int)(Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.ConventionalIndustry].Number) / 10.0f);
                float Mining = CurrentPopulation.CalcTotalMining();
                m_oSummaryPanel.MiningLabel.Text = String.Format("Ground-based Mines: {0}          Annual Production(accessibility 1.0): {1:N1}", Mines, Mining);
            }
        }

        #endregion

        #region Terraforming Tab
        /// <summary>
        /// Build the terraforming tab based on the currently selected population.
        /// </summary>
        private void BuildTerraformingTab()
        {
            if (CurrentPopulation != null)
            {
                SystemBody CurrentPlanet = CurrentPopulation.Planet;
                Atmosphere CurrentAtmosphere = CurrentPlanet.Atmosphere;
                m_oSummaryPanel.TerraformingAtmosphereListBox.Items.Clear();
                float TotalGas = CurrentAtmosphere.Pressure;
                String Entry = "N/A";

                float GHP = 0.0f;
                float AGHP = 0.0f;

                foreach (KeyValuePair<AtmosphericGas,float> pair in CurrentAtmosphere.Composition)
                {
                    if (pair.Key.GreenhouseEffect == 1)
                    {
                        GHP = GHP + pair.Value;
                    }
                    if (pair.Key.GreenhouseEffect == -1)
                    {
                        AGHP = AGHP + pair.Value;
                    }

                    //Nitrogen 79% 0.79 atm
                    float partial = (pair.Value / TotalGas) * 100.0f;
                    Entry = String.Format("{0} {1:N2}% {2:N4} atm",pair.Key.Name,partial,pair.Value);
                    m_oSummaryPanel.TerraformingAtmosphereListBox.Items.Add(Entry);
                }
                Entry = String.Format("Total Atmospheric Pressure: {0:N4}",TotalGas);
                m_oSummaryPanel.TerraformingAtmosphereListBox.Items.Add(Entry);

                Entry = String.Format("{0:N2}",CurrentPlanet.BaseTemperature);
                m_oSummaryPanel.TerraformingBaseTempCelsiusTextBox.Text = Entry;

                Entry = String.Format("{0:N2}", (CurrentPlanet.BaseTemperature + 273.0f));
                m_oSummaryPanel.TerraformingBaseTempKelvinTextBox.Text = Entry;

                Entry = String.Format("{0:N2}", CurrentAtmosphere.SurfaceTemperature);
                m_oSummaryPanel.TerraformingSurfaceTempCelsiusTextBox.Text = Entry;

                Entry = String.Format("{0:N2}", (CurrentAtmosphere.SurfaceTemperature + 273.0f));
                m_oSummaryPanel.TerraformingSurfaceTempKelvinTextBox.Text = Entry;

                Entry = String.Format("{0:N2}", GHP);
                m_oSummaryPanel.TerraforminGreenhousePressureTextBox.Text = Entry;

                Entry = String.Format("{0:N2}", AGHP);
                m_oSummaryPanel.TerraformingAntiGHPressureTextBox.Text = Entry;

                Entry = String.Format("{0:N2}", CurrentAtmosphere.GreenhouseFactor);
                m_oSummaryPanel.TerraformingGreenhouseFactorTextBox.Text = Entry;

                Entry = String.Format("{0:N2}", CurrentAtmosphere.Albedo);
                m_oSummaryPanel.TerraformingPlanetaryAlbedoTextBox.Text = Entry;

                Entry = String.Format("Terraforming Installations:{0}", ((int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.TerraformingInstallation].Number)));
                if (CurrentPopulation._OrbitalTerraformModules != 0.0f)
                    Entry = String.Format("{0} Effective Terraform Modules in Orbit (including Commander Bonuses):{1:N2}", Entry, CurrentPopulation._OrbitalTerraformModules);
                Entry = String.Format("{0}  Annual Production:{1:N4} atm", Entry, CurrentPopulation.CalcTotalTerraforming());
                m_oSummaryPanel.TerraformingInstProdLabel.Text = Entry;

                if (CurrentPlanet.AtmosphericDust == 0.0f && CurrentPlanet.RadiationLevel == 0.0f)
                {
                    Entry = "No Radiation or Atmospheric Dust";
                }
                else
                {
                    Entry = String.Format("Radiation Level:{0:N1} Dust Level:{0:N1}", CurrentPlanet.RadiationLevel, CurrentPlanet.AtmosphericDust);
                }
                m_oSummaryPanel.TerraformingDustRadLabel.Text = Entry;
            }
        }
        #endregion
        #endregion

    }
}
