using System.Windows.Forms;

namespace Pulsar4X.UI.Panels
{
    partial class FastOOB_Panel
    {
        /// <summary>
        /// combo box list of empires in game.
        /// </summary>
        public ComboBox EmpireComboBox
        {
            get { return m_oEmpireComboBox; }
        }

        /// <summary>
        /// Combobox for taskgroups
        /// </summary>
        public ComboBox TaskGroupComboBox
        {
            get { return m_oTaskGroupComboBox; }
        }

        /// <summary>
        /// Faction ship classes
        /// </summary>
        public ComboBox ClassComboBox
        {
            get { return m_oClassComboBox; }
        }

        /// <summary>
        /// Shipclass summary text box
        /// </summary>
        public RichTextBox SummaryTextBox
        {
            get { return m_oSummaryRichTextBox; }
        }
        
        /// <summary>
        /// Add this ship class to this taskgroup
        /// </summary>
        public Button AddButton
        {
            get { return m_oAddButton; }
        }

        /// <summary>
        /// close the display
        /// </summary>
        public Button CloseButton
        {
            get { return m_oCloseButton; }
        }

        /// <summary>
        /// Add this many ships.
        /// </summary>
        public TextBox NumberTextBox
        {
            get { return m_oNumberTextBox; }
        }

        /// <summary>
        /// cost of the ship.
        /// </summary>
        public TextBox CostTextBox
        {
            get { return m_oCostTextBox; }
        }

        /// <summary>
        /// Faction Ship BPs available
        /// </summary>
        public TextBox ShipBPTextBox
        {
            get { return m_oShipBPTextBox; }
        }


