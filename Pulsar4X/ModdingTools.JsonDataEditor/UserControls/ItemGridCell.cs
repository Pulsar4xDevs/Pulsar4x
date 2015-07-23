using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Windows.Forms;
using Pulsar4X.ECSLib;

namespace ModdingTools.JsonDataEditor.UserControls
{
    public partial class ItemGridCell : UserControl
    {
        public dynamic Data { get; protected set; }
        protected dynamic _editControl_;
        private dynamic _activeControl;
        public ItemGridUC ParentGrid { get; set; }
        public int Colomn { get; set; }
        public int Row { get; set; }

        public ItemGridCell() 
        {
            InitializeComponent();
            _activeControl = displayLabel;
            displayLabel.Text = Text;
            displayLabel.MouseClick += new MouseEventHandler(OnMouseClick);
            MouseClick += new MouseEventHandler(OnMouseClick);
        }

        public ItemGridCell(dynamic data) : this()
        {
            Data = data;
        }

        /// <summary>
        /// The display text for this control
        /// </summary>
        public new string Text {
            get
            {
                string returnstring = "";

                //canot use //if (Data != null) here due to the possiblity that Data is a struct. 
                if (!ReferenceEquals(null, Data))
                    returnstring = _getText_;

                return returnstring;  
            } 
        }

        /// <summary>
        /// a virtual method to return the wanted text
        /// this will be dependant on the type of _editControl_
        /// and therefor should be overriden.
        /// </summary>
        protected virtual string _getText_
        {
            get { return Data.Text; }
        }

        public override void Refresh()
        {
            displayLabel.Text = Text;
            Size = _activeControl.Size;
            if (ParentGrid !=  null)
                ParentGrid.Resize(this);
            base.Refresh();
        }

        /// <summary>
        /// detects a click on this control and causes the control to enter editing mode.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        protected void OnMouseClick(object o, MouseEventArgs e)
        {
            if(_editControl_ != null) //ie a headertype cell won't be editable
                StartEditing(this, e);

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
            Refresh();
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
    }

    /// <summary>
    /// this is a special cell type for headers, ie use as the first cell in a row. 
    /// </summary>
    public class ItemGridCell_HeaderType : ItemGridCell
    {
        public ItemGridCell_HeaderType(string text)
            : base(text)
        {
            _editControl_ = null;
            displayLabel.BackColor = DefaultBackColor;
            BackColor = DefaultBackColor;
            Refresh();
        }

        protected override string _getText_
        {
            get { return Data; }
        }
    }

    /// <summary>
    /// String Entry version of ItemGridCell
    /// This version uses TextBox as editing mode.
    /// </summary>
    public class ItemGridCell_String : ItemGridCell
    {
        public ItemGridCell_String(string str) : base(str)
        {
            TextBox textbox = new TextBox();

            textbox.Text = str;

            _editControl_ = textbox; //set the _editControl
            textbox.Leave += new EventHandler(StopEditing); //subscribe to the correct event handler (textbox.Leave) to stop editing.

            Refresh();
        }

        protected override string _getText_
        {
            get { return Data; }
        }
    }



    /// <summary>
    /// float version of the ItemGridCell
    /// this version uses a textbox as the edit control and checks wheather casting the textbox.text is valid.
    /// </summary>
    public class ItemGridCell_FloatType : ItemGridCell
    {
        public ItemGridCell_FloatType(float num) : base(num)
        {
            TextBox textbox = new TextBox();

            textbox.Text = num.ToString();

            _editControl_ = textbox; //set the _editControl
            textbox.Leave += new EventHandler(StopEditing); //subscribe to the correct event handler (textbox.Leave) to stop editing.

            Refresh();
        }

        protected override string _getText_
        {
            get
            {              
                return Data.ToString();
            }
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
    }



    /// <summary>
    /// AbilityType version of ItemGridCell this version is pulsar specific. 
    /// This version uses Listbox to display a list of possible enum AbilityType during Editing Mode
    /// despite being pulsar specific, it should be a good example of getting a list of enums.
    /// </summary>
    public class ItemGridCell_AbilityType : ItemGridCell
    {
        public ItemGridCell_AbilityType(AbilityType? ability)
            : base(ability)
        {
            ListBox listBox = new ListBox();
            listBox.DataSource = Enum.GetValues(typeof(AbilityType));

            //listBox.Dock = DockStyle.Fill;
            Data = ability;
            listBox.SelectedItem = Data;
            _editControl_ = listBox;
            listBox.SelectedIndexChanged += new EventHandler(StopEditing);
            Refresh();
        }


        protected override string _getText_
        {
            get
            {
                return Enum.GetName(typeof(AbilityType), (AbilityType)Data);
            }
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
    }


    /// <summary>
    /// this version is pulsar specific.
    /// </summary>
    public class ItemGridCell_TechStaticDataType : ItemGridCell
    {
 
        Dictionary<string, TechSD> _selectionDictionary = new Dictionary<string, TechSD>(); 
        public ItemGridCell_TechStaticDataType(TechSD? techSD, List<TechSD> selectionList)
            : base(techSD)
        {
            foreach (var tech in selectionList)
            {
                _selectionDictionary.Add(tech.Name, tech);
            }
            ListBox listBox = new ListBox();
            listBox.DataSource = new BindingSource(_selectionDictionary, null);
            listBox.DisplayMember = "Key";
            listBox.ValueMember = "Value";
            //listBox.Dock = DockStyle.Fill;
            Data = techSD;
            listBox.SelectedItem = techSD;
            _editControl_ = listBox;
            listBox.SelectedIndexChanged += new EventHandler(StopEditing);
            Refresh();
        }

        public override sealed void Refresh()
        {
            base.Refresh();
        }

        protected override string _getText_ { get { return Data.Name; } }

        protected override bool ValadateInput()
        {
            bool success = false;
            try
            {
                Data = (TechSD)_editControl_.SelectedItem;
                success = true;
            }
            catch
            {
                success = false;
            }
            return success;
        }
    }
}
