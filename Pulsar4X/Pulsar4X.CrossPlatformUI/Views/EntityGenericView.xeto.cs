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
        protected TabControl tabCtrl_DBTabs;

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
                _vm.SelectableEntites.SelectionChangedEvent += OnEntitySelected;
            }
        }



        private void OnEntitySelected(int oldindex, int newindex)
        {
            tabCtrl_DBTabs.RemoveAll();
            foreach(var item in _vm.Viewmodels)
            {
                TabPage newTab = new TabPage();
                SetViewForViewmodel(item, newTab);
                tabCtrl_DBTabs.Pages.Add(newTab);
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) 
        {
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
            else if (vm is TranslationMoveVM)
            {
                MoveOrderViews.TranslateMoveView view = new MoveOrderViews.TranslateMoveView();
                view.DataContext = vm;
                tabPage.Content = view;
                tabPage.Text = "Helm";
            }
        }
    }
}
