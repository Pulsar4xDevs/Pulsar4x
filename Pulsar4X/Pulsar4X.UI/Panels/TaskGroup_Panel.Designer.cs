using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Pulsar4X.UI.Panels
{
    partial class TaskGroup_Panel : DockContent
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>   
        /// Gets the TG selection combo box. 
        /// </summary>
        public ComboBox TaskGroupSelectionComboBox
        {
            get { return m_oTaskGroupName; }
        }

        /// <summary>
        /// Similarly this is the faction selection combo box.
        /// </summary>
        public ComboBox FactionSelectionComboBox
        {
            get { return m_oFactionName; }
        }

        /// <summary>
        /// This textbox is where the TG location should be printed.
        /// </summary>
        public TextBox TaskGroupLocationTextBox
        {
            get { return m_oTGLocation; }
        }

        /// <summary>
        /// Textbox for speed setting
        /// </summary>
        public TextBox SetSpeedTextBox
        {
            get { return m_oCurSpeedBox; }
        }

        /// <summary>
        /// Textbox for max speed.
        /// </summary>
        public TextBox MaxSpeedTextBox
        {
            get { return m_oMaxSpeedBox; }
        }

        /// <summary>
        /// ETA and distance to destination box.
        /// </summary>
        public TextBox TimeDistanceTextBox
        {
            get { return m_oTimeDistTextBox; }
        }

        /// <summary>
        /// Here is where the list of potential taskgroup locations should be printed.
        /// </summary>
        public ListBox SystemLocationsListBox
        {
            get { return m_oSystemLocationsListBox; }
        }

        /// <summary>
        /// Allowed actions go here.
        /// </summary>
        public ListBox AvailableActionsListBox
        {
            get { return m_oActionsAvailableListBox; }
        }

        /// <summary>
        /// Current orders are placed here.
        /// </summary>
        public ListBox PlottedMovesListBox
        {
            get { return m_oPlottedMoveListBox; }
        }

        /// <summary>
        /// Should detected contacts be displayed?
        /// </summary>
        public CheckBox DisplayContactsCheckBox
        {
            get { return m_oContactsCheckBox; }
        }

        /// <summary>
        /// display taskgroups checkbox.
        /// </summary>
        public CheckBox DisplayTaskGroupsCheckBox
        {
            get { return m_oTaskGroupsCheckBox; }
        }

        /// <summary>
        /// Display waypoints checkbox.
        /// </summary>
        public CheckBox DisplayWaypointsCheckBox
        {
            get { return m_oWaypointCheckBox; }
        }

        /// <summary>
        /// creates a new task group.
        /// </summary>
        public Button NewTaskGroupButton
        {
            get { return m_oNewTGButton; }
        }

        /// <summary>
        /// Renames the selected task group.
        /// </summary>
        public Button RenameTaskGroupButton
        {
            get { return m_oRenameTGButton; }
        }

        /// <summary>
        /// Sets speed to user entered value if valid.
        /// </summary>
        public Button SetSpeedButton
        {
            get { return m_oSetSpeedButton; }
        }

        /// <summary>
        /// Sets speed to maximum possible
        /// </summary>
        public Button MaxSpeedButton
        {
            get { return m_oMaxSpeedButton; }
        }

        /// <summary>
        /// Adds a taskgroup order to the current task group's orders list
        /// </summary>
        public Button AddMoveButton
        {
            get { return m_oAddMoveButton; }
        }

        /// <summary>
        /// Removes the last order issued to the current task group.
        /// </summary>
        public Button RemoveButton 
        { 
            get { return m_oRemoveButton; } 
        }

        /// <summary>
        /// Clears all taskgroup orders.
        /// </summary>
        public Button RemoveAllButton
        {
            get { return m_oRemoveAllButton; }
        }

        /// <summary>
        /// Time and distance for only the current order.
        /// </summary>
        public RadioButton CurrentTDRadioButton
        {
            get { return m_oCurrentTDRadioButton; }
        }

        /// <summary>
        /// Time and distance for all orders.
        /// </summary>
        public RadioButton AllOrdersTDRadioButton
        {
            get { return m_oAllOrdersRadioButton; }
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
            this.m_oTaskGroupName = new System.Windows.Forms.ComboBox();
            this.m_oShipsBox = new System.Windows.Forms.GroupBox();
            this.m_oGeneralTGDetailsBox = new System.Windows.Forms.GroupBox();
            this.m_oTaskForceName = new System.Windows.Forms.ComboBox();
            this.m_oTFLabel = new System.Windows.Forms.Label();
            this.m_oLocationLabel = new System.Windows.Forms.Label();
            this.m_oTGLocation = new System.Windows.Forms.TextBox();
            this.m_oFactionName = new System.Windows.Forms.ComboBox();
            this.m_oFactionLabel = new System.Windows.Forms.Label();
            this.m_oTaskGroupLabel = new System.Windows.Forms.Label();
            this.m_oSpeedBox = new System.Windows.Forms.GroupBox();
            this.m_oMaxSpeedButton = new System.Windows.Forms.Button();
            this.m_oSetSpeedButton = new System.Windows.Forms.Button();
            this.m_oMaxSpeedBox = new System.Windows.Forms.TextBox();
            this.m_oCurSpeedBox = new System.Windows.Forms.TextBox();
            this.m_oCenterShowGF = new System.Windows.Forms.GroupBox();
            this.m_oShowGroundForces = new System.Windows.Forms.CheckBox();
            this.m_oCenterMapCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oSurveyBox = new System.Windows.Forms.GroupBox();
            this.m_oGeoTextBox = new System.Windows.Forms.TextBox();
            this.m_oGeoLabel = new System.Windows.Forms.Label();
            this.m_oGravLabel = new System.Windows.Forms.Label();
            this.m_oGravTextBox = new System.Windows.Forms.TextBox();
            this.m_oInitiativeBox = new System.Windows.Forms.GroupBox();
            this.m_oInitiativeButton = new System.Windows.Forms.Button();
            this.m_oCurrentInitTextBox = new System.Windows.Forms.TextBox();
            this.m_oCurrentInitLabel = new System.Windows.Forms.Label();
            this.m_oMaxInitLabel = new System.Windows.Forms.Label();
            this.m_oMaxInitTextBox = new System.Windows.Forms.TextBox();
            this.m_oOfficerBox = new System.Windows.Forms.GroupBox();
            this.m_oSeniorOfficerTextBox = new System.Windows.Forms.TextBox();
            this.m_oOrderTimeDistBox = new System.Windows.Forms.GroupBox();
            this.m_oAllOrdersRadioButton = new System.Windows.Forms.RadioButton();
            this.m_oCurrentTDRadioButton = new System.Windows.Forms.RadioButton();
            this.m_oTimeDistTextBox = new System.Windows.Forms.TextBox();
            this.m_oTaskGroupTabControl = new System.Windows.Forms.TabControl();
            this.m_oTaskGroupOrdersTabPage = new System.Windows.Forms.TabPage();
            this.m_oCargoFightersTroopsGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oDefaultCondEscortOrdersGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oOOBGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oTaskGroupOrdersBox = new System.Windows.Forms.GroupBox();
            this.m_oRepeatOrdersTextBox = new System.Windows.Forms.TextBox();
            this.m_oRepeatOrderButton = new System.Windows.Forms.Button();
            this.m_oCycleMovesCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oOrderDelayTextBox = new System.Windows.Forms.TextBox();
            this.m_oOrderDelayLabel = new System.Windows.Forms.Label();
            this.m_oAutoRouteCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oLoadAmtTextBox = new System.Windows.Forms.TextBox();
            this.m_oOrbitalDistanceTextBox = new System.Windows.Forms.TextBox();
            this.m_oLoadLimitLabel = new System.Windows.Forms.Label();
            this.m_oOrbitalDistanceLabel = new System.Windows.Forms.Label();
            this.m_oRemoveAllButton = new System.Windows.Forms.Button();
            this.m_oRemoveButton = new System.Windows.Forms.Button();
            this.m_oAddMoveButton = new System.Windows.Forms.Button();
            this.m_oPlottedMoveLabel = new System.Windows.Forms.Label();
            this.m_oActionsAvailableLabel = new System.Windows.Forms.Label();
            this.m_oSystemLocationsLabel = new System.Windows.Forms.Label();
            this.m_oPlottedMoveListBox = new System.Windows.Forms.ListBox();
            this.m_oActionsAvailableListBox = new System.Windows.Forms.ListBox();
            this.m_oSystemLocationsListBox = new System.Windows.Forms.ListBox();
            this.m_oCopyOrdersGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oIncCondOrdersCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oIncDefaultCheckBox = new System.Windows.Forms.CheckBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.m_oCopyOrdersButton = new System.Windows.Forms.Button();
            this.m_oSystemDisplayOptionsBox = new System.Windows.Forms.GroupBox();
            this.m_oShowAllPopsCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oOrderFilteringCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oExcludeSurveyedCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oSurveyLocationsCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oWrecksCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oLifePodsCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oCometsCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oContactsCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oWaypointCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oTaskGroupsCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oAsteroidsCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oMoonsCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oSpecialOrdersTabPage = new System.Windows.Forms.TabPage();
            this.m_oHistoryTabPage = new System.Windows.Forms.TabPage();
            this.m_oNavalOrgTabPage = new System.Windows.Forms.TabPage();
            this.m_oButtonBox = new System.Windows.Forms.GroupBox();
            this.m_oAssembleButton = new System.Windows.Forms.Button();
            this.m_oDetachButton = new System.Windows.Forms.Button();
            this.m_oEscortButton = new System.Windows.Forms.Button();
            this.m_oSaveEscortsButton = new System.Windows.Forms.Button();
            this.m_oMissileLaunchButton = new System.Windows.Forms.Button();
            this.m_oReloadParaButton = new System.Windows.Forms.Button();
            this.m_oHyperOnButton = new System.Windows.Forms.Button();
            this.m_oHyperOffButton = new System.Windows.Forms.Button();
            this.m_oShieldsOnButton = new System.Windows.Forms.Button();
            this.m_oShieldsOffButton = new System.Windows.Forms.Button();
            this.m_oNoDefaultButton = new System.Windows.Forms.Button();
            this.m_oCloseButton = new System.Windows.Forms.Button();
            this.m_oDeployEscortsButton = new System.Windows.Forms.Button();
            this.m_oRecallEscortsButton = new System.Windows.Forms.Button();
            this.m_oEqualizeFuelButton = new System.Windows.Forms.Button();
            this.m_oEqualizeMaintButton = new System.Windows.Forms.Button();
            this.m_oNoConditionsButton = new System.Windows.Forms.Button();
            this.m_oRecoverParaButton = new System.Windows.Forms.Button();
            this.m_oLaunchParaButton = new System.Windows.Forms.Button();
            this.m_oDeleteTGButton = new System.Windows.Forms.Button();
            this.m_oOOBButton = new System.Windows.Forms.Button();
            this.m_oRenameTGButton = new System.Windows.Forms.Button();
            this.m_oAddColonyButton = new System.Windows.Forms.Button();
            this.m_oSystemMapButton = new System.Windows.Forms.Button();
            this.m_oNewTGButton = new System.Windows.Forms.Button();
            this.m_oGeneralTGDetailsBox.SuspendLayout();
            this.m_oSpeedBox.SuspendLayout();
            this.m_oCenterShowGF.SuspendLayout();
            this.m_oSurveyBox.SuspendLayout();
            this.m_oInitiativeBox.SuspendLayout();
            this.m_oOfficerBox.SuspendLayout();
            this.m_oOrderTimeDistBox.SuspendLayout();
            this.m_oTaskGroupTabControl.SuspendLayout();
            this.m_oTaskGroupOrdersTabPage.SuspendLayout();
            this.m_oTaskGroupOrdersBox.SuspendLayout();
            this.m_oCopyOrdersGroupBox.SuspendLayout();
            this.m_oSystemDisplayOptionsBox.SuspendLayout();
            this.m_oButtonBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_oTaskGroupName
            // 
            this.m_oTaskGroupName.FormattingEnabled = true;
            this.m_oTaskGroupName.Location = new System.Drawing.Point(54, 52);
            this.m_oTaskGroupName.Name = "m_oTaskGroupName";
            this.m_oTaskGroupName.Size = new System.Drawing.Size(171, 21);
            this.m_oTaskGroupName.TabIndex = 1;
            // 
            // m_oShipsBox
            // 
            this.m_oShipsBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oShipsBox.Location = new System.Drawing.Point(380, 12);
            this.m_oShipsBox.MaximumSize = new System.Drawing.Size(810, 300);
            this.m_oShipsBox.MinimumSize = new System.Drawing.Size(810, 300);
            this.m_oShipsBox.Name = "m_oShipsBox";
            this.m_oShipsBox.Size = new System.Drawing.Size(810, 300);
            this.m_oShipsBox.TabIndex = 2;
            this.m_oShipsBox.TabStop = false;
            this.m_oShipsBox.Text = "Ships in TaskGroup - Double-Click to open Ship window";
            // 
            // m_oGeneralTGDetailsBox
            // 
            this.m_oGeneralTGDetailsBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oGeneralTGDetailsBox.Controls.Add(this.m_oTaskForceName);
            this.m_oGeneralTGDetailsBox.Controls.Add(this.m_oTFLabel);
            this.m_oGeneralTGDetailsBox.Controls.Add(this.m_oLocationLabel);
            this.m_oGeneralTGDetailsBox.Controls.Add(this.m_oTGLocation);
            this.m_oGeneralTGDetailsBox.Controls.Add(this.m_oFactionName);
            this.m_oGeneralTGDetailsBox.Controls.Add(this.m_oFactionLabel);
            this.m_oGeneralTGDetailsBox.Controls.Add(this.m_oTaskGroupLabel);
            this.m_oGeneralTGDetailsBox.Controls.Add(this.m_oTaskGroupName);
            this.m_oGeneralTGDetailsBox.Location = new System.Drawing.Point(12, 12);
            this.m_oGeneralTGDetailsBox.MaximumSize = new System.Drawing.Size(240, 150);
            this.m_oGeneralTGDetailsBox.MinimumSize = new System.Drawing.Size(240, 150);
            this.m_oGeneralTGDetailsBox.Name = "m_oGeneralTGDetailsBox";
            this.m_oGeneralTGDetailsBox.Size = new System.Drawing.Size(240, 150);
            this.m_oGeneralTGDetailsBox.TabIndex = 3;
            this.m_oGeneralTGDetailsBox.TabStop = false;
            this.m_oGeneralTGDetailsBox.Text = "Details and Special Orders";
            // 
            // m_oTaskForceName
            // 
            this.m_oTaskForceName.FormattingEnabled = true;
            this.m_oTaskForceName.Location = new System.Drawing.Point(54, 111);
            this.m_oTaskForceName.Name = "m_oTaskForceName";
            this.m_oTaskForceName.Size = new System.Drawing.Size(171, 21);
            this.m_oTaskForceName.TabIndex = 28;
            // 
            // m_oTFLabel
            // 
            this.m_oTFLabel.AutoSize = true;
            this.m_oTFLabel.Location = new System.Drawing.Point(10, 106);
            this.m_oTFLabel.MaximumSize = new System.Drawing.Size(35, 26);
            this.m_oTFLabel.MinimumSize = new System.Drawing.Size(35, 26);
            this.m_oTFLabel.Name = "m_oTFLabel";
            this.m_oTFLabel.Size = new System.Drawing.Size(35, 26);
            this.m_oTFLabel.TabIndex = 27;
            this.m_oTFLabel.Text = "Task  Force";
            // 
            // m_oLocationLabel
            // 
            this.m_oLocationLabel.AutoSize = true;
            this.m_oLocationLabel.Location = new System.Drawing.Point(6, 82);
            this.m_oLocationLabel.Name = "m_oLocationLabel";
            this.m_oLocationLabel.Size = new System.Drawing.Size(48, 13);
            this.m_oLocationLabel.TabIndex = 26;
            this.m_oLocationLabel.Text = "Location";
            // 
            // m_oTGLocation
            // 
            this.m_oTGLocation.Enabled = false;
            this.m_oTGLocation.Location = new System.Drawing.Point(54, 79);
            this.m_oTGLocation.Name = "m_oTGLocation";
            this.m_oTGLocation.Size = new System.Drawing.Size(171, 20);
            this.m_oTGLocation.TabIndex = 25;
            // 
            // m_oFactionName
            // 
            this.m_oFactionName.FormattingEnabled = true;
            this.m_oFactionName.Location = new System.Drawing.Point(54, 25);
            this.m_oFactionName.Name = "m_oFactionName";
            this.m_oFactionName.Size = new System.Drawing.Size(171, 21);
            this.m_oFactionName.TabIndex = 24;
            // 
            // m_oFactionLabel
            // 
            this.m_oFactionLabel.AutoSize = true;
            this.m_oFactionLabel.Location = new System.Drawing.Point(6, 28);
            this.m_oFactionLabel.Name = "m_oFactionLabel";
            this.m_oFactionLabel.Size = new System.Drawing.Size(39, 13);
            this.m_oFactionLabel.TabIndex = 23;
            this.m_oFactionLabel.Text = "Empire";
            // 
            // m_oTaskGroupLabel
            // 
            this.m_oTaskGroupLabel.AutoSize = true;
            this.m_oTaskGroupLabel.Location = new System.Drawing.Point(6, 55);
            this.m_oTaskGroupLabel.Name = "m_oTaskGroupLabel";
            this.m_oTaskGroupLabel.Size = new System.Drawing.Size(35, 13);
            this.m_oTaskGroupLabel.TabIndex = 22;
            this.m_oTaskGroupLabel.Text = "Name";
            // 
            // m_oSpeedBox
            // 
            this.m_oSpeedBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oSpeedBox.Controls.Add(this.m_oMaxSpeedButton);
            this.m_oSpeedBox.Controls.Add(this.m_oSetSpeedButton);
            this.m_oSpeedBox.Controls.Add(this.m_oMaxSpeedBox);
            this.m_oSpeedBox.Controls.Add(this.m_oCurSpeedBox);
            this.m_oSpeedBox.Location = new System.Drawing.Point(258, 15);
            this.m_oSpeedBox.MaximumSize = new System.Drawing.Size(116, 78);
            this.m_oSpeedBox.MinimumSize = new System.Drawing.Size(116, 78);
            this.m_oSpeedBox.Name = "m_oSpeedBox";
            this.m_oSpeedBox.Size = new System.Drawing.Size(116, 78);
            this.m_oSpeedBox.TabIndex = 3;
            this.m_oSpeedBox.TabStop = false;
            this.m_oSpeedBox.Text = "Cur / Max Speed";
            // 
            // m_oMaxSpeedButton
            // 
            this.m_oMaxSpeedButton.Location = new System.Drawing.Point(55, 49);
            this.m_oMaxSpeedButton.Name = "m_oMaxSpeedButton";
            this.m_oMaxSpeedButton.Size = new System.Drawing.Size(47, 23);
            this.m_oMaxSpeedButton.TabIndex = 31;
            this.m_oMaxSpeedButton.Text = "Max";
            this.m_oMaxSpeedButton.UseVisualStyleBackColor = true;
            // 
            // m_oSetSpeedButton
            // 
            this.m_oSetSpeedButton.Location = new System.Drawing.Point(55, 22);
            this.m_oSetSpeedButton.Name = "m_oSetSpeedButton";
            this.m_oSetSpeedButton.Size = new System.Drawing.Size(47, 23);
            this.m_oSetSpeedButton.TabIndex = 4;
            this.m_oSetSpeedButton.Text = "Set";
            this.m_oSetSpeedButton.UseVisualStyleBackColor = true;
            // 
            // m_oMaxSpeedBox
            // 
            this.m_oMaxSpeedBox.Enabled = false;
            this.m_oMaxSpeedBox.Location = new System.Drawing.Point(6, 50);
            this.m_oMaxSpeedBox.Name = "m_oMaxSpeedBox";
            this.m_oMaxSpeedBox.Size = new System.Drawing.Size(43, 20);
            this.m_oMaxSpeedBox.TabIndex = 30;
            // 
            // m_oCurSpeedBox
            // 
            this.m_oCurSpeedBox.Location = new System.Drawing.Point(6, 23);
            this.m_oCurSpeedBox.Name = "m_oCurSpeedBox";
            this.m_oCurSpeedBox.Size = new System.Drawing.Size(43, 20);
            this.m_oCurSpeedBox.TabIndex = 29;
            // 
            // m_oCenterShowGF
            // 
            this.m_oCenterShowGF.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oCenterShowGF.Controls.Add(this.m_oShowGroundForces);
            this.m_oCenterShowGF.Controls.Add(this.m_oCenterMapCheckBox);
            this.m_oCenterShowGF.Location = new System.Drawing.Point(258, 94);
            this.m_oCenterShowGF.MaximumSize = new System.Drawing.Size(116, 68);
            this.m_oCenterShowGF.MinimumSize = new System.Drawing.Size(116, 68);
            this.m_oCenterShowGF.Name = "m_oCenterShowGF";
            this.m_oCenterShowGF.Size = new System.Drawing.Size(116, 68);
            this.m_oCenterShowGF.TabIndex = 32;
            this.m_oCenterShowGF.TabStop = false;
            // 
            // m_oShowGroundForces
            // 
            this.m_oShowGroundForces.AutoSize = true;
            this.m_oShowGroundForces.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oShowGroundForces.Location = new System.Drawing.Point(6, 42);
            this.m_oShowGroundForces.MaximumSize = new System.Drawing.Size(92, 17);
            this.m_oShowGroundForces.MinimumSize = new System.Drawing.Size(92, 17);
            this.m_oShowGroundForces.Name = "m_oShowGroundForces";
            this.m_oShowGroundForces.Size = new System.Drawing.Size(92, 17);
            this.m_oShowGroundForces.TabIndex = 10;
            this.m_oShowGroundForces.Text = "Show Ground";
            this.m_oShowGroundForces.UseVisualStyleBackColor = true;
            // 
            // m_oCenterMapCheckBox
            // 
            this.m_oCenterMapCheckBox.AutoSize = true;
            this.m_oCenterMapCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oCenterMapCheckBox.Location = new System.Drawing.Point(6, 19);
            this.m_oCenterMapCheckBox.MaximumSize = new System.Drawing.Size(92, 17);
            this.m_oCenterMapCheckBox.MinimumSize = new System.Drawing.Size(92, 17);
            this.m_oCenterMapCheckBox.Name = "m_oCenterMapCheckBox";
            this.m_oCenterMapCheckBox.Size = new System.Drawing.Size(92, 17);
            this.m_oCenterMapCheckBox.TabIndex = 9;
            this.m_oCenterMapCheckBox.Text = "Center Map";
            this.m_oCenterMapCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oSurveyBox
            // 
            this.m_oSurveyBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oSurveyBox.Controls.Add(this.m_oGeoTextBox);
            this.m_oSurveyBox.Controls.Add(this.m_oGeoLabel);
            this.m_oSurveyBox.Controls.Add(this.m_oGravLabel);
            this.m_oSurveyBox.Controls.Add(this.m_oGravTextBox);
            this.m_oSurveyBox.Location = new System.Drawing.Point(12, 168);
            this.m_oSurveyBox.MaximumSize = new System.Drawing.Size(156, 37);
            this.m_oSurveyBox.MinimumSize = new System.Drawing.Size(156, 37);
            this.m_oSurveyBox.Name = "m_oSurveyBox";
            this.m_oSurveyBox.Size = new System.Drawing.Size(156, 37);
            this.m_oSurveyBox.TabIndex = 33;
            this.m_oSurveyBox.TabStop = false;
            this.m_oSurveyBox.Text = "Survey Points";
            // 
            // m_oGeoTextBox
            // 
            this.m_oGeoTextBox.Enabled = false;
            this.m_oGeoTextBox.Location = new System.Drawing.Point(113, 13);
            this.m_oGeoTextBox.Name = "m_oGeoTextBox";
            this.m_oGeoTextBox.Size = new System.Drawing.Size(32, 20);
            this.m_oGeoTextBox.TabIndex = 34;
            this.m_oGeoTextBox.Text = "0.0";
            this.m_oGeoTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oGeoLabel
            // 
            this.m_oGeoLabel.AutoSize = true;
            this.m_oGeoLabel.Location = new System.Drawing.Point(80, 16);
            this.m_oGeoLabel.Name = "m_oGeoLabel";
            this.m_oGeoLabel.Size = new System.Drawing.Size(27, 13);
            this.m_oGeoLabel.TabIndex = 33;
            this.m_oGeoLabel.Text = "Geo";
            // 
            // m_oGravLabel
            // 
            this.m_oGravLabel.AutoSize = true;
            this.m_oGravLabel.Location = new System.Drawing.Point(6, 16);
            this.m_oGravLabel.Name = "m_oGravLabel";
            this.m_oGravLabel.Size = new System.Drawing.Size(30, 13);
            this.m_oGravLabel.TabIndex = 29;
            this.m_oGravLabel.Text = "Grav";
            // 
            // m_oGravTextBox
            // 
            this.m_oGravTextBox.Enabled = false;
            this.m_oGravTextBox.Location = new System.Drawing.Point(42, 13);
            this.m_oGravTextBox.Name = "m_oGravTextBox";
            this.m_oGravTextBox.Size = new System.Drawing.Size(32, 20);
            this.m_oGravTextBox.TabIndex = 32;
            this.m_oGravTextBox.Text = "0.0";
            this.m_oGravTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oInitiativeBox
            // 
            this.m_oInitiativeBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oInitiativeBox.Controls.Add(this.m_oInitiativeButton);
            this.m_oInitiativeBox.Controls.Add(this.m_oCurrentInitTextBox);
            this.m_oInitiativeBox.Controls.Add(this.m_oCurrentInitLabel);
            this.m_oInitiativeBox.Controls.Add(this.m_oMaxInitLabel);
            this.m_oInitiativeBox.Controls.Add(this.m_oMaxInitTextBox);
            this.m_oInitiativeBox.Location = new System.Drawing.Point(174, 168);
            this.m_oInitiativeBox.MaximumSize = new System.Drawing.Size(200, 37);
            this.m_oInitiativeBox.MinimumSize = new System.Drawing.Size(200, 37);
            this.m_oInitiativeBox.Name = "m_oInitiativeBox";
            this.m_oInitiativeBox.Size = new System.Drawing.Size(200, 37);
            this.m_oInitiativeBox.TabIndex = 35;
            this.m_oInitiativeBox.TabStop = false;
            this.m_oInitiativeBox.Text = "Initiative";
            // 
            // m_oInitiativeButton
            // 
            this.m_oInitiativeButton.Location = new System.Drawing.Point(162, 12);
            this.m_oInitiativeButton.Name = "m_oInitiativeButton";
            this.m_oInitiativeButton.Size = new System.Drawing.Size(32, 20);
            this.m_oInitiativeButton.TabIndex = 32;
            this.m_oInitiativeButton.Text = "Set";
            this.m_oInitiativeButton.UseVisualStyleBackColor = true;
            // 
            // m_oCurrentInitTextBox
            // 
            this.m_oCurrentInitTextBox.Location = new System.Drawing.Point(124, 13);
            this.m_oCurrentInitTextBox.Name = "m_oCurrentInitTextBox";
            this.m_oCurrentInitTextBox.Size = new System.Drawing.Size(32, 20);
            this.m_oCurrentInitTextBox.TabIndex = 34;
            // 
            // m_oCurrentInitLabel
            // 
            this.m_oCurrentInitLabel.AutoSize = true;
            this.m_oCurrentInitLabel.Location = new System.Drawing.Point(80, 16);
            this.m_oCurrentInitLabel.Name = "m_oCurrentInitLabel";
            this.m_oCurrentInitLabel.Size = new System.Drawing.Size(41, 13);
            this.m_oCurrentInitLabel.TabIndex = 33;
            this.m_oCurrentInitLabel.Text = "Current";
            // 
            // m_oMaxInitLabel
            // 
            this.m_oMaxInitLabel.AutoSize = true;
            this.m_oMaxInitLabel.Location = new System.Drawing.Point(6, 16);
            this.m_oMaxInitLabel.Name = "m_oMaxInitLabel";
            this.m_oMaxInitLabel.Size = new System.Drawing.Size(27, 13);
            this.m_oMaxInitLabel.TabIndex = 29;
            this.m_oMaxInitLabel.Text = "Max";
            // 
            // m_oMaxInitTextBox
            // 
            this.m_oMaxInitTextBox.Enabled = false;
            this.m_oMaxInitTextBox.Location = new System.Drawing.Point(42, 13);
            this.m_oMaxInitTextBox.Name = "m_oMaxInitTextBox";
            this.m_oMaxInitTextBox.Size = new System.Drawing.Size(32, 20);
            this.m_oMaxInitTextBox.TabIndex = 32;
            // 
            // m_oOfficerBox
            // 
            this.m_oOfficerBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oOfficerBox.Controls.Add(this.m_oSeniorOfficerTextBox);
            this.m_oOfficerBox.Location = new System.Drawing.Point(12, 211);
            this.m_oOfficerBox.MaximumSize = new System.Drawing.Size(362, 37);
            this.m_oOfficerBox.MinimumSize = new System.Drawing.Size(362, 37);
            this.m_oOfficerBox.Name = "m_oOfficerBox";
            this.m_oOfficerBox.Size = new System.Drawing.Size(362, 37);
            this.m_oOfficerBox.TabIndex = 35;
            this.m_oOfficerBox.TabStop = false;
            this.m_oOfficerBox.Text = "Senior Officer";
            // 
            // m_oSeniorOfficerTextBox
            // 
            this.m_oSeniorOfficerTextBox.Enabled = false;
            this.m_oSeniorOfficerTextBox.Location = new System.Drawing.Point(6, 14);
            this.m_oSeniorOfficerTextBox.Name = "m_oSeniorOfficerTextBox";
            this.m_oSeniorOfficerTextBox.Size = new System.Drawing.Size(350, 20);
            this.m_oSeniorOfficerTextBox.TabIndex = 32;
            // 
            // m_oOrderTimeDistBox
            // 
            this.m_oOrderTimeDistBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oOrderTimeDistBox.Controls.Add(this.m_oAllOrdersRadioButton);
            this.m_oOrderTimeDistBox.Controls.Add(this.m_oCurrentTDRadioButton);
            this.m_oOrderTimeDistBox.Controls.Add(this.m_oTimeDistTextBox);
            this.m_oOrderTimeDistBox.Location = new System.Drawing.Point(12, 254);
            this.m_oOrderTimeDistBox.MaximumSize = new System.Drawing.Size(362, 60);
            this.m_oOrderTimeDistBox.MinimumSize = new System.Drawing.Size(362, 60);
            this.m_oOrderTimeDistBox.Name = "m_oOrderTimeDistBox";
            this.m_oOrderTimeDistBox.Size = new System.Drawing.Size(362, 60);
            this.m_oOrderTimeDistBox.TabIndex = 36;
            this.m_oOrderTimeDistBox.TabStop = false;
            this.m_oOrderTimeDistBox.Text = "Time And Distance";
            // 
            // m_oAllOrdersRadioButton
            // 
            this.m_oAllOrdersRadioButton.AutoSize = true;
            this.m_oAllOrdersRadioButton.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oAllOrdersRadioButton.Location = new System.Drawing.Point(284, 37);
            this.m_oAllOrdersRadioButton.MaximumSize = new System.Drawing.Size(74, 17);
            this.m_oAllOrdersRadioButton.MinimumSize = new System.Drawing.Size(74, 17);
            this.m_oAllOrdersRadioButton.Name = "m_oAllOrdersRadioButton";
            this.m_oAllOrdersRadioButton.Size = new System.Drawing.Size(74, 17);
            this.m_oAllOrdersRadioButton.TabIndex = 38;
            this.m_oAllOrdersRadioButton.Text = "All Orders";
            this.m_oAllOrdersRadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oCurrentTDRadioButton
            // 
            this.m_oCurrentTDRadioButton.AutoSize = true;
            this.m_oCurrentTDRadioButton.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oCurrentTDRadioButton.Checked = true;
            this.m_oCurrentTDRadioButton.Location = new System.Drawing.Point(284, 14);
            this.m_oCurrentTDRadioButton.MaximumSize = new System.Drawing.Size(74, 17);
            this.m_oCurrentTDRadioButton.MinimumSize = new System.Drawing.Size(74, 17);
            this.m_oCurrentTDRadioButton.Name = "m_oCurrentTDRadioButton";
            this.m_oCurrentTDRadioButton.Size = new System.Drawing.Size(74, 17);
            this.m_oCurrentTDRadioButton.TabIndex = 37;
            this.m_oCurrentTDRadioButton.TabStop = true;
            this.m_oCurrentTDRadioButton.Text = "Current";
            this.m_oCurrentTDRadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oTimeDistTextBox
            // 
            this.m_oTimeDistTextBox.Enabled = false;
            this.m_oTimeDistTextBox.Location = new System.Drawing.Point(6, 23);
            this.m_oTimeDistTextBox.Name = "m_oTimeDistTextBox";
            this.m_oTimeDistTextBox.Size = new System.Drawing.Size(266, 20);
            this.m_oTimeDistTextBox.TabIndex = 32;
            // 
            // m_oTaskGroupTabControl
            // 
            this.m_oTaskGroupTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oTaskGroupOrdersTabPage);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oSpecialOrdersTabPage);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oHistoryTabPage);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oNavalOrgTabPage);
            this.m_oTaskGroupTabControl.Location = new System.Drawing.Point(12, 320);
            this.m_oTaskGroupTabControl.MaximumSize = new System.Drawing.Size(1178, 566);
            this.m_oTaskGroupTabControl.MinimumSize = new System.Drawing.Size(1178, 566);
            this.m_oTaskGroupTabControl.Name = "m_oTaskGroupTabControl";
            this.m_oTaskGroupTabControl.SelectedIndex = 0;
            this.m_oTaskGroupTabControl.Size = new System.Drawing.Size(1178, 566);
            this.m_oTaskGroupTabControl.TabIndex = 39;
            // 
            // m_oTaskGroupOrdersTabPage
            // 
            this.m_oTaskGroupOrdersTabPage.BackColor = System.Drawing.SystemColors.Control;
            this.m_oTaskGroupOrdersTabPage.Controls.Add(this.m_oCargoFightersTroopsGroupBox);
            this.m_oTaskGroupOrdersTabPage.Controls.Add(this.m_oDefaultCondEscortOrdersGroupBox);
            this.m_oTaskGroupOrdersTabPage.Controls.Add(this.m_oOOBGroupBox);
            this.m_oTaskGroupOrdersTabPage.Controls.Add(this.m_oTaskGroupOrdersBox);
            this.m_oTaskGroupOrdersTabPage.Controls.Add(this.m_oCopyOrdersGroupBox);
            this.m_oTaskGroupOrdersTabPage.Controls.Add(this.m_oSystemDisplayOptionsBox);
            this.m_oTaskGroupOrdersTabPage.Location = new System.Drawing.Point(4, 22);
            this.m_oTaskGroupOrdersTabPage.Name = "m_oTaskGroupOrdersTabPage";
            this.m_oTaskGroupOrdersTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.m_oTaskGroupOrdersTabPage.Size = new System.Drawing.Size(1170, 540);
            this.m_oTaskGroupOrdersTabPage.TabIndex = 0;
            this.m_oTaskGroupOrdersTabPage.Text = "Task Group Orders";
            // 
            // m_oCargoFightersTroopsGroupBox
            // 
            this.m_oCargoFightersTroopsGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oCargoFightersTroopsGroupBox.Location = new System.Drawing.Point(819, 398);
            this.m_oCargoFightersTroopsGroupBox.MaximumSize = new System.Drawing.Size(348, 125);
            this.m_oCargoFightersTroopsGroupBox.MinimumSize = new System.Drawing.Size(348, 125);
            this.m_oCargoFightersTroopsGroupBox.Name = "m_oCargoFightersTroopsGroupBox";
            this.m_oCargoFightersTroopsGroupBox.Size = new System.Drawing.Size(348, 125);
            this.m_oCargoFightersTroopsGroupBox.TabIndex = 35;
            this.m_oCargoFightersTroopsGroupBox.TabStop = false;
            this.m_oCargoFightersTroopsGroupBox.Text = "Fighters, Ground Units or Cargo carried by TaskGroup";
            // 
            // m_oDefaultCondEscortOrdersGroupBox
            // 
            this.m_oDefaultCondEscortOrdersGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oDefaultCondEscortOrdersGroupBox.Location = new System.Drawing.Point(819, 262);
            this.m_oDefaultCondEscortOrdersGroupBox.MaximumSize = new System.Drawing.Size(348, 125);
            this.m_oDefaultCondEscortOrdersGroupBox.MinimumSize = new System.Drawing.Size(348, 125);
            this.m_oDefaultCondEscortOrdersGroupBox.Name = "m_oDefaultCondEscortOrdersGroupBox";
            this.m_oDefaultCondEscortOrdersGroupBox.Size = new System.Drawing.Size(348, 125);
            this.m_oDefaultCondEscortOrdersGroupBox.TabIndex = 34;
            this.m_oDefaultCondEscortOrdersGroupBox.TabStop = false;
            this.m_oDefaultCondEscortOrdersGroupBox.Text = "Default, Conditional or Escort Orders";
            // 
            // m_oOOBGroupBox
            // 
            this.m_oOOBGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oOOBGroupBox.Location = new System.Drawing.Point(819, 6);
            this.m_oOOBGroupBox.MaximumSize = new System.Drawing.Size(348, 250);
            this.m_oOOBGroupBox.MinimumSize = new System.Drawing.Size(348, 250);
            this.m_oOOBGroupBox.Name = "m_oOOBGroupBox";
            this.m_oOOBGroupBox.Size = new System.Drawing.Size(348, 250);
            this.m_oOOBGroupBox.TabIndex = 33;
            this.m_oOOBGroupBox.TabStop = false;
            this.m_oOOBGroupBox.Text = "Order of Battle";
            // 
            // m_oTaskGroupOrdersBox
            // 
            this.m_oTaskGroupOrdersBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oRepeatOrdersTextBox);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oRepeatOrderButton);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oCycleMovesCheckBox);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oOrderDelayTextBox);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oOrderDelayLabel);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oAutoRouteCheckBox);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oLoadAmtTextBox);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oOrbitalDistanceTextBox);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oLoadLimitLabel);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oOrbitalDistanceLabel);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oRemoveAllButton);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oRemoveButton);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oAddMoveButton);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oPlottedMoveLabel);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oActionsAvailableLabel);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oSystemLocationsLabel);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oPlottedMoveListBox);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oActionsAvailableListBox);
            this.m_oTaskGroupOrdersBox.Controls.Add(this.m_oSystemLocationsListBox);
            this.m_oTaskGroupOrdersBox.Location = new System.Drawing.Point(2, 80);
            this.m_oTaskGroupOrdersBox.MaximumSize = new System.Drawing.Size(811, 443);
            this.m_oTaskGroupOrdersBox.MinimumSize = new System.Drawing.Size(811, 443);
            this.m_oTaskGroupOrdersBox.Name = "m_oTaskGroupOrdersBox";
            this.m_oTaskGroupOrdersBox.Size = new System.Drawing.Size(811, 443);
            this.m_oTaskGroupOrdersBox.TabIndex = 33;
            this.m_oTaskGroupOrdersBox.TabStop = false;
            // 
            // m_oRepeatOrdersTextBox
            // 
            this.m_oRepeatOrdersTextBox.Location = new System.Drawing.Point(769, 399);
            this.m_oRepeatOrdersTextBox.Name = "m_oRepeatOrdersTextBox";
            this.m_oRepeatOrdersTextBox.Size = new System.Drawing.Size(27, 20);
            this.m_oRepeatOrdersTextBox.TabIndex = 44;
            this.m_oRepeatOrdersTextBox.Text = "0";
            this.m_oRepeatOrdersTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oRepeatOrderButton
            // 
            this.m_oRepeatOrderButton.Location = new System.Drawing.Point(694, 397);
            this.m_oRepeatOrderButton.Name = "m_oRepeatOrderButton";
            this.m_oRepeatOrderButton.Size = new System.Drawing.Size(61, 26);
            this.m_oRepeatOrderButton.TabIndex = 43;
            this.m_oRepeatOrderButton.Text = "Repeat";
            this.m_oRepeatOrderButton.UseVisualStyleBackColor = true;
            // 
            // m_oCycleMovesCheckBox
            // 
            this.m_oCycleMovesCheckBox.AutoSize = true;
            this.m_oCycleMovesCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oCycleMovesCheckBox.Location = new System.Drawing.Point(694, 367);
            this.m_oCycleMovesCheckBox.MaximumSize = new System.Drawing.Size(102, 17);
            this.m_oCycleMovesCheckBox.MinimumSize = new System.Drawing.Size(102, 17);
            this.m_oCycleMovesCheckBox.Name = "m_oCycleMovesCheckBox";
            this.m_oCycleMovesCheckBox.Size = new System.Drawing.Size(102, 17);
            this.m_oCycleMovesCheckBox.TabIndex = 21;
            this.m_oCycleMovesCheckBox.Text = "Cycle Moves";
            this.m_oCycleMovesCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oOrderDelayTextBox
            // 
            this.m_oOrderDelayTextBox.Location = new System.Drawing.Point(601, 400);
            this.m_oOrderDelayTextBox.Name = "m_oOrderDelayTextBox";
            this.m_oOrderDelayTextBox.Size = new System.Drawing.Size(77, 20);
            this.m_oOrderDelayTextBox.TabIndex = 42;
            this.m_oOrderDelayTextBox.Text = "0";
            this.m_oOrderDelayTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oOrderDelayLabel
            // 
            this.m_oOrderDelayLabel.AutoSize = true;
            this.m_oOrderDelayLabel.Location = new System.Drawing.Point(532, 403);
            this.m_oOrderDelayLabel.Name = "m_oOrderDelayLabel";
            this.m_oOrderDelayLabel.Size = new System.Drawing.Size(63, 13);
            this.m_oOrderDelayLabel.TabIndex = 41;
            this.m_oOrderDelayLabel.Text = "Order Delay";
            // 
            // m_oAutoRouteCheckBox
            // 
            this.m_oAutoRouteCheckBox.AutoSize = true;
            this.m_oAutoRouteCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oAutoRouteCheckBox.Location = new System.Drawing.Point(523, 367);
            this.m_oAutoRouteCheckBox.MaximumSize = new System.Drawing.Size(155, 17);
            this.m_oAutoRouteCheckBox.MinimumSize = new System.Drawing.Size(155, 17);
            this.m_oAutoRouteCheckBox.Name = "m_oAutoRouteCheckBox";
            this.m_oAutoRouteCheckBox.Size = new System.Drawing.Size(155, 17);
            this.m_oAutoRouteCheckBox.TabIndex = 21;
            this.m_oAutoRouteCheckBox.Text = "No auto-route jump check";
            this.m_oAutoRouteCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oLoadAmtTextBox
            // 
            this.m_oLoadAmtTextBox.Enabled = false;
            this.m_oLoadAmtTextBox.Location = new System.Drawing.Point(441, 403);
            this.m_oLoadAmtTextBox.Name = "m_oLoadAmtTextBox";
            this.m_oLoadAmtTextBox.Size = new System.Drawing.Size(56, 20);
            this.m_oLoadAmtTextBox.TabIndex = 40;
            this.m_oLoadAmtTextBox.Text = "0";
            this.m_oLoadAmtTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oOrbitalDistanceTextBox
            // 
            this.m_oOrbitalDistanceTextBox.Location = new System.Drawing.Point(441, 374);
            this.m_oOrbitalDistanceTextBox.Name = "m_oOrbitalDistanceTextBox";
            this.m_oOrbitalDistanceTextBox.Size = new System.Drawing.Size(56, 20);
            this.m_oOrbitalDistanceTextBox.TabIndex = 35;
            this.m_oOrbitalDistanceTextBox.Text = "0";
            this.m_oOrbitalDistanceTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oLoadLimitLabel
            // 
            this.m_oLoadLimitLabel.AutoSize = true;
            this.m_oLoadLimitLabel.Enabled = false;
            this.m_oLoadLimitLabel.Location = new System.Drawing.Point(277, 406);
            this.m_oLoadLimitLabel.Name = "m_oLoadLimitLabel";
            this.m_oLoadLimitLabel.Size = new System.Drawing.Size(129, 13);
            this.m_oLoadLimitLabel.TabIndex = 39;
            this.m_oLoadLimitLabel.Text = "Maximum Amount to Load";
            // 
            // m_oOrbitalDistanceLabel
            // 
            this.m_oOrbitalDistanceLabel.AutoSize = true;
            this.m_oOrbitalDistanceLabel.Location = new System.Drawing.Point(277, 377);
            this.m_oOrbitalDistanceLabel.Name = "m_oOrbitalDistanceLabel";
            this.m_oOrbitalDistanceLabel.Size = new System.Drawing.Size(114, 13);
            this.m_oOrbitalDistanceLabel.TabIndex = 38;
            this.m_oOrbitalDistanceLabel.Text = "Orbital Distance (k km)";
            // 
            // m_oRemoveAllButton
            // 
            this.m_oRemoveAllButton.Location = new System.Drawing.Point(173, 406);
            this.m_oRemoveAllButton.Name = "m_oRemoveAllButton";
            this.m_oRemoveAllButton.Size = new System.Drawing.Size(82, 31);
            this.m_oRemoveAllButton.TabIndex = 37;
            this.m_oRemoveAllButton.Text = "Remove All";
            this.m_oRemoveAllButton.UseVisualStyleBackColor = true;
            // 
            // m_oRemoveButton
            // 
            this.m_oRemoveButton.Location = new System.Drawing.Point(85, 406);
            this.m_oRemoveButton.Name = "m_oRemoveButton";
            this.m_oRemoveButton.Size = new System.Drawing.Size(82, 31);
            this.m_oRemoveButton.TabIndex = 36;
            this.m_oRemoveButton.Text = "Remove";
            this.m_oRemoveButton.UseVisualStyleBackColor = true;
            // 
            // m_oAddMoveButton
            // 
            this.m_oAddMoveButton.Location = new System.Drawing.Point(1, 406);
            this.m_oAddMoveButton.Name = "m_oAddMoveButton";
            this.m_oAddMoveButton.Size = new System.Drawing.Size(78, 31);
            this.m_oAddMoveButton.TabIndex = 35;
            this.m_oAddMoveButton.Text = "Add Move";
            this.m_oAddMoveButton.UseVisualStyleBackColor = true;
            // 
            // m_oPlottedMoveLabel
            // 
            this.m_oPlottedMoveLabel.AutoSize = true;
            this.m_oPlottedMoveLabel.Location = new System.Drawing.Point(511, 14);
            this.m_oPlottedMoveLabel.Name = "m_oPlottedMoveLabel";
            this.m_oPlottedMoveLabel.Size = new System.Drawing.Size(70, 13);
            this.m_oPlottedMoveLabel.TabIndex = 31;
            this.m_oPlottedMoveLabel.Text = "Plotted Move";
            // 
            // m_oActionsAvailableLabel
            // 
            this.m_oActionsAvailableLabel.AutoSize = true;
            this.m_oActionsAvailableLabel.Location = new System.Drawing.Point(268, 14);
            this.m_oActionsAvailableLabel.Name = "m_oActionsAvailableLabel";
            this.m_oActionsAvailableLabel.Size = new System.Drawing.Size(88, 13);
            this.m_oActionsAvailableLabel.TabIndex = 30;
            this.m_oActionsAvailableLabel.Text = "Actions Available";
            // 
            // m_oSystemLocationsLabel
            // 
            this.m_oSystemLocationsLabel.AutoSize = true;
            this.m_oSystemLocationsLabel.Location = new System.Drawing.Point(6, 14);
            this.m_oSystemLocationsLabel.Name = "m_oSystemLocationsLabel";
            this.m_oSystemLocationsLabel.Size = new System.Drawing.Size(136, 13);
            this.m_oSystemLocationsLabel.TabIndex = 29;
            this.m_oSystemLocationsLabel.Text = "System Locations Available";
            // 
            // m_oPlottedMoveListBox
            // 
            this.m_oPlottedMoveListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oPlottedMoveListBox.FormattingEnabled = true;
            this.m_oPlottedMoveListBox.Location = new System.Drawing.Point(514, 32);
            this.m_oPlottedMoveListBox.Name = "m_oPlottedMoveListBox";
            this.m_oPlottedMoveListBox.Size = new System.Drawing.Size(291, 329);
            this.m_oPlottedMoveListBox.TabIndex = 3;
            // 
            // m_oActionsAvailableListBox
            // 
            this.m_oActionsAvailableListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oActionsAvailableListBox.FormattingEnabled = true;
            this.m_oActionsAvailableListBox.Location = new System.Drawing.Point(266, 32);
            this.m_oActionsAvailableListBox.Name = "m_oActionsAvailableListBox";
            this.m_oActionsAvailableListBox.Size = new System.Drawing.Size(242, 329);
            this.m_oActionsAvailableListBox.TabIndex = 2;
            // 
            // m_oSystemLocationsListBox
            // 
            this.m_oSystemLocationsListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oSystemLocationsListBox.FormattingEnabled = true;
            this.m_oSystemLocationsListBox.Location = new System.Drawing.Point(4, 32);
            this.m_oSystemLocationsListBox.Name = "m_oSystemLocationsListBox";
            this.m_oSystemLocationsListBox.Size = new System.Drawing.Size(251, 368);
            this.m_oSystemLocationsListBox.TabIndex = 1;
            // 
            // m_oCopyOrdersGroupBox
            // 
            this.m_oCopyOrdersGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oCopyOrdersGroupBox.Controls.Add(this.m_oIncCondOrdersCheckBox);
            this.m_oCopyOrdersGroupBox.Controls.Add(this.m_oIncDefaultCheckBox);
            this.m_oCopyOrdersGroupBox.Controls.Add(this.checkBox1);
            this.m_oCopyOrdersGroupBox.Controls.Add(this.m_oCopyOrdersButton);
            this.m_oCopyOrdersGroupBox.Location = new System.Drawing.Point(594, 6);
            this.m_oCopyOrdersGroupBox.MaximumSize = new System.Drawing.Size(219, 68);
            this.m_oCopyOrdersGroupBox.MinimumSize = new System.Drawing.Size(219, 68);
            this.m_oCopyOrdersGroupBox.Name = "m_oCopyOrdersGroupBox";
            this.m_oCopyOrdersGroupBox.Size = new System.Drawing.Size(219, 68);
            this.m_oCopyOrdersGroupBox.TabIndex = 33;
            this.m_oCopyOrdersGroupBox.TabStop = false;
            this.m_oCopyOrdersGroupBox.Text = "Copy Orders to Subordinate Formations";
            // 
            // m_oIncCondOrdersCheckBox
            // 
            this.m_oIncCondOrdersCheckBox.AutoSize = true;
            this.m_oIncCondOrdersCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oIncCondOrdersCheckBox.Location = new System.Drawing.Point(132, 40);
            this.m_oIncCondOrdersCheckBox.MaximumSize = new System.Drawing.Size(82, 17);
            this.m_oIncCondOrdersCheckBox.MinimumSize = new System.Drawing.Size(82, 17);
            this.m_oIncCondOrdersCheckBox.Name = "m_oIncCondOrdersCheckBox";
            this.m_oIncCondOrdersCheckBox.Size = new System.Drawing.Size(82, 17);
            this.m_oIncCondOrdersCheckBox.TabIndex = 34;
            this.m_oIncCondOrdersCheckBox.Text = "Inc. Cond.";
            this.m_oIncCondOrdersCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oIncDefaultCheckBox
            // 
            this.m_oIncDefaultCheckBox.AutoSize = true;
            this.m_oIncDefaultCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oIncDefaultCheckBox.Location = new System.Drawing.Point(132, 19);
            this.m_oIncDefaultCheckBox.MaximumSize = new System.Drawing.Size(82, 17);
            this.m_oIncDefaultCheckBox.MinimumSize = new System.Drawing.Size(82, 17);
            this.m_oIncDefaultCheckBox.Name = "m_oIncDefaultCheckBox";
            this.m_oIncDefaultCheckBox.Size = new System.Drawing.Size(82, 17);
            this.m_oIncDefaultCheckBox.TabIndex = 33;
            this.m_oIncDefaultCheckBox.Text = "Inc. Default";
            this.m_oIncDefaultCheckBox.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.checkBox1.Location = new System.Drawing.Point(62, 22);
            this.checkBox1.MaximumSize = new System.Drawing.Size(64, 35);
            this.checkBox1.MinimumSize = new System.Drawing.Size(64, 35);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(64, 35);
            this.checkBox1.TabIndex = 21;
            this.checkBox1.Text = "Match Speeds";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // m_oCopyOrdersButton
            // 
            this.m_oCopyOrdersButton.Location = new System.Drawing.Point(6, 25);
            this.m_oCopyOrdersButton.Name = "m_oCopyOrdersButton";
            this.m_oCopyOrdersButton.Size = new System.Drawing.Size(50, 27);
            this.m_oCopyOrdersButton.TabIndex = 32;
            this.m_oCopyOrdersButton.Text = "Copy";
            this.m_oCopyOrdersButton.UseVisualStyleBackColor = true;
            // 
            // m_oSystemDisplayOptionsBox
            // 
            this.m_oSystemDisplayOptionsBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oSystemDisplayOptionsBox.Controls.Add(this.m_oShowAllPopsCheckBox);
            this.m_oSystemDisplayOptionsBox.Controls.Add(this.m_oOrderFilteringCheckBox);
            this.m_oSystemDisplayOptionsBox.Controls.Add(this.m_oExcludeSurveyedCheckBox);
            this.m_oSystemDisplayOptionsBox.Controls.Add(this.m_oSurveyLocationsCheckBox);
            this.m_oSystemDisplayOptionsBox.Controls.Add(this.m_oWrecksCheckBox);
            this.m_oSystemDisplayOptionsBox.Controls.Add(this.m_oLifePodsCheckBox);
            this.m_oSystemDisplayOptionsBox.Controls.Add(this.m_oCometsCheckBox);
            this.m_oSystemDisplayOptionsBox.Controls.Add(this.m_oContactsCheckBox);
            this.m_oSystemDisplayOptionsBox.Controls.Add(this.m_oWaypointCheckBox);
            this.m_oSystemDisplayOptionsBox.Controls.Add(this.m_oTaskGroupsCheckBox);
            this.m_oSystemDisplayOptionsBox.Controls.Add(this.m_oAsteroidsCheckBox);
            this.m_oSystemDisplayOptionsBox.Controls.Add(this.m_oMoonsCheckBox);
            this.m_oSystemDisplayOptionsBox.Location = new System.Drawing.Point(3, 6);
            this.m_oSystemDisplayOptionsBox.MaximumSize = new System.Drawing.Size(585, 68);
            this.m_oSystemDisplayOptionsBox.MinimumSize = new System.Drawing.Size(585, 68);
            this.m_oSystemDisplayOptionsBox.Name = "m_oSystemDisplayOptionsBox";
            this.m_oSystemDisplayOptionsBox.Size = new System.Drawing.Size(585, 68);
            this.m_oSystemDisplayOptionsBox.TabIndex = 33;
            this.m_oSystemDisplayOptionsBox.TabStop = false;
            this.m_oSystemDisplayOptionsBox.Text = "System Location Display Options";
            // 
            // m_oShowAllPopsCheckBox
            // 
            this.m_oShowAllPopsCheckBox.AutoSize = true;
            this.m_oShowAllPopsCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oShowAllPopsCheckBox.Location = new System.Drawing.Point(456, 42);
            this.m_oShowAllPopsCheckBox.MaximumSize = new System.Drawing.Size(122, 17);
            this.m_oShowAllPopsCheckBox.MinimumSize = new System.Drawing.Size(122, 17);
            this.m_oShowAllPopsCheckBox.Name = "m_oShowAllPopsCheckBox";
            this.m_oShowAllPopsCheckBox.Size = new System.Drawing.Size(122, 17);
            this.m_oShowAllPopsCheckBox.TabIndex = 20;
            this.m_oShowAllPopsCheckBox.Text = "Show All Pops";
            this.m_oShowAllPopsCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oOrderFilteringCheckBox
            // 
            this.m_oOrderFilteringCheckBox.AutoSize = true;
            this.m_oOrderFilteringCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oOrderFilteringCheckBox.Checked = true;
            this.m_oOrderFilteringCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.m_oOrderFilteringCheckBox.Location = new System.Drawing.Point(456, 19);
            this.m_oOrderFilteringCheckBox.MaximumSize = new System.Drawing.Size(122, 17);
            this.m_oOrderFilteringCheckBox.MinimumSize = new System.Drawing.Size(122, 17);
            this.m_oOrderFilteringCheckBox.Name = "m_oOrderFilteringCheckBox";
            this.m_oOrderFilteringCheckBox.Size = new System.Drawing.Size(122, 17);
            this.m_oOrderFilteringCheckBox.TabIndex = 19;
            this.m_oOrderFilteringCheckBox.Text = "Order Filtering On";
            this.m_oOrderFilteringCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oExcludeSurveyedCheckBox
            // 
            this.m_oExcludeSurveyedCheckBox.AutoSize = true;
            this.m_oExcludeSurveyedCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oExcludeSurveyedCheckBox.Location = new System.Drawing.Point(328, 42);
            this.m_oExcludeSurveyedCheckBox.MaximumSize = new System.Drawing.Size(122, 17);
            this.m_oExcludeSurveyedCheckBox.MinimumSize = new System.Drawing.Size(122, 17);
            this.m_oExcludeSurveyedCheckBox.Name = "m_oExcludeSurveyedCheckBox";
            this.m_oExcludeSurveyedCheckBox.Size = new System.Drawing.Size(122, 17);
            this.m_oExcludeSurveyedCheckBox.TabIndex = 18;
            this.m_oExcludeSurveyedCheckBox.Text = "Exclude Surveyed";
            this.m_oExcludeSurveyedCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oSurveyLocationsCheckBox
            // 
            this.m_oSurveyLocationsCheckBox.AutoSize = true;
            this.m_oSurveyLocationsCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oSurveyLocationsCheckBox.Location = new System.Drawing.Point(328, 19);
            this.m_oSurveyLocationsCheckBox.MaximumSize = new System.Drawing.Size(122, 17);
            this.m_oSurveyLocationsCheckBox.MinimumSize = new System.Drawing.Size(122, 17);
            this.m_oSurveyLocationsCheckBox.Name = "m_oSurveyLocationsCheckBox";
            this.m_oSurveyLocationsCheckBox.Size = new System.Drawing.Size(122, 17);
            this.m_oSurveyLocationsCheckBox.TabIndex = 17;
            this.m_oSurveyLocationsCheckBox.Text = "Survey Locations";
            this.m_oSurveyLocationsCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oWrecksCheckBox
            // 
            this.m_oWrecksCheckBox.AutoSize = true;
            this.m_oWrecksCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oWrecksCheckBox.Location = new System.Drawing.Point(250, 42);
            this.m_oWrecksCheckBox.MaximumSize = new System.Drawing.Size(72, 17);
            this.m_oWrecksCheckBox.MinimumSize = new System.Drawing.Size(72, 17);
            this.m_oWrecksCheckBox.Name = "m_oWrecksCheckBox";
            this.m_oWrecksCheckBox.Size = new System.Drawing.Size(72, 17);
            this.m_oWrecksCheckBox.TabIndex = 16;
            this.m_oWrecksCheckBox.Text = "Wrecks";
            this.m_oWrecksCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oLifePodsCheckBox
            // 
            this.m_oLifePodsCheckBox.AutoSize = true;
            this.m_oLifePodsCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oLifePodsCheckBox.Location = new System.Drawing.Point(250, 19);
            this.m_oLifePodsCheckBox.MaximumSize = new System.Drawing.Size(72, 17);
            this.m_oLifePodsCheckBox.MinimumSize = new System.Drawing.Size(72, 17);
            this.m_oLifePodsCheckBox.Name = "m_oLifePodsCheckBox";
            this.m_oLifePodsCheckBox.Size = new System.Drawing.Size(72, 17);
            this.m_oLifePodsCheckBox.TabIndex = 15;
            this.m_oLifePodsCheckBox.Text = "Lifepods";
            this.m_oLifePodsCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oCometsCheckBox
            // 
            this.m_oCometsCheckBox.AutoSize = true;
            this.m_oCometsCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oCometsCheckBox.Location = new System.Drawing.Point(172, 42);
            this.m_oCometsCheckBox.MaximumSize = new System.Drawing.Size(72, 17);
            this.m_oCometsCheckBox.MinimumSize = new System.Drawing.Size(72, 17);
            this.m_oCometsCheckBox.Name = "m_oCometsCheckBox";
            this.m_oCometsCheckBox.Size = new System.Drawing.Size(72, 17);
            this.m_oCometsCheckBox.TabIndex = 14;
            this.m_oCometsCheckBox.Text = "Comets";
            this.m_oCometsCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oContactsCheckBox
            // 
            this.m_oContactsCheckBox.AutoSize = true;
            this.m_oContactsCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oContactsCheckBox.Location = new System.Drawing.Point(172, 19);
            this.m_oContactsCheckBox.MaximumSize = new System.Drawing.Size(72, 17);
            this.m_oContactsCheckBox.MinimumSize = new System.Drawing.Size(72, 17);
            this.m_oContactsCheckBox.Name = "m_oContactsCheckBox";
            this.m_oContactsCheckBox.Size = new System.Drawing.Size(72, 17);
            this.m_oContactsCheckBox.TabIndex = 13;
            this.m_oContactsCheckBox.Text = "Contacts";
            this.m_oContactsCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oWaypointCheckBox
            // 
            this.m_oWaypointCheckBox.AutoSize = true;
            this.m_oWaypointCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oWaypointCheckBox.Location = new System.Drawing.Point(84, 42);
            this.m_oWaypointCheckBox.MaximumSize = new System.Drawing.Size(82, 17);
            this.m_oWaypointCheckBox.MinimumSize = new System.Drawing.Size(82, 17);
            this.m_oWaypointCheckBox.Name = "m_oWaypointCheckBox";
            this.m_oWaypointCheckBox.Size = new System.Drawing.Size(82, 17);
            this.m_oWaypointCheckBox.TabIndex = 12;
            this.m_oWaypointCheckBox.Text = "Waypoints";
            this.m_oWaypointCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oTaskGroupsCheckBox
            // 
            this.m_oTaskGroupsCheckBox.AutoSize = true;
            this.m_oTaskGroupsCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oTaskGroupsCheckBox.Location = new System.Drawing.Point(84, 19);
            this.m_oTaskGroupsCheckBox.MaximumSize = new System.Drawing.Size(82, 17);
            this.m_oTaskGroupsCheckBox.MinimumSize = new System.Drawing.Size(82, 17);
            this.m_oTaskGroupsCheckBox.Name = "m_oTaskGroupsCheckBox";
            this.m_oTaskGroupsCheckBox.Size = new System.Drawing.Size(82, 17);
            this.m_oTaskGroupsCheckBox.TabIndex = 11;
            this.m_oTaskGroupsCheckBox.Text = "TaskGroups";
            this.m_oTaskGroupsCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oAsteroidsCheckBox
            // 
            this.m_oAsteroidsCheckBox.AutoSize = true;
            this.m_oAsteroidsCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oAsteroidsCheckBox.Location = new System.Drawing.Point(6, 42);
            this.m_oAsteroidsCheckBox.MaximumSize = new System.Drawing.Size(72, 17);
            this.m_oAsteroidsCheckBox.MinimumSize = new System.Drawing.Size(72, 17);
            this.m_oAsteroidsCheckBox.Name = "m_oAsteroidsCheckBox";
            this.m_oAsteroidsCheckBox.Size = new System.Drawing.Size(72, 17);
            this.m_oAsteroidsCheckBox.TabIndex = 10;
            this.m_oAsteroidsCheckBox.Text = "Asteroids";
            this.m_oAsteroidsCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oMoonsCheckBox
            // 
            this.m_oMoonsCheckBox.AutoSize = true;
            this.m_oMoonsCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oMoonsCheckBox.Location = new System.Drawing.Point(6, 19);
            this.m_oMoonsCheckBox.MaximumSize = new System.Drawing.Size(72, 17);
            this.m_oMoonsCheckBox.MinimumSize = new System.Drawing.Size(72, 17);
            this.m_oMoonsCheckBox.Name = "m_oMoonsCheckBox";
            this.m_oMoonsCheckBox.Size = new System.Drawing.Size(72, 17);
            this.m_oMoonsCheckBox.TabIndex = 9;
            this.m_oMoonsCheckBox.Text = "Moons";
            this.m_oMoonsCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oSpecialOrdersTabPage
            // 
            this.m_oSpecialOrdersTabPage.Location = new System.Drawing.Point(4, 22);
            this.m_oSpecialOrdersTabPage.Name = "m_oSpecialOrdersTabPage";
            this.m_oSpecialOrdersTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.m_oSpecialOrdersTabPage.Size = new System.Drawing.Size(1170, 540);
            this.m_oSpecialOrdersTabPage.TabIndex = 1;
            this.m_oSpecialOrdersTabPage.Text = "Special Orders / Organization";
            this.m_oSpecialOrdersTabPage.UseVisualStyleBackColor = true;
            // 
            // m_oHistoryTabPage
            // 
            this.m_oHistoryTabPage.Location = new System.Drawing.Point(4, 22);
            this.m_oHistoryTabPage.Name = "m_oHistoryTabPage";
            this.m_oHistoryTabPage.Size = new System.Drawing.Size(1170, 540);
            this.m_oHistoryTabPage.TabIndex = 2;
            this.m_oHistoryTabPage.Text = "History / Officers / Misc";
            this.m_oHistoryTabPage.UseVisualStyleBackColor = true;
            // 
            // m_oNavalOrgTabPage
            // 
            this.m_oNavalOrgTabPage.Location = new System.Drawing.Point(4, 22);
            this.m_oNavalOrgTabPage.Name = "m_oNavalOrgTabPage";
            this.m_oNavalOrgTabPage.Size = new System.Drawing.Size(1170, 540);
            this.m_oNavalOrgTabPage.TabIndex = 3;
            this.m_oNavalOrgTabPage.Text = "Naval Organization";
            this.m_oNavalOrgTabPage.UseVisualStyleBackColor = true;
            // 
            // m_oButtonBox
            // 
            this.m_oButtonBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oButtonBox.Controls.Add(this.m_oAssembleButton);
            this.m_oButtonBox.Controls.Add(this.m_oDetachButton);
            this.m_oButtonBox.Controls.Add(this.m_oEscortButton);
            this.m_oButtonBox.Controls.Add(this.m_oSaveEscortsButton);
            this.m_oButtonBox.Controls.Add(this.m_oMissileLaunchButton);
            this.m_oButtonBox.Controls.Add(this.m_oReloadParaButton);
            this.m_oButtonBox.Controls.Add(this.m_oHyperOnButton);
            this.m_oButtonBox.Controls.Add(this.m_oHyperOffButton);
            this.m_oButtonBox.Controls.Add(this.m_oShieldsOnButton);
            this.m_oButtonBox.Controls.Add(this.m_oShieldsOffButton);
            this.m_oButtonBox.Controls.Add(this.m_oNoDefaultButton);
            this.m_oButtonBox.Controls.Add(this.m_oCloseButton);
            this.m_oButtonBox.Controls.Add(this.m_oDeployEscortsButton);
            this.m_oButtonBox.Controls.Add(this.m_oRecallEscortsButton);
            this.m_oButtonBox.Controls.Add(this.m_oEqualizeFuelButton);
            this.m_oButtonBox.Controls.Add(this.m_oEqualizeMaintButton);
            this.m_oButtonBox.Controls.Add(this.m_oNoConditionsButton);
            this.m_oButtonBox.Controls.Add(this.m_oRecoverParaButton);
            this.m_oButtonBox.Controls.Add(this.m_oLaunchParaButton);
            this.m_oButtonBox.Controls.Add(this.m_oDeleteTGButton);
            this.m_oButtonBox.Controls.Add(this.m_oOOBButton);
            this.m_oButtonBox.Controls.Add(this.m_oRenameTGButton);
            this.m_oButtonBox.Controls.Add(this.m_oAddColonyButton);
            this.m_oButtonBox.Controls.Add(this.m_oSystemMapButton);
            this.m_oButtonBox.Controls.Add(this.m_oNewTGButton);
            this.m_oButtonBox.Location = new System.Drawing.Point(12, 888);
            this.m_oButtonBox.MaximumSize = new System.Drawing.Size(1170, 90);
            this.m_oButtonBox.MinimumSize = new System.Drawing.Size(1170, 90);
            this.m_oButtonBox.Name = "m_oButtonBox";
            this.m_oButtonBox.Size = new System.Drawing.Size(1170, 90);
            this.m_oButtonBox.TabIndex = 33;
            this.m_oButtonBox.TabStop = false;
            // 
            // m_oAssembleButton
            // 
            this.m_oAssembleButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oAssembleButton.Location = new System.Drawing.Point(1078, 16);
            this.m_oAssembleButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oAssembleButton.Name = "m_oAssembleButton";
            this.m_oAssembleButton.Size = new System.Drawing.Size(88, 31);
            this.m_oAssembleButton.TabIndex = 78;
            this.m_oAssembleButton.Text = "Assemble";
            this.m_oAssembleButton.UseVisualStyleBackColor = true;
            // 
            // m_oDetachButton
            // 
            this.m_oDetachButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oDetachButton.Location = new System.Drawing.Point(988, 16);
            this.m_oDetachButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oDetachButton.Name = "m_oDetachButton";
            this.m_oDetachButton.Size = new System.Drawing.Size(88, 31);
            this.m_oDetachButton.TabIndex = 77;
            this.m_oDetachButton.Text = "Detach";
            this.m_oDetachButton.UseVisualStyleBackColor = true;
            // 
            // m_oEscortButton
            // 
            this.m_oEscortButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oEscortButton.Location = new System.Drawing.Point(898, 16);
            this.m_oEscortButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oEscortButton.Name = "m_oEscortButton";
            this.m_oEscortButton.Size = new System.Drawing.Size(88, 31);
            this.m_oEscortButton.TabIndex = 76;
            this.m_oEscortButton.Text = "Escort";
            this.m_oEscortButton.UseVisualStyleBackColor = true;
            // 
            // m_oSaveEscortsButton
            // 
            this.m_oSaveEscortsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oSaveEscortsButton.Location = new System.Drawing.Point(628, 16);
            this.m_oSaveEscortsButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oSaveEscortsButton.Name = "m_oSaveEscortsButton";
            this.m_oSaveEscortsButton.Size = new System.Drawing.Size(88, 31);
            this.m_oSaveEscortsButton.TabIndex = 75;
            this.m_oSaveEscortsButton.Text = "Save Escorts";
            this.m_oSaveEscortsButton.UseVisualStyleBackColor = true;
            // 
            // m_oMissileLaunchButton
            // 
            this.m_oMissileLaunchButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oMissileLaunchButton.Location = new System.Drawing.Point(358, 16);
            this.m_oMissileLaunchButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oMissileLaunchButton.Name = "m_oMissileLaunchButton";
            this.m_oMissileLaunchButton.Size = new System.Drawing.Size(88, 31);
            this.m_oMissileLaunchButton.TabIndex = 74;
            this.m_oMissileLaunchButton.Text = "Msl Launch";
            this.m_oMissileLaunchButton.UseVisualStyleBackColor = true;
            // 
            // m_oReloadParaButton
            // 
            this.m_oReloadParaButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oReloadParaButton.Location = new System.Drawing.Point(448, 53);
            this.m_oReloadParaButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oReloadParaButton.Name = "m_oReloadParaButton";
            this.m_oReloadParaButton.Size = new System.Drawing.Size(88, 31);
            this.m_oReloadParaButton.TabIndex = 73;
            this.m_oReloadParaButton.Text = "Reload Para";
            this.m_oReloadParaButton.UseVisualStyleBackColor = true;
            // 
            // m_oHyperOnButton
            // 
            this.m_oHyperOnButton.Enabled = false;
            this.m_oHyperOnButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oHyperOnButton.Location = new System.Drawing.Point(538, 53);
            this.m_oHyperOnButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oHyperOnButton.Name = "m_oHyperOnButton";
            this.m_oHyperOnButton.Size = new System.Drawing.Size(88, 31);
            this.m_oHyperOnButton.TabIndex = 72;
            this.m_oHyperOnButton.Text = "Hyper On";
            this.m_oHyperOnButton.UseVisualStyleBackColor = true;
            // 
            // m_oHyperOffButton
            // 
            this.m_oHyperOffButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oHyperOffButton.Location = new System.Drawing.Point(628, 53);
            this.m_oHyperOffButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oHyperOffButton.Name = "m_oHyperOffButton";
            this.m_oHyperOffButton.Size = new System.Drawing.Size(88, 31);
            this.m_oHyperOffButton.TabIndex = 71;
            this.m_oHyperOffButton.Text = "Hyper Off";
            this.m_oHyperOffButton.UseVisualStyleBackColor = true;
            // 
            // m_oShieldsOnButton
            // 
            this.m_oShieldsOnButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oShieldsOnButton.Location = new System.Drawing.Point(718, 53);
            this.m_oShieldsOnButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oShieldsOnButton.Name = "m_oShieldsOnButton";
            this.m_oShieldsOnButton.Size = new System.Drawing.Size(88, 31);
            this.m_oShieldsOnButton.TabIndex = 70;
            this.m_oShieldsOnButton.Text = "Shields On";
            this.m_oShieldsOnButton.UseVisualStyleBackColor = true;
            // 
            // m_oShieldsOffButton
            // 
            this.m_oShieldsOffButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oShieldsOffButton.Location = new System.Drawing.Point(808, 53);
            this.m_oShieldsOffButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oShieldsOffButton.Name = "m_oShieldsOffButton";
            this.m_oShieldsOffButton.Size = new System.Drawing.Size(88, 31);
            this.m_oShieldsOffButton.TabIndex = 69;
            this.m_oShieldsOffButton.Text = "Shields Off";
            this.m_oShieldsOffButton.UseVisualStyleBackColor = true;
            // 
            // m_oNoDefaultButton
            // 
            this.m_oNoDefaultButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oNoDefaultButton.Location = new System.Drawing.Point(898, 53);
            this.m_oNoDefaultButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oNoDefaultButton.Name = "m_oNoDefaultButton";
            this.m_oNoDefaultButton.Size = new System.Drawing.Size(88, 31);
            this.m_oNoDefaultButton.TabIndex = 68;
            this.m_oNoDefaultButton.Text = "No Default";
            this.m_oNoDefaultButton.UseVisualStyleBackColor = true;
            // 
            // m_oCloseButton
            // 
            this.m_oCloseButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oCloseButton.Location = new System.Drawing.Point(1078, 53);
            this.m_oCloseButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oCloseButton.Name = "m_oCloseButton";
            this.m_oCloseButton.Size = new System.Drawing.Size(88, 31);
            this.m_oCloseButton.TabIndex = 67;
            this.m_oCloseButton.Text = "Close";
            this.m_oCloseButton.UseVisualStyleBackColor = true;
            // 
            // m_oDeployEscortsButton
            // 
            this.m_oDeployEscortsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oDeployEscortsButton.Location = new System.Drawing.Point(808, 16);
            this.m_oDeployEscortsButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oDeployEscortsButton.Name = "m_oDeployEscortsButton";
            this.m_oDeployEscortsButton.Size = new System.Drawing.Size(88, 31);
            this.m_oDeployEscortsButton.TabIndex = 66;
            this.m_oDeployEscortsButton.Text = "Deploy Escorts";
            this.m_oDeployEscortsButton.UseVisualStyleBackColor = true;
            // 
            // m_oRecallEscortsButton
            // 
            this.m_oRecallEscortsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oRecallEscortsButton.Location = new System.Drawing.Point(718, 16);
            this.m_oRecallEscortsButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oRecallEscortsButton.Name = "m_oRecallEscortsButton";
            this.m_oRecallEscortsButton.Size = new System.Drawing.Size(88, 31);
            this.m_oRecallEscortsButton.TabIndex = 65;
            this.m_oRecallEscortsButton.Text = "Recall Escorts";
            this.m_oRecallEscortsButton.UseVisualStyleBackColor = true;
            // 
            // m_oEqualizeFuelButton
            // 
            this.m_oEqualizeFuelButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oEqualizeFuelButton.Location = new System.Drawing.Point(538, 16);
            this.m_oEqualizeFuelButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oEqualizeFuelButton.Name = "m_oEqualizeFuelButton";
            this.m_oEqualizeFuelButton.Size = new System.Drawing.Size(88, 31);
            this.m_oEqualizeFuelButton.TabIndex = 63;
            this.m_oEqualizeFuelButton.Text = "Equalize Fuel";
            this.m_oEqualizeFuelButton.UseVisualStyleBackColor = true;
            // 
            // m_oEqualizeMaintButton
            // 
            this.m_oEqualizeMaintButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oEqualizeMaintButton.Location = new System.Drawing.Point(448, 16);
            this.m_oEqualizeMaintButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oEqualizeMaintButton.Name = "m_oEqualizeMaintButton";
            this.m_oEqualizeMaintButton.Size = new System.Drawing.Size(88, 31);
            this.m_oEqualizeMaintButton.TabIndex = 62;
            this.m_oEqualizeMaintButton.Text = "Equalize Maint";
            this.m_oEqualizeMaintButton.UseVisualStyleBackColor = true;
            // 
            // m_oNoConditionsButton
            // 
            this.m_oNoConditionsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oNoConditionsButton.Location = new System.Drawing.Point(988, 53);
            this.m_oNoConditionsButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oNoConditionsButton.Name = "m_oNoConditionsButton";
            this.m_oNoConditionsButton.Size = new System.Drawing.Size(88, 31);
            this.m_oNoConditionsButton.TabIndex = 59;
            this.m_oNoConditionsButton.Text = "No Conditions";
            this.m_oNoConditionsButton.UseVisualStyleBackColor = true;
            // 
            // m_oRecoverParaButton
            // 
            this.m_oRecoverParaButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oRecoverParaButton.Location = new System.Drawing.Point(358, 53);
            this.m_oRecoverParaButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oRecoverParaButton.Name = "m_oRecoverParaButton";
            this.m_oRecoverParaButton.Size = new System.Drawing.Size(88, 31);
            this.m_oRecoverParaButton.TabIndex = 52;
            this.m_oRecoverParaButton.Text = "Recover Para";
            this.m_oRecoverParaButton.UseVisualStyleBackColor = true;
            // 
            // m_oLaunchParaButton
            // 
            this.m_oLaunchParaButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oLaunchParaButton.Location = new System.Drawing.Point(268, 53);
            this.m_oLaunchParaButton.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.m_oLaunchParaButton.Name = "m_oLaunchParaButton";
            this.m_oLaunchParaButton.Size = new System.Drawing.Size(88, 31);
            this.m_oLaunchParaButton.TabIndex = 51;
            this.m_oLaunchParaButton.Text = "Launch Para";
            this.m_oLaunchParaButton.UseVisualStyleBackColor = true;
            // 
            // m_oDeleteTGButton
            // 
            this.m_oDeleteTGButton.Location = new System.Drawing.Point(183, 53);
            this.m_oDeleteTGButton.Name = "m_oDeleteTGButton";
            this.m_oDeleteTGButton.Size = new System.Drawing.Size(78, 31);
            this.m_oDeleteTGButton.TabIndex = 50;
            this.m_oDeleteTGButton.Text = "&Delete TG";
            this.m_oDeleteTGButton.UseVisualStyleBackColor = true;
            // 
            // m_oOOBButton
            // 
            this.m_oOOBButton.Location = new System.Drawing.Point(183, 16);
            this.m_oOOBButton.Name = "m_oOOBButton";
            this.m_oOOBButton.Size = new System.Drawing.Size(78, 31);
            this.m_oOOBButton.TabIndex = 49;
            this.m_oOOBButton.Text = "OOB";
            this.m_oOOBButton.UseVisualStyleBackColor = true;
            // 
            // m_oRenameTGButton
            // 
            this.m_oRenameTGButton.Location = new System.Drawing.Point(95, 53);
            this.m_oRenameTGButton.Name = "m_oRenameTGButton";
            this.m_oRenameTGButton.Size = new System.Drawing.Size(78, 31);
            this.m_oRenameTGButton.TabIndex = 48;
            this.m_oRenameTGButton.Text = "Rename TG";
            this.m_oRenameTGButton.UseVisualStyleBackColor = true;
            // 
            // m_oAddColonyButton
            // 
            this.m_oAddColonyButton.Enabled = false;
            this.m_oAddColonyButton.Location = new System.Drawing.Point(95, 16);
            this.m_oAddColonyButton.Name = "m_oAddColonyButton";
            this.m_oAddColonyButton.Size = new System.Drawing.Size(78, 31);
            this.m_oAddColonyButton.TabIndex = 47;
            this.m_oAddColonyButton.Text = "Add Colony";
            this.m_oAddColonyButton.UseVisualStyleBackColor = true;
            // 
            // m_oSystemMapButton
            // 
            this.m_oSystemMapButton.Location = new System.Drawing.Point(6, 16);
            this.m_oSystemMapButton.Name = "m_oSystemMapButton";
            this.m_oSystemMapButton.Size = new System.Drawing.Size(78, 31);
            this.m_oSystemMapButton.TabIndex = 46;
            this.m_oSystemMapButton.Text = "System Map";
            this.m_oSystemMapButton.UseVisualStyleBackColor = true;
            // 
            // m_oNewTGButton
            // 
            this.m_oNewTGButton.Location = new System.Drawing.Point(6, 53);
            this.m_oNewTGButton.Name = "m_oNewTGButton";
            this.m_oNewTGButton.Size = new System.Drawing.Size(78, 31);
            this.m_oNewTGButton.TabIndex = 45;
            this.m_oNewTGButton.Text = "&New TG";
            this.m_oNewTGButton.UseVisualStyleBackColor = true;
            // 
            // TaskGroup_Panel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1193, 986);
            this.Controls.Add(this.m_oButtonBox);
            this.Controls.Add(this.m_oTaskGroupTabControl);
            this.Controls.Add(this.m_oOrderTimeDistBox);
            this.Controls.Add(this.m_oOfficerBox);
            this.Controls.Add(this.m_oInitiativeBox);
            this.Controls.Add(this.m_oSurveyBox);
            this.Controls.Add(this.m_oCenterShowGF);
            this.Controls.Add(this.m_oSpeedBox);
            this.Controls.Add(this.m_oGeneralTGDetailsBox);
            this.Controls.Add(this.m_oShipsBox);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "TaskGroup_Panel";
            this.Text = "Task Groups";
            this.m_oGeneralTGDetailsBox.ResumeLayout(false);
            this.m_oGeneralTGDetailsBox.PerformLayout();
            this.m_oSpeedBox.ResumeLayout(false);
            this.m_oSpeedBox.PerformLayout();
            this.m_oCenterShowGF.ResumeLayout(false);
            this.m_oCenterShowGF.PerformLayout();
            this.m_oSurveyBox.ResumeLayout(false);
            this.m_oSurveyBox.PerformLayout();
            this.m_oInitiativeBox.ResumeLayout(false);
            this.m_oInitiativeBox.PerformLayout();
            this.m_oOfficerBox.ResumeLayout(false);
            this.m_oOfficerBox.PerformLayout();
            this.m_oOrderTimeDistBox.ResumeLayout(false);
            this.m_oOrderTimeDistBox.PerformLayout();
            this.m_oTaskGroupTabControl.ResumeLayout(false);
            this.m_oTaskGroupOrdersTabPage.ResumeLayout(false);
            this.m_oTaskGroupOrdersBox.ResumeLayout(false);
            this.m_oTaskGroupOrdersBox.PerformLayout();
            this.m_oCopyOrdersGroupBox.ResumeLayout(false);
            this.m_oCopyOrdersGroupBox.PerformLayout();
            this.m_oSystemDisplayOptionsBox.ResumeLayout(false);
            this.m_oSystemDisplayOptionsBox.PerformLayout();
            this.m_oButtonBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox m_oTaskGroupName;
        private GroupBox m_oShipsBox;
        private GroupBox m_oGeneralTGDetailsBox;
        private Label m_oTaskGroupLabel;
        private ComboBox m_oFactionName;
        private Label m_oFactionLabel;
        private Label m_oLocationLabel;
        private TextBox m_oTGLocation;
        private ComboBox m_oTaskForceName;
        private Label m_oTFLabel;
        private GroupBox m_oSpeedBox;
        private TextBox m_oMaxSpeedBox;
        private TextBox m_oCurSpeedBox;
        private Button m_oMaxSpeedButton;
        private Button m_oSetSpeedButton;
        private GroupBox m_oCenterShowGF;
        private CheckBox m_oCenterMapCheckBox;
        private CheckBox m_oShowGroundForces;
        private GroupBox m_oSurveyBox;
        private Label m_oGravLabel;
        private TextBox m_oGravTextBox;
        private TextBox m_oGeoTextBox;
        private Label m_oGeoLabel;
        private GroupBox m_oInitiativeBox;
        private Button m_oInitiativeButton;
        private TextBox m_oCurrentInitTextBox;
        private Label m_oCurrentInitLabel;
        private Label m_oMaxInitLabel;
        private TextBox m_oMaxInitTextBox;
        private GroupBox m_oOfficerBox;
        private TextBox m_oSeniorOfficerTextBox;
        private GroupBox m_oOrderTimeDistBox;
        private TextBox m_oTimeDistTextBox;
        private RadioButton m_oAllOrdersRadioButton;
        private RadioButton m_oCurrentTDRadioButton;
        private TabControl m_oTaskGroupTabControl;
        private TabPage m_oTaskGroupOrdersTabPage;
        private TabPage m_oSpecialOrdersTabPage;
        private TabPage m_oHistoryTabPage;
        private TabPage m_oNavalOrgTabPage;
        private GroupBox m_oSystemDisplayOptionsBox;
        private CheckBox m_oShowAllPopsCheckBox;
        private CheckBox m_oOrderFilteringCheckBox;
        private CheckBox m_oExcludeSurveyedCheckBox;
        private CheckBox m_oSurveyLocationsCheckBox;
        private CheckBox m_oWrecksCheckBox;
        private CheckBox m_oLifePodsCheckBox;
        private CheckBox m_oCometsCheckBox;
        private CheckBox m_oContactsCheckBox;
        private CheckBox m_oWaypointCheckBox;
        private CheckBox m_oTaskGroupsCheckBox;
        private CheckBox m_oAsteroidsCheckBox;
        private CheckBox m_oMoonsCheckBox;
        private GroupBox m_oCopyOrdersGroupBox;
        private CheckBox m_oIncCondOrdersCheckBox;
        private CheckBox m_oIncDefaultCheckBox;
        private CheckBox checkBox1;
        private Button m_oCopyOrdersButton;
        private GroupBox m_oCargoFightersTroopsGroupBox;
        private GroupBox m_oDefaultCondEscortOrdersGroupBox;
        private GroupBox m_oOOBGroupBox;
        private GroupBox m_oTaskGroupOrdersBox;
        private ListBox m_oPlottedMoveListBox;
        private ListBox m_oActionsAvailableListBox;
        private ListBox m_oSystemLocationsListBox;
        private Label m_oPlottedMoveLabel;
        private Label m_oActionsAvailableLabel;
        private Label m_oSystemLocationsLabel;
        private Button m_oRemoveAllButton;
        private Button m_oRemoveButton;
        private Button m_oAddMoveButton;
        private TextBox m_oLoadAmtTextBox;
        private TextBox m_oOrbitalDistanceTextBox;
        private Label m_oLoadLimitLabel;
        private Label m_oOrbitalDistanceLabel;
        private TextBox m_oRepeatOrdersTextBox;
        private Button m_oRepeatOrderButton;
        private CheckBox m_oCycleMovesCheckBox;
        private TextBox m_oOrderDelayTextBox;
        private Label m_oOrderDelayLabel;
        private CheckBox m_oAutoRouteCheckBox;
        private GroupBox m_oButtonBox;
        private Button m_oDeployEscortsButton;
        private Button m_oRecallEscortsButton;
        private Button m_oEqualizeFuelButton;
        private Button m_oEqualizeMaintButton;
        private Button m_oNoConditionsButton;
        private Button m_oRecoverParaButton;
        private Button m_oLaunchParaButton;
        private Button m_oDeleteTGButton;
        private Button m_oOOBButton;
        private Button m_oRenameTGButton;
        private Button m_oAddColonyButton;
        private Button m_oSystemMapButton;
        private Button m_oNewTGButton;
        private Button m_oAssembleButton;
        private Button m_oDetachButton;
        private Button m_oEscortButton;
        private Button m_oSaveEscortsButton;
        private Button m_oMissileLaunchButton;
        private Button m_oReloadParaButton;
        private Button m_oHyperOnButton;
        private Button m_oHyperOffButton;
        private Button m_oShieldsOnButton;
        private Button m_oShieldsOffButton;
        private Button m_oNoDefaultButton;
        private Button m_oCloseButton;
    }
}