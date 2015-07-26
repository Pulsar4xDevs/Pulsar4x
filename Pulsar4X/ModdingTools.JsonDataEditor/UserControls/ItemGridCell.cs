using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Pulsar4X.ECSLib;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ModdingTools.JsonDataEditor.UserControls
{
    public partial class ItemGridCell : UserControl 
    {
        protected dynamic _activeControl;

        /// <summary>
        /// a virtual method to return the wanted text
        /// this will be dependant on the type of _editControl_
        /// and therefor should be overriden.
        /// </summary>
        protected virtual string _get_Text_
        {
            get { return displayLabel.Text; }
        }

        public new string Text {
            get { return _get_Text_; }
            set { displayLabel.Text = value; } }

        public ItemGridUC ParentGrid { get; set; }
        public int Colomn { get; set; }
        public int Row { get; set; }

        /// <summary>
        /// Constructor. 
        /// </summary>
        protected ItemGridCell()
        {
            InitializeComponent();
            _activeControl = displayLabel;
            
            displayLabel.MouseClick += OnMouseClick;
            MouseClick += OnMouseClick;
            
        }

        /// <summary>
        /// </summary>
        /// <param name="data"></param>
        protected ItemGridCell(string text) : this()
        {           
            Text = text;
            Refresh();
        }



        public override void Refresh()
        {
            displayLabel.Text = Text;
            Size = _activeControl.Size;
            base.Refresh();
        }

        /// <summary>
        /// detects a click on this control and causes the control to enter editing mode.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        public virtual void OnMouseClick(object o, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:

                    //StartEditing(o, e);
                    break;
                case MouseButtons.Right:
                    //contextMenuStrip_Cell.Show(this, new Point(e.X, e.Y));
                    break;
            }
        }

        protected virtual void contextMenuStrip_Cell_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch (e.ClickedItem.ToString())
            {

            }
        }
    }



    public class ItemGridDataCell : ItemGridCell, INotifyPropertyChanged
    {
        
        protected dynamic _editControl_;
        private dynamic _data;

        public dynamic Data
        {
            get { return _data; }
            protected set
            {
                if (!ReferenceEquals(value, _data)) //cannot use if (value != _data) here due to the possiblity that _data is a struct. 
                {
                    _data = value;
                    NotifyPropertyChanged();
                }
            }
        }

        protected ItemGridDataCell(dynamic data):base()
        {
            _data = data;
            _activeControl = displayLabel;
        }

        protected ItemGridDataCell(string data) : base()
        {
            _data = data;
            _activeControl = displayLabel;
        }

        protected sealed override string _get_Text_
        {
            get
            {
                string returnstring = "null!";
                //cannot use if (Data != null) here due to the possiblity that Data is a struct. 
                if (!ReferenceEquals(null, Data))
                    returnstring = _getText_;
                else
                {
                    ;
                }
                return returnstring;
            }
        }

        protected virtual string _getText_ 
        {
            get { return Data.Text; }

        }

        public override void OnMouseClick(object o, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    StartEditing(o, e);
                    break;
                case MouseButtons.Right:
                    contextMenuStrip_Cell.Show(this, new Point(e.X, e.Y));
                    break;
            }
        }

        protected override void contextMenuStrip_Cell_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch (e.ClickedItem.ToString())
            {
                case "Edit":
                    StartEditing(sender, e);
                    break;
                case "Delete Cell":
                    break;
                case "Insert":
                    break;
            }
        }

        /// <summary>
        /// Starts Editing Mode causing the control to use the _editControl_
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        protected void StartEditing(object o, EventArgs e)
        {

                _activeControl = _editControl_;
                Controls.Remove(displayLabel);
                Controls.Add(_editControl_);

                //an attempt to get it to select the contents if editcontrol is a textbox
                TextBox txtBox = _editControl_ as TextBox;
                Refresh();
                if (txtBox != null)
                {
                    txtBox.Focus();
                    txtBox.SelectAll();
                }
            
        }

        /// <summary>
        /// Stops Editing Mode, causing the control to use displayLabel
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        protected void StopEditing(object o, EventArgs e)
        {
            ValadateInput();
            _activeControl = displayLabel;
            Controls.Remove(_editControl_);
            Controls.Add(displayLabel);
            Refresh();
        }

        /// <summary>
        /// translates the _editControl_ into Data
        /// since this is going to depend on what the Data Type is
        /// and what the _editControl Type is it should be overridden.
        /// </summary>
        /// <returns></returns>
        protected virtual bool ValadateInput()
        {
            Data = _editControl_.Text;
            displayLabel.Text = Text;
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property. 
        // The CallerMemberName attribute that is applied to the optional propertyName 
        // parameter causes the property name of the caller to be substituted as an argument. 
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public virtual object Copy()
        {
            return new ItemGridDataCell(_data);
        }
    }

    /// <summary>
    /// This is a special cell type for headers, ie use as the first cell in a row. 
    /// </summary>    
    public class ItemGridHeaderCell : ItemGridCell
    {
        /// <summary>
        /// this is for fancy reflection stuff...
        /// </summary>
        private PropertyInfo _rowData;

        public PropertyInfo RowData
        {
            get { return _rowData; }
            private set
            {
                if (value != null)
                    _rowData = value;
            }
        }

        public ItemGridHeaderCell(string text, PropertyInfo rowData) : base(text)
        {
            RowData = rowData;
            displayLabel.BackColor = DefaultBackColor;
            BackColor = DefaultBackColor;
            Refresh();
        }

        public override sealed void Refresh()
        {
            base.Refresh();
        }

    }

    /// <summary>
    /// This is a special cell type which gets added to the end of each row.
    /// It creates a new cell when clicked.
    /// </summary>
    public class ItemGridFooterCell : ItemGridCell
    {
        private ItemGridDataCell _newcell;

        /// <summary>
        /// an empty cell, this special cell type creates a new cell of teh defaultcell (copies the given) when clicked,
        /// adding it to the grid. 
        /// </summary>
        /// <param name="defaultCell"></param>
        public ItemGridFooterCell(ItemGridDataCell defaultCell) : base("Add Cell")
        {
            _newcell = defaultCell;
            
        }

        public override void OnMouseClick(object o, MouseEventArgs e)
        {

            ItemGridDataCell newCell = (ItemGridDataCell)_newcell.Copy();
            ParentGrid.InsertCellAt(this.Colomn, this.Row, newCell);
            newCell.OnMouseClick(o, e);
        }
    }

    /// <summary>
    /// String Entry version of ItemGridCell
    /// This version uses TextBox as editing mode.
    /// </summary>
    public class ItemGridCell_String : ItemGridDataCell
    {
        public ItemGridCell_String(string str) : base(str)
        {
            TextBox textbox = new TextBox();

            textbox.Text = str;
            textbox.Dock = DockStyle.Fill;
            _editControl_ = textbox; //set the _editControl
            textbox.Leave += new EventHandler(StopEditing); //subscribe to the correct event handler (textbox.Leave) to stop editing.
            textbox.KeyDown += new KeyEventHandler(OnKeyDown);
            Refresh();
        }

        private void OnKeyDown(object o, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                StopEditing(o, e);
            if (e.KeyCode == Keys.Escape)
            {
                _editControl_.Text = Data;
                StopEditing(o, e);
            }
        }

        public override sealed void Refresh()
        {
            base.Refresh();
        }

        protected override string _getText_
        {
            get { return Data; }
        }

        public override object Copy()
        {
            return new ItemGridCell_String(Data);
        }
    }


    /// <summary>
    /// float version of the ItemGridCell
    /// this version uses a textbox as the edit control and checks wheather casting the textbox.text is valid.
    /// </summary>
    public class ItemGridCell_FloatType : ItemGridDataCell
    {
        public ItemGridCell_FloatType(float num) : base(num)
        {
            TextBox textbox = new TextBox();

            textbox.Text = num.ToString();
            textbox.Dock = DockStyle.Fill;
            _editControl_ = textbox; //set the _editControl
            textbox.Leave += StopEditing; //subscribe to the correct event handler (textbox.Leave) to stop editing.
            textbox.KeyDown += OnKeyDown;
            Refresh();
        }

        private void OnKeyDown(object o, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                StopEditing(o, e);
            if (e.KeyCode == Keys.Escape)
            {
                _editControl_.Text = Data;
                StopEditing(o, e);
            }
        }

        protected override string _getText_
        {
            get { return Data.ToString(); }
        }


        protected override bool ValadateInput()
        {
            bool success = false;
            float newdata;
            if (float.TryParse(_editControl_.Text, out newdata))
            {
                Data = newdata;
                success = true;
            }
            return success;
        }

        public override object Copy()
        {
            return new ItemGridCell_FloatType(Data);
        }
    }



    /// <summary>
    /// AbilityType version of ItemGridCell this version is pulsar specific. 
    /// This version uses Listbox to display a list of possible enum AbilityType during Editing Mode
    /// despite being pulsar specific, it should be a good example of getting a list of enums.
    /// </summary>
    public class ItemGridCell_AbilityType : ItemGridDataCell
    {
        public ItemGridCell_AbilityType(AbilityType? ability) : base(ability)
        {
            ListBox listBox = new ListBox();
            listBox.DataSource = Enum.GetValues(typeof(AbilityType));

            //listBox.Dock = DockStyle.Fill; //DockStyle.fill really does not work well with the listbox for this is seems. 
            listBox.Width = Width;
            listBox.Height = 500;

            Data = ability;
            listBox.SelectedItem = Data;
            _editControl_ = listBox;
            listBox.SelectedIndexChanged += new EventHandler(StopEditing);
            Refresh();
        }

        protected override string _getText_
        {
            get { return Enum.GetName(typeof(AbilityType), (AbilityType)Data); }
        }

        protected override bool ValadateInput()
        {
            bool success = false;

            ListBox listBox = _editControl_;
            if (listBox.SelectedItem != null)
            {
                Data = (AbilityType)listBox.SelectedItem;
                success = true;
            }
            return success;
        }

        public override object Copy()
        {
            return new ItemGridCell_AbilityType(Data);
        }
    }


    /// <summary>
    /// this version is pulsar specific.
    /// </summary>
    public class ItemGridCell_TechStaticDataType : ItemGridDataCell
    {

        private Dictionary<Guid, TechSD> _guidDictionary = new Dictionary<Guid, TechSD>();
        private Dictionary<string, Guid> _selectionDictionary = new Dictionary<string, Guid>();

        public ItemGridCell_TechStaticDataType(Guid? techGuid, List<TechSD> selectionList) : base(techGuid)
        {
            foreach (TechSD tech in selectionList)
            {
                _guidDictionary.Add(tech.ID, tech);
                _selectionDictionary.Add(tech.Name, tech.ID);
            }
            ListBox listBox = new ListBox();
            listBox.DataSource = new BindingSource(_selectionDictionary.Keys, null);


            listBox.Width = 400;
            listBox.Height = 500;

            Data = techGuid;
            listBox.SelectedItem = techGuid;
            _editControl_ = listBox;
            listBox.SelectedIndexChanged += new EventHandler(StopEditing);
            Refresh();
        }

        protected override string _getText_
        {
            get { return _guidDictionary[Data].Name; }
        }

        protected override bool ValadateInput()
        {
            Data = _selectionDictionary[_editControl_.SelectedItem];
            return true;
        }

        public override object Copy()
        {
            return new ItemGridCell_TechStaticDataType(Data, _guidDictionary.Values.ToList());
        }
    }
}
