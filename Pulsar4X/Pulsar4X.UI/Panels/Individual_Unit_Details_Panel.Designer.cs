using System.Windows.Forms;

namespace Pulsar4X.UI.Panels
{
    partial class Individual_Unit_Details_Panel
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// The DACListBox will contain ship Damage Allocation chart data.
        /// </summary>
        public ListBox DACListBox
        {
            get { return m_oDACListBox; }
        }

        /// <summary>
        /// the list of damaged components will go here.
        /// </summary>
        public ListBox DamagedSystemsListBox
        {
            get { return m_oDamagedSystemsListBox; }
        }

        /// <summary>
        /// List of Weapons will go here.
        /// </summary>
        public ListBox WeaponListBox
        {
            get { return m_oWeaponListBox; }
        }

        /// <summary>
        /// List of contacts here.
        /// </summary>
        public ListBox ContactListBox
        {
            get { return m_oContactsListBox; }
        }


        /// <summary>
        /// Selected Fire Control Combo box
        /// </summary>
        public ComboBox SFCComboBox
        {
            get { return m_oSFCComboBox; }
        }

        /// <summary>
        /// Selected PD mode box.
        /// </summary>
        public ComboBox PDComboBox
        {
            get { return m_oPDModeComboBox; }
        }

        /// <summary>
        /// User selected active sensor from ship sensor list.
        /// </summary>
        public ComboBox SelectedActiveComboBox
        {
            get { return m_oSelectedActiveComboBox; }
        }

        /// <summary>
        /// Set FC to open fire mode.
        /// </summary>
        public Button OpenFireButton
        {
            get { return m_oOpenFireButton; }
        }

        /// <summary>
        /// Set FC to cease fire mode.
        /// </summary>
        public Button CeaseFireButton
        {
            get { return m_oCeaseFireButton; }
        }

        /// <summary>
        /// Raises shields
        /// </summary>
        public Button RaiseShieldsButton
        {
            get { return m_oRaiseShieldsButton; }
        }
        
        /// <summary>
        /// Lowers shields.
        /// </summary>
        public Button LowerShieldsButton
        {
            get { return m_oLowerShieldsButton; }
        }

        /// <summary>
        /// Sets selected sensor to active.
        /// </summary>
        public Button ActiveButton
        {
            get { return m_oActiveButton; }
        }

        /// <summary>
        /// Sets selected sensor to Inactive.
        /// </summary>
        public Button InactiveButton
        {
            get { return m_oInactiveButton; }
        }

        /// <summary>
        /// Assign target from contact box to FC.
        /// </summary>
        public Button AssignTargetButton
        {
            get { return m_oAssignTargetButton; }
        }

        /// <summary>
        /// Clear FC targeting.
        /// </summary>
        public Button ClearTargetButton
        {
            get { return m_oClearTargetButton; }
        }

        /// <summary>
        /// Assigns the selected weapon to the current FC.
        /// </summary>
        public Button AssignWeaponButton
        {
            get { return m_oAssignWeaponsButton; }
        }

        /// <summary>
        /// Assigns all weapons to the selected FC., deassigns them from other FCs.
        /// </summary>
        public Button AssignAllWeaponsButton
        {
            get { return m_oAssignAllWeaponsButton; }
        }

        /// <summary>
        /// Clears all weapons from the selected FC.
        /// </summary>
        public Button ClearWeaponsButton
        {
            get { return m_oClearWeaponsButton; }
        }

        /// <summary>
        /// Groupbox for shields, will display on/off status.
        /// </summary>
        public GroupBox ShieldGroupBox
        {
            get { return m_oShieldGroupBox; }
        }

        /// <summary>
        /// Groupbox for sensors, will display on/off status.
        /// </summary>
        public GroupBox ActiveGroupBox
        {
            get { return m_oActiveSensorGroupBox; }
        }

        /// <summary>
        /// Textbox for printing combat summary.
        /// </summary>
        public RichTextBox CombatSummaryTextBox
        {
            get { return m_oCombatSummaryRTBox; }
        }

