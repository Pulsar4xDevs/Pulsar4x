namespace ModdingTools.JsonDataEditor
{
    partial class TechesWindow
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
            this.loadButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.availibleTechs = new System.Windows.Forms.ListBox();
            this.searchBox = new System.Windows.Forms.TextBox();
            this.techName = new System.Windows.Forms.Label();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.descTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.searchLabel = new System.Windows.Forms.Label();
            this.categoryComboBox = new System.Windows.Forms.ComboBox();
            this.categoryLabel = new System.Windows.Forms.Label();
            this.costUpDown = new System.Windows.Forms.NumericUpDown();
            this.costLabel = new System.Windows.Forms.Label();
            this.requirementsListBox = new System.Windows.Forms.ListBox();
            this.requirementsLabel = new System.Windows.Forms.Label();
            this.fileDialog = new System.Windows.Forms.OpenFileDialog();
            this.addRequirementButton = new System.Windows.Forms.Button();
            this.removeRequirementButton = new System.Windows.Forms.Button();
            this.newTechButton = new System.Windows.Forms.Button();
            this.selectTechButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.costUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // loadButton
            // 
            this.loadButton.Location = new System.Drawing.Point(5, 434);
            this.loadButton.Name = "loadButton";
            this.loadButton.Size = new System.Drawing.Size(75, 23);
            this.loadButton.TabIndex = 0;
            this.loadButton.Text = "Load";
            this.loadButton.UseVisualStyleBackColor = true;
            this.loadButton.Click += new System.EventHandler(this.loadButton_Click);
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(86, 434);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(75, 23);
            this.saveButton.TabIndex = 1;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // availibleTechs
            // 
            this.availibleTechs.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.availibleTechs.FormattingEnabled = true;
            this.availibleTechs.Location = new System.Drawing.Point(356, 30);
            this.availibleTechs.Name = "availibleTechs";
            this.availibleTechs.Size = new System.Drawing.Size(241, 418);
            this.availibleTechs.TabIndex = 2;
            // 
            // searchBox
            // 
            this.searchBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.searchBox.Location = new System.Drawing.Point(400, 4);
            this.searchBox.Name = "searchBox";
            this.searchBox.Size = new System.Drawing.Size(197, 20);
            this.searchBox.TabIndex = 3;
            this.searchBox.TextChanged += new System.EventHandler(this.searchBox_TextChanged);
            // 
            // techName
            // 
            this.techName.AutoSize = true;
            this.techName.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.techName.Location = new System.Drawing.Point(34, 35);
            this.techName.Name = "techName";
            this.techName.Size = new System.Drawing.Size(35, 13);
            this.techName.TabIndex = 4;
            this.techName.Text = "Name";
            // 
            // nameTextBox
            // 
            this.nameTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.nameTextBox.Location = new System.Drawing.Point(75, 32);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(247, 20);
            this.nameTextBox.TabIndex = 5;
            this.nameTextBox.TextChanged += new System.EventHandler(this.nameTextBox_TextChanged);
            // 
            // descTextBox
            // 
            this.descTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.descTextBox.Location = new System.Drawing.Point(75, 58);
            this.descTextBox.Multiline = true;
            this.descTextBox.Name = "descTextBox";
            this.descTextBox.Size = new System.Drawing.Size(247, 121);
            this.descTextBox.TabIndex = 6;
            this.descTextBox.TextChanged += new System.EventHandler(this.descTextBox_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label1.Location = new System.Drawing.Point(34, 58);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Desc";
            // 
            // searchLabel
            // 
            this.searchLabel.AutoSize = true;
            this.searchLabel.Location = new System.Drawing.Point(353, 7);
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
            this.categoryComboBox.Location = new System.Drawing.Point(75, 186);
            this.categoryComboBox.Name = "categoryComboBox";
            this.categoryComboBox.Size = new System.Drawing.Size(247, 21);
            this.categoryComboBox.TabIndex = 9;
            this.categoryComboBox.SelectedIndexChanged += new System.EventHandler(this.categoryComboBox_SelectedIndexChanged);
            // 
            // categoryLabel
            // 
            this.categoryLabel.AutoSize = true;
            this.categoryLabel.Location = new System.Drawing.Point(20, 189);
            this.categoryLabel.Name = "categoryLabel";
            this.categoryLabel.Size = new System.Drawing.Size(49, 13);
            this.categoryLabel.TabIndex = 10;
            this.categoryLabel.Text = "Category";
            // 
            // costUpDown
            // 
            this.costUpDown.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.costUpDown.Location = new System.Drawing.Point(75, 214);
            this.costUpDown.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.costUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.costUpDown.Name = "costUpDown";
            this.costUpDown.Size = new System.Drawing.Size(247, 20);
            this.costUpDown.TabIndex = 11;
            this.costUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.costUpDown.ValueChanged += new System.EventHandler(this.costUpDown_ValueChanged);
            // 
            // costLabel
            // 
            this.costLabel.AutoSize = true;
            this.costLabel.Location = new System.Drawing.Point(41, 216);
            this.costLabel.Name = "costLabel";
            this.costLabel.Size = new System.Drawing.Size(28, 13);
            this.costLabel.TabIndex = 12;
            this.costLabel.Text = "Cost";
            // 
            // requirementsListBox
            // 
            this.requirementsListBox.FormattingEnabled = true;
            this.requirementsListBox.Location = new System.Drawing.Point(75, 240);
            this.requirementsListBox.Name = "requirementsListBox";
            this.requirementsListBox.Size = new System.Drawing.Size(247, 121);
            this.requirementsListBox.TabIndex = 13;
            this.requirementsListBox.SelectedValueChanged += new System.EventHandler(this.requirementsListBox_SelectedValueChanged);
            // 
            // requirementsLabel
            // 
            this.requirementsLabel.AutoSize = true;
            this.requirementsLabel.Location = new System.Drawing.Point(2, 240);
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
            this.addRequirementButton.Location = new System.Drawing.Point(75, 367);
            this.addRequirementButton.Name = "addRequirementButton";
            this.addRequirementButton.Size = new System.Drawing.Size(75, 23);
            this.addRequirementButton.TabIndex = 15;
            this.addRequirementButton.Text = "Add";
            this.addRequirementButton.UseVisualStyleBackColor = true;
            this.addRequirementButton.Click += new System.EventHandler(this.addRequirementButton_Click);
            // 
            // removeRequirementButton
            // 
            this.removeRequirementButton.Location = new System.Drawing.Point(247, 367);
            this.removeRequirementButton.Name = "removeRequirementButton";
            this.removeRequirementButton.Size = new System.Drawing.Size(75, 23);
            this.removeRequirementButton.TabIndex = 16;
            this.removeRequirementButton.Text = "Remove";
            this.removeRequirementButton.UseVisualStyleBackColor = true;
            this.removeRequirementButton.Click += new System.EventHandler(this.removeRequirementButton_Click);
            // 
            // newTechButton
            // 
            this.newTechButton.Location = new System.Drawing.Point(75, 3);
            this.newTechButton.Name = "newTechButton";
            this.newTechButton.Size = new System.Drawing.Size(75, 23);
            this.newTechButton.TabIndex = 17;
            this.newTechButton.Text = "New";
            this.newTechButton.UseVisualStyleBackColor = true;
            this.newTechButton.Click += new System.EventHandler(this.newTechButton_Click);
            // 
            // selectTechButton
            // 
            this.selectTechButton.Location = new System.Drawing.Point(247, 2);
            this.selectTechButton.Name = "selectTechButton";
            this.selectTechButton.Size = new System.Drawing.Size(75, 23);
            this.selectTechButton.TabIndex = 18;
            this.selectTechButton.Text = "Select";
            this.selectTechButton.UseVisualStyleBackColor = true;
            this.selectTechButton.Click += new System.EventHandler(this.selectTechButton_Click);
            // 
            // TechesWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.selectTechButton);
            this.Controls.Add(this.newTechButton);
            this.Controls.Add(this.removeRequirementButton);
            this.Controls.Add(this.addRequirementButton);
            this.Controls.Add(this.requirementsLabel);
            this.Controls.Add(this.requirementsListBox);
            this.Controls.Add(this.costLabel);
            this.Controls.Add(this.costUpDown);
            this.Controls.Add(this.categoryLabel);
            this.Controls.Add(this.categoryComboBox);
            this.Controls.Add(this.searchLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.descTextBox);
            this.Controls.Add(this.nameTextBox);
            this.Controls.Add(this.techName);
            this.Controls.Add(this.searchBox);
            this.Controls.Add(this.availibleTechs);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.loadButton);
            this.Name = "TechesWindow";
            this.Size = new System.Drawing.Size(600, 460);
            ((System.ComponentModel.ISupportInitialize)(this.costUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button loadButton;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.ListBox availibleTechs;
        private System.Windows.Forms.TextBox searchBox;
        private System.Windows.Forms.Label techName;
        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.TextBox descTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label searchLabel;
        private System.Windows.Forms.ComboBox categoryComboBox;
        private System.Windows.Forms.Label categoryLabel;
        private System.Windows.Forms.NumericUpDown costUpDown;
        private System.Windows.Forms.Label costLabel;
        private System.Windows.Forms.ListBox requirementsListBox;
        private System.Windows.Forms.Label requirementsLabel;
        private System.Windows.Forms.OpenFileDialog fileDialog;
        private System.Windows.Forms.Button addRequirementButton;
        private System.Windows.Forms.Button removeRequirementButton;
        private System.Windows.Forms.Button newTechButton;
        private System.Windows.Forms.Button selectTechButton;

    }
}
