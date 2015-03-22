using System.Windows.Forms;
namespace Pulsar4X.UI.Panels
{
    partial class Eco_Summary
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region General Economics Buttons, Faction Combo Box, and Population Tree View.
        /// <summary>
        /// Selection for which faction's economics page to view
        /// </summary>
        public ComboBox FactionComboBox
        {
            get { return m_oFactionComboBox; }
        }

        /// <summary>
        /// Populations held by this empire are selectable here.
        /// </summary>
        public TreeView PopulationTreeView
        {
            get { return m_oPopulationTreeView; }
        }

        /// <summary>
        /// Hide Civilian Mining Complexes
        /// </summary>
        public CheckBox HideCMCCheckBox
        {
            get { return m_oHideCMCCheckBox; }
        }

        /// <summary>
        /// Group populations by function
        /// </summary>
        public CheckBox GroupByFunctionCheckBox
        {
            get { return m_oFunctionGroupCheckBox; }
        }

        #region Time Control
        /// <summary>
        /// Time Control buttons
        /// </summary>
        public Button FiveSecondsButton
        {
            get { return m_o5SecondsButton; }
        }

        public Button ThirtySecondsButton
        {
            get { return m_o30SecondsButton; }
        }

        public Button FiveMinutesButton
        {
            get { return m_o5MinutesButton; }
        }

        public Button TwentyMinutesButton
        {
            get { return m_o20MinutesButton; }
        }

        public Button OneHourButton
        {
            get { return m_o1HourButton; }
        }

        public Button ThreeHoursButton
        {
            get { return m_o3HoursButton; }
        }

        public Button EightHoursButton
        {
            get { return m_o8HoursButton; }
        }

        public Button OneDayButton
        {
            get { return m_o1DayButton; }
        }

        public Button FiveDaysButton
        {
            get { return m_o5DaysButton; }
        }

        public Button ThirtyDaysButton
        {
            get { return m_o30DaysButton; }
        }
        #endregion

        #region Industrial Tab
        /// <summary>
        /// These groupboxes need to be set to visible or not depending on user input. 
        /// </summary>
        public GroupBox ShipComponentGroupBox
        {
            get { return m_oShipComponentGroupBox; }
        }
        public GroupBox PlanetMissileGroupBox
        {
            get { return m_oPlanetMissileGroupBox; }
        }
        public GroupBox PlanetPDCGroupBox
        {
            get { return m_oPlanetPDCGroupBox; }
        }
        public GroupBox PlanetFighterGroupBox
        {
            get { return m_oPlanetFighterGroupBox; }
        }

        public ComboBox InstallationTypeComboBox
        {
            get { return m_oInstallationTypeComboBox; }
        }

        public ListBox InstallationCostListBox
        {
            get { return m_oInstallationCostListBox; }
        }
        /// <summary>
        /// Stockpile button swaps the display between the industrial projects under construction display and the listing of planetary stockpiles.
        /// </summary>
        public Button StockpileButton
        {
            get { return m_oStockpileButton; }
        }

        /// <summary>
        /// Create an industrial project at this population.
        /// </summary>
        public Button CreateBuildProjButton
        {
            get { return m_oCreateButton; }
        }

        /// <summary>
        /// Modify an industrial project at this population.
        /// </summary>
        public Button ModifyBuildProjButton
        {
            get { return m_oModifyButton; }
        }

        /// <summary>
        /// Cancel an industrial project at this population.
        /// </summary>
        public Button CancelBuildProjButton
        {
            get { return m_oCancelButton; }
        }

        /// <summary>
        /// Pause an industrial project at this population.
        /// </summary>
        public Button PauseBuildProjButton
        {
            get { return m_oPauseButton; }
        }

        /// <summary>
        /// This colony should begin refining fuel.
        /// </summary>
        public Button StartRefiningButton
        {
            get { return m_oStartFuelButton; }
        }

        /// <summary>
        /// This colony should halt fuel production, but leave refineries staffed and capable of operating.
        /// </summary>
        public Button StopRefiningButton
        {
            get { return m_oStopFuelButton; }
        }

        /// <summary>
        /// User input textbox for number of an item to construct.
        /// </summary>
        public TextBox ItemNumberTextBox
        {
            get { return m_oItemNumberTextBox; }
        }

        /// <summary>
        /// User input textbox for percentage of population build capacity to devote to construction.
        /// </summary>
        public TextBox ItemPercentTextBox
        {
            get { return m_oItemPercentTextBox; }
        }

        /// <summary>
        /// List of Ship components
        /// </summary>
        public ListBox ShipCompListBox
        {
            get { return m_oShipCompListBox; }
        }

        /// <summary>
        /// List of missiles
        /// </summary>
        public ListBox MissileStockListBox
        {
            get { return m_oMissileStockListBox; }
        }

        /// <summary>
        /// Label for construction totals from Construction Factories, Fighter and Ordnance Factories, Engineering Brigades and Conventional Industry.
        /// </summary>
        public Label ConstructionLabel
        {
            get { return m_oConstructionLabel; }
        }

        /// <summary>
        /// Refineries at this colony.
        /// </summary>
        public Label RefineryLabel
        {
            get { return m_oRefineriesLabel; }
        }

        /// <summary>
        /// Annual Fuel Production at this colony.
        /// </summary>
        public Label FuelProductionLabel
        {
            get { return m_oFuelProductionlabel; }
        }

        /// <summary>
        /// Total fuel on this colony.
        /// </summary>
        public Label FuelStockpileLabel
        {
            get { return m_oFuelReservesLabel; }
        }

        #endregion

        #region Mining Tab
        /// <summary>
        /// Label for Mining Totals and Production.
        /// </summary>
        public Label MiningLabel
        {
            get { return m_oMineProductionLabel; }
        }
        #endregion

        #region Shipyard Tab
        /// <summary>
        /// Sets shipyard activity. This should bring up a prompt if there is another activity in progress.
        /// </summary>
        public Button SetActivityButton
        {
            get { return m_oSetActivityButton; }
        }

        /// <summary>
        /// Sets a shipyard task. gray out if task isn't possible.
        /// </summary>
        public Button AddTaskButton
        {
            get { return m_oAddTaskButton; }
        }

        /// <summary>
        /// What task will this shipyard itself be undergoing(as opposed to what will an individual slipway be doing.
        /// </summary>
        public ComboBox SYCTaskTypeComboBox
        {
            get { return m_oSYCTaskTypeComboBox; }
        }

        /// <summary>
        /// Should this Shipyard build/repair/refit, or scrap a ship?
        /// </summary>
        public ComboBox SYTaskTypeComboBox
        {
            get { return m_oSYTaskTypeComboBox; }
        }

        /// <summary>
        /// Eligible class list combo box for construction and refit.
        /// </summary>
        public ComboBox NewClassComboBox
        {
            get { return m_oSYNewClassComboBox; }
        }

        /// <summary>
        /// Retool target shipclass combo box
        /// </summary>
        public ComboBox NewShipClassComboBox
        {
            get { return m_oSYCNewShipClassComboBox; }
        }

        /// <summary>
        /// Ship selector for repair, refit, or scrapping.
        /// </summary>
        public ComboBox RepairRefitScrapShipComboBox
        {
            get { return m_oRepairRefitScrapShipComboBox; }
        }

        /// <summary>
        /// Eligible Ship class selector. the visibility of this will change based on user settings.
        /// </summary>
        public ComboBox SYNewClassComboBox
        {
            get { return m_oSYNewClassComboBox; }
        }

        /// <summary>
        /// What taskgroup will newly constructed ships be placed into. The visibility of this will change.
        /// </summary>
        public ComboBox SYTaskGroupComboBox
        {
            get { return m_oSYTaskGroupComboBox; }
        }

        /// <summary>
        /// List of shipclasses in orbit that can be targets of Repair/refit/scrap tasks.
        /// </summary>
        public ComboBox RepairRefitScrapClassComboBox
        {
            get { return m_oRepairRefitScrapClassComboBox; }
        }

        /// <summary>
        /// Text will change based on the selected shipyard.
        /// </summary>
        public GroupBox ShipyardTaskGroupBox
        {
            get { return m_oShipyardTaskGroupBox; }
        }

        /// <summary>
        /// Text wll change based on the selected Shipyard.
        /// </summary>
        public GroupBox ShipyardCreateTaskGroupBox
        {
            get { return m_oShipyardCreateTaskGroupBox; }
        }

        /// <summary>
        /// This will be set to visible based on user UI selections.
        /// </summary>
        public Label SYCShipClassLabel
        {
            get { return m_oSYCShipClassLabel; }
        }

        /// <summary>
        /// Need this for UI settings. Its visibility will change.
        /// </summary>
        public Label NewClassLabel
        {
            get { return m_oNewClassLabel; }
        }

        /// <summary>
        /// Need this for UI settings. Its visibility will change.
        /// </summary>
        public Label TaskGroupLabel
        {
            get { return m_oTaskGroupLabel; }
        }

        /// <summary>
        /// This label will update with Repair/refit/scrap as per the currently selected activity.
        /// </summary>
        public Label RepairRefitScrapLabel
        {
            get { return m_oRepairRefitScrapLabel; }
        }

        /// <summary>
        /// Shipyard complex required resources listbox.
        /// </summary>
        public ListBox SYCRequiredMaterialsListBox
        {
            get { return m_oSYCRequiredMaterialsListBox; }
        }

        /// <summary>
        /// Required materials for shipyard task.
        /// </summary>
        public ListBox ShipRequiredMaterialsListBox
        {
            get { return m_oShipRequiredMaterialsListBox; }
        }

        /// <summary>
        /// This textbox will display the name of the new ship to be built. Its visibility will also change.
        /// </summary>
        public TextBox SYShipNameTextBox
        {
            get { return m_oSYShipNameTextBox; }
        }

        /// <summary>
        /// This textbox is where the user will input the desired capacity expansion size for a shipyard. Visibility will change.
        /// </summary>
        public TextBox ExpandCapUntilXTextBox
        {
            get { return m_oExpandCapUntilXTextBox; }
        }

        /// <summary>
        /// Total cost of the selected shipyard complex activity.
        /// </summary>
        public TextBox SYCBuildCostTextBox
        {
            get { return m_oSYCBuildCostTextBox; }
        }

        /// <summary>
        /// The estimated completion date will go here.
        /// </summary>
        public TextBox SYCCompletionDateTextBox
        {
            get { return m_oSYCCompletionDateTextBox; }
        }

        /// <summary>
        /// Shipyard task cost goes here.
        /// </summary>
        public TextBox SYTaskCostTextBox
        {
            get { return m_oSYTaskCostTextBox; }
        }

        /// <summary>
        /// Estimated date of completion goes here.
        /// </summary>
        public TextBox SYTaskCompletionDateTextBox
        {
            get { return m_oSYTaskCompletionDateTextBox; }
        }

        #endregion

        #region Shipyard Tasks Tab
        #endregion

