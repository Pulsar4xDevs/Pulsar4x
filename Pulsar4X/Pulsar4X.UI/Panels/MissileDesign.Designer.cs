using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using OpenTK;

namespace Pulsar4X.UI.Panels
{
    partial class MissileDesign
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Faction selection combo box.
        /// </summary>
        public ComboBox EmpireComboBox
        {
            get { return m_oEmpireComboBox; }
        }

        /// <summary>
        /// Missile engine selection combo box.
        /// </summary>
        public ComboBox MissileEngineComboBox
        {
            get { return m_oMissileEngineComboBox; }
        }

        /// <summary>
        /// Missile Series selection combobox.
        /// </summary>
        public ComboBox MSeriesComboBox
        {
            get { return m_oMSeriesComboBox; }
        }

        /// <summary>
        /// Ordnance selection combobox.
        /// </summary>
        public ComboBox PreviousOrdnanceComboBox
        {
            get { return m_oPreviousOrdnanceComboBox; }
        }

        /// <summary>
        /// Ordnance selection for submunition purposes.
        /// </summary>
        public ComboBox SubMunitionComboBox
        {
            get { return m_oSubMunitionComboBox; }
        }

        /// <summary>
        /// missile design close button.
        /// </summary>
        public Button CloseMDButton
        {
            get { return m_oCloseButton; }
        }

        /// <summary>
        /// player will enter the desired number of engines into this text box
        /// </summary>
        public TextBox NumEnginesTextBox
        {
            get { return m_oNumberEnginesTextBox; }
        }

        /// <summary>
        /// Engine size based on number of engines will be printed here.
        /// </summary>
        public TextBox TotalEngineSizeTextBox
        {
            get { return m_oTotalEngineSizeTextBox; }
        }

        /// <summary>
        /// Total cost of all engines will be printed here.
        /// </summary>
        public TextBox TotalEngineCostTextBox
        {
            get { return m_oTotalEngineCostTextBox; }
        }

        /// <summary>
        /// Total EP of all engines will be printed here.
        /// </summary>
        public TextBox TotalEPTextBox
        {
            get { return m_oTotalEPTextBox; }
        }

        /// <summary>
        /// player entered Warhead MSP value.
        /// </summary>
        public TextBox WHMSPTextBox
        {
            get { return m_oWHMSPTextBox; }
        }

        /// <summary>
        /// Translation of WHMSP into WHValue
        /// </summary>
        public TextBox WHValueTextBox
        {
            get { return m_oWHValueTextBox; }
        }

        /// <summary>
        /// Player entered fuel MSP value.
        /// </summary>
        public TextBox FuelMSPTextBox
        {
            get { return m_oFuelMSPTextBox; }
        }

        /// <summary>
        /// Fuel value derived from MSP
        /// </summary>
        public TextBox FuelValueTextBox
        {
            get { return m_oFuelValueTextBox; }
        }

        /// <summary>
        /// player agility MSP value
        /// </summary>
        public TextBox AgilityMSPTextBox
        {
            get { return m_oAgilityMSPTextBox; }
        }

        /// <summary>
        /// Agility rating from player entry.
        /// </summary>
        public TextBox AgilityValueTextBox
        {
            get { return m_oAgilityValueTextBox; }
        }

        /// <summary>
        /// Both the player entered name and the automatically generated name will go here.
        /// </summary>
        public TextBox MissileNameTextBox
        {
            get { return m_oMissileNameTextBox; }
        }

        /// <summary>
        /// MSP devoted to active sensor space
        /// </summary>
        public TextBox ActiveMSPTextBox
        {
            get { return m_oActiveMSPTextBox; }
        }

        /// <summary>
        /// Strength of active sensor based on MSP.
        /// </summary>
        public TextBox ActiveValueTextBox
        {
            get { return m_oActiveValueTextBox; }
        }

        /// <summary>
        /// MSP devoted to passive thermal sensor.
        /// </summary>
        public TextBox ThermalMSPTextBox
        {
            get { return m_oThermalMSPTextBox; }
        }

        /// <summary>
        /// strength of thermal passive sensor based on MSP
        /// </summary>
        public TextBox ThermalValueTextBox
        {
            get { return m_oThermalValueTextBox; }
        }

        /// <summary>
        /// MSP devoted to EM sensor
        /// </summary>
        public TextBox EMMSPTextBox
        {
            get { return m_oEMMSPTextBox; }
        }

        /// <summary>
        /// Strength of EM sensor based on MSP
        /// </summary>
        public TextBox EMValueTextBox
        {
            get { return m_oEMValueTextBox; }
        }

        /// <summary>
        /// MSP devoted to Geo sensors
        /// </summary>
        public TextBox GeoMSPTextBox
        {
            get { return m_oGeoMSPTextBox; }
        }

        /// <summary>
        /// Geo points generated per hour by missile
        /// </summary>
        public TextBox GeoValueTextBox
        {
            get { return m_oGeoValueTextBox; }
        }

        /// <summary>
        /// Resolution for active sensor if present
        /// </summary>
        public TextBox ResolutionTextBox
        {
            get { return m_oResolutionTextBox; }
        }

        /// <summary>
        /// MSP devoted to Reactor to account for required reactor value.
        /// </summary>
        public TextBox ReactorMSPTextBox
        {
            get { return m_oReactorMSPTextBox; }
        }

        /// <summary>
        /// Summation of the strength of every sensor / 5
        /// </summary>
        public TextBox ReactorValueTextBox
        {
            get { return m_oReactorValueTextBox; }
        }

        /// <summary>
        /// MSP devoted to Reactor to account for required reactor value.
        /// </summary>
        public TextBox ArmourMSPTextBox
        {
            get { return m_oArmourMSPTextBox; }
        }

        /// <summary>
        /// Summation of the strength of every sensor / 5
        /// </summary>
        public TextBox ArmourValueTextBox
        {
            get { return m_oArmourValueTextBox; }
        }

        /// <summary>
        /// MSP devoted to Reactor to account for required reactor value.
        /// </summary>
        public TextBox ECMMSPTextBox
        {
            get { return m_oECMMSPTextBox; }
        }

        /// <summary>
        /// Summation of the strength of every sensor / 5
        /// </summary>
        public TextBox ECMValueTextBox
        {
            get { return m_oECMValueTextBox; }
        }

        /// <summary>
        /// Separation range text box holds the player entered desired separation range for submunitions.
        /// </summary>
        public TextBox SepRangeTextBox
        {
            get { return m_oSepRangeTextBox; }
        }

        /// <summary>
        /// Player entry for number of submunitions to include on this ordnance.
        /// </summary>
        public TextBox SubNumberTextBox
        {
            get { return m_oSubNumberTextBox; }
        }

        /// <summary>
        /// size of selected submunition
        /// </summary>
        public TextBox SubSizeTextBox
        {
            get { return m_oSubSizeTextBox; }
        }
        
        /// <summary>
        /// Total size of all submunitions on this missile.
        /// </summary>
        public TextBox SubTotalSizeTextBox
        {
            get { return m_oSubTotalSizeTextBox; }
        }

        /// <summary>
        /// Cost of selected Submunition.
        /// </summary>
        public TextBox SubCostTextBox
        {
            get { return m_oSubCostTextBox; }
        }

        /// <summary>
        /// Cost of all submunitions on this missile
        /// </summary>
        public TextBox SubTotalCostTextBox
        {
            get { return m_oSubTotalCostTextBox; }
        }

        /// <summary>
        /// Listing of all required minerals to build the selected number of submunitions.
        /// </summary>
        public RichTextBox MaterialsRichTextBox
        {
            get { return m_oMaterialsRichTextBox; }
        }

        /// <summary>
        /// The overall missile summary will be printed to this box.
        /// </summary>
        public RichTextBox MissileSummaryTextBox
        {
            get { return m_oMissileSummaryTextBox; }
        }

        /// <summary>
        /// An enhanced radiation warhead?
        /// </summary>
        public CheckBox ERCheckBox
        {
            get { return m_oERCheckBox; }
        }

        /// <summary>
        /// Is this a laser warhead?
        /// </summary>
        public CheckBox LaserWCheckBox
        {
            get { return m_oLaserWCheckBox; }
        }

        /// <summary>
        /// List of missiles in this missile series.
        /// </summary>
        public ListBox MSeriesListBox
        {
            get { return m_oMSeriesListBox; }
        }

        /// <summary>
        /// zeros out the current design page.
        /// </summary>
        public Button ClearDesignButton
        {
            get { return m_oClearDesignButton; }
        }

        /// <summary>
        /// Creates a new missile series
        /// </summary>
        public Button CreateSeriesButton
        {
            get { return m_oCreateSeriesButton; }
        }

        /// <summary>
        /// Deletes an existing Series
        /// </summary>
        public Button DeleteSeriesButton
        {
            get { return m_oDeleteSeriesButton; }
        }

        /// <summary>
        /// Sets the previous missile selections series to the currently selected series.
        /// </summary>
        public Button SetSeriesButton
        {
            get { return m_oSetSeriesButton; }
        }

        /// <summary>
        /// Instantly makes a missile definition
        /// </summary>
        public Button InstantButton
        {
            get { return m_oInstantButton; }
        }

        /// <summary>
        /// toggles between tech data, and general information on missiles.
        /// </summary>
        public Button ToggleInfoButton
        {
            get { return m_oInfoButton; }
        }

