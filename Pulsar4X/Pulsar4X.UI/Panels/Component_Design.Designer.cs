using System.Windows.Forms;

namespace Pulsar4X.UI.Panels
{
    partial class Component_Design
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// List of factions in game
        /// </summary>
        public ComboBox FactionComboBox
        {
            get { return m_oFactionComboBox; }
        }

        /// <summary>
        /// List of research projects.
        /// </summary>
        public ComboBox ResearchComboBox
        {
            get { return m_oResearchComboBox; }
        }

        /// <summary>
        /// text boxes for tech one through seven
        /// </summary>
        public ComboBox TechComboBoxOne   { get { return m_oTechComboBox1; } }
        public ComboBox TechComboBoxTwo   { get { return m_oTechComboBox2; } }
        public ComboBox TechComboBoxThree { get { return m_oTechComboBox3; } }
        public ComboBox TechComboBoxFour  { get { return m_oTechComboBox4; } }
        public ComboBox TechComboBoxFive  { get { return m_oTechComboBox5; } }
        public ComboBox TechComboBoxSix   { get { return m_oTechComboBox6; } }
        public ComboBox TechComboBoxSeven { get { return m_oTechComboBox7; } }

        /// <summary>
        /// Text labels for tech one through seven
        /// </summary>
        public Label TechLabelOne   { get { return m_oTechLabel1; } }
        public Label TechLabelTwo   { get { return m_oTechLabel2; } }
        public Label TechLabelThree { get { return m_oTechLabel3; } }
        public Label TechLabelFour  { get { return m_oTechLabel4; } }
        public Label TechLabelFive  { get { return m_oTechLabel5; } }
        public Label TechLabelSix   { get { return m_oTechLabel6; } }
        public Label TechLabelSeven { get { return m_oTechLabel7; } }

        /// <summary>
        /// Text box for notes pertaining to each tech.
        /// </summary>
        public Label NotesLabel
        {
            get { return m_oNotesLabel; }
        }

        /// <summary>
        /// Tech name display text box.
        /// </summary>
        public RichTextBox TechNameTextBox
        {
            get { return m_oTechNameTextBox; }
        }

        /// <summary>
        /// Display parameters text box for each tech.
        /// </summary>
        public RichTextBox ParametersTextBox
        {
            get { return m_oTechParaTextBox; }
        }

        /// <summary>
        /// Should sizes be displayed in tons check box.
        /// </summary>
        public CheckBox SizeTonsCheckBox
        {
            get { return m_oSizeTonsCheckBox; }
        }

        /// <summary>
        /// Create tech project button
        /// </summary>
        public Button CreateButton
        {
            get { return m_oCreateRPButton; }
        }

        /// <summary>
        /// instantly create component via SM mode button.
        /// </summary>
        public Button InstantButton
        {
            get { return m_oSMInstantButton; }
        }

        /// <summary>
        /// Close project window button
        /// </summary>
        public Button CloseButton
        {
            get { return m_oCloseButton; }
        }

        /// <summary>
        /// Turret design button
        /// </summary>
        public Button TurretButton
        {
            get { return m_oTurretDesButton; }
        }

