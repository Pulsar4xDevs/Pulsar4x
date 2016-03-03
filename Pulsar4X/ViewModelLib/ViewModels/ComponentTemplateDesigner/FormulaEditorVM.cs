using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
{

    public class FormulaEditorVM : INotifyPropertyChanged
    {

        private ComponentTemplateParentVM _parent;

        public string FormulaName { get; set; }

        public DictionaryVM<Guid, string, string> TechList { get; private set; }
        
        public string Formula
        {
            get
            {
                if (_parent.ControlInFocus != null)
                    return _parent.ControlInFocus.FocusedText;
                else
                    return "";
            }
            set { _parent.ControlInFocus.FocusedText = value; OnPropertyChanged(); }
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

        public FormulaEditorVM(ComponentTemplateParentVM parent, StaticDataStore staticData)
        {
            _parent = parent;

            TechList = new DictionaryVM<Guid, string, string>();
            foreach (var item in staticData.Techs.Values)
            {
                TechList.Add(item.ID, item.Name);
            }
            TechList.SelectionChangedEvent += TechList_SelectionChangedEvent;

            ParameterButtons = new List<ButtonInfo>();
            ParameterButtons.Add(new ButtonInfo("[Size]","Links to the Size formula field", this ));
            ParameterButtons.Add(new ButtonInfo("[Crew]", "Links to the Crew requred formula field", this));
            ParameterButtons.Add(new ButtonInfo("[HTK]", "Links to the Hit To Kill formula field", this));
            ParameterButtons.Add(new ButtonInfo("[ResearchCost]", "Links to the Research cost formula field", this));
            ParameterButtons.Add(new ButtonInfo("[MineralCost]", "Links to the Mineral cost formula field", this));
            ParameterButtons.Add(new ButtonInfo("[CreditCost]", "Links to the Credit Cost formula field", this));
            ParameterButtons.Add(new ButtonInfo("[GuidDict]", "A special parameter for a key value pair collection, used in ability formula fields", this));

            FunctionButtons = new List<ButtonInfo>();

            
        }

        private void TechList_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            AddParam("TechData('" + TechList.GetKey().ToString() + "')");
        }

        public void RefreshFormula()
        {
            OnPropertyChanged(nameof(Formula));
        }


        public void AddParam(string param)
        {
            //Formula = Formula.Insert(CaretIndex, param);
            //CaretIndex doesn't fire an event so we'll have to just append for now
            Formula += param;
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
