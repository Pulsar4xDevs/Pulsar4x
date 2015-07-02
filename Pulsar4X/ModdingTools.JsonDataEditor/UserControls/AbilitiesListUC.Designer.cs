namespace ModdingTools.JsonDataEditor
{
    partial class AbilitiesListUC
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
            this.listBox_allAbilities = new System.Windows.Forms.ListBox();
            this.dataGridView_addedAbilities = new System.Windows.Forms.DataGridView();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_addedAbilities)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.listBox_allAbilities, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.dataGridView_addedAbilities, 0, 1);
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
            this.label1.Size = new System.Drawing.Size(45, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Abilities:";
            // 
            // listBox_allAbilities
            // 
            this.listBox_allAbilities.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox_allAbilities.FormattingEnabled = true;
            this.listBox_allAbilities.Location = new System.Drawing.Point(78, 3);
            this.listBox_allAbilities.Name = "listBox_allAbilities";
            this.tableLayoutPanel1.SetRowSpan(this.listBox_allAbilities, 2);
            this.listBox_allAbilities.Size = new System.Drawing.Size(69, 144);
            this.listBox_allAbilities.TabIndex = 2;
            this.listBox_allAbilities.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listBox_allAbilities_MouseDoubleClick);
            // 
            // dataGridView_addedAbilities
            // 
            this.dataGridView_addedAbilities.AllowUserToAddRows = false;
            this.dataGridView_addedAbilities.AllowUserToDeleteRows = false;
            this.dataGridView_addedAbilities.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView_addedAbilities.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView_addedAbilities.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView_addedAbilities.Location = new System.Drawing.Point(3, 23);
            this.dataGridView_addedAbilities.MultiSelect = false;
            this.dataGridView_addedAbilities.Name = "dataGridView_addedAbilities";
            this.dataGridView_addedAbilities.RowHeadersVisible = false;
            this.dataGridView_addedAbilities.Size = new System.Drawing.Size(69, 124);
            this.dataGridView_addedAbilities.TabIndex = 3;
            this.dataGridView_addedAbilities.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView_addedAbilities_CellEndEdit);
            this.dataGridView_addedAbilities.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_addedAbilities_CellMouseClick);
            // 
            // AbilitiesListUC
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "AbilitiesListUC";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView_addedAbilities)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox listBox_allAbilities;
        private System.Windows.Forms.DataGridView dataGridView_addedAbilities;
    }
}
