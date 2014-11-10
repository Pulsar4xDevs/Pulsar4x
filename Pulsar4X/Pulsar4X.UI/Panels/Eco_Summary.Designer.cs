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
            this.m_oMiningTab = new System.Windows.Forms.TabPage();
            this.m_oShipyardTab = new System.Windows.Forms.TabPage();
            this.m_oShipyardTaskTab = new System.Windows.Forms.TabPage();
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
            this.m_oIndustryTab.Location = new System.Drawing.Point(4, 40);
            this.m_oIndustryTab.Name = "m_oIndustryTab";
            this.m_oIndustryTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oIndustryTab.Size = new System.Drawing.Size(831, 690);
            this.m_oIndustryTab.TabIndex = 1;
            this.m_oIndustryTab.Text = "Industry";
            this.m_oIndustryTab.UseVisualStyleBackColor = true;
            // 
            // m_oMiningTab
            // 
            this.m_oMiningTab.Location = new System.Drawing.Point(4, 40);
            this.m_oMiningTab.Name = "m_oMiningTab";
            this.m_oMiningTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oMiningTab.Size = new System.Drawing.Size(831, 690);
            this.m_oMiningTab.TabIndex = 2;
            this.m_oMiningTab.Text = "Mining/Maintenance";
            this.m_oMiningTab.UseVisualStyleBackColor = true;
            // 
            // m_oShipyardTab
            // 
            this.m_oShipyardTab.Location = new System.Drawing.Point(4, 40);
            this.m_oShipyardTab.Name = "m_oShipyardTab";
            this.m_oShipyardTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oShipyardTab.Size = new System.Drawing.Size(831, 690);
            this.m_oShipyardTab.TabIndex = 3;
            this.m_oShipyardTab.Text = "Manage Shipyards ";
            this.m_oShipyardTab.UseVisualStyleBackColor = true;
            // 
            // m_oShipyardTaskTab
            // 
            this.m_oShipyardTaskTab.Location = new System.Drawing.Point(4, 40);
            this.m_oShipyardTaskTab.Name = "m_oShipyardTaskTab";
            this.m_oShipyardTaskTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oShipyardTaskTab.Size = new System.Drawing.Size(831, 690);
            this.m_oShipyardTaskTab.TabIndex = 4;
            this.m_oShipyardTaskTab.Text = "Shipyard Tasks";
            this.m_oShipyardTaskTab.UseVisualStyleBackColor = true;
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
    }
}
