using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ViewModel
{
    public class MineralFormulaVM : IViewModel
    {
        private StaticDataStore _dataStore;
        public string Forumula { get; set; }
        public DictionaryVM<Guid, string, string> Minerals { get; set; }

        public MineralFormulaVM(StaticDataStore staticDataStore)
        {
            _dataStore = staticDataStore;
            Minerals = new DictionaryVM<Guid, string, string>(DisplayMode.Value);
            foreach (var item in staticDataStore.Minerals)
            {
                Minerals.Add(item.ID, item.Name);
            }
                        
        }

        public MineralFormulaVM(StaticDataStore staticDataStore, KeyValuePair<Guid, string> guidFormulaKVP) : this(staticDataStore)
        {
            this.Forumula = guidFormulaKVP.Value;
            Minerals.SelectedIndex = Minerals.GetIndex(new KeyValuePair<Guid, string>(guidFormulaKVP.Key, Minerals[guidFormulaKVP.Key]));
        }

        public void OnSelectionChange(object sender, EventArgs e)
        {
            OnPropertyChanged();
        }
        //public void SetSelectedMineral(int index)
        //{            
        //    //selectedMineralKVP = Minerals.GetKeyValuePair(index);
        //    //OnPropertyChanged("selectedMineralKVP");
        //}

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public void Refresh(bool partialRefresh = false)
        {
            throw new NotImplementedException();
        }
    }
}
