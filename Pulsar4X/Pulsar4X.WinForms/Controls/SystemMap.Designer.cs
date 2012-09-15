namespace Pulsar4X.WinForms.Controls
{
    partial class SystemMap
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
            this.button1 = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.FPSLabel = new System.Windows.Forms.Label();
            this.ControlsPanel = new System.Windows.Forms.Panel();
            this.AUScaleLabel = new System.Windows.Forms.Label();
            this.KmScaleLabel = new System.Windows.Forms.Label();
            this.SystemSelectComboBox = new System.Windows.Forms.ComboBox();
            this.PanUpButton = new System.Windows.Forms.Button();
            this.PanLeftButton = new System.Windows.Forms.Button();
            this.PanDownButton = new System.Windows.Forms.Button();
            this.PanRightButton = new System.Windows.Forms.Button();
            this.ZoomInButton = new System.Windows.Forms.Button();
            this.ZoomOutButton = new System.Windows.Forms.Button();
            this.ResetViewButton = new System.Windows.Forms.Button();
            this.ControlsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(0, 655);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(81, 655);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(100, 20);
            this.textBox1.TabIndex = 1;
            // 
            // FPSLabel
            // 
            this.FPSLabel.AutoSize = true;
            this.FPSLabel.Location = new System.Drawing.Point(187, 655);
            this.FPSLabel.Name = "FPSLabel";
            this.FPSLabel.Size = new System.Drawing.Size(10, 13);
            this.FPSLabel.TabIndex = 2;
            this.FPSLabel.Text = " ";
            // 
            // ControlsPanel
            // 
            this.ControlsPanel.BackColor = System.Drawing.Color.MidnightBlue;
            this.ControlsPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ControlsPanel.Controls.Add(this.ResetViewButton);
            this.ControlsPanel.Controls.Add(this.ZoomOutButton);
            this.ControlsPanel.Controls.Add(this.ZoomInButton);
            this.ControlsPanel.Controls.Add(this.PanRightButton);
            this.ControlsPanel.Controls.Add(this.PanDownButton);
            this.ControlsPanel.Controls.Add(this.PanLeftButton);
            this.ControlsPanel.Controls.Add(this.PanUpButton);
            this.ControlsPanel.Controls.Add(this.AUScaleLabel);
            this.ControlsPanel.Controls.Add(this.KmScaleLabel);
            this.ControlsPanel.Controls.Add(this.SystemSelectComboBox);
            this.ControlsPanel.Location = new System.Drawing.Point(4, 4);
            this.ControlsPanel.Name = "ControlsPanel";
            this.ControlsPanel.Size = new System.Drawing.Size(200, 645);
            this.ControlsPanel.TabIndex = 3;
            // 
            // AUScaleLabel
            // 
            this.AUScaleLabel.AutoSize = true;
            this.AUScaleLabel.ForeColor = System.Drawing.Color.White;
            this.AUScaleLabel.Location = new System.Drawing.Point(3, 115);
            this.AUScaleLabel.Name = "AUScaleLabel";
            this.AUScaleLabel.Size = new System.Drawing.Size(52, 13);
            this.AUScaleLabel.TabIndex = 2;
            this.AUScaleLabel.Text = "Scale AU";
            // 
            // KmScaleLabel
            // 
            this.KmScaleLabel.AutoSize = true;
            this.KmScaleLabel.ForeColor = System.Drawing.Color.White;
            this.KmScaleLabel.Location = new System.Drawing.Point(3, 93);
            this.KmScaleLabel.Name = "KmScaleLabel";
            this.KmScaleLabel.Size = new System.Drawing.Size(52, 13);
            this.KmScaleLabel.TabIndex = 1;
            this.KmScaleLabel.Text = "Scale Km";
            // 
            // SystemSelectComboBox
            // 
            this.SystemSelectComboBox.BackColor = System.Drawing.Color.MidnightBlue;
            this.SystemSelectComboBox.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.SystemSelectComboBox.ForeColor = System.Drawing.Color.White;
            this.SystemSelectComboBox.FormattingEnabled = true;
            this.SystemSelectComboBox.Location = new System.Drawing.Point(2, 142);
            this.SystemSelectComboBox.Name = "SystemSelectComboBox";
            this.SystemSelectComboBox.Size = new System.Drawing.Size(190, 21);
            this.SystemSelectComboBox.TabIndex = 0;
            this.SystemSelectComboBox.SelectionChangeCommitted += new System.EventHandler(this.SystemSelectComboBox_SelectionChangeCommitted);
            // 
            // PanUpButton
            // 
            this.PanUpButton.Location = new System.Drawing.Point(32, 3);
            this.PanUpButton.Name = "PanUpButton";
            this.PanUpButton.Size = new System.Drawing.Size(23, 23);
            this.PanUpButton.TabIndex = 3;
            this.PanUpButton.Text = "^";
            this.PanUpButton.UseVisualStyleBackColor = true;
            this.PanUpButton.Click += new System.EventHandler(this.PanUpButton_Click);
            // 
            // PanLeftButton
            // 
            this.PanLeftButton.Location = new System.Drawing.Point(6, 29);
            this.PanLeftButton.Name = "PanLeftButton";
            this.PanLeftButton.Size = new System.Drawing.Size(26, 23);
            this.PanLeftButton.TabIndex = 4;
            this.PanLeftButton.Text = "<-";
            this.PanLeftButton.UseVisualStyleBackColor = true;
            this.PanLeftButton.Click += new System.EventHandler(this.PanLeftButton_Click);
            // 
            // PanDownButton
            // 
            this.PanDownButton.Location = new System.Drawing.Point(31, 58);
            this.PanDownButton.Name = "PanDownButton";
            this.PanDownButton.Size = new System.Drawing.Size(23, 23);
            this.PanDownButton.TabIndex = 5;
            this.PanDownButton.Text = "v";
            this.PanDownButton.UseVisualStyleBackColor = true;
            this.PanDownButton.Click += new System.EventHandler(this.PanDownButton_Click);
            // 
            // PanRightButton
            // 
            this.PanRightButton.Location = new System.Drawing.Point(56, 29);
            this.PanRightButton.Name = "PanRightButton";
            this.PanRightButton.Size = new System.Drawing.Size(26, 23);
            this.PanRightButton.TabIndex = 6;
            this.PanRightButton.Text = "->";
            this.PanRightButton.UseVisualStyleBackColor = true;
            this.PanRightButton.Click += new System.EventHandler(this.PanRightButton_Click);
            // 
            // ZoomInButton
            // 
            this.ZoomInButton.Location = new System.Drawing.Point(112, 3);
            this.ZoomInButton.Name = "ZoomInButton";
            this.ZoomInButton.Size = new System.Drawing.Size(23, 23);
            this.ZoomInButton.TabIndex = 7;
            this.ZoomInButton.Text = "+";
            this.ZoomInButton.UseVisualStyleBackColor = true;
            this.ZoomInButton.Click += new System.EventHandler(this.ZoomInButton_Click);
            // 
            // ZoomOutButton
            // 
            this.ZoomOutButton.Location = new System.Drawing.Point(153, 3);
            this.ZoomOutButton.Name = "ZoomOutButton";
            this.ZoomOutButton.Size = new System.Drawing.Size(23, 23);
            this.ZoomOutButton.TabIndex = 8;
            this.ZoomOutButton.Text = "-";
            this.ZoomOutButton.UseVisualStyleBackColor = true;
            this.ZoomOutButton.Click += new System.EventHandler(this.ZoomOutButton_Click);
            // 
            // ResetViewButton
            // 
            this.ResetViewButton.Location = new System.Drawing.Point(112, 58);
            this.ResetViewButton.Name = "ResetViewButton";
            this.ResetViewButton.Size = new System.Drawing.Size(64, 23);
            this.ResetViewButton.TabIndex = 9;
            this.ResetViewButton.Text = "&Reset";
            this.ResetViewButton.UseVisualStyleBackColor = true;
            this.ResetViewButton.Click += new System.EventHandler(this.ResetViewButton_Click);
            // 
            // SystemMap
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Controls.Add(this.ControlsPanel);
            this.Controls.Add(this.FPSLabel);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.button1);
            this.Name = "SystemMap";
            this.Size = new System.Drawing.Size(1009, 681);
            this.Load += new System.EventHandler(this.SystemMap_Load);
            this.MouseHover += new System.EventHandler(this.SystemMap_MouseHover);
            this.ControlsPanel.ResumeLayout(false);
            this.ControlsPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label FPSLabel;
        private System.Windows.Forms.Panel ControlsPanel;
        private System.Windows.Forms.ComboBox SystemSelectComboBox;
        private System.Windows.Forms.Label KmScaleLabel;
        private System.Windows.Forms.Label AUScaleLabel;
        private System.Windows.Forms.Button ResetViewButton;
        private System.Windows.Forms.Button ZoomOutButton;
        private System.Windows.Forms.Button ZoomInButton;
        private System.Windows.Forms.Button PanRightButton;
        private System.Windows.Forms.Button PanDownButton;
        private System.Windows.Forms.Button PanLeftButton;
        private System.Windows.Forms.Button PanUpButton;
    }
}
