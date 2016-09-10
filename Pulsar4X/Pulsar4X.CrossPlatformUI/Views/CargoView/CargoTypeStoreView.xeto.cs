using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views.CargoView
{
    public class CargoTypeStoreView : Panel
    {
        private CargoStorageByTypeVM _vm;
        protected Expander Expanderer;
        protected Label ExpanderHeader = new Label();
        protected GridView CargoGrid;
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
                _vm.PropertyChanged += Vm_PropertyChanged;
            }
        }

        private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_vm.TypeStore)) 
                CargoGrid.ReloadData(new Range<int>(0, _vm.TypeStore.Count -1));              
        }
    }
}
