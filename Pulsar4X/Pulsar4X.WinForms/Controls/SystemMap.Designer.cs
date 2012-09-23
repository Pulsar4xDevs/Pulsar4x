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
            this.components = new System.ComponentModel.Container();
            this.FPSLabel = new System.Windows.Forms.Label();
            this.ControlsPanel = new System.Windows.Forms.Panel();
            this.ScaleGroupBox = new System.Windows.Forms.GroupBox();
            this.KmScaleLabel = new System.Windows.Forms.Label();
            this.AUScaleLabel = new System.Windows.Forms.Label();
            this.ViewControlsGroupBox = new System.Windows.Forms.GroupBox();
            this.PanRightButton = new System.Windows.Forms.Button();
            this.ResetViewButton = new System.Windows.Forms.Button();
            this.PanUpButton = new System.Windows.Forms.Button();
            this.PanLeftButton = new System.Windows.Forms.Button();
            this.ZoomOutButton = new System.Windows.Forms.Button();
            this.PanDownButton = new System.Windows.Forms.Button();
            this.ZoomInButton = new System.Windows.Forms.Button();
            this.SystemSelectComboBox = new System.Windows.Forms.ComboBox();
            this.InfoToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.ControlsPanel.SuspendLayout();
            this.ScaleGroupBox.SuspendLayout();
            this.ViewControlsGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // FPSLabel
            // 
            this.FPSLabel.AutoSize = true;
            this.FPSLabel.ForeColor = System.Drawing.Color.White;
            this.FPSLabel.Location = new System.Drawing.Point(3, 659);
            this.FPSLabel.Name = "FPSLabel";
            this.FPSLabel.Size = new System.Drawing.Size(36, 13);
            this.FPSLabel.TabIndex = 2;
            this.FPSLabel.Text = " FPS: ";
            this.InfoToolTip.SetToolTip(this.FPSLabel, "FPS - Should be 16 - 18 on most systems.");
            // 
            // ControlsPanel
            // 
            this.ControlsPanel.BackColor = System.Drawing.Color.MidnightBlue;
            this.ControlsPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ControlsPanel.Controls.Add(this.ScaleGroupBox);
            this.ControlsPanel.Controls.Add(this.ViewControlsGroupBox);
            this.ControlsPanel.Controls.Add(this.FPSLabel);
            this.ControlsPanel.Controls.Add(this.SystemSelectComboBox);
            this.ControlsPanel.Location = new System.Drawing.Point(3, 7);
            this.ControlsPanel.Name = "ControlsPanel";
            this.ControlsPanel.Size = new System.Drawing.Size(200, 674);
            this.ControlsPanel.TabIndex = 3;
            // 
            // ScaleGroupBox
            // 
            this.ScaleGroupBox.Controls.Add(this.KmScaleLabel);
            this.ScaleGroupBox.Controls.Add(this.AUScaleLabel);
            this.ScaleGroupBox.ForeColor = System.Drawing.Color.White;
            this.ScaleGroupBox.Location = new System.Drawing.Point(8, 144);
            this.ScaleGroupBox.Name = "ScaleGroupBox";
            this.ScaleGroupBox.Size = new System.Drawing.Size(185, 49);
            this.ScaleGroupBox.TabIndex = 11;
            this.ScaleGroupBox.TabStop = false;
            this.ScaleGroupBox.Text = "Screen Width In:";
            // 
            // KmScaleLabel
            // 
            this.KmScaleLabel.AutoSize = true;
            this.KmScaleLabel.ForeColor = System.Drawing.Color.White;
            this.KmScaleLabel.Location = new System.Drawing.Point(6, 16);
            this.KmScaleLabel.Name = "KmScaleLabel";
            this.KmScaleLabel.Size = new System.Drawing.Size(52, 13);
            this.KmScaleLabel.TabIndex = 1;
            this.KmScaleLabel.Text = "Scale Km";
            this.InfoToolTip.SetToolTip(this.KmScaleLabel, "The width of the screen in Km");
            // 
            // AUScaleLabel
            // 
            this.AUScaleLabel.AutoSize = true;
            this.AUScaleLabel.ForeColor = System.Drawing.Color.White;
            this.AUScaleLabel.Location = new System.Drawing.Point(5, 29);
            this.AUScaleLabel.Name = "AUScaleLabel";
            this.AUScaleLabel.Size = new System.Drawing.Size(52, 13);
            this.AUScaleLabel.TabIndex = 2;
            this.AUScaleLabel.Text = "Scale AU";
            this.InfoToolTip.SetToolTip(this.AUScaleLabel, "The Width of the screen in AU");
            // 
            // ViewControlsGroupBox
            // 
            this.ViewControlsGroupBox.Controls.Add(this.PanRightButton);
            this.ViewControlsGroupBox.Controls.Add(this.ResetViewButton);
            this.ViewControlsGroupBox.Controls.Add(this.PanUpButton);
            this.ViewControlsGroupBox.Controls.Add(this.PanLeftButton);
            this.ViewControlsGroupBox.Controls.Add(this.ZoomOutButton);
            this.ViewControlsGroupBox.Controls.Add(this.PanDownButton);
            this.ViewControlsGroupBox.Controls.Add(this.ZoomInButton);
            this.ViewControlsGroupBox.ForeColor = System.Drawing.Color.White;
            this.ViewControlsGroupBox.Location = new System.Drawing.Point(8, 31);
            this.ViewControlsGroupBox.Name = "ViewControlsGroupBox";
            this.ViewControlsGroupBox.Size = new System.Drawing.Size(185, 106);
            this.ViewControlsGroupBox.TabIndex = 10;
            this.ViewControlsGroupBox.TabStop = false;
            this.ViewControlsGroupBox.Text = "View Controls:";
            // 
            // PanRightButton
            // 
            this.PanRightButton.ForeColor = System.Drawing.Color.Black;
            this.PanRightButton.Location = new System.Drawing.Point(58, 44);
            this.PanRightButton.Name = "PanRightButton";
            this.PanRightButton.Size = new System.Drawing.Size(26, 23);
            this.PanRightButton.TabIndex = 6;
            this.PanRightButton.Text = "->";
            this.InfoToolTip.SetToolTip(this.PanRightButton, "Pan Right (R)");
            this.PanRightButton.UseVisualStyleBackColor = true;
            this.PanRightButton.Click += new System.EventHandler(this.PanRightButton_Click);
            // 
            // ResetViewButton
            // 
            this.ResetViewButton.ForeColor = System.Drawing.Color.Black;
            this.ResetViewButton.Location = new System.Drawing.Point(114, 73);
            this.ResetViewButton.Name = "ResetViewButton";
            this.ResetViewButton.Size = new System.Drawing.Size(64, 23);
            this.ResetViewButton.TabIndex = 9;
            this.ResetViewButton.Text = "&Reset";
            this.InfoToolTip.SetToolTip(this.ResetViewButton, "Reset View (R)");
            this.ResetViewButton.UseVisualStyleBackColor = true;
            this.ResetViewButton.Click += new System.EventHandler(this.ResetViewButton_Click);
            // 
            // PanUpButton
            // 
            this.PanUpButton.ForeColor = System.Drawing.Color.Black;
            this.PanUpButton.Location = new System.Drawing.Point(34, 18);
            this.PanUpButton.Name = "PanUpButton";
            this.PanUpButton.Size = new System.Drawing.Size(23, 23);
            this.PanUpButton.TabIndex = 3;
            this.PanUpButton.Text = "^";
            this.InfoToolTip.SetToolTip(this.PanUpButton, "Pan Up (W)");
            this.PanUpButton.UseVisualStyleBackColor = true;
            this.PanUpButton.Click += new System.EventHandler(this.PanUpButton_Click);
            // 
            // PanLeftButton
            // 
            this.PanLeftButton.ForeColor = System.Drawing.Color.Black;
            this.PanLeftButton.Location = new System.Drawing.Point(8, 44);
            this.PanLeftButton.Name = "PanLeftButton";
            this.PanLeftButton.Size = new System.Drawing.Size(26, 23);
            this.PanLeftButton.TabIndex = 4;
            this.PanLeftButton.Text = "<-";
            this.InfoToolTip.SetToolTip(this.PanLeftButton, "Pan Left (A)");
            this.PanLeftButton.UseVisualStyleBackColor = true;
            this.PanLeftButton.Click += new System.EventHandler(this.PanLeftButton_Click);
            // 
            // ZoomOutButton
            // 
            this.ZoomOutButton.ForeColor = System.Drawing.Color.Black;
            this.ZoomOutButton.Location = new System.Drawing.Point(114, 18);
            this.ZoomOutButton.Name = "ZoomOutButton";
            this.ZoomOutButton.Size = new System.Drawing.Size(23, 23);
            this.ZoomOutButton.TabIndex = 8;
            this.ZoomOutButton.Text = "-";
            this.InfoToolTip.SetToolTip(this.ZoomOutButton, "Zoom out (Q or -)");
            this.ZoomOutButton.UseVisualStyleBackColor = true;
            this.ZoomOutButton.Click += new System.EventHandler(this.ZoomOutButton_Click);
            // 
            // PanDownButton
            // 
            this.PanDownButton.ForeColor = System.Drawing.Color.Black;
            this.PanDownButton.Location = new System.Drawing.Point(33, 73);
            this.PanDownButton.Name = "PanDownButton";
            this.PanDownButton.Size = new System.Drawing.Size(23, 23);
            this.PanDownButton.TabIndex = 5;
            this.PanDownButton.Text = "v";
            this.InfoToolTip.SetToolTip(this.PanDownButton, "Pan Down (S)");
            this.PanDownButton.UseVisualStyleBackColor = true;
            this.PanDownButton.Click += new System.EventHandler(this.PanDownButton_Click);
            // 
            // ZoomInButton
            // 
            this.ZoomInButton.ForeColor = System.Drawing.Color.Black;
            this.ZoomInButton.Location = new System.Drawing.Point(155, 19);
            this.ZoomInButton.Name = "ZoomInButton";
            this.ZoomInButton.Size = new System.Drawing.Size(23, 23);
            this.ZoomInButton.TabIndex = 7;
            this.ZoomInButton.Text = "+";
            this.InfoToolTip.SetToolTip(this.ZoomInButton, "Zoom in (E or +)");
            this.ZoomInButton.UseVisualStyleBackColor = true;
            this.ZoomInButton.Click += new System.EventHandler(this.ZoomInButton_Click);
            // 
            // SystemSelectComboBox
            // 
            this.SystemSelectComboBox.BackColor = System.Drawing.Color.MidnightBlue;
            this.SystemSelectComboBox.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.SystemSelectComboBox.ForeColor = System.Drawing.Color.White;
            this.SystemSelectComboBox.FormattingEnabled = true;
            this.SystemSelectComboBox.Location = new System.Drawing.Point(3, 3);
            this.SystemSelectComboBox.Name = "SystemSelectComboBox";
            this.SystemSelectComboBox.Size = new System.Drawing.Size(190, 21);
            this.SystemSelectComboBox.TabIndex = 0;
            this.InfoToolTip.SetToolTip(this.SystemSelectComboBox, "Select a Star System to view");
            // 
            // InfoToolTip
            // 
            this.InfoToolTip.ShowAlways = true;
            // 
            // SystemMap
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Controls.Add(this.ControlsPanel);
            this.Name = "SystemMap";
            this.Size = new System.Drawing.Size(1009, 681);
            this.Load += new System.EventHandler(this.SystemMap_Load);
            this.SizeChanged += new System.EventHandler(this.SystemMap_SizeChanged);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnKeyDown);
            this.MouseHover += new System.EventHandler(this.SystemMap_MouseHover);
            this.ControlsPanel.ResumeLayout(false);
            this.ControlsPanel.PerformLayout();
            this.ScaleGroupBox.ResumeLayout(false);
            this.ScaleGroupBox.PerformLayout();
            this.ViewControlsGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

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
        private System.Windows.Forms.GroupBox ScaleGroupBox;
        private System.Windows.Forms.GroupBox ViewControlsGroupBox;
        public System.Windows.Forms.ToolTip InfoToolTip;
    }
}
