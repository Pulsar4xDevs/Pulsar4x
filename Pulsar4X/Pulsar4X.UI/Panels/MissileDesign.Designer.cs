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
        /// The overall missile summary will be printed to this box.
        /// </summary>
        public RichTextBox MissileSummaryTextBox
        {
            get { return m_oMissileSummaryTextBox; }
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
            this.m_oDefenceGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oModGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oMissileSeriesGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oPreviousDesignGroupBox = new System.Windows.Forms.GroupBox();
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
            this.m_oInfoGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oInfoButton = new System.Windows.Forms.Button();
            this.m_oCloseButton = new System.Windows.Forms.Button();
            this.m_oClearDesignButton = new System.Windows.Forms.Button();
            this.m_oCreateSeriesButton = new System.Windows.Forms.Button();
            this.m_oDeleteSeriesButton = new System.Windows.Forms.Button();
            this.m_oSetSeriesButton = new System.Windows.Forms.Button();
            this.m_oReplaceAllButton = new System.Windows.Forms.Button();
            this.m_oMissileSummaryTextBox = new System.Windows.Forms.RichTextBox();
            this.m_oMissileNameTextBox = new System.Windows.Forms.TextBox();
            this.m_oCreateButton = new System.Windows.Forms.Button();
            this.m_oEmpireGroupBox.SuspendLayout();
            this.m_oCompSizeBox.SuspendLayout();
            this.m_oParametersGroupBox.SuspendLayout();
            this.m_oMissileEngineGroupBox.SuspendLayout();
            this.m_oSystemParametersGroupBox.SuspendLayout();
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
            this.m_oCompSizeBox.Size = new System.Drawing.Size(277, 491);
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
            this.m_oParametersGroupBox.Location = new System.Drawing.Point(8, 56);
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
            this.m_oSensorGroupBox.Location = new System.Drawing.Point(8, 193);
            this.m_oSensorGroupBox.Margin = new System.Windows.Forms.Padding(5);
            this.m_oSensorGroupBox.Name = "m_oSensorGroupBox";
            this.m_oSensorGroupBox.Size = new System.Drawing.Size(261, 127);
            this.m_oSensorGroupBox.TabIndex = 2;
            this.m_oSensorGroupBox.TabStop = false;
            this.m_oSensorGroupBox.Text = "Sensors";
            // 
            // m_oDefenceGroupBox
            // 
            this.m_oDefenceGroupBox.Location = new System.Drawing.Point(8, 330);
            this.m_oDefenceGroupBox.Margin = new System.Windows.Forms.Padding(5);
            this.m_oDefenceGroupBox.Name = "m_oDefenceGroupBox";
            this.m_oDefenceGroupBox.Size = new System.Drawing.Size(261, 100);
            this.m_oDefenceGroupBox.TabIndex = 2;
            this.m_oDefenceGroupBox.TabStop = false;
            this.m_oDefenceGroupBox.Text = "Defences";
            // 
            // m_oModGroupBox
            // 
            this.m_oModGroupBox.Location = new System.Drawing.Point(8, 433);
            this.m_oModGroupBox.Margin = new System.Windows.Forms.Padding(5);
            this.m_oModGroupBox.Name = "m_oModGroupBox";
            this.m_oModGroupBox.Size = new System.Drawing.Size(261, 50);
            this.m_oModGroupBox.TabIndex = 2;
            this.m_oModGroupBox.TabStop = false;
            this.m_oModGroupBox.Text = "Special Modifiers";
            // 
            // m_oMissileSeriesGroupBox
            // 
            this.m_oMissileSeriesGroupBox.Location = new System.Drawing.Point(12, 572);
            this.m_oMissileSeriesGroupBox.Name = "m_oMissileSeriesGroupBox";
            this.m_oMissileSeriesGroupBox.Size = new System.Drawing.Size(277, 140);
            this.m_oMissileSeriesGroupBox.TabIndex = 2;
            this.m_oMissileSeriesGroupBox.TabStop = false;
            this.m_oMissileSeriesGroupBox.Text = "Missile Series";
            // 
            // m_oPreviousDesignGroupBox
            // 
            this.m_oPreviousDesignGroupBox.Location = new System.Drawing.Point(295, 12);
            this.m_oPreviousDesignGroupBox.Name = "m_oPreviousDesignGroupBox";
            this.m_oPreviousDesignGroupBox.Size = new System.Drawing.Size(319, 50);
            this.m_oPreviousDesignGroupBox.TabIndex = 2;
            this.m_oPreviousDesignGroupBox.TabStop = false;
            this.m_oPreviousDesignGroupBox.Text = "MSP Allocation of Previous Designs";
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
            // m_oInfoGroupBox
            // 
            this.m_oInfoGroupBox.Location = new System.Drawing.Point(620, 68);
            this.m_oInfoGroupBox.Name = "m_oInfoGroupBox";
            this.m_oInfoGroupBox.Size = new System.Drawing.Size(343, 324);
            this.m_oInfoGroupBox.TabIndex = 2;
            this.m_oInfoGroupBox.TabStop = false;
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
            // m_oMissileSummaryTextBox
            // 
            this.m_oMissileSummaryTextBox.Location = new System.Drawing.Point(13, 57);
            this.m_oMissileSummaryTextBox.Margin = new System.Windows.Forms.Padding(10);
            this.m_oMissileSummaryTextBox.Name = "m_oMissileSummaryTextBox";
            this.m_oMissileSummaryTextBox.Size = new System.Drawing.Size(642, 244);
            this.m_oMissileSummaryTextBox.TabIndex = 0;
            this.m_oMissileSummaryTextBox.Text = "";
            // 
            // m_oMissileNameTextBox
            // 
            this.m_oMissileNameTextBox.Location = new System.Drawing.Point(13, 28);
            this.m_oMissileNameTextBox.Margin = new System.Windows.Forms.Padding(10);
            this.m_oMissileNameTextBox.Name = "m_oMissileNameTextBox";
            this.m_oMissileNameTextBox.Size = new System.Drawing.Size(540, 20);
            this.m_oMissileNameTextBox.TabIndex = 1;
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
            // MissileDesign
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(975, 759);
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
            this.m_oMissileEngineGroupBox.ResumeLayout(false);
            this.m_oMissileEngineGroupBox.PerformLayout();
            this.m_oSystemParametersGroupBox.ResumeLayout(false);
            this.m_oSystemParametersGroupBox.PerformLayout();
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
    }
}