        /// <summary>
        /// goes through every ship, and replaces all older series members with the current missile
        /// </summary>
        public Button ReplaceAllButton
        {
            get { return m_oReplaceAllButton; }
        }

        /// <summary>
        /// Creates a missile research project based on the current specs.
        /// </summary>
        public Button CreateMissileButton
        {
            get { return m_oCreateButton; }
        }

        /// <summary>
        /// This label holds missile information, and can be set to not visible.
        /// </summary>
        public Label InfoLabel
        {
            get { return m_oInfoLabel; }
        }

        /// <summary>
        /// this Groupbox holds either info or tech depending on user selection.
        /// </summary>
        public GroupBox InfoGroupBox
        {
            get { return m_oInfoGroupBox; }
        }

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MissileDesign));
            this.m_oEmpireGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oEmpireComboBox = new System.Windows.Forms.ComboBox();
            this.m_oCompSizeBox = new System.Windows.Forms.GroupBox();
            this.m_oValueLabel = new System.Windows.Forms.Label();
            this.m_oMSPLabel = new System.Windows.Forms.Label();
            this.m_oParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oReactorLabel = new System.Windows.Forms.Label();
            this.m_oAgilityLabel = new System.Windows.Forms.Label();
            this.m_oFuelLabel = new System.Windows.Forms.Label();
            this.m_oWarheadLabel = new System.Windows.Forms.Label();
            this.m_oReactorMSPTextBox = new System.Windows.Forms.TextBox();
            this.m_oAgilityMSPTextBox = new System.Windows.Forms.TextBox();
            this.m_oFuelMSPTextBox = new System.Windows.Forms.TextBox();
            this.m_oWHMSPTextBox = new System.Windows.Forms.TextBox();
            this.m_oReactorValueTextBox = new System.Windows.Forms.TextBox();
            this.m_oAgilityValueTextBox = new System.Windows.Forms.TextBox();
            this.m_oFuelValueTextBox = new System.Windows.Forms.TextBox();
            this.m_oWHValueTextBox = new System.Windows.Forms.TextBox();
            this.m_oSensorGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oActiveResLabel = new System.Windows.Forms.Label();
            this.m_oResolutionTextBox = new System.Windows.Forms.TextBox();
            this.m_oGeoLabel = new System.Windows.Forms.Label();
            this.m_oActiveLabel = new System.Windows.Forms.Label();
            this.m_oEMLabel = new System.Windows.Forms.Label();
            this.m_oActiveValueTextBox = new System.Windows.Forms.TextBox();
            this.m_oThermalLabel = new System.Windows.Forms.Label();
            this.m_oThermalValueTextBox = new System.Windows.Forms.TextBox();
            this.m_oEMValueTextBox = new System.Windows.Forms.TextBox();
            this.m_oGeoMSPTextBox = new System.Windows.Forms.TextBox();
            this.m_oGeoValueTextBox = new System.Windows.Forms.TextBox();
            this.m_oEMMSPTextBox = new System.Windows.Forms.TextBox();
            this.m_oActiveMSPTextBox = new System.Windows.Forms.TextBox();
            this.m_oThermalMSPTextBox = new System.Windows.Forms.TextBox();
            this.m_oDefenceGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oArmourLabel = new System.Windows.Forms.Label();
            this.m_oArmourValueTextBox = new System.Windows.Forms.TextBox();
            this.m_oECMMSPTextBox = new System.Windows.Forms.TextBox();
            this.m_oECMLabel = new System.Windows.Forms.Label();
            this.m_oArmourMSPTextBox = new System.Windows.Forms.TextBox();
            this.m_oECMValueTextBox = new System.Windows.Forms.TextBox();
            this.m_oModGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oERCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oLaserWCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oMissileSeriesGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oMSeriesListBox = new System.Windows.Forms.ListBox();
            this.m_oMSeriesComboBox = new System.Windows.Forms.ComboBox();
            this.m_oPreviousDesignGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oPreviousOrdnanceComboBox = new System.Windows.Forms.ComboBox();
            this.m_oMissileEngineGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oTotalEngineSizeTextBox = new System.Windows.Forms.TextBox();
            this.m_oNumberEnginesTextBox = new System.Windows.Forms.TextBox();
            this.m_oTotalEngineCostTextBox = new System.Windows.Forms.TextBox();
            this.m_oTotalEPTextBox = new System.Windows.Forms.TextBox();
            this.m_oTotalCostLabel = new System.Windows.Forms.Label();
            this.m_oTotalEPLabel = new System.Windows.Forms.Label();
            this.m_oESizeLabel = new System.Windows.Forms.Label();
            this.m_oNumberLabel = new System.Windows.Forms.Label();
            this.m_oEngineLabel = new System.Windows.Forms.Label();
            this.m_oMissileEngineComboBox = new System.Windows.Forms.ComboBox();
            this.m_oSecondStageGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oSystemParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oCreateButton = new System.Windows.Forms.Button();
            this.m_oMissileNameTextBox = new System.Windows.Forms.TextBox();
            this.m_oMissileSummaryTextBox = new System.Windows.Forms.RichTextBox();
            this.m_oInfoGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oInfoLabel = new System.Windows.Forms.Label();
            this.m_oInfoButton = new System.Windows.Forms.Button();
            this.m_oCloseButton = new System.Windows.Forms.Button();
            this.m_oClearDesignButton = new System.Windows.Forms.Button();
            this.m_oCreateSeriesButton = new System.Windows.Forms.Button();
            this.m_oDeleteSeriesButton = new System.Windows.Forms.Button();
            this.m_oSetSeriesButton = new System.Windows.Forms.Button();
            this.m_oReplaceAllButton = new System.Windows.Forms.Button();
            this.m_oInstantButton = new System.Windows.Forms.Button();
            this.m_oSubMunitionComboBox = new System.Windows.Forms.ComboBox();
            this.m_oMissileTypeLabel = new System.Windows.Forms.Label();
            this.m_oSubNumberLabel = new System.Windows.Forms.Label();
            this.m_oSubSizeLabel = new System.Windows.Forms.Label();
            this.m_oTotalMSizeLabel = new System.Windows.Forms.Label();
            this.m_oMaterialsLabel = new System.Windows.Forms.Label();
            this.m_oSepRangeLabel = new System.Windows.Forms.Label();
            this.m_oMissileCostLabel = new System.Windows.Forms.Label();
            this.m_oTotalMCostLabel = new System.Windows.Forms.Label();
            this.m_oSubTotalSizeTextBox = new System.Windows.Forms.TextBox();
            this.m_oSubNumberTextBox = new System.Windows.Forms.TextBox();
            this.m_oSubSizeTextBox = new System.Windows.Forms.TextBox();
            this.m_oSubTotalCostTextBox = new System.Windows.Forms.TextBox();
            this.m_oSepRangeTextBox = new System.Windows.Forms.TextBox();
            this.m_oSubCostTextBox = new System.Windows.Forms.TextBox();
            this.m_oMaterialsRichTextBox = new System.Windows.Forms.RichTextBox();
            this.m_oEmpireGroupBox.SuspendLayout();
            this.m_oCompSizeBox.SuspendLayout();
            this.m_oParametersGroupBox.SuspendLayout();
            this.m_oSensorGroupBox.SuspendLayout();
            this.m_oDefenceGroupBox.SuspendLayout();
            this.m_oModGroupBox.SuspendLayout();
            this.m_oMissileSeriesGroupBox.SuspendLayout();
            this.m_oPreviousDesignGroupBox.SuspendLayout();
            this.m_oMissileEngineGroupBox.SuspendLayout();
            this.m_oSecondStageGroupBox.SuspendLayout();
            this.m_oSystemParametersGroupBox.SuspendLayout();
            this.m_oInfoGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_oEmpireGroupBox
            // 
            this.m_oEmpireGroupBox.Controls.Add(this.m_oEmpireComboBox);
            this.m_oEmpireGroupBox.Location = new System.Drawing.Point(12, 12);
            this.m_oEmpireGroupBox.Name = "m_oEmpireGroupBox";
            this.m_oEmpireGroupBox.Size = new System.Drawing.Size(277, 50);
            this.m_oEmpireGroupBox.TabIndex = 0;
            this.m_oEmpireGroupBox.TabStop = false;
            this.m_oEmpireGroupBox.Text = "Empire";
            // 
            // m_oEmpireComboBox
            // 
            this.m_oEmpireComboBox.FormattingEnabled = true;
            this.m_oEmpireComboBox.Location = new System.Drawing.Point(13, 18);
            this.m_oEmpireComboBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oEmpireComboBox.Name = "m_oEmpireComboBox";
            this.m_oEmpireComboBox.Size = new System.Drawing.Size(251, 21);
            this.m_oEmpireComboBox.TabIndex = 0;
            // 
            // m_oCompSizeBox
            // 
            this.m_oCompSizeBox.Controls.Add(this.m_oValueLabel);
            this.m_oCompSizeBox.Controls.Add(this.m_oMSPLabel);
            this.m_oCompSizeBox.Controls.Add(this.m_oParametersGroupBox);
            this.m_oCompSizeBox.Controls.Add(this.m_oSensorGroupBox);
            this.m_oCompSizeBox.Controls.Add(this.m_oDefenceGroupBox);
            this.m_oCompSizeBox.Controls.Add(this.m_oModGroupBox);
            this.m_oCompSizeBox.Location = new System.Drawing.Point(12, 68);
            this.m_oCompSizeBox.Name = "m_oCompSizeBox";
            this.m_oCompSizeBox.Size = new System.Drawing.Size(277, 498);
            this.m_oCompSizeBox.TabIndex = 1;
            this.m_oCompSizeBox.TabStop = false;
            this.m_oCompSizeBox.Text = "Enter Component Sizes";
            // 
            // m_oValueLabel
            // 
            this.m_oValueLabel.AutoSize = true;
            this.m_oValueLabel.Location = new System.Drawing.Point(218, 27);
            this.m_oValueLabel.Margin = new System.Windows.Forms.Padding(3);
            this.m_oValueLabel.Name = "m_oValueLabel";
            this.m_oValueLabel.Size = new System.Drawing.Size(34, 13);
            this.m_oValueLabel.TabIndex = 23;
            this.m_oValueLabel.Text = "Value";
            // 
            // m_oMSPLabel
            // 
            this.m_oMSPLabel.AutoSize = true;
            this.m_oMSPLabel.Location = new System.Drawing.Point(162, 27);
            this.m_oMSPLabel.Margin = new System.Windows.Forms.Padding(3);
            this.m_oMSPLabel.Name = "m_oMSPLabel";
            this.m_oMSPLabel.Size = new System.Drawing.Size(30, 13);
            this.m_oMSPLabel.TabIndex = 22;
            this.m_oMSPLabel.Text = "MSP";
            // 
            // m_oParametersGroupBox
            // 
            this.m_oParametersGroupBox.Controls.Add(this.m_oReactorLabel);
            this.m_oParametersGroupBox.Controls.Add(this.m_oAgilityLabel);
            this.m_oParametersGroupBox.Controls.Add(this.m_oFuelLabel);
            this.m_oParametersGroupBox.Controls.Add(this.m_oWarheadLabel);
            this.m_oParametersGroupBox.Controls.Add(this.m_oReactorMSPTextBox);
            this.m_oParametersGroupBox.Controls.Add(this.m_oAgilityMSPTextBox);
            this.m_oParametersGroupBox.Controls.Add(this.m_oFuelMSPTextBox);
            this.m_oParametersGroupBox.Controls.Add(this.m_oWHMSPTextBox);
            this.m_oParametersGroupBox.Controls.Add(this.m_oReactorValueTextBox);
            this.m_oParametersGroupBox.Controls.Add(this.m_oAgilityValueTextBox);
            this.m_oParametersGroupBox.Controls.Add(this.m_oFuelValueTextBox);
            this.m_oParametersGroupBox.Controls.Add(this.m_oWHValueTextBox);
            this.m_oParametersGroupBox.Location = new System.Drawing.Point(8, 48);
            this.m_oParametersGroupBox.Margin = new System.Windows.Forms.Padding(5);
            this.m_oParametersGroupBox.Name = "m_oParametersGroupBox";
            this.m_oParametersGroupBox.Size = new System.Drawing.Size(261, 127);
            this.m_oParametersGroupBox.TabIndex = 2;
            this.m_oParametersGroupBox.TabStop = false;
            this.m_oParametersGroupBox.Text = "Basic Missile Parameters";
            // 
            // m_oReactorLabel
            // 
            this.m_oReactorLabel.AutoSize = true;
            this.m_oReactorLabel.Location = new System.Drawing.Point(13, 100);
            this.m_oReactorLabel.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oReactorLabel.Name = "m_oReactorLabel";
            this.m_oReactorLabel.Size = new System.Drawing.Size(45, 13);
            this.m_oReactorLabel.TabIndex = 21;
            this.m_oReactorLabel.Text = "Reactor";
            // 
            // m_oAgilityLabel
            // 
            this.m_oAgilityLabel.AutoSize = true;
            this.m_oAgilityLabel.Location = new System.Drawing.Point(13, 74);
            this.m_oAgilityLabel.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oAgilityLabel.Name = "m_oAgilityLabel";
            this.m_oAgilityLabel.Size = new System.Drawing.Size(34, 13);
            this.m_oAgilityLabel.TabIndex = 20;
            this.m_oAgilityLabel.Text = "Agility";
            // 
            // m_oFuelLabel
            // 
            this.m_oFuelLabel.AutoSize = true;
            this.m_oFuelLabel.Location = new System.Drawing.Point(13, 48);
            this.m_oFuelLabel.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oFuelLabel.Name = "m_oFuelLabel";
            this.m_oFuelLabel.Size = new System.Drawing.Size(71, 13);
            this.m_oFuelLabel.TabIndex = 19;
            this.m_oFuelLabel.Text = "Fuel Capacity";
            // 
            // m_oWarheadLabel
            // 
            this.m_oWarheadLabel.AutoSize = true;
            this.m_oWarheadLabel.Location = new System.Drawing.Point(13, 22);
            this.m_oWarheadLabel.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oWarheadLabel.Name = "m_oWarheadLabel";
            this.m_oWarheadLabel.Size = new System.Drawing.Size(94, 13);
            this.m_oWarheadLabel.TabIndex = 11;
            this.m_oWarheadLabel.Text = "Warhead Strength";
            // 
            // m_oReactorMSPTextBox
            // 
            this.m_oReactorMSPTextBox.Location = new System.Drawing.Point(149, 98);
            this.m_oReactorMSPTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oReactorMSPTextBox.Name = "m_oReactorMSPTextBox";
            this.m_oReactorMSPTextBox.ReadOnly = true;
            this.m_oReactorMSPTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oReactorMSPTextBox.TabIndex = 18;
            this.m_oReactorMSPTextBox.Text = "0";
            this.m_oReactorMSPTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oAgilityMSPTextBox
            // 
            this.m_oAgilityMSPTextBox.Location = new System.Drawing.Point(149, 72);
            this.m_oAgilityMSPTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oAgilityMSPTextBox.Name = "m_oAgilityMSPTextBox";
            this.m_oAgilityMSPTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oAgilityMSPTextBox.TabIndex = 17;
            this.m_oAgilityMSPTextBox.Text = "0";
            this.m_oAgilityMSPTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oFuelMSPTextBox
            // 
            this.m_oFuelMSPTextBox.Location = new System.Drawing.Point(149, 46);
            this.m_oFuelMSPTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oFuelMSPTextBox.Name = "m_oFuelMSPTextBox";
            this.m_oFuelMSPTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oFuelMSPTextBox.TabIndex = 16;
            this.m_oFuelMSPTextBox.Text = "0";
            this.m_oFuelMSPTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oWHMSPTextBox
            // 
            this.m_oWHMSPTextBox.Location = new System.Drawing.Point(149, 20);
            this.m_oWHMSPTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oWHMSPTextBox.Name = "m_oWHMSPTextBox";
            this.m_oWHMSPTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oWHMSPTextBox.TabIndex = 15;
            this.m_oWHMSPTextBox.Text = "0";
            this.m_oWHMSPTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oReactorValueTextBox
            // 
            this.m_oReactorValueTextBox.Location = new System.Drawing.Point(204, 97);
            this.m_oReactorValueTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oReactorValueTextBox.Name = "m_oReactorValueTextBox";
            this.m_oReactorValueTextBox.ReadOnly = true;
            this.m_oReactorValueTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oReactorValueTextBox.TabIndex = 14;
            this.m_oReactorValueTextBox.Text = "0";
            this.m_oReactorValueTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oAgilityValueTextBox
            // 
            this.m_oAgilityValueTextBox.Location = new System.Drawing.Point(204, 71);
            this.m_oAgilityValueTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oAgilityValueTextBox.Name = "m_oAgilityValueTextBox";
            this.m_oAgilityValueTextBox.ReadOnly = true;
            this.m_oAgilityValueTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oAgilityValueTextBox.TabIndex = 13;
            this.m_oAgilityValueTextBox.Text = "0";
            this.m_oAgilityValueTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oFuelValueTextBox
            // 
            this.m_oFuelValueTextBox.Location = new System.Drawing.Point(204, 45);
            this.m_oFuelValueTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oFuelValueTextBox.Name = "m_oFuelValueTextBox";
            this.m_oFuelValueTextBox.ReadOnly = true;
            this.m_oFuelValueTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oFuelValueTextBox.TabIndex = 12;
            this.m_oFuelValueTextBox.Text = "0";
            this.m_oFuelValueTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oWHValueTextBox
            // 
            this.m_oWHValueTextBox.Location = new System.Drawing.Point(204, 19);
            this.m_oWHValueTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oWHValueTextBox.Name = "m_oWHValueTextBox";
            this.m_oWHValueTextBox.ReadOnly = true;
            this.m_oWHValueTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oWHValueTextBox.TabIndex = 11;
            this.m_oWHValueTextBox.Text = "0";
            this.m_oWHValueTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oSensorGroupBox
            // 
            this.m_oSensorGroupBox.Controls.Add(this.m_oActiveResLabel);
            this.m_oSensorGroupBox.Controls.Add(this.m_oResolutionTextBox);
            this.m_oSensorGroupBox.Controls.Add(this.m_oGeoLabel);
            this.m_oSensorGroupBox.Controls.Add(this.m_oActiveLabel);
            this.m_oSensorGroupBox.Controls.Add(this.m_oEMLabel);
            this.m_oSensorGroupBox.Controls.Add(this.m_oActiveValueTextBox);
            this.m_oSensorGroupBox.Controls.Add(this.m_oThermalLabel);
            this.m_oSensorGroupBox.Controls.Add(this.m_oThermalValueTextBox);
            this.m_oSensorGroupBox.Controls.Add(this.m_oEMValueTextBox);
            this.m_oSensorGroupBox.Controls.Add(this.m_oGeoMSPTextBox);
            this.m_oSensorGroupBox.Controls.Add(this.m_oGeoValueTextBox);
            this.m_oSensorGroupBox.Controls.Add(this.m_oEMMSPTextBox);
            this.m_oSensorGroupBox.Controls.Add(this.m_oActiveMSPTextBox);
            this.m_oSensorGroupBox.Controls.Add(this.m_oThermalMSPTextBox);
            this.m_oSensorGroupBox.Location = new System.Drawing.Point(8, 185);
            this.m_oSensorGroupBox.Margin = new System.Windows.Forms.Padding(5);
            this.m_oSensorGroupBox.Name = "m_oSensorGroupBox";
            this.m_oSensorGroupBox.Size = new System.Drawing.Size(261, 153);
            this.m_oSensorGroupBox.TabIndex = 2;
            this.m_oSensorGroupBox.TabStop = false;
            this.m_oSensorGroupBox.Text = "Sensors";
            // 
            // m_oActiveResLabel
            // 
            this.m_oActiveResLabel.AutoSize = true;
            this.m_oActiveResLabel.Location = new System.Drawing.Point(13, 125);
            this.m_oActiveResLabel.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oActiveResLabel.Name = "m_oActiveResLabel";
            this.m_oActiveResLabel.Size = new System.Drawing.Size(126, 13);
            this.m_oActiveResLabel.TabIndex = 36;
            this.m_oActiveResLabel.Text = "Active Sensor Resolution";
            // 
            // m_oResolutionTextBox
            // 
            this.m_oResolutionTextBox.Location = new System.Drawing.Point(204, 122);
            this.m_oResolutionTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oResolutionTextBox.Name = "m_oResolutionTextBox";
            this.m_oResolutionTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oResolutionTextBox.TabIndex = 34;
            this.m_oResolutionTextBox.Text = "1";
            this.m_oResolutionTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oGeoLabel
            // 
            this.m_oGeoLabel.AutoSize = true;
            this.m_oGeoLabel.Location = new System.Drawing.Point(13, 99);
            this.m_oGeoLabel.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oGeoLabel.Name = "m_oGeoLabel";
            this.m_oGeoLabel.Size = new System.Drawing.Size(63, 13);
            this.m_oGeoLabel.TabIndex = 33;
            this.m_oGeoLabel.Text = "Geo Sensor";
            // 
            // m_oActiveLabel
            // 
            this.m_oActiveLabel.AutoSize = true;
            this.m_oActiveLabel.Location = new System.Drawing.Point(13, 19);
            this.m_oActiveLabel.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oActiveLabel.Name = "m_oActiveLabel";
            this.m_oActiveLabel.Size = new System.Drawing.Size(73, 13);
            this.m_oActiveLabel.TabIndex = 22;
            this.m_oActiveLabel.Text = "Active Sensor";
            // 
            // m_oEMLabel
            // 
            this.m_oEMLabel.AutoSize = true;
            this.m_oEMLabel.Location = new System.Drawing.Point(13, 71);
            this.m_oEMLabel.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oEMLabel.Name = "m_oEMLabel";
            this.m_oEMLabel.Size = new System.Drawing.Size(59, 13);
            this.m_oEMLabel.TabIndex = 32;
            this.m_oEMLabel.Text = "EM Sensor";
            // 
            // m_oActiveValueTextBox
            // 
            this.m_oActiveValueTextBox.Location = new System.Drawing.Point(204, 18);
            this.m_oActiveValueTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oActiveValueTextBox.Name = "m_oActiveValueTextBox";
            this.m_oActiveValueTextBox.ReadOnly = true;
            this.m_oActiveValueTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oActiveValueTextBox.TabIndex = 23;
            this.m_oActiveValueTextBox.Text = "0";
            this.m_oActiveValueTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oThermalLabel
            // 
            this.m_oThermalLabel.AutoSize = true;
            this.m_oThermalLabel.Location = new System.Drawing.Point(13, 45);
            this.m_oThermalLabel.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oThermalLabel.Name = "m_oThermalLabel";
            this.m_oThermalLabel.Size = new System.Drawing.Size(81, 13);
            this.m_oThermalLabel.TabIndex = 31;
            this.m_oThermalLabel.Text = "Thermal Sensor";
            // 
            // m_oThermalValueTextBox
            // 
            this.m_oThermalValueTextBox.Location = new System.Drawing.Point(204, 44);
            this.m_oThermalValueTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oThermalValueTextBox.Name = "m_oThermalValueTextBox";
            this.m_oThermalValueTextBox.ReadOnly = true;
            this.m_oThermalValueTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oThermalValueTextBox.TabIndex = 24;
            this.m_oThermalValueTextBox.Text = "0";
            this.m_oThermalValueTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oEMValueTextBox
            // 
            this.m_oEMValueTextBox.Location = new System.Drawing.Point(204, 70);
            this.m_oEMValueTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oEMValueTextBox.Name = "m_oEMValueTextBox";
            this.m_oEMValueTextBox.ReadOnly = true;
            this.m_oEMValueTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oEMValueTextBox.TabIndex = 25;
            this.m_oEMValueTextBox.Text = "0";
            this.m_oEMValueTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oGeoMSPTextBox
            // 
            this.m_oGeoMSPTextBox.Location = new System.Drawing.Point(149, 97);
            this.m_oGeoMSPTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oGeoMSPTextBox.Name = "m_oGeoMSPTextBox";
            this.m_oGeoMSPTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oGeoMSPTextBox.TabIndex = 30;
            this.m_oGeoMSPTextBox.Text = "0";
            this.m_oGeoMSPTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oGeoValueTextBox
            // 
            this.m_oGeoValueTextBox.Location = new System.Drawing.Point(204, 96);
            this.m_oGeoValueTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oGeoValueTextBox.Name = "m_oGeoValueTextBox";
            this.m_oGeoValueTextBox.ReadOnly = true;
            this.m_oGeoValueTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oGeoValueTextBox.TabIndex = 26;
            this.m_oGeoValueTextBox.Text = "0";
            this.m_oGeoValueTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oEMMSPTextBox
            // 
            this.m_oEMMSPTextBox.Location = new System.Drawing.Point(149, 71);
            this.m_oEMMSPTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oEMMSPTextBox.Name = "m_oEMMSPTextBox";
            this.m_oEMMSPTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oEMMSPTextBox.TabIndex = 29;
            this.m_oEMMSPTextBox.Text = "0";
            this.m_oEMMSPTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oActiveMSPTextBox
            // 
            this.m_oActiveMSPTextBox.Location = new System.Drawing.Point(149, 19);
            this.m_oActiveMSPTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oActiveMSPTextBox.Name = "m_oActiveMSPTextBox";
            this.m_oActiveMSPTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oActiveMSPTextBox.TabIndex = 27;
            this.m_oActiveMSPTextBox.Text = "0";
            this.m_oActiveMSPTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oThermalMSPTextBox
            // 
            this.m_oThermalMSPTextBox.Location = new System.Drawing.Point(149, 45);
            this.m_oThermalMSPTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oThermalMSPTextBox.Name = "m_oThermalMSPTextBox";
            this.m_oThermalMSPTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oThermalMSPTextBox.TabIndex = 28;
            this.m_oThermalMSPTextBox.Text = "0";
            this.m_oThermalMSPTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oDefenceGroupBox
            // 
            this.m_oDefenceGroupBox.Controls.Add(this.m_oArmourLabel);
            this.m_oDefenceGroupBox.Controls.Add(this.m_oArmourValueTextBox);
            this.m_oDefenceGroupBox.Controls.Add(this.m_oECMMSPTextBox);
            this.m_oDefenceGroupBox.Controls.Add(this.m_oECMLabel);
            this.m_oDefenceGroupBox.Controls.Add(this.m_oArmourMSPTextBox);
            this.m_oDefenceGroupBox.Controls.Add(this.m_oECMValueTextBox);
            this.m_oDefenceGroupBox.Location = new System.Drawing.Point(8, 348);
            this.m_oDefenceGroupBox.Margin = new System.Windows.Forms.Padding(5);
            this.m_oDefenceGroupBox.Name = "m_oDefenceGroupBox";
            this.m_oDefenceGroupBox.Size = new System.Drawing.Size(261, 82);
            this.m_oDefenceGroupBox.TabIndex = 2;
            this.m_oDefenceGroupBox.TabStop = false;
            this.m_oDefenceGroupBox.Text = "Defences";
            // 
            // m_oArmourLabel
            // 
            this.m_oArmourLabel.AutoSize = true;
            this.m_oArmourLabel.Location = new System.Drawing.Point(13, 22);
            this.m_oArmourLabel.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oArmourLabel.Name = "m_oArmourLabel";
            this.m_oArmourLabel.Size = new System.Drawing.Size(81, 13);
            this.m_oArmourLabel.TabIndex = 37;
            this.m_oArmourLabel.Text = "Ablative Armour";
            // 
            // m_oArmourValueTextBox
            // 
            this.m_oArmourValueTextBox.Location = new System.Drawing.Point(204, 19);
            this.m_oArmourValueTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oArmourValueTextBox.Name = "m_oArmourValueTextBox";
            this.m_oArmourValueTextBox.ReadOnly = true;
            this.m_oArmourValueTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oArmourValueTextBox.TabIndex = 38;
            this.m_oArmourValueTextBox.Text = "0";
            this.m_oArmourValueTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oECMMSPTextBox
            // 
            this.m_oECMMSPTextBox.Location = new System.Drawing.Point(149, 45);
            this.m_oECMMSPTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oECMMSPTextBox.Name = "m_oECMMSPTextBox";
            this.m_oECMMSPTextBox.ReadOnly = true;
            this.m_oECMMSPTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oECMMSPTextBox.TabIndex = 41;
            this.m_oECMMSPTextBox.Text = "0";
            this.m_oECMMSPTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oECMLabel
            // 
            this.m_oECMLabel.AutoSize = true;
            this.m_oECMLabel.Location = new System.Drawing.Point(13, 48);
            this.m_oECMLabel.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oECMLabel.Name = "m_oECMLabel";
            this.m_oECMLabel.Size = new System.Drawing.Size(30, 13);
            this.m_oECMLabel.TabIndex = 42;
            this.m_oECMLabel.Text = "ECM";
            // 
            // m_oArmourMSPTextBox
            // 
            this.m_oArmourMSPTextBox.Location = new System.Drawing.Point(149, 19);
            this.m_oArmourMSPTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oArmourMSPTextBox.Name = "m_oArmourMSPTextBox";
            this.m_oArmourMSPTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oArmourMSPTextBox.TabIndex = 40;
            this.m_oArmourMSPTextBox.Text = "0";
            this.m_oArmourMSPTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oECMValueTextBox
            // 
            this.m_oECMValueTextBox.Location = new System.Drawing.Point(204, 45);
            this.m_oECMValueTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oECMValueTextBox.Name = "m_oECMValueTextBox";
            this.m_oECMValueTextBox.ReadOnly = true;
            this.m_oECMValueTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oECMValueTextBox.TabIndex = 39;
            this.m_oECMValueTextBox.Text = "0";
            this.m_oECMValueTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oModGroupBox
            // 
            this.m_oModGroupBox.Controls.Add(this.m_oERCheckBox);
            this.m_oModGroupBox.Controls.Add(this.m_oLaserWCheckBox);
            this.m_oModGroupBox.Location = new System.Drawing.Point(8, 440);
            this.m_oModGroupBox.Margin = new System.Windows.Forms.Padding(5);
            this.m_oModGroupBox.Name = "m_oModGroupBox";
            this.m_oModGroupBox.Size = new System.Drawing.Size(261, 50);
            this.m_oModGroupBox.TabIndex = 2;
            this.m_oModGroupBox.TabStop = false;
            this.m_oModGroupBox.Text = "Special Modifiers";
            // 
            // m_oERCheckBox
            // 
            this.m_oERCheckBox.AutoSize = true;
            this.m_oERCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oERCheckBox.Enabled = false;
            this.m_oERCheckBox.Location = new System.Drawing.Point(125, 19);
            this.m_oERCheckBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oERCheckBox.Name = "m_oERCheckBox";
            this.m_oERCheckBox.Size = new System.Drawing.Size(123, 17);
            this.m_oERCheckBox.TabIndex = 1;
            this.m_oERCheckBox.Text = "Enhanced Radiation";
            this.m_oERCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oLaserWCheckBox
            // 
            this.m_oLaserWCheckBox.AutoSize = true;
            this.m_oLaserWCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oLaserWCheckBox.Enabled = false;
            this.m_oLaserWCheckBox.Location = new System.Drawing.Point(13, 19);
            this.m_oLaserWCheckBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oLaserWCheckBox.Name = "m_oLaserWCheckBox";
            this.m_oLaserWCheckBox.Size = new System.Drawing.Size(99, 17);
            this.m_oLaserWCheckBox.TabIndex = 0;
            this.m_oLaserWCheckBox.Text = "Laser Warhead";
            this.m_oLaserWCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oMissileSeriesGroupBox
            // 
            this.m_oMissileSeriesGroupBox.Controls.Add(this.m_oMSeriesListBox);
            this.m_oMissileSeriesGroupBox.Controls.Add(this.m_oMSeriesComboBox);
            this.m_oMissileSeriesGroupBox.Location = new System.Drawing.Point(12, 572);
            this.m_oMissileSeriesGroupBox.Name = "m_oMissileSeriesGroupBox";
            this.m_oMissileSeriesGroupBox.Size = new System.Drawing.Size(277, 140);
            this.m_oMissileSeriesGroupBox.TabIndex = 2;
            this.m_oMissileSeriesGroupBox.TabStop = false;
            this.m_oMissileSeriesGroupBox.Text = "Missile Series";
            // 
            // m_oMSeriesListBox
            // 
            this.m_oMSeriesListBox.FormattingEnabled = true;
            this.m_oMSeriesListBox.Location = new System.Drawing.Point(13, 46);
            this.m_oMSeriesListBox.Name = "m_oMSeriesListBox";
            this.m_oMSeriesListBox.Size = new System.Drawing.Size(251, 82);
            this.m_oMSeriesListBox.TabIndex = 12;
            // 
            // m_oMSeriesComboBox
            // 
            this.m_oMSeriesComboBox.FormattingEnabled = true;
            this.m_oMSeriesComboBox.Location = new System.Drawing.Point(13, 19);
            this.m_oMSeriesComboBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oMSeriesComboBox.Name = "m_oMSeriesComboBox";
            this.m_oMSeriesComboBox.Size = new System.Drawing.Size(251, 21);
            this.m_oMSeriesComboBox.TabIndex = 11;
            // 
            // m_oPreviousDesignGroupBox
            // 
            this.m_oPreviousDesignGroupBox.Controls.Add(this.m_oPreviousOrdnanceComboBox);
            this.m_oPreviousDesignGroupBox.Location = new System.Drawing.Point(295, 12);
            this.m_oPreviousDesignGroupBox.Name = "m_oPreviousDesignGroupBox";
            this.m_oPreviousDesignGroupBox.Size = new System.Drawing.Size(319, 50);
            this.m_oPreviousDesignGroupBox.TabIndex = 2;
            this.m_oPreviousDesignGroupBox.TabStop = false;
            this.m_oPreviousDesignGroupBox.Text = "MSP Allocation of Previous Designs";
            // 
            // m_oPreviousOrdnanceComboBox
            // 
            this.m_oPreviousOrdnanceComboBox.FormattingEnabled = true;
            this.m_oPreviousOrdnanceComboBox.Location = new System.Drawing.Point(13, 18);
            this.m_oPreviousOrdnanceComboBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oPreviousOrdnanceComboBox.Name = "m_oPreviousOrdnanceComboBox";
            this.m_oPreviousOrdnanceComboBox.Size = new System.Drawing.Size(293, 21);
            this.m_oPreviousOrdnanceComboBox.TabIndex = 11;
            // 
            // m_oMissileEngineGroupBox
            // 
            this.m_oMissileEngineGroupBox.Controls.Add(this.m_oTotalEngineSizeTextBox);
            this.m_oMissileEngineGroupBox.Controls.Add(this.m_oNumberEnginesTextBox);
            this.m_oMissileEngineGroupBox.Controls.Add(this.m_oTotalEngineCostTextBox);
            this.m_oMissileEngineGroupBox.Controls.Add(this.m_oTotalEPTextBox);
            this.m_oMissileEngineGroupBox.Controls.Add(this.m_oTotalCostLabel);
            this.m_oMissileEngineGroupBox.Controls.Add(this.m_oTotalEPLabel);
            this.m_oMissileEngineGroupBox.Controls.Add(this.m_oESizeLabel);
            this.m_oMissileEngineGroupBox.Controls.Add(this.m_oNumberLabel);
            this.m_oMissileEngineGroupBox.Controls.Add(this.m_oEngineLabel);
            this.m_oMissileEngineGroupBox.Controls.Add(this.m_oMissileEngineComboBox);
            this.m_oMissileEngineGroupBox.Location = new System.Drawing.Point(295, 68);
            this.m_oMissileEngineGroupBox.Name = "m_oMissileEngineGroupBox";
            this.m_oMissileEngineGroupBox.Size = new System.Drawing.Size(319, 109);
            this.m_oMissileEngineGroupBox.TabIndex = 2;
            this.m_oMissileEngineGroupBox.TabStop = false;
            this.m_oMissileEngineGroupBox.Text = "Missile Engines";
            // 
            // m_oTotalEngineSizeTextBox
            // 
            this.m_oTotalEngineSizeTextBox.Location = new System.Drawing.Point(87, 79);
            this.m_oTotalEngineSizeTextBox.Name = "m_oTotalEngineSizeTextBox";
            this.m_oTotalEngineSizeTextBox.ReadOnly = true;
            this.m_oTotalEngineSizeTextBox.Size = new System.Drawing.Size(54, 20);
            this.m_oTotalEngineSizeTextBox.TabIndex = 10;
            this.m_oTotalEngineSizeTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oNumberEnginesTextBox
            // 
            this.m_oNumberEnginesTextBox.Location = new System.Drawing.Point(87, 53);
            this.m_oNumberEnginesTextBox.Name = "m_oNumberEnginesTextBox";
            this.m_oNumberEnginesTextBox.Size = new System.Drawing.Size(54, 20);
            this.m_oNumberEnginesTextBox.TabIndex = 9;
            this.m_oNumberEnginesTextBox.Text = "1";
            this.m_oNumberEnginesTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oTotalEngineCostTextBox
            // 
            this.m_oTotalEngineCostTextBox.Location = new System.Drawing.Point(262, 79);
            this.m_oTotalEngineCostTextBox.Name = "m_oTotalEngineCostTextBox";
            this.m_oTotalEngineCostTextBox.ReadOnly = true;
            this.m_oTotalEngineCostTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oTotalEngineCostTextBox.TabIndex = 8;
            this.m_oTotalEngineCostTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oTotalEPTextBox
            // 
            this.m_oTotalEPTextBox.Location = new System.Drawing.Point(262, 53);
            this.m_oTotalEPTextBox.Name = "m_oTotalEPTextBox";
            this.m_oTotalEPTextBox.ReadOnly = true;
            this.m_oTotalEPTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oTotalEPTextBox.TabIndex = 7;
            this.m_oTotalEPTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oTotalCostLabel
            // 
            this.m_oTotalCostLabel.AutoSize = true;
            this.m_oTotalCostLabel.Location = new System.Drawing.Point(156, 82);
            this.m_oTotalCostLabel.Name = "m_oTotalCostLabel";
            this.m_oTotalCostLabel.Size = new System.Drawing.Size(55, 13);
            this.m_oTotalCostLabel.TabIndex = 6;
            this.m_oTotalCostLabel.Text = "Total Cost";
            // 
            // m_oTotalEPLabel
            // 
            this.m_oTotalEPLabel.AutoSize = true;
            this.m_oTotalEPLabel.Location = new System.Drawing.Point(156, 56);
            this.m_oTotalEPLabel.Name = "m_oTotalEPLabel";
            this.m_oTotalEPLabel.Size = new System.Drawing.Size(100, 13);
            this.m_oTotalEPLabel.TabIndex = 5;
            this.m_oTotalEPLabel.Text = "Total Engine Power";
            // 
            // m_oESizeLabel
            // 
            this.m_oESizeLabel.AutoSize = true;
            this.m_oESizeLabel.Location = new System.Drawing.Point(7, 83);
            this.m_oESizeLabel.Name = "m_oESizeLabel";
            this.m_oESizeLabel.Size = new System.Drawing.Size(54, 13);
            this.m_oESizeLabel.TabIndex = 4;
            this.m_oESizeLabel.Text = "Total Size";
            // 
            // m_oNumberLabel
            // 
            this.m_oNumberLabel.AutoSize = true;
            this.m_oNumberLabel.Location = new System.Drawing.Point(7, 56);
            this.m_oNumberLabel.Name = "m_oNumberLabel";
            this.m_oNumberLabel.Size = new System.Drawing.Size(44, 13);
            this.m_oNumberLabel.TabIndex = 3;
            this.m_oNumberLabel.Text = "Number";
            // 
            // m_oEngineLabel
            // 
            this.m_oEngineLabel.AutoSize = true;
            this.m_oEngineLabel.Location = new System.Drawing.Point(7, 26);
            this.m_oEngineLabel.Name = "m_oEngineLabel";
            this.m_oEngineLabel.Size = new System.Drawing.Size(67, 13);
            this.m_oEngineLabel.TabIndex = 2;
            this.m_oEngineLabel.Text = "Engine Type";
            // 
            // m_oMissileEngineComboBox
            // 
            this.m_oMissileEngineComboBox.FormattingEnabled = true;
            this.m_oMissileEngineComboBox.Location = new System.Drawing.Point(87, 19);
            this.m_oMissileEngineComboBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oMissileEngineComboBox.Name = "m_oMissileEngineComboBox";
            this.m_oMissileEngineComboBox.Size = new System.Drawing.Size(219, 21);
            this.m_oMissileEngineComboBox.TabIndex = 1;
            // 
            // m_oSecondStageGroupBox
            // 
            this.m_oSecondStageGroupBox.Controls.Add(this.m_oMaterialsRichTextBox);
            this.m_oSecondStageGroupBox.Controls.Add(this.m_oSubTotalCostTextBox);
            this.m_oSecondStageGroupBox.Controls.Add(this.m_oSepRangeTextBox);
            this.m_oSecondStageGroupBox.Controls.Add(this.m_oSubCostTextBox);
            this.m_oSecondStageGroupBox.Controls.Add(this.m_oSubTotalSizeTextBox);
            this.m_oSecondStageGroupBox.Controls.Add(this.m_oTotalMCostLabel);
            this.m_oSecondStageGroupBox.Controls.Add(this.m_oSubNumberTextBox);
            this.m_oSecondStageGroupBox.Controls.Add(this.m_oSubSizeTextBox);
            this.m_oSecondStageGroupBox.Controls.Add(this.m_oMissileCostLabel);
            this.m_oSecondStageGroupBox.Controls.Add(this.m_oSepRangeLabel);
            this.m_oSecondStageGroupBox.Controls.Add(this.m_oMaterialsLabel);
            this.m_oSecondStageGroupBox.Controls.Add(this.m_oTotalMSizeLabel);
            this.m_oSecondStageGroupBox.Controls.Add(this.m_oSubSizeLabel);
            this.m_oSecondStageGroupBox.Controls.Add(this.m_oSubNumberLabel);
            this.m_oSecondStageGroupBox.Controls.Add(this.m_oMissileTypeLabel);
            this.m_oSecondStageGroupBox.Controls.Add(this.m_oSubMunitionComboBox);
            this.m_oSecondStageGroupBox.Location = new System.Drawing.Point(295, 183);
            this.m_oSecondStageGroupBox.Name = "m_oSecondStageGroupBox";
            this.m_oSecondStageGroupBox.Size = new System.Drawing.Size(319, 209);
            this.m_oSecondStageGroupBox.TabIndex = 2;
            this.m_oSecondStageGroupBox.TabStop = false;
            this.m_oSecondStageGroupBox.Text = "Second Stage(if desired)";
            // 
            // m_oSystemParametersGroupBox
            // 
            this.m_oSystemParametersGroupBox.Controls.Add(this.m_oCreateButton);
            this.m_oSystemParametersGroupBox.Controls.Add(this.m_oMissileNameTextBox);
            this.m_oSystemParametersGroupBox.Controls.Add(this.m_oMissileSummaryTextBox);
            this.m_oSystemParametersGroupBox.Location = new System.Drawing.Point(295, 398);
            this.m_oSystemParametersGroupBox.Name = "m_oSystemParametersGroupBox";
            this.m_oSystemParametersGroupBox.Size = new System.Drawing.Size(668, 314);
            this.m_oSystemParametersGroupBox.TabIndex = 2;
            this.m_oSystemParametersGroupBox.TabStop = false;
            this.m_oSystemParametersGroupBox.Text = "Proposed System Parameters";
            // 
            // m_oCreateButton
            // 
            this.m_oCreateButton.Location = new System.Drawing.Point(573, 26);
            this.m_oCreateButton.Margin = new System.Windows.Forms.Padding(10);
            this.m_oCreateButton.Name = "m_oCreateButton";
            this.m_oCreateButton.Size = new System.Drawing.Size(82, 23);
            this.m_oCreateButton.TabIndex = 2;
            this.m_oCreateButton.Text = "Create";
            this.m_oCreateButton.UseVisualStyleBackColor = true;
            // 
            // m_oMissileNameTextBox
            // 
            this.m_oMissileNameTextBox.Location = new System.Drawing.Point(13, 28);
            this.m_oMissileNameTextBox.Margin = new System.Windows.Forms.Padding(10);
            this.m_oMissileNameTextBox.Name = "m_oMissileNameTextBox";
            this.m_oMissileNameTextBox.Size = new System.Drawing.Size(540, 20);
            this.m_oMissileNameTextBox.TabIndex = 1;
            // 
            // m_oMissileSummaryTextBox
            // 
            this.m_oMissileSummaryTextBox.Location = new System.Drawing.Point(13, 57);
            this.m_oMissileSummaryTextBox.Margin = new System.Windows.Forms.Padding(10);
            this.m_oMissileSummaryTextBox.Name = "m_oMissileSummaryTextBox";
            this.m_oMissileSummaryTextBox.Size = new System.Drawing.Size(642, 244);
            this.m_oMissileSummaryTextBox.TabIndex = 0;
            this.m_oMissileSummaryTextBox.Text = "";
            // 
            // m_oInfoGroupBox
            // 
            this.m_oInfoGroupBox.Controls.Add(this.m_oInfoLabel);
            this.m_oInfoGroupBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oInfoGroupBox.Location = new System.Drawing.Point(620, 68);
            this.m_oInfoGroupBox.Name = "m_oInfoGroupBox";
            this.m_oInfoGroupBox.Size = new System.Drawing.Size(343, 324);
            this.m_oInfoGroupBox.TabIndex = 2;
            this.m_oInfoGroupBox.TabStop = false;
            this.m_oInfoGroupBox.Text = "Current Missile Technology";
            // 
            // m_oInfoLabel
            // 
            this.m_oInfoLabel.AutoSize = true;
            this.m_oInfoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oInfoLabel.Location = new System.Drawing.Point(13, 26);
            this.m_oInfoLabel.Margin = new System.Windows.Forms.Padding(10);
            this.m_oInfoLabel.Name = "m_oInfoLabel";
            this.m_oInfoLabel.Size = new System.Drawing.Size(311, 272);
            this.m_oInfoLabel.TabIndex = 0;
            this.m_oInfoLabel.Text = resources.GetString("m_oInfoLabel.Text");
            this.m_oInfoLabel.Visible = false;
            // 
            // m_oInfoButton
            // 
            this.m_oInfoButton.Location = new System.Drawing.Point(881, 30);
            this.m_oInfoButton.Name = "m_oInfoButton";
            this.m_oInfoButton.Size = new System.Drawing.Size(82, 23);
            this.m_oInfoButton.TabIndex = 3;
            this.m_oInfoButton.Text = "Toggle Info";
            this.m_oInfoButton.UseVisualStyleBackColor = true;
            // 
            // m_oCloseButton
            // 
            this.m_oCloseButton.Location = new System.Drawing.Point(881, 724);
            this.m_oCloseButton.Name = "m_oCloseButton";
            this.m_oCloseButton.Size = new System.Drawing.Size(82, 23);
            this.m_oCloseButton.TabIndex = 4;
            this.m_oCloseButton.Text = "Close";
            this.m_oCloseButton.UseVisualStyleBackColor = true;
            // 
            // m_oClearDesignButton
            // 
            this.m_oClearDesignButton.Location = new System.Drawing.Point(12, 724);
            this.m_oClearDesignButton.Name = "m_oClearDesignButton";
            this.m_oClearDesignButton.Size = new System.Drawing.Size(82, 23);
            this.m_oClearDesignButton.TabIndex = 5;
            this.m_oClearDesignButton.Text = "Clear Design";
            this.m_oClearDesignButton.UseVisualStyleBackColor = true;
            // 
            // m_oCreateSeriesButton
            // 
            this.m_oCreateSeriesButton.Location = new System.Drawing.Point(100, 724);
            this.m_oCreateSeriesButton.Name = "m_oCreateSeriesButton";
            this.m_oCreateSeriesButton.Size = new System.Drawing.Size(82, 23);
            this.m_oCreateSeriesButton.TabIndex = 6;
            this.m_oCreateSeriesButton.Text = "Create Series";
            this.m_oCreateSeriesButton.UseVisualStyleBackColor = true;
            // 
            // m_oDeleteSeriesButton
            // 
            this.m_oDeleteSeriesButton.Location = new System.Drawing.Point(188, 724);
            this.m_oDeleteSeriesButton.Name = "m_oDeleteSeriesButton";
            this.m_oDeleteSeriesButton.Size = new System.Drawing.Size(82, 23);
            this.m_oDeleteSeriesButton.TabIndex = 7;
            this.m_oDeleteSeriesButton.Text = "Delete Series";
            this.m_oDeleteSeriesButton.UseVisualStyleBackColor = true;
            // 
            // m_oSetSeriesButton
            // 
            this.m_oSetSeriesButton.Location = new System.Drawing.Point(276, 724);
            this.m_oSetSeriesButton.Name = "m_oSetSeriesButton";
            this.m_oSetSeriesButton.Size = new System.Drawing.Size(82, 23);
            this.m_oSetSeriesButton.TabIndex = 8;
            this.m_oSetSeriesButton.Text = "Set Series";
            this.m_oSetSeriesButton.UseVisualStyleBackColor = true;
            // 
            // m_oReplaceAllButton
            // 
            this.m_oReplaceAllButton.Location = new System.Drawing.Point(364, 724);
            this.m_oReplaceAllButton.Name = "m_oReplaceAllButton";
            this.m_oReplaceAllButton.Size = new System.Drawing.Size(82, 23);
            this.m_oReplaceAllButton.TabIndex = 9;
            this.m_oReplaceAllButton.Text = "Replace All";
            this.m_oReplaceAllButton.UseVisualStyleBackColor = true;
            // 
            // m_oInstantButton
            // 
            this.m_oInstantButton.Location = new System.Drawing.Point(786, 724);
            this.m_oInstantButton.Margin = new System.Windows.Forms.Padding(10);
            this.m_oInstantButton.Name = "m_oInstantButton";
            this.m_oInstantButton.Size = new System.Drawing.Size(82, 23);
            this.m_oInstantButton.TabIndex = 3;
            this.m_oInstantButton.Text = "Instant";
            this.m_oInstantButton.UseVisualStyleBackColor = true;
            // 
            // m_oSubMunitionComboBox
            // 
            this.m_oSubMunitionComboBox.FormattingEnabled = true;
            this.m_oSubMunitionComboBox.Location = new System.Drawing.Point(87, 13);
            this.m_oSubMunitionComboBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oSubMunitionComboBox.Name = "m_oSubMunitionComboBox";
            this.m_oSubMunitionComboBox.Size = new System.Drawing.Size(219, 21);
            this.m_oSubMunitionComboBox.TabIndex = 11;
            // 
            // m_oMissileTypeLabel
            // 
            this.m_oMissileTypeLabel.AutoSize = true;
            this.m_oMissileTypeLabel.Location = new System.Drawing.Point(6, 16);
            this.m_oMissileTypeLabel.Name = "m_oMissileTypeLabel";
            this.m_oMissileTypeLabel.Size = new System.Drawing.Size(65, 13);
            this.m_oMissileTypeLabel.TabIndex = 11;
            this.m_oMissileTypeLabel.Text = "Missile Type";
            // 
            // m_oSubNumberLabel
            // 
            this.m_oSubNumberLabel.AutoSize = true;
            this.m_oSubNumberLabel.Location = new System.Drawing.Point(7, 43);
            this.m_oSubNumberLabel.Name = "m_oSubNumberLabel";
            this.m_oSubNumberLabel.Size = new System.Drawing.Size(44, 13);
            this.m_oSubNumberLabel.TabIndex = 12;
            this.m_oSubNumberLabel.Text = "Number";
            // 
            // m_oSubSizeLabel
            // 
            this.m_oSubSizeLabel.AutoSize = true;
            this.m_oSubSizeLabel.Location = new System.Drawing.Point(6, 69);
            this.m_oSubSizeLabel.Name = "m_oSubSizeLabel";
            this.m_oSubSizeLabel.Size = new System.Drawing.Size(61, 13);
            this.m_oSubSizeLabel.TabIndex = 13;
            this.m_oSubSizeLabel.Text = "Missile Size";
            // 
            // m_oTotalMSizeLabel
            // 
            this.m_oTotalMSizeLabel.AutoSize = true;
            this.m_oTotalMSizeLabel.Location = new System.Drawing.Point(6, 95);
            this.m_oTotalMSizeLabel.Name = "m_oTotalMSizeLabel";
            this.m_oTotalMSizeLabel.Size = new System.Drawing.Size(54, 13);
            this.m_oTotalMSizeLabel.TabIndex = 14;
            this.m_oTotalMSizeLabel.Text = "Total Size";
            // 
            // m_oMaterialsLabel
            // 
            this.m_oMaterialsLabel.AutoSize = true;
            this.m_oMaterialsLabel.Location = new System.Drawing.Point(6, 121);
            this.m_oMaterialsLabel.Name = "m_oMaterialsLabel";
            this.m_oMaterialsLabel.Size = new System.Drawing.Size(49, 13);
            this.m_oMaterialsLabel.TabIndex = 15;
            this.m_oMaterialsLabel.Text = "Materials";
            // 
            // m_oSepRangeLabel
            // 
            this.m_oSepRangeLabel.AutoSize = true;
            this.m_oSepRangeLabel.Location = new System.Drawing.Point(144, 43);
            this.m_oSepRangeLabel.Name = "m_oSepRangeLabel";
            this.m_oSepRangeLabel.Size = new System.Drawing.Size(105, 13);
            this.m_oSepRangeLabel.TabIndex = 16;
            this.m_oSepRangeLabel.Text = "Separation Range(k)";
            // 
            // m_oMissileCostLabel
            // 
            this.m_oMissileCostLabel.AutoSize = true;
            this.m_oMissileCostLabel.Location = new System.Drawing.Point(187, 69);
            this.m_oMissileCostLabel.Name = "m_oMissileCostLabel";
            this.m_oMissileCostLabel.Size = new System.Drawing.Size(62, 13);
            this.m_oMissileCostLabel.TabIndex = 17;
            this.m_oMissileCostLabel.Text = "Missile Cost";
            // 
            // m_oTotalMCostLabel
            // 
            this.m_oTotalMCostLabel.AutoSize = true;
            this.m_oTotalMCostLabel.Location = new System.Drawing.Point(194, 95);
            this.m_oTotalMCostLabel.Name = "m_oTotalMCostLabel";
            this.m_oTotalMCostLabel.Size = new System.Drawing.Size(55, 13);
            this.m_oTotalMCostLabel.TabIndex = 18;
            this.m_oTotalMCostLabel.Text = "Total Cost";
            // 
            // m_oSubTotalSizeTextBox
            // 
            this.m_oSubTotalSizeTextBox.Location = new System.Drawing.Point(87, 92);
            this.m_oSubTotalSizeTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oSubTotalSizeTextBox.Name = "m_oSubTotalSizeTextBox";
            this.m_oSubTotalSizeTextBox.ReadOnly = true;
            this.m_oSubTotalSizeTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oSubTotalSizeTextBox.TabIndex = 39;
            this.m_oSubTotalSizeTextBox.Text = "0";
            this.m_oSubTotalSizeTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oSubNumberTextBox
            // 
            this.m_oSubNumberTextBox.Location = new System.Drawing.Point(87, 40);
            this.m_oSubNumberTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oSubNumberTextBox.Name = "m_oSubNumberTextBox";
            this.m_oSubNumberTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oSubNumberTextBox.TabIndex = 37;
            this.m_oSubNumberTextBox.Text = "0";
            this.m_oSubNumberTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oSubSizeTextBox
            // 
            this.m_oSubSizeTextBox.Location = new System.Drawing.Point(87, 66);
            this.m_oSubSizeTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oSubSizeTextBox.Name = "m_oSubSizeTextBox";
            this.m_oSubSizeTextBox.ReadOnly = true;
            this.m_oSubSizeTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oSubSizeTextBox.TabIndex = 38;
            this.m_oSubSizeTextBox.Text = "0";
            this.m_oSubSizeTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oSubTotalCostTextBox
            // 
            this.m_oSubTotalCostTextBox.Location = new System.Drawing.Point(262, 92);
            this.m_oSubTotalCostTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oSubTotalCostTextBox.Name = "m_oSubTotalCostTextBox";
            this.m_oSubTotalCostTextBox.ReadOnly = true;
            this.m_oSubTotalCostTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oSubTotalCostTextBox.TabIndex = 42;
            this.m_oSubTotalCostTextBox.Text = "0";
            this.m_oSubTotalCostTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oSepRangeTextBox
            // 
            this.m_oSepRangeTextBox.Location = new System.Drawing.Point(262, 40);
            this.m_oSepRangeTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oSepRangeTextBox.Name = "m_oSepRangeTextBox";
            this.m_oSepRangeTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oSepRangeTextBox.TabIndex = 40;
            this.m_oSepRangeTextBox.Text = "150";
            this.m_oSepRangeTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oSubCostTextBox
            // 
            this.m_oSubCostTextBox.Location = new System.Drawing.Point(262, 66);
            this.m_oSubCostTextBox.Margin = new System.Windows.Forms.Padding(10, 3, 10, 3);
            this.m_oSubCostTextBox.Name = "m_oSubCostTextBox";
            this.m_oSubCostTextBox.ReadOnly = true;
            this.m_oSubCostTextBox.Size = new System.Drawing.Size(44, 20);
            this.m_oSubCostTextBox.TabIndex = 41;
            this.m_oSubCostTextBox.Text = "0";
            this.m_oSubCostTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oMaterialsRichTextBox
            // 
            this.m_oMaterialsRichTextBox.Location = new System.Drawing.Point(87, 118);
            this.m_oMaterialsRichTextBox.Margin = new System.Windows.Forms.Padding(10);
            this.m_oMaterialsRichTextBox.Name = "m_oMaterialsRichTextBox";
            this.m_oMaterialsRichTextBox.ReadOnly = true;
            this.m_oMaterialsRichTextBox.Size = new System.Drawing.Size(219, 64);
            this.m_oMaterialsRichTextBox.TabIndex = 3;
            this.m_oMaterialsRichTextBox.Text = "0x Boronide  0x Corbomite  0x Tritanium  0x Uridium  0x Gallicite  0x Corundium  " +
                "0x Duranium  0x Neutronium\nNot Yet Implemented";
            // 
            // MissileDesign
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(975, 759);
            this.Controls.Add(this.m_oInstantButton);
            this.Controls.Add(this.m_oReplaceAllButton);
            this.Controls.Add(this.m_oSetSeriesButton);
            this.Controls.Add(this.m_oDeleteSeriesButton);
            this.Controls.Add(this.m_oCreateSeriesButton);
            this.Controls.Add(this.m_oClearDesignButton);
            this.Controls.Add(this.m_oCloseButton);
            this.Controls.Add(this.m_oInfoButton);
            this.Controls.Add(this.m_oSecondStageGroupBox);
            this.Controls.Add(this.m_oSystemParametersGroupBox);
            this.Controls.Add(this.m_oInfoGroupBox);
            this.Controls.Add(this.m_oMissileEngineGroupBox);
            this.Controls.Add(this.m_oPreviousDesignGroupBox);
            this.Controls.Add(this.m_oMissileSeriesGroupBox);
            this.Controls.Add(this.m_oCompSizeBox);
            this.Controls.Add(this.m_oEmpireGroupBox);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "MissileDesign";
            this.Text = "MissileDesign";
            this.m_oEmpireGroupBox.ResumeLayout(false);
            this.m_oCompSizeBox.ResumeLayout(false);
            this.m_oCompSizeBox.PerformLayout();
            this.m_oParametersGroupBox.ResumeLayout(false);
            this.m_oParametersGroupBox.PerformLayout();
            this.m_oSensorGroupBox.ResumeLayout(false);
            this.m_oSensorGroupBox.PerformLayout();
            this.m_oDefenceGroupBox.ResumeLayout(false);
            this.m_oDefenceGroupBox.PerformLayout();
            this.m_oModGroupBox.ResumeLayout(false);
            this.m_oModGroupBox.PerformLayout();
            this.m_oMissileSeriesGroupBox.ResumeLayout(false);
            this.m_oPreviousDesignGroupBox.ResumeLayout(false);
            this.m_oMissileEngineGroupBox.ResumeLayout(false);
            this.m_oMissileEngineGroupBox.PerformLayout();
            this.m_oSecondStageGroupBox.ResumeLayout(false);
            this.m_oSecondStageGroupBox.PerformLayout();
            this.m_oSystemParametersGroupBox.ResumeLayout(false);
            this.m_oSystemParametersGroupBox.PerformLayout();
            this.m_oInfoGroupBox.ResumeLayout(false);
            this.m_oInfoGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private GroupBox m_oEmpireGroupBox;
        private GroupBox m_oCompSizeBox;
        private GroupBox m_oParametersGroupBox;
        private GroupBox m_oSensorGroupBox;
        private GroupBox m_oDefenceGroupBox;
        private GroupBox m_oModGroupBox;
        private GroupBox m_oMissileSeriesGroupBox;
        private GroupBox m_oPreviousDesignGroupBox;
        private GroupBox m_oMissileEngineGroupBox;
        private GroupBox m_oSecondStageGroupBox;
        private GroupBox m_oSystemParametersGroupBox;
        private GroupBox m_oInfoGroupBox;
        private ComboBox m_oEmpireComboBox;
        private Button m_oInfoButton;
        private Button m_oCloseButton;
        private Button m_oClearDesignButton;
        private Button m_oCreateSeriesButton;
        private Button m_oDeleteSeriesButton;
        private Button m_oSetSeriesButton;
        private Button m_oReplaceAllButton;
        private TextBox m_oTotalEngineSizeTextBox;
        private TextBox m_oNumberEnginesTextBox;
        private TextBox m_oTotalEngineCostTextBox;
        private TextBox m_oTotalEPTextBox;
        private Label m_oTotalCostLabel;
        private Label m_oTotalEPLabel;
        private Label m_oESizeLabel;
        private Label m_oNumberLabel;
        private Label m_oEngineLabel;
        private ComboBox m_oMissileEngineComboBox;
        private Label m_oValueLabel;
        private Label m_oMSPLabel;
        private Label m_oReactorLabel;
        private Label m_oAgilityLabel;
        private Label m_oFuelLabel;
        private Label m_oWarheadLabel;
        private TextBox m_oReactorMSPTextBox;
        private TextBox m_oAgilityMSPTextBox;
        private TextBox m_oFuelMSPTextBox;
        private TextBox m_oWHMSPTextBox;
        private TextBox m_oReactorValueTextBox;
        private TextBox m_oAgilityValueTextBox;
        private TextBox m_oFuelValueTextBox;
        private TextBox m_oWHValueTextBox;
        private Button m_oCreateButton;
        private TextBox m_oMissileNameTextBox;
        private RichTextBox m_oMissileSummaryTextBox;
        private Label m_oActiveResLabel;
        private TextBox m_oResolutionTextBox;
        private Label m_oGeoLabel;
        private Label m_oActiveLabel;
        private Label m_oEMLabel;
        private TextBox m_oActiveValueTextBox;
        private Label m_oThermalLabel;
        private TextBox m_oThermalValueTextBox;
        private TextBox m_oEMValueTextBox;
        private TextBox m_oGeoMSPTextBox;
        private TextBox m_oGeoValueTextBox;
        private TextBox m_oEMMSPTextBox;
        private TextBox m_oActiveMSPTextBox;
        private TextBox m_oThermalMSPTextBox;
        private Label m_oArmourLabel;
        private TextBox m_oArmourValueTextBox;
        private TextBox m_oECMMSPTextBox;
        private Label m_oECMLabel;
        private TextBox m_oArmourMSPTextBox;
        private TextBox m_oECMValueTextBox;
        private CheckBox m_oERCheckBox;
        private CheckBox m_oLaserWCheckBox;
        private ListBox m_oMSeriesListBox;
        private ComboBox m_oMSeriesComboBox;
        private Button m_oInstantButton;
        private ComboBox m_oPreviousOrdnanceComboBox;
        private Label m_oInfoLabel;
        private RichTextBox m_oMaterialsRichTextBox;
        private TextBox m_oSubTotalCostTextBox;
        private TextBox m_oSepRangeTextBox;
        private TextBox m_oSubCostTextBox;
        private TextBox m_oSubTotalSizeTextBox;
        private Label m_oTotalMCostLabel;
        private TextBox m_oSubNumberTextBox;
        private TextBox m_oSubSizeTextBox;
        private Label m_oMissileCostLabel;
        private Label m_oSepRangeLabel;
        private Label m_oMaterialsLabel;
        private Label m_oTotalMSizeLabel;
        private Label m_oSubSizeLabel;
        private Label m_oSubNumberLabel;
        private Label m_oMissileTypeLabel;
        private ComboBox m_oSubMunitionComboBox;
    }
}