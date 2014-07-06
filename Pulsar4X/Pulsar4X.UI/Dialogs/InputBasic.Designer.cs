namespace Pulsar4X.UI.Dialogs
{
    partial class InputBasic
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
            this.m_oDescription = new System.Windows.Forms.Label();
            this.m_oInputTextBox = new System.Windows.Forms.TextBox();
            this.m_oOkButton = new System.Windows.Forms.Button();
            this.m_oCancelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // m_oDescription
            // 
            this.m_oDescription.AutoSize = true;
            this.m_oDescription.Location = new System.Drawing.Point(13, 13);
            this.m_oDescription.Name = "m_oDescription";
            this.m_oDescription.Size = new System.Drawing.Size(34, 13);
            this.m_oDescription.TabIndex = 0;
            this.m_oDescription.Text = "Input:";
            // 
            // m_oInputTextBox
            // 
            this.m_oInputTextBox.Location = new System.Drawing.Point(13, 39);
            this.m_oInputTextBox.Name = "m_oInputTextBox";
            this.m_oInputTextBox.Size = new System.Drawing.Size(159, 20);
            this.m_oInputTextBox.TabIndex = 1;
            // 
            // m_oOkButton
            // 
            this.m_oOkButton.Location = new System.Drawing.Point(16, 66);
            this.m_oOkButton.Name = "m_oOkButton";
            this.m_oOkButton.Size = new System.Drawing.Size(75, 23);
            this.m_oOkButton.TabIndex = 2;
            this.m_oOkButton.Text = "OK";
            this.m_oOkButton.UseVisualStyleBackColor = true;
            // 
            // m_oCancelButton
            // 
            this.m_oCancelButton.Location = new System.Drawing.Point(97, 66);
            this.m_oCancelButton.Name = "m_oCancelButton";
            this.m_oCancelButton.Size = new System.Drawing.Size(75, 23);
            this.m_oCancelButton.TabIndex = 3;
            this.m_oCancelButton.Text = "Cancel";
            this.m_oCancelButton.UseVisualStyleBackColor = true;
            // 
            // InputBasic
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(180, 98);
            this.Controls.Add(this.m_oCancelButton);
            this.Controls.Add(this.m_oOkButton);
            this.Controls.Add(this.m_oInputTextBox);
            this.Controls.Add(this.m_oDescription);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximumSize = new System.Drawing.Size(186, 126);
            this.MinimumSize = new System.Drawing.Size(186, 126);
            this.Name = "InputBasic";
            this.Text = "Input";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label m_oDescription;
        private System.Windows.Forms.TextBox m_oInputTextBox;
        private System.Windows.Forms.Button m_oOkButton;
        private System.Windows.Forms.Button m_oCancelButton;
    }
}