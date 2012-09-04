namespace Pulsar4X.WinForms.Forms
{
    partial class StartupSplashScreen
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StartupSplashScreen));
            this.label1 = new System.Windows.Forms.Label();
            this.m_oStatusLabel = new System.Windows.Forms.Label();
            this.m_oTimer = new System.Windows.Forms.Timer(this.components);
            this.m_oStatusPanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Segoe UI Semibold", 27.75F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(169, 50);
            this.label1.TabIndex = 0;
            this.label1.Text = "Pulsar4X";
            // 
            // m_oStatusLabel
            // 
            this.m_oStatusLabel.AutoSize = true;
            this.m_oStatusLabel.BackColor = System.Drawing.Color.Transparent;
            this.m_oStatusLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 14.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_oStatusLabel.ForeColor = System.Drawing.Color.White;
            this.m_oStatusLabel.Location = new System.Drawing.Point(12, 59);
            this.m_oStatusLabel.Name = "m_oStatusLabel";
            this.m_oStatusLabel.Size = new System.Drawing.Size(95, 25);
            this.m_oStatusLabel.TabIndex = 1;
            this.m_oStatusLabel.Text = "Loading...";
            // 
            // m_oTimer
            // 
            this.m_oTimer.Tick += new System.EventHandler(this.Timer_Tick);
            // 
            // m_oStatusPanel
            // 
            this.m_oStatusPanel.BackColor = System.Drawing.Color.Transparent;
            this.m_oStatusPanel.Location = new System.Drawing.Point(13, 369);
            this.m_oStatusPanel.Name = "m_oStatusPanel";
            this.m_oStatusPanel.Size = new System.Drawing.Size(575, 19);
            this.m_oStatusPanel.TabIndex = 2;
            this.m_oStatusPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.m_oStatusPanel_Paint);
            // 
            // StartupSplashScreen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.Controls.Add(this.m_oStatusPanel);
            this.Controls.Add(this.m_oStatusLabel);
            this.Controls.Add(this.label1);
            this.Cursor = System.Windows.Forms.Cursors.AppStarting;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "StartupSplashScreen";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Pulasr4X Loading...";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label m_oStatusLabel;
        private System.Windows.Forms.Timer m_oTimer;
        private System.Windows.Forms.Panel m_oStatusPanel;
    }
}