using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Pulsar4X.ViewModel
{

    public class FormulaEditorVM : INotifyPropertyChanged
    {

        private ComponentTemplateVM _parent;

        public string FormulaName { get; set; }

        
        public string Formula
        {
            get { return _parent.FocusedText; }
            set { _parent.FocusedText = value; OnPropertyChanged(); }
        }

        private int _caretIndex;
        public int CaretIndex
        {
            get { return _caretIndex; }
            set { _caretIndex = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public List <ButtonInfo> ParameterButtons { get; set; }
        public List<ButtonInfo> FunctionButtons { get; set; }

        public FormulaEditorVM(ComponentTemplateVM parent)
        {
            _parent = parent;
            ParameterButtons = new List<ButtonInfo>();
            ParameterButtons.Add(new ButtonInfo("[Size]","Links to the Size formula field", this ));
            ParameterButtons.Add(new ButtonInfo("[Crew]", "Links to the Crew requred formula field", this));
            ParameterButtons.Add(new ButtonInfo("[HTK]", "Links to the Hit To Kill formula field", this));
            ParameterButtons.Add(new ButtonInfo("[ResearchCost]", "Links to the Research cost formula field", this));
            ParameterButtons.Add(new ButtonInfo("[MineralCost]", "Links to the Mineral cost formula field", this));
            ParameterButtons.Add(new ButtonInfo("[CreditCost]", "Links to the Credit Cost formula field", this));
            ParameterButtons.Add(new ButtonInfo("[GuidDict]", "A special parameter for a key value pair collection, used in ability formula fields", this));

            _parent.PropertyChanged += _parent_PropertyChanged;
        }

        private void _parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FocusText")
                OnPropertyChanged("Formula");
        }

        public void AddParam(string param)
        {
            Formula = Formula.Insert(CaretIndex, param);
        }
    }

    public class ButtonInfo
    {
        public string Text { get; set; }
        public string ToolTipText { get; set; }
        public FormulaEditorVM Parent { get; set; }
        public ICommand AddCommand { get { return new RelayCommand<object>(obj => Parent.AddParam(Text)); } }

        public ButtonInfo(string text, string tooltext, FormulaEditorVM parent)
        {
            Text = text; ToolTipText = tooltext; Parent = parent;
        }


    }
}
