using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Pulsar4X.ECSLib;

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

        //private List<List<ItemGridCell>> _grid = new List<List<ItemGridCell>>();
        private List<ItemGridRow<dynamic>> _grid = new List<ItemGridRow<dynamic>>();
        private int _columnCountData = 1;
        private int _colomnCountTLP { get { return _columnCountData + 2;} }
        private int _rowCount = 1;

        //private TableLayoutPannelDoubleBuffered tableLayoutPanel1 = new TableLayoutPannelDoubleBuffered();

        public ItemGridUC()
        {
            InitializeComponent();
            RedrawTLP();
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Show();
            tableLayoutPanel1.Visible = true;
            tableLayoutPanel1.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
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

        private void RedrawTLP()
        {
            UpdateColumnCount();
            tableLayoutPanel1.Controls.Clear();
            tableLayoutPanel1.RowStyles.Clear();
            tableLayoutPanel1.RowCount = _grid.Count +1;// add an extra row for the end to make it look nicer.
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.AutoSize)); 
            tableLayoutPanel1.ColumnStyles.Clear();
            tableLayoutPanel1.ColumnCount = _colomnCountTLP;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        }

        private void ResetCells()
        {
            int rownum = 0;
            foreach (var row in _grid)
            {
                row.HeaderCell.Row = rownum;
                tableLayoutPanel1.Controls.Add(row.HeaderCell, 0, rownum);
                int columnNum = 1;
                foreach (var cell in row.DataCells)
                {
                    cell.Row = rownum;
                    cell.Colomn = columnNum;
                    tableLayoutPanel1.Controls.Add(cell, columnNum, rownum);
                    columnNum++;
                }
                row.FooterCell.Row = rownum;
                row.FooterCell.Colomn = row.Count + 1;
                tableLayoutPanel1.Controls.Add(row.FooterCell, row.Count + 1, rownum);
                rownum++;
            }
        }

        public void HardRedraw()
        {
            tableLayoutPanel1.Visible = false;
            RedrawTLP();
            ResetCells();
            tableLayoutPanel1.Visible = true;
        }

        public void SoftRefresh()
        {
            tableLayoutPanel1.Visible = false;
            UpdateColumnCount();
            tableLayoutPanel1.RowCount = _rowCount + 1;
            tableLayoutPanel1.ColumnCount = _colomnCountTLP;
            ResetCells();
            tableLayoutPanel1.Visible = true;
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
            _grid = new List<ItemGridRow<dynamic>>();
            _columnCountData = 1;
            _rowCount = 1;
        }

        /// <summary>
        /// adds a row of ItemGridCells (or children).
        /// </summary>
        /// <param name="rowlist"></param>
        //public void AddRow(ItemGridHeaderCell headerCell, List<ItemGridDataCell> rowlist, ItemGridFooterCell footerCell = null)
        //{
        //    if (rowlist.IsNullOrEmpty() && footerCell == null )
        //    {
        //        throw new Exception("Row must contain at least one item or a footerCell included");
        //    }

        //    //create a list of ItemGridCell 
        //    List<ItemGridCell> rowGridCells = new List<ItemGridCell>();
        //    rowGridCells.Add(headerCell); //populate with the header.
        //    rowGridCells.AddRange(rowlist); //populate with the datacells
        //    //if the footer cell was given, add the footer, else create a new footer and add that.
        //    if(footerCell != null)
        //        rowGridCells.Add(footerCell);
        //    else 
        //        rowGridCells.Add(new ItemGridFooterCell(rowlist[0]));


        //    //add the row to the _grid.
        //    _grid.Add(rowGridCells);

        //    int x = 0;
        //    int y = _grid.Count -1;
        //    foreach (var cell in rowGridCells)
        //    {
        //        SetCellGridData(cell, x++, y);
        //    }
        //    //UpdateRow(_grid.Count -1);

        //}
        public void AddRow(ItemGridRow<dynamic> row)
        {
            _grid.Add(row);
        }

        public void AddRow(ItemGridHeaderCell header, List<ItemGridDataCell> dataCells, ItemGridFooterCell footer)
        {
            ItemGridRow<dynamic> row = new ItemGridRow<dynamic>(this, _grid.Count, header, dataCells, footer);
            _grid.Add(row);
            //SoftRefresh();
        }

        /// <summary>
        /// this is not properly tested.
        /// probibly don't use this.
        /// </summary>
        /// <param name="colomnlist"></param>
        //public void AddColomn(List<ItemGridCell> colomnlist)
        //{
        //    int i = 0;
        //    foreach (var cell in colomnlist)
        //    {
        //        _grid[i].Add(cell);
        //        i++;
        //    }
        //    if (colomnlist.Count > _rowCount)
        //        _rowCount = colomnlist.Count;
        //}

        public ItemGridDataCell GetCellItem(int x, int y)
        {
            return _grid[y].DataCells[x];
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
            _grid[y].AddCell(cell);
            cell.PropertyChanged += cell_PropertyChanged;
        }

        /// <summary>
        /// inserts a cell in a row.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="cell"></param>
        public void InsertCellAt(int x, int y, ItemGridDataCell cell)
        {
            _grid[y].InsertCell(x, cell);
            SoftRefresh();
        }

        public void AddCellAt(int row, ItemGridDataCell cell)
        {
            _grid[row].AddCell(cell);
            SoftRefresh();
        }

        /// <summary>
        /// returns the data object at the given row and column
        /// </summary>
        /// <param name="x">column</param>
        /// <param name="y">row</param>
        /// <returns>data object</returns>
        public object GetCellData(int x, int y)
        {
            object data = _grid[y].GetCellData(x);
            return data;
        }

        /// <summary>
        /// returns a row of data as a generic object
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public List<dynamic> GetRowData(int row)
        {
            return _grid[row].GetRowData;
        }

        public void DeleteCell(int row, int column)
        {
            _grid[row].RemoveCell(column);
            UpdateColumnCount();
        }

        private void UpdateColumnCount()
        {
            int count = 0;
            bool ischanged = false;
            //first, itterate through the rows and get the highest count.
            foreach (var row in _grid)
            {
                if (row.Count > count)
                    count = row.Count;
            }
            _columnCountData = count;
            tableLayoutPanel1.ColumnCount = _colomnCountTLP;
        }

        public ItemGridHeaderCell GetHeaderCell(int row)
        {
            return _grid[row].HeaderCell;
        }

        public ItemGridFooterCell GetFooterCell(int row)
        {
            return _grid[row].FooterCell;
        }


        
    }

    /// <summary>
    /// *should* this be ItemGridRow T ? I'm currently not using T (it's always dynamic)
    /// Is there a way to use it as T without making adding it diffucult? maybe use reflection?
    /// should it inheret from List? 
    /// </summary>
    public class ItemGridRow<T> : INotifyPropertyChanged
    {
        public ItemGridHeaderCell HeaderCell { get; set; }
        public ItemGridFooterCell FooterCell { get; set; }
        private List<ItemGridDataCell> _dataCells = new List<ItemGridDataCell>();
        private ItemGridUC ParentGrid { get; set; }
        private int Row { get; set; }

        public List<ItemGridDataCell> DataCells
        {
            get { return _dataCells; }
            set
            {
                foreach (var cell in value)
                {
                    try
                    {
                        AddCell(cell);
                    }
                    catch (Exception e)
                    {
                        throw new Exception(e.Message + "At index: " + value.IndexOf(cell));
                    }
                    HeaderCell.ParentGrid = this.ParentGrid;
                    HeaderCell.Row = this.Row;
                    FooterCell.ParentGrid = this.ParentGrid;
                    FooterCell.Row = this.Row;
                }
            }
        }

        public ItemGridRow(ItemGridUC parentGrid, int row, ItemGridHeaderCell header, List<ItemGridDataCell> dataCells, ItemGridFooterCell footer)
        {
            ParentGrid = parentGrid;
            Row = row;
            HeaderCell = header;
            HeaderCell.ParentGrid = parentGrid;
            HeaderCell.Row = row;
            FooterCell = footer;
            FooterCell.ParentGrid = parentGrid;
            FooterCell.Row = row;
            DataCells = dataCells;
        }


        /// <summary>
        /// Adds a cell to the datagrid list, sets the cells parentGrid, Row, and Column.
        /// </summary>
        /// <param name="cell"></param>
        public void AddCell(ItemGridDataCell cell)
        {
            if (cell.Data == null)
            {
                if (DataCells.Count > 0)
                    cell.SetData(DataCells[DataCells.Count - 1].Data);
            }
            if (cell.Data is T || cell.Data == null)
            {
                cell.ParentGrid = ParentGrid;
                cell.Row = Row;
                cell.Colomn = _dataCells.Count;
                _dataCells.Add(cell);
                FooterCell.Colomn = _dataCells.Count + 1;
            }
            else
            {
                throw new Exception("Cell Data Type: " + cell.Data.GetType().ToString() + " does not match the Row Data Type: " + typeof(T).ToString());
            }
        }

        /// <summary>
        /// Removes the given cell
        /// </summary>
        /// <param name="cell"></param>
        public void RemoveCell(ItemGridDataCell cell)
        {
            _dataCells.Remove(cell);
            UpdateColumnNums();
        }
        /// <summary>
        /// Removes the cell at given index
        /// </summary>
        /// <param name="index"></param>
        public void RemoveCell(int index)
        {
            _dataCells.RemoveAt(index);
            UpdateColumnNums();
        }

        public void InsertCell(int index, ItemGridDataCell cell)
        {
            if (cell.Data == null)
            {
                if (DataCells.Count > 0)
                    cell.SetData(DataCells[DataCells.Count - 1].Data);
            }
            if (cell.Data is T || cell.Data == null)
            {
                cell.ParentGrid = ParentGrid;
                cell.Row = Row;
                cell.Colomn = index;
                _dataCells.Insert(index, cell);
                UpdateColumnNums();
            }
            else
            {
                throw new Exception("Cell Data Type: " + cell.Data.GetType().ToString() + " does not match the Row Data Type: " + typeof(T).ToString());
            }
        }

        private void UpdateColumnNums()
        {
            int i = 0;
            foreach (var cell in _dataCells)
            {
                cell.Colomn = i;
                i++;
            }
            FooterCell.Colomn = _dataCells.Count;
        }

        /// <summary>
        /// returns the number of datacells (not counting header and footer cell)
        /// </summary>
        public int Count
        {
            get { return DataCells.Count; }
        }

        /// <summary>
        /// returns a list of this Rows Data.
        /// </summary>
        public List<T> GetRowData
        {
            get
            {
                List<T> dataList = new List<T>();
                foreach (var cell in DataCells)
                {
                    dataList.Add(cell.Data);
                }
                return dataList;
            }
        }

        /// <summary>
        /// returns a single cells data. 
        /// </summary>
        /// <param name="x">column (not counting header), ie first data cell is x=0</param>
        /// <returns></returns>
        public T GetCellData(int x)
        {
            return DataCells[x].Data;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }



    public class TableLayoutPannelDoubleBuffered : TableLayoutPanel
    {
        public TableLayoutPannelDoubleBuffered()
        {
            InitializeComponent();        
   
            SetStyle(ControlStyles.AllPaintingInWmPaint |
              ControlStyles.OptimizedDoubleBuffer |
              ControlStyles.UserPaint, true);
        }

        public TableLayoutPannelDoubleBuffered(IContainer container)
        {
            InitializeComponent();
      
            container.Add(this);
            SetStyle(ControlStyles.AllPaintingInWmPaint |
              ControlStyles.OptimizedDoubleBuffer |
              ControlStyles.UserPaint, true);
        }

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
            components = new System.ComponentModel.Container();
            //this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        }

        #endregion
    }
}
