using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Pulsar4X.UI.ViewModels;
using Pulsar4X.Entities;

#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif

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

        private Faction m_oCurrnetFaction;
        public Faction CurrentFaction
        {
            get { return m_oCurrnetFaction; }
            set
            {
                if (value != m_oCurrnetFaction)
                {
                    m_oCurrnetFaction = value;
                    RefreshPanels();
                }
            }
        }

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

        public Economics()
        {
            //Create the summary panel.
            m_oSummaryPanel = new Panels.Eco_Summary();

            // Create Viewmodel:
            VM = new EconomicsViewModel();

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
            SetupSummaryDataGrid();
            RefreshSummaryCells();

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

        #region Time Advancement Buttons
        /// <summary>
        /// Function to advance time for all buttons.
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


        /// <summary>
        /// Refresh all the various panels that make up this display.
        /// </summary>
        private void RefreshPanels()
        {
            if (m_oCurrnetFaction != null)
            {
                BuildTreeView();
            }
        }


        /// <summary>
        /// Build the tree view box of populations.
        /// </summary>
        private void BuildTreeView()
        {
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
                    }

                    /// <summary>
                    /// Archeological Dig. will have ruins, and may have orbital terraforming.
                    /// </summary>
                    else if (Pop.Planet.PlanetaryRuins != null)
                    {
                        Class = String.Format(" Archeological Dig");

                        String Entry = String.Format("{0} - {1}{2}", Pop.Name, Pop.Species.Name, Class);

                        int DisplayIndex = 3;
                        if (m_oSummaryPanel.HideCMCCheckBox.Checked == false)
                            DisplayIndex = 4;
                        int CurrentSystemIndex = m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes.IndexOfKey(CurrentSystem.Name);
                        m_oSummaryPanel.PopulationTreeView.Nodes[DisplayIndex].Nodes[CurrentSystemIndex].Nodes.Add(Entry, Entry);
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
                    bool print = true;

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
                            print = false;
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
                    else if(Pop.Planet.PlanetaryRuins != null)
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
                    /// </summary>
 
                    String Entry = String.Format("{0} - {1}{2}", Pop.Name, Pop.Species.Name, Class);
                        
                    int CurrentSystemIndex = m_oSummaryPanel.PopulationTreeView.Nodes[0].Nodes.IndexOfKey(CurrentSystem.Name);
                    m_oSummaryPanel.PopulationTreeView.Nodes[0].Nodes[CurrentSystemIndex].Nodes.Add(Entry, Entry);
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

        private void SetupSummaryDataGrid()
        {
            try
            {
                // Add coloums:
                Padding newPadding = new Padding(2, 0, 2, 0);
                using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
                {
                    col.HeaderText = "Item";
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    col.DefaultCellStyle.Padding = newPadding;
                    if (col != null)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Columns.Add(col);
                    }
                }
                using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
                {
                    col.HeaderText = "Amount";
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    col.DefaultCellStyle.Padding = newPadding;
                    if (col != null)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Columns.Add(col);
                    }
                }
                using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
                {
                    col.HeaderText = "Installation";
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    col.DefaultCellStyle.Padding = newPadding;
                    if (col != null)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Columns.Add(col);
                    }
                }
                using (DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn())
                {
                    col.HeaderText = "Number or Level";
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    col.DefaultCellStyle.Padding = newPadding;
                    if (col != null)
                    {
                        m_oSummaryPanel.SummaryDataGrid.Columns.Add(col);
                    }
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
                m_oSummaryPanel.SummaryDataGrid.ClearSelection();
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
                m_oSummaryPanel.SummaryDataGrid.Rows[4].Cells[3].Value = iShipyards.ToString() + " / <Slipways>";
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
            catch
            {
#if LOG4NET_ENABLED
                logger.Error("Something whent wrong Refreshing Cells for Economics summary screen...");
#endif
            }
        }


        #endregion

    }
}
