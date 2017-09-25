using System;
using System.Collections.Generic;
using System.ComponentModel;
using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class EntityGenericView : Panel
    {
        EntityVM _vm;
        protected ComboBox selectableEntitesCB;
        protected TabControl datablobVM_tabs;

        public EntityGenericView()
        {
            XamlReader.Load(this);

            selectableEntitesCB.BindDataContext(c => c.DataStore, (DictionaryVM<Guid, string> m) => m.DisplayList);
            selectableEntitesCB.SelectedIndexBinding.BindDataContext((DictionaryVM<Guid, string> m) => m.SelectedIndex);
            
            DataContextChanged += OnDataContextChanged;

        }

        private void OnDataContextChanged(object sender, EventArgs e)
        {
            if(DataContext is EntityVM)
            {
                _vm = (EntityVM)DataContext;
                _vm.PropertyChanged += OnPropertyChanged;
                if(_vm.HasEntity)
                { }
                selectableEntitesCB.DataContext = _vm.SelectableEntites;
            }
        }



        private void OnEntitySelected()
        {
            foreach(var item in _vm.Viewmodels)
            {
                TabPage newTab = new TabPage();
                SetViewForViewmodel(item, newTab);
                datablobVM_tabs.Pages.Add(newTab);
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) 
        {
            if(e.PropertyName == nameof(_vm.HasEntity) && _vm.HasEntity == true)
                OnEntitySelected();
               
        }

        private void SetViewForViewmodel(IDBViewmodel vm, TabPage tabPage)
        {

            if(vm is CargoStorageVM)
            {
                CargoView.CargoStorageView view = new CargoView.CargoStorageView();
                view.DataContext = vm;
                tabPage.Content = view;
                tabPage.Text = "Cargo";
            }
            else if (vm is RefiningVM)
            {
                RefinaryView.RefinaryView view = new RefinaryView.RefinaryView();
                view.DataContext = vm;
                tabPage.Content = view;
                tabPage.Text = "Refinary";
            }



        }


        
    }

}