        /// <summary>
        /// Textbox for printing class design information.
        /// </summary>
        public RichTextBox ClassDesignTextBox
        {
            get { return m_oClassDesignRTBox; }
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
            this.m_oTaskGroupTabControl = new System.Windows.Forms.TabControl();
            this.m_oArmourStatusTab = new System.Windows.Forms.TabPage();
            this.m_oArmorGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oDamageControlTab = new System.Windows.Forms.TabPage();
            this.m_oAbandonShipGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oAbandonShipButton = new System.Windows.Forms.Button();
            this.m_oManualDamageGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oApplyDamageButton = new System.Windows.Forms.Button();
            this.m_oApplyDamageTextBox = new System.Windows.Forms.TextBox();
            this.m_oDCQGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oDemoteItemButton = new System.Windows.Forms.Button();
            this.m_oAdvanceItemButton = new System.Windows.Forms.Button();
            this.m_oRemoveItemButton = new System.Windows.Forms.Button();
            this.m_oDamageControlQueueListBox = new System.Windows.Forms.ListBox();
            this.m_oCurrentDCGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oAbortDCButton = new System.Windows.Forms.Button();
            this.m_oCurrentDCTargetTextBox = new System.Windows.Forms.TextBox();
            this.m_oDamagedSystemsGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oAddToQueueButton = new System.Windows.Forms.Button();
            this.m_oBeginDCButton = new System.Windows.Forms.Button();
            this.m_oDamagedSystemsListBox = new System.Windows.Forms.ListBox();
            this.m_oDACGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oDACListBox = new System.Windows.Forms.ListBox();
            this.m_oParaCargoGUTab = new System.Windows.Forms.TabPage();
            this.m_oMiscellaneousTab = new System.Windows.Forms.TabPage();
            this.m_oHistoryNotesTDTab = new System.Windows.Forms.TabPage();
            this.m_oShipDesignTab = new System.Windows.Forms.TabPage();
            this.m_oClassDesignTab = new System.Windows.Forms.TabPage();
            this.m_oClassDesignGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oClassDesignRTBox = new System.Windows.Forms.RichTextBox();
            this.m_oCombatSettingsTab = new System.Windows.Forms.TabPage();
            this.m_oActiveSensorGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oSelectedActiveComboBox = new System.Windows.Forms.ComboBox();
            this.m_oInactiveButton = new System.Windows.Forms.Button();
            this.m_oActiveButton = new System.Windows.Forms.Button();
            this.m_oShieldGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oLowerShieldsButton = new System.Windows.Forms.Button();
            this.m_oRaiseShieldsButton = new System.Windows.Forms.Button();
            this.m_oFireControlsGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oCeaseFireButton = new System.Windows.Forms.Button();
            this.m_oOpenFireButton = new System.Windows.Forms.Button();
            this.m_oAutoFireGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oSyncFireCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oAutoFireCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oECCMAssignGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oAssignECCMButton = new System.Windows.Forms.Button();
            this.m_oECCMComboBox = new System.Windows.Forms.ComboBox();
            this.m_oPointDefenseModeGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oPDRangeLabel2 = new System.Windows.Forms.Label();
            this.m_oSelectRangeLabel = new System.Windows.Forms.Label();
            this.m_oPDRangeTextBox = new System.Windows.Forms.TextBox();
            this.m_oSetModeButton = new System.Windows.Forms.Button();
            this.m_oPDModeComboBox = new System.Windows.Forms.ComboBox();
            this.m_oSelectedFireControlGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oSFCComboBox = new System.Windows.Forms.ComboBox();
            this.m_oAssignTargetsGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oClearTargetButton = new System.Windows.Forms.Button();
            this.m_oAssignTargetButton = new System.Windows.Forms.Button();
            this.m_oContactsListBox = new System.Windows.Forms.ListBox();
            this.m_oAssignWeaponsGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oClearWeaponsButton = new System.Windows.Forms.Button();
            this.m_oAssignAllWeaponsButton = new System.Windows.Forms.Button();
            this.m_oAssignWeaponsButton = new System.Windows.Forms.Button();
            this.m_oWeaponListBox = new System.Windows.Forms.ListBox();
            this.m_oCombatSummaryTab = new System.Windows.Forms.TabPage();
            this.m_oCombatSummaryGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oCombatSummaryRTBox = new System.Windows.Forms.RichTextBox();
            this.m_oOrdnanceManagementTab = new System.Windows.Forms.TabPage();
            this.m_oTaskGroupTabControl.SuspendLayout();
            this.m_oArmourStatusTab.SuspendLayout();
            this.m_oDamageControlTab.SuspendLayout();
            this.m_oAbandonShipGroupBox.SuspendLayout();
            this.m_oManualDamageGroupBox.SuspendLayout();
            this.m_oDCQGroupBox.SuspendLayout();
            this.m_oCurrentDCGroupBox.SuspendLayout();
            this.m_oDamagedSystemsGroupBox.SuspendLayout();
            this.m_oDACGroupBox.SuspendLayout();
            this.m_oClassDesignTab.SuspendLayout();
            this.m_oClassDesignGroupBox.SuspendLayout();
            this.m_oCombatSettingsTab.SuspendLayout();
            this.m_oActiveSensorGroupBox.SuspendLayout();
            this.m_oShieldGroupBox.SuspendLayout();
            this.m_oFireControlsGroupBox.SuspendLayout();
            this.m_oAutoFireGroupBox.SuspendLayout();
            this.m_oECCMAssignGroupBox.SuspendLayout();
            this.m_oPointDefenseModeGroupBox.SuspendLayout();
            this.m_oSelectedFireControlGroupBox.SuspendLayout();
            this.m_oAssignTargetsGroupBox.SuspendLayout();
            this.m_oAssignWeaponsGroupBox.SuspendLayout();
            this.m_oCombatSummaryTab.SuspendLayout();
            this.m_oCombatSummaryGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_oTaskGroupTabControl
            // 
            this.m_oTaskGroupTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oArmourStatusTab);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oDamageControlTab);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oParaCargoGUTab);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oMiscellaneousTab);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oHistoryNotesTDTab);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oShipDesignTab);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oClassDesignTab);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oCombatSettingsTab);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oCombatSummaryTab);
            this.m_oTaskGroupTabControl.Controls.Add(this.m_oOrdnanceManagementTab);
            this.m_oTaskGroupTabControl.Location = new System.Drawing.Point(71, 261);
            this.m_oTaskGroupTabControl.MaximumSize = new System.Drawing.Size(1041, 566);
            this.m_oTaskGroupTabControl.MinimumSize = new System.Drawing.Size(1041, 566);
            this.m_oTaskGroupTabControl.Name = "m_oTaskGroupTabControl";
            this.m_oTaskGroupTabControl.SelectedIndex = 0;
            this.m_oTaskGroupTabControl.Size = new System.Drawing.Size(1041, 566);
            this.m_oTaskGroupTabControl.TabIndex = 40;
            // 
            // m_oArmourStatusTab
            // 
            this.m_oArmourStatusTab.Controls.Add(this.m_oArmorGroupBox);
            this.m_oArmourStatusTab.Location = new System.Drawing.Point(4, 22);
            this.m_oArmourStatusTab.Name = "m_oArmourStatusTab";
            this.m_oArmourStatusTab.Padding = new System.Windows.Forms.Padding(3);
            this.m_oArmourStatusTab.Size = new System.Drawing.Size(1033, 540);
            this.m_oArmourStatusTab.TabIndex = 1;
            this.m_oArmourStatusTab.Text = "Armour Status";
            this.m_oArmourStatusTab.UseVisualStyleBackColor = true;
            // 
            // m_oArmorGroupBox
            // 
            this.m_oArmorGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oArmorGroupBox.Location = new System.Drawing.Point(6, 6);
            this.m_oArmorGroupBox.Name = "m_oArmorGroupBox";
            this.m_oArmorGroupBox.Size = new System.Drawing.Size(1027, 534);
            this.m_oArmorGroupBox.TabIndex = 35;
            this.m_oArmorGroupBox.TabStop = false;
            // 
            // m_oDamageControlTab
            // 
            this.m_oDamageControlTab.Controls.Add(this.m_oAbandonShipGroupBox);
            this.m_oDamageControlTab.Controls.Add(this.m_oManualDamageGroupBox);
            this.m_oDamageControlTab.Controls.Add(this.m_oDCQGroupBox);
            this.m_oDamageControlTab.Controls.Add(this.m_oCurrentDCGroupBox);
            this.m_oDamageControlTab.Controls.Add(this.m_oDamagedSystemsGroupBox);
            this.m_oDamageControlTab.Controls.Add(this.m_oDACGroupBox);
            this.m_oDamageControlTab.Location = new System.Drawing.Point(4, 22);
            this.m_oDamageControlTab.Name = "m_oDamageControlTab";
            this.m_oDamageControlTab.Size = new System.Drawing.Size(1033, 540);
            this.m_oDamageControlTab.TabIndex = 2;
            this.m_oDamageControlTab.Text = "Damage Control";
            this.m_oDamageControlTab.UseVisualStyleBackColor = true;
            // 
            // m_oAbandonShipGroupBox
            // 
            this.m_oAbandonShipGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oAbandonShipGroupBox.Controls.Add(this.m_oAbandonShipButton);
            this.m_oAbandonShipGroupBox.Location = new System.Drawing.Point(656, 452);
            this.m_oAbandonShipGroupBox.Name = "m_oAbandonShipGroupBox";
            this.m_oAbandonShipGroupBox.Size = new System.Drawing.Size(157, 85);
            this.m_oAbandonShipGroupBox.TabIndex = 37;
            this.m_oAbandonShipGroupBox.TabStop = false;
            // 
            // m_oAbandonShipButton
            // 
            this.m_oAbandonShipButton.BackColor = System.Drawing.Color.Red;
            this.m_oAbandonShipButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oAbandonShipButton.Location = new System.Drawing.Point(27, 28);
            this.m_oAbandonShipButton.Name = "m_oAbandonShipButton";
            this.m_oAbandonShipButton.Size = new System.Drawing.Size(100, 40);
            this.m_oAbandonShipButton.TabIndex = 46;
            this.m_oAbandonShipButton.Text = "Abandon Ship";
            this.m_oAbandonShipButton.UseVisualStyleBackColor = false;
            // 
            // m_oManualDamageGroupBox
            // 
            this.m_oManualDamageGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oManualDamageGroupBox.Controls.Add(this.m_oApplyDamageButton);
            this.m_oManualDamageGroupBox.Controls.Add(this.m_oApplyDamageTextBox);
            this.m_oManualDamageGroupBox.Location = new System.Drawing.Point(819, 452);
            this.m_oManualDamageGroupBox.Name = "m_oManualDamageGroupBox";
            this.m_oManualDamageGroupBox.Size = new System.Drawing.Size(214, 85);
            this.m_oManualDamageGroupBox.TabIndex = 36;
            this.m_oManualDamageGroupBox.TabStop = false;
            this.m_oManualDamageGroupBox.Text = "Manual Damage";
            // 
            // m_oApplyDamageButton
            // 
            this.m_oApplyDamageButton.Location = new System.Drawing.Point(94, 33);
            this.m_oApplyDamageButton.Name = "m_oApplyDamageButton";
            this.m_oApplyDamageButton.Size = new System.Drawing.Size(90, 30);
            this.m_oApplyDamageButton.TabIndex = 49;
            this.m_oApplyDamageButton.Text = "Apply Damage";
            this.m_oApplyDamageButton.UseVisualStyleBackColor = true;
            // 
            // m_oApplyDamageTextBox
            // 
            this.m_oApplyDamageTextBox.Location = new System.Drawing.Point(26, 39);
            this.m_oApplyDamageTextBox.Name = "m_oApplyDamageTextBox";
            this.m_oApplyDamageTextBox.Size = new System.Drawing.Size(50, 20);
            this.m_oApplyDamageTextBox.TabIndex = 50;
            // 
            // m_oDCQGroupBox
            // 
            this.m_oDCQGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oDCQGroupBox.Controls.Add(this.m_oDemoteItemButton);
            this.m_oDCQGroupBox.Controls.Add(this.m_oAdvanceItemButton);
            this.m_oDCQGroupBox.Controls.Add(this.m_oRemoveItemButton);
            this.m_oDCQGroupBox.Controls.Add(this.m_oDamageControlQueueListBox);
            this.m_oDCQGroupBox.Location = new System.Drawing.Point(656, 94);
            this.m_oDCQGroupBox.Name = "m_oDCQGroupBox";
            this.m_oDCQGroupBox.Size = new System.Drawing.Size(374, 352);
            this.m_oDCQGroupBox.TabIndex = 36;
            this.m_oDCQGroupBox.TabStop = false;
            this.m_oDCQGroupBox.Text = "Damage Control Queue";
            // 
            // m_oDemoteItemButton
            // 
            this.m_oDemoteItemButton.Location = new System.Drawing.Point(310, 171);
            this.m_oDemoteItemButton.Name = "m_oDemoteItemButton";
            this.m_oDemoteItemButton.Size = new System.Drawing.Size(30, 30);
            this.m_oDemoteItemButton.TabIndex = 48;
            this.m_oDemoteItemButton.Text = "\\/";
            this.m_oDemoteItemButton.UseVisualStyleBackColor = true;
            // 
            // m_oAdvanceItemButton
            // 
            this.m_oAdvanceItemButton.Location = new System.Drawing.Point(310, 135);
            this.m_oAdvanceItemButton.Name = "m_oAdvanceItemButton";
            this.m_oAdvanceItemButton.Size = new System.Drawing.Size(30, 30);
            this.m_oAdvanceItemButton.TabIndex = 47;
            this.m_oAdvanceItemButton.Text = "/\\";
            this.m_oAdvanceItemButton.UseVisualStyleBackColor = true;
            // 
            // m_oRemoveItemButton
            // 
            this.m_oRemoveItemButton.Location = new System.Drawing.Point(105, 315);
            this.m_oRemoveItemButton.Name = "m_oRemoveItemButton";
            this.m_oRemoveItemButton.Size = new System.Drawing.Size(90, 30);
            this.m_oRemoveItemButton.TabIndex = 46;
            this.m_oRemoveItemButton.Text = "Remove";
            this.m_oRemoveItemButton.UseVisualStyleBackColor = true;
            // 
            // m_oDamageControlQueueListBox
            // 
            this.m_oDamageControlQueueListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oDamageControlQueueListBox.FormattingEnabled = true;
            this.m_oDamageControlQueueListBox.Location = new System.Drawing.Point(6, 19);
            this.m_oDamageControlQueueListBox.Name = "m_oDamageControlQueueListBox";
            this.m_oDamageControlQueueListBox.Size = new System.Drawing.Size(298, 290);
            this.m_oDamageControlQueueListBox.TabIndex = 46;
            // 
            // m_oCurrentDCGroupBox
            // 
            this.m_oCurrentDCGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oCurrentDCGroupBox.Controls.Add(this.m_oAbortDCButton);
            this.m_oCurrentDCGroupBox.Controls.Add(this.m_oCurrentDCTargetTextBox);
            this.m_oCurrentDCGroupBox.Location = new System.Drawing.Point(656, 3);
            this.m_oCurrentDCGroupBox.Name = "m_oCurrentDCGroupBox";
            this.m_oCurrentDCGroupBox.Size = new System.Drawing.Size(374, 85);
            this.m_oCurrentDCGroupBox.TabIndex = 35;
            this.m_oCurrentDCGroupBox.TabStop = false;
            this.m_oCurrentDCGroupBox.Text = "Current Damage Control Assignment";
            // 
            // m_oAbortDCButton
            // 
            this.m_oAbortDCButton.Location = new System.Drawing.Point(144, 45);
            this.m_oAbortDCButton.Name = "m_oAbortDCButton";
            this.m_oAbortDCButton.Size = new System.Drawing.Size(75, 25);
            this.m_oAbortDCButton.TabIndex = 49;
            this.m_oAbortDCButton.Text = "Abort DC";
            this.m_oAbortDCButton.UseVisualStyleBackColor = true;
            // 
            // m_oCurrentDCTargetTextBox
            // 
            this.m_oCurrentDCTargetTextBox.Enabled = false;
            this.m_oCurrentDCTargetTextBox.Location = new System.Drawing.Point(6, 19);
            this.m_oCurrentDCTargetTextBox.Name = "m_oCurrentDCTargetTextBox";
            this.m_oCurrentDCTargetTextBox.Size = new System.Drawing.Size(362, 20);
            this.m_oCurrentDCTargetTextBox.TabIndex = 49;
            // 
            // m_oDamagedSystemsGroupBox
            // 
            this.m_oDamagedSystemsGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oDamagedSystemsGroupBox.Controls.Add(this.m_oAddToQueueButton);
            this.m_oDamagedSystemsGroupBox.Controls.Add(this.m_oBeginDCButton);
            this.m_oDamagedSystemsGroupBox.Controls.Add(this.m_oDamagedSystemsListBox);
            this.m_oDamagedSystemsGroupBox.Location = new System.Drawing.Point(359, 3);
            this.m_oDamagedSystemsGroupBox.Name = "m_oDamagedSystemsGroupBox";
            this.m_oDamagedSystemsGroupBox.Size = new System.Drawing.Size(291, 534);
            this.m_oDamagedSystemsGroupBox.TabIndex = 34;
            this.m_oDamagedSystemsGroupBox.TabStop = false;
            this.m_oDamagedSystemsGroupBox.Text = "Damaged Systems";
            // 
            // m_oAddToQueueButton
            // 
            this.m_oAddToQueueButton.Location = new System.Drawing.Point(195, 497);
            this.m_oAddToQueueButton.Name = "m_oAddToQueueButton";
            this.m_oAddToQueueButton.Size = new System.Drawing.Size(90, 30);
            this.m_oAddToQueueButton.TabIndex = 45;
            this.m_oAddToQueueButton.Text = "Add To Queue";
            this.m_oAddToQueueButton.UseVisualStyleBackColor = true;
            // 
            // m_oBeginDCButton
            // 
            this.m_oBeginDCButton.Location = new System.Drawing.Point(6, 497);
            this.m_oBeginDCButton.Name = "m_oBeginDCButton";
            this.m_oBeginDCButton.Size = new System.Drawing.Size(90, 30);
            this.m_oBeginDCButton.TabIndex = 44;
            this.m_oBeginDCButton.Text = "Begin DC";
            this.m_oBeginDCButton.UseVisualStyleBackColor = true;
            // 
            // m_oDamagedSystemsListBox
            // 
            this.m_oDamagedSystemsListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oDamagedSystemsListBox.FormattingEnabled = true;
            this.m_oDamagedSystemsListBox.Location = new System.Drawing.Point(6, 19);
            this.m_oDamagedSystemsListBox.Name = "m_oDamagedSystemsListBox";
            this.m_oDamagedSystemsListBox.Size = new System.Drawing.Size(279, 472);
            this.m_oDamagedSystemsListBox.TabIndex = 3;
            // 
            // m_oDACGroupBox
            // 
            this.m_oDACGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oDACGroupBox.Controls.Add(this.m_oDACListBox);
            this.m_oDACGroupBox.Location = new System.Drawing.Point(3, 3);
            this.m_oDACGroupBox.Name = "m_oDACGroupBox";
            this.m_oDACGroupBox.Size = new System.Drawing.Size(350, 534);
            this.m_oDACGroupBox.TabIndex = 34;
            this.m_oDACGroupBox.TabStop = false;
            this.m_oDACGroupBox.Text = "Damage Allocation Chart";
            // 
            // m_oDACListBox
            // 
            this.m_oDACListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oDACListBox.FormattingEnabled = true;
            this.m_oDACListBox.Location = new System.Drawing.Point(6, 19);
            this.m_oDACListBox.Name = "m_oDACListBox";
            this.m_oDACListBox.Size = new System.Drawing.Size(338, 511);
            this.m_oDACListBox.TabIndex = 2;
            // 
            // m_oParaCargoGUTab
            // 
            this.m_oParaCargoGUTab.Location = new System.Drawing.Point(4, 22);
            this.m_oParaCargoGUTab.Name = "m_oParaCargoGUTab";
            this.m_oParaCargoGUTab.Size = new System.Drawing.Size(1033, 540);
            this.m_oParaCargoGUTab.TabIndex = 3;
            this.m_oParaCargoGUTab.Text = "Parasites / Cargo / GU";
            this.m_oParaCargoGUTab.UseVisualStyleBackColor = true;
            // 
            // m_oMiscellaneousTab
            // 
            this.m_oMiscellaneousTab.Location = new System.Drawing.Point(4, 22);
            this.m_oMiscellaneousTab.Name = "m_oMiscellaneousTab";
            this.m_oMiscellaneousTab.Size = new System.Drawing.Size(1033, 540);
            this.m_oMiscellaneousTab.TabIndex = 4;
            this.m_oMiscellaneousTab.Text = "Miscellaneous";
            this.m_oMiscellaneousTab.UseVisualStyleBackColor = true;
            // 
            // m_oHistoryNotesTDTab
            // 
            this.m_oHistoryNotesTDTab.Location = new System.Drawing.Point(4, 22);
            this.m_oHistoryNotesTDTab.Name = "m_oHistoryNotesTDTab";
            this.m_oHistoryNotesTDTab.Size = new System.Drawing.Size(1033, 540);
            this.m_oHistoryNotesTDTab.TabIndex = 5;
            this.m_oHistoryNotesTDTab.Text = "History / Notes / Tech Data";
            this.m_oHistoryNotesTDTab.UseVisualStyleBackColor = true;
            // 
            // m_oShipDesignTab
            // 
            this.m_oShipDesignTab.Location = new System.Drawing.Point(4, 22);
            this.m_oShipDesignTab.Name = "m_oShipDesignTab";
            this.m_oShipDesignTab.Size = new System.Drawing.Size(1033, 540);
            this.m_oShipDesignTab.TabIndex = 6;
            this.m_oShipDesignTab.Text = "Ship Design Display";
            this.m_oShipDesignTab.UseVisualStyleBackColor = true;
            // 
            // m_oClassDesignTab
            // 
            this.m_oClassDesignTab.Controls.Add(this.m_oClassDesignGroupBox);
            this.m_oClassDesignTab.Location = new System.Drawing.Point(4, 22);
            this.m_oClassDesignTab.Name = "m_oClassDesignTab";
            this.m_oClassDesignTab.Size = new System.Drawing.Size(1033, 540);
            this.m_oClassDesignTab.TabIndex = 7;
            this.m_oClassDesignTab.Text = "Class Design Display";
            this.m_oClassDesignTab.UseVisualStyleBackColor = true;
            // 
            // m_oClassDesignGroupBox
            // 
            this.m_oClassDesignGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oClassDesignGroupBox.Controls.Add(this.m_oClassDesignRTBox);
            this.m_oClassDesignGroupBox.Location = new System.Drawing.Point(3, 3);
            this.m_oClassDesignGroupBox.Name = "m_oClassDesignGroupBox";
            this.m_oClassDesignGroupBox.Size = new System.Drawing.Size(1027, 534);
            this.m_oClassDesignGroupBox.TabIndex = 39;
            this.m_oClassDesignGroupBox.TabStop = false;
            // 
            // m_oClassDesignRTBox
            // 
            this.m_oClassDesignRTBox.Location = new System.Drawing.Point(13, 26);
            this.m_oClassDesignRTBox.Margin = new System.Windows.Forms.Padding(10);
            this.m_oClassDesignRTBox.Name = "m_oClassDesignRTBox";
            this.m_oClassDesignRTBox.Size = new System.Drawing.Size(1001, 495);
            this.m_oClassDesignRTBox.TabIndex = 0;
            this.m_oClassDesignRTBox.Text = "";
            // 
            // m_oCombatSettingsTab
            // 
            this.m_oCombatSettingsTab.Controls.Add(this.m_oActiveSensorGroupBox);
            this.m_oCombatSettingsTab.Controls.Add(this.m_oShieldGroupBox);
            this.m_oCombatSettingsTab.Controls.Add(this.m_oFireControlsGroupBox);
            this.m_oCombatSettingsTab.Controls.Add(this.m_oAutoFireGroupBox);
            this.m_oCombatSettingsTab.Controls.Add(this.m_oECCMAssignGroupBox);
            this.m_oCombatSettingsTab.Controls.Add(this.m_oPointDefenseModeGroupBox);
            this.m_oCombatSettingsTab.Controls.Add(this.m_oSelectedFireControlGroupBox);
            this.m_oCombatSettingsTab.Controls.Add(this.m_oAssignTargetsGroupBox);
            this.m_oCombatSettingsTab.Controls.Add(this.m_oAssignWeaponsGroupBox);
            this.m_oCombatSettingsTab.Location = new System.Drawing.Point(4, 22);
            this.m_oCombatSettingsTab.Name = "m_oCombatSettingsTab";
            this.m_oCombatSettingsTab.Size = new System.Drawing.Size(1033, 540);
            this.m_oCombatSettingsTab.TabIndex = 8;
            this.m_oCombatSettingsTab.Text = "Combat Settings";
            this.m_oCombatSettingsTab.UseVisualStyleBackColor = true;
            // 
            // m_oActiveSensorGroupBox
            // 
            this.m_oActiveSensorGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oActiveSensorGroupBox.Controls.Add(this.m_oSelectedActiveComboBox);
            this.m_oActiveSensorGroupBox.Controls.Add(this.m_oInactiveButton);
            this.m_oActiveSensorGroupBox.Controls.Add(this.m_oActiveButton);
            this.m_oActiveSensorGroupBox.Location = new System.Drawing.Point(3, 418);
            this.m_oActiveSensorGroupBox.Name = "m_oActiveSensorGroupBox";
            this.m_oActiveSensorGroupBox.Size = new System.Drawing.Size(247, 119);
            this.m_oActiveSensorGroupBox.TabIndex = 41;
            this.m_oActiveSensorGroupBox.TabStop = false;
            this.m_oActiveSensorGroupBox.Text = "Selected Active(Off)";
            // 
            // m_oSelectedActiveComboBox
            // 
            this.m_oSelectedActiveComboBox.FormattingEnabled = true;
            this.m_oSelectedActiveComboBox.Location = new System.Drawing.Point(6, 19);
            this.m_oSelectedActiveComboBox.Name = "m_oSelectedActiveComboBox";
            this.m_oSelectedActiveComboBox.Size = new System.Drawing.Size(229, 21);
            this.m_oSelectedActiveComboBox.TabIndex = 52;
            // 
            // m_oInactiveButton
            // 
            this.m_oInactiveButton.BackColor = System.Drawing.Color.LimeGreen;
            this.m_oInactiveButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oInactiveButton.Location = new System.Drawing.Point(142, 69);
            this.m_oInactiveButton.Margin = new System.Windows.Forms.Padding(12);
            this.m_oInactiveButton.Name = "m_oInactiveButton";
            this.m_oInactiveButton.Size = new System.Drawing.Size(90, 35);
            this.m_oInactiveButton.TabIndex = 51;
            this.m_oInactiveButton.Text = "Inactive";
            this.m_oInactiveButton.UseVisualStyleBackColor = false;
            // 
            // m_oActiveButton
            // 
            this.m_oActiveButton.BackColor = System.Drawing.Color.Red;
            this.m_oActiveButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oActiveButton.Location = new System.Drawing.Point(15, 69);
            this.m_oActiveButton.Margin = new System.Windows.Forms.Padding(12);
            this.m_oActiveButton.Name = "m_oActiveButton";
            this.m_oActiveButton.Size = new System.Drawing.Size(90, 35);
            this.m_oActiveButton.TabIndex = 51;
            this.m_oActiveButton.Text = "Active";
            this.m_oActiveButton.UseVisualStyleBackColor = false;
            // 
            // m_oShieldGroupBox
            // 
            this.m_oShieldGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oShieldGroupBox.Controls.Add(this.m_oLowerShieldsButton);
            this.m_oShieldGroupBox.Controls.Add(this.m_oRaiseShieldsButton);
            this.m_oShieldGroupBox.Location = new System.Drawing.Point(3, 342);
            this.m_oShieldGroupBox.Name = "m_oShieldGroupBox";
            this.m_oShieldGroupBox.Size = new System.Drawing.Size(247, 70);
            this.m_oShieldGroupBox.TabIndex = 41;
            this.m_oShieldGroupBox.TabStop = false;
            this.m_oShieldGroupBox.Text = "Shields(Off)";
            // 
            // m_oLowerShieldsButton
            // 
            this.m_oLowerShieldsButton.BackColor = System.Drawing.Color.LimeGreen;
            this.m_oLowerShieldsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oLowerShieldsButton.Location = new System.Drawing.Point(136, 20);
            this.m_oLowerShieldsButton.Margin = new System.Windows.Forms.Padding(12);
            this.m_oLowerShieldsButton.Name = "m_oLowerShieldsButton";
            this.m_oLowerShieldsButton.Size = new System.Drawing.Size(90, 35);
            this.m_oLowerShieldsButton.TabIndex = 50;
            this.m_oLowerShieldsButton.Text = "Lower";
            this.m_oLowerShieldsButton.UseVisualStyleBackColor = false;
            // 
            // m_oRaiseShieldsButton
            // 
            this.m_oRaiseShieldsButton.BackColor = System.Drawing.Color.Red;
            this.m_oRaiseShieldsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oRaiseShieldsButton.Location = new System.Drawing.Point(15, 20);
            this.m_oRaiseShieldsButton.Margin = new System.Windows.Forms.Padding(12);
            this.m_oRaiseShieldsButton.Name = "m_oRaiseShieldsButton";
            this.m_oRaiseShieldsButton.Size = new System.Drawing.Size(90, 35);
            this.m_oRaiseShieldsButton.TabIndex = 50;
            this.m_oRaiseShieldsButton.Text = "Raise";
            this.m_oRaiseShieldsButton.UseVisualStyleBackColor = false;
            // 
            // m_oFireControlsGroupBox
            // 
            this.m_oFireControlsGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oFireControlsGroupBox.Controls.Add(this.m_oCeaseFireButton);
            this.m_oFireControlsGroupBox.Controls.Add(this.m_oOpenFireButton);
            this.m_oFireControlsGroupBox.Location = new System.Drawing.Point(3, 266);
            this.m_oFireControlsGroupBox.Name = "m_oFireControlsGroupBox";
            this.m_oFireControlsGroupBox.Size = new System.Drawing.Size(241, 70);
            this.m_oFireControlsGroupBox.TabIndex = 41;
            this.m_oFireControlsGroupBox.TabStop = false;
            this.m_oFireControlsGroupBox.Text = "Fire Controls";
            // 
            // m_oCeaseFireButton
            // 
            this.m_oCeaseFireButton.BackColor = System.Drawing.Color.LimeGreen;
            this.m_oCeaseFireButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oCeaseFireButton.Location = new System.Drawing.Point(136, 20);
            this.m_oCeaseFireButton.Margin = new System.Windows.Forms.Padding(12);
            this.m_oCeaseFireButton.Name = "m_oCeaseFireButton";
            this.m_oCeaseFireButton.Size = new System.Drawing.Size(90, 35);
            this.m_oCeaseFireButton.TabIndex = 49;
            this.m_oCeaseFireButton.Text = "Cease Fire";
            this.m_oCeaseFireButton.UseVisualStyleBackColor = false;
            // 
            // m_oOpenFireButton
            // 
            this.m_oOpenFireButton.BackColor = System.Drawing.Color.Red;
            this.m_oOpenFireButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oOpenFireButton.Location = new System.Drawing.Point(15, 20);
            this.m_oOpenFireButton.Margin = new System.Windows.Forms.Padding(12);
            this.m_oOpenFireButton.Name = "m_oOpenFireButton";
            this.m_oOpenFireButton.Size = new System.Drawing.Size(90, 35);
            this.m_oOpenFireButton.TabIndex = 48;
            this.m_oOpenFireButton.Text = "Open Fire";
            this.m_oOpenFireButton.UseVisualStyleBackColor = false;
            // 
            // m_oAutoFireGroupBox
            // 
            this.m_oAutoFireGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oAutoFireGroupBox.Controls.Add(this.m_oSyncFireCheckBox);
            this.m_oAutoFireGroupBox.Controls.Add(this.m_oAutoFireCheckBox);
            this.m_oAutoFireGroupBox.Location = new System.Drawing.Point(3, 210);
            this.m_oAutoFireGroupBox.Name = "m_oAutoFireGroupBox";
            this.m_oAutoFireGroupBox.Size = new System.Drawing.Size(241, 50);
            this.m_oAutoFireGroupBox.TabIndex = 41;
            this.m_oAutoFireGroupBox.TabStop = false;
            this.m_oAutoFireGroupBox.Text = "Automated Firing Options";
            // 
            // m_oSyncFireCheckBox
            // 
            this.m_oSyncFireCheckBox.AutoSize = true;
            this.m_oSyncFireCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oSyncFireCheckBox.Location = new System.Drawing.Point(133, 23);
            this.m_oSyncFireCheckBox.Name = "m_oSyncFireCheckBox";
            this.m_oSyncFireCheckBox.Size = new System.Drawing.Size(76, 17);
            this.m_oSyncFireCheckBox.TabIndex = 50;
            this.m_oSyncFireCheckBox.Text = "Synch Fire";
            this.m_oSyncFireCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oAutoFireCheckBox
            // 
            this.m_oAutoFireCheckBox.AutoSize = true;
            this.m_oAutoFireCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oAutoFireCheckBox.Location = new System.Drawing.Point(32, 23);
            this.m_oAutoFireCheckBox.Name = "m_oAutoFireCheckBox";
            this.m_oAutoFireCheckBox.Size = new System.Drawing.Size(68, 17);
            this.m_oAutoFireCheckBox.TabIndex = 49;
            this.m_oAutoFireCheckBox.Text = "Auto Fire";
            this.m_oAutoFireCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oECCMAssignGroupBox
            // 
            this.m_oECCMAssignGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oECCMAssignGroupBox.Controls.Add(this.m_oAssignECCMButton);
            this.m_oECCMAssignGroupBox.Controls.Add(this.m_oECCMComboBox);
            this.m_oECCMAssignGroupBox.Location = new System.Drawing.Point(3, 154);
            this.m_oECCMAssignGroupBox.Name = "m_oECCMAssignGroupBox";
            this.m_oECCMAssignGroupBox.Size = new System.Drawing.Size(241, 50);
            this.m_oECCMAssignGroupBox.TabIndex = 41;
            this.m_oECCMAssignGroupBox.TabStop = false;
            this.m_oECCMAssignGroupBox.Text = "Assign ECCM to FireControl";
            // 
            // m_oAssignECCMButton
            // 
            this.m_oAssignECCMButton.Location = new System.Drawing.Point(166, 15);
            this.m_oAssignECCMButton.Name = "m_oAssignECCMButton";
            this.m_oAssignECCMButton.Size = new System.Drawing.Size(69, 26);
            this.m_oAssignECCMButton.TabIndex = 51;
            this.m_oAssignECCMButton.Text = "Assign";
            this.m_oAssignECCMButton.UseVisualStyleBackColor = true;
            // 
            // m_oECCMComboBox
            // 
            this.m_oECCMComboBox.FormattingEnabled = true;
            this.m_oECCMComboBox.Location = new System.Drawing.Point(6, 19);
            this.m_oECCMComboBox.Name = "m_oECCMComboBox";
            this.m_oECCMComboBox.Size = new System.Drawing.Size(142, 21);
            this.m_oECCMComboBox.TabIndex = 50;
            // 
            // m_oPointDefenseModeGroupBox
            // 
            this.m_oPointDefenseModeGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oPointDefenseModeGroupBox.Controls.Add(this.m_oPDRangeLabel2);
            this.m_oPointDefenseModeGroupBox.Controls.Add(this.m_oSelectRangeLabel);
            this.m_oPointDefenseModeGroupBox.Controls.Add(this.m_oPDRangeTextBox);
            this.m_oPointDefenseModeGroupBox.Controls.Add(this.m_oSetModeButton);
            this.m_oPointDefenseModeGroupBox.Controls.Add(this.m_oPDModeComboBox);
            this.m_oPointDefenseModeGroupBox.Location = new System.Drawing.Point(3, 61);
            this.m_oPointDefenseModeGroupBox.Name = "m_oPointDefenseModeGroupBox";
            this.m_oPointDefenseModeGroupBox.Size = new System.Drawing.Size(241, 87);
            this.m_oPointDefenseModeGroupBox.TabIndex = 41;
            this.m_oPointDefenseModeGroupBox.TabStop = false;
            this.m_oPointDefenseModeGroupBox.Text = "Point Defense Mode for SFC";
            // 
            // m_oPDRangeLabel2
            // 
            this.m_oPDRangeLabel2.AutoSize = true;
            this.m_oPDRangeLabel2.Location = new System.Drawing.Point(6, 66);
            this.m_oPDRangeLabel2.Name = "m_oPDRangeLabel2";
            this.m_oPDRangeLabel2.Size = new System.Drawing.Size(63, 13);
            this.m_oPDRangeLabel2.TabIndex = 54;
            this.m_oPDRangeLabel2.Text = "in 10k Units";
            // 
            // m_oSelectRangeLabel
            // 
            this.m_oSelectRangeLabel.AutoSize = true;
            this.m_oSelectRangeLabel.Location = new System.Drawing.Point(6, 53);
            this.m_oSelectRangeLabel.Name = "m_oSelectRangeLabel";
            this.m_oSelectRangeLabel.Size = new System.Drawing.Size(80, 13);
            this.m_oSelectRangeLabel.TabIndex = 53;
            this.m_oSelectRangeLabel.Text = "Max PD Range";
            // 
            // m_oPDRangeTextBox
            // 
            this.m_oPDRangeTextBox.Location = new System.Drawing.Point(105, 57);
            this.m_oPDRangeTextBox.Name = "m_oPDRangeTextBox";
            this.m_oPDRangeTextBox.Size = new System.Drawing.Size(43, 20);
            this.m_oPDRangeTextBox.TabIndex = 50;
            this.m_oPDRangeTextBox.Text = "1000";
            // 
            // m_oSetModeButton
            // 
            this.m_oSetModeButton.Location = new System.Drawing.Point(166, 53);
            this.m_oSetModeButton.Name = "m_oSetModeButton";
            this.m_oSetModeButton.Size = new System.Drawing.Size(69, 26);
            this.m_oSetModeButton.TabIndex = 52;
            this.m_oSetModeButton.Text = "Set Mode";
            this.m_oSetModeButton.UseVisualStyleBackColor = true;
            // 
            // m_oPDModeComboBox
            // 
            this.m_oPDModeComboBox.FormattingEnabled = true;
            this.m_oPDModeComboBox.Location = new System.Drawing.Point(6, 19);
            this.m_oPDModeComboBox.Name = "m_oPDModeComboBox";
            this.m_oPDModeComboBox.Size = new System.Drawing.Size(229, 21);
            this.m_oPDModeComboBox.TabIndex = 49;
            // 
            // m_oSelectedFireControlGroupBox
            // 
            this.m_oSelectedFireControlGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oSelectedFireControlGroupBox.Controls.Add(this.m_oSFCComboBox);
            this.m_oSelectedFireControlGroupBox.Location = new System.Drawing.Point(3, 3);
            this.m_oSelectedFireControlGroupBox.Name = "m_oSelectedFireControlGroupBox";
            this.m_oSelectedFireControlGroupBox.Size = new System.Drawing.Size(241, 52);
            this.m_oSelectedFireControlGroupBox.TabIndex = 40;
            this.m_oSelectedFireControlGroupBox.TabStop = false;
            this.m_oSelectedFireControlGroupBox.Text = "Selected Fire Control(SFC)";
            // 
            // m_oSFCComboBox
            // 
            this.m_oSFCComboBox.FormattingEnabled = true;
            this.m_oSFCComboBox.Location = new System.Drawing.Point(6, 19);
            this.m_oSFCComboBox.Name = "m_oSFCComboBox";
            this.m_oSFCComboBox.Size = new System.Drawing.Size(229, 21);
            this.m_oSFCComboBox.TabIndex = 48;
            // 
            // m_oAssignTargetsGroupBox
            // 
            this.m_oAssignTargetsGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oAssignTargetsGroupBox.Controls.Add(this.m_oClearTargetButton);
            this.m_oAssignTargetsGroupBox.Controls.Add(this.m_oAssignTargetButton);
            this.m_oAssignTargetsGroupBox.Controls.Add(this.m_oContactsListBox);
            this.m_oAssignTargetsGroupBox.Location = new System.Drawing.Point(250, 3);
            this.m_oAssignTargetsGroupBox.Name = "m_oAssignTargetsGroupBox";
            this.m_oAssignTargetsGroupBox.Size = new System.Drawing.Size(322, 534);
            this.m_oAssignTargetsGroupBox.TabIndex = 39;
            this.m_oAssignTargetsGroupBox.TabStop = false;
            this.m_oAssignTargetsGroupBox.Text = "Assign Targets to Selected Fire Control";
            // 
            // m_oClearTargetButton
            // 
            this.m_oClearTargetButton.Location = new System.Drawing.Point(247, 497);
            this.m_oClearTargetButton.Name = "m_oClearTargetButton";
            this.m_oClearTargetButton.Size = new System.Drawing.Size(69, 26);
            this.m_oClearTargetButton.TabIndex = 53;
            this.m_oClearTargetButton.Text = "Clear";
            this.m_oClearTargetButton.UseVisualStyleBackColor = true;
            // 
            // m_oAssignTargetButton
            // 
            this.m_oAssignTargetButton.Location = new System.Drawing.Point(6, 497);
            this.m_oAssignTargetButton.Name = "m_oAssignTargetButton";
            this.m_oAssignTargetButton.Size = new System.Drawing.Size(69, 26);
            this.m_oAssignTargetButton.TabIndex = 52;
            this.m_oAssignTargetButton.Text = "Assign";
            this.m_oAssignTargetButton.UseVisualStyleBackColor = true;
            // 
            // m_oContactsListBox
            // 
            this.m_oContactsListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oContactsListBox.FormattingEnabled = true;
            this.m_oContactsListBox.Location = new System.Drawing.Point(6, 19);
            this.m_oContactsListBox.Name = "m_oContactsListBox";
            this.m_oContactsListBox.Size = new System.Drawing.Size(310, 472);
            this.m_oContactsListBox.TabIndex = 47;
            // 
            // m_oAssignWeaponsGroupBox
            // 
            this.m_oAssignWeaponsGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oAssignWeaponsGroupBox.Controls.Add(this.m_oClearWeaponsButton);
            this.m_oAssignWeaponsGroupBox.Controls.Add(this.m_oAssignAllWeaponsButton);
            this.m_oAssignWeaponsGroupBox.Controls.Add(this.m_oAssignWeaponsButton);
            this.m_oAssignWeaponsGroupBox.Controls.Add(this.m_oWeaponListBox);
            this.m_oAssignWeaponsGroupBox.Location = new System.Drawing.Point(578, 3);
            this.m_oAssignWeaponsGroupBox.Name = "m_oAssignWeaponsGroupBox";
            this.m_oAssignWeaponsGroupBox.Size = new System.Drawing.Size(452, 534);
            this.m_oAssignWeaponsGroupBox.TabIndex = 38;
            this.m_oAssignWeaponsGroupBox.TabStop = false;
            this.m_oAssignWeaponsGroupBox.Text = "Assign Weapons to Selected Fire Control";
            // 
            // m_oClearWeaponsButton
            // 
            this.m_oClearWeaponsButton.Location = new System.Drawing.Point(377, 497);
            this.m_oClearWeaponsButton.Name = "m_oClearWeaponsButton";
            this.m_oClearWeaponsButton.Size = new System.Drawing.Size(69, 26);
            this.m_oClearWeaponsButton.TabIndex = 56;
            this.m_oClearWeaponsButton.Text = "Clear";
            this.m_oClearWeaponsButton.UseVisualStyleBackColor = true;
            // 
            // m_oAssignAllWeaponsButton
            // 
            this.m_oAssignAllWeaponsButton.Location = new System.Drawing.Point(81, 497);
            this.m_oAssignAllWeaponsButton.Name = "m_oAssignAllWeaponsButton";
            this.m_oAssignAllWeaponsButton.Size = new System.Drawing.Size(69, 26);
            this.m_oAssignAllWeaponsButton.TabIndex = 55;
            this.m_oAssignAllWeaponsButton.Text = "Assign All";
            this.m_oAssignAllWeaponsButton.UseVisualStyleBackColor = true;
            // 
            // m_oAssignWeaponsButton
            // 
            this.m_oAssignWeaponsButton.Location = new System.Drawing.Point(6, 497);
            this.m_oAssignWeaponsButton.Name = "m_oAssignWeaponsButton";
            this.m_oAssignWeaponsButton.Size = new System.Drawing.Size(69, 26);
            this.m_oAssignWeaponsButton.TabIndex = 54;
            this.m_oAssignWeaponsButton.Text = "Assign";
            this.m_oAssignWeaponsButton.UseVisualStyleBackColor = true;
            // 
            // m_oWeaponListBox
            // 
            this.m_oWeaponListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oWeaponListBox.FormattingEnabled = true;
            this.m_oWeaponListBox.Location = new System.Drawing.Point(6, 19);
            this.m_oWeaponListBox.Name = "m_oWeaponListBox";
            this.m_oWeaponListBox.Size = new System.Drawing.Size(440, 472);
            this.m_oWeaponListBox.TabIndex = 47;
            // 
            // m_oCombatSummaryTab
            // 
            this.m_oCombatSummaryTab.Controls.Add(this.m_oCombatSummaryGroupBox);
            this.m_oCombatSummaryTab.Location = new System.Drawing.Point(4, 22);
            this.m_oCombatSummaryTab.Name = "m_oCombatSummaryTab";
            this.m_oCombatSummaryTab.Size = new System.Drawing.Size(1033, 540);
            this.m_oCombatSummaryTab.TabIndex = 9;
            this.m_oCombatSummaryTab.Text = "Combat Summary";
            this.m_oCombatSummaryTab.UseVisualStyleBackColor = true;
            // 
            // m_oCombatSummaryGroupBox
            // 
            this.m_oCombatSummaryGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oCombatSummaryGroupBox.Controls.Add(this.m_oCombatSummaryRTBox);
            this.m_oCombatSummaryGroupBox.Location = new System.Drawing.Point(3, 3);
            this.m_oCombatSummaryGroupBox.Name = "m_oCombatSummaryGroupBox";
            this.m_oCombatSummaryGroupBox.Size = new System.Drawing.Size(1027, 534);
            this.m_oCombatSummaryGroupBox.TabIndex = 38;
            this.m_oCombatSummaryGroupBox.TabStop = false;
            this.m_oCombatSummaryGroupBox.Text = "Weapon and Fire Control Summary";
            // 
            // m_oCombatSummaryRTBox
            // 
            this.m_oCombatSummaryRTBox.Location = new System.Drawing.Point(13, 26);
            this.m_oCombatSummaryRTBox.Margin = new System.Windows.Forms.Padding(10);
            this.m_oCombatSummaryRTBox.Name = "m_oCombatSummaryRTBox";
            this.m_oCombatSummaryRTBox.Size = new System.Drawing.Size(1001, 495);
            this.m_oCombatSummaryRTBox.TabIndex = 0;
            this.m_oCombatSummaryRTBox.Text = "";
            // 
            // m_oOrdnanceManagementTab
            // 
            this.m_oOrdnanceManagementTab.Location = new System.Drawing.Point(4, 22);
            this.m_oOrdnanceManagementTab.Name = "m_oOrdnanceManagementTab";
            this.m_oOrdnanceManagementTab.Size = new System.Drawing.Size(1033, 540);
            this.m_oOrdnanceManagementTab.TabIndex = 10;
            this.m_oOrdnanceManagementTab.Text = "Ordnance Management";
            this.m_oOrdnanceManagementTab.UseVisualStyleBackColor = true;
            // 
            // Individual_Unit_Details_Panel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1296, 914);
            this.Controls.Add(this.m_oTaskGroupTabControl);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "Individual_Unit_Details_Panel";
            this.Text = "Individual_Unit_Details_Panel";
            this.m_oTaskGroupTabControl.ResumeLayout(false);
            this.m_oArmourStatusTab.ResumeLayout(false);
            this.m_oDamageControlTab.ResumeLayout(false);
            this.m_oAbandonShipGroupBox.ResumeLayout(false);
            this.m_oManualDamageGroupBox.ResumeLayout(false);
            this.m_oManualDamageGroupBox.PerformLayout();
            this.m_oDCQGroupBox.ResumeLayout(false);
            this.m_oCurrentDCGroupBox.ResumeLayout(false);
            this.m_oCurrentDCGroupBox.PerformLayout();
            this.m_oDamagedSystemsGroupBox.ResumeLayout(false);
            this.m_oDACGroupBox.ResumeLayout(false);
            this.m_oClassDesignTab.ResumeLayout(false);
            this.m_oClassDesignGroupBox.ResumeLayout(false);
            this.m_oCombatSettingsTab.ResumeLayout(false);
            this.m_oActiveSensorGroupBox.ResumeLayout(false);
            this.m_oShieldGroupBox.ResumeLayout(false);
            this.m_oFireControlsGroupBox.ResumeLayout(false);
            this.m_oAutoFireGroupBox.ResumeLayout(false);
            this.m_oAutoFireGroupBox.PerformLayout();
            this.m_oECCMAssignGroupBox.ResumeLayout(false);
            this.m_oPointDefenseModeGroupBox.ResumeLayout(false);
            this.m_oPointDefenseModeGroupBox.PerformLayout();
            this.m_oSelectedFireControlGroupBox.ResumeLayout(false);
            this.m_oAssignTargetsGroupBox.ResumeLayout(false);
            this.m_oAssignWeaponsGroupBox.ResumeLayout(false);
            this.m_oCombatSummaryTab.ResumeLayout(false);
            this.m_oCombatSummaryGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl m_oTaskGroupTabControl;
        private System.Windows.Forms.TabPage m_oArmourStatusTab;
        private System.Windows.Forms.TabPage m_oDamageControlTab;
        private System.Windows.Forms.TabPage m_oParaCargoGUTab;
        private System.Windows.Forms.TabPage m_oMiscellaneousTab;
        private System.Windows.Forms.TabPage m_oHistoryNotesTDTab;
        private System.Windows.Forms.TabPage m_oShipDesignTab;
        private System.Windows.Forms.TabPage m_oClassDesignTab;
        private System.Windows.Forms.TabPage m_oCombatSettingsTab;
        private System.Windows.Forms.TabPage m_oCombatSummaryTab;
        private System.Windows.Forms.TabPage m_oOrdnanceManagementTab;
        private System.Windows.Forms.GroupBox m_oArmorGroupBox;
        private System.Windows.Forms.GroupBox m_oAbandonShipGroupBox;
        private System.Windows.Forms.GroupBox m_oManualDamageGroupBox;
        private System.Windows.Forms.GroupBox m_oDCQGroupBox;
        private System.Windows.Forms.GroupBox m_oCurrentDCGroupBox;
        private System.Windows.Forms.GroupBox m_oDamagedSystemsGroupBox;
        private System.Windows.Forms.GroupBox m_oDACGroupBox;
        private System.Windows.Forms.ListBox m_oDACListBox;
        private System.Windows.Forms.ListBox m_oDamagedSystemsListBox;
        private Button m_oAddToQueueButton;
        private Button m_oBeginDCButton;
        private Button m_oDemoteItemButton;
        private Button m_oAdvanceItemButton;
        private Button m_oRemoveItemButton;
        private ListBox m_oDamageControlQueueListBox;
        private Button m_oAbandonShipButton;
        private Button m_oApplyDamageButton;
        private TextBox m_oApplyDamageTextBox;
        private Button m_oAbortDCButton;
        private TextBox m_oCurrentDCTargetTextBox;
        private GroupBox m_oActiveSensorGroupBox;
        private GroupBox m_oShieldGroupBox;
        private GroupBox m_oFireControlsGroupBox;
        private GroupBox m_oAutoFireGroupBox;
        private GroupBox m_oECCMAssignGroupBox;
        private GroupBox m_oPointDefenseModeGroupBox;
        private GroupBox m_oSelectedFireControlGroupBox;
        private GroupBox m_oAssignTargetsGroupBox;
        private GroupBox m_oAssignWeaponsGroupBox;
        private ListBox m_oContactsListBox;
        private ListBox m_oWeaponListBox;
        private ComboBox m_oECCMComboBox;
        private ComboBox m_oPDModeComboBox;
        private ComboBox m_oSFCComboBox;
        private CheckBox m_oSyncFireCheckBox;
        private CheckBox m_oAutoFireCheckBox;
        private Button m_oOpenFireButton;
        private Button m_oAssignECCMButton;
        private Button m_oSetModeButton;
        private TextBox m_oPDRangeTextBox;
        private Label m_oSelectRangeLabel;
        private Label m_oPDRangeLabel2;
        private ComboBox m_oSelectedActiveComboBox;
        private Button m_oInactiveButton;
        private Button m_oActiveButton;
        private Button m_oLowerShieldsButton;
        private Button m_oRaiseShieldsButton;
        private Button m_oCeaseFireButton;
        private Button m_oClearTargetButton;
        private Button m_oAssignTargetButton;
        private Button m_oClearWeaponsButton;
        private Button m_oAssignAllWeaponsButton;
        private Button m_oAssignWeaponsButton;
        private GroupBox m_oCombatSummaryGroupBox;
        private RichTextBox m_oCombatSummaryRTBox;
        private GroupBox m_oClassDesignGroupBox;
        private RichTextBox m_oClassDesignRTBox;
    }
}