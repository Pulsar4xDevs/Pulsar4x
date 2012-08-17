namespace Pulsar4X.WinForms.Controls
{
    partial class SystemGenAndDisplay
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
            this.NameListBox = new System.Windows.Forms.ListBox();
            this.labelName = new System.Windows.Forms.Label();
            this.GeneralInfoGroupBox = new System.Windows.Forms.GroupBox();
            this.AgetextBox = new System.Windows.Forms.TextBox();
            this.TypetextBox = new System.Windows.Forms.TextBox();
            this.labelID = new System.Windows.Forms.Label();
            this.labelDiscovered = new System.Windows.Forms.Label();
            this.labelControllingEmpire = new System.Windows.Forms.Label();
            this.labelAge = new System.Windows.Forms.Label();
            this.labelType = new System.Windows.Forms.Label();
            this.DiscoveredTextBox = new System.Windows.Forms.TextBox();
            this.IDTextBox = new System.Windows.Forms.TextBox();
            this.ControllingEmpireListBox = new System.Windows.Forms.ListBox();
            this.groupBoxEnvironTolerances = new System.Windows.Forms.GroupBox();
            this.labelSpecies = new System.Windows.Forms.Label();
            this.labelBreaths = new System.Windows.Forms.Label();
            this.SpeciesLlistBox = new System.Windows.Forms.ListBox();
            this.labelMin = new System.Windows.Forms.Label();
            this.Max = new System.Windows.Forms.Label();
            this.GravityMinTextBox = new System.Windows.Forms.TextBox();
            this.GravityMaxTextBox = new System.Windows.Forms.TextBox();
            this.OxygenMinTextBox = new System.Windows.Forms.TextBox();
            this.OxygenMaxTextBox = new System.Windows.Forms.TextBox();
            this.TempMinTextBox = new System.Windows.Forms.TextBox();
            this.TempMaxTextBox = new System.Windows.Forms.TextBox();
            this.PressureTextBox = new System.Windows.Forms.TextBox();
            this.labelGravity = new System.Windows.Forms.Label();
            this.Oxygen = new System.Windows.Forms.Label();
            this.labelTemperature = new System.Windows.Forms.Label();
            this.labelPressure = new System.Windows.Forms.Label();
            this.BreathsTextBox = new System.Windows.Forms.TextBox();
            this.GeneralInfoGroupBox.SuspendLayout();
            this.groupBoxEnvironTolerances.SuspendLayout();
            this.SuspendLayout();
            // 
            // NameListBox
            // 
            this.NameListBox.FormattingEnabled = true;
            this.NameListBox.Location = new System.Drawing.Point(47, 16);
            this.NameListBox.Name = "NameListBox";
            this.NameListBox.Size = new System.Drawing.Size(120, 17);
            this.NameListBox.TabIndex = 0;
            // 
            // labelName
            // 
            this.labelName.AutoSize = true;
            this.labelName.Location = new System.Drawing.Point(6, 19);
            this.labelName.Name = "labelName";
            this.labelName.Size = new System.Drawing.Size(35, 13);
            this.labelName.TabIndex = 1;
            this.labelName.Text = "Name";
            // 
            // GeneralInfoGroupBox
            // 
            this.GeneralInfoGroupBox.Controls.Add(this.ControllingEmpireListBox);
            this.GeneralInfoGroupBox.Controls.Add(this.IDTextBox);
            this.GeneralInfoGroupBox.Controls.Add(this.DiscoveredTextBox);
            this.GeneralInfoGroupBox.Controls.Add(this.AgetextBox);
            this.GeneralInfoGroupBox.Controls.Add(this.TypetextBox);
            this.GeneralInfoGroupBox.Controls.Add(this.labelID);
            this.GeneralInfoGroupBox.Controls.Add(this.labelDiscovered);
            this.GeneralInfoGroupBox.Controls.Add(this.labelControllingEmpire);
            this.GeneralInfoGroupBox.Controls.Add(this.labelAge);
            this.GeneralInfoGroupBox.Controls.Add(this.labelType);
            this.GeneralInfoGroupBox.Controls.Add(this.NameListBox);
            this.GeneralInfoGroupBox.Controls.Add(this.labelName);
            this.GeneralInfoGroupBox.Location = new System.Drawing.Point(3, 3);
            this.GeneralInfoGroupBox.Name = "GeneralInfoGroupBox";
            this.GeneralInfoGroupBox.Size = new System.Drawing.Size(1015, 41);
            this.GeneralInfoGroupBox.TabIndex = 2;
            this.GeneralInfoGroupBox.TabStop = false;
            this.GeneralInfoGroupBox.Text = "General System Info";
            // 
            // AgetextBox
            // 
            this.AgetextBox.Enabled = false;
            this.AgetextBox.Location = new System.Drawing.Point(368, 16);
            this.AgetextBox.Name = "AgetextBox";
            this.AgetextBox.Size = new System.Drawing.Size(62, 20);
            this.AgetextBox.TabIndex = 8;
            this.AgetextBox.Text = "10 GY";
            this.AgetextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // TypetextBox
            // 
            this.TypetextBox.Enabled = false;
            this.TypetextBox.Location = new System.Drawing.Point(210, 16);
            this.TypetextBox.Name = "TypetextBox";
            this.TypetextBox.Size = new System.Drawing.Size(120, 20);
            this.TypetextBox.TabIndex = 7;
            this.TypetextBox.Text = "Single Star";
            this.TypetextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // labelID
            // 
            this.labelID.AutoSize = true;
            this.labelID.Location = new System.Drawing.Point(939, 18);
            this.labelID.Name = "labelID";
            this.labelID.Size = new System.Drawing.Size(18, 13);
            this.labelID.TabIndex = 6;
            this.labelID.Text = "ID";
            // 
            // labelDiscovered
            // 
            this.labelDiscovered.AutoSize = true;
            this.labelDiscovered.Location = new System.Drawing.Point(710, 18);
            this.labelDiscovered.Name = "labelDiscovered";
            this.labelDiscovered.Size = new System.Drawing.Size(61, 13);
            this.labelDiscovered.TabIndex = 5;
            this.labelDiscovered.Text = "Discovered";
            // 
            // labelControllingEmpire
            // 
            this.labelControllingEmpire.AutoSize = true;
            this.labelControllingEmpire.Location = new System.Drawing.Point(436, 20);
            this.labelControllingEmpire.Name = "labelControllingEmpire";
            this.labelControllingEmpire.Size = new System.Drawing.Size(91, 13);
            this.labelControllingEmpire.TabIndex = 4;
            this.labelControllingEmpire.Text = "Controlling Empire";
            // 
            // labelAge
            // 
            this.labelAge.AutoSize = true;
            this.labelAge.Location = new System.Drawing.Point(336, 20);
            this.labelAge.Name = "labelAge";
            this.labelAge.Size = new System.Drawing.Size(26, 13);
            this.labelAge.TabIndex = 3;
            this.labelAge.Text = "Age";
            // 
            // labelType
            // 
            this.labelType.AutoSize = true;
            this.labelType.Location = new System.Drawing.Point(173, 20);
            this.labelType.Name = "labelType";
            this.labelType.Size = new System.Drawing.Size(31, 13);
            this.labelType.TabIndex = 2;
            this.labelType.Text = "Type";
            // 
            // DiscoveredTextBox
            // 
            this.DiscoveredTextBox.Enabled = false;
            this.DiscoveredTextBox.Location = new System.Drawing.Point(777, 15);
            this.DiscoveredTextBox.Name = "DiscoveredTextBox";
            this.DiscoveredTextBox.Size = new System.Drawing.Size(156, 20);
            this.DiscoveredTextBox.TabIndex = 9;
            this.DiscoveredTextBox.Text = "1st Jan 2025";
            this.DiscoveredTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // IDTextBox
            // 
            this.IDTextBox.Enabled = false;
            this.IDTextBox.Location = new System.Drawing.Point(963, 15);
            this.IDTextBox.Name = "IDTextBox";
            this.IDTextBox.Size = new System.Drawing.Size(46, 20);
            this.IDTextBox.TabIndex = 10;
            this.IDTextBox.Text = "0";
            this.IDTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // ControllingEmpireListBox
            // 
            this.ControllingEmpireListBox.FormattingEnabled = true;
            this.ControllingEmpireListBox.Location = new System.Drawing.Point(533, 18);
            this.ControllingEmpireListBox.Name = "ControllingEmpireListBox";
            this.ControllingEmpireListBox.Size = new System.Drawing.Size(171, 17);
            this.ControllingEmpireListBox.TabIndex = 11;
            // 
            // groupBoxEnvironTolerances
            // 
            this.groupBoxEnvironTolerances.Controls.Add(this.BreathsTextBox);
            this.groupBoxEnvironTolerances.Controls.Add(this.labelPressure);
            this.groupBoxEnvironTolerances.Controls.Add(this.labelTemperature);
            this.groupBoxEnvironTolerances.Controls.Add(this.Oxygen);
            this.groupBoxEnvironTolerances.Controls.Add(this.labelGravity);
            this.groupBoxEnvironTolerances.Controls.Add(this.PressureTextBox);
            this.groupBoxEnvironTolerances.Controls.Add(this.TempMaxTextBox);
            this.groupBoxEnvironTolerances.Controls.Add(this.TempMinTextBox);
            this.groupBoxEnvironTolerances.Controls.Add(this.OxygenMaxTextBox);
            this.groupBoxEnvironTolerances.Controls.Add(this.OxygenMinTextBox);
            this.groupBoxEnvironTolerances.Controls.Add(this.GravityMaxTextBox);
            this.groupBoxEnvironTolerances.Controls.Add(this.GravityMinTextBox);
            this.groupBoxEnvironTolerances.Controls.Add(this.Max);
            this.groupBoxEnvironTolerances.Controls.Add(this.labelMin);
            this.groupBoxEnvironTolerances.Controls.Add(this.SpeciesLlistBox);
            this.groupBoxEnvironTolerances.Controls.Add(this.labelBreaths);
            this.groupBoxEnvironTolerances.Controls.Add(this.labelSpecies);
            this.groupBoxEnvironTolerances.Location = new System.Drawing.Point(1024, 3);
            this.groupBoxEnvironTolerances.Name = "groupBoxEnvironTolerances";
            this.groupBoxEnvironTolerances.Size = new System.Drawing.Size(196, 181);
            this.groupBoxEnvironTolerances.TabIndex = 3;
            this.groupBoxEnvironTolerances.TabStop = false;
            this.groupBoxEnvironTolerances.Text = "Environmental Tolerances";
            // 
            // labelSpecies
            // 
            this.labelSpecies.AutoSize = true;
            this.labelSpecies.Location = new System.Drawing.Point(6, 18);
            this.labelSpecies.Name = "labelSpecies";
            this.labelSpecies.Size = new System.Drawing.Size(45, 13);
            this.labelSpecies.TabIndex = 0;
            this.labelSpecies.Text = "Species";
            // 
            // labelBreaths
            // 
            this.labelBreaths.AutoSize = true;
            this.labelBreaths.Location = new System.Drawing.Point(6, 38);
            this.labelBreaths.Name = "labelBreaths";
            this.labelBreaths.Size = new System.Drawing.Size(43, 13);
            this.labelBreaths.TabIndex = 1;
            this.labelBreaths.Text = "Breaths";
            // 
            // SpeciesLlistBox
            // 
            this.SpeciesLlistBox.FormattingEnabled = true;
            this.SpeciesLlistBox.Location = new System.Drawing.Point(58, 16);
            this.SpeciesLlistBox.Name = "SpeciesLlistBox";
            this.SpeciesLlistBox.Size = new System.Drawing.Size(132, 17);
            this.SpeciesLlistBox.TabIndex = 2;
            // 
            // labelMin
            // 
            this.labelMin.AutoSize = true;
            this.labelMin.Location = new System.Drawing.Point(108, 59);
            this.labelMin.Name = "labelMin";
            this.labelMin.Size = new System.Drawing.Size(24, 13);
            this.labelMin.TabIndex = 4;
            this.labelMin.Text = "Min";
            // 
            // Max
            // 
            this.Max.AutoSize = true;
            this.Max.Location = new System.Drawing.Point(155, 60);
            this.Max.Name = "Max";
            this.Max.Size = new System.Drawing.Size(27, 13);
            this.Max.TabIndex = 5;
            this.Max.Text = "Max";
            // 
            // GravityMinTextBox
            // 
            this.GravityMinTextBox.Enabled = false;
            this.GravityMinTextBox.Location = new System.Drawing.Point(98, 76);
            this.GravityMinTextBox.Name = "GravityMinTextBox";
            this.GravityMinTextBox.Size = new System.Drawing.Size(45, 20);
            this.GravityMinTextBox.TabIndex = 6;
            this.GravityMinTextBox.Text = "0.30";
            this.GravityMinTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // GravityMaxTextBox
            // 
            this.GravityMaxTextBox.Enabled = false;
            this.GravityMaxTextBox.Location = new System.Drawing.Point(149, 76);
            this.GravityMaxTextBox.Name = "GravityMaxTextBox";
            this.GravityMaxTextBox.Size = new System.Drawing.Size(41, 20);
            this.GravityMaxTextBox.TabIndex = 7;
            this.GravityMaxTextBox.Text = "1.70";
            this.GravityMaxTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // OxygenMinTextBox
            // 
            this.OxygenMinTextBox.Enabled = false;
            this.OxygenMinTextBox.Location = new System.Drawing.Point(98, 102);
            this.OxygenMinTextBox.Name = "OxygenMinTextBox";
            this.OxygenMinTextBox.Size = new System.Drawing.Size(45, 20);
            this.OxygenMinTextBox.TabIndex = 8;
            this.OxygenMinTextBox.Text = "0.100";
            this.OxygenMinTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // OxygenMaxTextBox
            // 
            this.OxygenMaxTextBox.Enabled = false;
            this.OxygenMaxTextBox.Location = new System.Drawing.Point(149, 102);
            this.OxygenMaxTextBox.Name = "OxygenMaxTextBox";
            this.OxygenMaxTextBox.Size = new System.Drawing.Size(41, 20);
            this.OxygenMaxTextBox.TabIndex = 9;
            this.OxygenMaxTextBox.Text = "0.300";
            this.OxygenMaxTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // TempMinTextBox
            // 
            this.TempMinTextBox.Enabled = false;
            this.TempMinTextBox.Location = new System.Drawing.Point(98, 128);
            this.TempMinTextBox.Name = "TempMinTextBox";
            this.TempMinTextBox.Size = new System.Drawing.Size(45, 20);
            this.TempMinTextBox.TabIndex = 10;
            this.TempMinTextBox.Text = "0.0";
            this.TempMinTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // TempMaxTextBox
            // 
            this.TempMaxTextBox.Enabled = false;
            this.TempMaxTextBox.Location = new System.Drawing.Point(149, 128);
            this.TempMaxTextBox.Name = "TempMaxTextBox";
            this.TempMaxTextBox.Size = new System.Drawing.Size(41, 20);
            this.TempMaxTextBox.TabIndex = 11;
            this.TempMaxTextBox.Text = "44.0";
            this.TempMaxTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // PressureTextBox
            // 
            this.PressureTextBox.Enabled = false;
            this.PressureTextBox.Location = new System.Drawing.Point(149, 154);
            this.PressureTextBox.Name = "PressureTextBox";
            this.PressureTextBox.Size = new System.Drawing.Size(41, 20);
            this.PressureTextBox.TabIndex = 12;
            this.PressureTextBox.Text = "4.00";
            this.PressureTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // labelGravity
            // 
            this.labelGravity.AutoSize = true;
            this.labelGravity.Location = new System.Drawing.Point(10, 83);
            this.labelGravity.Name = "labelGravity";
            this.labelGravity.Size = new System.Drawing.Size(57, 13);
            this.labelGravity.TabIndex = 13;
            this.labelGravity.Text = "Gravity (G)";
            // 
            // Oxygen
            // 
            this.Oxygen.AutoSize = true;
            this.Oxygen.Location = new System.Drawing.Point(10, 105);
            this.Oxygen.Name = "Oxygen";
            this.Oxygen.Size = new System.Drawing.Size(69, 13);
            this.Oxygen.TabIndex = 14;
            this.Oxygen.Text = "Oxygen (atm)";
            // 
            // labelTemperature
            // 
            this.labelTemperature.AutoSize = true;
            this.labelTemperature.Location = new System.Drawing.Point(10, 135);
            this.labelTemperature.Name = "labelTemperature";
            this.labelTemperature.Size = new System.Drawing.Size(83, 13);
            this.labelTemperature.TabIndex = 16;
            this.labelTemperature.Text = "Temperature (C)";
            // 
            // labelPressure
            // 
            this.labelPressure.AutoSize = true;
            this.labelPressure.Location = new System.Drawing.Point(10, 161);
            this.labelPressure.Name = "labelPressure";
            this.labelPressure.Size = new System.Drawing.Size(74, 13);
            this.labelPressure.TabIndex = 17;
            this.labelPressure.Text = "Pressure (atm)";
            // 
            // BreathsTextBox
            // 
            this.BreathsTextBox.Enabled = false;
            this.BreathsTextBox.Location = new System.Drawing.Point(58, 35);
            this.BreathsTextBox.Name = "BreathsTextBox";
            this.BreathsTextBox.Size = new System.Drawing.Size(132, 20);
            this.BreathsTextBox.TabIndex = 18;
            this.BreathsTextBox.Text = "Oxygen";
            this.BreathsTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // SystemGenAndDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBoxEnvironTolerances);
            this.Controls.Add(this.GeneralInfoGroupBox);
            this.Name = "SystemGenAndDisplay";
            this.Size = new System.Drawing.Size(1223, 565);
            this.GeneralInfoGroupBox.ResumeLayout(false);
            this.GeneralInfoGroupBox.PerformLayout();
            this.groupBoxEnvironTolerances.ResumeLayout(false);
            this.groupBoxEnvironTolerances.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox NameListBox;
        private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.GroupBox GeneralInfoGroupBox;
        private System.Windows.Forms.Label labelID;
        private System.Windows.Forms.Label labelDiscovered;
        private System.Windows.Forms.Label labelControllingEmpire;
        private System.Windows.Forms.Label labelAge;
        private System.Windows.Forms.Label labelType;
        private System.Windows.Forms.TextBox AgetextBox;
        private System.Windows.Forms.TextBox TypetextBox;
        private System.Windows.Forms.ListBox ControllingEmpireListBox;
        private System.Windows.Forms.TextBox IDTextBox;
        private System.Windows.Forms.TextBox DiscoveredTextBox;
        private System.Windows.Forms.GroupBox groupBoxEnvironTolerances;
        private System.Windows.Forms.ListBox SpeciesLlistBox;
        private System.Windows.Forms.Label labelBreaths;
        private System.Windows.Forms.Label labelSpecies;
        private System.Windows.Forms.Label labelPressure;
        private System.Windows.Forms.Label labelTemperature;
        private System.Windows.Forms.Label Oxygen;
        private System.Windows.Forms.Label labelGravity;
        private System.Windows.Forms.TextBox PressureTextBox;
        private System.Windows.Forms.TextBox TempMaxTextBox;
        private System.Windows.Forms.TextBox TempMinTextBox;
        private System.Windows.Forms.TextBox OxygenMaxTextBox;
        private System.Windows.Forms.TextBox OxygenMinTextBox;
        private System.Windows.Forms.TextBox GravityMaxTextBox;
        private System.Windows.Forms.TextBox GravityMinTextBox;
        private System.Windows.Forms.Label Max;
        private System.Windows.Forms.Label labelMin;
        private System.Windows.Forms.TextBox BreathsTextBox;
    }
}
