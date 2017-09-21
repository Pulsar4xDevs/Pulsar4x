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
        protected TabControl datablobVM_tabs;

        public EntityGenericView()
        {
            XamlReader.Load(this);
            DataContextChanged += OnDataContextChanged;

        }

        private void OnDataContextChanged(object sender, EventArgs e)
        {
            if(sender is EntityVM)
            {
                _vm = (EntityVM)sender;
                _vm.PropertyChanged += OnPropertyChanged;
                if(_vm.HasEntity)
                { }
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
            Type type = vm.GetType();
            if(type is CargoStorageVM)
            {
                CargoView.CargoStorageView view = new CargoView.CargoStorageView();
                view.DataContext = vm;
                tabPage.Content = view;
            }
            else if(type is RefiningVM)
            {
                //refi view = new CargoView.CargoStorageView();
                //view.DataContext = vm;
                //tabPage.Content = view;
            }


        }


        
    }

}
