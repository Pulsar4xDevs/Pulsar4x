namespace ModdingTools.JsonDataEditor
{
    partial class TechRequirementsUC
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.listBox_allTechs = new System.Windows.Forms.ListBox();
            this.listBox_requredTechs = new System.Windows.Forms.ListBox();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.listBox_allTechs, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.listBox_requredTechs, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(150, 150);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 3);
            this.label1.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Tech Reqs:";
            // 
            // listBox_allTechs
            // 
            this.listBox_allTechs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox_allTechs.FormattingEnabled = true;
            this.listBox_allTechs.Location = new System.Drawing.Point(78, 3);
            this.listBox_allTechs.Name = "listBox_allTechs";
            this.tableLayoutPanel1.SetRowSpan(this.listBox_allTechs, 2);
            this.listBox_allTechs.Size = new System.Drawing.Size(69, 144);
            this.listBox_allTechs.TabIndex = 2;
            // 
            // listBox_requredTechs
            // 
            this.listBox_requredTechs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox_requredTechs.FormattingEnabled = true;
            this.listBox_requredTechs.Location = new System.Drawing.Point(3, 23);
            this.listBox_requredTechs.Name = "listBox_requredTechs";
            this.listBox_requredTechs.Size = new System.Drawing.Size(69, 124);
            this.listBox_requredTechs.TabIndex = 3;
            // 
            // TechRequirementsUC
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "TechRequirementsUC";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox listBox_allTechs;
        private System.Windows.Forms.ListBox listBox_requredTechs;
    }
}
