namespace Pulsar4X.UI.Panels
{
    partial class SysMap_ViewPort
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
            this.m_oAdvanceTime5Seconds = new System.Windows.Forms.Button();
            this.m_oAdvanceTime10Seconds = new System.Windows.Forms.Button();
            this.m_oStartSim = new System.Windows.Forms.Button();
            this.m_oAdvanceTime100Seconds = new System.Windows.Forms.Button();
            this.m_oAdvanceTime1000Seconds = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // m_oAdvanceTime5Seconds
            // 
            this.m_oAdvanceTime5Seconds.Location = new System.Drawing.Point(107, 12);
            this.m_oAdvanceTime5Seconds.Name = "m_oAdvanceTime5Seconds";
            this.m_oAdvanceTime5Seconds.Size = new System.Drawing.Size(75, 23);
            this.m_oAdvanceTime5Seconds.TabIndex = 1;
            this.m_oAdvanceTime5Seconds.Text = "5 Seconds";
            this.m_oAdvanceTime5Seconds.UseVisualStyleBackColor = true;
            // 
            // m_oAdvanceTime10Seconds
            // 
            this.m_oAdvanceTime10Seconds.Location = new System.Drawing.Point(188, 12);
            this.m_oAdvanceTime10Seconds.Name = "m_oAdvanceTime10Seconds";
            this.m_oAdvanceTime10Seconds.Size = new System.Drawing.Size(75, 23);
            this.m_oAdvanceTime10Seconds.TabIndex = 2;
            this.m_oAdvanceTime10Seconds.Text = "10 Seconds";
            this.m_oAdvanceTime10Seconds.UseVisualStyleBackColor = true;
            // 
            // m_oStartSim
            // 
            this.m_oStartSim.Location = new System.Drawing.Point(431, 12);
            this.m_oStartSim.Name = "m_oStartSim";
            this.m_oStartSim.Size = new System.Drawing.Size(75, 23);
            this.m_oStartSim.TabIndex = 3;
            this.m_oStartSim.Text = "Start Sim";
            this.m_oStartSim.UseVisualStyleBackColor = true;
            // 
            // m_oAdvanceTime100Seconds
            // 
            this.m_oAdvanceTime100Seconds.Location = new System.Drawing.Point(269, 12);
            this.m_oAdvanceTime100Seconds.Name = "m_oAdvanceTime100Seconds";
            this.m_oAdvanceTime100Seconds.Size = new System.Drawing.Size(75, 23);
            this.m_oAdvanceTime100Seconds.TabIndex = 4;
            this.m_oAdvanceTime100Seconds.Text = "100 Seconds";
            this.m_oAdvanceTime100Seconds.UseVisualStyleBackColor = true;
            // 
            // m_oAdvanceTime1000Seconds
            // 
            this.m_oAdvanceTime1000Seconds.Location = new System.Drawing.Point(350, 12);
            this.m_oAdvanceTime1000Seconds.Name = "m_oAdvanceTime1000Seconds";
            this.m_oAdvanceTime1000Seconds.Size = new System.Drawing.Size(75, 23);
            this.m_oAdvanceTime1000Seconds.TabIndex = 5;
            this.m_oAdvanceTime1000Seconds.Text = "1000 Seconds";
            this.m_oAdvanceTime1000Seconds.UseVisualStyleBackColor = true;
            // 
            // SysMap_ViewPort
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(527, 266);
            this.Controls.Add(this.m_oAdvanceTime1000Seconds);
            this.Controls.Add(this.m_oAdvanceTime100Seconds);
            this.Controls.Add(this.m_oStartSim);
            this.Controls.Add(this.m_oAdvanceTime10Seconds);
            this.Controls.Add(this.m_oAdvanceTime5Seconds);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "SysMap_ViewPort";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button m_oAdvanceTime5Seconds;
        private System.Windows.Forms.Button m_oAdvanceTime10Seconds;
        private System.Windows.Forms.Button m_oStartSim;
        private System.Windows.Forms.Button m_oAdvanceTime100Seconds;
        private System.Windows.Forms.Button m_oAdvanceTime1000Seconds;
    }
}
