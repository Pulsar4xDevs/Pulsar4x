namespace ModdingTools.JsonDataEditor.UserControls
{
    partial class ItemGridCell
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
            this.displayLabel = new System.Windows.Forms.Label();
            this.contextMenuStrip_Cell = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem_Edit = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_DelCell = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Insert = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip_Cell.SuspendLayout();
            this.SuspendLayout();
            // 
            // displayLabel
            // 
            this.displayLabel.AutoSize = true;
            this.displayLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.displayLabel.Location = new System.Drawing.Point(0, 0);
            this.displayLabel.Name = "displayLabel";
            this.displayLabel.Padding = new System.Windows.Forms.Padding(1);
            this.displayLabel.Size = new System.Drawing.Size(37, 15);
            this.displayLabel.TabIndex = 0;
            this.displayLabel.Text = "label1";
            // 
            // contextMenuStrip_Cell
            // 
            this.contextMenuStrip_Cell.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem_Edit,
            this.toolStripMenuItem_DelCell,
            this.toolStripMenuItem_Insert});
            this.contextMenuStrip_Cell.Name = "contextMenuStrip_Cell";
            this.contextMenuStrip_Cell.Size = new System.Drawing.Size(148, 70);
            this.contextMenuStrip_Cell.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.contextMenuStrip_Cell_ItemClicked);
            // 
            // toolStripMenuItem_Edit
            // 
            this.toolStripMenuItem_Edit.Name = "toolStripMenuItem_Edit";
            this.toolStripMenuItem_Edit.Size = new System.Drawing.Size(147, 22);
            this.toolStripMenuItem_Edit.Text = "Edit";
            // 
            // toolStripMenuItem_DelCell
            // 
            this.toolStripMenuItem_DelCell.Name = "toolStripMenuItem_DelCell";
            this.toolStripMenuItem_DelCell.Size = new System.Drawing.Size(147, 22);
            this.toolStripMenuItem_DelCell.Text = "Delete This Cell";
            // 
            // toolStripMenuItem_Insert
            // 
            this.toolStripMenuItem_Insert.Name = "toolStripMenuItem_Insert";
            this.toolStripMenuItem_Insert.Size = new System.Drawing.Size(147, 22);
            this.toolStripMenuItem_Insert.Text = "Insert Cell";
            // 
            // ItemGridCell
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.displayLabel);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "ItemGridCell";
            this.Size = new System.Drawing.Size(41, 19);
            this.contextMenuStrip_Cell.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        protected System.Windows.Forms.Label displayLabel;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Edit;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_DelCell;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Insert;
        protected System.Windows.Forms.ContextMenuStrip contextMenuStrip_Cell;

    }
}
