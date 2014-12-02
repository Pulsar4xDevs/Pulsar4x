using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Pulsar4X.UI.ViewModels;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;

#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif

/*
m_oIndustrialProjectGroupBox
m_oIndustrialAllocationGroupBox
m_oStockpileButton
m_oConstructionLabel
m_oOrdnanceLabel
m_oFighterLabel
m_oRefineriesLabel
m_oFuelProductionLabel
m_oFuelReservesLabel
m_oShipCompListBox
m_oMissileStockListBox
m_oFighterListBox
m_oPDCListBox
m_oCreateButton
m_oModifyButton
m_oCancelButton
m_oPauseButton
m_oSMAddButton
 * m_oPriorityUpButton
 * m_oPriorityDownButton
 * m_oItemNumberTextBox
m_oItemPercentTextBox
m_oNewFighterTaskGroupComboBox
 * m_oInstallationCostListBox
m_oInstallationTypeComboBox
*/

namespace Pulsar4X.UI.Handlers
{
    public class Economics
    {
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
                    if(m_oCurrnetFaction.Populations.Count != 0)
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

        public Economics()
        {
            //Create the summary panel.
            m_oSummaryPanel = new Panels.Eco_Summary();

            // Create Viewmodel:
            VM = new EconomicsViewModel();

            /// <summary>
            /// Create the tree view dictionary obviously.
            /// </summary>
            TreeViewDictionary = new Dictionary<string, Population>();

            // create Bindings:
            m_oSummaryPanel.FactionComboBox.Bind(c => c.DataSource, VM, d => d.Factions);
            m_oSummaryPanel.FactionComboBox.Bind(c => c.SelectedItem, VM, d => d.CurrentFaction, DataSourceUpdateMode.OnPropertyChanged);
            m_oSummaryPanel.FactionComboBox.DisplayMember = "Name";
            VM.FactionChanged += (s, args) => CurrentFaction = VM.CurrentFaction;
            CurrentFaction = VM.CurrentFaction;
            m_oSummaryPanel.FactionComboBox.SelectedIndexChanged += (s, args) => m_oSummaryPanel.FactionComboBox.DataBindings["SelectedItem"].WriteValue();
            m_oSummaryPanel.FactionComboBox.SelectedIndexChanged += new EventHandler(FactionComboBox_SelectedIndexChanged);

            /// <summary>
            /// Checkboxes:
            /// </summary>
            m_oSummaryPanel.GroupByFunctionCheckBox.CheckedChanged += new EventHandler(GroupByFunctionCheckBox_CheckedChanged);
            m_oSummaryPanel.HideCMCCheckBox.CheckedChanged += new EventHandler(HideCMCCheckBox_CheckedChanged);

            /// <summary>
            /// Tree view
            /// </summary>
            m_oSummaryPanel.PopulationTreeView.KeyPress += new KeyPressEventHandler(PopulationTreeView_Input);
            m_oSummaryPanel.PopulationTreeView.MouseClick += new MouseEventHandler(PopulationTreeView_Input);

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
            SetupSummaryDataGrid();
            RefreshSummaryCells();

            #region Industrial Tab
            m_oSummaryPanel.StockpileButton.Click += new EventHandler(StockpileButton_Click);
            StockpileButton_Click(null, null);

            m_oSummaryPanel.InstallationTypeComboBox.SelectedIndexChanged +=new EventHandler(InstallationTypeComboBox_SelectedIndexChanged);
            BuildConstructionComboBox();
            #endregion

            // Setup Pop Tree view. I do not know if I can bind this one, so I'll wind up doing it by hand.
            RefreshPanels();
             
            // setup Event handlers:

        }

        #region EventHandlers
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

        #endregion


        #region PrivateMethods

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
            if( m_oSummaryPanel.PopulationTreeView.SelectedNode != null)
            {
                if (TreeViewDictionary.ContainsKey(m_oSummaryPanel.PopulationTreeView.SelectedNode.Name) == true)
                {
                    m_oCurrnetPopulation = TreeViewDictionary[m_oSummaryPanel.PopulationTreeView.SelectedNode.Name];
                }
            }
        }

