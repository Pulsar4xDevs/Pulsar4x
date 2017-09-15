using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;
using System.Collections.Specialized;

namespace Pulsar4X.CrossPlatformUI.Views.CargoView
{
    public class CargoTypeStoreView : Panel
    {
        private CargoTypeStoreVM _vm;
        protected Expander Expanderer;
        protected Label ExpanderHeader = new Label();
        protected GridView CargoGrid;
        protected StackLayout ComponentsStack;

        public CargoTypeStoreView()
        {
            XamlReader.Load(this);
            ExpanderHeader.TextBinding.BindDataContext((CargoTypeStoreVM m) => m.HeaderText);
            Expanderer.Header = ExpanderHeader;

            CargoGrid.Columns.Add(new GridColumn
            {
                HeaderText = "Item",
                DataCell = new TextBoxCell { Binding = Binding.Property<CargoItemVM, string>(r => r.ItemName) }
            });
            CargoGrid.Columns.Add(new GridColumn
            {
                HeaderText = "Item Type",
                DataCell = new TextBoxCell { Binding = Binding.Property<CargoItemVM, string>(r => r.ItemName) }
            });
            CargoGrid.Columns.Add(new GridColumn
            {
                HeaderText = "Amount",
                DataCell = new TextBoxCell { ID = "AmountCell", Binding = Binding.Property<CargoItemVM, string>(r => r.NumberOfItems)}//.Convert(r => r.ToString()) }
            });
            CargoGrid.Columns.Add(new GridColumn
            {
                HeaderText = "Item Weight",
                DataCell = new TextBoxCell { Binding = Binding.Property<CargoItemVM, string>(r => r.ItemWeightPerUnit)}//.Convert(r => r.ToString()) }
            });
            CargoGrid.Columns.Add(new GridColumn
            {
                HeaderText = "Total Weight",
                AutoSize = true,
                DataCell = new TextBoxCell { Binding = Binding.Property<CargoItemVM, String>(r => r.TotalWeight)}//.Convert(r => r.ToString()) }
            });

            DataContextChanged += CargoTypeStoreView_DataContextChanged;
            
        }

        private void CargoTypeStoreView_DataContextChanged(object sender, EventArgs e)
        {
            if (DataContext is CargoTypeStoreVM)
            {
                CargoTypeStoreVM vm = (CargoTypeStoreVM)DataContext;
                _vm = vm;
                ResetDesignStore();
                CargoGrid.Height = 52 + 24 * _vm.CargoItems.Count;
                vm.CargoItems.CollectionChanged += TypeStore_CollectionChanged;
                //_vm.DesignStore.CollectionChanged += DesignStore_CollectionChanged;
                //_vm.TypeStore.CollectionChanged += TypeStore_CollectionChanged;
                //_vm.PropertyChanged += Vm_PropertyChanged;
            }
        }

        private void ResetDesignStore()
        {
            //foreach (var item in _vm.DesignStore)
            //{
            //    ComponentsStack.Items.Add(new CargoTypeStoreView() { DataContext = item });
            //}
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
            CargoGrid.Height = 52 + 24 * _vm.CargoItems.Count;
        }
    }
}
