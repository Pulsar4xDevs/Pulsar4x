using System.Windows.Forms;

namespace Pulsar4X.UI.Panels
{
    partial class ClassDes_RenameClass
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// user entered name for the new class.
        /// </summary>
        public TextBox RenameClassTextBox
        {
            get { return m_oRenameClassTextBox; }
        }

        /// <summary>
        /// If okayed, the new name will be copied over.
        /// </summary>
        public Button OKButton
        {
            get { return m_oOKButton; }
        }

        /// <summary>
        /// If canceled it will not be.
        /// </summary>
        public Button CancelButton
        {
            get { return m_oCancelButton; }
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
            this.m_oNewClassNameLabel = new System.Windows.Forms.Label();
            this.m_oRenameClassTextBox = new System.Windows.Forms.TextBox();
            this.m_oOKButton = new System.Windows.Forms.Button();
            this.m_oCancelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // m_oNewClassNameLabel
            // 
            this.m_oNewClassNameLabel.AutoSize = true;
            this.m_oNewClassNameLabel.Location = new System.Drawing.Point(13, 13);
            this.m_oNewClassNameLabel.Name = "m_oNewClassNameLabel";
            this.m_oNewClassNameLabel.Size = new System.Drawing.Size(157, 13);
            this.m_oNewClassNameLabel.TabIndex = 0;
            this.m_oNewClassNameLabel.Text = "Please enter a new class name:";
            // 
            // m_oRenameClassTextBox
            // 
            this.m_oRenameClassTextBox.Location = new System.Drawing.Point(16, 89);
            this.m_oRenameClassTextBox.Name = "m_oRenameClassTextBox";
            this.m_oRenameClassTextBox.Size = new System.Drawing.Size(327, 20);
            this.m_oRenameClassTextBox.TabIndex = 1;
            // 
            // m_oOKButton
            // 
            this.m_oOKButton.Location = new System.Drawing.Point(268, 13);
            this.m_oOKButton.Name = "m_oOKButton";
            this.m_oOKButton.Size = new System.Drawing.Size(75, 23);
            this.m_oOKButton.TabIndex = 2;
            this.m_oOKButton.Text = "OK";
            this.m_oOKButton.UseVisualStyleBackColor = true;
            // 
            // m_oCancelButton
            // 
            this.m_oCancelButton.Location = new System.Drawing.Point(268, 42);
            this.m_oCancelButton.Name = "m_oCancelButton";
            this.m_oCancelButton.Size = new System.Drawing.Size(75, 23);
            this.m_oCancelButton.TabIndex = 3;
            this.m_oCancelButton.Text = "Cancel";
            this.m_oCancelButton.UseVisualStyleBackColor = true;
            // 
            // ClassDes_RenameClass
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(355, 121);
            this.Controls.Add(this.m_oCancelButton);
            this.Controls.Add(this.m_oOKButton);
            this.Controls.Add(this.m_oRenameClassTextBox);
            this.Controls.Add(this.m_oNewClassNameLabel);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(363, 155);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(363, 155);
            this.Name = "ClassDes_RenameClass";
            this.Text = "Change Name";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label m_oNewClassNameLabel;
        private System.Windows.Forms.TextBox m_oRenameClassTextBox;
        private System.Windows.Forms.Button m_oOKButton;
        private System.Windows.Forms.Button m_oCancelButton;
    }
}