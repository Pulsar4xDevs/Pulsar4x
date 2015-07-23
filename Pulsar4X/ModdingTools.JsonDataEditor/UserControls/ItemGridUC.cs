using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

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
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void tableLayoutPanel_CellPaint(object sender, TableLayoutCellPaintEventArgs e)
        {
            e.Graphics.DrawLine(Pens.Black, e.CellBounds.Location, new Point(e.CellBounds.Right, e.CellBounds.Top));
            e.Graphics.DrawLine(Pens.Black, e.CellBounds.Location, new Point(e.CellBounds.Left, e.CellBounds.Bottom));
        }

        /// <summary>
        /// this redraws the table layout pannel.
        /// could probbily be more efficent, esp since it's called every time a row is added.
        /// </summary>
        private void UpdateTable()
        {
            tableLayoutPanel1.Controls.Clear();
            tableLayoutPanel1.RowStyles.Clear();
            tableLayoutPanel1.RowCount = 0;
            tableLayoutPanel1.ColumnStyles.Clear();
            tableLayoutPanel1.ColumnCount = _colomnCount;
            int y = 0;
            foreach (List<ItemGridCell> row in _grid)
            {
                int x = 0;
                tableLayoutPanel1.RowCount++;
                tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.AutoSize)); 
                foreach (ItemGridCell cell in row)
                {
                    if (x == 0)
                        tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                    tableLayoutPanel1.Controls.Add(cell, x, y);
                    cell.ParentGrid = this;
                    cell.Colomn = x;
                    cell.Row = y;
                    cell.Dock = DockStyle.Fill;
                    x++;
                }
                y++;
            }
            
        }

        public void RowResize(int row, int height)
        {
            tableLayoutPanel1.RowStyles[row].Height = height;
        }

        public void ColomnResize(int column, int width)
        {
            tableLayoutPanel1.ColumnStyles[column].Width = width;
        }

        /// <summary>
        /// suposedly resizes the row and column for a given cell to a given cell's size
        /// this may not be even neciscary or usefull since at the moment the styles are auto. 
        /// </summary>
        /// <param name="cell"></param>
        // ReSharper disable once InconsistentNaming
        public void ResizeXY(ItemGridCell cell)
        {
            tableLayoutPanel1.RowStyles[cell.Colomn].Height = cell.Size.Height;
            tableLayoutPanel1.ColumnStyles[cell.Row].Width = cell.Size.Width;
        }

        /// <summary>
        /// returns the height of a specific row.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public int GetRowHeight(int row)
        {
            return tableLayoutPanel1.GetRowHeights()[row];
        }

        /// <summary>
        /// returns the width of a specific column
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public int GetColomnWidth(int column)
        {
            return tableLayoutPanel1.GetColumnWidths()[column];
        }

        /// <summary>
        /// clears the grid.
        /// </summary>
        public void Clear()
        {
            _grid = new List<List<ItemGridCell>>();
            _colomnCount = 1;
            _rowCount = 1;
        }

        /// <summary>
        /// adds a row of ItemGridCells (or children).
        /// </summary>
        /// <param name="rowlist"></param>
        public void AddRow(List<ItemGridCell> rowlist)
        {
            _grid.Add(rowlist);
            if (rowlist.Count > _colomnCount)
                _colomnCount = rowlist.Count;
            UpdateTable();
        }

        /// <summary>
        /// this is not properly tested.
        /// </summary>
        /// <param name="colomnlist"></param>
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

        /// <summary>
        /// not tested, this will crash if the row and or colomn are out of bounds..
        /// </summary>
        /// <param name="x">colomn</param>
        /// <param name="y">row</param>
        /// <param name="cell">cell</param>
        public void SetCellItem(int x, int y, ItemGridCell cell)
        {
            _grid[y][x] = cell;
        }

        /// <summary>
        /// returns the data object at the given row and column
        /// </summary>
        /// <param name="x">column</param>
        /// <param name="y">row</param>
        /// <returns>data object</returns>
        public object Data(int x, int y)
        {
            return _grid[y][x].Data;
        }
    }
}
