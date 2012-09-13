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
            this.AUScaleLabel.Location = new System.Drawing.Point(3, 44);
            this.AUScaleLabel.Name = "AUScaleLabel";
            this.AUScaleLabel.Size = new System.Drawing.Size(52, 13);
            this.AUScaleLabel.TabIndex = 2;
            this.AUScaleLabel.Text = "Scale AU";
            // 
            // KmScaleLabel
            // 
            this.KmScaleLabel.AutoSize = true;
            this.KmScaleLabel.ForeColor = System.Drawing.Color.White;
            this.KmScaleLabel.Location = new System.Drawing.Point(4, 31);
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
            this.SystemSelectComboBox.Location = new System.Drawing.Point(3, 3);
            this.SystemSelectComboBox.Name = "SystemSelectComboBox";
            this.SystemSelectComboBox.Size = new System.Drawing.Size(190, 21);
            this.SystemSelectComboBox.TabIndex = 0;
            this.SystemSelectComboBox.SelectionChangeCommitted += new System.EventHandler(this.SystemSelectComboBox_SelectionChangeCommitted);
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
    }
}