        #endregion

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.m_oEmpireGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oFactionComboBox = new System.Windows.Forms.ComboBox();
            this.m_oPopulationGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oFunctionGroupCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oHideCMCCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oPopulationTreeView = new System.Windows.Forms.TreeView();
            this.m_oIndustryControlGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oSMModButton = new System.Windows.Forms.Button();
            this.m_oAllResearchButton = new System.Windows.Forms.Button();
            this.m_oCloseButton = new System.Windows.Forms.Button();
            this.m_oAbandonButton = new System.Windows.Forms.Button();
            this.m_oMissileButton = new System.Windows.Forms.Button();
            this.m_oTurretButton = new System.Windows.Forms.Button();
            this.m_oDesignButton = new System.Windows.Forms.Button();
            this.m_oGeoStatusButton = new System.Windows.Forms.Button();
            this.m_oRefreshAllButton = new System.Windows.Forms.Button();
            this.m_oTransferButton = new System.Windows.Forms.Button();
            this.m_oSectorButton = new System.Windows.Forms.Button();
            this.m_oRenameBodyButton = new System.Windows.Forms.Button();
            this.m_oCapitolButton = new System.Windows.Forms.Button();
            this.m_oTimeGroupBox = new System.Windows.Forms.GroupBox();
            this.m_o30DaysButton = new System.Windows.Forms.Button();
            this.m_o5DaysButton = new System.Windows.Forms.Button();
            this.m_o1DayButton = new System.Windows.Forms.Button();
            this.m_o8HoursButton = new System.Windows.Forms.Button();
            this.m_o3HoursButton = new System.Windows.Forms.Button();
            this.m_o1HourButton = new System.Windows.Forms.Button();
            this.m_o20MinutesButton = new System.Windows.Forms.Button();
            this.m_o5MinutesButton = new System.Windows.Forms.Button();
            this.m_o30SecondsButton = new System.Windows.Forms.Button();
            this.m_o5SecondsButton = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.m_oSummaryTab = new System.Windows.Forms.TabPage();
            this.m_oSectorGovernorTextBox = new System.Windows.Forms.TextBox();
            this.m_oPlanetaryGovernorTextBox = new System.Windows.Forms.TextBox();
            this.m_oSectorGovernorLabel = new System.Windows.Forms.Label();
            this.m_oPlanetaryGovernorLabel = new System.Windows.Forms.Label();
            this.m_oSummaryGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oIndustryTab = new System.Windows.Forms.TabPage();
            this.m_oStockpileButton = new System.Windows.Forms.Button();
            this.m_oConstructionLabel = new System.Windows.Forms.Label();
            this.m_oFuelProductionGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oFuelReservesLabel = new System.Windows.Forms.Label();
            this.m_oFuelProductionlabel = new System.Windows.Forms.Label();
            this.m_oRefineriesLabel = new System.Windows.Forms.Label();
            this.m_oStartFuelButton = new System.Windows.Forms.Button();
            this.m_oStopFuelButton = new System.Windows.Forms.Button();
            this.m_oIndustrialProjectGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oItemNumberTextBox = new System.Windows.Forms.TextBox();
            this.m_oItemPercentTextBox = new System.Windows.Forms.TextBox();
            this.m_oNewFightersLabel = new System.Windows.Forms.Label();
            this.m_oPercentageLabel = new System.Windows.Forms.Label();
            this.m_oItemNumberLabel = new System.Windows.Forms.Label();
            this.m_oNewFighterTaskGroupComboBox = new System.Windows.Forms.ComboBox();
            this.m_oPriorityDownButton = new System.Windows.Forms.Button();
            this.m_oPriorityUpButton = new System.Windows.Forms.Button();
            this.m_oSMAddButton = new System.Windows.Forms.Button();
            this.m_oPauseButton = new System.Windows.Forms.Button();
            this.m_oCancelButton = new System.Windows.Forms.Button();
            this.m_oModifyButton = new System.Windows.Forms.Button();
            this.m_oCreateButton = new System.Windows.Forms.Button();
            this.m_oIndustrialAllocationGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oPlanetFighterGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oFighterListBox = new System.Windows.Forms.ListBox();
            this.m_oScrapFightersButton = new System.Windows.Forms.Button();
            this.m_oPlanetMissileGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oMissileStockListBox = new System.Windows.Forms.ListBox();
            this.m_oScrapMissilesButton = new System.Windows.Forms.Button();
            this.m_oPlanetPDCGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oPDCListBox = new System.Windows.Forms.ListBox();
            this.m_oShipComponentGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oShipCompListBox = new System.Windows.Forms.ListBox();
            this.m_oDisassembleCompButton = new System.Windows.Forms.Button();
            this.m_oScrapCompButton = new System.Windows.Forms.Button();
            this.m_oConstructionOptionsGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oInstallationGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oInstallationTypeComboBox = new System.Windows.Forms.ComboBox();
            this.m_oInstallationCostListBox = new System.Windows.Forms.ListBox();
            this.m_oMiningTab = new System.Windows.Forms.TabPage();
            this.m_oUsageNoteLabel = new System.Windows.Forms.Label();
            this.m_oMaintFacilityGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oMiningReportGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oMineProductionLabel = new System.Windows.Forms.Label();
            this.m_oMineralProductionGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oMassDriverDestGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oMassDriverDestinationComboBox = new System.Windows.Forms.ComboBox();
            this.m_oShipyardTab = new System.Windows.Forms.TabPage();
            this.m_oShipyardCreateTaskGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oRepairRefitScrapClassComboBox = new System.Windows.Forms.ComboBox();
            this.m_oRepairRefitScrapLabel = new System.Windows.Forms.Label();
            this.m_oRepairRefitScrapShipComboBox = new System.Windows.Forms.ComboBox();
            this.m_oSYShipNameTextBox = new System.Windows.Forms.TextBox();
            this.m_oSYTaskGroupComboBox = new System.Windows.Forms.ComboBox();
            this.m_oSYNewClassComboBox = new System.Windows.Forms.ComboBox();
            this.m_oTaskGroupLabel = new System.Windows.Forms.Label();
            this.m_oShipNameLabel = new System.Windows.Forms.Label();
            this.m_oNewClassLabel = new System.Windows.Forms.Label();
            this.m_oSYTaskTypeComboBox = new System.Windows.Forms.ComboBox();
            this.m_oCreateDefaultnameCheckBox = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.m_oSYTaskCompletionDateTextBox = new System.Windows.Forms.TextBox();
            this.m_oSYTaskCostTextBox = new System.Windows.Forms.TextBox();
            this.m_oCompletionDateLabel2 = new System.Windows.Forms.Label();
            this.m_oBuildCostLabel2 = new System.Windows.Forms.Label();
            this.m_oTaskTypelabel2 = new System.Windows.Forms.Label();
            this.m_oDefaultFleetButton = new System.Windows.Forms.Button();
            this.m_oAddTaskButton = new System.Windows.Forms.Button();
            this.m_oRefitDetailsButton = new System.Windows.Forms.Button();
            this.m_oNewNameButton = new System.Windows.Forms.Button();
            this.m_oReqMat2Label = new System.Windows.Forms.Label();
            this.m_oShipRequiredMaterialsListBox = new System.Windows.Forms.ListBox();
            this.m_oShipyardTaskGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oExpandCapUntilXTextBox = new System.Windows.Forms.TextBox();
            this.m_oSYCNewShipClassComboBox = new System.Windows.Forms.ComboBox();
            this.m_oSYCShipClassLabel = new System.Windows.Forms.Label();
            this.m_oSYCTaskTypeComboBox = new System.Windows.Forms.ComboBox();
            this.m_oSYCBuildCostTextBox = new System.Windows.Forms.TextBox();
            this.m_oSYCCompletionDateTextBox = new System.Windows.Forms.TextBox();
            this.m_oCompletionDateLabel = new System.Windows.Forms.Label();
            this.m_oBuildCostLabel = new System.Windows.Forms.Label();
            this.m_oTaskTypeLabel = new System.Windows.Forms.Label();
            this.m_oSMSYButton = new System.Windows.Forms.Button();
            this.m_oAutoRenameButton = new System.Windows.Forms.Button();
            this.m_oRenameSYButton = new System.Windows.Forms.Button();
            this.m_oPauseActivityButton = new System.Windows.Forms.Button();
            this.m_oDeleteActivityButton = new System.Windows.Forms.Button();
            this.m_oSetActivityButton = new System.Windows.Forms.Button();
            this.m_oReqMatLabel = new System.Windows.Forms.Label();
            this.m_oSYCRequiredMaterialsListBox = new System.Windows.Forms.ListBox();
            this.m_oShipyardListGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oShipyardTaskTab = new System.Windows.Forms.TabPage();
            this.m_oAnnualBuildRateLabel = new System.Windows.Forms.Label();
            this.m_oSYActivityButtonGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oSYARenameShipButton = new System.Windows.Forms.Button();
            this.m_oSYAScheduleButton = new System.Windows.Forms.Button();
            this.m_oSYALowerPriorityButton = new System.Windows.Forms.Button();
            this.m_oSYARaisePriorityButton = new System.Windows.Forms.Button();
            this.m_oSYAPauseTaskButton = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.m_oSYActivityGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oResearchTab = new System.Windows.Forms.TabPage();
            this.m_oEnvironmentTab = new System.Windows.Forms.TabPage();
            this.m_oTeamsTab = new System.Windows.Forms.TabPage();
            this.m_oCivTab = new System.Windows.Forms.TabPage();
            this.m_oGUTab = new System.Windows.Forms.TabPage();
            this.m_oGUTrainingTab = new System.Windows.Forms.TabPage();
            this.m_oWealthTab = new System.Windows.Forms.TabPage();
            this.m_oEmpireGroupBox.SuspendLayout();
            this.m_oPopulationGroupBox.SuspendLayout();
            this.m_oIndustryControlGroupBox.SuspendLayout();
            this.m_oTimeGroupBox.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.m_oSummaryTab.SuspendLayout();
            this.m_oIndustryTab.SuspendLayout();
            this.m_oFuelProductionGroupBox.SuspendLayout();
            this.m_oIndustrialProjectGroupBox.SuspendLayout();
            this.m_oIndustrialAllocationGroupBox.SuspendLayout();
            this.m_oPlanetFighterGroupBox.SuspendLayout();
            this.m_oPlanetMissileGroupBox.SuspendLayout();
            this.m_oPlanetPDCGroupBox.SuspendLayout();
            this.m_oShipComponentGroupBox.SuspendLayout();
            this.m_oConstructionOptionsGroupBox.SuspendLayout();
            this.m_oMiningTab.SuspendLayout();
            this.m_oMiningReportGroupBox.SuspendLayout();
            this.m_oMassDriverDestGroupBox.SuspendLayout();
            this.m_oShipyardTab.SuspendLayout();
            this.m_oShipyardCreateTaskGroupBox.SuspendLayout();
            this.m_oShipyardTaskGroupBox.SuspendLayout();
            this.m_oShipyardTaskTab.SuspendLayout();
            this.m_oSYActivityButtonGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_oEmpireGroupBox
            // 
            this.m_oEmpireGroupBox.Controls.Add(this.m_oFactionComboBox);
            this.m_oEmpireGroupBox.Location = new System.Drawing.Point(12, 12);
            this.m_oEmpireGroupBox.Name = "m_oEmpireGroupBox";
            this.m_oEmpireGroupBox.Size = new System.Drawing.Size(366, 53);
            this.m_oEmpireGroupBox.TabIndex = 0;
            this.m_oEmpireGroupBox.TabStop = false;
            this.m_oEmpireGroupBox.Text = "Empire";
            // 
            // m_oFactionComboBox
            // 
            this.m_oFactionComboBox.FormattingEnabled = true;
            this.m_oFactionComboBox.Location = new System.Drawing.Point(6, 19);
            this.m_oFactionComboBox.Name = "m_oFactionComboBox";
            this.m_oFactionComboBox.Size = new System.Drawing.Size(354, 21);
            this.m_oFactionComboBox.TabIndex = 0;
            // 
            // m_oPopulationGroupBox
            // 
            this.m_oPopulationGroupBox.Controls.Add(this.m_oFunctionGroupCheckBox);
            this.m_oPopulationGroupBox.Controls.Add(this.m_oHideCMCCheckBox);
            this.m_oPopulationGroupBox.Controls.Add(this.m_oPopulationTreeView);
            this.m_oPopulationGroupBox.Location = new System.Drawing.Point(12, 71);
            this.m_oPopulationGroupBox.Name = "m_oPopulationGroupBox";
            this.m_oPopulationGroupBox.Size = new System.Drawing.Size(366, 734);
            this.m_oPopulationGroupBox.TabIndex = 1;
            this.m_oPopulationGroupBox.TabStop = false;
            this.m_oPopulationGroupBox.Text = "Populated Systems";
            // 
            // m_oFunctionGroupCheckBox
            // 
            this.m_oFunctionGroupCheckBox.AutoSize = true;
            this.m_oFunctionGroupCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oFunctionGroupCheckBox.Location = new System.Drawing.Point(86, 711);
            this.m_oFunctionGroupCheckBox.Name = "m_oFunctionGroupCheckBox";
            this.m_oFunctionGroupCheckBox.Size = new System.Drawing.Size(114, 17);
            this.m_oFunctionGroupCheckBox.TabIndex = 2;
            this.m_oFunctionGroupCheckBox.Text = "Group By Function";
            this.m_oFunctionGroupCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oHideCMCCheckBox
            // 
            this.m_oHideCMCCheckBox.AutoSize = true;
            this.m_oHideCMCCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oHideCMCCheckBox.Location = new System.Drawing.Point(6, 711);
            this.m_oHideCMCCheckBox.Name = "m_oHideCMCCheckBox";
            this.m_oHideCMCCheckBox.Size = new System.Drawing.Size(74, 17);
            this.m_oHideCMCCheckBox.TabIndex = 1;
            this.m_oHideCMCCheckBox.Text = "Hide CMC";
            this.m_oHideCMCCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oPopulationTreeView
            // 
            this.m_oPopulationTreeView.Location = new System.Drawing.Point(6, 19);
            this.m_oPopulationTreeView.Name = "m_oPopulationTreeView";
            this.m_oPopulationTreeView.Size = new System.Drawing.Size(354, 686);
            this.m_oPopulationTreeView.TabIndex = 0;
            // 
            // m_oIndustryControlGroupBox
            // 
            this.m_oIndustryControlGroupBox.Controls.Add(this.m_oSMModButton);
            this.m_oIndustryControlGroupBox.Controls.Add(this.m_oAllResearchButton);
            this.m_oIndustryControlGroupBox.Controls.Add(this.m_oCloseButton);
            this.m_oIndustryControlGroupBox.Controls.Add(this.m_oAbandonButton);
            this.m_oIndustryControlGroupBox.Controls.Add(this.m_oMissileButton);
            this.m_oIndustryControlGroupBox.Controls.Add(this.m_oTurretButton);
            this.m_oIndustryControlGroupBox.Controls.Add(this.m_oDesignButton);
            this.m_oIndustryControlGroupBox.Controls.Add(this.m_oGeoStatusButton);
            this.m_oIndustryControlGroupBox.Controls.Add(this.m_oRefreshAllButton);
            this.m_oIndustryControlGroupBox.Controls.Add(this.m_oTransferButton);
            this.m_oIndustryControlGroupBox.Controls.Add(this.m_oSectorButton);
            this.m_oIndustryControlGroupBox.Controls.Add(this.m_oRenameBodyButton);
            this.m_oIndustryControlGroupBox.Controls.Add(this.m_oCapitolButton);
            this.m_oIndustryControlGroupBox.Location = new System.Drawing.Point(12, 811);
            this.m_oIndustryControlGroupBox.Name = "m_oIndustryControlGroupBox";
            this.m_oIndustryControlGroupBox.Size = new System.Drawing.Size(1211, 55);
            this.m_oIndustryControlGroupBox.TabIndex = 1;
            this.m_oIndustryControlGroupBox.TabStop = false;
            // 
            // m_oSMModButton
            // 
            this.m_oSMModButton.Location = new System.Drawing.Point(926, 19);
            this.m_oSMModButton.Name = "m_oSMModButton";
            this.m_oSMModButton.Size = new System.Drawing.Size(86, 23);
            this.m_oSMModButton.TabIndex = 12;
            this.m_oSMModButton.Text = "SM Mods";
            this.m_oSMModButton.UseVisualStyleBackColor = true;
            this.m_oSMModButton.Visible = false;
            // 
            // m_oAllResearchButton
            // 
            this.m_oAllResearchButton.Location = new System.Drawing.Point(834, 19);
            this.m_oAllResearchButton.Name = "m_oAllResearchButton";
            this.m_oAllResearchButton.Size = new System.Drawing.Size(86, 23);
            this.m_oAllResearchButton.TabIndex = 11;
            this.m_oAllResearchButton.Text = "All Research";
            this.m_oAllResearchButton.UseVisualStyleBackColor = true;
            this.m_oAllResearchButton.Visible = false;
            // 
            // m_oCloseButton
            // 
            this.m_oCloseButton.Location = new System.Drawing.Point(1110, 19);
            this.m_oCloseButton.Name = "m_oCloseButton";
            this.m_oCloseButton.Size = new System.Drawing.Size(86, 23);
            this.m_oCloseButton.TabIndex = 10;
            this.m_oCloseButton.Text = "&Close";
            this.m_oCloseButton.UseVisualStyleBackColor = true;
            // 
            // m_oAbandonButton
            // 
            this.m_oAbandonButton.Location = new System.Drawing.Point(1018, 19);
            this.m_oAbandonButton.Name = "m_oAbandonButton";
            this.m_oAbandonButton.Size = new System.Drawing.Size(86, 23);
            this.m_oAbandonButton.TabIndex = 9;
            this.m_oAbandonButton.Text = "Abandon";
            this.m_oAbandonButton.UseVisualStyleBackColor = true;
            // 
            // m_oMissileButton
            // 
            this.m_oMissileButton.Location = new System.Drawing.Point(742, 19);
            this.m_oMissileButton.Name = "m_oMissileButton";
            this.m_oMissileButton.Size = new System.Drawing.Size(86, 23);
            this.m_oMissileButton.TabIndex = 8;
            this.m_oMissileButton.Text = "Missiles";
            this.m_oMissileButton.UseVisualStyleBackColor = true;
            // 
            // m_oTurretButton
            // 
            this.m_oTurretButton.Location = new System.Drawing.Point(650, 19);
            this.m_oTurretButton.Name = "m_oTurretButton";
            this.m_oTurretButton.Size = new System.Drawing.Size(86, 23);
            this.m_oTurretButton.TabIndex = 7;
            this.m_oTurretButton.Text = "Turrets";
            this.m_oTurretButton.UseVisualStyleBackColor = true;
            // 
            // m_oDesignButton
            // 
            this.m_oDesignButton.Location = new System.Drawing.Point(558, 19);
            this.m_oDesignButton.Name = "m_oDesignButton";
            this.m_oDesignButton.Size = new System.Drawing.Size(86, 23);
            this.m_oDesignButton.TabIndex = 6;
            this.m_oDesignButton.Text = "Design";
            this.m_oDesignButton.UseVisualStyleBackColor = true;
            // 
            // m_oGeoStatusButton
            // 
            this.m_oGeoStatusButton.Location = new System.Drawing.Point(466, 19);
            this.m_oGeoStatusButton.Name = "m_oGeoStatusButton";
            this.m_oGeoStatusButton.Size = new System.Drawing.Size(86, 23);
            this.m_oGeoStatusButton.TabIndex = 5;
            this.m_oGeoStatusButton.Text = "Geo Status";
            this.m_oGeoStatusButton.UseVisualStyleBackColor = true;
            // 
            // m_oRefreshAllButton
            // 
            this.m_oRefreshAllButton.Location = new System.Drawing.Point(374, 19);
            this.m_oRefreshAllButton.Name = "m_oRefreshAllButton";
            this.m_oRefreshAllButton.Size = new System.Drawing.Size(86, 23);
            this.m_oRefreshAllButton.TabIndex = 4;
            this.m_oRefreshAllButton.Text = "Refresh All";
            this.m_oRefreshAllButton.UseVisualStyleBackColor = true;
            // 
            // m_oTransferButton
            // 
            this.m_oTransferButton.Location = new System.Drawing.Point(282, 19);
            this.m_oTransferButton.Name = "m_oTransferButton";
            this.m_oTransferButton.Size = new System.Drawing.Size(86, 23);
            this.m_oTransferButton.TabIndex = 3;
            this.m_oTransferButton.Text = "Transfer";
            this.m_oTransferButton.UseVisualStyleBackColor = true;
            // 
            // m_oSectorButton
            // 
            this.m_oSectorButton.Location = new System.Drawing.Point(190, 19);
            this.m_oSectorButton.Name = "m_oSectorButton";
            this.m_oSectorButton.Size = new System.Drawing.Size(86, 23);
            this.m_oSectorButton.TabIndex = 2;
            this.m_oSectorButton.Text = "Sectors";
            this.m_oSectorButton.UseVisualStyleBackColor = true;
            // 
            // m_oRenameBodyButton
            // 
            this.m_oRenameBodyButton.Location = new System.Drawing.Point(98, 19);
            this.m_oRenameBodyButton.Name = "m_oRenameBodyButton";
            this.m_oRenameBodyButton.Size = new System.Drawing.Size(86, 23);
            this.m_oRenameBodyButton.TabIndex = 1;
            this.m_oRenameBodyButton.Text = "Rename Body";
            this.m_oRenameBodyButton.UseVisualStyleBackColor = true;
            // 
            // m_oCapitolButton
            // 
            this.m_oCapitolButton.Location = new System.Drawing.Point(6, 19);
            this.m_oCapitolButton.Name = "m_oCapitolButton";
            this.m_oCapitolButton.Size = new System.Drawing.Size(86, 23);
            this.m_oCapitolButton.TabIndex = 0;
            this.m_oCapitolButton.Text = "Capitol";
            this.m_oCapitolButton.UseVisualStyleBackColor = true;
            // 
            // m_oTimeGroupBox
            // 
            this.m_oTimeGroupBox.Controls.Add(this.m_o30DaysButton);
            this.m_oTimeGroupBox.Controls.Add(this.m_o5DaysButton);
            this.m_oTimeGroupBox.Controls.Add(this.m_o1DayButton);
            this.m_oTimeGroupBox.Controls.Add(this.m_o8HoursButton);
            this.m_oTimeGroupBox.Controls.Add(this.m_o3HoursButton);
            this.m_oTimeGroupBox.Controls.Add(this.m_o1HourButton);
            this.m_oTimeGroupBox.Controls.Add(this.m_o20MinutesButton);
            this.m_oTimeGroupBox.Controls.Add(this.m_o5MinutesButton);
            this.m_oTimeGroupBox.Controls.Add(this.m_o30SecondsButton);
            this.m_oTimeGroupBox.Controls.Add(this.m_o5SecondsButton);
            this.m_oTimeGroupBox.Location = new System.Drawing.Point(384, 12);
            this.m_oTimeGroupBox.Name = "m_oTimeGroupBox";
            this.m_oTimeGroupBox.Size = new System.Drawing.Size(839, 53);
            this.m_oTimeGroupBox.TabIndex = 1;
            this.m_oTimeGroupBox.TabStop = false;
            this.m_oTimeGroupBox.Text = "Time Control";
            // 
            // m_o30DaysButton
            // 
            this.m_o30DaysButton.Location = new System.Drawing.Point(742, 19);
            this.m_o30DaysButton.Name = "m_o30DaysButton";
            this.m_o30DaysButton.Size = new System.Drawing.Size(75, 23);
            this.m_o30DaysButton.TabIndex = 20;
            this.m_o30DaysButton.Text = "30 Days";
            this.m_o30DaysButton.UseVisualStyleBackColor = true;
            // 
            // m_o5DaysButton
            // 
            this.m_o5DaysButton.Location = new System.Drawing.Point(661, 19);
            this.m_o5DaysButton.Name = "m_o5DaysButton";
            this.m_o5DaysButton.Size = new System.Drawing.Size(75, 23);
            this.m_o5DaysButton.TabIndex = 19;
            this.m_o5DaysButton.Text = "5 Days";
            this.m_o5DaysButton.UseVisualStyleBackColor = true;
            // 
            // m_o1DayButton
            // 
            this.m_o1DayButton.Location = new System.Drawing.Point(580, 19);
            this.m_o1DayButton.Name = "m_o1DayButton";
            this.m_o1DayButton.Size = new System.Drawing.Size(75, 23);
            this.m_o1DayButton.TabIndex = 18;
            this.m_o1DayButton.Text = "1 Day";
            this.m_o1DayButton.UseVisualStyleBackColor = true;
            // 
            // m_o8HoursButton
            // 
            this.m_o8HoursButton.Location = new System.Drawing.Point(499, 19);
            this.m_o8HoursButton.Name = "m_o8HoursButton";
            this.m_o8HoursButton.Size = new System.Drawing.Size(75, 23);
            this.m_o8HoursButton.TabIndex = 17;
            this.m_o8HoursButton.Text = "8 Hours";
            this.m_o8HoursButton.UseVisualStyleBackColor = true;
            // 
            // m_o3HoursButton
            // 
            this.m_o3HoursButton.Location = new System.Drawing.Point(418, 19);
            this.m_o3HoursButton.Name = "m_o3HoursButton";
            this.m_o3HoursButton.Size = new System.Drawing.Size(75, 23);
            this.m_o3HoursButton.TabIndex = 16;
            this.m_o3HoursButton.Text = "3 Hours";
            this.m_o3HoursButton.UseVisualStyleBackColor = true;
            // 
            // m_o1HourButton
            // 
            this.m_o1HourButton.Location = new System.Drawing.Point(337, 19);
            this.m_o1HourButton.Name = "m_o1HourButton";
            this.m_o1HourButton.Size = new System.Drawing.Size(75, 23);
            this.m_o1HourButton.TabIndex = 15;
            this.m_o1HourButton.Text = "1 Hour";
            this.m_o1HourButton.UseVisualStyleBackColor = true;
            // 
            // m_o20MinutesButton
            // 
            this.m_o20MinutesButton.Location = new System.Drawing.Point(256, 19);
            this.m_o20MinutesButton.Name = "m_o20MinutesButton";
            this.m_o20MinutesButton.Size = new System.Drawing.Size(75, 23);
            this.m_o20MinutesButton.TabIndex = 14;
            this.m_o20MinutesButton.Text = "20 Minutes";
            this.m_o20MinutesButton.UseVisualStyleBackColor = true;
            // 
            // m_o5MinutesButton
            // 
            this.m_o5MinutesButton.Location = new System.Drawing.Point(175, 19);
            this.m_o5MinutesButton.Name = "m_o5MinutesButton";
            this.m_o5MinutesButton.Size = new System.Drawing.Size(75, 23);
            this.m_o5MinutesButton.TabIndex = 13;
            this.m_o5MinutesButton.Text = "5 Minutes";
            this.m_o5MinutesButton.UseVisualStyleBackColor = true;
            // 
            // m_o30SecondsButton
            // 
            this.m_o30SecondsButton.Location = new System.Drawing.Point(94, 19);
            this.m_o30SecondsButton.Name = "m_o30SecondsButton";
            this.m_o30SecondsButton.Size = new System.Drawing.Size(75, 23);
            this.m_o30SecondsButton.TabIndex = 12;
            this.m_o30SecondsButton.Text = "30 Seconds";
            this.m_o30SecondsButton.UseVisualStyleBackColor = true;
            // 
            // m_o5SecondsButton
            // 
            this.m_o5SecondsButton.Location = new System.Drawing.Point(13, 19);
            this.m_o5SecondsButton.Name = "m_o5SecondsButton";
            this.m_o5SecondsButton.Size = new System.Drawing.Size(75, 23);
            this.m_o5SecondsButton.TabIndex = 11;
            this.m_o5SecondsButton.Text = "5 Seconds";
            this.m_o5SecondsButton.UseVisualStyleBackColor = true;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.m_oSummaryTab);
            this.tabControl1.Controls.Add(this.m_oIndustryTab);
            this.tabControl1.Controls.Add(this.m_oMiningTab);
            this.tabControl1.Controls.Add(this.m_oShipyardTab);
            this.tabControl1.Controls.Add(this.m_oShipyardTaskTab);
            this.tabControl1.Controls.Add(this.m_oResearchTab);
            this.tabControl1.Controls.Add(this.m_oEnvironmentTab);
            this.tabControl1.Controls.Add(this.m_oTeamsTab);
            this.tabControl1.Controls.Add(this.m_oCivTab);
            this.tabControl1.Controls.Add(this.m_oGUTab);
            this.tabControl1.Controls.Add(this.m_oGUTrainingTab);
            this.tabControl1.Controls.Add(this.m_oWealthTab);
            this.tabControl1.Location = new System.Drawing.Point(384, 71);
            this.tabControl1.Multiline = true;
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(839, 734);
            this.tabControl1.TabIndex = 2;
            // 
            // m_oSummaryTab
            // 
            this.m_oSummaryTab.Controls.Add(this.m_oSectorGovernorTextBox);
            this.m_oSummaryTab.Controls.Add(this.m_oPlanetaryGovernorTextBox);
            this.m_oSummaryTab.Controls.Add(this.m_oSectorGovernorLabel);
            this.m_oSummaryTab.Controls.Add(this.m_oPlanetaryGovernorLabel);
            this.m_oSummaryTab.Controls.Add(this.m_oSummaryGroupBox);
            this.m_oSummaryTab.Location = new System.Drawing.Point(4, 40);
            this.m_oSummaryTab.Name = "m_oSummaryTab";
            this.m_oSummaryTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oSummaryTab.Size = new System.Drawing.Size(831, 690);
            this.m_oSummaryTab.TabIndex = 0;
            this.m_oSummaryTab.Text = "Summary";
            this.m_oSummaryTab.UseVisualStyleBackColor = true;
            // 
            // m_oSectorGovernorTextBox
            // 
            this.m_oSectorGovernorTextBox.Location = new System.Drawing.Point(111, 33);
            this.m_oSectorGovernorTextBox.Name = "m_oSectorGovernorTextBox";
            this.m_oSectorGovernorTextBox.ReadOnly = true;
            this.m_oSectorGovernorTextBox.Size = new System.Drawing.Size(714, 20);
            this.m_oSectorGovernorTextBox.TabIndex = 4;
            this.m_oSectorGovernorTextBox.Text = "No Commander Assigned";
            // 
            // m_oPlanetaryGovernorTextBox
            // 
            this.m_oPlanetaryGovernorTextBox.Location = new System.Drawing.Point(111, 10);
            this.m_oPlanetaryGovernorTextBox.Name = "m_oPlanetaryGovernorTextBox";
            this.m_oPlanetaryGovernorTextBox.ReadOnly = true;
            this.m_oPlanetaryGovernorTextBox.Size = new System.Drawing.Size(714, 20);
            this.m_oPlanetaryGovernorTextBox.TabIndex = 3;
            this.m_oPlanetaryGovernorTextBox.Text = "No Commander Assigned";
            // 
            // m_oSectorGovernorLabel
            // 
            this.m_oSectorGovernorLabel.AutoSize = true;
            this.m_oSectorGovernorLabel.Location = new System.Drawing.Point(6, 36);
            this.m_oSectorGovernorLabel.Name = "m_oSectorGovernorLabel";
            this.m_oSectorGovernorLabel.Size = new System.Drawing.Size(85, 13);
            this.m_oSectorGovernorLabel.TabIndex = 2;
            this.m_oSectorGovernorLabel.Text = "Sector Governor";
            // 
            // m_oPlanetaryGovernorLabel
            // 
            this.m_oPlanetaryGovernorLabel.AutoSize = true;
            this.m_oPlanetaryGovernorLabel.Location = new System.Drawing.Point(6, 13);
            this.m_oPlanetaryGovernorLabel.Name = "m_oPlanetaryGovernorLabel";
            this.m_oPlanetaryGovernorLabel.Size = new System.Drawing.Size(98, 13);
            this.m_oPlanetaryGovernorLabel.TabIndex = 1;
            this.m_oPlanetaryGovernorLabel.Text = "Planetary Governor";
            // 
            // m_oSummaryGroupBox
            // 
            this.m_oSummaryGroupBox.Location = new System.Drawing.Point(6, 59);
            this.m_oSummaryGroupBox.Name = "m_oSummaryGroupBox";
            this.m_oSummaryGroupBox.Size = new System.Drawing.Size(819, 643);
            this.m_oSummaryGroupBox.TabIndex = 0;
            this.m_oSummaryGroupBox.TabStop = false;
            // 
            // m_oIndustryTab
            // 
            this.m_oIndustryTab.Controls.Add(this.m_oStockpileButton);
            this.m_oIndustryTab.Controls.Add(this.m_oConstructionLabel);
            this.m_oIndustryTab.Controls.Add(this.m_oFuelProductionGroupBox);
            this.m_oIndustryTab.Controls.Add(this.m_oIndustrialProjectGroupBox);
            this.m_oIndustryTab.Controls.Add(this.m_oIndustrialAllocationGroupBox);
            this.m_oIndustryTab.Controls.Add(this.m_oConstructionOptionsGroupBox);
            this.m_oIndustryTab.Location = new System.Drawing.Point(4, 40);
            this.m_oIndustryTab.Name = "m_oIndustryTab";
            this.m_oIndustryTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oIndustryTab.Size = new System.Drawing.Size(831, 690);
            this.m_oIndustryTab.TabIndex = 1;
            this.m_oIndustryTab.Text = "Industry";
            this.m_oIndustryTab.UseVisualStyleBackColor = true;
            // 
            // m_oStockpileButton
            // 
            this.m_oStockpileButton.Location = new System.Drawing.Point(750, 19);
            this.m_oStockpileButton.Name = "m_oStockpileButton";
            this.m_oStockpileButton.Size = new System.Drawing.Size(75, 23);
            this.m_oStockpileButton.TabIndex = 5;
            this.m_oStockpileButton.Text = "Stockpiles";
            this.m_oStockpileButton.UseVisualStyleBackColor = true;
            // 
            // m_oConstructionLabel
            // 
            this.m_oConstructionLabel.AutoSize = true;
            this.m_oConstructionLabel.Location = new System.Drawing.Point(96, 24);
            this.m_oConstructionLabel.Name = "m_oConstructionLabel";
            this.m_oConstructionLabel.Size = new System.Drawing.Size(504, 13);
            this.m_oConstructionLabel.TabIndex = 2;
            this.m_oConstructionLabel.Text = "Construction: 0.0 (CF/EB/CI)                    Ordnance Production: 0 (0)       " +
                "             Fighter Production: 0 (0)\r\n";
            // 
            // m_oFuelProductionGroupBox
            // 
            this.m_oFuelProductionGroupBox.Controls.Add(this.m_oFuelReservesLabel);
            this.m_oFuelProductionGroupBox.Controls.Add(this.m_oFuelProductionlabel);
            this.m_oFuelProductionGroupBox.Controls.Add(this.m_oRefineriesLabel);
            this.m_oFuelProductionGroupBox.Controls.Add(this.m_oStartFuelButton);
            this.m_oFuelProductionGroupBox.Controls.Add(this.m_oStopFuelButton);
            this.m_oFuelProductionGroupBox.Location = new System.Drawing.Point(215, 621);
            this.m_oFuelProductionGroupBox.Name = "m_oFuelProductionGroupBox";
            this.m_oFuelProductionGroupBox.Size = new System.Drawing.Size(610, 63);
            this.m_oFuelProductionGroupBox.TabIndex = 1;
            this.m_oFuelProductionGroupBox.TabStop = false;
            this.m_oFuelProductionGroupBox.Text = "Fuel Production";
            // 
            // m_oFuelReservesLabel
            // 
            this.m_oFuelReservesLabel.AutoSize = true;
            this.m_oFuelReservesLabel.Location = new System.Drawing.Point(214, 40);
            this.m_oFuelReservesLabel.Name = "m_oFuelReservesLabel";
            this.m_oFuelReservesLabel.Size = new System.Drawing.Size(75, 13);
            this.m_oFuelReservesLabel.TabIndex = 12;
            this.m_oFuelReservesLabel.Text = "Fuel Reserves";
            // 
            // m_oFuelProductionlabel
            // 
            this.m_oFuelProductionlabel.AutoSize = true;
            this.m_oFuelProductionlabel.Location = new System.Drawing.Point(6, 40);
            this.m_oFuelProductionlabel.Name = "m_oFuelProductionlabel";
            this.m_oFuelProductionlabel.Size = new System.Drawing.Size(97, 13);
            this.m_oFuelProductionlabel.TabIndex = 11;
            this.m_oFuelProductionlabel.Text = "Annual Production:";
            // 
            // m_oRefineriesLabel
            // 
            this.m_oRefineriesLabel.AutoSize = true;
            this.m_oRefineriesLabel.Location = new System.Drawing.Point(6, 20);
            this.m_oRefineriesLabel.Name = "m_oRefineriesLabel";
            this.m_oRefineriesLabel.Size = new System.Drawing.Size(57, 13);
            this.m_oRefineriesLabel.TabIndex = 6;
            this.m_oRefineriesLabel.Text = "Refineries:";
            // 
            // m_oStartFuelButton
            // 
            this.m_oStartFuelButton.Location = new System.Drawing.Point(448, 23);
            this.m_oStartFuelButton.Name = "m_oStartFuelButton";
            this.m_oStartFuelButton.Size = new System.Drawing.Size(75, 23);
            this.m_oStartFuelButton.TabIndex = 10;
            this.m_oStartFuelButton.Text = "Start";
            this.m_oStartFuelButton.UseVisualStyleBackColor = true;
            // 
            // m_oStopFuelButton
            // 
            this.m_oStopFuelButton.Location = new System.Drawing.Point(529, 23);
            this.m_oStopFuelButton.Name = "m_oStopFuelButton";
            this.m_oStopFuelButton.Size = new System.Drawing.Size(75, 23);
            this.m_oStopFuelButton.TabIndex = 9;
            this.m_oStopFuelButton.Text = "Stop";
            this.m_oStopFuelButton.UseVisualStyleBackColor = true;
            // 
            // m_oIndustrialProjectGroupBox
            // 
            this.m_oIndustrialProjectGroupBox.Controls.Add(this.m_oItemNumberTextBox);
            this.m_oIndustrialProjectGroupBox.Controls.Add(this.m_oItemPercentTextBox);
            this.m_oIndustrialProjectGroupBox.Controls.Add(this.m_oNewFightersLabel);
            this.m_oIndustrialProjectGroupBox.Controls.Add(this.m_oPercentageLabel);
            this.m_oIndustrialProjectGroupBox.Controls.Add(this.m_oItemNumberLabel);
            this.m_oIndustrialProjectGroupBox.Controls.Add(this.m_oNewFighterTaskGroupComboBox);
            this.m_oIndustrialProjectGroupBox.Controls.Add(this.m_oPriorityDownButton);
            this.m_oIndustrialProjectGroupBox.Controls.Add(this.m_oPriorityUpButton);
            this.m_oIndustrialProjectGroupBox.Controls.Add(this.m_oSMAddButton);
            this.m_oIndustrialProjectGroupBox.Controls.Add(this.m_oPauseButton);
            this.m_oIndustrialProjectGroupBox.Controls.Add(this.m_oCancelButton);
            this.m_oIndustrialProjectGroupBox.Controls.Add(this.m_oModifyButton);
            this.m_oIndustrialProjectGroupBox.Controls.Add(this.m_oCreateButton);
            this.m_oIndustrialProjectGroupBox.Location = new System.Drawing.Point(215, 515);
            this.m_oIndustrialProjectGroupBox.Name = "m_oIndustrialProjectGroupBox";
            this.m_oIndustrialProjectGroupBox.Size = new System.Drawing.Size(610, 100);
            this.m_oIndustrialProjectGroupBox.TabIndex = 1;
            this.m_oIndustrialProjectGroupBox.TabStop = false;
            this.m_oIndustrialProjectGroupBox.Text = "Create Industrial Project for";
            // 
            // m_oItemNumberTextBox
            // 
            this.m_oItemNumberTextBox.Location = new System.Drawing.Point(103, 28);
            this.m_oItemNumberTextBox.Name = "m_oItemNumberTextBox";
            this.m_oItemNumberTextBox.Size = new System.Drawing.Size(42, 20);
            this.m_oItemNumberTextBox.TabIndex = 25;
            this.m_oItemNumberTextBox.Text = "1";
            this.m_oItemNumberTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oItemPercentTextBox
            // 
            this.m_oItemPercentTextBox.Location = new System.Drawing.Point(225, 28);
            this.m_oItemPercentTextBox.Name = "m_oItemPercentTextBox";
            this.m_oItemPercentTextBox.Size = new System.Drawing.Size(42, 20);
            this.m_oItemPercentTextBox.TabIndex = 24;
            this.m_oItemPercentTextBox.Text = "100";
            this.m_oItemPercentTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oNewFightersLabel
            // 
            this.m_oNewFightersLabel.AutoSize = true;
            this.m_oNewFightersLabel.Location = new System.Drawing.Point(280, 31);
            this.m_oNewFightersLabel.Name = "m_oNewFightersLabel";
            this.m_oNewFightersLabel.Size = new System.Drawing.Size(123, 13);
            this.m_oNewFightersLabel.TabIndex = 23;
            this.m_oNewFightersLabel.Text = "New Fighters Taskgroup";
            // 
            // m_oPercentageLabel
            // 
            this.m_oPercentageLabel.AutoSize = true;
            this.m_oPercentageLabel.Location = new System.Drawing.Point(157, 31);
            this.m_oPercentageLabel.Name = "m_oPercentageLabel";
            this.m_oPercentageLabel.Size = new System.Drawing.Size(62, 13);
            this.m_oPercentageLabel.TabIndex = 22;
            this.m_oPercentageLabel.Text = "Percentage";
            // 
            // m_oItemNumberLabel
            // 
            this.m_oItemNumberLabel.AutoSize = true;
            this.m_oItemNumberLabel.Location = new System.Drawing.Point(6, 31);
            this.m_oItemNumberLabel.Name = "m_oItemNumberLabel";
            this.m_oItemNumberLabel.Size = new System.Drawing.Size(84, 13);
            this.m_oItemNumberLabel.TabIndex = 21;
            this.m_oItemNumberLabel.Text = "Number of Items";
            // 
            // m_oNewFighterTaskGroupComboBox
            // 
            this.m_oNewFighterTaskGroupComboBox.FormattingEnabled = true;
            this.m_oNewFighterTaskGroupComboBox.Location = new System.Drawing.Point(411, 28);
            this.m_oNewFighterTaskGroupComboBox.Name = "m_oNewFighterTaskGroupComboBox";
            this.m_oNewFighterTaskGroupComboBox.Size = new System.Drawing.Size(151, 21);
            this.m_oNewFighterTaskGroupComboBox.TabIndex = 20;
            // 
            // m_oPriorityDownButton
            // 
            this.m_oPriorityDownButton.Location = new System.Drawing.Point(572, 62);
            this.m_oPriorityDownButton.Margin = new System.Windows.Forms.Padding(7);
            this.m_oPriorityDownButton.Name = "m_oPriorityDownButton";
            this.m_oPriorityDownButton.Size = new System.Drawing.Size(28, 28);
            this.m_oPriorityDownButton.TabIndex = 19;
            this.m_oPriorityDownButton.Text = "\\/";
            this.m_oPriorityDownButton.UseVisualStyleBackColor = true;
            // 
            // m_oPriorityUpButton
            // 
            this.m_oPriorityUpButton.Location = new System.Drawing.Point(572, 23);
            this.m_oPriorityUpButton.Margin = new System.Windows.Forms.Padding(7);
            this.m_oPriorityUpButton.Name = "m_oPriorityUpButton";
            this.m_oPriorityUpButton.Size = new System.Drawing.Size(28, 28);
            this.m_oPriorityUpButton.TabIndex = 18;
            this.m_oPriorityUpButton.Text = "/\\";
            this.m_oPriorityUpButton.UseVisualStyleBackColor = true;
            // 
            // m_oSMAddButton
            // 
            this.m_oSMAddButton.Location = new System.Drawing.Point(330, 71);
            this.m_oSMAddButton.Name = "m_oSMAddButton";
            this.m_oSMAddButton.Size = new System.Drawing.Size(75, 23);
            this.m_oSMAddButton.TabIndex = 17;
            this.m_oSMAddButton.Text = "SM Add";
            this.m_oSMAddButton.UseVisualStyleBackColor = true;
            // 
            // m_oPauseButton
            // 
            this.m_oPauseButton.Location = new System.Drawing.Point(249, 71);
            this.m_oPauseButton.Name = "m_oPauseButton";
            this.m_oPauseButton.Size = new System.Drawing.Size(75, 23);
            this.m_oPauseButton.TabIndex = 16;
            this.m_oPauseButton.Text = "Pause";
            this.m_oPauseButton.UseVisualStyleBackColor = true;
            // 
            // m_oCancelButton
            // 
            this.m_oCancelButton.Location = new System.Drawing.Point(168, 71);
            this.m_oCancelButton.Name = "m_oCancelButton";
            this.m_oCancelButton.Size = new System.Drawing.Size(75, 23);
            this.m_oCancelButton.TabIndex = 15;
            this.m_oCancelButton.Text = "Cancel";
            this.m_oCancelButton.UseVisualStyleBackColor = true;
            // 
            // m_oModifyButton
            // 
            this.m_oModifyButton.Location = new System.Drawing.Point(87, 71);
            this.m_oModifyButton.Name = "m_oModifyButton";
            this.m_oModifyButton.Size = new System.Drawing.Size(75, 23);
            this.m_oModifyButton.TabIndex = 14;
            this.m_oModifyButton.Text = "Modify";
            this.m_oModifyButton.UseVisualStyleBackColor = true;
            // 
            // m_oCreateButton
            // 
            this.m_oCreateButton.Location = new System.Drawing.Point(6, 71);
            this.m_oCreateButton.Name = "m_oCreateButton";
            this.m_oCreateButton.Size = new System.Drawing.Size(75, 23);
            this.m_oCreateButton.TabIndex = 13;
            this.m_oCreateButton.Text = "Create";
            this.m_oCreateButton.UseVisualStyleBackColor = true;
            // 
            // m_oIndustrialAllocationGroupBox
            // 
            this.m_oIndustrialAllocationGroupBox.Controls.Add(this.m_oPlanetFighterGroupBox);
            this.m_oIndustrialAllocationGroupBox.Controls.Add(this.m_oPlanetMissileGroupBox);
            this.m_oIndustrialAllocationGroupBox.Controls.Add(this.m_oPlanetPDCGroupBox);
            this.m_oIndustrialAllocationGroupBox.Controls.Add(this.m_oShipComponentGroupBox);
            this.m_oIndustrialAllocationGroupBox.Location = new System.Drawing.Point(215, 55);
            this.m_oIndustrialAllocationGroupBox.Name = "m_oIndustrialAllocationGroupBox";
            this.m_oIndustrialAllocationGroupBox.Size = new System.Drawing.Size(610, 454);
            this.m_oIndustrialAllocationGroupBox.TabIndex = 1;
            this.m_oIndustrialAllocationGroupBox.TabStop = false;
            this.m_oIndustrialAllocationGroupBox.Text = "Industrial Allocation";
            // 
            // m_oPlanetFighterGroupBox
            // 
            this.m_oPlanetFighterGroupBox.Controls.Add(this.m_oFighterListBox);
            this.m_oPlanetFighterGroupBox.Controls.Add(this.m_oScrapFightersButton);
            this.m_oPlanetFighterGroupBox.Location = new System.Drawing.Point(315, 244);
            this.m_oPlanetFighterGroupBox.Margin = new System.Windows.Forms.Padding(10);
            this.m_oPlanetFighterGroupBox.Name = "m_oPlanetFighterGroupBox";
            this.m_oPlanetFighterGroupBox.Size = new System.Drawing.Size(282, 197);
            this.m_oPlanetFighterGroupBox.TabIndex = 1;
            this.m_oPlanetFighterGroupBox.TabStop = false;
            this.m_oPlanetFighterGroupBox.Text = "Fighters in Orbit";
            // 
            // m_oFighterListBox
            // 
            this.m_oFighterListBox.FormattingEnabled = true;
            this.m_oFighterListBox.Location = new System.Drawing.Point(6, 19);
            this.m_oFighterListBox.Name = "m_oFighterListBox";
            this.m_oFighterListBox.Size = new System.Drawing.Size(270, 147);
            this.m_oFighterListBox.TabIndex = 10;
            // 
            // m_oScrapFightersButton
            // 
            this.m_oScrapFightersButton.Location = new System.Drawing.Point(102, 168);
            this.m_oScrapFightersButton.Name = "m_oScrapFightersButton";
            this.m_oScrapFightersButton.Size = new System.Drawing.Size(85, 23);
            this.m_oScrapFightersButton.TabIndex = 7;
            this.m_oScrapFightersButton.Text = "Scrap Fighters";
            this.m_oScrapFightersButton.UseVisualStyleBackColor = true;
            // 
            // m_oPlanetMissileGroupBox
            // 
            this.m_oPlanetMissileGroupBox.Controls.Add(this.m_oMissileStockListBox);
            this.m_oPlanetMissileGroupBox.Controls.Add(this.m_oScrapMissilesButton);
            this.m_oPlanetMissileGroupBox.Location = new System.Drawing.Point(321, 26);
            this.m_oPlanetMissileGroupBox.Margin = new System.Windows.Forms.Padding(10);
            this.m_oPlanetMissileGroupBox.Name = "m_oPlanetMissileGroupBox";
            this.m_oPlanetMissileGroupBox.Size = new System.Drawing.Size(282, 197);
            this.m_oPlanetMissileGroupBox.TabIndex = 1;
            this.m_oPlanetMissileGroupBox.TabStop = false;
            this.m_oPlanetMissileGroupBox.Text = "Planetary Missile Stockpile";
            // 
            // m_oMissileStockListBox
            // 
            this.m_oMissileStockListBox.FormattingEnabled = true;
            this.m_oMissileStockListBox.Location = new System.Drawing.Point(6, 19);
            this.m_oMissileStockListBox.Name = "m_oMissileStockListBox";
            this.m_oMissileStockListBox.Size = new System.Drawing.Size(270, 147);
            this.m_oMissileStockListBox.TabIndex = 10;
            // 
            // m_oScrapMissilesButton
            // 
            this.m_oScrapMissilesButton.Location = new System.Drawing.Point(96, 168);
            this.m_oScrapMissilesButton.Name = "m_oScrapMissilesButton";
            this.m_oScrapMissilesButton.Size = new System.Drawing.Size(85, 23);
            this.m_oScrapMissilesButton.TabIndex = 9;
            this.m_oScrapMissilesButton.Text = "Scrap Missiles";
            this.m_oScrapMissilesButton.UseVisualStyleBackColor = true;
            // 
            // m_oPlanetPDCGroupBox
            // 
            this.m_oPlanetPDCGroupBox.Controls.Add(this.m_oPDCListBox);
            this.m_oPlanetPDCGroupBox.Location = new System.Drawing.Point(13, 244);
            this.m_oPlanetPDCGroupBox.Margin = new System.Windows.Forms.Padding(10);
            this.m_oPlanetPDCGroupBox.Name = "m_oPlanetPDCGroupBox";
            this.m_oPlanetPDCGroupBox.Size = new System.Drawing.Size(282, 197);
            this.m_oPlanetPDCGroupBox.TabIndex = 1;
            this.m_oPlanetPDCGroupBox.TabStop = false;
            this.m_oPlanetPDCGroupBox.Text = "Prefabricated PDC Components";
            // 
            // m_oPDCListBox
            // 
            this.m_oPDCListBox.FormattingEnabled = true;
            this.m_oPDCListBox.Location = new System.Drawing.Point(6, 19);
            this.m_oPDCListBox.Name = "m_oPDCListBox";
            this.m_oPDCListBox.Size = new System.Drawing.Size(270, 173);
            this.m_oPDCListBox.TabIndex = 11;
            // 
            // m_oShipComponentGroupBox
            // 
            this.m_oShipComponentGroupBox.Controls.Add(this.m_oShipCompListBox);
            this.m_oShipComponentGroupBox.Controls.Add(this.m_oDisassembleCompButton);
            this.m_oShipComponentGroupBox.Controls.Add(this.m_oScrapCompButton);
            this.m_oShipComponentGroupBox.Location = new System.Drawing.Point(13, 26);
            this.m_oShipComponentGroupBox.Margin = new System.Windows.Forms.Padding(10);
            this.m_oShipComponentGroupBox.Name = "m_oShipComponentGroupBox";
            this.m_oShipComponentGroupBox.Size = new System.Drawing.Size(282, 197);
            this.m_oShipComponentGroupBox.TabIndex = 0;
            this.m_oShipComponentGroupBox.TabStop = false;
            this.m_oShipComponentGroupBox.Text = "Ship Component Stockpile";
            // 
            // m_oShipCompListBox
            // 
            this.m_oShipCompListBox.FormattingEnabled = true;
            this.m_oShipCompListBox.Location = new System.Drawing.Point(6, 19);
            this.m_oShipCompListBox.Name = "m_oShipCompListBox";
            this.m_oShipCompListBox.Size = new System.Drawing.Size(270, 147);
            this.m_oShipCompListBox.TabIndex = 9;
            // 
            // m_oDisassembleCompButton
            // 
            this.m_oDisassembleCompButton.Location = new System.Drawing.Point(191, 168);
            this.m_oDisassembleCompButton.Name = "m_oDisassembleCompButton";
            this.m_oDisassembleCompButton.Size = new System.Drawing.Size(85, 23);
            this.m_oDisassembleCompButton.TabIndex = 8;
            this.m_oDisassembleCompButton.Text = "Disassemble";
            this.m_oDisassembleCompButton.UseVisualStyleBackColor = true;
            // 
            // m_oScrapCompButton
            // 
            this.m_oScrapCompButton.Location = new System.Drawing.Point(6, 168);
            this.m_oScrapCompButton.Name = "m_oScrapCompButton";
            this.m_oScrapCompButton.Size = new System.Drawing.Size(85, 23);
            this.m_oScrapCompButton.TabIndex = 6;
            this.m_oScrapCompButton.Text = "Scrap";
            this.m_oScrapCompButton.UseVisualStyleBackColor = true;
            // 
            // m_oConstructionOptionsGroupBox
            // 
            this.m_oConstructionOptionsGroupBox.Controls.Add(this.m_oInstallationGroupBox);
            this.m_oConstructionOptionsGroupBox.Controls.Add(this.m_oInstallationTypeComboBox);
            this.m_oConstructionOptionsGroupBox.Controls.Add(this.m_oInstallationCostListBox);
            this.m_oConstructionOptionsGroupBox.Location = new System.Drawing.Point(6, 55);
            this.m_oConstructionOptionsGroupBox.Name = "m_oConstructionOptionsGroupBox";
            this.m_oConstructionOptionsGroupBox.Size = new System.Drawing.Size(203, 629);
            this.m_oConstructionOptionsGroupBox.TabIndex = 0;
            this.m_oConstructionOptionsGroupBox.TabStop = false;
            this.m_oConstructionOptionsGroupBox.Text = "Construction Options";
            // 
            // m_oInstallationGroupBox
            // 
            this.m_oInstallationGroupBox.Location = new System.Drawing.Point(7, 45);
            this.m_oInstallationGroupBox.Name = "m_oInstallationGroupBox";
            this.m_oInstallationGroupBox.Size = new System.Drawing.Size(190, 425);
            this.m_oInstallationGroupBox.TabIndex = 27;
            this.m_oInstallationGroupBox.TabStop = false;
            // 
            // m_oInstallationTypeComboBox
            // 
            this.m_oInstallationTypeComboBox.FormattingEnabled = true;
            this.m_oInstallationTypeComboBox.Location = new System.Drawing.Point(6, 19);
            this.m_oInstallationTypeComboBox.Name = "m_oInstallationTypeComboBox";
            this.m_oInstallationTypeComboBox.Size = new System.Drawing.Size(191, 21);
            this.m_oInstallationTypeComboBox.TabIndex = 26;
            // 
            // m_oInstallationCostListBox
            // 
            this.m_oInstallationCostListBox.FormattingEnabled = true;
            this.m_oInstallationCostListBox.Location = new System.Drawing.Point(6, 476);
            this.m_oInstallationCostListBox.Name = "m_oInstallationCostListBox";
            this.m_oInstallationCostListBox.Size = new System.Drawing.Size(191, 147);
            this.m_oInstallationCostListBox.TabIndex = 11;
            // 
            // m_oMiningTab
            // 
            this.m_oMiningTab.Controls.Add(this.m_oUsageNoteLabel);
            this.m_oMiningTab.Controls.Add(this.m_oMaintFacilityGroupBox);
            this.m_oMiningTab.Controls.Add(this.m_oMiningReportGroupBox);
            this.m_oMiningTab.Location = new System.Drawing.Point(4, 40);
            this.m_oMiningTab.Name = "m_oMiningTab";
            this.m_oMiningTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oMiningTab.Size = new System.Drawing.Size(831, 690);
            this.m_oMiningTab.TabIndex = 2;
            this.m_oMiningTab.Text = "Mining/Maintenance";
            this.m_oMiningTab.UseVisualStyleBackColor = true;
            // 
            // m_oUsageNoteLabel
            // 
            this.m_oUsageNoteLabel.AutoSize = true;
            this.m_oUsageNoteLabel.Location = new System.Drawing.Point(14, 338);
            this.m_oUsageNoteLabel.Name = "m_oUsageNoteLabel";
            this.m_oUsageNoteLabel.Size = new System.Drawing.Size(752, 13);
            this.m_oUsageNoteLabel.TabIndex = 2;
            this.m_oUsageNoteLabel.Text = "Projected Usage includes all queued ordnance, fighters, and construction, any cur" +
                "rent shipyard tasks, maintenance facilities and one year of supply production";
            // 
            // m_oMaintFacilityGroupBox
            // 
            this.m_oMaintFacilityGroupBox.Location = new System.Drawing.Point(6, 369);
            this.m_oMaintFacilityGroupBox.Name = "m_oMaintFacilityGroupBox";
            this.m_oMaintFacilityGroupBox.Size = new System.Drawing.Size(819, 315);
            this.m_oMaintFacilityGroupBox.TabIndex = 1;
            this.m_oMaintFacilityGroupBox.TabStop = false;
            this.m_oMaintFacilityGroupBox.Text = "Estimated Annual Mineral Requirements for Maintenance Facilities";
            // 
            // m_oMiningReportGroupBox
            // 
            this.m_oMiningReportGroupBox.Controls.Add(this.m_oMineProductionLabel);
            this.m_oMiningReportGroupBox.Controls.Add(this.m_oMineralProductionGroupBox);
            this.m_oMiningReportGroupBox.Controls.Add(this.m_oMassDriverDestGroupBox);
            this.m_oMiningReportGroupBox.Location = new System.Drawing.Point(9, 7);
            this.m_oMiningReportGroupBox.Name = "m_oMiningReportGroupBox";
            this.m_oMiningReportGroupBox.Size = new System.Drawing.Size(816, 315);
            this.m_oMiningReportGroupBox.TabIndex = 0;
            this.m_oMiningReportGroupBox.TabStop = false;
            this.m_oMiningReportGroupBox.Text = "Mining Report";
            // 
            // m_oMineProductionLabel
            // 
            this.m_oMineProductionLabel.AutoSize = true;
            this.m_oMineProductionLabel.Location = new System.Drawing.Point(6, 32);
            this.m_oMineProductionLabel.Name = "m_oMineProductionLabel";
            this.m_oMineProductionLabel.Size = new System.Drawing.Size(326, 13);
            this.m_oMineProductionLabel.TabIndex = 2;
            this.m_oMineProductionLabel.Text = "Ground-based Mines: 0          Annual Production(accessibility 1.0): 0";
            // 
            // m_oMineralProductionGroupBox
            // 
            this.m_oMineralProductionGroupBox.Location = new System.Drawing.Point(6, 64);
            this.m_oMineralProductionGroupBox.Name = "m_oMineralProductionGroupBox";
            this.m_oMineralProductionGroupBox.Size = new System.Drawing.Size(804, 245);
            this.m_oMineralProductionGroupBox.TabIndex = 1;
            this.m_oMineralProductionGroupBox.TabStop = false;
            // 
            // m_oMassDriverDestGroupBox
            // 
            this.m_oMassDriverDestGroupBox.Controls.Add(this.m_oMassDriverDestinationComboBox);
            this.m_oMassDriverDestGroupBox.Location = new System.Drawing.Point(608, 9);
            this.m_oMassDriverDestGroupBox.Name = "m_oMassDriverDestGroupBox";
            this.m_oMassDriverDestGroupBox.Size = new System.Drawing.Size(202, 49);
            this.m_oMassDriverDestGroupBox.TabIndex = 0;
            this.m_oMassDriverDestGroupBox.TabStop = false;
            this.m_oMassDriverDestGroupBox.Text = "Mass Driver Destination";
            // 
            // m_oMassDriverDestinationComboBox
            // 
            this.m_oMassDriverDestinationComboBox.FormattingEnabled = true;
            this.m_oMassDriverDestinationComboBox.Location = new System.Drawing.Point(7, 20);
            this.m_oMassDriverDestinationComboBox.Name = "m_oMassDriverDestinationComboBox";
            this.m_oMassDriverDestinationComboBox.Size = new System.Drawing.Size(189, 21);
            this.m_oMassDriverDestinationComboBox.TabIndex = 0;
            // 
            // m_oShipyardTab
            // 
            this.m_oShipyardTab.Controls.Add(this.m_oShipyardCreateTaskGroupBox);
            this.m_oShipyardTab.Controls.Add(this.m_oShipyardTaskGroupBox);
            this.m_oShipyardTab.Controls.Add(this.m_oShipyardListGroupBox);
            this.m_oShipyardTab.Location = new System.Drawing.Point(4, 40);
            this.m_oShipyardTab.Name = "m_oShipyardTab";
            this.m_oShipyardTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oShipyardTab.Size = new System.Drawing.Size(831, 690);
            this.m_oShipyardTab.TabIndex = 3;
            this.m_oShipyardTab.Text = "Manage Shipyards ";
            this.m_oShipyardTab.UseVisualStyleBackColor = true;
            // 
            // m_oShipyardCreateTaskGroupBox
            // 
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oRepairRefitScrapClassComboBox);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oRepairRefitScrapLabel);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oRepairRefitScrapShipComboBox);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oSYShipNameTextBox);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oSYTaskGroupComboBox);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oSYNewClassComboBox);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oTaskGroupLabel);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oShipNameLabel);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oNewClassLabel);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oSYTaskTypeComboBox);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oCreateDefaultnameCheckBox);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.label1);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oSYTaskCompletionDateTextBox);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oSYTaskCostTextBox);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oCompletionDateLabel2);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oBuildCostLabel2);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oTaskTypelabel2);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oDefaultFleetButton);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oAddTaskButton);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oRefitDetailsButton);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oNewNameButton);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oReqMat2Label);
            this.m_oShipyardCreateTaskGroupBox.Controls.Add(this.m_oShipRequiredMaterialsListBox);
            this.m_oShipyardCreateTaskGroupBox.Location = new System.Drawing.Point(6, 514);
            this.m_oShipyardCreateTaskGroupBox.Name = "m_oShipyardCreateTaskGroupBox";
            this.m_oShipyardCreateTaskGroupBox.Size = new System.Drawing.Size(819, 170);
            this.m_oShipyardCreateTaskGroupBox.TabIndex = 2;
            this.m_oShipyardCreateTaskGroupBox.TabStop = false;
            this.m_oShipyardCreateTaskGroupBox.Text = "Create Task(Title)";
            // 
            // m_oRepairRefitScrapClassComboBox
            // 
            this.m_oRepairRefitScrapClassComboBox.FormattingEnabled = true;
            this.m_oRepairRefitScrapClassComboBox.Location = new System.Drawing.Point(75, 45);
            this.m_oRepairRefitScrapClassComboBox.Name = "m_oRepairRefitScrapClassComboBox";
            this.m_oRepairRefitScrapClassComboBox.Size = new System.Drawing.Size(213, 21);
            this.m_oRepairRefitScrapClassComboBox.TabIndex = 30;
            // 
            // m_oRepairRefitScrapLabel
            // 
            this.m_oRepairRefitScrapLabel.AutoSize = true;
            this.m_oRepairRefitScrapLabel.Location = new System.Drawing.Point(6, 48);
            this.m_oRepairRefitScrapLabel.Name = "m_oRepairRefitScrapLabel";
            this.m_oRepairRefitScrapLabel.Size = new System.Drawing.Size(30, 13);
            this.m_oRepairRefitScrapLabel.TabIndex = 29;
            this.m_oRepairRefitScrapLabel.Text = "RRS";
            // 
            // m_oRepairRefitScrapShipComboBox
            // 
            this.m_oRepairRefitScrapShipComboBox.FormattingEnabled = true;
            this.m_oRepairRefitScrapShipComboBox.Location = new System.Drawing.Point(393, 44);
            this.m_oRepairRefitScrapShipComboBox.Name = "m_oRepairRefitScrapShipComboBox";
            this.m_oRepairRefitScrapShipComboBox.Size = new System.Drawing.Size(216, 21);
            this.m_oRepairRefitScrapShipComboBox.TabIndex = 28;
            this.m_oRepairRefitScrapShipComboBox.Visible = false;
            // 
            // m_oSYShipNameTextBox
            // 
            this.m_oSYShipNameTextBox.Location = new System.Drawing.Point(393, 45);
            this.m_oSYShipNameTextBox.Name = "m_oSYShipNameTextBox";
            this.m_oSYShipNameTextBox.Size = new System.Drawing.Size(216, 20);
            this.m_oSYShipNameTextBox.TabIndex = 27;
            this.m_oSYShipNameTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oSYTaskGroupComboBox
            // 
            this.m_oSYTaskGroupComboBox.FormattingEnabled = true;
            this.m_oSYTaskGroupComboBox.Location = new System.Drawing.Point(393, 71);
            this.m_oSYTaskGroupComboBox.Name = "m_oSYTaskGroupComboBox";
            this.m_oSYTaskGroupComboBox.Size = new System.Drawing.Size(216, 21);
            this.m_oSYTaskGroupComboBox.TabIndex = 26;
            // 
            // m_oSYNewClassComboBox
            // 
            this.m_oSYNewClassComboBox.FormattingEnabled = true;
            this.m_oSYNewClassComboBox.Location = new System.Drawing.Point(393, 18);
            this.m_oSYNewClassComboBox.Name = "m_oSYNewClassComboBox";
            this.m_oSYNewClassComboBox.Size = new System.Drawing.Size(216, 21);
            this.m_oSYNewClassComboBox.TabIndex = 25;
            // 
            // m_oTaskGroupLabel
            // 
            this.m_oTaskGroupLabel.AutoSize = true;
            this.m_oTaskGroupLabel.Location = new System.Drawing.Point(324, 74);
            this.m_oTaskGroupLabel.Name = "m_oTaskGroupLabel";
            this.m_oTaskGroupLabel.Size = new System.Drawing.Size(63, 13);
            this.m_oTaskGroupLabel.TabIndex = 24;
            this.m_oTaskGroupLabel.Text = "Task Group";
            // 
            // m_oShipNameLabel
            // 
            this.m_oShipNameLabel.AutoSize = true;
            this.m_oShipNameLabel.Location = new System.Drawing.Point(324, 48);
            this.m_oShipNameLabel.Name = "m_oShipNameLabel";
            this.m_oShipNameLabel.Size = new System.Drawing.Size(59, 13);
            this.m_oShipNameLabel.TabIndex = 23;
            this.m_oShipNameLabel.Text = "Ship Name";
            // 
            // m_oNewClassLabel
            // 
            this.m_oNewClassLabel.AutoSize = true;
            this.m_oNewClassLabel.Location = new System.Drawing.Point(324, 21);
            this.m_oNewClassLabel.Name = "m_oNewClassLabel";
            this.m_oNewClassLabel.Size = new System.Drawing.Size(57, 13);
            this.m_oNewClassLabel.TabIndex = 22;
            this.m_oNewClassLabel.Text = "New Class";
            // 
            // m_oSYTaskTypeComboBox
            // 
            this.m_oSYTaskTypeComboBox.FormattingEnabled = true;
            this.m_oSYTaskTypeComboBox.Location = new System.Drawing.Point(75, 19);
            this.m_oSYTaskTypeComboBox.Name = "m_oSYTaskTypeComboBox";
            this.m_oSYTaskTypeComboBox.Size = new System.Drawing.Size(213, 21);
            this.m_oSYTaskTypeComboBox.TabIndex = 19;
            // 
            // m_oCreateDefaultnameCheckBox
            // 
            this.m_oCreateDefaultnameCheckBox.AutoSize = true;
            this.m_oCreateDefaultnameCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oCreateDefaultnameCheckBox.Checked = true;
            this.m_oCreateDefaultnameCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.m_oCreateDefaultnameCheckBox.Location = new System.Drawing.Point(162, 73);
            this.m_oCreateDefaultnameCheckBox.Name = "m_oCreateDefaultnameCheckBox";
            this.m_oCreateDefaultnameCheckBox.Size = new System.Drawing.Size(126, 17);
            this.m_oCreateDefaultnameCheckBox.TabIndex = 21;
            this.m_oCreateDefaultnameCheckBox.Text = "Create default names";
            this.m_oCreateDefaultnameCheckBox.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 108);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(30, 13);
            this.label1.TabIndex = 20;
            this.label1.Text = "Date";
            // 
            // m_oSYTaskCompletionDateTextBox
            // 
            this.m_oSYTaskCompletionDateTextBox.Location = new System.Drawing.Point(75, 97);
            this.m_oSYTaskCompletionDateTextBox.Name = "m_oSYTaskCompletionDateTextBox";
            this.m_oSYTaskCompletionDateTextBox.ReadOnly = true;
            this.m_oSYTaskCompletionDateTextBox.Size = new System.Drawing.Size(534, 20);
            this.m_oSYTaskCompletionDateTextBox.TabIndex = 19;
            this.m_oSYTaskCompletionDateTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oSYTaskCostTextBox
            // 
            this.m_oSYTaskCostTextBox.Location = new System.Drawing.Point(75, 71);
            this.m_oSYTaskCostTextBox.Name = "m_oSYTaskCostTextBox";
            this.m_oSYTaskCostTextBox.ReadOnly = true;
            this.m_oSYTaskCostTextBox.Size = new System.Drawing.Size(69, 20);
            this.m_oSYTaskCostTextBox.TabIndex = 19;
            this.m_oSYTaskCostTextBox.Text = "0";
            this.m_oSYTaskCostTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oCompletionDateLabel2
            // 
            this.m_oCompletionDateLabel2.AutoSize = true;
            this.m_oCompletionDateLabel2.Location = new System.Drawing.Point(6, 94);
            this.m_oCompletionDateLabel2.Name = "m_oCompletionDateLabel2";
            this.m_oCompletionDateLabel2.Size = new System.Drawing.Size(59, 13);
            this.m_oCompletionDateLabel2.TabIndex = 19;
            this.m_oCompletionDateLabel2.Text = "Completion";
            // 
            // m_oBuildCostLabel2
            // 
            this.m_oBuildCostLabel2.AutoSize = true;
            this.m_oBuildCostLabel2.Location = new System.Drawing.Point(6, 74);
            this.m_oBuildCostLabel2.Name = "m_oBuildCostLabel2";
            this.m_oBuildCostLabel2.Size = new System.Drawing.Size(54, 13);
            this.m_oBuildCostLabel2.TabIndex = 19;
            this.m_oBuildCostLabel2.Text = "Build Cost";
            // 
            // m_oTaskTypelabel2
            // 
            this.m_oTaskTypelabel2.AutoSize = true;
            this.m_oTaskTypelabel2.Location = new System.Drawing.Point(6, 22);
            this.m_oTaskTypelabel2.Name = "m_oTaskTypelabel2";
            this.m_oTaskTypelabel2.Size = new System.Drawing.Size(58, 13);
            this.m_oTaskTypelabel2.TabIndex = 19;
            this.m_oTaskTypelabel2.Text = "Task Type";
            // 
            // m_oDefaultFleetButton
            // 
            this.m_oDefaultFleetButton.Location = new System.Drawing.Point(95, 141);
            this.m_oDefaultFleetButton.Name = "m_oDefaultFleetButton";
            this.m_oDefaultFleetButton.Size = new System.Drawing.Size(78, 23);
            this.m_oDefaultFleetButton.TabIndex = 6;
            this.m_oDefaultFleetButton.Text = "Default Fleet";
            this.m_oDefaultFleetButton.UseVisualStyleBackColor = true;
            // 
            // m_oAddTaskButton
            // 
            this.m_oAddTaskButton.Location = new System.Drawing.Point(6, 141);
            this.m_oAddTaskButton.Name = "m_oAddTaskButton";
            this.m_oAddTaskButton.Size = new System.Drawing.Size(78, 23);
            this.m_oAddTaskButton.TabIndex = 5;
            this.m_oAddTaskButton.Text = "&Add Task";
            this.m_oAddTaskButton.UseVisualStyleBackColor = true;
            // 
            // m_oRefitDetailsButton
            // 
            this.m_oRefitDetailsButton.Location = new System.Drawing.Point(651, 141);
            this.m_oRefitDetailsButton.Name = "m_oRefitDetailsButton";
            this.m_oRefitDetailsButton.Size = new System.Drawing.Size(78, 23);
            this.m_oRefitDetailsButton.TabIndex = 4;
            this.m_oRefitDetailsButton.Text = "Refit Details";
            this.m_oRefitDetailsButton.UseVisualStyleBackColor = true;
            // 
            // m_oNewNameButton
            // 
            this.m_oNewNameButton.Location = new System.Drawing.Point(735, 141);
            this.m_oNewNameButton.Name = "m_oNewNameButton";
            this.m_oNewNameButton.Size = new System.Drawing.Size(78, 23);
            this.m_oNewNameButton.TabIndex = 3;
            this.m_oNewNameButton.Text = "New Na&mes";
            this.m_oNewNameButton.UseVisualStyleBackColor = true;
            // 
            // m_oReqMat2Label
            // 
            this.m_oReqMat2Label.AutoSize = true;
            this.m_oReqMat2Label.Location = new System.Drawing.Point(648, 15);
            this.m_oReqMat2Label.Name = "m_oReqMat2Label";
            this.m_oReqMat2Label.Size = new System.Drawing.Size(95, 13);
            this.m_oReqMat2Label.TabIndex = 2;
            this.m_oReqMat2Label.Text = "Required Materials";
            // 
            // m_oShipRequiredMaterialsListBox
            // 
            this.m_oShipRequiredMaterialsListBox.FormattingEnabled = true;
            this.m_oShipRequiredMaterialsListBox.Location = new System.Drawing.Point(651, 32);
            this.m_oShipRequiredMaterialsListBox.Name = "m_oShipRequiredMaterialsListBox";
            this.m_oShipRequiredMaterialsListBox.Size = new System.Drawing.Size(162, 95);
            this.m_oShipRequiredMaterialsListBox.TabIndex = 0;
            // 
            // m_oShipyardTaskGroupBox
            // 
            this.m_oShipyardTaskGroupBox.Controls.Add(this.m_oExpandCapUntilXTextBox);
            this.m_oShipyardTaskGroupBox.Controls.Add(this.m_oSYCNewShipClassComboBox);
            this.m_oShipyardTaskGroupBox.Controls.Add(this.m_oSYCShipClassLabel);
            this.m_oShipyardTaskGroupBox.Controls.Add(this.m_oSYCTaskTypeComboBox);
            this.m_oShipyardTaskGroupBox.Controls.Add(this.m_oSYCBuildCostTextBox);
            this.m_oShipyardTaskGroupBox.Controls.Add(this.m_oSYCCompletionDateTextBox);
            this.m_oShipyardTaskGroupBox.Controls.Add(this.m_oCompletionDateLabel);
            this.m_oShipyardTaskGroupBox.Controls.Add(this.m_oBuildCostLabel);
            this.m_oShipyardTaskGroupBox.Controls.Add(this.m_oTaskTypeLabel);
            this.m_oShipyardTaskGroupBox.Controls.Add(this.m_oSMSYButton);
            this.m_oShipyardTaskGroupBox.Controls.Add(this.m_oAutoRenameButton);
            this.m_oShipyardTaskGroupBox.Controls.Add(this.m_oRenameSYButton);
            this.m_oShipyardTaskGroupBox.Controls.Add(this.m_oPauseActivityButton);
            this.m_oShipyardTaskGroupBox.Controls.Add(this.m_oDeleteActivityButton);
            this.m_oShipyardTaskGroupBox.Controls.Add(this.m_oSetActivityButton);
            this.m_oShipyardTaskGroupBox.Controls.Add(this.m_oReqMatLabel);
            this.m_oShipyardTaskGroupBox.Controls.Add(this.m_oSYCRequiredMaterialsListBox);
            this.m_oShipyardTaskGroupBox.Location = new System.Drawing.Point(6, 373);
            this.m_oShipyardTaskGroupBox.Name = "m_oShipyardTaskGroupBox";
            this.m_oShipyardTaskGroupBox.Size = new System.Drawing.Size(819, 135);
            this.m_oShipyardTaskGroupBox.TabIndex = 1;
            this.m_oShipyardTaskGroupBox.TabStop = false;
            this.m_oShipyardTaskGroupBox.Text = "Shipyard Complex Activity(Title)";
            // 
            // m_oExpandCapUntilXTextBox
            // 
            this.m_oExpandCapUntilXTextBox.Location = new System.Drawing.Point(393, 31);
            this.m_oExpandCapUntilXTextBox.Name = "m_oExpandCapUntilXTextBox";
            this.m_oExpandCapUntilXTextBox.Size = new System.Drawing.Size(216, 20);
            this.m_oExpandCapUntilXTextBox.TabIndex = 21;
            this.m_oExpandCapUntilXTextBox.Text = "0";
            this.m_oExpandCapUntilXTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.m_oExpandCapUntilXTextBox.Visible = false;
            // 
            // m_oSYCNewShipClassComboBox
            // 
            this.m_oSYCNewShipClassComboBox.FormattingEnabled = true;
            this.m_oSYCNewShipClassComboBox.Location = new System.Drawing.Point(393, 31);
            this.m_oSYCNewShipClassComboBox.Name = "m_oSYCNewShipClassComboBox";
            this.m_oSYCNewShipClassComboBox.Size = new System.Drawing.Size(216, 21);
            this.m_oSYCNewShipClassComboBox.TabIndex = 20;
            // 
            // m_oSYCShipClassLabel
            // 
            this.m_oSYCShipClassLabel.AutoSize = true;
            this.m_oSYCShipClassLabel.Location = new System.Drawing.Point(324, 34);
            this.m_oSYCShipClassLabel.Name = "m_oSYCShipClassLabel";
            this.m_oSYCShipClassLabel.Size = new System.Drawing.Size(56, 13);
            this.m_oSYCShipClassLabel.TabIndex = 19;
            this.m_oSYCShipClassLabel.Text = "Ship Class";
            // 
            // m_oSYCTaskTypeComboBox
            // 
            this.m_oSYCTaskTypeComboBox.FormattingEnabled = true;
            this.m_oSYCTaskTypeComboBox.Location = new System.Drawing.Point(75, 31);
            this.m_oSYCTaskTypeComboBox.Name = "m_oSYCTaskTypeComboBox";
            this.m_oSYCTaskTypeComboBox.Size = new System.Drawing.Size(213, 21);
            this.m_oSYCTaskTypeComboBox.TabIndex = 18;
            // 
            // m_oSYCBuildCostTextBox
            // 
            this.m_oSYCBuildCostTextBox.Location = new System.Drawing.Point(75, 71);
            this.m_oSYCBuildCostTextBox.Name = "m_oSYCBuildCostTextBox";
            this.m_oSYCBuildCostTextBox.ReadOnly = true;
            this.m_oSYCBuildCostTextBox.Size = new System.Drawing.Size(69, 20);
            this.m_oSYCBuildCostTextBox.TabIndex = 17;
            this.m_oSYCBuildCostTextBox.Text = "0";
            this.m_oSYCBuildCostTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oSYCCompletionDateTextBox
            // 
            this.m_oSYCCompletionDateTextBox.Location = new System.Drawing.Point(254, 71);
            this.m_oSYCCompletionDateTextBox.Name = "m_oSYCCompletionDateTextBox";
            this.m_oSYCCompletionDateTextBox.ReadOnly = true;
            this.m_oSYCCompletionDateTextBox.Size = new System.Drawing.Size(355, 20);
            this.m_oSYCCompletionDateTextBox.TabIndex = 16;
            this.m_oSYCCompletionDateTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oCompletionDateLabel
            // 
            this.m_oCompletionDateLabel.AutoSize = true;
            this.m_oCompletionDateLabel.Location = new System.Drawing.Point(162, 74);
            this.m_oCompletionDateLabel.Name = "m_oCompletionDateLabel";
            this.m_oCompletionDateLabel.Size = new System.Drawing.Size(85, 13);
            this.m_oCompletionDateLabel.TabIndex = 15;
            this.m_oCompletionDateLabel.Text = "Completion Date";
            // 
            // m_oBuildCostLabel
            // 
            this.m_oBuildCostLabel.AutoSize = true;
            this.m_oBuildCostLabel.Location = new System.Drawing.Point(6, 74);
            this.m_oBuildCostLabel.Name = "m_oBuildCostLabel";
            this.m_oBuildCostLabel.Size = new System.Drawing.Size(54, 13);
            this.m_oBuildCostLabel.TabIndex = 14;
            this.m_oBuildCostLabel.Text = "Build Cost";
            // 
            // m_oTaskTypeLabel
            // 
            this.m_oTaskTypeLabel.AutoSize = true;
            this.m_oTaskTypeLabel.Location = new System.Drawing.Point(6, 34);
            this.m_oTaskTypeLabel.Name = "m_oTaskTypeLabel";
            this.m_oTaskTypeLabel.Size = new System.Drawing.Size(58, 13);
            this.m_oTaskTypeLabel.TabIndex = 13;
            this.m_oTaskTypeLabel.Text = "Task Type";
            // 
            // m_oSMSYButton
            // 
            this.m_oSMSYButton.Location = new System.Drawing.Point(561, 106);
            this.m_oSMSYButton.Name = "m_oSMSYButton";
            this.m_oSMSYButton.Size = new System.Drawing.Size(84, 23);
            this.m_oSMSYButton.TabIndex = 12;
            this.m_oSMSYButton.Text = "SM SY Mod";
            this.m_oSMSYButton.UseVisualStyleBackColor = true;
            this.m_oSMSYButton.Visible = false;
            // 
            // m_oAutoRenameButton
            // 
            this.m_oAutoRenameButton.Location = new System.Drawing.Point(399, 106);
            this.m_oAutoRenameButton.Name = "m_oAutoRenameButton";
            this.m_oAutoRenameButton.Size = new System.Drawing.Size(84, 23);
            this.m_oAutoRenameButton.TabIndex = 11;
            this.m_oAutoRenameButton.Text = "Auto-Rename";
            this.m_oAutoRenameButton.UseVisualStyleBackColor = true;
            // 
            // m_oRenameSYButton
            // 
            this.m_oRenameSYButton.Location = new System.Drawing.Point(304, 106);
            this.m_oRenameSYButton.Name = "m_oRenameSYButton";
            this.m_oRenameSYButton.Size = new System.Drawing.Size(84, 23);
            this.m_oRenameSYButton.TabIndex = 10;
            this.m_oRenameSYButton.Text = "Rename SY";
            this.m_oRenameSYButton.UseVisualStyleBackColor = true;
            // 
            // m_oPauseActivityButton
            // 
            this.m_oPauseActivityButton.Location = new System.Drawing.Point(204, 106);
            this.m_oPauseActivityButton.Name = "m_oPauseActivityButton";
            this.m_oPauseActivityButton.Size = new System.Drawing.Size(84, 23);
            this.m_oPauseActivityButton.TabIndex = 9;
            this.m_oPauseActivityButton.Text = "Pause Activity";
            this.m_oPauseActivityButton.UseVisualStyleBackColor = true;
            // 
            // m_oDeleteActivityButton
            // 
            this.m_oDeleteActivityButton.Location = new System.Drawing.Point(105, 106);
            this.m_oDeleteActivityButton.Name = "m_oDeleteActivityButton";
            this.m_oDeleteActivityButton.Size = new System.Drawing.Size(84, 23);
            this.m_oDeleteActivityButton.TabIndex = 8;
            this.m_oDeleteActivityButton.Text = "Delete Activity";
            this.m_oDeleteActivityButton.UseVisualStyleBackColor = true;
            // 
            // m_oSetActivityButton
            // 
            this.m_oSetActivityButton.Location = new System.Drawing.Point(6, 106);
            this.m_oSetActivityButton.Name = "m_oSetActivityButton";
            this.m_oSetActivityButton.Size = new System.Drawing.Size(84, 23);
            this.m_oSetActivityButton.TabIndex = 7;
            this.m_oSetActivityButton.Text = "Set Activity";
            this.m_oSetActivityButton.UseVisualStyleBackColor = true;
            // 
            // m_oReqMatLabel
            // 
            this.m_oReqMatLabel.AutoSize = true;
            this.m_oReqMatLabel.Location = new System.Drawing.Point(648, 16);
            this.m_oReqMatLabel.Name = "m_oReqMatLabel";
            this.m_oReqMatLabel.Size = new System.Drawing.Size(95, 13);
            this.m_oReqMatLabel.TabIndex = 1;
            this.m_oReqMatLabel.Text = "Required Materials";
            // 
            // m_oSYCRequiredMaterialsListBox
            // 
            this.m_oSYCRequiredMaterialsListBox.FormattingEnabled = true;
            this.m_oSYCRequiredMaterialsListBox.Location = new System.Drawing.Point(651, 34);
            this.m_oSYCRequiredMaterialsListBox.Name = "m_oSYCRequiredMaterialsListBox";
            this.m_oSYCRequiredMaterialsListBox.Size = new System.Drawing.Size(162, 95);
            this.m_oSYCRequiredMaterialsListBox.TabIndex = 0;
            // 
            // m_oShipyardListGroupBox
            // 
            this.m_oShipyardListGroupBox.Location = new System.Drawing.Point(7, 7);
            this.m_oShipyardListGroupBox.Name = "m_oShipyardListGroupBox";
            this.m_oShipyardListGroupBox.Size = new System.Drawing.Size(818, 360);
            this.m_oShipyardListGroupBox.TabIndex = 0;
            this.m_oShipyardListGroupBox.TabStop = false;
            this.m_oShipyardListGroupBox.Text = "Total Shipyard Capacity:";
            // 
            // m_oShipyardTaskTab
            // 
            this.m_oShipyardTaskTab.Controls.Add(this.m_oAnnualBuildRateLabel);
            this.m_oShipyardTaskTab.Controls.Add(this.m_oSYActivityButtonGroupBox);
            this.m_oShipyardTaskTab.Controls.Add(this.m_oSYActivityGroupBox);
            this.m_oShipyardTaskTab.Location = new System.Drawing.Point(4, 40);
            this.m_oShipyardTaskTab.Name = "m_oShipyardTaskTab";
            this.m_oShipyardTaskTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oShipyardTaskTab.Size = new System.Drawing.Size(831, 690);
            this.m_oShipyardTaskTab.TabIndex = 4;
            this.m_oShipyardTaskTab.Text = "Shipyard Tasks";
            this.m_oShipyardTaskTab.UseVisualStyleBackColor = true;
            // 
            // m_oAnnualBuildRateLabel
            // 
            this.m_oAnnualBuildRateLabel.AutoSize = true;
            this.m_oAnnualBuildRateLabel.Location = new System.Drawing.Point(211, 25);
            this.m_oAnnualBuildRateLabel.Name = "m_oAnnualBuildRateLabel";
            this.m_oAnnualBuildRateLabel.Size = new System.Drawing.Size(405, 13);
            this.m_oAnnualBuildRateLabel.TabIndex = 2;
            this.m_oAnnualBuildRateLabel.Text = "Annual Ship Building Rate Per Slipway for 5000 ton ship: 111    Available Slipway" +
                "s: 1";
            // 
            // m_oSYActivityButtonGroupBox
            // 
            this.m_oSYActivityButtonGroupBox.Controls.Add(this.m_oSYARenameShipButton);
            this.m_oSYActivityButtonGroupBox.Controls.Add(this.m_oSYAScheduleButton);
            this.m_oSYActivityButtonGroupBox.Controls.Add(this.m_oSYALowerPriorityButton);
            this.m_oSYActivityButtonGroupBox.Controls.Add(this.m_oSYARaisePriorityButton);
            this.m_oSYActivityButtonGroupBox.Controls.Add(this.m_oSYAPauseTaskButton);
            this.m_oSYActivityButtonGroupBox.Controls.Add(this.button1);
            this.m_oSYActivityButtonGroupBox.Location = new System.Drawing.Point(6, 622);
            this.m_oSYActivityButtonGroupBox.Name = "m_oSYActivityButtonGroupBox";
            this.m_oSYActivityButtonGroupBox.Size = new System.Drawing.Size(819, 62);
            this.m_oSYActivityButtonGroupBox.TabIndex = 1;
            this.m_oSYActivityButtonGroupBox.TabStop = false;
            // 
            // m_oSYARenameShipButton
            // 
            this.m_oSYARenameShipButton.Location = new System.Drawing.Point(510, 26);
            this.m_oSYARenameShipButton.Name = "m_oSYARenameShipButton";
            this.m_oSYARenameShipButton.Size = new System.Drawing.Size(84, 23);
            this.m_oSYARenameShipButton.TabIndex = 14;
            this.m_oSYARenameShipButton.Text = "Rename Ship";
            this.m_oSYARenameShipButton.UseVisualStyleBackColor = true;
            // 
            // m_oSYAScheduleButton
            // 
            this.m_oSYAScheduleButton.Location = new System.Drawing.Point(408, 26);
            this.m_oSYAScheduleButton.Name = "m_oSYAScheduleButton";
            this.m_oSYAScheduleButton.Size = new System.Drawing.Size(84, 23);
            this.m_oSYAScheduleButton.TabIndex = 13;
            this.m_oSYAScheduleButton.Text = "Schedule";
            this.m_oSYAScheduleButton.UseVisualStyleBackColor = true;
            // 
            // m_oSYALowerPriorityButton
            // 
            this.m_oSYALowerPriorityButton.Location = new System.Drawing.Point(307, 26);
            this.m_oSYALowerPriorityButton.Name = "m_oSYALowerPriorityButton";
            this.m_oSYALowerPriorityButton.Size = new System.Drawing.Size(84, 23);
            this.m_oSYALowerPriorityButton.TabIndex = 12;
            this.m_oSYALowerPriorityButton.Text = "Lower Priority";
            this.m_oSYALowerPriorityButton.UseVisualStyleBackColor = true;
            // 
            // m_oSYARaisePriorityButton
            // 
            this.m_oSYARaisePriorityButton.Location = new System.Drawing.Point(208, 26);
            this.m_oSYARaisePriorityButton.Name = "m_oSYARaisePriorityButton";
            this.m_oSYARaisePriorityButton.Size = new System.Drawing.Size(84, 23);
            this.m_oSYARaisePriorityButton.TabIndex = 11;
            this.m_oSYARaisePriorityButton.Text = "Higher Priority";
            this.m_oSYARaisePriorityButton.UseVisualStyleBackColor = true;
            // 
            // m_oSYAPauseTaskButton
            // 
            this.m_oSYAPauseTaskButton.Location = new System.Drawing.Point(106, 26);
            this.m_oSYAPauseTaskButton.Name = "m_oSYAPauseTaskButton";
            this.m_oSYAPauseTaskButton.Size = new System.Drawing.Size(84, 23);
            this.m_oSYAPauseTaskButton.TabIndex = 10;
            this.m_oSYAPauseTaskButton.Text = "Pause Task";
            this.m_oSYAPauseTaskButton.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(6, 26);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(84, 23);
            this.button1.TabIndex = 9;
            this.button1.Text = "De&lete Task";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // m_oSYActivityGroupBox
            // 
            this.m_oSYActivityGroupBox.Location = new System.Drawing.Point(9, 56);
            this.m_oSYActivityGroupBox.Name = "m_oSYActivityGroupBox";
            this.m_oSYActivityGroupBox.Size = new System.Drawing.Size(811, 548);
            this.m_oSYActivityGroupBox.TabIndex = 0;
            this.m_oSYActivityGroupBox.TabStop = false;
            this.m_oSYActivityGroupBox.Text = "Shipyard Activity(1 Slipways)";
            // 
            // m_oResearchTab
            // 
            this.m_oResearchTab.Location = new System.Drawing.Point(4, 40);
            this.m_oResearchTab.Name = "m_oResearchTab";
            this.m_oResearchTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oResearchTab.Size = new System.Drawing.Size(831, 690);
            this.m_oResearchTab.TabIndex = 5;
            this.m_oResearchTab.Text = "Research";
            this.m_oResearchTab.UseVisualStyleBackColor = true;
            // 
            // m_oEnvironmentTab
            // 
            this.m_oEnvironmentTab.Location = new System.Drawing.Point(4, 40);
            this.m_oEnvironmentTab.Name = "m_oEnvironmentTab";
            this.m_oEnvironmentTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oEnvironmentTab.Size = new System.Drawing.Size(831, 690);
            this.m_oEnvironmentTab.TabIndex = 6;
            this.m_oEnvironmentTab.Text = "Environment / GMC";
            this.m_oEnvironmentTab.UseVisualStyleBackColor = true;
            // 
            // m_oTeamsTab
            // 
            this.m_oTeamsTab.Location = new System.Drawing.Point(4, 40);
            this.m_oTeamsTab.Name = "m_oTeamsTab";
            this.m_oTeamsTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oTeamsTab.Size = new System.Drawing.Size(831, 690);
            this.m_oTeamsTab.TabIndex = 7;
            this.m_oTeamsTab.Text = "Teams / Academy";
            this.m_oTeamsTab.UseVisualStyleBackColor = true;
            // 
            // m_oCivTab
            // 
            this.m_oCivTab.Location = new System.Drawing.Point(4, 40);
            this.m_oCivTab.Name = "m_oCivTab";
            this.m_oCivTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oCivTab.Size = new System.Drawing.Size(831, 690);
            this.m_oCivTab.TabIndex = 8;
            this.m_oCivTab.Text = "Civilians / Ind Status";
            this.m_oCivTab.UseVisualStyleBackColor = true;
            // 
            // m_oGUTab
            // 
            this.m_oGUTab.Location = new System.Drawing.Point(4, 40);
            this.m_oGUTab.Name = "m_oGUTab";
            this.m_oGUTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oGUTab.Size = new System.Drawing.Size(831, 690);
            this.m_oGUTab.TabIndex = 9;
            this.m_oGUTab.Text = "Ground Units";
            this.m_oGUTab.UseVisualStyleBackColor = true;
            // 
            // m_oGUTrainingTab
            // 
            this.m_oGUTrainingTab.Location = new System.Drawing.Point(4, 40);
            this.m_oGUTrainingTab.Name = "m_oGUTrainingTab";
            this.m_oGUTrainingTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oGUTrainingTab.Size = new System.Drawing.Size(831, 690);
            this.m_oGUTrainingTab.TabIndex = 10;
            this.m_oGUTrainingTab.Text = "GU Training";
            this.m_oGUTrainingTab.UseVisualStyleBackColor = true;
            // 
            // m_oWealthTab
            // 
            this.m_oWealthTab.Location = new System.Drawing.Point(4, 40);
            this.m_oWealthTab.Name = "m_oWealthTab";
            this.m_oWealthTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oWealthTab.Size = new System.Drawing.Size(831, 690);
            this.m_oWealthTab.TabIndex = 11;
            this.m_oWealthTab.Text = "Wealth / Trade";
            this.m_oWealthTab.UseVisualStyleBackColor = true;
            // 
            // Eco_Summary
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(1235, 878);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.m_oTimeGroupBox);
            this.Controls.Add(this.m_oIndustryControlGroupBox);
            this.Controls.Add(this.m_oPopulationGroupBox);
            this.Controls.Add(this.m_oEmpireGroupBox);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "Eco_Summary";
            this.m_oEmpireGroupBox.ResumeLayout(false);
            this.m_oPopulationGroupBox.ResumeLayout(false);
            this.m_oPopulationGroupBox.PerformLayout();
            this.m_oIndustryControlGroupBox.ResumeLayout(false);
            this.m_oTimeGroupBox.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.m_oSummaryTab.ResumeLayout(false);
            this.m_oSummaryTab.PerformLayout();
            this.m_oIndustryTab.ResumeLayout(false);
            this.m_oIndustryTab.PerformLayout();
            this.m_oFuelProductionGroupBox.ResumeLayout(false);
            this.m_oFuelProductionGroupBox.PerformLayout();
            this.m_oIndustrialProjectGroupBox.ResumeLayout(false);
            this.m_oIndustrialProjectGroupBox.PerformLayout();
            this.m_oIndustrialAllocationGroupBox.ResumeLayout(false);
            this.m_oPlanetFighterGroupBox.ResumeLayout(false);
            this.m_oPlanetMissileGroupBox.ResumeLayout(false);
            this.m_oPlanetPDCGroupBox.ResumeLayout(false);
            this.m_oShipComponentGroupBox.ResumeLayout(false);
            this.m_oConstructionOptionsGroupBox.ResumeLayout(false);
            this.m_oMiningTab.ResumeLayout(false);
            this.m_oMiningTab.PerformLayout();
            this.m_oMiningReportGroupBox.ResumeLayout(false);
            this.m_oMiningReportGroupBox.PerformLayout();
            this.m_oMassDriverDestGroupBox.ResumeLayout(false);
            this.m_oShipyardTab.ResumeLayout(false);
            this.m_oShipyardCreateTaskGroupBox.ResumeLayout(false);
            this.m_oShipyardCreateTaskGroupBox.PerformLayout();
            this.m_oShipyardTaskGroupBox.ResumeLayout(false);
            this.m_oShipyardTaskGroupBox.PerformLayout();
            this.m_oShipyardTaskTab.ResumeLayout(false);
            this.m_oShipyardTaskTab.PerformLayout();
            this.m_oSYActivityButtonGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox m_oEmpireGroupBox;
        private System.Windows.Forms.GroupBox m_oPopulationGroupBox;
        private System.Windows.Forms.GroupBox m_oIndustryControlGroupBox;
        private System.Windows.Forms.GroupBox m_oTimeGroupBox;
        private System.Windows.Forms.CheckBox m_oFunctionGroupCheckBox;
        private System.Windows.Forms.CheckBox m_oHideCMCCheckBox;
        private System.Windows.Forms.TreeView m_oPopulationTreeView;
        private System.Windows.Forms.Button m_oCloseButton;
        private System.Windows.Forms.Button m_oAbandonButton;
        private System.Windows.Forms.Button m_oMissileButton;
        private System.Windows.Forms.Button m_oTurretButton;
        private System.Windows.Forms.Button m_oDesignButton;
        private System.Windows.Forms.Button m_oGeoStatusButton;
        private System.Windows.Forms.Button m_oRefreshAllButton;
        private System.Windows.Forms.Button m_oTransferButton;
        private System.Windows.Forms.Button m_oSectorButton;
        private System.Windows.Forms.Button m_oRenameBodyButton;
        private System.Windows.Forms.Button m_oCapitolButton;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage m_oSummaryTab;
        private System.Windows.Forms.TabPage m_oIndustryTab;
        private System.Windows.Forms.TabPage m_oMiningTab;
        private System.Windows.Forms.TabPage m_oShipyardTab;
        private System.Windows.Forms.TabPage m_oShipyardTaskTab;
        private System.Windows.Forms.TabPage m_oResearchTab;
        private System.Windows.Forms.ComboBox m_oFactionComboBox;
        private System.Windows.Forms.GroupBox m_oSummaryGroupBox;
        private System.Windows.Forms.Button m_o30DaysButton;
        private System.Windows.Forms.Button m_o5DaysButton;
        private System.Windows.Forms.Button m_o1DayButton;
        private System.Windows.Forms.Button m_o8HoursButton;
        private System.Windows.Forms.Button m_o3HoursButton;
        private System.Windows.Forms.Button m_o1HourButton;
        private System.Windows.Forms.Button m_o20MinutesButton;
        private System.Windows.Forms.Button m_o5MinutesButton;
        private System.Windows.Forms.Button m_o30SecondsButton;
        private System.Windows.Forms.Button m_o5SecondsButton;
        private System.Windows.Forms.Button m_oSMModButton;
        private System.Windows.Forms.Button m_oAllResearchButton;
        private System.Windows.Forms.TextBox m_oSectorGovernorTextBox;
        private System.Windows.Forms.TextBox m_oPlanetaryGovernorTextBox;
        private System.Windows.Forms.Label m_oSectorGovernorLabel;
        private System.Windows.Forms.Label m_oPlanetaryGovernorLabel;
        private System.Windows.Forms.TabPage m_oEnvironmentTab;
        private System.Windows.Forms.TabPage m_oTeamsTab;
        private System.Windows.Forms.TabPage m_oCivTab;
        private System.Windows.Forms.TabPage m_oGUTab;
        private System.Windows.Forms.TabPage m_oGUTrainingTab;
        private System.Windows.Forms.TabPage m_oWealthTab;
        private GroupBox m_oFuelProductionGroupBox;
        private GroupBox m_oIndustrialProjectGroupBox;
        private GroupBox m_oIndustrialAllocationGroupBox;
        private GroupBox m_oConstructionOptionsGroupBox;
        private Button m_oStockpileButton;
        private Label m_oConstructionLabel;
        private GroupBox m_oPlanetFighterGroupBox;
        private GroupBox m_oPlanetMissileGroupBox;
        private GroupBox m_oPlanetPDCGroupBox;
        private GroupBox m_oShipComponentGroupBox;
        private Button m_oScrapFightersButton;
        private Button m_oScrapMissilesButton;
        private Button m_oDisassembleCompButton;
        private Button m_oScrapCompButton;
        private Label m_oFuelReservesLabel;
        private Label m_oFuelProductionlabel;
        private Label m_oRefineriesLabel;
        private Button m_oStartFuelButton;
        private Button m_oStopFuelButton;
        private Button m_oSMAddButton;
        private Button m_oPauseButton;
        private Button m_oCancelButton;
        private Button m_oModifyButton;
        private Button m_oCreateButton;
        private ListBox m_oFighterListBox;
        private ListBox m_oMissileStockListBox;
        private ListBox m_oPDCListBox;
        private ListBox m_oShipCompListBox;
        private Button m_oPriorityDownButton;
        private Button m_oPriorityUpButton;
        private TextBox m_oItemNumberTextBox;
        private TextBox m_oItemPercentTextBox;
        private Label m_oNewFightersLabel;
        private Label m_oPercentageLabel;
        private Label m_oItemNumberLabel;
        private ComboBox m_oNewFighterTaskGroupComboBox;
        private ComboBox m_oInstallationTypeComboBox;
        private ListBox m_oInstallationCostListBox;
        private GroupBox m_oInstallationGroupBox;
        private Label m_oUsageNoteLabel;
        private GroupBox m_oMaintFacilityGroupBox;
        private GroupBox m_oMiningReportGroupBox;
        private Label m_oMineProductionLabel;
        private GroupBox m_oMineralProductionGroupBox;
        private GroupBox m_oMassDriverDestGroupBox;
        private ComboBox m_oMassDriverDestinationComboBox;
        private GroupBox m_oShipyardCreateTaskGroupBox;
        private GroupBox m_oShipyardTaskGroupBox;
        private ListBox m_oSYCRequiredMaterialsListBox;
        private GroupBox m_oShipyardListGroupBox;
        private Label m_oReqMatLabel;
        private Button m_oRefitDetailsButton;
        private Button m_oNewNameButton;
        private Label m_oReqMat2Label;
        private ListBox m_oShipRequiredMaterialsListBox;
        private Button m_oDefaultFleetButton;
        private Button m_oAddTaskButton;
        private Button m_oAutoRenameButton;
        private Button m_oRenameSYButton;
        private Button m_oPauseActivityButton;
        private Button m_oDeleteActivityButton;
        private Button m_oSetActivityButton;
        private Button m_oSMSYButton;
        private Label label1;
        private TextBox m_oSYTaskCompletionDateTextBox;
        private TextBox m_oSYTaskCostTextBox;
        private Label m_oCompletionDateLabel2;
        private Label m_oBuildCostLabel2;
        private Label m_oTaskTypelabel2;
        private ComboBox m_oSYCTaskTypeComboBox;
        private TextBox m_oSYCBuildCostTextBox;
        private TextBox m_oSYCCompletionDateTextBox;
        private Label m_oCompletionDateLabel;
        private Label m_oBuildCostLabel;
        private Label m_oTaskTypeLabel;
        private ComboBox m_oSYTaskTypeComboBox;
        private CheckBox m_oCreateDefaultnameCheckBox;
        private TextBox m_oSYShipNameTextBox;
        private ComboBox m_oSYTaskGroupComboBox;
        private ComboBox m_oSYNewClassComboBox;
        private Label m_oTaskGroupLabel;
        private Label m_oShipNameLabel;
        private Label m_oNewClassLabel;
        private Label m_oAnnualBuildRateLabel;
        private GroupBox m_oSYActivityButtonGroupBox;
        private Button m_oSYARenameShipButton;
        private Button m_oSYAScheduleButton;
        private Button m_oSYALowerPriorityButton;
        private Button m_oSYARaisePriorityButton;
        private Button m_oSYAPauseTaskButton;
        private Button button1;
        private GroupBox m_oSYActivityGroupBox;
        private Label m_oSYCShipClassLabel;
        private ComboBox m_oSYCNewShipClassComboBox;
        private ComboBox m_oRepairRefitScrapShipComboBox;
        private TextBox m_oExpandCapUntilXTextBox;
        private ComboBox m_oRepairRefitScrapClassComboBox;
        private Label m_oRepairRefitScrapLabel;
    }
}
