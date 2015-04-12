using System.Windows.Forms;
namespace Pulsar4X.UI.Panels
{
    partial class ConfirmActivity
    {
        public enum UserSelection
        {
            Yes,
            No
        }
        /// <summary>
        /// Change activity.
        /// </summary>
        public Button YesButton
        {
            get { return m_oYesConfirmButton; }
        }

        /// <summary>
        /// Don't change activity.
        /// </summary>
        public Button NoButton
        {
            get { return m_oNoConfirmButton; }
        }

        public Label ShipyardNamePromptLabel
        {
            get { return m_oSYConfirmLabel; }
        }

        public UserSelection UserEntry { get; set; }


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
            this.m_oYesConfirmButton = new System.Windows.Forms.Button();
            this.m_oNoConfirmButton = new System.Windows.Forms.Button();
            this.m_oSYConfirmLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // m_oYesConfirmButton
            // 
            this.m_oYesConfirmButton.Location = new System.Drawing.Point(310, 30);
            this.m_oYesConfirmButton.Name = "m_oYesConfirmButton";
            this.m_oYesConfirmButton.Size = new System.Drawing.Size(75, 23);
            this.m_oYesConfirmButton.TabIndex = 0;
            this.m_oYesConfirmButton.Text = "Yes";
            this.m_oYesConfirmButton.UseVisualStyleBackColor = true;
            // 
            // m_oNoConfirmButton
            // 
            this.m_oNoConfirmButton.Location = new System.Drawing.Point(390, 30);
            this.m_oNoConfirmButton.Name = "m_oNoConfirmButton";
            this.m_oNoConfirmButton.Size = new System.Drawing.Size(75, 23);
            this.m_oNoConfirmButton.TabIndex = 1;
            this.m_oNoConfirmButton.Text = "No";
            this.m_oNoConfirmButton.UseVisualStyleBackColor = true;
            // 
            // m_oSYConfirmLabel
            // 
            this.m_oSYConfirmLabel.AutoSize = true;
            this.m_oSYConfirmLabel.Location = new System.Drawing.Point(136, 9);
            this.m_oSYConfirmLabel.Name = "m_oSYConfirmLabel";
            this.m_oSYConfirmLabel.Size = new System.Drawing.Size(464, 13);
            this.m_oSYConfirmLabel.TabIndex = 2;
            this.m_oSYConfirmLabel.Text = "Shipyard SY already has a task in progress. Are you sure you want to cancel the e" +
                "xisting activity?\r\n";
            // 
            // ConfirmActivity
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(792, 66);
            this.Controls.Add(this.m_oSYConfirmLabel);
            this.Controls.Add(this.m_oNoConfirmButton);
            this.Controls.Add(this.m_oYesConfirmButton);
            this.Name = "ConfirmActivity";
            this.Text = "ConfirmActivity";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button m_oYesConfirmButton;
        private System.Windows.Forms.Button m_oNoConfirmButton;
        private System.Windows.Forms.Label m_oSYConfirmLabel;
    }
}