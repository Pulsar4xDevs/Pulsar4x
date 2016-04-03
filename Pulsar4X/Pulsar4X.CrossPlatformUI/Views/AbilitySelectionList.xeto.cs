using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
using System;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class AbilitySelectionList : Panel
    {
        protected ComboBox AbilitySelection { get; set; }
        protected Label AbilityName { get; set; }

        private ComponentAbilityDesignVM _designAbility;

        public AbilitySelectionList()
        {
            XamlReader.Load(this);
                 AbilitySelection.BindDataContext(c => c.DataStore, (DictionaryVM<Guid, string> m) => m.DisplayList);
                 AbilitySelection.SelectedIndexBinding.BindDataContext((DictionaryVM<Guid, string> m) => m.SelectedIndex);
        }

        public AbilitySelectionList(ComponentAbilityDesignVM designAbility)
            : this()
        {
            _designAbility = designAbility;
            DataContext = _designAbility;            
            AbilitySelection.SelectedIndex = 0;      
        }
    }
}
