using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Pulsar4X.ECSLib;

namespace ModdingTools.JsonDataEditor.UserControls
{
    /// <summary>
    /// a horrible attempt at a more usable grid control for multiple data types.
    /// </summary>
    public partial class ItemGridUC : UserControl
    {
        
        private List<List<ItemGridCell>> _grid = new List<List<ItemGridCell>>();

        private int _colomnCount = 1;
        private int _rowCount = 1;

        public ItemGridUC()
        {
            InitializeComponent();
            //tableLayoutPanel1.CellPaint += tableLayoutPanel_CellPaint;
        }

        /// <summary>
        /// draws lines. it's way too slow though.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tableLayoutPanel_CellPaint(object sender, TableLayoutCellPaintEventArgs e)
        {
            e.Graphics.DrawLine(Pens.Black, e.CellBounds.Location, new Point(e.CellBounds.Right, e.CellBounds.Top));
            e.Graphics.DrawLine(Pens.Black, e.CellBounds.Location, new Point(e.CellBounds.Left, e.CellBounds.Bottom));
        }

        private void Update()
        {
            tableLayoutPanel1.Controls.Clear();
            tableLayoutPanel1.RowStyles.Clear();
            tableLayoutPanel1.RowCount = 0;
            tableLayoutPanel1.ColumnCount = _colomnCount;
            int y = 0;
            foreach (List<ItemGridCell> row in _grid)
            {
                int x = 0;
                tableLayoutPanel1.RowCount++;
                tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.AutoSize)); //(row[0].Height));
                foreach (ItemGridCell cell in row)
                {
                    
                    tableLayoutPanel1.Controls.Add(cell, x, y);
                    cell.Dock = DockStyle.Fill;
                    x++;
                }
                y++;
            }
            
        }

        public void Clear()
        {
            _grid = new List<List<ItemGridCell>>();
            _colomnCount = 1;
            _rowCount = 1;
        }

        public void AddRow(List<ItemGridCell> rowlist)
        {
            _grid.Add(rowlist);
            if (rowlist.Count > _colomnCount)
                _colomnCount = rowlist.Count;
            Update();
        }
        public void AddColomn(List<ItemGridCell> colomnlist)
        {
            int i = 0;
            foreach (var cell in colomnlist)
            {
                _grid[i].Add(cell);
                i++;
            }
            if (colomnlist.Count > _rowCount)
                _rowCount = colomnlist.Count;
        }

        public ItemGridCell GetCellItem(int x, int y)
        {
            return _grid[x][y];
        }

        public void SetCellItem(int x, int y, ItemGridCell cell)
        {
            
        }

        public object Data(int x, int y)
        {
            return _grid[x][y].Data;
        }
    }
}
