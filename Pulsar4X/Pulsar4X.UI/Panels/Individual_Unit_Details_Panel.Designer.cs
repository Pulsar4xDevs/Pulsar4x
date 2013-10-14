namespace Pulsar4X.UI.Panels
{
    partial class Individual_Unit_Details_Panel
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.m_oParaCargoGUTab = new System.Windows.Forms.TabPage();
            this.m_oMiscellaneousTab = new System.Windows.Forms.TabPage();
            this.m_oHistoryNotesTDTab = new System.Windows.Forms.TabPage();
            this.m_oShipDesignTab = new System.Windows.Forms.TabPage();
            this.m_oClassDesignTab = new System.Windows.Forms.TabPage();
            this.m_oCombatSettingsTab = new System.Windows.Forms.TabPage();
            this.m_oCombatSummaryTab = new System.Windows.Forms.TabPage();
            this.m_oOrdnanceManagementTab = new System.Windows.Forms.TabPage();
            this.m_oTaskGroupTabControl.SuspendLayout();
            this.m_oArmourStatusTab.SuspendLayout();
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
            this.m_oTaskGroupTabControl.Location = new System.Drawing.Point(56, 166);
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
            this.m_oDamageControlTab.Location = new System.Drawing.Point(4, 22);
            this.m_oDamageControlTab.Name = "m_oDamageControlTab";
            this.m_oDamageControlTab.Size = new System.Drawing.Size(1033, 540);
            this.m_oDamageControlTab.TabIndex = 2;
            this.m_oDamageControlTab.Text = "Damage Control";
            this.m_oDamageControlTab.UseVisualStyleBackColor = true;
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
            this.m_oClassDesignTab.Location = new System.Drawing.Point(4, 22);
            this.m_oClassDesignTab.Name = "m_oClassDesignTab";
            this.m_oClassDesignTab.Size = new System.Drawing.Size(1033, 540);
            this.m_oClassDesignTab.TabIndex = 7;
            this.m_oClassDesignTab.Text = "Class Design Display";
            this.m_oClassDesignTab.UseVisualStyleBackColor = true;
            // 
            // m_oCombatSettingsTab
            // 
            this.m_oCombatSettingsTab.Location = new System.Drawing.Point(4, 22);
            this.m_oCombatSettingsTab.Name = "m_oCombatSettingsTab";
            this.m_oCombatSettingsTab.Size = new System.Drawing.Size(1033, 540);
            this.m_oCombatSettingsTab.TabIndex = 8;
            this.m_oCombatSettingsTab.Text = "Combat Settings";
            this.m_oCombatSettingsTab.UseVisualStyleBackColor = true;
            // 
            // m_oCombatSummaryTab
            // 
            this.m_oCombatSummaryTab.Location = new System.Drawing.Point(4, 22);
            this.m_oCombatSummaryTab.Name = "m_oCombatSummaryTab";
            this.m_oCombatSummaryTab.Size = new System.Drawing.Size(1033, 540);
            this.m_oCombatSummaryTab.TabIndex = 9;
            this.m_oCombatSummaryTab.Text = "Combat Summary";
            this.m_oCombatSummaryTab.UseVisualStyleBackColor = true;
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
    }
}