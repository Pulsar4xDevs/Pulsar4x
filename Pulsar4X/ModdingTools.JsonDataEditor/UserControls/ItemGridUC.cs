using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace ModdingTools.JsonDataEditor.UserControls
{
    /// <summary>
    /// a horrible attempt at a more usable grid control for multiple data types.
    /// </summary>
    public partial class ItemGridUC : UserControl , INotifyPropertyChanged
    {
        /// <summary>
        /// this gets invoked when a cell's Data changes. 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
         
        /// <summary>
        /// this gets invoked when a row changes. 
        /// This is the same as PropertyChanged, however it returns the row number.
        /// (cast to an int)
        /// </summary>
        public event PropertyChangedEventHandler RowChanged;

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
                    cell.PropertyChanged += cell_PropertyChanged;
                    x++;
                }
                y++;
            }
            
        }

        void cell_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //throw new System.NotImplementedException();
            NotifyCellPropertyChanged(sender);
            NotifyRowPropertyChanged(sender as ItemGridCell);
        }

        //private void row_PropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    NotifyRowPropertyChanged(sender);
        //}

        private void NotifyRowPropertyChanged(ItemGridCell cell, [CallerMemberName] String propertyName = "")
        {
            
            if (RowChanged != null)
            {
                RowChanged(cell.Row, new PropertyChangedEventArgs(propertyName));
            }
        }

        // This method is called by the Set accessor of each property. 
        // The CallerMemberName attribute that is applied to the optional propertyName 
        // parameter causes the property name of the caller to be substituted as an argument. 
        private void NotifyCellPropertyChanged(object cell, [CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(cell, new PropertyChangedEventArgs(propertyName));
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
        public void AddRow(ItemGridCell_HeaderType headerCell, List<ItemGridCell> rowlist)
        {
            if (rowlist.IsNullOrEmpty() )
            {
                throw new Exception("Row must contain at least one item, consider using an ItemGridCell_EmptyType");
            }

            if (!(rowlist[rowlist.Count-1] is ItemGridCell_EmptyCellType))
            {   if (!(rowlist[0] is ItemGridCell_HeaderType))
                    rowlist.Add(new ItemGridCell_EmptyCellType(rowlist[0]));
                else
                    rowlist.Add(new ItemGridCell_EmptyCellType(rowlist[1]));
            }
            if (rowlist[0] is ItemGridCell_HeaderType)
                rowlist[0] = headerCell;
            else
                rowlist.Insert(0,headerCell);

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
        /// Replaces a cell at the given colomn and row with the given cell.
        /// not tested, this will crash if the row and or colomn are out of bounds..
        /// </summary>
        /// <param name="x">colomn</param>
        /// <param name="y">row</param>
        /// <param name="cell">cell</param>
        public void SetCellItem(int x, int y, ItemGridCell cell)
        {
            _grid[y][x] = cell;
            cell.PropertyChanged += cell_PropertyChanged;
        }

        /// <summary>
        /// inserts a cell in a row.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="cell"></param>
        public void InsertCellAt(int x, int y, ItemGridCell cell)
        {
            _grid[y].Insert(x, cell);
            UpdateTable();
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

        /// <summary>
        /// returns a row of data as a generic object
        /// </summary>
        /// <param name="row"></param>
        /// <param name="ignoreHeader">if true, checks the first cell type and ignores it if it's ItemGridCell_HeaderType</param>
        /// <returns></returns>
        public List<object> RowData(int row, bool ignoreHeader = true)
        {
            List<object> rowdataList = new List<object>();

            int upper = _grid[row].Count -1;
            for (int i = 1; i < upper; i++)
            {
                rowdataList.Add(_grid[row][i].Data);
            }
            return rowdataList;
        }

        /// <summary>
        /// a static which returns a list of Data in a given row that matches the data type T
        /// </summary>
        /// <typeparam name="T">type of data expected</typeparam>
        /// <param name="itemGridUC">the ItemGridUC</param>
        /// <param name="row">the row of data to get</param>
        /// <returns></returns>
        public static List<T> RowDataofType<T>(ItemGridUC itemGridUC, int row)
        {
            List<T> rowdataList = new List<T>();
            foreach (var cell in itemGridUC._grid[row])
            {
                if(cell.Data is T)
                    rowdataList.Add((T)cell.Data);
            }
            return rowdataList;
        }

        
    }
}
