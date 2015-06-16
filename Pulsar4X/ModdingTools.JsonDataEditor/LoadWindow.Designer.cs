namespace ModdingTools.JsonDataEditor
{
    partial class LoadWindow
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
            this.loadFileButton = new System.Windows.Forms.Button();
            this.saveAllButton = new System.Windows.Forms.Button();
            this.loadedFilesCount = new System.Windows.Forms.Label();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.techButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.installationsButton = new System.Windows.Forms.Button();
            this.ComponentButton = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.SuspendLayout();
            // 
            // loadFileButton
            // 
            this.loadFileButton.Location = new System.Drawing.Point(3, 359);
            this.loadFileButton.Name = "loadFileButton";
            this.loadFileButton.Size = new System.Drawing.Size(75, 23);
            this.loadFileButton.TabIndex = 0;
            this.loadFileButton.Text = "Load Directory";
            this.loadFileButton.UseVisualStyleBackColor = true;
            this.loadFileButton.Click += new System.EventHandler(this.loadFileButton_Click);
            // 
            // saveAllButton
            // 
            this.saveAllButton.Location = new System.Drawing.Point(85, 359);
            this.saveAllButton.Name = "saveAllButton";
            this.saveAllButton.Size = new System.Drawing.Size(75, 23);
            this.saveAllButton.TabIndex = 1;
            this.saveAllButton.Text = "Save All";
            this.saveAllButton.UseVisualStyleBackColor = true;
            this.saveAllButton.Click += new System.EventHandler(this.saveAllButton_Click);
            // 
            // loadedFilesCount
            // 
            this.loadedFilesCount.AutoSize = true;
            this.loadedFilesCount.Location = new System.Drawing.Point(3, 0);
            this.loadedFilesCount.Name = "loadedFilesCount";
            this.loadedFilesCount.Size = new System.Drawing.Size(76, 13);
            this.loadedFilesCount.TabIndex = 2;
            this.loadedFilesCount.Text = "Loaded files: 0";
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog1";
            // 
            // techButton
            // 
            this.techButton.Location = new System.Drawing.Point(3, 212);
            this.techButton.Name = "techButton";
            this.techButton.Size = new System.Drawing.Size(97, 23);
            this.techButton.TabIndex = 3;
            this.techButton.Text = "Technologies";
            this.techButton.UseVisualStyleBackColor = true;
            this.techButton.Click += new System.EventHandler(this.techButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(183, 143);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(319, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Label which fills horribly empty space which would be filled up later";
            // 
            // installationsButton
            // 
            this.installationsButton.Location = new System.Drawing.Point(3, 183);
            this.installationsButton.Name = "installationsButton";
            this.installationsButton.Size = new System.Drawing.Size(97, 23);
            this.installationsButton.TabIndex = 5;
            this.installationsButton.Text = "Installations";
            this.installationsButton.UseVisualStyleBackColor = true;
            this.installationsButton.Click += new System.EventHandler(this.installationsButton_Click);
            // 
            // ComponentButton
            // 
            this.ComponentButton.Location = new System.Drawing.Point(3, 242);
            this.ComponentButton.Name = "ComponentButton";
            this.ComponentButton.Size = new System.Drawing.Size(97, 23);
            this.ComponentButton.TabIndex = 6;
            this.ComponentButton.Text = "Components";
            this.ComponentButton.UseVisualStyleBackColor = true;
            this.ComponentButton.Click += new System.EventHandler(this.ComponentButton_Click);
            // 
            // folderBrowserDialog1
            // 
            this.folderBrowserDialog1.RootFolder = System.Environment.SpecialFolder.MyComputer;
            // 
            // LoadWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ComponentButton);
            this.Controls.Add(this.installationsButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.techButton);
            this.Controls.Add(this.loadedFilesCount);
            this.Controls.Add(this.saveAllButton);
            this.Controls.Add(this.loadFileButton);
            this.Name = "LoadWindow";
            this.Size = new System.Drawing.Size(560, 385);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button loadFileButton;
        private System.Windows.Forms.Button saveAllButton;
        private System.Windows.Forms.Label loadedFilesCount;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Button techButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button installationsButton;
        private System.Windows.Forms.Button ComponentButton;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
    }
}
