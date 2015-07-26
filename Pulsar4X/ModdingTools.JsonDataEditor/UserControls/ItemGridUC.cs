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

        private void UpdateTLP()
        {
            tableLayoutPanel1.Controls.Clear();
            tableLayoutPanel1.RowStyles.Clear();
            tableLayoutPanel1.RowCount = _grid.Count +1;// add an extra row for the end to make it look nicer.
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.AutoSize)); 
            tableLayoutPanel1.ColumnStyles.Clear();           
            tableLayoutPanel1.ColumnCount = _colomnCount;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        }

        /// <summary>
        /// this redraws the table layout pannel.
        /// could probbily be more efficent, esp since it's called every time a row is added.
        /// </summary>
        private void UpdateTable()
        {
            UpdateTLP();

            int y = 0;
            foreach (List<ItemGridCell> row in _grid)
            {
                
                //for each cell
                for (int x = 0; x < row.Count; x++)
                {
                   //UpdateCellGridData(row[x], x, y);      
                    SetCellGridData(row[x], x, y);
                    UpdateCellGridData(row[x], x, y);
                }
                //for each datagridcell
                for (int x = 1; x < row.Count -1; x++)
                {
                    ItemGridDataCell cell = (ItemGridDataCell)row[x];
                    cell.PropertyChanged += cell_PropertyChanged;
                }

                y++;
            }
            
        }

        public void HardRedraw()
        {
            UpdateTable();
        }

        private void SetCellGridData(ItemGridCell cell, int x, int y)
        {
            if (x > tableLayoutPanel1.ColumnCount)
                UpdateColumnCount();
            
            tableLayoutPanel1.Controls.Add(cell, x, y);
            cell.ParentGrid = this;
            cell.Colomn = x;
            cell.Row = y;
            cell.Dock = DockStyle.Fill;
        }

        private void UpdateCellGridData(ItemGridCell cell, int x, int y)
        {
            if (x > tableLayoutPanel1.ColumnCount)
                UpdateColumnCount();
            tableLayoutPanel1.SetRow(cell, y);
            tableLayoutPanel1.SetColumn(cell,x);
            cell.Colomn = x;
            cell.Row = y;
        }

        private void UpdateRow(int rowNum)
        {
            List<ItemGridCell> row = _grid[rowNum];
            if (row.Count > _colomnCount)
            {
                tableLayoutPanel1.ColumnStyles.Clear();
                tableLayoutPanel1.ColumnCount = _colomnCount;
                for (int i = 0; i < _colomnCount; i++)
                {
                    tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                }

                for (int x = 0; x < row.Count; x++)
                {
                    tableLayoutPanel1.SetColumn(row[x], x);
                    row[x].Colomn = x;
                }

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
        public void AddRow(ItemGridHeaderCell headerCell, List<ItemGridDataCell> rowlist, ItemGridFooterCell footerCell = null)
        {
            if (rowlist.IsNullOrEmpty() && footerCell == null )
            {
                throw new Exception("Row must contain at least one item or a footerCell included");
            }
            
            //create a list of ItemGridCell 
            List<ItemGridCell> rowGridCells = new List<ItemGridCell>();
            rowGridCells.Add(headerCell); //populate with the header.
            rowGridCells.AddRange(rowlist); //populate with the datacells
            //if the footer cell was given, add the footer, else create a new footer and add that.
            if(footerCell != null)
                rowGridCells.Add(footerCell);
            else 
                rowGridCells.Add(new ItemGridFooterCell(rowlist[0]));


            //add the row to the _grid.
            _grid.Add(rowGridCells);

            int x = 1;
            int y = _grid.Count -1;
            foreach (var cell in rowGridCells)
            {
                SetCellGridData(cell, x++, y);
            }
            //UpdateRow(_grid.Count -1);
    
        }

        /// <summary>
        /// this is not properly tested.
        /// probibly don't use this.
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
        public void SetCellItem(int x, int y, ItemGridDataCell cell)
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
            if (_colomnCount < _grid[y].Count)
                _colomnCount = _grid[y].Count;
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
            ItemGridDataCell cell = (ItemGridDataCell)_grid[y][x];
            return cell.Data;
        }

        /// <summary>
        /// returns a row of data as a generic object
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public List<object> RowData(int row)
        {
            List<object> rowdataList = new List<object>();

            int upper = _grid[row].Count -1; // count-1 so we don't get the footer.
            for (int i = 1; i < upper; i++) // start at +1 so we don't get the header.
            {
                ItemGridDataCell cell = (ItemGridDataCell)_grid[row][i];
                rowdataList.Add(cell.Data);
            }
            return rowdataList;
        }

        public void DeleteCell(int row, int column)
        {
            _grid[row].RemoveAt(column);
            if(!UpdateColumnCount())
                UpdateTable();
        }

        private bool UpdateColumnCount()
        {
            int count = 0;
            bool ischanged = false;
            foreach (var row in _grid)
            {
                if (row.Count > count)
                    count = row.Count;
            }
            if (tableLayoutPanel1.ColumnCount != count)
            {
                _colomnCount = count;
                tableLayoutPanel1.ColumnCount = count;
                ischanged = true;
            }
            return ischanged;
        }

        public ItemGridHeaderCell GetHeaderCell(int row)
        {
            return (ItemGridHeaderCell)_grid[row][0];
        }

        public ItemGridFooterCell GetFooterCell(int row)
        {
            return (ItemGridFooterCell)_grid[row][_grid[row].Count - 1];
        }

        /// <summary>
        /// a static which returns a list of Data in a given row that matches the data type T
        /// </summary>
        /// <typeparam name="T">type of data expected</typeparam>
        /// <param name="itemGridUC">the ItemGridUC</param>
        /// <param name="row">the row of data to get</param>
        /// <returns></returns>
        //public static List<T> RowDataofType<T>(ItemGridUC itemGridUC, int row)
        //{
        //    List<T> rowdataList = new List<T>();
        //    foreach (var cell in itemGridUC._grid[row])
        //    {
        //        if(cell.Data is T)
        //            rowdataList.Add((T)cell.Data);
        //    }
        //    return rowdataList;
        //}

        
    }

    /// <summary>
    /// TODO: future make the _grid be List<ItemGridRow>
    /// should this be ItemGridRow T ?
    /// should it inheret from List T?
    /// </summary>
    public class ItemGridRow<T>
    {
        public ItemGridHeaderCell HeaderCell { get; set; }
        public ItemGridFooterCell FooterCell { get; set; }
        public List<ItemGridDataCell> Cells { get; set; } 
        public ItemGridRow(ItemGridHeaderCell header, List<ItemGridDataCell> dataCells, ItemGridFooterCell footer )
        {
        }
    }
}
