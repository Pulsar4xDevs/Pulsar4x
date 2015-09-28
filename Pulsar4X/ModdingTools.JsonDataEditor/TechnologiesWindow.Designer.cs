namespace ModdingTools.JsonDataEditor
{
    partial class TechnologiesWindow
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
            this.availibleTechs = new System.Windows.Forms.ListBox();
            this.searchBox = new System.Windows.Forms.TextBox();
            this.techName = new System.Windows.Forms.Label();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.descTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.searchLabel = new System.Windows.Forms.Label();
            this.categoryComboBox = new System.Windows.Forms.ComboBox();
            this.categoryLabel = new System.Windows.Forms.Label();
            this.costLabel = new System.Windows.Forms.Label();
            this.requirementsListBox = new System.Windows.Forms.ListBox();
            this.requirementsLabel = new System.Windows.Forms.Label();
            this.fileDialog = new System.Windows.Forms.OpenFileDialog();
            this.addRequirementButton = new System.Windows.Forms.Button();
            this.removeRequirementButton = new System.Windows.Forms.Button();
            this.newTechButton = new System.Windows.Forms.Button();
            this.selectTechButton = new System.Windows.Forms.Button();
            this.removeTechButton = new System.Windows.Forms.Button();
            this.guidLabel = new System.Windows.Forms.Label();
            this.guidDataLabel = new System.Windows.Forms.Label();
            this.costTextBox = new System.Windows.Forms.TextBox();
            this.selectedFileComboBox = new System.Windows.Forms.ComboBox();
            this.mainMenuButton = new System.Windows.Forms.Button();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.SuspendLayout();
            // 
            // availibleTechs
            // 
            this.availibleTechs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.availibleTechs.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.availibleTechs.FormattingEnabled = true;
            this.availibleTechs.Location = new System.Drawing.Point(356, 108);
            this.availibleTechs.Name = "availibleTechs";
            this.availibleTechs.Size = new System.Drawing.Size(241, 340);
            this.availibleTechs.TabIndex = 2;
            this.availibleTechs.DoubleClick += new System.EventHandler(this.availibleTechs_MouseDoubleClick);
            // 
            // searchBox
            // 
            this.searchBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.searchBox.Location = new System.Drawing.Point(400, 82);
            this.searchBox.Name = "searchBox";
            this.searchBox.Size = new System.Drawing.Size(197, 20);
            this.searchBox.TabIndex = 3;
            this.searchBox.TextChanged += new System.EventHandler(this.searchBox_TextChanged);
            // 
            // techName
            // 
            this.techName.AutoSize = true;
            this.techName.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.techName.Location = new System.Drawing.Point(43, 29);
            this.techName.Name = "techName";
            this.techName.Size = new System.Drawing.Size(35, 13);
            this.techName.TabIndex = 4;
            this.techName.Text = "Name";
            // 
            // nameTextBox
            // 
            this.nameTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.nameTextBox.Location = new System.Drawing.Point(87, 27);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(247, 20);
            this.nameTextBox.TabIndex = 5;
            this.nameTextBox.TextChanged += new System.EventHandler(this.nameTextBox_TextChanged);
            // 
            // descTextBox
            // 
            this.descTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.descTextBox.Location = new System.Drawing.Point(87, 53);
            this.descTextBox.Multiline = true;
            this.descTextBox.Name = "descTextBox";
            this.descTextBox.Size = new System.Drawing.Size(247, 70);
            this.descTextBox.TabIndex = 6;
            this.descTextBox.TextChanged += new System.EventHandler(this.descTextBox_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label1.Location = new System.Drawing.Point(46, 53);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Desc";
            // 
            // searchLabel
            // 
            this.searchLabel.AutoSize = true;
            this.searchLabel.Location = new System.Drawing.Point(353, 85);
            this.searchLabel.Name = "searchLabel";
            this.searchLabel.Size = new System.Drawing.Size(41, 13);
            this.searchLabel.TabIndex = 8;
            this.searchLabel.Text = "Search";
            // 
            // categoryComboBox
            // 
            this.categoryComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.categoryComboBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.categoryComboBox.FormattingEnabled = true;
            this.categoryComboBox.Location = new System.Drawing.Point(87, 181);
            this.categoryComboBox.Name = "categoryComboBox";
            this.categoryComboBox.Size = new System.Drawing.Size(247, 21);
            this.categoryComboBox.TabIndex = 9;
            this.categoryComboBox.SelectedIndexChanged += new System.EventHandler(this.categoryComboBox_SelectedIndexChanged);
            // 
            // categoryLabel
            // 
            this.categoryLabel.AutoSize = true;
            this.categoryLabel.Location = new System.Drawing.Point(29, 184);
            this.categoryLabel.Name = "categoryLabel";
            this.categoryLabel.Size = new System.Drawing.Size(49, 13);
            this.categoryLabel.TabIndex = 10;
            this.categoryLabel.Text = "Category";
            // 
            // costLabel
            // 
            this.costLabel.AutoSize = true;
            this.costLabel.Location = new System.Drawing.Point(10, 211);
            this.costLabel.Name = "costLabel";
            this.costLabel.Size = new System.Drawing.Size(68, 13);
            this.costLabel.TabIndex = 12;
            this.costLabel.Text = "Cost Formula";
            this.costLabel.Click += new System.EventHandler(this.costLabel_Click);
            // 
            // requirementsListBox
            // 
            this.requirementsListBox.FormattingEnabled = true;
            this.requirementsListBox.Location = new System.Drawing.Point(87, 235);
            this.requirementsListBox.Name = "requirementsListBox";
            this.requirementsListBox.Size = new System.Drawing.Size(247, 121);
            this.requirementsListBox.TabIndex = 13;
            this.requirementsListBox.SelectedValueChanged += new System.EventHandler(this.requirementsListBox_SelectedValueChanged);
            // 
            // requirementsLabel
            // 
            this.requirementsLabel.AutoSize = true;
            this.requirementsLabel.Location = new System.Drawing.Point(6, 235);
            this.requirementsLabel.Name = "requirementsLabel";
            this.requirementsLabel.Size = new System.Drawing.Size(72, 13);
            this.requirementsLabel.TabIndex = 14;
            this.requirementsLabel.Text = "Requirements";
            // 
            // fileDialog
            // 
            this.fileDialog.FileName = "openFileDialog1";
            this.fileDialog.Filter = "json files|*.json";
            // 
            // addRequirementButton
            // 
            this.addRequirementButton.Location = new System.Drawing.Point(87, 362);
            this.addRequirementButton.Name = "addRequirementButton";
            this.addRequirementButton.Size = new System.Drawing.Size(108, 23);
            this.addRequirementButton.TabIndex = 15;
            this.addRequirementButton.Text = "Add Requirement";
            this.addRequirementButton.UseVisualStyleBackColor = true;
            this.addRequirementButton.Click += new System.EventHandler(this.addRequirementButton_Click);
            // 
            // removeRequirementButton
            // 
            this.removeRequirementButton.Location = new System.Drawing.Point(201, 362);
            this.removeRequirementButton.Name = "removeRequirementButton";
            this.removeRequirementButton.Size = new System.Drawing.Size(133, 23);
            this.removeRequirementButton.TabIndex = 16;
            this.removeRequirementButton.Text = "Remove Requirement";
            this.removeRequirementButton.UseVisualStyleBackColor = true;
            this.removeRequirementButton.Click += new System.EventHandler(this.removeRequirementButton_Click);
            // 
            // newTechButton
            // 
            this.newTechButton.Location = new System.Drawing.Point(356, 42);
            this.newTechButton.Name = "newTechButton";
            this.newTechButton.Size = new System.Drawing.Size(71, 23);
            this.newTechButton.TabIndex = 17;
            this.newTechButton.Text = "New Tech";
            this.newTechButton.UseVisualStyleBackColor = true;
            this.newTechButton.Click += new System.EventHandler(this.newTechButton_Click);
            // 
            // selectTechButton
            // 
            this.selectTechButton.Location = new System.Drawing.Point(524, 42);
            this.selectTechButton.Name = "selectTechButton";
            this.selectTechButton.Size = new System.Drawing.Size(73, 23);
            this.selectTechButton.TabIndex = 18;
            this.selectTechButton.Text = "Select Tech";
            this.selectTechButton.UseVisualStyleBackColor = true;
            this.selectTechButton.Click += new System.EventHandler(this.selectTechButton_Click);
            // 
            // removeTechButton
            // 
            this.removeTechButton.Location = new System.Drawing.Point(433, 42);
            this.removeTechButton.Name = "removeTechButton";
            this.removeTechButton.Size = new System.Drawing.Size(85, 23);
            this.removeTechButton.TabIndex = 19;
            this.removeTechButton.Text = "Remove Tech";
            this.removeTechButton.UseVisualStyleBackColor = true;
            this.removeTechButton.Click += new System.EventHandler(this.removeTechButton_Click);
            // 
            // guidLabel
            // 
            this.guidLabel.AutoSize = true;
            this.guidLabel.Location = new System.Drawing.Point(49, 7);
            this.guidLabel.Name = "guidLabel";
            this.guidLabel.Size = new System.Drawing.Size(29, 13);
            this.guidLabel.TabIndex = 20;
            this.guidLabel.Text = "Guid";
            // 
            // guidDataLabel
            // 
            this.guidDataLabel.AutoSize = true;
            this.guidDataLabel.Location = new System.Drawing.Point(84, 7);
            this.guidDataLabel.Name = "guidDataLabel";
            this.guidDataLabel.Size = new System.Drawing.Size(31, 13);
            this.guidDataLabel.TabIndex = 21;
            this.guidDataLabel.Text = "0000";
            // 
            // costTextBox
            // 
            this.costTextBox.Location = new System.Drawing.Point(87, 208);
            this.costTextBox.Name = "costTextBox";
            this.costTextBox.Size = new System.Drawing.Size(247, 20);
            this.costTextBox.TabIndex = 22;
            this.costTextBox.TextChanged += new System.EventHandler(this.costTextBox_TextChanged);
            // 
            // selectedFileComboBox
            // 
            this.selectedFileComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.selectedFileComboBox.FormattingEnabled = true;
            this.selectedFileComboBox.Location = new System.Drawing.Point(356, 4);
            this.selectedFileComboBox.Name = "selectedFileComboBox";
            this.selectedFileComboBox.Size = new System.Drawing.Size(241, 21);
            this.selectedFileComboBox.TabIndex = 23;
            this.selectedFileComboBox.SelectedIndexChanged += new System.EventHandler(this.selectedFileComboBox_SelectedIndexChanged);
            // 
            // mainMenuButton
            // 
            this.mainMenuButton.Location = new System.Drawing.Point(3, 434);
            this.mainMenuButton.Name = "mainMenuButton";
            this.mainMenuButton.Size = new System.Drawing.Size(75, 23);
            this.mainMenuButton.TabIndex = 24;
            this.mainMenuButton.Text = "Main Menu";
            this.mainMenuButton.UseVisualStyleBackColor = true;
            this.mainMenuButton.Click += new System.EventHandler(this.mainMenuButton_Click);
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Location = new System.Drawing.Point(87, 129);
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(120, 20);
            this.numericUpDown1.TabIndex = 25;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(87, 155);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(247, 20);
            this.textBox1.TabIndex = 26;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(22, 131);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 13);
            this.label2.TabIndex = 27;
            this.label2.Text = "Max Level";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 158);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(70, 13);
            this.label3.TabIndex = 28;
            this.label3.Text = "Data Formula";
            // 
            // TechnologiesWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.numericUpDown1);
            this.Controls.Add(this.mainMenuButton);
            this.Controls.Add(this.selectedFileComboBox);
            this.Controls.Add(this.costTextBox);
            this.Controls.Add(this.guidDataLabel);
            this.Controls.Add(this.guidLabel);
            this.Controls.Add(this.removeTechButton);
            this.Controls.Add(this.selectTechButton);
            this.Controls.Add(this.newTechButton);
            this.Controls.Add(this.removeRequirementButton);
            this.Controls.Add(this.addRequirementButton);
            this.Controls.Add(this.requirementsLabel);
            this.Controls.Add(this.requirementsListBox);
            this.Controls.Add(this.costLabel);
            this.Controls.Add(this.categoryLabel);
            this.Controls.Add(this.categoryComboBox);
            this.Controls.Add(this.searchLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.descTextBox);
            this.Controls.Add(this.nameTextBox);
            this.Controls.Add(this.techName);
            this.Controls.Add(this.searchBox);
            this.Controls.Add(this.availibleTechs);
            this.Name = "TechnologiesWindow";
            this.Size = new System.Drawing.Size(600, 460);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox availibleTechs;
        private System.Windows.Forms.TextBox searchBox;
        private System.Windows.Forms.Label techName;
        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.TextBox descTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label searchLabel;
        private System.Windows.Forms.ComboBox categoryComboBox;
        private System.Windows.Forms.Label categoryLabel;
        private System.Windows.Forms.Label costLabel;
        private System.Windows.Forms.ListBox requirementsListBox;
        private System.Windows.Forms.Label requirementsLabel;
        private System.Windows.Forms.OpenFileDialog fileDialog;
        private System.Windows.Forms.Button addRequirementButton;
        private System.Windows.Forms.Button removeRequirementButton;
        private System.Windows.Forms.Button newTechButton;
        private System.Windows.Forms.Button selectTechButton;
        private System.Windows.Forms.Button removeTechButton;
        private System.Windows.Forms.Label guidLabel;
        private System.Windows.Forms.Label guidDataLabel;
        private System.Windows.Forms.TextBox costTextBox;
        private System.Windows.Forms.ComboBox selectedFileComboBox;
        private System.Windows.Forms.Button mainMenuButton;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;

    }
}
