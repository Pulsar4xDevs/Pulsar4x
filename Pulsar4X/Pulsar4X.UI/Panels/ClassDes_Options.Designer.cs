using System.Windows.Forms;

namespace Pulsar4X.UI.Panels
{
    partial class ClassDes_Options
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Class summary text should be printed here.
        /// </summary>
        public RichTextBox ClassSummaryTextBox
        {
            get { return m_oClassSummaryTextBox; }
        }


        #region Power Systems
        /// <summary>
        /// Total engine power
        /// </summary>
        public TextBox EnginePowerTextBox
        {
            get { return m_oEnginePowerTextBox; }
        }

        /// <summary>
        /// Maximum class speed.
        /// </summary>
        public TextBox MaxSpeedTextBox
        {
            get { return m_oMaxSpeedTextBox; }
        }

        /// <summary>
        /// Total Supplied power by ship reactors.
        /// </summary>
        public TextBox ReactorPowerTextBox
        {
            get { return m_oReactorPowerTextBox; }
        }

        /// <summary>
        /// Total required power for beam weapons.
        /// </summary>
        public TextBox RequiredPowerTextBox
        {
            get { return m_oRequiredPowerTextBox; }
        }
        #endregion

        #region Passive Defenses

        /// <summary>
        /// Depth of armor, this one will be editable if design isn't locked.
        /// </summary>
        public TextBox ArmorRatingTextBox
        {
            get { return m_oArmorRatingTextBox; }
        }

        /// <summary>
        /// size in tons of ship
        /// </summary>
        public TextBox ExactClassSizeTextBox
        {
            get { return m_oClassSizeTextBox; }
        }

        /// <summary>
        /// Area the armor covers
        /// </summary>
        public TextBox ArmorAreaTextBox
        {
            get { return m_oAAreaTextBox; }
        }

        /// <summary>
        /// Strength of Armor
        /// </summary>
        public TextBox ArmorStrengthTextBox
        {
            get { return m_oAStrengthTextBox; }
        }

        /// <summary>
        /// Width of the armor on a ship.
        /// </summary>
        public TextBox ArmorColumnsTextBox
        {
            get { return m_oAColumnsTextBox; }
        }

        /// <summary>
        /// Shield Strength
        /// </summary>
        public TextBox ShieldStrengthTextBox
        {
            get { return m_oSStrengthTextBox; }
        }

        /// <summary>
        /// Shield recharge
        /// </summary>
        public TextBox ShieldRechargeTextBox
        {
            get { return m_oSRechargeTextBox; }
        }