        /// <summary>
        /// Faction PDC BPs available
        /// </summary>
        public TextBox PDCBPTextBox
        {
            get { return m_oPDCBPTextBox; }
        }

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
            this.m_oAddShipGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oCostLabel = new System.Windows.Forms.Label();
            this.m_oNumLabel = new System.Windows.Forms.Label();
            this.m_oClassLabel = new System.Windows.Forms.Label();
            this.m_oTGLabel = new System.Windows.Forms.Label();
            this.m_oSpeciesLabel = new System.Windows.Forms.Label();
            this.m_oFactionLabel = new System.Windows.Forms.Label();
            this.m_oCostTextBox = new System.Windows.Forms.TextBox();
            this.m_oNumberTextBox = new System.Windows.Forms.TextBox();
            this.m_oClassComboBox = new System.Windows.Forms.ComboBox();
            this.m_oTaskGroupComboBox = new System.Windows.Forms.ComboBox();
            this.m_oSpeciesComboBox = new System.Windows.Forms.ComboBox();
            this.m_oEmpireComboBox = new System.Windows.Forms.ComboBox();
            this.m_oRemBPGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oPDCLabel = new System.Windows.Forms.Label();
            this.m_oShipLabel = new System.Windows.Forms.Label();
            this.m_oPDCBPTextBox = new System.Windows.Forms.TextBox();
            this.m_oShipBPTextBox = new System.Windows.Forms.TextBox();
            this.m_oClassSummaryGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oSummaryRichTextBox = new System.Windows.Forms.RichTextBox();
            this.m_oAddButton = new System.Windows.Forms.Button();
            this.m_oCloseButton = new System.Windows.Forms.Button();
            this.m_oAddShipGroupBox.SuspendLayout();
            this.m_oRemBPGroupBox.SuspendLayout();
            this.m_oClassSummaryGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_oAddShipGroupBox
            // 
            this.m_oAddShipGroupBox.Controls.Add(this.m_oCostLabel);
            this.m_oAddShipGroupBox.Controls.Add(this.m_oNumLabel);
            this.m_oAddShipGroupBox.Controls.Add(this.m_oClassLabel);
            this.m_oAddShipGroupBox.Controls.Add(this.m_oTGLabel);
            this.m_oAddShipGroupBox.Controls.Add(this.m_oSpeciesLabel);
            this.m_oAddShipGroupBox.Controls.Add(this.m_oFactionLabel);
            this.m_oAddShipGroupBox.Controls.Add(this.m_oCostTextBox);
            this.m_oAddShipGroupBox.Controls.Add(this.m_oNumberTextBox);
            this.m_oAddShipGroupBox.Controls.Add(this.m_oClassComboBox);
            this.m_oAddShipGroupBox.Controls.Add(this.m_oTaskGroupComboBox);
            this.m_oAddShipGroupBox.Controls.Add(this.m_oSpeciesComboBox);
            this.m_oAddShipGroupBox.Controls.Add(this.m_oEmpireComboBox);
            this.m_oAddShipGroupBox.Location = new System.Drawing.Point(12, 12);
            this.m_oAddShipGroupBox.Name = "m_oAddShipGroupBox";
            this.m_oAddShipGroupBox.Size = new System.Drawing.Size(253, 173);
            this.m_oAddShipGroupBox.TabIndex = 0;
            this.m_oAddShipGroupBox.TabStop = false;
            // 
            // m_oCostLabel
            // 
            this.m_oCostLabel.AutoSize = true;
            this.m_oCostLabel.Location = new System.Drawing.Point(124, 147);
            this.m_oCostLabel.Name = "m_oCostLabel";
            this.m_oCostLabel.Size = new System.Drawing.Size(55, 13);
            this.m_oCostLabel.TabIndex = 11;
            this.m_oCostLabel.Text = "Total Cost";
            // 
            // m_oNumLabel
            // 
            this.m_oNumLabel.AutoSize = true;
            this.m_oNumLabel.Location = new System.Drawing.Point(6, 147);
            this.m_oNumLabel.Name = "m_oNumLabel";
            this.m_oNumLabel.Size = new System.Drawing.Size(44, 13);
            this.m_oNumLabel.TabIndex = 10;
            this.m_oNumLabel.Text = "Number";
            // 
            // m_oClassLabel
            // 
            this.m_oClassLabel.AutoSize = true;
            this.m_oClassLabel.Location = new System.Drawing.Point(6, 102);
            this.m_oClassLabel.Name = "m_oClassLabel";
            this.m_oClassLabel.Size = new System.Drawing.Size(32, 13);
            this.m_oClassLabel.TabIndex = 9;
            this.m_oClassLabel.Text = "Class";
            // 
            // m_oTGLabel
            // 
            this.m_oTGLabel.AutoSize = true;
            this.m_oTGLabel.Location = new System.Drawing.Point(6, 68);
            this.m_oTGLabel.MaximumSize = new System.Drawing.Size(38, 30);
            this.m_oTGLabel.MinimumSize = new System.Drawing.Size(38, 30);
            this.m_oTGLabel.Name = "m_oTGLabel";
            this.m_oTGLabel.Size = new System.Drawing.Size(38, 30);
            this.m_oTGLabel.TabIndex = 8;
            this.m_oTGLabel.Text = "Task Group";
            // 
            // m_oSpeciesLabel
            // 
            this.m_oSpeciesLabel.AutoSize = true;
            this.m_oSpeciesLabel.Location = new System.Drawing.Point(6, 49);
            this.m_oSpeciesLabel.Name = "m_oSpeciesLabel";
            this.m_oSpeciesLabel.Size = new System.Drawing.Size(45, 13);
            this.m_oSpeciesLabel.TabIndex = 7;
            this.m_oSpeciesLabel.Text = "Species";
            // 
            // m_oFactionLabel
            // 
            this.m_oFactionLabel.AutoSize = true;
            this.m_oFactionLabel.Location = new System.Drawing.Point(6, 23);
            this.m_oFactionLabel.Name = "m_oFactionLabel";
            this.m_oFactionLabel.Size = new System.Drawing.Size(39, 13);
            this.m_oFactionLabel.TabIndex = 6;
            this.m_oFactionLabel.Text = "Empire";
            // 
            // m_oCostTextBox
            // 
            this.m_oCostTextBox.Location = new System.Drawing.Point(185, 144);
            this.m_oCostTextBox.Name = "m_oCostTextBox";
            this.m_oCostTextBox.ReadOnly = true;
            this.m_oCostTextBox.Size = new System.Drawing.Size(45, 20);
            this.m_oCostTextBox.TabIndex = 5;
            this.m_oCostTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oNumberTextBox
            // 
            this.m_oNumberTextBox.Location = new System.Drawing.Point(56, 144);
            this.m_oNumberTextBox.Name = "m_oNumberTextBox";
            this.m_oNumberTextBox.Size = new System.Drawing.Size(42, 20);
            this.m_oNumberTextBox.TabIndex = 4;
            this.m_oNumberTextBox.Text = "1";
            this.m_oNumberTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oClassComboBox
            // 
            this.m_oClassComboBox.FormattingEnabled = true;
            this.m_oClassComboBox.Location = new System.Drawing.Point(57, 99);
            this.m_oClassComboBox.Name = "m_oClassComboBox";
            this.m_oClassComboBox.Size = new System.Drawing.Size(190, 21);
            this.m_oClassComboBox.TabIndex = 3;
            // 
            // m_oTaskGroupComboBox
            // 
            this.m_oTaskGroupComboBox.FormattingEnabled = true;
            this.m_oTaskGroupComboBox.Location = new System.Drawing.Point(57, 73);
            this.m_oTaskGroupComboBox.Name = "m_oTaskGroupComboBox";
            this.m_oTaskGroupComboBox.Size = new System.Drawing.Size(190, 21);
            this.m_oTaskGroupComboBox.TabIndex = 2;
            // 
            // m_oSpeciesComboBox
            // 
            this.m_oSpeciesComboBox.FormattingEnabled = true;
            this.m_oSpeciesComboBox.Location = new System.Drawing.Point(57, 46);
            this.m_oSpeciesComboBox.Name = "m_oSpeciesComboBox";
            this.m_oSpeciesComboBox.Size = new System.Drawing.Size(190, 21);
            this.m_oSpeciesComboBox.TabIndex = 1;
            // 
            // m_oEmpireComboBox
            // 
            this.m_oEmpireComboBox.FormattingEnabled = true;
            this.m_oEmpireComboBox.Location = new System.Drawing.Point(57, 19);
            this.m_oEmpireComboBox.Name = "m_oEmpireComboBox";
            this.m_oEmpireComboBox.Size = new System.Drawing.Size(190, 21);
            this.m_oEmpireComboBox.TabIndex = 0;
            // 
            // m_oRemBPGroupBox
            // 
            this.m_oRemBPGroupBox.Controls.Add(this.m_oPDCLabel);
            this.m_oRemBPGroupBox.Controls.Add(this.m_oShipLabel);
            this.m_oRemBPGroupBox.Controls.Add(this.m_oPDCBPTextBox);
            this.m_oRemBPGroupBox.Controls.Add(this.m_oShipBPTextBox);
            this.m_oRemBPGroupBox.Location = new System.Drawing.Point(12, 191);
            this.m_oRemBPGroupBox.Name = "m_oRemBPGroupBox";
            this.m_oRemBPGroupBox.Size = new System.Drawing.Size(253, 91);
            this.m_oRemBPGroupBox.TabIndex = 1;
            this.m_oRemBPGroupBox.TabStop = false;
            this.m_oRemBPGroupBox.Text = "Remaining Build Points";
            // 
            // m_oPDCLabel
            // 
            this.m_oPDCLabel.AutoSize = true;
            this.m_oPDCLabel.Location = new System.Drawing.Point(41, 55);
            this.m_oPDCLabel.Name = "m_oPDCLabel";
            this.m_oPDCLabel.Size = new System.Drawing.Size(29, 13);
            this.m_oPDCLabel.TabIndex = 3;
            this.m_oPDCLabel.Text = "PDC";
            // 
            // m_oShipLabel
            // 
            this.m_oShipLabel.AutoSize = true;
            this.m_oShipLabel.Location = new System.Drawing.Point(42, 31);
            this.m_oShipLabel.Name = "m_oShipLabel";
            this.m_oShipLabel.Size = new System.Drawing.Size(28, 13);
            this.m_oShipLabel.TabIndex = 2;
            this.m_oShipLabel.Text = "Ship";
            // 
            // m_oPDCBPTextBox
            // 
            this.m_oPDCBPTextBox.Location = new System.Drawing.Point(76, 52);
            this.m_oPDCBPTextBox.Name = "m_oPDCBPTextBox";
            this.m_oPDCBPTextBox.ReadOnly = true;
            this.m_oPDCBPTextBox.Size = new System.Drawing.Size(119, 20);
            this.m_oPDCBPTextBox.TabIndex = 1;
            this.m_oPDCBPTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oShipBPTextBox
            // 
            this.m_oShipBPTextBox.Location = new System.Drawing.Point(76, 28);
            this.m_oShipBPTextBox.Name = "m_oShipBPTextBox";
            this.m_oShipBPTextBox.ReadOnly = true;
            this.m_oShipBPTextBox.Size = new System.Drawing.Size(119, 20);
            this.m_oShipBPTextBox.TabIndex = 0;
            this.m_oShipBPTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // m_oClassSummaryGroupBox
            // 
            this.m_oClassSummaryGroupBox.Controls.Add(this.m_oSummaryRichTextBox);
            this.m_oClassSummaryGroupBox.Location = new System.Drawing.Point(271, 12);
            this.m_oClassSummaryGroupBox.Name = "m_oClassSummaryGroupBox";
            this.m_oClassSummaryGroupBox.Size = new System.Drawing.Size(605, 270);
            this.m_oClassSummaryGroupBox.TabIndex = 2;
            this.m_oClassSummaryGroupBox.TabStop = false;
            // 
            // m_oSummaryRichTextBox
            // 
            this.m_oSummaryRichTextBox.Location = new System.Drawing.Point(7, 20);
            this.m_oSummaryRichTextBox.Name = "m_oSummaryRichTextBox";
            this.m_oSummaryRichTextBox.Size = new System.Drawing.Size(592, 244);
            this.m_oSummaryRichTextBox.TabIndex = 0;
            this.m_oSummaryRichTextBox.Text = "";
            // 
            // m_oAddButton
            // 
            this.m_oAddButton.Location = new System.Drawing.Point(12, 288);
            this.m_oAddButton.Name = "m_oAddButton";
            this.m_oAddButton.Size = new System.Drawing.Size(70, 24);
            this.m_oAddButton.TabIndex = 3;
            this.m_oAddButton.Text = "&Add";
            this.m_oAddButton.UseVisualStyleBackColor = true;
            // 
            // m_oCloseButton
            // 
            this.m_oCloseButton.Location = new System.Drawing.Point(806, 288);
            this.m_oCloseButton.Name = "m_oCloseButton";
            this.m_oCloseButton.Size = new System.Drawing.Size(70, 24);
            this.m_oCloseButton.TabIndex = 4;
            this.m_oCloseButton.Text = "&Close";
            this.m_oCloseButton.UseVisualStyleBackColor = true;
            // 
            // FastOOB_Panel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(888, 321);
            this.Controls.Add(this.m_oCloseButton);
            this.Controls.Add(this.m_oAddButton);
            this.Controls.Add(this.m_oClassSummaryGroupBox);
            this.Controls.Add(this.m_oRemBPGroupBox);
            this.Controls.Add(this.m_oAddShipGroupBox);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(896, 355);
            this.MinimumSize = new System.Drawing.Size(896, 355);
            this.Name = "FastOOB_Panel";
            this.Text = "Create Racial Order of Battle";
            this.m_oAddShipGroupBox.ResumeLayout(false);
            this.m_oAddShipGroupBox.PerformLayout();
            this.m_oRemBPGroupBox.ResumeLayout(false);
            this.m_oRemBPGroupBox.PerformLayout();
            this.m_oClassSummaryGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox m_oAddShipGroupBox;
        private System.Windows.Forms.GroupBox m_oRemBPGroupBox;
        private System.Windows.Forms.GroupBox m_oClassSummaryGroupBox;
        private System.Windows.Forms.Label m_oCostLabel;
        private System.Windows.Forms.Label m_oNumLabel;
        private System.Windows.Forms.Label m_oClassLabel;
        private System.Windows.Forms.Label m_oTGLabel;
        private System.Windows.Forms.Label m_oSpeciesLabel;
        private System.Windows.Forms.Label m_oFactionLabel;
        private System.Windows.Forms.TextBox m_oCostTextBox;
        private System.Windows.Forms.TextBox m_oNumberTextBox;
        private System.Windows.Forms.ComboBox m_oClassComboBox;
        private System.Windows.Forms.ComboBox m_oTaskGroupComboBox;
        private System.Windows.Forms.ComboBox m_oSpeciesComboBox;
        private System.Windows.Forms.ComboBox m_oEmpireComboBox;
        private System.Windows.Forms.Label m_oPDCLabel;
        private System.Windows.Forms.Label m_oShipLabel;
        private System.Windows.Forms.TextBox m_oPDCBPTextBox;
        private System.Windows.Forms.TextBox m_oShipBPTextBox;
        private System.Windows.Forms.RichTextBox m_oSummaryRichTextBox;
        private System.Windows.Forms.Button m_oAddButton;
        private System.Windows.Forms.Button m_oCloseButton;
    }
}