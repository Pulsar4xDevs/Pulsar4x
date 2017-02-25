using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
using System.Collections.Specialized;

namespace Pulsar4X.CrossPlatformUI.Views.CargoView
{
    public class CargoTypeStoreView : Panel
    {
        private CargoStorageByTypeVM _vm;
        protected Expander Expanderer;
        protected Label ExpanderHeader = new Label();
        protected GridView CargoGrid;
        protected StackLayout ComponentsStack;

        public CargoTypeStoreView()
        {
            XamlReader.Load(this);
            ExpanderHeader.TextBinding.BindDataContext((CargoStorageByTypeVM m) => m.HeaderText);
            Expanderer.Header = ExpanderHeader;
            
            CargoGrid.Columns.Add(new GridColumn
            {
                HeaderText = "Item",
                DataCell = new TextBoxCell { Binding = Binding.Property<CargoItemVM, string>(r => r.ItemName) }
            });
            CargoGrid.Columns.Add(new GridColumn
            {
                HeaderText = "Item Type",
                DataCell = new TextBoxCell { Binding = Binding.Property<CargoItemVM, string>(r => r.ItemTypeName) }
            });
            CargoGrid.Columns.Add(new GridColumn
            {
                HeaderText = "Amount",
                DataCell = new TextBoxCell { ID = "AmountCell", Binding = Binding.Property<CargoItemVM, long>(r => r.Amount).Convert(r => r.ToString()) }
            });
            CargoGrid.Columns.Add(new GridColumn
            {
                HeaderText = "Item Weight",
                DataCell = new TextBoxCell { Binding = Binding.Property<CargoItemVM, float>(r => r.ItemWeight).Convert(r => r.ToString()) }
            });
            CargoGrid.Columns.Add(new GridColumn
            {
                HeaderText = "Total Weight",
                DataCell = new TextBoxCell { Binding = Binding.Property<CargoItemVM, float>(r => r.TotalWeight).Convert(r => r.ToString()) }
            });
            DataContextChanged += CargoTypeStoreView_DataContextChanged;
        }

        private void CargoTypeStoreView_DataContextChanged(object sender, EventArgs e)
        {
            if (DataContext is CargoStorageByTypeVM)
            {
                CargoStorageByTypeVM vm = (CargoStorageByTypeVM)DataContext;
                _vm = vm;
                ResetDesignStore();
                _vm.DesignStore.CollectionChanged += DesignStore_CollectionChanged;
                _vm.TypeStore.CollectionChanged += TypeStore_CollectionChanged;
                _vm.PropertyChanged += Vm_PropertyChanged;
            }
        }

        private void ResetDesignStore()
        {
            foreach (var item in _vm.DesignStore)
            {
                ComponentsStack.Items.Add(new CargoTypeStoreView() { DataContext = item });
            }
        }

        private void DesignStore_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ComponentsStack.Items.Clear();
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    ComponentsStack.Items.Add(new CargoTypeStoreView() { DataContext = item });
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (ComponentSpecificDesignVM item in e.OldItems)
                {
                    foreach (var stackitem in ComponentsStack.Items)
                    {
                        if (stackitem.Control.DataContext == item)
                        {
                            ComponentsStack.Items.Remove(stackitem);
                            break;
                        }
                    }

                }
            }
            else
            {
                ResetDesignStore();
            } 
        }

        private void TypeStore_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CargoGrid.Height = 52 + 24 * _vm.TypeStore.Count;
            //CargoGrid.DataStore = _vm.TypeStore;
            //if (e.Action == NotifyCollectionChangedAction.Add )
            //{
            //    CargoGrid.DataStore = _vm.TypeStore;
            //}
        }

        private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_vm.TypeStore)) 
                CargoGrid.ReloadData(new Range<int>(0, _vm.TypeStore.Count -1));              
        }
    }
}