        public TextBox InternalHTKTextBox
        {
            get { return m_oInternalHTKTextBox; }
        }
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
            this.m_oCivGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oShowCivilianDesignsCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oSortCostRadioButton = new System.Windows.Forms.RadioButton();
            this.m_oSortHullRadioButton = new System.Windows.Forms.RadioButton();
            this.m_oSortSizeRadioButton = new System.Windows.Forms.RadioButton();
            this.m_oSortAlphaRadioButton = new System.Windows.Forms.RadioButton();
            this.m_oNoThemeCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oHideObsoleteCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oHullLabel = new System.Windows.Forms.Label();
            this.m_oTypeLabel = new System.Windows.Forms.Label();
            this.m_oClassLabel = new System.Windows.Forms.Label();
            this.m_oEmpireLabel = new System.Windows.Forms.Label();
            this.m_oHullComboBox = new System.Windows.Forms.ComboBox();
            this.m_oTypeComboBox = new System.Windows.Forms.ComboBox();
            this.m_oClassComboBox = new System.Windows.Forms.ComboBox();
            this.m_oFactionComboBox = new System.Windows.Forms.ComboBox();
            this.m_oClassOptionsGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oKeepExcessQCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oConscriptCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oSupplyShipCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oObsoleteCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oSizeinTonsCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oCollierCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oTankerCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oButtonsGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oCloseButton = new System.Windows.Forms.Button();
            this.m_oSMModeButton = new System.Windows.Forms.Button();
            this.m_oNewButton = new System.Windows.Forms.Button();
            this.m_oViewTechButton = new System.Windows.Forms.Button();
            this.m_oTextFileButton = new System.Windows.Forms.Button();
            this.m_oObsoleteCompButton = new System.Windows.Forms.Button();
            this.m_oFleetAssignBbutton = new System.Windows.Forms.Button();
            this.m_oRefreshTechButton = new System.Windows.Forms.Button();
            this.m_oCopyDesignButton = new System.Windows.Forms.Button();
            this.m_oReNumberButton = new System.Windows.Forms.Button();
            this.m_oAutoRenameButton = new System.Windows.Forms.Button();
            this.m_oRandomNameButton = new System.Windows.Forms.Button();
            this.m_oDeleteButton = new System.Windows.Forms.Button();
            this.m_oNewHullButton = new System.Windows.Forms.Button();
            this.m_oLockDesignButton = new System.Windows.Forms.Button();
            this.m_oNewArmorButton = new System.Windows.Forms.Button();
            this.m_oDesignTechButton = new System.Windows.Forms.Button();
            this.m_oNPRClassButton = new System.Windows.Forms.Button();
            this.m_oRenameButton = new System.Windows.Forms.Button();
            this.m_oTaskGroupTabControl = new System.Windows.Forms.TabControl();
            this.m_oSummaryTab = new System.Windows.Forms.TabPage();
            this.m_oArmorGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oClassSummaryGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oClassSummaryTextBox = new System.Windows.Forms.RichTextBox();
            this.m_oTargetSpeedGroupBox = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.m_oSpeedCustomTextBox = new System.Windows.Forms.TextBox();
            this.m_oSpeedCustomRadioButton = new System.Windows.Forms.RadioButton();
            this.m_oSpeed100000RadioButton = new System.Windows.Forms.RadioButton();
            this.m_oSpeed50000RadioButton = new System.Windows.Forms.RadioButton();
            this.m_oSpeed2000RadioButton = new System.Windows.Forms.RadioButton();
            this.m_oSpeed3000RadioButton = new System.Windows.Forms.RadioButton();
            this.m_oSpeed5000RadioButton = new System.Windows.Forms.RadioButton();
            this.m_oSpeed20000RadioButton = new System.Windows.Forms.RadioButton();
            this.m_oSpeed10000RadioButton = new System.Windows.Forms.RadioButton();
            this.m_oSpeed1000RadioButton = new System.Windows.Forms.RadioButton();
            this.m_oRangeBandsGroupBox = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.m_oRangeCustomTextBox = new System.Windows.Forms.TextBox();
            this.m_oRangeCustomRadioButton = new System.Windows.Forms.RadioButton();
            this.m_oRange1000000RadioButton = new System.Windows.Forms.RadioButton();
            this.m_oRange500000RadioButton = new System.Windows.Forms.RadioButton();
            this.m_oRange20000RadioButton = new System.Windows.Forms.RadioButton();
            this.m_oRange30000RadioButton = new System.Windows.Forms.RadioButton();
            this.m_oRange50000RadioButton = new System.Windows.Forms.RadioButton();
            this.m_oRange200000RadioButton = new System.Windows.Forms.RadioButton();
            this.m_oTange100000RadioButton = new System.Windows.Forms.RadioButton();
            this.m_oRange10000RadioButton = new System.Windows.Forms.RadioButton();
            this.m_oDesignTab = new System.Windows.Forms.TabPage();
            this.m_oOrdFightersTab = new System.Windows.Forms.TabPage();
            this.m_oCompSummaryTab = new System.Windows.Forms.TabPage();
            this.m_oDACRankInfoTab = new System.Windows.Forms.TabPage();
            this.m_oShipsTab = new System.Windows.Forms.TabPage();
            this.m_oGlossaryTab = new System.Windows.Forms.TabPage();
            this.m_oArmourUpButton = new System.Windows.Forms.Button();
            this.m_oArmourDownButton = new System.Windows.Forms.Button();
            this.m_oGeneralInfoGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oBuildPointGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oBuildLoadGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oLoadTimeLabel = new System.Windows.Forms.Label();
            this.m_oBuildTimeLabel = new System.Windows.Forms.Label();
            this.m_oLoadTimeTextBox = new System.Windows.Forms.TextBox();
            this.m_oBuildTimeTextBox = new System.Windows.Forms.TextBox();
            this.m_oPowerSystemGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oRequiredPowerLabel = new System.Windows.Forms.Label();
            this.m_oReactorPowerLabel = new System.Windows.Forms.Label();
            this.m_oJumpDistLabel = new System.Windows.Forms.Label();
            this.m_oJumpRatingLabel = new System.Windows.Forms.Label();
            this.m_oMaxSpeedLabel = new System.Windows.Forms.Label();
            this.m_oEnginePowerLabel = new System.Windows.Forms.Label();
            this.m_oRequiredPowerTextBox = new System.Windows.Forms.TextBox();
            this.m_oReactorPowerTextBox = new System.Windows.Forms.TextBox();
            this.m_oJumpDistTextBox = new System.Windows.Forms.TextBox();
            this.m_oJumpRatingTextBox = new System.Windows.Forms.TextBox();
            this.m_oMaxSpeedTextBox = new System.Windows.Forms.TextBox();
            this.m_oEnginePowerTextBox = new System.Windows.Forms.TextBox();
            this.m_oPassiveDefGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oArmorRatingLabel = new System.Windows.Forms.Label();
            this.m_oArmorRatingTextBox = new System.Windows.Forms.TextBox();
            this.m_oClassSizeTextBox = new System.Windows.Forms.TextBox();
            this.m_oClassSizeLabel = new System.Windows.Forms.Label();
            this.m_oIHTKLabel = new System.Windows.Forms.Label();
            this.m_oAAreaLabel = new System.Windows.Forms.Label();
            this.m_oSRechargeLabel = new System.Windows.Forms.Label();
            this.m_oAAreaTextBox = new System.Windows.Forms.TextBox();
            this.m_oSStrengthLabel = new System.Windows.Forms.Label();
            this.m_oAStrengthTextBox = new System.Windows.Forms.TextBox();
            this.m_oAColumnLabel = new System.Windows.Forms.Label();
            this.m_oAColumnsTextBox = new System.Windows.Forms.TextBox();
            this.m_oAStrengthLabel = new System.Windows.Forms.Label();
            this.m_oSStrengthTextBox = new System.Windows.Forms.TextBox();
            this.m_oSRechargeTextBox = new System.Windows.Forms.TextBox();
            this.m_oInternalHTKTextBox = new System.Windows.Forms.TextBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.m_oDeployTimeLabel = new System.Windows.Forms.Label();
            this.m_oDeployTimeTextBox = new System.Windows.Forms.TextBox();
            this.m_oTonsManTextBox = new System.Windows.Forms.TextBox();
            this.m_oTonPerManLabel = new System.Windows.Forms.Label();
            this.m_oCryoBerthsLabel = new System.Windows.Forms.Label();
            this.m_oCapPerHSLabel = new System.Windows.Forms.Label();
            this.m_oSpareBerthsLabel = new System.Windows.Forms.Label();
            this.m_oCapPerHSTextBox = new System.Windows.Forms.TextBox();
            this.m_oCrewBerthsLabel = new System.Windows.Forms.Label();
            this.m_oAccomHSReqTextBox = new System.Windows.Forms.TextBox();
            this.m_oAccomHSAvailLabel = new System.Windows.Forms.Label();
            this.m_oAccomHSAvailTextBox = new System.Windows.Forms.TextBox();
            this.m_oAccomHSReqLabel = new System.Windows.Forms.Label();
            this.m_oCrewBerthsTextBox = new System.Windows.Forms.TextBox();
            this.m_oSpareBerthsTextBox = new System.Windows.Forms.TextBox();
            this.m_oCryoBerthsTextBox = new System.Windows.Forms.TextBox();
            this.m_oCivGroupBox.SuspendLayout();
            this.m_oClassOptionsGroupBox.SuspendLayout();
            this.m_oButtonsGroupBox.SuspendLayout();
            this.m_oTaskGroupTabControl.SuspendLayout();
            this.m_oSummaryTab.SuspendLayout();
            this.m_oArmorGroupBox.SuspendLayout();
            this.m_oClassSummaryGroupBox.SuspendLayout();
            this.m_oTargetSpeedGroupBox.SuspendLayout();
            this.m_oRangeBandsGroupBox.SuspendLayout();
            this.m_oGeneralInfoGroupBox.SuspendLayout();
            this.m_oBuildLoadGroupBox.SuspendLayout();
            this.m_oPowerSystemGroupBox.SuspendLayout();
            this.m_oPassiveDefGroupBox.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_oCivGroupBox
            // 
            this.m_oCivGroupBox.Controls.Add(this.m_oShowCivilianDesignsCheckBox);
            this.m_oCivGroupBox.Location = new System.Drawing.Point(10, 780);
            this.m_oCivGroupBox.Margin = new System.Windows.Forms.Padding(1);
            this.m_oCivGroupBox.Name = "m_oCivGroupBox";
            this.m_oCivGroupBox.Size = new System.Drawing.Size(186, 40);
            this.m_oCivGroupBox.TabIndex = 0;
            this.m_oCivGroupBox.TabStop = false;
            // 
            // m_oShowCivilianDesignsCheckBox
            // 
            this.m_oShowCivilianDesignsCheckBox.AutoSize = true;
            this.m_oShowCivilianDesignsCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oShowCivilianDesignsCheckBox.Location = new System.Drawing.Point(25, 18);
            this.m_oShowCivilianDesignsCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.m_oShowCivilianDesignsCheckBox.Name = "m_oShowCivilianDesignsCheckBox";
            this.m_oShowCivilianDesignsCheckBox.Size = new System.Drawing.Size(130, 17);
            this.m_oShowCivilianDesignsCheckBox.TabIndex = 8;
            this.m_oShowCivilianDesignsCheckBox.Text = "Show Civilian Designs";
            this.m_oShowCivilianDesignsCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oSortCostRadioButton
            // 
            this.m_oSortCostRadioButton.AutoSize = true;
            this.m_oSortCostRadioButton.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oSortCostRadioButton.Location = new System.Drawing.Point(1132, 22);
            this.m_oSortCostRadioButton.Name = "m_oSortCostRadioButton";
            this.m_oSortCostRadioButton.Size = new System.Drawing.Size(82, 17);
            this.m_oSortCostRadioButton.TabIndex = 14;
            this.m_oSortCostRadioButton.Text = "Sort by Cost";
            this.m_oSortCostRadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oSortHullRadioButton
            // 
            this.m_oSortHullRadioButton.AutoSize = true;
            this.m_oSortHullRadioButton.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oSortHullRadioButton.Location = new System.Drawing.Point(1043, 22);
            this.m_oSortHullRadioButton.Name = "m_oSortHullRadioButton";
            this.m_oSortHullRadioButton.Size = new System.Drawing.Size(79, 17);
            this.m_oSortHullRadioButton.TabIndex = 13;
            this.m_oSortHullRadioButton.Text = "Sort by Hull";
            this.m_oSortHullRadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oSortSizeRadioButton
            // 
            this.m_oSortSizeRadioButton.AutoSize = true;
            this.m_oSortSizeRadioButton.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oSortSizeRadioButton.Location = new System.Drawing.Point(949, 22);
            this.m_oSortSizeRadioButton.Name = "m_oSortSizeRadioButton";
            this.m_oSortSizeRadioButton.Size = new System.Drawing.Size(81, 17);
            this.m_oSortSizeRadioButton.TabIndex = 12;
            this.m_oSortSizeRadioButton.Text = "Sort by Size";
            this.m_oSortSizeRadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oSortAlphaRadioButton
            // 
            this.m_oSortAlphaRadioButton.AutoSize = true;
            this.m_oSortAlphaRadioButton.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oSortAlphaRadioButton.Checked = true;
            this.m_oSortAlphaRadioButton.Location = new System.Drawing.Point(850, 22);
            this.m_oSortAlphaRadioButton.Name = "m_oSortAlphaRadioButton";
            this.m_oSortAlphaRadioButton.Size = new System.Drawing.Size(88, 17);
            this.m_oSortAlphaRadioButton.TabIndex = 11;
            this.m_oSortAlphaRadioButton.TabStop = true;
            this.m_oSortAlphaRadioButton.Text = "Sort by Alpha";
            this.m_oSortAlphaRadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oNoThemeCheckBox
            // 
            this.m_oNoThemeCheckBox.AutoSize = true;
            this.m_oNoThemeCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oNoThemeCheckBox.Location = new System.Drawing.Point(640, 23);
            this.m_oNoThemeCheckBox.Name = "m_oNoThemeCheckBox";
            this.m_oNoThemeCheckBox.Size = new System.Drawing.Size(107, 17);
            this.m_oNoThemeCheckBox.TabIndex = 10;
            this.m_oNoThemeCheckBox.Text = "No Theme Name";
            this.m_oNoThemeCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oHideObsoleteCheckBox
            // 
            this.m_oHideObsoleteCheckBox.AutoSize = true;
            this.m_oHideObsoleteCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oHideObsoleteCheckBox.Location = new System.Drawing.Point(753, 23);
            this.m_oHideObsoleteCheckBox.Name = "m_oHideObsoleteCheckBox";
            this.m_oHideObsoleteCheckBox.Size = new System.Drawing.Size(93, 17);
            this.m_oHideObsoleteCheckBox.TabIndex = 9;
            this.m_oHideObsoleteCheckBox.Text = "Hide Obsolete";
            this.m_oHideObsoleteCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oHullLabel
            // 
            this.m_oHullLabel.AutoSize = true;
            this.m_oHullLabel.Location = new System.Drawing.Point(522, 16);
            this.m_oHullLabel.Name = "m_oHullLabel";
            this.m_oHullLabel.Size = new System.Drawing.Size(25, 13);
            this.m_oHullLabel.TabIndex = 7;
            this.m_oHullLabel.Text = "Hull";
            // 
            // m_oTypeLabel
            // 
            this.m_oTypeLabel.AutoSize = true;
            this.m_oTypeLabel.Location = new System.Drawing.Point(374, 16);
            this.m_oTypeLabel.Name = "m_oTypeLabel";
            this.m_oTypeLabel.Size = new System.Drawing.Size(31, 13);
            this.m_oTypeLabel.TabIndex = 6;
            this.m_oTypeLabel.Text = "Type";
            // 
            // m_oClassLabel
            // 
            this.m_oClassLabel.AutoSize = true;
            this.m_oClassLabel.Location = new System.Drawing.Point(194, 16);
            this.m_oClassLabel.Name = "m_oClassLabel";
            this.m_oClassLabel.Size = new System.Drawing.Size(32, 13);
            this.m_oClassLabel.TabIndex = 5;
            this.m_oClassLabel.Text = "Class";
            // 
            // m_oEmpireLabel
            // 
            this.m_oEmpireLabel.AutoSize = true;
            this.m_oEmpireLabel.Location = new System.Drawing.Point(6, 16);
            this.m_oEmpireLabel.Name = "m_oEmpireLabel";
            this.m_oEmpireLabel.Size = new System.Drawing.Size(39, 13);
            this.m_oEmpireLabel.TabIndex = 4;
            this.m_oEmpireLabel.Text = "Empire";
            // 
            // m_oHullComboBox
            // 
            this.m_oHullComboBox.FormattingEnabled = true;
            this.m_oHullComboBox.Location = new System.Drawing.Point(556, 13);
            this.m_oHullComboBox.Name = "m_oHullComboBox";
            this.m_oHullComboBox.Size = new System.Drawing.Size(140, 21);
            this.m_oHullComboBox.TabIndex = 3;
            // 
            // m_oTypeComboBox
            // 
            this.m_oTypeComboBox.FormattingEnabled = true;
            this.m_oTypeComboBox.Location = new System.Drawing.Point(414, 13);
            this.m_oTypeComboBox.Name = "m_oTypeComboBox";
            this.m_oTypeComboBox.Size = new System.Drawing.Size(102, 21);
            this.m_oTypeComboBox.TabIndex = 2;
            // 
            // m_oClassComboBox
            // 
            this.m_oClassComboBox.FormattingEnabled = true;
            this.m_oClassComboBox.Location = new System.Drawing.Point(235, 13);
            this.m_oClassComboBox.Name = "m_oClassComboBox";
            this.m_oClassComboBox.Size = new System.Drawing.Size(133, 21);
            this.m_oClassComboBox.TabIndex = 1;
            // 
            // m_oFactionComboBox
            // 
            this.m_oFactionComboBox.FormattingEnabled = true;
            this.m_oFactionComboBox.Location = new System.Drawing.Point(57, 13);
            this.m_oFactionComboBox.Name = "m_oFactionComboBox";
            this.m_oFactionComboBox.Size = new System.Drawing.Size(122, 21);
            this.m_oFactionComboBox.TabIndex = 0;
            // 
            // m_oClassOptionsGroupBox
            // 
            this.m_oClassOptionsGroupBox.Controls.Add(this.m_oKeepExcessQCheckBox);
            this.m_oClassOptionsGroupBox.Controls.Add(this.m_oConscriptCheckBox);
            this.m_oClassOptionsGroupBox.Controls.Add(this.m_oSupplyShipCheckBox);
            this.m_oClassOptionsGroupBox.Controls.Add(this.m_oObsoleteCheckBox);
            this.m_oClassOptionsGroupBox.Controls.Add(this.m_oSizeinTonsCheckBox);
            this.m_oClassOptionsGroupBox.Controls.Add(this.m_oCollierCheckBox);
            this.m_oClassOptionsGroupBox.Controls.Add(this.m_oTankerCheckBox);
            this.m_oClassOptionsGroupBox.Location = new System.Drawing.Point(877, 12);
            this.m_oClassOptionsGroupBox.Name = "m_oClassOptionsGroupBox";
            this.m_oClassOptionsGroupBox.Size = new System.Drawing.Size(349, 51);
            this.m_oClassOptionsGroupBox.TabIndex = 1;
            this.m_oClassOptionsGroupBox.TabStop = false;
            this.m_oClassOptionsGroupBox.Text = "Class Options";
            // 
            // m_oKeepExcessQCheckBox
            // 
            this.m_oKeepExcessQCheckBox.AutoSize = true;
            this.m_oKeepExcessQCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oKeepExcessQCheckBox.Location = new System.Drawing.Point(230, 12);
            this.m_oKeepExcessQCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.m_oKeepExcessQCheckBox.Name = "m_oKeepExcessQCheckBox";
            this.m_oKeepExcessQCheckBox.Size = new System.Drawing.Size(99, 17);
            this.m_oKeepExcessQCheckBox.TabIndex = 6;
            this.m_oKeepExcessQCheckBox.Text = "Keep Excess Q";
            this.m_oKeepExcessQCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oConscriptCheckBox
            // 
            this.m_oConscriptCheckBox.AutoSize = true;
            this.m_oConscriptCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oConscriptCheckBox.Location = new System.Drawing.Point(68, 12);
            this.m_oConscriptCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.m_oConscriptCheckBox.Name = "m_oConscriptCheckBox";
            this.m_oConscriptCheckBox.Size = new System.Drawing.Size(70, 17);
            this.m_oConscriptCheckBox.TabIndex = 5;
            this.m_oConscriptCheckBox.Text = "Conscript";
            this.m_oConscriptCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oSupplyShipCheckBox
            // 
            this.m_oSupplyShipCheckBox.AutoSize = true;
            this.m_oSupplyShipCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oSupplyShipCheckBox.Location = new System.Drawing.Point(143, 12);
            this.m_oSupplyShipCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.m_oSupplyShipCheckBox.Name = "m_oSupplyShipCheckBox";
            this.m_oSupplyShipCheckBox.Size = new System.Drawing.Size(82, 17);
            this.m_oSupplyShipCheckBox.TabIndex = 4;
            this.m_oSupplyShipCheckBox.Text = "Supply Ship";
            this.m_oSupplyShipCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oObsoleteCheckBox
            // 
            this.m_oObsoleteCheckBox.AutoSize = true;
            this.m_oObsoleteCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oObsoleteCheckBox.Location = new System.Drawing.Point(70, 31);
            this.m_oObsoleteCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.m_oObsoleteCheckBox.Name = "m_oObsoleteCheckBox";
            this.m_oObsoleteCheckBox.Size = new System.Drawing.Size(68, 17);
            this.m_oObsoleteCheckBox.TabIndex = 3;
            this.m_oObsoleteCheckBox.Text = "Obsolete";
            this.m_oObsoleteCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oSizeinTonsCheckBox
            // 
            this.m_oSizeinTonsCheckBox.AutoSize = true;
            this.m_oSizeinTonsCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oSizeinTonsCheckBox.Location = new System.Drawing.Point(142, 31);
            this.m_oSizeinTonsCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.m_oSizeinTonsCheckBox.Name = "m_oSizeinTonsCheckBox";
            this.m_oSizeinTonsCheckBox.Size = new System.Drawing.Size(84, 17);
            this.m_oSizeinTonsCheckBox.TabIndex = 2;
            this.m_oSizeinTonsCheckBox.Text = "Size in Tons";
            this.m_oSizeinTonsCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oCollierCheckBox
            // 
            this.m_oCollierCheckBox.AutoSize = true;
            this.m_oCollierCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oCollierCheckBox.Location = new System.Drawing.Point(7, 31);
            this.m_oCollierCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.m_oCollierCheckBox.Name = "m_oCollierCheckBox";
            this.m_oCollierCheckBox.Size = new System.Drawing.Size(54, 17);
            this.m_oCollierCheckBox.TabIndex = 1;
            this.m_oCollierCheckBox.Text = "Collier";
            this.m_oCollierCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oTankerCheckBox
            // 
            this.m_oTankerCheckBox.AutoSize = true;
            this.m_oTankerCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oTankerCheckBox.Location = new System.Drawing.Point(4, 12);
            this.m_oTankerCheckBox.Margin = new System.Windows.Forms.Padding(2);
            this.m_oTankerCheckBox.Name = "m_oTankerCheckBox";
            this.m_oTankerCheckBox.Size = new System.Drawing.Size(60, 17);
            this.m_oTankerCheckBox.TabIndex = 0;
            this.m_oTankerCheckBox.Text = "Tanker";
            this.m_oTankerCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oButtonsGroupBox
            // 
            this.m_oButtonsGroupBox.Controls.Add(this.m_oSortCostRadioButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oCloseButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oSortHullRadioButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oSMModeButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oSortSizeRadioButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oNewButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oSortAlphaRadioButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oViewTechButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oHideObsoleteCheckBox);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oNoThemeCheckBox);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oTextFileButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oObsoleteCompButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oFleetAssignBbutton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oRefreshTechButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oCopyDesignButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oReNumberButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oAutoRenameButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oRandomNameButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oDeleteButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oNewHullButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oLockDesignButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oNewArmorButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oDesignTechButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oNPRClassButton);
            this.m_oButtonsGroupBox.Controls.Add(this.m_oRenameButton);
            this.m_oButtonsGroupBox.Location = new System.Drawing.Point(10, 824);
            this.m_oButtonsGroupBox.Margin = new System.Windows.Forms.Padding(1);
            this.m_oButtonsGroupBox.Name = "m_oButtonsGroupBox";
            this.m_oButtonsGroupBox.Size = new System.Drawing.Size(1222, 80);
            this.m_oButtonsGroupBox.TabIndex = 2;
            this.m_oButtonsGroupBox.TabStop = false;
            // 
            // m_oCloseButton
            // 
            this.m_oCloseButton.Location = new System.Drawing.Point(1130, 48);
            this.m_oCloseButton.Name = "m_oCloseButton";
            this.m_oCloseButton.Size = new System.Drawing.Size(86, 23);
            this.m_oCloseButton.TabIndex = 19;
            this.m_oCloseButton.Text = "Close";
            this.m_oCloseButton.UseVisualStyleBackColor = true;
            // 
            // m_oSMModeButton
            // 
            this.m_oSMModeButton.Location = new System.Drawing.Point(578, 48);
            this.m_oSMModeButton.Name = "m_oSMModeButton";
            this.m_oSMModeButton.Size = new System.Drawing.Size(86, 23);
            this.m_oSMModeButton.TabIndex = 18;
            this.m_oSMModeButton.Text = "SM Mode";
            this.m_oSMModeButton.UseVisualStyleBackColor = true;
            // 
            // m_oNewButton
            // 
            this.m_oNewButton.Location = new System.Drawing.Point(6, 48);
            this.m_oNewButton.Name = "m_oNewButton";
            this.m_oNewButton.Size = new System.Drawing.Size(88, 23);
            this.m_oNewButton.TabIndex = 16;
            this.m_oNewButton.Text = "New";
            this.m_oNewButton.UseVisualStyleBackColor = true;
            // 
            // m_oViewTechButton
            // 
            this.m_oViewTechButton.Location = new System.Drawing.Point(946, 48);
            this.m_oViewTechButton.Name = "m_oViewTechButton";
            this.m_oViewTechButton.Size = new System.Drawing.Size(86, 23);
            this.m_oViewTechButton.TabIndex = 15;
            this.m_oViewTechButton.Text = "View Tech";
            this.m_oViewTechButton.UseVisualStyleBackColor = true;
            // 
            // m_oTextFileButton
            // 
            this.m_oTextFileButton.Location = new System.Drawing.Point(1038, 48);
            this.m_oTextFileButton.Name = "m_oTextFileButton";
            this.m_oTextFileButton.Size = new System.Drawing.Size(86, 23);
            this.m_oTextFileButton.TabIndex = 14;
            this.m_oTextFileButton.Text = "Text File";
            this.m_oTextFileButton.UseVisualStyleBackColor = true;
            // 
            // m_oObsoleteCompButton
            // 
            this.m_oObsoleteCompButton.Location = new System.Drawing.Point(762, 48);
            this.m_oObsoleteCompButton.Name = "m_oObsoleteCompButton";
            this.m_oObsoleteCompButton.Size = new System.Drawing.Size(86, 23);
            this.m_oObsoleteCompButton.TabIndex = 13;
            this.m_oObsoleteCompButton.Text = "Obsol. Comp";
            this.m_oObsoleteCompButton.UseVisualStyleBackColor = true;
            // 
            // m_oFleetAssignBbutton
            // 
            this.m_oFleetAssignBbutton.Location = new System.Drawing.Point(854, 48);
            this.m_oFleetAssignBbutton.Name = "m_oFleetAssignBbutton";
            this.m_oFleetAssignBbutton.Size = new System.Drawing.Size(86, 23);
            this.m_oFleetAssignBbutton.TabIndex = 12;
            this.m_oFleetAssignBbutton.Text = "Fleet Assigns";
            this.m_oFleetAssignBbutton.UseVisualStyleBackColor = true;
            // 
            // m_oRefreshTechButton
            // 
            this.m_oRefreshTechButton.Location = new System.Drawing.Point(670, 48);
            this.m_oRefreshTechButton.Name = "m_oRefreshTechButton";
            this.m_oRefreshTechButton.Size = new System.Drawing.Size(86, 23);
            this.m_oRefreshTechButton.TabIndex = 11;
            this.m_oRefreshTechButton.Text = "Refresh Tech";
            this.m_oRefreshTechButton.UseVisualStyleBackColor = true;
            // 
            // m_oCopyDesignButton
            // 
            this.m_oCopyDesignButton.Location = new System.Drawing.Point(466, 48);
            this.m_oCopyDesignButton.Name = "m_oCopyDesignButton";
            this.m_oCopyDesignButton.Size = new System.Drawing.Size(88, 23);
            this.m_oCopyDesignButton.TabIndex = 10;
            this.m_oCopyDesignButton.Text = "Copy Design";
            this.m_oCopyDesignButton.UseVisualStyleBackColor = true;
            // 
            // m_oReNumberButton
            // 
            this.m_oReNumberButton.Location = new System.Drawing.Point(282, 19);
            this.m_oReNumberButton.Name = "m_oReNumberButton";
            this.m_oReNumberButton.Size = new System.Drawing.Size(88, 23);
            this.m_oReNumberButton.TabIndex = 9;
            this.m_oReNumberButton.Text = "Re-Number";
            this.m_oReNumberButton.UseVisualStyleBackColor = true;
            // 
            // m_oAutoRenameButton
            // 
            this.m_oAutoRenameButton.Location = new System.Drawing.Point(98, 19);
            this.m_oAutoRenameButton.Name = "m_oAutoRenameButton";
            this.m_oAutoRenameButton.Size = new System.Drawing.Size(88, 23);
            this.m_oAutoRenameButton.TabIndex = 8;
            this.m_oAutoRenameButton.Text = "Auto Rename";
            this.m_oAutoRenameButton.UseVisualStyleBackColor = true;
            // 
            // m_oRandomNameButton
            // 
            this.m_oRandomNameButton.Location = new System.Drawing.Point(190, 19);
            this.m_oRandomNameButton.Name = "m_oRandomNameButton";
            this.m_oRandomNameButton.Size = new System.Drawing.Size(88, 23);
            this.m_oRandomNameButton.TabIndex = 7;
            this.m_oRandomNameButton.Text = "Random Name";
            this.m_oRandomNameButton.UseVisualStyleBackColor = true;
            // 
            // m_oDeleteButton
            // 
            this.m_oDeleteButton.Location = new System.Drawing.Point(98, 48);
            this.m_oDeleteButton.Name = "m_oDeleteButton";
            this.m_oDeleteButton.Size = new System.Drawing.Size(88, 23);
            this.m_oDeleteButton.TabIndex = 6;
            this.m_oDeleteButton.Text = "Delete";
            this.m_oDeleteButton.UseVisualStyleBackColor = true;
            // 
            // m_oNewHullButton
            // 
            this.m_oNewHullButton.Location = new System.Drawing.Point(374, 19);
            this.m_oNewHullButton.Name = "m_oNewHullButton";
            this.m_oNewHullButton.Size = new System.Drawing.Size(88, 23);
            this.m_oNewHullButton.TabIndex = 5;
            this.m_oNewHullButton.Text = "New Hull";
            this.m_oNewHullButton.UseVisualStyleBackColor = true;
            // 
            // m_oLockDesignButton
            // 
            this.m_oLockDesignButton.Location = new System.Drawing.Point(374, 48);
            this.m_oLockDesignButton.Name = "m_oLockDesignButton";
            this.m_oLockDesignButton.Size = new System.Drawing.Size(88, 23);
            this.m_oLockDesignButton.TabIndex = 4;
            this.m_oLockDesignButton.Text = "Lock Design";
            this.m_oLockDesignButton.UseVisualStyleBackColor = true;
            // 
            // m_oNewArmorButton
            // 
            this.m_oNewArmorButton.Location = new System.Drawing.Point(282, 48);
            this.m_oNewArmorButton.Name = "m_oNewArmorButton";
            this.m_oNewArmorButton.Size = new System.Drawing.Size(88, 23);
            this.m_oNewArmorButton.TabIndex = 3;
            this.m_oNewArmorButton.Text = "New Armour";
            this.m_oNewArmorButton.UseVisualStyleBackColor = true;
            // 
            // m_oDesignTechButton
            // 
            this.m_oDesignTechButton.Location = new System.Drawing.Point(466, 19);
            this.m_oDesignTechButton.Name = "m_oDesignTechButton";
            this.m_oDesignTechButton.Size = new System.Drawing.Size(88, 23);
            this.m_oDesignTechButton.TabIndex = 2;
            this.m_oDesignTechButton.Text = "Design Tech";
            this.m_oDesignTechButton.UseVisualStyleBackColor = true;
            // 
            // m_oNPRClassButton
            // 
            this.m_oNPRClassButton.Location = new System.Drawing.Point(190, 48);
            this.m_oNPRClassButton.Name = "m_oNPRClassButton";
            this.m_oNPRClassButton.Size = new System.Drawing.Size(88, 23);
            this.m_oNPRClassButton.TabIndex = 1;
            this.m_oNPRClassButton.Text = "NPR Class";
            this.m_oNPRClassButton.UseVisualStyleBackColor = true;
            // 
            // m_oRenameButton
            // 
            this.m_oRenameButton.Location = new System.Drawing.Point(6, 19);
            this.m_oRenameButton.Name = "m_oRenameButton";
            this.m_oRenameButton.Size = new System.Drawing.Size(88, 23);
            this.m_oRenameButton.TabIndex = 0;
            this.m_oRenameButton.Text = "Rename";
            this.m_oRenameButton.UseVisualStyleBackColor = true;
            // 
            // m_oTaskGroupTabControl
            // 
            this.m_oTaskGroupTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oSummaryTab);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oDesignTab);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oOrdFightersTab);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oCompSummaryTab);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oDACRankInfoTab);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oShipsTab);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oGlossaryTab);
            this.m_oTaskGroupTabControl.Location = new System.Drawing.Point(198, 69);
            this.m_oTaskGroupTabControl.MaximumSize = new System.Drawing.Size(1034, 751);
            this.m_oTaskGroupTabControl.MinimumSize = new System.Drawing.Size(1034, 751);
            this.m_oTaskGroupTabControl.Name = "m_oTaskGroupTabControl";
            this.m_oTaskGroupTabControl.SelectedIndex = 0;
            this.m_oTaskGroupTabControl.Size = new System.Drawing.Size(1034, 751);
            this.m_oTaskGroupTabControl.TabIndex = 41;
            // 
            // m_oSummaryTab
            // 
            this.m_oSummaryTab.Controls.Add(this.m_oArmorGroupBox);
            this.m_oSummaryTab.Location = new System.Drawing.Point(4, 22);
            this.m_oSummaryTab.Name = "m_oSummaryTab";
            this.m_oSummaryTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oSummaryTab.Size = new System.Drawing.Size(1026, 725);
            this.m_oSummaryTab.TabIndex = 1;
            this.m_oSummaryTab.Text = "Full Summary View";
            this.m_oSummaryTab.UseVisualStyleBackColor = true;
            // 
            // m_oArmorGroupBox
            // 
            this.m_oArmorGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oArmorGroupBox.Controls.Add(this.m_oClassSummaryGroupBox);
            this.m_oArmorGroupBox.Controls.Add(this.m_oTargetSpeedGroupBox);
            this.m_oArmorGroupBox.Controls.Add(this.m_oRangeBandsGroupBox);
            this.m_oArmorGroupBox.Location = new System.Drawing.Point(6, 6);
            this.m_oArmorGroupBox.Name = "m_oArmorGroupBox";
            this.m_oArmorGroupBox.Size = new System.Drawing.Size(1017, 713);
            this.m_oArmorGroupBox.TabIndex = 35;
            this.m_oArmorGroupBox.TabStop = false;
            // 
            // m_oClassSummaryGroupBox
            // 
            this.m_oClassSummaryGroupBox.Controls.Add(this.m_oClassSummaryTextBox);
            this.m_oClassSummaryGroupBox.Location = new System.Drawing.Point(6, 19);
            this.m_oClassSummaryGroupBox.Name = "m_oClassSummaryGroupBox";
            this.m_oClassSummaryGroupBox.Size = new System.Drawing.Size(878, 688);
            this.m_oClassSummaryGroupBox.TabIndex = 13;
            this.m_oClassSummaryGroupBox.TabStop = false;
            this.m_oClassSummaryGroupBox.Text = "Class Summary Display";
            // 
            // m_oClassSummaryTextBox
            // 
            this.m_oClassSummaryTextBox.Location = new System.Drawing.Point(6, 19);
            this.m_oClassSummaryTextBox.Name = "m_oClassSummaryTextBox";
            this.m_oClassSummaryTextBox.Size = new System.Drawing.Size(866, 663);
            this.m_oClassSummaryTextBox.TabIndex = 12;
            this.m_oClassSummaryTextBox.Text = "";
            // 
            // m_oTargetSpeedGroupBox
            // 
            this.m_oTargetSpeedGroupBox.Controls.Add(this.label3);
            this.m_oTargetSpeedGroupBox.Controls.Add(this.label2);
            this.m_oTargetSpeedGroupBox.Controls.Add(this.m_oSpeedCustomTextBox);
            this.m_oTargetSpeedGroupBox.Controls.Add(this.m_oSpeedCustomRadioButton);
            this.m_oTargetSpeedGroupBox.Controls.Add(this.m_oSpeed100000RadioButton);
            this.m_oTargetSpeedGroupBox.Controls.Add(this.m_oSpeed50000RadioButton);
            this.m_oTargetSpeedGroupBox.Controls.Add(this.m_oSpeed2000RadioButton);
            this.m_oTargetSpeedGroupBox.Controls.Add(this.m_oSpeed3000RadioButton);
            this.m_oTargetSpeedGroupBox.Controls.Add(this.m_oSpeed5000RadioButton);
            this.m_oTargetSpeedGroupBox.Controls.Add(this.m_oSpeed20000RadioButton);
            this.m_oTargetSpeedGroupBox.Controls.Add(this.m_oSpeed10000RadioButton);
            this.m_oTargetSpeedGroupBox.Controls.Add(this.m_oSpeed1000RadioButton);
            this.m_oTargetSpeedGroupBox.Location = new System.Drawing.Point(890, 294);
            this.m_oTargetSpeedGroupBox.Name = "m_oTargetSpeedGroupBox";
            this.m_oTargetSpeedGroupBox.Size = new System.Drawing.Size(121, 284);
            this.m_oTargetSpeedGroupBox.TabIndex = 11;
            this.m_oTargetSpeedGroupBox.TabStop = false;
            this.m_oTargetSpeedGroupBox.Text = "Target Speed";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(79, 201);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(40, 13);
            this.label3.TabIndex = 22;
            this.label3.Text = "k km/s";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(5, 224);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 13);
            this.label2.TabIndex = 21;
            this.label2.Text = "(Fractions OK)";
            // 
            // m_oSpeedCustomTextBox
            // 
            this.m_oSpeedCustomTextBox.Location = new System.Drawing.Point(27, 198);
            this.m_oSpeedCustomTextBox.Name = "m_oSpeedCustomTextBox";
            this.m_oSpeedCustomTextBox.Size = new System.Drawing.Size(52, 20);
            this.m_oSpeedCustomTextBox.TabIndex = 20;
            // 
            // m_oSpeedCustomRadioButton
            // 
            this.m_oSpeedCustomRadioButton.AutoSize = true;
            this.m_oSpeedCustomRadioButton.Location = new System.Drawing.Point(6, 201);
            this.m_oSpeedCustomRadioButton.Name = "m_oSpeedCustomRadioButton";
            this.m_oSpeedCustomRadioButton.Size = new System.Drawing.Size(14, 13);
            this.m_oSpeedCustomRadioButton.TabIndex = 19;
            this.m_oSpeedCustomRadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oSpeed100000RadioButton
            // 
            this.m_oSpeed100000RadioButton.AutoSize = true;
            this.m_oSpeed100000RadioButton.Location = new System.Drawing.Point(6, 178);
            this.m_oSpeed100000RadioButton.Name = "m_oSpeed100000RadioButton";
            this.m_oSpeed100000RadioButton.Size = new System.Drawing.Size(91, 17);
            this.m_oSpeed100000RadioButton.TabIndex = 18;
            this.m_oSpeed100000RadioButton.Text = "100,000 km/s";
            this.m_oSpeed100000RadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oSpeed50000RadioButton
            // 
            this.m_oSpeed50000RadioButton.AutoSize = true;
            this.m_oSpeed50000RadioButton.Location = new System.Drawing.Point(5, 155);
            this.m_oSpeed50000RadioButton.Name = "m_oSpeed50000RadioButton";
            this.m_oSpeed50000RadioButton.Size = new System.Drawing.Size(85, 17);
            this.m_oSpeed50000RadioButton.TabIndex = 17;
            this.m_oSpeed50000RadioButton.Text = "50,000 km/s";
            this.m_oSpeed50000RadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oSpeed2000RadioButton
            // 
            this.m_oSpeed2000RadioButton.AutoSize = true;
            this.m_oSpeed2000RadioButton.Location = new System.Drawing.Point(6, 40);
            this.m_oSpeed2000RadioButton.Name = "m_oSpeed2000RadioButton";
            this.m_oSpeed2000RadioButton.Size = new System.Drawing.Size(76, 17);
            this.m_oSpeed2000RadioButton.TabIndex = 16;
            this.m_oSpeed2000RadioButton.Text = "2000 km/s";
            this.m_oSpeed2000RadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oSpeed3000RadioButton
            // 
            this.m_oSpeed3000RadioButton.AutoSize = true;
            this.m_oSpeed3000RadioButton.Location = new System.Drawing.Point(6, 63);
            this.m_oSpeed3000RadioButton.Name = "m_oSpeed3000RadioButton";
            this.m_oSpeed3000RadioButton.Size = new System.Drawing.Size(76, 17);
            this.m_oSpeed3000RadioButton.TabIndex = 15;
            this.m_oSpeed3000RadioButton.Text = "3000 km/s";
            this.m_oSpeed3000RadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oSpeed5000RadioButton
            // 
            this.m_oSpeed5000RadioButton.AutoSize = true;
            this.m_oSpeed5000RadioButton.Location = new System.Drawing.Point(6, 86);
            this.m_oSpeed5000RadioButton.Name = "m_oSpeed5000RadioButton";
            this.m_oSpeed5000RadioButton.Size = new System.Drawing.Size(76, 17);
            this.m_oSpeed5000RadioButton.TabIndex = 14;
            this.m_oSpeed5000RadioButton.Text = "5000 km/s";
            this.m_oSpeed5000RadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oSpeed20000RadioButton
            // 
            this.m_oSpeed20000RadioButton.AutoSize = true;
            this.m_oSpeed20000RadioButton.Location = new System.Drawing.Point(6, 132);
            this.m_oSpeed20000RadioButton.Name = "m_oSpeed20000RadioButton";
            this.m_oSpeed20000RadioButton.Size = new System.Drawing.Size(85, 17);
            this.m_oSpeed20000RadioButton.TabIndex = 13;
            this.m_oSpeed20000RadioButton.Text = "20,000 km/s";
            this.m_oSpeed20000RadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oSpeed10000RadioButton
            // 
            this.m_oSpeed10000RadioButton.AutoSize = true;
            this.m_oSpeed10000RadioButton.Location = new System.Drawing.Point(6, 109);
            this.m_oSpeed10000RadioButton.Name = "m_oSpeed10000RadioButton";
            this.m_oSpeed10000RadioButton.Size = new System.Drawing.Size(85, 17);
            this.m_oSpeed10000RadioButton.TabIndex = 12;
            this.m_oSpeed10000RadioButton.Text = "10,000 km/s";
            this.m_oSpeed10000RadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oSpeed1000RadioButton
            // 
            this.m_oSpeed1000RadioButton.AutoSize = true;
            this.m_oSpeed1000RadioButton.Checked = true;
            this.m_oSpeed1000RadioButton.Location = new System.Drawing.Point(6, 17);
            this.m_oSpeed1000RadioButton.Name = "m_oSpeed1000RadioButton";
            this.m_oSpeed1000RadioButton.Size = new System.Drawing.Size(76, 17);
            this.m_oSpeed1000RadioButton.TabIndex = 11;
            this.m_oSpeed1000RadioButton.TabStop = true;
            this.m_oSpeed1000RadioButton.Text = "1000 km/s";
            this.m_oSpeed1000RadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oRangeBandsGroupBox
            // 
            this.m_oRangeBandsGroupBox.Controls.Add(this.label1);
            this.m_oRangeBandsGroupBox.Controls.Add(this.m_oRangeCustomTextBox);
            this.m_oRangeBandsGroupBox.Controls.Add(this.m_oRangeCustomRadioButton);
            this.m_oRangeBandsGroupBox.Controls.Add(this.m_oRange1000000RadioButton);
            this.m_oRangeBandsGroupBox.Controls.Add(this.m_oRange500000RadioButton);
            this.m_oRangeBandsGroupBox.Controls.Add(this.m_oRange20000RadioButton);
            this.m_oRangeBandsGroupBox.Controls.Add(this.m_oRange30000RadioButton);
            this.m_oRangeBandsGroupBox.Controls.Add(this.m_oRange50000RadioButton);
            this.m_oRangeBandsGroupBox.Controls.Add(this.m_oRange200000RadioButton);
            this.m_oRangeBandsGroupBox.Controls.Add(this.m_oTange100000RadioButton);
            this.m_oRangeBandsGroupBox.Controls.Add(this.m_oRange10000RadioButton);
            this.m_oRangeBandsGroupBox.Location = new System.Drawing.Point(890, 14);
            this.m_oRangeBandsGroupBox.Name = "m_oRangeBandsGroupBox";
            this.m_oRangeBandsGroupBox.Size = new System.Drawing.Size(121, 274);
            this.m_oRangeBandsGroupBox.TabIndex = 2;
            this.m_oRangeBandsGroupBox.TabStop = false;
            this.m_oRangeBandsGroupBox.Text = "Range Bands";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 223);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "(Units of 10k km)";
            // 
            // m_oRangeCustomTextBox
            // 
            this.m_oRangeCustomTextBox.Location = new System.Drawing.Point(27, 197);
            this.m_oRangeCustomTextBox.Name = "m_oRangeCustomTextBox";
            this.m_oRangeCustomTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oRangeCustomTextBox.TabIndex = 9;
            // 
            // m_oRangeCustomRadioButton
            // 
            this.m_oRangeCustomRadioButton.AutoSize = true;
            this.m_oRangeCustomRadioButton.Location = new System.Drawing.Point(7, 200);
            this.m_oRangeCustomRadioButton.Name = "m_oRangeCustomRadioButton";
            this.m_oRangeCustomRadioButton.Size = new System.Drawing.Size(14, 13);
            this.m_oRangeCustomRadioButton.TabIndex = 8;
            this.m_oRangeCustomRadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oRange1000000RadioButton
            // 
            this.m_oRange1000000RadioButton.AutoSize = true;
            this.m_oRange1000000RadioButton.Location = new System.Drawing.Point(7, 177);
            this.m_oRange1000000RadioButton.Name = "m_oRange1000000RadioButton";
            this.m_oRange1000000RadioButton.Size = new System.Drawing.Size(90, 17);
            this.m_oRange1000000RadioButton.TabIndex = 7;
            this.m_oRange1000000RadioButton.Text = "1,000,000 km";
            this.m_oRange1000000RadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oRange500000RadioButton
            // 
            this.m_oRange500000RadioButton.AutoSize = true;
            this.m_oRange500000RadioButton.Location = new System.Drawing.Point(6, 154);
            this.m_oRange500000RadioButton.Name = "m_oRange500000RadioButton";
            this.m_oRange500000RadioButton.Size = new System.Drawing.Size(81, 17);
            this.m_oRange500000RadioButton.TabIndex = 6;
            this.m_oRange500000RadioButton.Text = "500,000 km";
            this.m_oRange500000RadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oRange20000RadioButton
            // 
            this.m_oRange20000RadioButton.AutoSize = true;
            this.m_oRange20000RadioButton.Location = new System.Drawing.Point(7, 39);
            this.m_oRange20000RadioButton.Name = "m_oRange20000RadioButton";
            this.m_oRange20000RadioButton.Size = new System.Drawing.Size(75, 17);
            this.m_oRange20000RadioButton.TabIndex = 5;
            this.m_oRange20000RadioButton.Text = "20,000 km";
            this.m_oRange20000RadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oRange30000RadioButton
            // 
            this.m_oRange30000RadioButton.AutoSize = true;
            this.m_oRange30000RadioButton.Location = new System.Drawing.Point(7, 62);
            this.m_oRange30000RadioButton.Name = "m_oRange30000RadioButton";
            this.m_oRange30000RadioButton.Size = new System.Drawing.Size(75, 17);
            this.m_oRange30000RadioButton.TabIndex = 4;
            this.m_oRange30000RadioButton.Text = "30,000 km";
            this.m_oRange30000RadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oRange50000RadioButton
            // 
            this.m_oRange50000RadioButton.AutoSize = true;
            this.m_oRange50000RadioButton.Location = new System.Drawing.Point(7, 85);
            this.m_oRange50000RadioButton.Name = "m_oRange50000RadioButton";
            this.m_oRange50000RadioButton.Size = new System.Drawing.Size(75, 17);
            this.m_oRange50000RadioButton.TabIndex = 3;
            this.m_oRange50000RadioButton.Text = "50,000 km";
            this.m_oRange50000RadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oRange200000RadioButton
            // 
            this.m_oRange200000RadioButton.AutoSize = true;
            this.m_oRange200000RadioButton.Location = new System.Drawing.Point(7, 131);
            this.m_oRange200000RadioButton.Name = "m_oRange200000RadioButton";
            this.m_oRange200000RadioButton.Size = new System.Drawing.Size(81, 17);
            this.m_oRange200000RadioButton.TabIndex = 2;
            this.m_oRange200000RadioButton.Text = "200,000 km";
            this.m_oRange200000RadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oTange100000RadioButton
            // 
            this.m_oTange100000RadioButton.AutoSize = true;
            this.m_oTange100000RadioButton.Location = new System.Drawing.Point(7, 108);
            this.m_oTange100000RadioButton.Name = "m_oTange100000RadioButton";
            this.m_oTange100000RadioButton.Size = new System.Drawing.Size(81, 17);
            this.m_oTange100000RadioButton.TabIndex = 1;
            this.m_oTange100000RadioButton.Text = "100,000 km";
            this.m_oTange100000RadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oRange10000RadioButton
            // 
            this.m_oRange10000RadioButton.AutoSize = true;
            this.m_oRange10000RadioButton.Checked = true;
            this.m_oRange10000RadioButton.Location = new System.Drawing.Point(7, 16);
            this.m_oRange10000RadioButton.Name = "m_oRange10000RadioButton";
            this.m_oRange10000RadioButton.Size = new System.Drawing.Size(75, 17);
            this.m_oRange10000RadioButton.TabIndex = 0;
            this.m_oRange10000RadioButton.TabStop = true;
            this.m_oRange10000RadioButton.Text = "10,000 km";
            this.m_oRange10000RadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oDesignTab
            // 
            this.m_oDesignTab.Location = new System.Drawing.Point(4, 22);
            this.m_oDesignTab.Name = "m_oDesignTab";
            this.m_oDesignTab.Size = new System.Drawing.Size(1026, 725);
            this.m_oDesignTab.TabIndex = 2;
            this.m_oDesignTab.Text = "Design View";
            this.m_oDesignTab.UseVisualStyleBackColor = true;
            // 
            // m_oOrdFightersTab
            // 
            this.m_oOrdFightersTab.Location = new System.Drawing.Point(4, 22);
            this.m_oOrdFightersTab.Name = "m_oOrdFightersTab";
            this.m_oOrdFightersTab.Size = new System.Drawing.Size(1026, 725);
            this.m_oOrdFightersTab.TabIndex = 3;
            this.m_oOrdFightersTab.Text = "Ordnance / Fighters";
            this.m_oOrdFightersTab.UseVisualStyleBackColor = true;
            // 
            // m_oCompSummaryTab
            // 
            this.m_oCompSummaryTab.Location = new System.Drawing.Point(4, 22);
            this.m_oCompSummaryTab.Name = "m_oCompSummaryTab";
            this.m_oCompSummaryTab.Size = new System.Drawing.Size(1026, 725);
            this.m_oCompSummaryTab.TabIndex = 4;
            this.m_oCompSummaryTab.Text = "Component Summary";
            this.m_oCompSummaryTab.UseVisualStyleBackColor = true;
            // 
            // m_oDACRankInfoTab
            // 
            this.m_oDACRankInfoTab.Location = new System.Drawing.Point(4, 22);
            this.m_oDACRankInfoTab.Name = "m_oDACRankInfoTab";
            this.m_oDACRankInfoTab.Size = new System.Drawing.Size(1026, 725);
            this.m_oDACRankInfoTab.TabIndex = 5;
            this.m_oDACRankInfoTab.Text = "DAC / Rank / Info";
            this.m_oDACRankInfoTab.UseVisualStyleBackColor = true;
            // 
            // m_oShipsTab
            // 
            this.m_oShipsTab.Location = new System.Drawing.Point(4, 22);
            this.m_oShipsTab.Name = "m_oShipsTab";
            this.m_oShipsTab.Size = new System.Drawing.Size(1026, 725);
            this.m_oShipsTab.TabIndex = 6;
            this.m_oShipsTab.Text = "Ships in Class";
            this.m_oShipsTab.UseVisualStyleBackColor = true;
            // 
            // m_oGlossaryTab
            // 
            this.m_oGlossaryTab.Location = new System.Drawing.Point(4, 22);
            this.m_oGlossaryTab.Name = "m_oGlossaryTab";
            this.m_oGlossaryTab.Size = new System.Drawing.Size(1026, 725);
            this.m_oGlossaryTab.TabIndex = 7;
            this.m_oGlossaryTab.Text = "Glossary";
            this.m_oGlossaryTab.UseVisualStyleBackColor = true;
            // 
            // m_oArmourUpButton
            // 
            this.m_oArmourUpButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oArmourUpButton.Location = new System.Drawing.Point(166, 10);
            this.m_oArmourUpButton.Name = "m_oArmourUpButton";
            this.m_oArmourUpButton.Size = new System.Drawing.Size(14, 14);
            this.m_oArmourUpButton.TabIndex = 20;
            this.m_oArmourUpButton.Text = "^";
            this.m_oArmourUpButton.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.m_oArmourUpButton.UseVisualStyleBackColor = true;
            // 
            // m_oArmourDownButton
            // 
            this.m_oArmourDownButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 4F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oArmourDownButton.Location = new System.Drawing.Point(166, 21);
            this.m_oArmourDownButton.Name = "m_oArmourDownButton";
            this.m_oArmourDownButton.Size = new System.Drawing.Size(14, 14);
            this.m_oArmourDownButton.TabIndex = 41;
            this.m_oArmourDownButton.Text = "v";
            this.m_oArmourDownButton.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.m_oArmourDownButton.UseVisualStyleBackColor = true;
            // 
            // m_oGeneralInfoGroupBox
            // 
            this.m_oGeneralInfoGroupBox.Controls.Add(this.m_oEmpireLabel);
            this.m_oGeneralInfoGroupBox.Controls.Add(this.m_oFactionComboBox);
            this.m_oGeneralInfoGroupBox.Controls.Add(this.m_oClassLabel);
            this.m_oGeneralInfoGroupBox.Controls.Add(this.m_oClassComboBox);
            this.m_oGeneralInfoGroupBox.Controls.Add(this.m_oTypeLabel);
            this.m_oGeneralInfoGroupBox.Controls.Add(this.m_oTypeComboBox);
            this.m_oGeneralInfoGroupBox.Controls.Add(this.m_oHullLabel);
            this.m_oGeneralInfoGroupBox.Controls.Add(this.m_oHullComboBox);
            this.m_oGeneralInfoGroupBox.Location = new System.Drawing.Point(10, 12);
            this.m_oGeneralInfoGroupBox.Name = "m_oGeneralInfoGroupBox";
            this.m_oGeneralInfoGroupBox.Size = new System.Drawing.Size(705, 51);
            this.m_oGeneralInfoGroupBox.TabIndex = 42;
            this.m_oGeneralInfoGroupBox.TabStop = false;
            // 
            // m_oBuildPointGroupBox
            // 
            this.m_oBuildPointGroupBox.Location = new System.Drawing.Point(721, 12);
            this.m_oBuildPointGroupBox.Name = "m_oBuildPointGroupBox";
            this.m_oBuildPointGroupBox.Size = new System.Drawing.Size(150, 51);
            this.m_oBuildPointGroupBox.TabIndex = 44;
            this.m_oBuildPointGroupBox.TabStop = false;
            // 
            // m_oBuildLoadGroupBox
            // 
            this.m_oBuildLoadGroupBox.Controls.Add(this.m_oLoadTimeLabel);
            this.m_oBuildLoadGroupBox.Controls.Add(this.m_oBuildTimeLabel);
            this.m_oBuildLoadGroupBox.Controls.Add(this.m_oLoadTimeTextBox);
            this.m_oBuildLoadGroupBox.Controls.Add(this.m_oBuildTimeTextBox);
            this.m_oBuildLoadGroupBox.Location = new System.Drawing.Point(10, 702);
            this.m_oBuildLoadGroupBox.Margin = new System.Windows.Forms.Padding(1);
            this.m_oBuildLoadGroupBox.Name = "m_oBuildLoadGroupBox";
            this.m_oBuildLoadGroupBox.Size = new System.Drawing.Size(185, 72);
            this.m_oBuildLoadGroupBox.TabIndex = 9;
            this.m_oBuildLoadGroupBox.TabStop = false;
            this.m_oBuildLoadGroupBox.Text = "Build and Load Time";
            // 
            // m_oLoadTimeLabel
            // 
            this.m_oLoadTimeLabel.AutoSize = true;
            this.m_oLoadTimeLabel.Location = new System.Drawing.Point(9, 48);
            this.m_oLoadTimeLabel.Name = "m_oLoadTimeLabel";
            this.m_oLoadTimeLabel.Size = new System.Drawing.Size(91, 13);
            this.m_oLoadTimeLabel.TabIndex = 13;
            this.m_oLoadTimeLabel.Text = "Load Time (DHM)";
            // 
            // m_oBuildTimeLabel
            // 
            this.m_oBuildTimeLabel.AutoSize = true;
            this.m_oBuildTimeLabel.Location = new System.Drawing.Point(9, 22);
            this.m_oBuildTimeLabel.Name = "m_oBuildTimeLabel";
            this.m_oBuildTimeLabel.Size = new System.Drawing.Size(78, 13);
            this.m_oBuildTimeLabel.TabIndex = 8;
            this.m_oBuildTimeLabel.Text = "Build Time (yrs)";
            // 
            // m_oLoadTimeTextBox
            // 
            this.m_oLoadTimeTextBox.Location = new System.Drawing.Point(108, 45);
            this.m_oLoadTimeTextBox.Name = "m_oLoadTimeTextBox";
            this.m_oLoadTimeTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oLoadTimeTextBox.TabIndex = 12;
            // 
            // m_oBuildTimeTextBox
            // 
            this.m_oBuildTimeTextBox.Location = new System.Drawing.Point(108, 19);
            this.m_oBuildTimeTextBox.Name = "m_oBuildTimeTextBox";
            this.m_oBuildTimeTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oBuildTimeTextBox.TabIndex = 11;
            // 
            // m_oPowerSystemGroupBox
            // 
            this.m_oPowerSystemGroupBox.Controls.Add(this.m_oRequiredPowerLabel);
            this.m_oPowerSystemGroupBox.Controls.Add(this.m_oReactorPowerLabel);
            this.m_oPowerSystemGroupBox.Controls.Add(this.m_oJumpDistLabel);
            this.m_oPowerSystemGroupBox.Controls.Add(this.m_oJumpRatingLabel);
            this.m_oPowerSystemGroupBox.Controls.Add(this.m_oMaxSpeedLabel);
            this.m_oPowerSystemGroupBox.Controls.Add(this.m_oEnginePowerLabel);
            this.m_oPowerSystemGroupBox.Controls.Add(this.m_oRequiredPowerTextBox);
            this.m_oPowerSystemGroupBox.Controls.Add(this.m_oReactorPowerTextBox);
            this.m_oPowerSystemGroupBox.Controls.Add(this.m_oJumpDistTextBox);
            this.m_oPowerSystemGroupBox.Controls.Add(this.m_oJumpRatingTextBox);
            this.m_oPowerSystemGroupBox.Controls.Add(this.m_oMaxSpeedTextBox);
            this.m_oPowerSystemGroupBox.Controls.Add(this.m_oEnginePowerTextBox);
            this.m_oPowerSystemGroupBox.Location = new System.Drawing.Point(10, 524);
            this.m_oPowerSystemGroupBox.Margin = new System.Windows.Forms.Padding(1);
            this.m_oPowerSystemGroupBox.Name = "m_oPowerSystemGroupBox";
            this.m_oPowerSystemGroupBox.Size = new System.Drawing.Size(186, 175);
            this.m_oPowerSystemGroupBox.TabIndex = 10;
            this.m_oPowerSystemGroupBox.TabStop = false;
            this.m_oPowerSystemGroupBox.Text = "Power Systems";
            // 
            // m_oRequiredPowerLabel
            // 
            this.m_oRequiredPowerLabel.AutoSize = true;
            this.m_oRequiredPowerLabel.Location = new System.Drawing.Point(6, 152);
            this.m_oRequiredPowerLabel.Name = "m_oRequiredPowerLabel";
            this.m_oRequiredPowerLabel.Size = new System.Drawing.Size(83, 13);
            this.m_oRequiredPowerLabel.TabIndex = 24;
            this.m_oRequiredPowerLabel.Text = "Required Power";
            // 
            // m_oReactorPowerLabel
            // 
            this.m_oReactorPowerLabel.AutoSize = true;
            this.m_oReactorPowerLabel.Location = new System.Drawing.Point(6, 126);
            this.m_oReactorPowerLabel.Name = "m_oReactorPowerLabel";
            this.m_oReactorPowerLabel.Size = new System.Drawing.Size(78, 13);
            this.m_oReactorPowerLabel.TabIndex = 23;
            this.m_oReactorPowerLabel.Text = "Reactor Power";
            // 
            // m_oJumpDistLabel
            // 
            this.m_oJumpDistLabel.AutoSize = true;
            this.m_oJumpDistLabel.Location = new System.Drawing.Point(6, 100);
            this.m_oJumpDistLabel.Name = "m_oJumpDistLabel";
            this.m_oJumpDistLabel.Size = new System.Drawing.Size(77, 13);
            this.m_oJumpDistLabel.TabIndex = 22;
            this.m_oJumpDistLabel.Text = "Jump Distance";
            // 
            // m_oJumpRatingLabel
            // 
            this.m_oJumpRatingLabel.AutoSize = true;
            this.m_oJumpRatingLabel.Location = new System.Drawing.Point(6, 74);
            this.m_oJumpRatingLabel.Name = "m_oJumpRatingLabel";
            this.m_oJumpRatingLabel.Size = new System.Drawing.Size(66, 13);
            this.m_oJumpRatingLabel.TabIndex = 21;
            this.m_oJumpRatingLabel.Text = "Jump Rating";
            // 
            // m_oMaxSpeedLabel
            // 
            this.m_oMaxSpeedLabel.AutoSize = true;
            this.m_oMaxSpeedLabel.Location = new System.Drawing.Point(6, 48);
            this.m_oMaxSpeedLabel.Name = "m_oMaxSpeedLabel";
            this.m_oMaxSpeedLabel.Size = new System.Drawing.Size(61, 13);
            this.m_oMaxSpeedLabel.TabIndex = 20;
            this.m_oMaxSpeedLabel.Text = "Max Speed";
            // 
            // m_oEnginePowerLabel
            // 
            this.m_oEnginePowerLabel.AutoSize = true;
            this.m_oEnginePowerLabel.Location = new System.Drawing.Point(6, 22);
            this.m_oEnginePowerLabel.Name = "m_oEnginePowerLabel";
            this.m_oEnginePowerLabel.Size = new System.Drawing.Size(73, 13);
            this.m_oEnginePowerLabel.TabIndex = 14;
            this.m_oEnginePowerLabel.Text = "Engine Power";
            // 
            // m_oRequiredPowerTextBox
            // 
            this.m_oRequiredPowerTextBox.Location = new System.Drawing.Point(108, 149);
            this.m_oRequiredPowerTextBox.Name = "m_oRequiredPowerTextBox";
            this.m_oRequiredPowerTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oRequiredPowerTextBox.TabIndex = 19;
            // 
            // m_oReactorPowerTextBox
            // 
            this.m_oReactorPowerTextBox.Location = new System.Drawing.Point(108, 123);
            this.m_oReactorPowerTextBox.Name = "m_oReactorPowerTextBox";
            this.m_oReactorPowerTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oReactorPowerTextBox.TabIndex = 18;
            // 
            // m_oJumpDistTextBox
            // 
            this.m_oJumpDistTextBox.Location = new System.Drawing.Point(108, 97);
            this.m_oJumpDistTextBox.Name = "m_oJumpDistTextBox";
            this.m_oJumpDistTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oJumpDistTextBox.TabIndex = 17;
            // 
            // m_oJumpRatingTextBox
            // 
            this.m_oJumpRatingTextBox.Location = new System.Drawing.Point(108, 71);
            this.m_oJumpRatingTextBox.Name = "m_oJumpRatingTextBox";
            this.m_oJumpRatingTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oJumpRatingTextBox.TabIndex = 16;
            // 
            // m_oMaxSpeedTextBox
            // 
            this.m_oMaxSpeedTextBox.Location = new System.Drawing.Point(108, 45);
            this.m_oMaxSpeedTextBox.Name = "m_oMaxSpeedTextBox";
            this.m_oMaxSpeedTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oMaxSpeedTextBox.TabIndex = 15;
            // 
            // m_oEnginePowerTextBox
            // 
            this.m_oEnginePowerTextBox.Location = new System.Drawing.Point(108, 19);
            this.m_oEnginePowerTextBox.Name = "m_oEnginePowerTextBox";
            this.m_oEnginePowerTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oEnginePowerTextBox.TabIndex = 14;
            // 
            // m_oPassiveDefGroupBox
            // 
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oArmourDownButton);
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oArmourUpButton);
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oArmorRatingLabel);
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oArmorRatingTextBox);
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oClassSizeTextBox);
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oClassSizeLabel);
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oIHTKLabel);
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oAAreaLabel);
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oSRechargeLabel);
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oAAreaTextBox);
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oSStrengthLabel);
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oAStrengthTextBox);
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oAColumnLabel);
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oAColumnsTextBox);
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oAStrengthLabel);
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oSStrengthTextBox);
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oSRechargeTextBox);
            this.m_oPassiveDefGroupBox.Controls.Add(this.m_oInternalHTKTextBox);
            this.m_oPassiveDefGroupBox.Location = new System.Drawing.Point(10, 298);
            this.m_oPassiveDefGroupBox.Margin = new System.Windows.Forms.Padding(1);
            this.m_oPassiveDefGroupBox.Name = "m_oPassiveDefGroupBox";
            this.m_oPassiveDefGroupBox.Size = new System.Drawing.Size(185, 224);
            this.m_oPassiveDefGroupBox.TabIndex = 10;
            this.m_oPassiveDefGroupBox.TabStop = false;
            this.m_oPassiveDefGroupBox.Text = "Passive Defences";
            // 
            // m_oArmorRatingLabel
            // 
            this.m_oArmorRatingLabel.AutoSize = true;
            this.m_oArmorRatingLabel.Location = new System.Drawing.Point(6, 18);
            this.m_oArmorRatingLabel.Name = "m_oArmorRatingLabel";
            this.m_oArmorRatingLabel.Size = new System.Drawing.Size(74, 13);
            this.m_oArmorRatingLabel.TabIndex = 38;
            this.m_oArmorRatingLabel.Text = "Armour Rating";
            // 
            // m_oArmorRatingTextBox
            // 
            this.m_oArmorRatingTextBox.Location = new System.Drawing.Point(108, 15);
            this.m_oArmorRatingTextBox.Name = "m_oArmorRatingTextBox";
            this.m_oArmorRatingTextBox.Size = new System.Drawing.Size(52, 20);
            this.m_oArmorRatingTextBox.TabIndex = 37;
            // 
            // m_oClassSizeTextBox
            // 
            this.m_oClassSizeTextBox.Location = new System.Drawing.Point(108, 41);
            this.m_oClassSizeTextBox.Name = "m_oClassSizeTextBox";
            this.m_oClassSizeTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oClassSizeTextBox.TabIndex = 39;
            // 
            // m_oClassSizeLabel
            // 
            this.m_oClassSizeLabel.AutoSize = true;
            this.m_oClassSizeLabel.Location = new System.Drawing.Point(6, 44);
            this.m_oClassSizeLabel.Name = "m_oClassSizeLabel";
            this.m_oClassSizeLabel.Size = new System.Drawing.Size(85, 13);
            this.m_oClassSizeLabel.TabIndex = 40;
            this.m_oClassSizeLabel.Text = "Exact Class Size";
            // 
            // m_oIHTKLabel
            // 
            this.m_oIHTKLabel.AutoSize = true;
            this.m_oIHTKLabel.Location = new System.Drawing.Point(6, 200);
            this.m_oIHTKLabel.Name = "m_oIHTKLabel";
            this.m_oIHTKLabel.Size = new System.Drawing.Size(67, 13);
            this.m_oIHTKLabel.TabIndex = 36;
            this.m_oIHTKLabel.Text = "Internal HTK";
            // 
            // m_oAAreaLabel
            // 
            this.m_oAAreaLabel.AutoSize = true;
            this.m_oAAreaLabel.Location = new System.Drawing.Point(6, 70);
            this.m_oAAreaLabel.Name = "m_oAAreaLabel";
            this.m_oAAreaLabel.Size = new System.Drawing.Size(65, 13);
            this.m_oAAreaLabel.TabIndex = 26;
            this.m_oAAreaLabel.Text = "Armour Area";
            // 
            // m_oSRechargeLabel
            // 
            this.m_oSRechargeLabel.AutoSize = true;
            this.m_oSRechargeLabel.Location = new System.Drawing.Point(6, 174);
            this.m_oSRechargeLabel.Name = "m_oSRechargeLabel";
            this.m_oSRechargeLabel.Size = new System.Drawing.Size(86, 13);
            this.m_oSRechargeLabel.TabIndex = 35;
            this.m_oSRechargeLabel.Text = "Shield Recharge";
            // 
            // m_oAAreaTextBox
            // 
            this.m_oAAreaTextBox.Location = new System.Drawing.Point(108, 67);
            this.m_oAAreaTextBox.Name = "m_oAAreaTextBox";
            this.m_oAAreaTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oAAreaTextBox.TabIndex = 25;
            // 
            // m_oSStrengthLabel
            // 
            this.m_oSStrengthLabel.AutoSize = true;
            this.m_oSStrengthLabel.Location = new System.Drawing.Point(6, 148);
            this.m_oSStrengthLabel.Name = "m_oSStrengthLabel";
            this.m_oSStrengthLabel.Size = new System.Drawing.Size(79, 13);
            this.m_oSStrengthLabel.TabIndex = 34;
            this.m_oSStrengthLabel.Text = "Shield Strength";
            // 
            // m_oAStrengthTextBox
            // 
            this.m_oAStrengthTextBox.Location = new System.Drawing.Point(108, 93);
            this.m_oAStrengthTextBox.Name = "m_oAStrengthTextBox";
            this.m_oAStrengthTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oAStrengthTextBox.TabIndex = 27;
            // 
            // m_oAColumnLabel
            // 
            this.m_oAColumnLabel.AutoSize = true;
            this.m_oAColumnLabel.Location = new System.Drawing.Point(6, 122);
            this.m_oAColumnLabel.Name = "m_oAColumnLabel";
            this.m_oAColumnLabel.Size = new System.Drawing.Size(83, 13);
            this.m_oAColumnLabel.TabIndex = 33;
            this.m_oAColumnLabel.Text = "Armour Columns";
            // 
            // m_oAColumnsTextBox
            // 
            this.m_oAColumnsTextBox.Location = new System.Drawing.Point(108, 119);
            this.m_oAColumnsTextBox.Name = "m_oAColumnsTextBox";
            this.m_oAColumnsTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oAColumnsTextBox.TabIndex = 28;
            // 
            // m_oAStrengthLabel
            // 
            this.m_oAStrengthLabel.AutoSize = true;
            this.m_oAStrengthLabel.Location = new System.Drawing.Point(6, 96);
            this.m_oAStrengthLabel.Name = "m_oAStrengthLabel";
            this.m_oAStrengthLabel.Size = new System.Drawing.Size(83, 13);
            this.m_oAStrengthLabel.TabIndex = 32;
            this.m_oAStrengthLabel.Text = "Armour Strength";
            // 
            // m_oSStrengthTextBox
            // 
            this.m_oSStrengthTextBox.Location = new System.Drawing.Point(108, 145);
            this.m_oSStrengthTextBox.Name = "m_oSStrengthTextBox";
            this.m_oSStrengthTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oSStrengthTextBox.TabIndex = 29;
            // 
            // m_oSRechargeTextBox
            // 
            this.m_oSRechargeTextBox.Location = new System.Drawing.Point(108, 171);
            this.m_oSRechargeTextBox.Name = "m_oSRechargeTextBox";
            this.m_oSRechargeTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oSRechargeTextBox.TabIndex = 30;
            // 
            // m_oInternalHTKTextBox
            // 
            this.m_oInternalHTKTextBox.Location = new System.Drawing.Point(108, 197);
            this.m_oInternalHTKTextBox.Name = "m_oInternalHTKTextBox";
            this.m_oInternalHTKTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oInternalHTKTextBox.TabIndex = 31;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.m_oDeployTimeLabel);
            this.groupBox4.Controls.Add(this.m_oDeployTimeTextBox);
            this.groupBox4.Controls.Add(this.m_oTonsManTextBox);
            this.groupBox4.Controls.Add(this.m_oTonPerManLabel);
            this.groupBox4.Controls.Add(this.m_oCryoBerthsLabel);
            this.groupBox4.Controls.Add(this.m_oCapPerHSLabel);
            this.groupBox4.Controls.Add(this.m_oSpareBerthsLabel);
            this.groupBox4.Controls.Add(this.m_oCapPerHSTextBox);
            this.groupBox4.Controls.Add(this.m_oCrewBerthsLabel);
            this.groupBox4.Controls.Add(this.m_oAccomHSReqTextBox);
            this.groupBox4.Controls.Add(this.m_oAccomHSAvailLabel);
            this.groupBox4.Controls.Add(this.m_oAccomHSAvailTextBox);
            this.groupBox4.Controls.Add(this.m_oAccomHSReqLabel);
            this.groupBox4.Controls.Add(this.m_oCrewBerthsTextBox);
            this.groupBox4.Controls.Add(this.m_oSpareBerthsTextBox);
            this.groupBox4.Controls.Add(this.m_oCryoBerthsTextBox);
            this.groupBox4.Location = new System.Drawing.Point(10, 67);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(1);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(182, 229);
            this.groupBox4.TabIndex = 10;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Crew Accommodations";
            // 
            // m_oDeployTimeLabel
            // 
            this.m_oDeployTimeLabel.AutoSize = true;
            this.m_oDeployTimeLabel.Location = new System.Drawing.Point(6, 21);
            this.m_oDeployTimeLabel.Name = "m_oDeployTimeLabel";
            this.m_oDeployTimeLabel.Size = new System.Drawing.Size(89, 13);
            this.m_oDeployTimeLabel.TabIndex = 54;
            this.m_oDeployTimeLabel.Text = "Deployment Time";
            // 
            // m_oDeployTimeTextBox
            // 
            this.m_oDeployTimeTextBox.Location = new System.Drawing.Point(108, 18);
            this.m_oDeployTimeTextBox.Name = "m_oDeployTimeTextBox";
            this.m_oDeployTimeTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oDeployTimeTextBox.TabIndex = 53;
            // 
            // m_oTonsManTextBox
            // 
            this.m_oTonsManTextBox.Location = new System.Drawing.Point(108, 44);
            this.m_oTonsManTextBox.Name = "m_oTonsManTextBox";
            this.m_oTonsManTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oTonsManTextBox.TabIndex = 55;
            // 
            // m_oTonPerManLabel
            // 
            this.m_oTonPerManLabel.AutoSize = true;
            this.m_oTonPerManLabel.Location = new System.Drawing.Point(6, 47);
            this.m_oTonPerManLabel.Name = "m_oTonPerManLabel";
            this.m_oTonPerManLabel.Size = new System.Drawing.Size(73, 13);
            this.m_oTonPerManLabel.TabIndex = 56;
            this.m_oTonPerManLabel.Text = "Tons per Man";
            // 
            // m_oCryoBerthsLabel
            // 
            this.m_oCryoBerthsLabel.AutoSize = true;
            this.m_oCryoBerthsLabel.Location = new System.Drawing.Point(6, 203);
            this.m_oCryoBerthsLabel.Name = "m_oCryoBerthsLabel";
            this.m_oCryoBerthsLabel.Size = new System.Drawing.Size(87, 13);
            this.m_oCryoBerthsLabel.TabIndex = 52;
            this.m_oCryoBerthsLabel.Text = "Cryogenic Berths";
            // 
            // m_oCapPerHSLabel
            // 
            this.m_oCapPerHSLabel.AutoSize = true;
            this.m_oCapPerHSLabel.Location = new System.Drawing.Point(6, 73);
            this.m_oCapPerHSLabel.Name = "m_oCapPerHSLabel";
            this.m_oCapPerHSLabel.Size = new System.Drawing.Size(84, 13);
            this.m_oCapPerHSLabel.TabIndex = 42;
            this.m_oCapPerHSLabel.Text = "Capacity per HS";
            // 
            // m_oSpareBerthsLabel
            // 
            this.m_oSpareBerthsLabel.AutoSize = true;
            this.m_oSpareBerthsLabel.Location = new System.Drawing.Point(6, 177);
            this.m_oSpareBerthsLabel.Name = "m_oSpareBerthsLabel";
            this.m_oSpareBerthsLabel.Size = new System.Drawing.Size(68, 13);
            this.m_oSpareBerthsLabel.TabIndex = 51;
            this.m_oSpareBerthsLabel.Text = "Spare Berths";
            // 
            // m_oCapPerHSTextBox
            // 
            this.m_oCapPerHSTextBox.Location = new System.Drawing.Point(108, 70);
            this.m_oCapPerHSTextBox.Name = "m_oCapPerHSTextBox";
            this.m_oCapPerHSTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oCapPerHSTextBox.TabIndex = 41;
            // 
            // m_oCrewBerthsLabel
            // 
            this.m_oCrewBerthsLabel.AutoSize = true;
            this.m_oCrewBerthsLabel.Location = new System.Drawing.Point(6, 151);
            this.m_oCrewBerthsLabel.Name = "m_oCrewBerthsLabel";
            this.m_oCrewBerthsLabel.Size = new System.Drawing.Size(64, 13);
            this.m_oCrewBerthsLabel.TabIndex = 50;
            this.m_oCrewBerthsLabel.Text = "Crew Berths";
            // 
            // m_oAccomHSReqTextBox
            // 
            this.m_oAccomHSReqTextBox.Location = new System.Drawing.Point(108, 96);
            this.m_oAccomHSReqTextBox.Name = "m_oAccomHSReqTextBox";
            this.m_oAccomHSReqTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oAccomHSReqTextBox.TabIndex = 43;
            // 
            // m_oAccomHSAvailLabel
            // 
            this.m_oAccomHSAvailLabel.AutoSize = true;
            this.m_oAccomHSAvailLabel.Location = new System.Drawing.Point(6, 125);
            this.m_oAccomHSAvailLabel.Name = "m_oAccomHSAvailLabel";
            this.m_oAccomHSAvailLabel.Size = new System.Drawing.Size(84, 13);
            this.m_oAccomHSAvailLabel.TabIndex = 49;
            this.m_oAccomHSAvailLabel.Text = "Accom HS Avail";
            // 
            // m_oAccomHSAvailTextBox
            // 
            this.m_oAccomHSAvailTextBox.Location = new System.Drawing.Point(108, 122);
            this.m_oAccomHSAvailTextBox.Name = "m_oAccomHSAvailTextBox";
            this.m_oAccomHSAvailTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oAccomHSAvailTextBox.TabIndex = 44;
            // 
            // m_oAccomHSReqLabel
            // 
            this.m_oAccomHSReqLabel.AutoSize = true;
            this.m_oAccomHSReqLabel.Location = new System.Drawing.Point(6, 99);
            this.m_oAccomHSReqLabel.Name = "m_oAccomHSReqLabel";
            this.m_oAccomHSReqLabel.Size = new System.Drawing.Size(81, 13);
            this.m_oAccomHSReqLabel.TabIndex = 48;
            this.m_oAccomHSReqLabel.Text = "Accom HS Req";
            // 
            // m_oCrewBerthsTextBox
            // 
            this.m_oCrewBerthsTextBox.Location = new System.Drawing.Point(108, 148);
            this.m_oCrewBerthsTextBox.Name = "m_oCrewBerthsTextBox";
            this.m_oCrewBerthsTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oCrewBerthsTextBox.TabIndex = 45;
            // 
            // m_oSpareBerthsTextBox
            // 
            this.m_oSpareBerthsTextBox.Location = new System.Drawing.Point(108, 174);
            this.m_oSpareBerthsTextBox.Name = "m_oSpareBerthsTextBox";
            this.m_oSpareBerthsTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oSpareBerthsTextBox.TabIndex = 46;
            // 
            // m_oCryoBerthsTextBox
            // 
            this.m_oCryoBerthsTextBox.Location = new System.Drawing.Point(108, 200);
            this.m_oCryoBerthsTextBox.Name = "m_oCryoBerthsTextBox";
            this.m_oCryoBerthsTextBox.Size = new System.Drawing.Size(72, 20);
            this.m_oCryoBerthsTextBox.TabIndex = 47;
            // 
            // ClassDes_Options
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1242, 914);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.m_oPassiveDefGroupBox);
            this.Controls.Add(this.m_oPowerSystemGroupBox);
            this.Controls.Add(this.m_oBuildLoadGroupBox);
            this.Controls.Add(this.m_oBuildPointGroupBox);
            this.Controls.Add(this.m_oGeneralInfoGroupBox);
            this.Controls.Add(this.m_oTaskGroupTabControl);
            this.Controls.Add(this.m_oButtonsGroupBox);
            this.Controls.Add(this.m_oClassOptionsGroupBox);
            this.Controls.Add(this.m_oCivGroupBox);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "ClassDes_Options";
            this.m_oCivGroupBox.ResumeLayout(false);
            this.m_oCivGroupBox.PerformLayout();
            this.m_oClassOptionsGroupBox.ResumeLayout(false);
            this.m_oClassOptionsGroupBox.PerformLayout();
            this.m_oButtonsGroupBox.ResumeLayout(false);
            this.m_oButtonsGroupBox.PerformLayout();
            this.m_oTaskGroupTabControl.ResumeLayout(false);
            this.m_oSummaryTab.ResumeLayout(false);
            this.m_oArmorGroupBox.ResumeLayout(false);
            this.m_oClassSummaryGroupBox.ResumeLayout(false);
            this.m_oTargetSpeedGroupBox.ResumeLayout(false);
            this.m_oTargetSpeedGroupBox.PerformLayout();
            this.m_oRangeBandsGroupBox.ResumeLayout(false);
            this.m_oRangeBandsGroupBox.PerformLayout();
            this.m_oGeneralInfoGroupBox.ResumeLayout(false);
            this.m_oGeneralInfoGroupBox.PerformLayout();
            this.m_oBuildLoadGroupBox.ResumeLayout(false);
            this.m_oBuildLoadGroupBox.PerformLayout();
            this.m_oPowerSystemGroupBox.ResumeLayout(false);
            this.m_oPowerSystemGroupBox.PerformLayout();
            this.m_oPassiveDefGroupBox.ResumeLayout(false);
            this.m_oPassiveDefGroupBox.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox m_oCivGroupBox;
        private System.Windows.Forms.Label m_oHullLabel;
        private System.Windows.Forms.Label m_oTypeLabel;
        private System.Windows.Forms.Label m_oClassLabel;
        private System.Windows.Forms.Label m_oEmpireLabel;
        private System.Windows.Forms.ComboBox m_oHullComboBox;
        private System.Windows.Forms.ComboBox m_oTypeComboBox;
        private System.Windows.Forms.ComboBox m_oClassComboBox;
        private System.Windows.Forms.ComboBox m_oFactionComboBox;
        private System.Windows.Forms.GroupBox m_oClassOptionsGroupBox;
        private System.Windows.Forms.CheckBox m_oConscriptCheckBox;
        private System.Windows.Forms.CheckBox m_oSupplyShipCheckBox;
        private System.Windows.Forms.CheckBox m_oObsoleteCheckBox;
        private System.Windows.Forms.CheckBox m_oSizeinTonsCheckBox;
        private System.Windows.Forms.CheckBox m_oCollierCheckBox;
        private System.Windows.Forms.CheckBox m_oTankerCheckBox;
        private System.Windows.Forms.CheckBox m_oNoThemeCheckBox;
        private System.Windows.Forms.CheckBox m_oHideObsoleteCheckBox;
        private System.Windows.Forms.CheckBox m_oShowCivilianDesignsCheckBox;
        private System.Windows.Forms.CheckBox m_oKeepExcessQCheckBox;
        private System.Windows.Forms.GroupBox m_oButtonsGroupBox;
        private System.Windows.Forms.Button m_oSMModeButton;
        private System.Windows.Forms.Button m_oNewButton;
        private System.Windows.Forms.Button m_oViewTechButton;
        private System.Windows.Forms.Button m_oTextFileButton;
        private System.Windows.Forms.Button m_oObsoleteCompButton;
        private System.Windows.Forms.Button m_oFleetAssignBbutton;
        private System.Windows.Forms.Button m_oRefreshTechButton;
        private System.Windows.Forms.Button m_oCopyDesignButton;
        private System.Windows.Forms.Button m_oReNumberButton;
        private System.Windows.Forms.Button m_oAutoRenameButton;
        private System.Windows.Forms.Button m_oRandomNameButton;
        private System.Windows.Forms.Button m_oDeleteButton;
        private System.Windows.Forms.Button m_oNewHullButton;
        private System.Windows.Forms.Button m_oLockDesignButton;
        private System.Windows.Forms.Button m_oNewArmorButton;
        private System.Windows.Forms.Button m_oDesignTechButton;
        private System.Windows.Forms.Button m_oNPRClassButton;
        private System.Windows.Forms.Button m_oRenameButton;
        private System.Windows.Forms.RadioButton m_oSortCostRadioButton;
        private System.Windows.Forms.RadioButton m_oSortHullRadioButton;
        private System.Windows.Forms.RadioButton m_oSortSizeRadioButton;
        private System.Windows.Forms.RadioButton m_oSortAlphaRadioButton;
        private System.Windows.Forms.TabControl m_oTaskGroupTabControl;
        private System.Windows.Forms.TabPage m_oSummaryTab;
        private System.Windows.Forms.GroupBox m_oArmorGroupBox;
        private System.Windows.Forms.GroupBox m_oGeneralInfoGroupBox;
        private System.Windows.Forms.GroupBox m_oBuildPointGroupBox;
        private System.Windows.Forms.Button m_oCloseButton;
        private System.Windows.Forms.GroupBox m_oBuildLoadGroupBox;
        private System.Windows.Forms.GroupBox m_oPowerSystemGroupBox;
        private System.Windows.Forms.GroupBox m_oPassiveDefGroupBox;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.GroupBox m_oRangeBandsGroupBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox m_oRangeCustomTextBox;
        private System.Windows.Forms.RadioButton m_oRangeCustomRadioButton;
        private System.Windows.Forms.RadioButton m_oRange1000000RadioButton;
        private System.Windows.Forms.RadioButton m_oRange500000RadioButton;
        private System.Windows.Forms.RadioButton m_oRange20000RadioButton;
        private System.Windows.Forms.RadioButton m_oRange30000RadioButton;
        private System.Windows.Forms.RadioButton m_oRange50000RadioButton;
        private System.Windows.Forms.RadioButton m_oRange200000RadioButton;
        private System.Windows.Forms.RadioButton m_oTange100000RadioButton;
        private System.Windows.Forms.RadioButton m_oRange10000RadioButton;
        private System.Windows.Forms.GroupBox m_oTargetSpeedGroupBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox m_oSpeedCustomTextBox;
        private System.Windows.Forms.RadioButton m_oSpeedCustomRadioButton;
        private System.Windows.Forms.RadioButton m_oSpeed100000RadioButton;
        private System.Windows.Forms.RadioButton m_oSpeed50000RadioButton;
        private System.Windows.Forms.RadioButton m_oSpeed2000RadioButton;
        private System.Windows.Forms.RadioButton m_oSpeed3000RadioButton;
        private System.Windows.Forms.RadioButton m_oSpeed5000RadioButton;
        private System.Windows.Forms.RadioButton m_oSpeed20000RadioButton;
        private System.Windows.Forms.RadioButton m_oSpeed10000RadioButton;
        private System.Windows.Forms.RadioButton m_oSpeed1000RadioButton;
        private System.Windows.Forms.TabPage m_oDesignTab;
        private System.Windows.Forms.TabPage m_oOrdFightersTab;
        private System.Windows.Forms.TabPage m_oCompSummaryTab;
        private System.Windows.Forms.TabPage m_oDACRankInfoTab;
        private System.Windows.Forms.TabPage m_oShipsTab;
        private System.Windows.Forms.TabPage m_oGlossaryTab;
        private System.Windows.Forms.Label m_oLoadTimeLabel;
        private System.Windows.Forms.Label m_oBuildTimeLabel;
        private System.Windows.Forms.TextBox m_oLoadTimeTextBox;
        private System.Windows.Forms.TextBox m_oBuildTimeTextBox;
        private System.Windows.Forms.Label m_oRequiredPowerLabel;
        private System.Windows.Forms.Label m_oReactorPowerLabel;
        private System.Windows.Forms.Label m_oJumpDistLabel;
        private System.Windows.Forms.Label m_oJumpRatingLabel;
        private System.Windows.Forms.Label m_oMaxSpeedLabel;
        private System.Windows.Forms.Label m_oEnginePowerLabel;
        private System.Windows.Forms.TextBox m_oRequiredPowerTextBox;
        private System.Windows.Forms.TextBox m_oReactorPowerTextBox;
        private System.Windows.Forms.TextBox m_oJumpDistTextBox;
        private System.Windows.Forms.TextBox m_oJumpRatingTextBox;
        private System.Windows.Forms.TextBox m_oMaxSpeedTextBox;
        private System.Windows.Forms.TextBox m_oEnginePowerTextBox;
        private System.Windows.Forms.Label m_oArmorRatingLabel;
        private System.Windows.Forms.TextBox m_oArmorRatingTextBox;
        private System.Windows.Forms.TextBox m_oClassSizeTextBox;
        private System.Windows.Forms.Label m_oClassSizeLabel;
        private System.Windows.Forms.Label m_oIHTKLabel;
        private System.Windows.Forms.Label m_oAAreaLabel;
        private System.Windows.Forms.Label m_oSRechargeLabel;
        private System.Windows.Forms.TextBox m_oAAreaTextBox;
        private System.Windows.Forms.Label m_oSStrengthLabel;
        private System.Windows.Forms.TextBox m_oAStrengthTextBox;
        private System.Windows.Forms.Label m_oAColumnLabel;
        private System.Windows.Forms.TextBox m_oAColumnsTextBox;
        private System.Windows.Forms.Label m_oAStrengthLabel;
        private System.Windows.Forms.TextBox m_oSStrengthTextBox;
        private System.Windows.Forms.TextBox m_oSRechargeTextBox;
        private System.Windows.Forms.TextBox m_oInternalHTKTextBox;
        private System.Windows.Forms.Label m_oDeployTimeLabel;
        private System.Windows.Forms.TextBox m_oDeployTimeTextBox;
        private System.Windows.Forms.TextBox m_oTonsManTextBox;
        private System.Windows.Forms.Label m_oTonPerManLabel;
        private System.Windows.Forms.Label m_oCryoBerthsLabel;
        private System.Windows.Forms.Label m_oCapPerHSLabel;
        private System.Windows.Forms.Label m_oSpareBerthsLabel;
        private System.Windows.Forms.TextBox m_oCapPerHSTextBox;
        private System.Windows.Forms.Label m_oCrewBerthsLabel;
        private System.Windows.Forms.TextBox m_oAccomHSReqTextBox;
        private System.Windows.Forms.Label m_oAccomHSAvailLabel;
        private System.Windows.Forms.TextBox m_oAccomHSAvailTextBox;
        private System.Windows.Forms.Label m_oAccomHSReqLabel;
        private System.Windows.Forms.TextBox m_oCrewBerthsTextBox;
        private System.Windows.Forms.TextBox m_oSpareBerthsTextBox;
        private System.Windows.Forms.TextBox m_oCryoBerthsTextBox;
        private System.Windows.Forms.GroupBox m_oClassSummaryGroupBox;
        private System.Windows.Forms.RichTextBox m_oClassSummaryTextBox;
        private Button m_oArmourUpButton;
        private Button m_oArmourDownButton;
    }
}
