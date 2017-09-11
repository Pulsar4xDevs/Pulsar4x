using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Pulsar4X.ECSLib;
using System.Windows.Input;

namespace Pulsar4X.ECSLib
{
    public class TechListVM : INotifyPropertyChanged
    {
        public DictionaryVM<Guid, string> SelectedItems {get; private set;}
        public DictionaryVM<Guid, string> PossibleItems { get; private set; }

        public ICommand AddCommand { get { return new RelayCommand<object>(obj => AddSelectedPossibleToSelected()); } }

        public TechListVM(StaticDataStore staticData)
        {
            SelectedItems = new DictionaryVM<Guid, string>();
            PossibleItems = new DictionaryVM<Guid, string>();
            foreach (var item in staticData.Techs.Values)
            {
                PossibleItems.Add(item.ID, item.Name);
            }
        }

        public TechListVM(DictionaryVM<Guid, string> selectedItems, StaticDataStore staticData) : this(staticData)
        {
            SelectedItems = selectedItems;
            foreach (var item in SelectedItems)
            {
                PossibleItems.Remove(item.Key);
            }           
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void AddSelectedPossibleToSelected()
        {
            int selectedIndex = PossibleItems.SelectedIndex;
            SelectedItems.Add(PossibleItems.GetKeyValuePair());
            PossibleItems.Remove(PossibleItems.GetKey());
            if (PossibleItems.Count > selectedIndex)
                PossibleItems.SelectedIndex = selectedIndex;
            else
                PossibleItems.SelectedIndex = PossibleItems.Count - 1;
        }
    }
}
