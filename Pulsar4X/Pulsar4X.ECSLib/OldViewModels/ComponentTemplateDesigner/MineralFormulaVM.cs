using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public class MineralFormulaVM : ComponentTemplateDesignerBaseVM
    {
        private StaticDataStore _dataStore;
        private string _mineralFormula;
        public string MineralFormula {
            get { return _mineralFormula; }
            set { _mineralFormula = value; OnPropertyChanged(); } }
        public DictionaryVM<Guid, string> Minerals { get; set; }

        public override string FocusedText
        {
            get
            {
                switch (SubControlInFocus)
                {
                    case FocusedControl.AbilityFormulaControl:
                        return MineralFormula;
                    default:
                        return "";
                }
            }

            set
            {
                switch (SubControlInFocus)
                {

                    case FocusedControl.AbilityFormulaControl:
                        MineralFormula = value;
                        break;
                }
                ParentVM.ControlInFocus = this;
            }
        }


        public MineralFormulaVM(ComponentTemplateParentVM parent, StaticDataStore staticDataStore) : base(parent)
        {
            _dataStore = staticDataStore;
            Minerals = new DictionaryVM<Guid, string>(DisplayMode.Value);
            foreach (var item in staticDataStore.CargoGoods.GetMineralsList())
            {
                Minerals.Add(item.ID, item.Name);
            }
        }

        public MineralFormulaVM(ComponentTemplateParentVM parent, StaticDataStore staticDataStore, KeyValuePair<Guid, string> guidFormulaKVP) : this(parent, staticDataStore)
        {
            MineralFormula = guidFormulaKVP.Value;
            Minerals.SelectedIndex = Minerals.GetIndex(new KeyValuePair<Guid, string>(guidFormulaKVP.Key, Minerals[guidFormulaKVP.Key]));
        }

        public void OnSelectionChange(object sender, EventArgs e)
        {
            OnPropertyChanged();
        }

    }
}