        #region Time Advancement Buttons
        /// <summary>
        /// Function to advance time for all buttons. this is all lifted from the system map time code.
        /// </summary>
        /// <param name="TickValue"></param>
        private void AdvanceTime(int TickValue)
        {
            int elapsed = GameState.SE.SubpulseHandler(GameState.Instance.Factions, GameState.RNG, TickValue);

            TimeSpan TS = new TimeSpan(0, 0, elapsed);
            GameState.Instance.GameDateTime = GameState.Instance.GameDateTime.Add(TS);

            int Seconds = GameState.Instance.GameDateTime.Second + (GameState.Instance.GameDateTime.Minute * 60) + (GameState.Instance.GameDateTime.Hour * 3600) +
                           (GameState.Instance.GameDateTime.DayOfYear * 86400) - 86400;

            GameState.Instance.YearTickValue = Seconds;

            /// <summary>
            /// Put the date time on the main form.
            /// </summary>
            MainFormReference.Text = "Pulsar4X - " + GameState.Instance.GameDateTime.ToString();

            /// <summary>
            /// update planet/taskgroup/other positions as needed.
            /// </summary>
            SystemMapReference.RefreshStarSystem();
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
        private void StockpileButton_Click(object sender, EventArgs e)
        {
            if(m_oSummaryPanel.ConstructionDataGrid.Visible == false)
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
        #endregion


        /// <summary>
        /// Refresh all the various panels that make up this display.
        /// </summary>
        private void RefreshPanels()
        {
            if (m_oCurrnetFaction != null)
            {
                /// <summary>
                /// reset the construction type combo box selection to 0.
                /// </summary>
                if(m_oSummaryPanel.InstallationTypeComboBox.Items.Count != 0)
                    m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex = 0;


                /// <summary>
                /// Build the population lists.
                /// </summary>
                BuildTreeView();

                /// <summary>
                /// Summary Tab:
                /// </summary>
                RefreshSummaryCells();

                /// <summary>
                /// Industry Tab:
                /// </summary>
                RefreshIndustryTab();
            }
        }

        /// <summary>
        /// Just a space saver here to avoid copy pasting a lot. this is copied from taskgroup
        /// </summary>
        /// <param name="Header">Text of column header.</param>
        /// <param name="newPadding">Padding in use, not sure what this is or why its necessary. Cargo culting it is.</param>
        private void AddColumn(String Header, Padding newPadding, DataGridView TheDataGrid)
        {
            using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
            {
                col.HeaderText = Header;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
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

            Dictionary<StarSystem, float> SystemPopulation = new Dictionary<StarSystem,float>();

            m_oSummaryPanel.PopulationTreeView.Nodes.Clear();

            m_oSummaryPanel.PopulationTreeView.Nodes.Add("Populated Systems");

            if (m_oSummaryPanel.GroupByFunctionCheckBox.Checked == true)
            {
                m_oSummaryPanel.PopulationTreeView.Nodes.Add("Automated Mining Colonies");

                if(m_oSummaryPanel.HideCMCCheckBox.Checked == false)
                    m_oSummaryPanel.PopulationTreeView.Nodes.Add("Civilian Mining Colonies");

                m_oSummaryPanel.PopulationTreeView.Nodes.Add("Listening Posts");
                m_oSummaryPanel.PopulationTreeView.Nodes.Add("Archeological Digs");
                m_oSummaryPanel.PopulationTreeView.Nodes.Add("Terraforming Sites");
                m_oSummaryPanel.PopulationTreeView.Nodes.Add("Other Colonies");

                foreach (Population Pop in m_oCurrnetFaction.Populations)
                {
                    StarSystem CurrentSystem = Pop.Planet.Primary.StarSystem;

                    if (m_oSummaryPanel.PopulationTreeView.Nodes[0].Nodes.ContainsKey(CurrentSystem.Name) == false)
                    {
                        m_oSummaryPanel.PopulationTreeView.Nodes[0].Nodes.Add(CurrentSystem.Name, CurrentSystem.Name);
                    }


                    /// <summary>
                    /// What type of colony is this, and should it be placed into the tree view(no if CMC and CMC are hidden)
                    /// <summary>
                    String Class = "";

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

                        Class = String.Format(": {0:n2}m", Pop.CivilianPopulation);

                        String Entry = String.Format("{0} - {1}{2}", Pop.Name, Pop.Species.Name, Class);

#warning DisplayIndex here is kludgy, do a find on the appropriate section? if so alter the adds to include a key string in addition to a text string
                        int DisplayIndex = 0;
                        int CurrentSystemIndex = m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes.IndexOfKey(CurrentSystem.Name);
                        m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes[CurrentSystemIndex].Nodes.Add(Entry, Entry);

                        TreeViewDictionary.Add(Entry, Pop);
                    }
                    /// <summary>
                    /// Automining colony will only mine, but may have CMCs listening posts, terraforming gear and ruins
                    /// </summary>
                    else if (Pop.Installations[(int)Installation.InstallationType.AutomatedMine].Number >= 1)
                    {
                        int mines = (int)Math.Floor(Pop.Installations[(int)Installation.InstallationType.AutomatedMine].Number);
                        Class = String.Format(": {0}x Auto Mines", mines);

                        String Entry = String.Format("{0} - {1}{2}", Pop.Name, Pop.Species.Name, Class);

                        int DisplayIndex = 1;
                        int CurrentSystemIndex = m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes.IndexOfKey(CurrentSystem.Name);
                        m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes[CurrentSystemIndex].Nodes.Add(Entry, Entry);

                        TreeViewDictionary.Add(Entry, Pop);
                    }
                    /// <summary>
                    /// CMCs. don't print this one if they should be hidden(by user input request). will also have a DSTS(or should I roll that into the CMC?), and may have terraforming and ruins)
                    /// </summary.
                    else if (Pop.Installations[(int)Installation.InstallationType.CivilianMiningComplex].Number >= 1 && m_oSummaryPanel.HideCMCCheckBox.Checked == false)
                    {
                        int mines = (int)Math.Floor(Pop.Installations[(int)Installation.InstallationType.CivilianMiningComplex].Number);
                        Class = String.Format(": {0}x Civ Mines", mines);

                        String Entry = String.Format("{0} - {1}{2}", Pop.Name, Pop.Species.Name, Class);

                        int DisplayIndex = 2;
                        int CurrentSystemIndex = m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes.IndexOfKey(CurrentSystem.Name);
                        m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes[CurrentSystemIndex].Nodes.Add(Entry, Entry);

                        TreeViewDictionary.Add(Entry, Pop);

                    }
                    /// <summary>
                    /// Listening Post. will have DSTS, and maybe terraforming or ruins.
                    /// </summary>
                    else if (Pop.Installations[(int)Installation.InstallationType.DeepSpaceTrackingStation].Number >= 1)
                    {
                        int DSTS = (int)Math.Floor(Pop.Installations[(int)Installation.InstallationType.DeepSpaceTrackingStation].Number);
                        Class = String.Format(": {0}x DSTS", DSTS);

                        String Entry = String.Format("{0} - {1}{2}", Pop.Name, Pop.Species.Name, Class);

                        int DisplayIndex = 2;
                        if (m_oSummaryPanel.HideCMCCheckBox.Checked == false)
                            DisplayIndex = 3;
                        int CurrentSystemIndex = m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes.IndexOfKey(CurrentSystem.Name);
                        m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes[CurrentSystemIndex].Nodes.Add(Entry, Entry);

                        TreeViewDictionary.Add(Entry, Pop);
                    }

                    /// <summary>
                    /// Archeological Dig. will have ruins, and may have orbital terraforming.
                    /// </summary>
                    else if (Pop.Planet.PlanetaryRuins.RuinSize != Ruins.RSize.NoRuins)
                    {
                        Class = String.Format(" Archeological Dig");

                        String Entry = String.Format("{0} - {1}{2}", Pop.Name, Pop.Species.Name, Class);

                        int DisplayIndex = 3;
                        if (m_oSummaryPanel.HideCMCCheckBox.Checked == false)
                            DisplayIndex = 4;
                        int CurrentSystemIndex = m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes.IndexOfKey(CurrentSystem.Name);
                        m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes[CurrentSystemIndex].Nodes.Add(Entry, Entry);

                        TreeViewDictionary.Add(Entry, Pop);
                    }
                    /// <summary>
                    /// Orbital Terraforming modules. a planet with ships in orbit that will terraform it.
                    /// </summary>
                    else if (Pop.OrbitalTerraformModules >= 1)
                    {
                        Class = String.Format(": {0:n1}x Orbital Terraform", Pop.OrbitalTerraformModules);

                        String Entry = String.Format("{0} - {1}{2}", Pop.Name, Pop.Species.Name, Class);

                        int DisplayIndex = 4;
                        if (m_oSummaryPanel.HideCMCCheckBox.Checked == false)
                            DisplayIndex = 5;
                        int CurrentSystemIndex = m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes.IndexOfKey(CurrentSystem.Name);
                        m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes[CurrentSystemIndex].Nodes.Add(Entry, Entry);

                        TreeViewDictionary.Add(Entry, Pop);
                    }
                    else
                    {

                        /// <summary>
                        /// If none of the above are true, then the colony is simply dropped into the other colonies category.
                        /// </summary>

                        String Entry = String.Format("{0} - {1}", Pop.Name, Pop.Species.Name);

                        int DisplayIndex = 5;
                        if (m_oSummaryPanel.HideCMCCheckBox.Checked == false)
                            DisplayIndex = 6;
                        int CurrentSystemIndex = m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes.IndexOfKey(CurrentSystem.Name);
                        m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes[CurrentSystemIndex].Nodes.Add(Entry, Entry);

                        TreeViewDictionary.Add(Entry, Pop);
                    }
                }
            }
            else
            {
                foreach (Population Pop in m_oCurrnetFaction.Populations)
                {
                    StarSystem CurrentSystem = Pop.Planet.Primary.StarSystem;

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

                        Class = String.Format(": {0:n2}m", Pop.CivilianPopulation);
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

                        if(m_oSummaryPanel.HideCMCCheckBox.Checked == true)
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
                    else if(Pop.Planet.PlanetaryRuins.RuinSize != Ruins.RSize.NoRuins)
                    {
                        Class = String.Format(" Archeological Dig");
                    }
                    /// <summary>
                    /// Orbital Terraforming modules. a planet with ships in orbit that will terraform it.
                    /// </summary>
                    else if(Pop.OrbitalTerraformModules >= 1)
                    {
                        Class = String.Format(": {0:n1}x Orbital Terraform",Pop.OrbitalTerraformModules);
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

                String Entry = String.Format("{0}({1:n1}m)",pair.Key.Name,pair.Value);

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
                AddColumn("Item", newPadding, m_oSummaryPanel.SummaryDataGrid);
                AddColumn("Amount", newPadding, m_oSummaryPanel.SummaryDataGrid);
                AddColumn("Installation", newPadding, m_oSummaryPanel.SummaryDataGrid);
                AddColumn("Number or Level", newPadding, m_oSummaryPanel.SummaryDataGrid);

                AddColumn("Item", newPadding, m_oSummaryPanel.BuildDataGrid);

                AddColumn("Project", newPadding, m_oSummaryPanel.ConstructionDataGrid);
                AddColumn("Amount Remaining", newPadding, m_oSummaryPanel.ConstructionDataGrid);
                AddColumn("% of Capacity", newPadding, m_oSummaryPanel.ConstructionDataGrid);
                AddColumn("Production Rate", newPadding, m_oSummaryPanel.ConstructionDataGrid);
                AddColumn("Cost Per Item", newPadding, m_oSummaryPanel.ConstructionDataGrid);
                AddColumn("Estimated Completion Date", newPadding, m_oSummaryPanel.ConstructionDataGrid);
                AddColumn("Pause / Queue", newPadding, m_oSummaryPanel.ConstructionDataGrid);

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
                m_oSummaryPanel.SummaryDataGrid.Rows[0].Cells[0].Value  = "Political Status";
                m_oSummaryPanel.SummaryDataGrid.Rows[1].Cells[0].Value  = "Species";
                m_oSummaryPanel.SummaryDataGrid.Rows[2].Cells[0].Value = "Planetary Suitability(colony cost)";
                m_oSummaryPanel.SummaryDataGrid.Rows[3].Cells[0].Value = "Administration Level Required";

                /// <summary>
                /// Wealth = Population * wealth tech
                /// </summary>
                m_oSummaryPanel.SummaryDataGrid.Rows[5].Cells[0].Value = "Annual Wealth Creation";

                /// <summary>
                /// Population Breakdown
                /// </summary>
                m_oSummaryPanel.SummaryDataGrid.Rows[7].Cells[0].Value  = "Population";
                m_oSummaryPanel.SummaryDataGrid.Rows[8].Cells[0].Value  = "   Agriculture and Enviromental (5.0%)";
                m_oSummaryPanel.SummaryDataGrid.Rows[9].Cells[0].Value  = "   Service Industries (75.0%)";
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
                if (CurrentPopulation != null)
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
                    m_oSummaryPanel.SummaryDataGrid.Rows[0].Cells[1].Value = CurrentPopulation.PoliticalPopStatus.ToString() + " Population" ;
                    m_oSummaryPanel.SummaryDataGrid.Rows[1].Cells[1].Value = CurrentPopulation.Species.Name;

                    // need planetary hab rating vs species tolerance
                    double ColCost = CurrentPopulation.Species.ColonyCost(CurrentPopulation.Planet);

                    m_oSummaryPanel.SummaryDataGrid.Rows[2].Cells[1].Value = ColCost.ToString();
                    m_oSummaryPanel.SummaryDataGrid.Rows[3].Cells[1].Value = CurrentPopulation.AdminRating;

                    /// <summary>
                    /// Wealth Creation
                    /// </summary>
                    int Expand = CurrentFaction.FactionTechLevel[(int)Faction.FactionTechnology.ExpandCivilianEconomy];
                    if (Expand > 12)
                        Expand = 12;
                    double Wealth = CurrentPopulation.CivilianPopulation * Expand * 20.0;
                    m_oSummaryPanel.SummaryDataGrid.Rows[5].Cells[1].Value = Wealth.ToString();

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
                        m_oSummaryPanel.SummaryDataGrid.Rows[11].Cells[1].Value = CurrentPopulation.PopulationGrowthRate.ToString() + "%";

                        Adjust1 = 5;
                    }
                    /// <summary>
                    /// Infrastructure information.
                    /// </summary>
                    m_oSummaryPanel.SummaryDataGrid.Rows[8 + Adjust1].Cells[1].Value = (ColCost * 200.0).ToString();
                    m_oSummaryPanel.SummaryDataGrid.Rows[9 + Adjust1].Cells[1].Value = CurrentPopulation.Installations[(int)Installation.InstallationType.Infrastructure].Number.ToString();

                    if (ColCost != 0.0f)
                        m_oSummaryPanel.SummaryDataGrid.Rows[10+Adjust1].Cells[1].Value = (CurrentPopulation.Installations[(int)Installation.InstallationType.Infrastructure].Number / (ColCost * 200.0)).ToString();
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
                        int slips = CurrentPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].Slipways[CSYIterator];
                        int tons = CurrentPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].Tonnage[CSYIterator];
                        iSlipways = iSlipways + slips;

                        /// <summary>
                        /// Manpower requirement = 1,000,000 + num_slipways * capacity_per_slipway_in_tons * 100 / DIVISOR.  DIVISOR is 1 for military yards and 10 for commercial yards.  Thus, the flat 1,000,000 manpower required is not reduced for commercial yards, only the capacity-based component.
                        /// </summary>
                        ShipyardWorkers = ShipyardWorkers + (slips * tons * 100 / 10); 
                    }

                    for (int NSYIterator = 0; NSYIterator < (int)CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].Number; NSYIterator++)
                    {
                        int slips = CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].Slipways[NSYIterator];
                        int tons = CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].Tonnage[NSYIterator];
                        iSlipways = iSlipways + CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].Slipways[NSYIterator];

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

                        int Strength = (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.DeepSpaceTrackingStation].Number) * Constants.Colony.DeepSpaceStrength[DSTS];
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
                    if(iShipyards != 0)
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
                        Entry = String.Format("{0:N2}",(CurrentPopulation.PopulationWorkingInManufacturing - TotalWorkerReq));
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
                        m_oSummaryPanel.SummaryDataGrid.Rows[14 + Adjust1].Cells[1].Value = CurrentPopulation.Planet.Primary.StarSystem.GetProtectionLevel(CurrentFaction).ToString();
                        Adjust1 = Adjust1 + 3;
                    }

                    /// <summary>
                    /// Planetary Geology
                    /// </summary>
                    m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[0].Value = "";
                    m_oSummaryPanel.SummaryDataGrid.Rows[12 + Adjust1].Cells[1].Value = "";
                    m_oSummaryPanel.SummaryDataGrid.Rows[13 + Adjust1].Cells[0].Value = "Tectonics";
                    m_oSummaryPanel.SummaryDataGrid.Rows[13 + Adjust1].Cells[1].Value = CurrentPopulation.Planet.PlanetaryTectonics;
                    if (CurrentPopulation.Planet.GeoTeamSurvey == true)
                    {
                        Entry = "Completed";
                    }
                    else
                        Entry = "No";
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
                    if (Adjust2 != 0 && (string)m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[3].Value != "")
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

                    
                    
