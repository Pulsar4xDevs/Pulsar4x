namespace Pulsar4X.UI.Panels
{
    partial class Ships_ShipList
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.m_oFactionGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oFactionComboBox = new System.Windows.Forms.ComboBox();
            this.m_oFilterSortOptionsGroupBox = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.m_oFilterMilitaryCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oSortHullRadioButton = new System.Windows.Forms.RadioButton();
            this.m_oSortAlphaRadioButton = new System.Windows.Forms.RadioButton();
            this.m_oSortSizeRadioButton = new System.Windows.Forms.RadioButton();
            this.m_oFilterCivilianCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oFilterNoFightersCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oShipsGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oShipsListBox = new System.Windows.Forms.ListBox();
            this.m_oFactionGroupBox.SuspendLayout();
            this.m_oFilterSortOptionsGroupBox.SuspendLayout();
            this.m_oShipsGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_oFactionGroupBox
            // 
            this.m_oFactionGroupBox.Controls.Add(this.m_oFactionComboBox);
            this.m_oFactionGroupBox.Location = new System.Drawing.Point(12, 12);
            this.m_oFactionGroupBox.Name = "m_oFactionGroupBox";
            this.m_oFactionGroupBox.Size = new System.Drawing.Size(184, 57);
            this.m_oFactionGroupBox.TabIndex = 0;
            this.m_oFactionGroupBox.TabStop = false;
            this.m_oFactionGroupBox.Text = "Empire";
            // 
            // m_oFactionComboBox
            // 
            this.m_oFactionComboBox.FormattingEnabled = true;
            this.m_oFactionComboBox.Location = new System.Drawing.Point(7, 20);
            this.m_oFactionComboBox.Name = "m_oFactionComboBox";
            this.m_oFactionComboBox.Size = new System.Drawing.Size(171, 21);
            this.m_oFactionComboBox.TabIndex = 0;
            // 
            // m_oFilterSortOptionsGroupBox
            // 
            this.m_oFilterSortOptionsGroupBox.Controls.Add(this.label2);
            this.m_oFilterSortOptionsGroupBox.Controls.Add(this.label1);
            this.m_oFilterSortOptionsGroupBox.Controls.Add(this.m_oFilterMilitaryCheckBox);
            this.m_oFilterSortOptionsGroupBox.Controls.Add(this.m_oSortHullRadioButton);
            this.m_oFilterSortOptionsGroupBox.Controls.Add(this.m_oSortAlphaRadioButton);
            this.m_oFilterSortOptionsGroupBox.Controls.Add(this.m_oSortSizeRadioButton);
            this.m_oFilterSortOptionsGroupBox.Controls.Add(this.m_oFilterCivilianCheckBox);
            this.m_oFilterSortOptionsGroupBox.Controls.Add(this.m_oFilterNoFightersCheckBox);
            this.m_oFilterSortOptionsGroupBox.Location = new System.Drawing.Point(12, 75);
            this.m_oFilterSortOptionsGroupBox.Name = "m_oFilterSortOptionsGroupBox";
            this.m_oFilterSortOptionsGroupBox.Size = new System.Drawing.Size(184, 97);
            this.m_oFilterSortOptionsGroupBox.TabIndex = 1;
            this.m_oFilterSortOptionsGroupBox.TabStop = false;
            this.m_oFilterSortOptionsGroupBox.Text = "Filter/Sort Options";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(88, 20);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(71, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Filter Options:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Sort By:";
            // 
            // m_oFilterMilitaryCheckBox
            // 
            this.m_oFilterMilitaryCheckBox.AutoSize = true;
            this.m_oFilterMilitaryCheckBox.Location = new System.Drawing.Point(91, 56);
            this.m_oFilterMilitaryCheckBox.Name = "m_oFilterMilitaryCheckBox";
            this.m_oFilterMilitaryCheckBox.Size = new System.Drawing.Size(82, 17);
            this.m_oFilterMilitaryCheckBox.TabIndex = 5;
            this.m_oFilterMilitaryCheckBox.Text = "Military Only";
            this.m_oFilterMilitaryCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oSortHullRadioButton
            // 
            this.m_oSortHullRadioButton.AutoSize = true;
            this.m_oSortHullRadioButton.Location = new System.Drawing.Point(7, 76);
            this.m_oSortHullRadioButton.Name = "m_oSortHullRadioButton";
            this.m_oSortHullRadioButton.Size = new System.Drawing.Size(43, 17);
            this.m_oSortHullRadioButton.TabIndex = 3;
            this.m_oSortHullRadioButton.TabStop = true;
            this.m_oSortHullRadioButton.Text = "Hull";
            this.m_oSortHullRadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oSortAlphaRadioButton
            // 
            this.m_oSortAlphaRadioButton.AutoSize = true;
            this.m_oSortAlphaRadioButton.Location = new System.Drawing.Point(7, 56);
            this.m_oSortAlphaRadioButton.Name = "m_oSortAlphaRadioButton";
            this.m_oSortAlphaRadioButton.Size = new System.Drawing.Size(52, 17);
            this.m_oSortAlphaRadioButton.TabIndex = 2;
            this.m_oSortAlphaRadioButton.TabStop = true;
            this.m_oSortAlphaRadioButton.Text = "Alpha";
            this.m_oSortAlphaRadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oSortSizeRadioButton
            // 
            this.m_oSortSizeRadioButton.AutoSize = true;
            this.m_oSortSizeRadioButton.Location = new System.Drawing.Point(7, 36);
            this.m_oSortSizeRadioButton.Name = "m_oSortSizeRadioButton";
            this.m_oSortSizeRadioButton.Size = new System.Drawing.Size(45, 17);
            this.m_oSortSizeRadioButton.TabIndex = 1;
            this.m_oSortSizeRadioButton.TabStop = true;
            this.m_oSortSizeRadioButton.Text = "Size";
            this.m_oSortSizeRadioButton.UseVisualStyleBackColor = true;
            // 
            // m_oFilterCivilianCheckBox
            // 
            this.m_oFilterCivilianCheckBox.AutoSize = true;
            this.m_oFilterCivilianCheckBox.Location = new System.Drawing.Point(91, 76);
            this.m_oFilterCivilianCheckBox.Name = "m_oFilterCivilianCheckBox";
            this.m_oFilterCivilianCheckBox.Size = new System.Drawing.Size(83, 17);
            this.m_oFilterCivilianCheckBox.TabIndex = 6;
            this.m_oFilterCivilianCheckBox.Text = "Civilian Only";
            this.m_oFilterCivilianCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oFilterNoFightersCheckBox
            // 
            this.m_oFilterNoFightersCheckBox.AutoSize = true;
            this.m_oFilterNoFightersCheckBox.Location = new System.Drawing.Point(91, 37);
            this.m_oFilterNoFightersCheckBox.Name = "m_oFilterNoFightersCheckBox";
            this.m_oFilterNoFightersCheckBox.Size = new System.Drawing.Size(80, 17);
            this.m_oFilterNoFightersCheckBox.TabIndex = 4;
            this.m_oFilterNoFightersCheckBox.Text = "No Fighters";
            this.m_oFilterNoFightersCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oShipsGroupBox
            // 
            this.m_oShipsGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.m_oShipsGroupBox.Controls.Add(this.m_oShipsListBox);
            this.m_oShipsGroupBox.Location = new System.Drawing.Point(12, 178);
            this.m_oShipsGroupBox.MinimumSize = new System.Drawing.Size(184, 377);
            this.m_oShipsGroupBox.Name = "m_oShipsGroupBox";
            this.m_oShipsGroupBox.Size = new System.Drawing.Size(184, 377);
            this.m_oShipsGroupBox.TabIndex = 2;
            this.m_oShipsGroupBox.TabStop = false;
            this.m_oShipsGroupBox.Text = "Ships";
            // 
            // m_oShipsListBox
            // 
            this.m_oShipsListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_oShipsListBox.FormattingEnabled = true;
            this.m_oShipsListBox.Location = new System.Drawing.Point(3, 16);
            this.m_oShipsListBox.Name = "m_oShipsListBox";
            this.m_oShipsListBox.Size = new System.Drawing.Size(178, 358);
            this.m_oShipsListBox.TabIndex = 0;
            // 
            // Ships_ShipList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(208, 567);
            this.Controls.Add(this.m_oShipsGroupBox);
            this.Controls.Add(this.m_oFilterSortOptionsGroupBox);
            this.Controls.Add(this.m_oFactionGroupBox);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "Ships_ShipList";
            this.m_oFactionGroupBox.ResumeLayout(false);
            this.m_oFilterSortOptionsGroupBox.ResumeLayout(false);
            this.m_oFilterSortOptionsGroupBox.PerformLayout();
            this.m_oShipsGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox m_oFactionGroupBox;
        private System.Windows.Forms.GroupBox m_oFilterSortOptionsGroupBox;
        private System.Windows.Forms.GroupBox m_oShipsGroupBox;
        private System.Windows.Forms.ComboBox m_oFactionComboBox;
        private System.Windows.Forms.CheckBox m_oFilterMilitaryCheckBox;
        private System.Windows.Forms.RadioButton m_oSortHullRadioButton;
        private System.Windows.Forms.RadioButton m_oSortAlphaRadioButton;
        private System.Windows.Forms.RadioButton m_oSortSizeRadioButton;
        private System.Windows.Forms.CheckBox m_oFilterCivilianCheckBox;
        private System.Windows.Forms.CheckBox m_oFilterNoFightersCheckBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox m_oShipsListBox;
    }
}