        /// <summary>
        /// Missile design button
        /// </summary>
        public Button MissileButton
        {
            get { return m_oMissileDesButton; }
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
            this.m_oFactionComboBox = new System.Windows.Forms.ComboBox();
            this.m_oResProjectGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oResearchComboBox = new System.Windows.Forms.ComboBox();
            this.m_oBackgroundTechGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oTechLabel7 = new System.Windows.Forms.Label();
            this.m_oTechLabel6 = new System.Windows.Forms.Label();
            this.m_oTechLabel5 = new System.Windows.Forms.Label();
            this.m_oTechLabel4 = new System.Windows.Forms.Label();
            this.m_oTechLabel3 = new System.Windows.Forms.Label();
            this.m_oTechLabel2 = new System.Windows.Forms.Label();
            this.m_oTechLabel1 = new System.Windows.Forms.Label();
            this.m_oTechComboBox7 = new System.Windows.Forms.ComboBox();
            this.m_oTechComboBox6 = new System.Windows.Forms.ComboBox();
            this.m_oTechComboBox5 = new System.Windows.Forms.ComboBox();
            this.m_oTechComboBox4 = new System.Windows.Forms.ComboBox();
            this.m_oTechComboBox3 = new System.Windows.Forms.ComboBox();
            this.m_oTechComboBox2 = new System.Windows.Forms.ComboBox();
            this.m_oTechComboBox1 = new System.Windows.Forms.ComboBox();
            this.m_oSysParaGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oCreateRPButton = new System.Windows.Forms.Button();
            this.m_oSMInstantButton = new System.Windows.Forms.Button();
            this.m_oTechParaTextBox = new System.Windows.Forms.RichTextBox();
            this.m_oTechNameTextBox = new System.Windows.Forms.RichTextBox();
            this.m_oCloseButton = new System.Windows.Forms.Button();
            this.m_oMissileDesButton = new System.Windows.Forms.Button();
            this.m_oTurretDesButton = new System.Windows.Forms.Button();
            this.m_oSizeTonsCheckBox = new System.Windows.Forms.CheckBox();
            this.m_oNotesGroupBox = new System.Windows.Forms.GroupBox();
            this.m_oNotesLabel = new System.Windows.Forms.Label();
            this.m_oEmpireGroupBox.SuspendLayout();
            this.m_oResProjectGroupBox.SuspendLayout();
            this.m_oBackgroundTechGroupBox.SuspendLayout();
            this.m_oSysParaGroupBox.SuspendLayout();
            this.m_oNotesGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_oEmpireGroupBox
            // 
            this.m_oEmpireGroupBox.Controls.Add(this.m_oFactionComboBox);
            this.m_oEmpireGroupBox.Location = new System.Drawing.Point(12, 12);
            this.m_oEmpireGroupBox.Name = "m_oEmpireGroupBox";
            this.m_oEmpireGroupBox.Size = new System.Drawing.Size(278, 48);
            this.m_oEmpireGroupBox.TabIndex = 0;
            this.m_oEmpireGroupBox.TabStop = false;
            this.m_oEmpireGroupBox.Text = "Empire";
            // 
            // m_oFactionComboBox
            // 
            this.m_oFactionComboBox.Location = new System.Drawing.Point(6, 17);
            this.m_oFactionComboBox.Name = "m_oFactionComboBox";
            this.m_oFactionComboBox.Size = new System.Drawing.Size(266, 21);
            this.m_oFactionComboBox.TabIndex = 1;
            // 
            // m_oResProjectGroupBox
            // 
            this.m_oResProjectGroupBox.Controls.Add(this.m_oResearchComboBox);
            this.m_oResProjectGroupBox.Location = new System.Drawing.Point(302, 12);
            this.m_oResProjectGroupBox.Name = "m_oResProjectGroupBox";
            this.m_oResProjectGroupBox.Size = new System.Drawing.Size(278, 48);
            this.m_oResProjectGroupBox.TabIndex = 1;
            this.m_oResProjectGroupBox.TabStop = false;
            this.m_oResProjectGroupBox.Text = "Research Project Type";
            // 
            // m_oResearchComboBox
            // 
            this.m_oResearchComboBox.Location = new System.Drawing.Point(6, 17);
            this.m_oResearchComboBox.Name = "m_oResearchComboBox";
            this.m_oResearchComboBox.Size = new System.Drawing.Size(266, 21);
            this.m_oResearchComboBox.TabIndex = 0;
            // 
            // m_oBackgroundTechGroupBox
            // 
            this.m_oBackgroundTechGroupBox.Controls.Add(this.m_oTechLabel7);
            this.m_oBackgroundTechGroupBox.Controls.Add(this.m_oTechLabel6);
            this.m_oBackgroundTechGroupBox.Controls.Add(this.m_oTechLabel5);
            this.m_oBackgroundTechGroupBox.Controls.Add(this.m_oTechLabel4);
            this.m_oBackgroundTechGroupBox.Controls.Add(this.m_oTechLabel3);
            this.m_oBackgroundTechGroupBox.Controls.Add(this.m_oTechLabel2);
            this.m_oBackgroundTechGroupBox.Controls.Add(this.m_oTechLabel1);
            this.m_oBackgroundTechGroupBox.Controls.Add(this.m_oTechComboBox7);
            this.m_oBackgroundTechGroupBox.Controls.Add(this.m_oTechComboBox6);
            this.m_oBackgroundTechGroupBox.Controls.Add(this.m_oTechComboBox5);
            this.m_oBackgroundTechGroupBox.Controls.Add(this.m_oTechComboBox4);
            this.m_oBackgroundTechGroupBox.Controls.Add(this.m_oTechComboBox3);
            this.m_oBackgroundTechGroupBox.Controls.Add(this.m_oTechComboBox2);
            this.m_oBackgroundTechGroupBox.Controls.Add(this.m_oTechComboBox1);
            this.m_oBackgroundTechGroupBox.Location = new System.Drawing.Point(12, 67);
            this.m_oBackgroundTechGroupBox.Margin = new System.Windows.Forms.Padding(12);
            this.m_oBackgroundTechGroupBox.Name = "m_oBackgroundTechGroupBox";
            this.m_oBackgroundTechGroupBox.Size = new System.Drawing.Size(568, 269);
            this.m_oBackgroundTechGroupBox.TabIndex = 1;
            this.m_oBackgroundTechGroupBox.TabStop = false;
            this.m_oBackgroundTechGroupBox.Text = "Background Technology";
            // 
            // m_oTechLabel7
            // 
            this.m_oTechLabel7.AutoSize = true;
            this.m_oTechLabel7.Location = new System.Drawing.Point(6, 178);
            this.m_oTechLabel7.MaximumSize = new System.Drawing.Size(185, 13);
            this.m_oTechLabel7.MinimumSize = new System.Drawing.Size(185, 13);
            this.m_oTechLabel7.Name = "m_oTechLabel7";
            this.m_oTechLabel7.Size = new System.Drawing.Size(185, 13);
            this.m_oTechLabel7.TabIndex = 15;
            this.m_oTechLabel7.Text = "Tech Seven Label Box";
            // 
            // m_oTechLabel6
            // 
            this.m_oTechLabel6.AutoSize = true;
            this.m_oTechLabel6.Location = new System.Drawing.Point(6, 152);
            this.m_oTechLabel6.MaximumSize = new System.Drawing.Size(185, 13);
            this.m_oTechLabel6.MinimumSize = new System.Drawing.Size(185, 13);
            this.m_oTechLabel6.Name = "m_oTechLabel6";
            this.m_oTechLabel6.Size = new System.Drawing.Size(185, 13);
            this.m_oTechLabel6.TabIndex = 14;
            this.m_oTechLabel6.Text = "Tech Six Label Box";
            // 
            // m_oTechLabel5
            // 
            this.m_oTechLabel5.AutoSize = true;
            this.m_oTechLabel5.Location = new System.Drawing.Point(6, 126);
            this.m_oTechLabel5.MaximumSize = new System.Drawing.Size(185, 13);
            this.m_oTechLabel5.MinimumSize = new System.Drawing.Size(185, 13);
            this.m_oTechLabel5.Name = "m_oTechLabel5";
            this.m_oTechLabel5.Size = new System.Drawing.Size(185, 13);
            this.m_oTechLabel5.TabIndex = 13;
            this.m_oTechLabel5.Text = "Tech Five Label Box";
            // 
            // m_oTechLabel4
            // 
            this.m_oTechLabel4.AutoSize = true;
            this.m_oTechLabel4.Location = new System.Drawing.Point(6, 100);
            this.m_oTechLabel4.MaximumSize = new System.Drawing.Size(185, 13);
            this.m_oTechLabel4.MinimumSize = new System.Drawing.Size(185, 13);
            this.m_oTechLabel4.Name = "m_oTechLabel4";
            this.m_oTechLabel4.Size = new System.Drawing.Size(185, 13);
            this.m_oTechLabel4.TabIndex = 12;
            this.m_oTechLabel4.Text = "Tech Four Label Box";
            // 
            // m_oTechLabel3
            // 
            this.m_oTechLabel3.AutoSize = true;
            this.m_oTechLabel3.Location = new System.Drawing.Point(6, 74);
            this.m_oTechLabel3.MaximumSize = new System.Drawing.Size(185, 13);
            this.m_oTechLabel3.MinimumSize = new System.Drawing.Size(185, 13);
            this.m_oTechLabel3.Name = "m_oTechLabel3";
            this.m_oTechLabel3.Size = new System.Drawing.Size(185, 13);
            this.m_oTechLabel3.TabIndex = 11;
            this.m_oTechLabel3.Text = "Tech Three Label Box";
            // 
            // m_oTechLabel2
            // 
            this.m_oTechLabel2.AutoSize = true;
            this.m_oTechLabel2.Location = new System.Drawing.Point(6, 48);
            this.m_oTechLabel2.MaximumSize = new System.Drawing.Size(185, 13);
            this.m_oTechLabel2.MinimumSize = new System.Drawing.Size(185, 13);
            this.m_oTechLabel2.Name = "m_oTechLabel2";
            this.m_oTechLabel2.Size = new System.Drawing.Size(185, 13);
            this.m_oTechLabel2.TabIndex = 10;
            this.m_oTechLabel2.Text = "Tech Two Label Box";
            // 
            // m_oTechLabel1
            // 
            this.m_oTechLabel1.AutoSize = true;
            this.m_oTechLabel1.Location = new System.Drawing.Point(6, 22);
            this.m_oTechLabel1.MaximumSize = new System.Drawing.Size(185, 13);
            this.m_oTechLabel1.MinimumSize = new System.Drawing.Size(185, 13);
            this.m_oTechLabel1.Name = "m_oTechLabel1";
            this.m_oTechLabel1.Size = new System.Drawing.Size(185, 13);
            this.m_oTechLabel1.TabIndex = 9;
            this.m_oTechLabel1.Text = "Tech One Label Box";
            // 
            // m_oTechComboBox7
            // 
            this.m_oTechComboBox7.Location = new System.Drawing.Point(245, 175);
            this.m_oTechComboBox7.Name = "m_oTechComboBox7";
            this.m_oTechComboBox7.Size = new System.Drawing.Size(317, 21);
            this.m_oTechComboBox7.TabIndex = 8;
            // 
            // m_oTechComboBox6
            // 
            this.m_oTechComboBox6.Location = new System.Drawing.Point(245, 149);
            this.m_oTechComboBox6.Name = "m_oTechComboBox6";
            this.m_oTechComboBox6.Size = new System.Drawing.Size(317, 21);
            this.m_oTechComboBox6.TabIndex = 7;
            // 
            // m_oTechComboBox5
            // 
            this.m_oTechComboBox5.Location = new System.Drawing.Point(245, 123);
            this.m_oTechComboBox5.Name = "m_oTechComboBox5";
            this.m_oTechComboBox5.Size = new System.Drawing.Size(317, 21);
            this.m_oTechComboBox5.TabIndex = 6;
            // 
            // m_oTechComboBox4
            // 
            this.m_oTechComboBox4.Location = new System.Drawing.Point(245, 97);
            this.m_oTechComboBox4.Name = "m_oTechComboBox4";
            this.m_oTechComboBox4.Size = new System.Drawing.Size(317, 21);
            this.m_oTechComboBox4.TabIndex = 5;
            // 
            // m_oTechComboBox3
            // 
            this.m_oTechComboBox3.Location = new System.Drawing.Point(245, 71);
            this.m_oTechComboBox3.Name = "m_oTechComboBox3";
            this.m_oTechComboBox3.Size = new System.Drawing.Size(317, 21);
            this.m_oTechComboBox3.TabIndex = 4;
            // 
            // m_oTechComboBox2
            // 
            this.m_oTechComboBox2.Location = new System.Drawing.Point(245, 45);
            this.m_oTechComboBox2.Name = "m_oTechComboBox2";
            this.m_oTechComboBox2.Size = new System.Drawing.Size(317, 21);
            this.m_oTechComboBox2.TabIndex = 3;
            // 
            // m_oTechComboBox1
            // 
            this.m_oTechComboBox1.Location = new System.Drawing.Point(245, 19);
            this.m_oTechComboBox1.Name = "m_oTechComboBox1";
            this.m_oTechComboBox1.Size = new System.Drawing.Size(317, 21);
            this.m_oTechComboBox1.TabIndex = 2;
            // 
            // m_oSysParaGroupBox
            // 
            this.m_oSysParaGroupBox.Controls.Add(this.m_oCreateRPButton);
            this.m_oSysParaGroupBox.Controls.Add(this.m_oSMInstantButton);
            this.m_oSysParaGroupBox.Controls.Add(this.m_oTechParaTextBox);
            this.m_oSysParaGroupBox.Controls.Add(this.m_oTechNameTextBox);
            this.m_oSysParaGroupBox.Location = new System.Drawing.Point(12, 471);
            this.m_oSysParaGroupBox.Name = "m_oSysParaGroupBox";
            this.m_oSysParaGroupBox.Size = new System.Drawing.Size(568, 248);
            this.m_oSysParaGroupBox.TabIndex = 1;
            this.m_oSysParaGroupBox.TabStop = false;
            this.m_oSysParaGroupBox.Text = "Proposed System Parameters";
            // 
            // m_oCreateRPButton
            // 
            this.m_oCreateRPButton.Location = new System.Drawing.Point(487, 20);
            this.m_oCreateRPButton.Name = "m_oCreateRPButton";
            this.m_oCreateRPButton.Size = new System.Drawing.Size(75, 29);
            this.m_oCreateRPButton.TabIndex = 3;
            this.m_oCreateRPButton.Text = "Create";
            this.m_oCreateRPButton.UseVisualStyleBackColor = true;
            // 
            // m_oSMInstantButton
            // 
            this.m_oSMInstantButton.Enabled = false;
            this.m_oSMInstantButton.Location = new System.Drawing.Point(408, 20);
            this.m_oSMInstantButton.Name = "m_oSMInstantButton";
            this.m_oSMInstantButton.Size = new System.Drawing.Size(75, 29);
            this.m_oSMInstantButton.TabIndex = 2;
            this.m_oSMInstantButton.Text = "Instant";
            this.m_oSMInstantButton.UseVisualStyleBackColor = true;
            this.m_oSMInstantButton.Visible = false;
            // 
            // m_oTechParaTextBox
            // 
            this.m_oTechParaTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oTechParaTextBox.Location = new System.Drawing.Point(9, 55);
            this.m_oTechParaTextBox.Name = "m_oTechParaTextBox";
            this.m_oTechParaTextBox.Size = new System.Drawing.Size(553, 187);
            this.m_oTechParaTextBox.TabIndex = 1;
            this.m_oTechParaTextBox.Text = "";
            // 
            // m_oTechNameTextBox
            // 
            this.m_oTechNameTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oTechNameTextBox.Location = new System.Drawing.Point(6, 19);
            this.m_oTechNameTextBox.Name = "m_oTechNameTextBox";
            this.m_oTechNameTextBox.Size = new System.Drawing.Size(395, 29);
            this.m_oTechNameTextBox.TabIndex = 0;
            this.m_oTechNameTextBox.Text = "";
            // 
            // m_oCloseButton
            // 
            this.m_oCloseButton.Location = new System.Drawing.Point(495, 725);
            this.m_oCloseButton.Name = "m_oCloseButton";
            this.m_oCloseButton.Size = new System.Drawing.Size(85, 29);
            this.m_oCloseButton.TabIndex = 4;
            this.m_oCloseButton.Text = "Close";
            this.m_oCloseButton.UseVisualStyleBackColor = true;
            // 
            // m_oMissileDesButton
            // 
            this.m_oMissileDesButton.Location = new System.Drawing.Point(404, 725);
            this.m_oMissileDesButton.Name = "m_oMissileDesButton";
            this.m_oMissileDesButton.Size = new System.Drawing.Size(85, 29);
            this.m_oMissileDesButton.TabIndex = 5;
            this.m_oMissileDesButton.Text = "Missile Design";
            this.m_oMissileDesButton.UseVisualStyleBackColor = true;
            // 
            // m_oTurretDesButton
            // 
            this.m_oTurretDesButton.Location = new System.Drawing.Point(313, 725);
            this.m_oTurretDesButton.Name = "m_oTurretDesButton";
            this.m_oTurretDesButton.Size = new System.Drawing.Size(85, 29);
            this.m_oTurretDesButton.TabIndex = 6;
            this.m_oTurretDesButton.Text = "Turret Design";
            this.m_oTurretDesButton.UseVisualStyleBackColor = true;
            // 
            // m_oSizeTonsCheckBox
            // 
            this.m_oSizeTonsCheckBox.AutoSize = true;
            this.m_oSizeTonsCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.m_oSizeTonsCheckBox.Location = new System.Drawing.Point(21, 737);
            this.m_oSizeTonsCheckBox.Name = "m_oSizeTonsCheckBox";
            this.m_oSizeTonsCheckBox.Size = new System.Drawing.Size(124, 17);
            this.m_oSizeTonsCheckBox.TabIndex = 7;
            this.m_oSizeTonsCheckBox.Text = "Shows Sizes in Tons";
            this.m_oSizeTonsCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_oNotesGroupBox
            // 
            this.m_oNotesGroupBox.Controls.Add(this.m_oNotesLabel);
            this.m_oNotesGroupBox.Location = new System.Drawing.Point(12, 351);
            this.m_oNotesGroupBox.Name = "m_oNotesGroupBox";
            this.m_oNotesGroupBox.Size = new System.Drawing.Size(562, 114);
            this.m_oNotesGroupBox.TabIndex = 8;
            this.m_oNotesGroupBox.TabStop = false;
            // 
            // m_oNotesLabel
            // 
            this.m_oNotesLabel.AutoSize = true;
            this.m_oNotesLabel.Location = new System.Drawing.Point(7, 20);
            this.m_oNotesLabel.MaximumSize = new System.Drawing.Size(538, 90);
            this.m_oNotesLabel.MinimumSize = new System.Drawing.Size(538, 90);
            this.m_oNotesLabel.Name = "m_oNotesLabel";
            this.m_oNotesLabel.Size = new System.Drawing.Size(538, 90);
            this.m_oNotesLabel.TabIndex = 0;
            // 
            // Component_Design
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(592, 766);
            this.Controls.Add(this.m_oNotesGroupBox);
            this.Controls.Add(this.m_oSizeTonsCheckBox);
            this.Controls.Add(this.m_oTurretDesButton);
            this.Controls.Add(this.m_oMissileDesButton);
            this.Controls.Add(this.m_oCloseButton);
            this.Controls.Add(this.m_oSysParaGroupBox);
            this.Controls.Add(this.m_oBackgroundTechGroupBox);
            this.Controls.Add(this.m_oResProjectGroupBox);
            this.Controls.Add(this.m_oEmpireGroupBox);
            this.MaximumSize = new System.Drawing.Size(600, 800);
            this.MinimumSize = new System.Drawing.Size(600, 800);
            this.Name = "Component_Design";
            this.Text = "Create Research Project";
            this.m_oEmpireGroupBox.ResumeLayout(false);
            this.m_oResProjectGroupBox.ResumeLayout(false);
            this.m_oBackgroundTechGroupBox.ResumeLayout(false);
            this.m_oBackgroundTechGroupBox.PerformLayout();
            this.m_oSysParaGroupBox.ResumeLayout(false);
            this.m_oNotesGroupBox.ResumeLayout(false);
            this.m_oNotesGroupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox m_oEmpireGroupBox;
        private System.Windows.Forms.GroupBox m_oResProjectGroupBox;
        private System.Windows.Forms.GroupBox m_oBackgroundTechGroupBox;
        private System.Windows.Forms.GroupBox m_oSysParaGroupBox;
        private System.Windows.Forms.ComboBox m_oFactionComboBox;
        private System.Windows.Forms.ComboBox m_oResearchComboBox;
        private System.Windows.Forms.Label m_oTechLabel7;
        private System.Windows.Forms.Label m_oTechLabel6;
        private System.Windows.Forms.Label m_oTechLabel5;
        private System.Windows.Forms.Label m_oTechLabel4;
        private System.Windows.Forms.Label m_oTechLabel3;
        private System.Windows.Forms.Label m_oTechLabel2;
        private System.Windows.Forms.Label m_oTechLabel1;
        private System.Windows.Forms.ComboBox m_oTechComboBox7;
        private System.Windows.Forms.ComboBox m_oTechComboBox6;
        private System.Windows.Forms.ComboBox m_oTechComboBox5;
        private System.Windows.Forms.ComboBox m_oTechComboBox4;
        private System.Windows.Forms.ComboBox m_oTechComboBox3;
        private System.Windows.Forms.ComboBox m_oTechComboBox2;
        private System.Windows.Forms.ComboBox m_oTechComboBox1;
        private System.Windows.Forms.RichTextBox m_oTechParaTextBox;
        private System.Windows.Forms.RichTextBox m_oTechNameTextBox;
        private System.Windows.Forms.Button m_oCreateRPButton;
        private System.Windows.Forms.Button m_oSMInstantButton;
        private System.Windows.Forms.Button m_oCloseButton;
        private System.Windows.Forms.Button m_oMissileDesButton;
        private System.Windows.Forms.Button m_oTurretDesButton;
        private System.Windows.Forms.CheckBox m_oSizeTonsCheckBox;
        private GroupBox m_oNotesGroupBox;
        private Label m_oNotesLabel;
    }
}