#warning Calcluate these signatures in simEntity
                    CurrentPopulation.CalcThermalSignature();
                    CurrentPopulation.CalcEMSignature();

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
                    m_oSummaryPanel.SummaryDataGrid.Rows[Adjust2].Cells[2].Value ="Political Status Wealth/Trade Modifier";
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
                logger.Error("Something whent wrong Refreshing Cells for Economics summary screen...");
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
            if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex != -1)
            {
                if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.Installations)
                {
                    int row = 0;
                    foreach (Installation Install in CurrentFaction.InstallationTypes)
                    {
                        if (Install.IsBuildable(CurrentFaction, CurrentPopulation) == true)
                        {
                            if (row < BuildTabMaxRows)
                            {
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Install.Name;
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
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Install.Name;
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
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.Missiles)
                {
                    int row = 0;
                    foreach (OrdnanceDefTN Missile in CurrentFaction.ComponentList.MissileDef)
                    {
                        if (Missile.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
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
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.Fighters)
                {
                    int row = 0;
                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
#warning do fighter list here.
                }
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.BasicComponents)
                {
                    int row = 0;
                    foreach (GeneralComponentDefTN Crew in CurrentFaction.ComponentList.CrewQuarters)
                    {
                        if (Crew.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
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
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.ElectronicShieldComponents)
                {
                    int row = 0;
                    foreach (ShieldDefTN Shield in CurrentFaction.ComponentList.ShieldDef)
                    {
                        if (Shield.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
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
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Engine.Name;
                                row++;
                                BuildTabMaxRows++;
                            }
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
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.SensorsFCComponents)
                {
                    int row = 0;
                    foreach (PassiveSensorDefTN Passive in CurrentFaction.ComponentList.PassiveSensorDef)
                    {
                        if (Passive.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
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
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Visible = true;
                                m_oSummaryPanel.BuildDataGrid.Rows[row].Cells[0].Value = Passive.Name;
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
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.TransportIndustryComponents)
                {
                    int row = 0;
                    foreach (CargoDefTN Hold in CurrentFaction.ComponentList.CargoHoldDef)
                    {
                        if (Hold.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
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
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.WeaponsSupportComponents)
                {
                    int row = 0;
                    foreach (BeamDefTN Beam in CurrentFaction.ComponentList.BeamWeaponDef)
                    {
                        if (Beam.isObsolete == false)
                        {
                            if (row < BuildTabMaxRows)
                            {
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
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.BuildPDCOrbitalHabitat)
                {
                    int row = 0;
                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
#warning do Industrial PDC orbhab build here
                }
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.PrefabPDC)
                {
                    int row = 0;
                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
#warning do Industrial PDC Prefab here
                }
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.AssemblePDC)
                {
                    int row = 0;
                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
#warning do Industrial PDC assembly here
                }
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.RefitPDC)
                {
                    int row = 0;
                    for (int RowIterator = row; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
#warning do Industrial PDC refit here
                }
                else if (m_oSummaryPanel.InstallationTypeComboBox.SelectedIndex == (int)UIConstants.EconomicsPage.ConstructionID.MaintenanceSupplies)
                {
                    m_oSummaryPanel.BuildDataGrid.Rows[0].Visible = true;
                    m_oSummaryPanel.BuildDataGrid.Rows[0].Cells[0].Value = "Maintenance Supplies";

                    for (int RowIterator = 1; RowIterator < BuildTabMaxRows; RowIterator++)
                    {
                        m_oSummaryPanel.BuildDataGrid.Rows[RowIterator].Visible = false;
                    }
                    
                }
            }
        }
        #endregion


        #endregion

    }
}
