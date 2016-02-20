using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
{
    public class TechListVM : INotifyPropertyChanged
    {
        public DictionaryVM<Guid, string, string> SelectedItems {get; private set;}
        public DictionaryVM<Guid, string, string> PossibleItems { get; private set; }

        public TechListVM(StaticDataStore staticData)
        {
            SelectedItems = new DictionaryVM<Guid, string, string>();
            PossibleItems = new DictionaryVM<Guid, string, string>();
            foreach (var item in staticData.Techs.Values)
            {
                PossibleItems.Add(item.ID, item.Name);
            }
        }

        public TechListVM(DictionaryVM<Guid, string, string> selectedItems, StaticDataStore staticData) : this(staticData)
        {
            SelectedItems = selectedItems;
            foreach (var item in SelectedItems)
            {
                if(!SelectedItems.Keys.Contains(item.Key))
                    PossibleItems.Remove(item);
            }           
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void AddSelectedPossibleToSelected()
        {
            SelectedItems.Add(PossibleItems.GetKeyValuePair());
            PossibleItems.Remove(PossibleItems.GetKey());
        }
    }
}
