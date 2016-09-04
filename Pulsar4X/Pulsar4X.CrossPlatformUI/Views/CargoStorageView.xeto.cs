using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class CargoStorageView : Panel
    {
        protected GridView CargoGrid;

        public CargoStorageView()
        {
            XamlReader.Load(this);

            CargoGrid.Columns.Add(new GridColumn
            {
                HeaderText = "Item",
                DataCell = new TextBoxCell { Binding = Binding.Property<CargoItemVM, string>(r => r.ItemName) }
            });
            CargoGrid.Columns.Add(new GridColumn
            {
                HeaderText = "Cargo Type",
                DataCell = new TextBoxCell { Binding = Binding.Property<CargoItemVM, string>(r => r.CargoTypeName) }
            });
            CargoGrid.Columns.Add(new GridColumn
            {
                HeaderText = "Item Type",
                DataCell = new TextBoxCell { Binding = Binding.Property<CargoItemVM, string>(r => r.ItemTypeName) }
            });
            CargoGrid.Columns.Add(new GridColumn
            {
                HeaderText = "Amount",
                DataCell = new TextBoxCell { Binding = Binding.Property<CargoItemVM, int>(r => r.Amount).Convert(r => r.ToString()) }
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

            DataContextChanged += CargoStorageView_DataContextChanged;

        }

        public void SetDataContextFrom(ShipOrderVM shipOdersVM)
        {
            DataContext = new CargoStorageVM(shipOdersVM.GameVM, shipOdersVM.SelectedShip);
        }
        private void CargoStorageView_DataContextChanged(object sender, EventArgs e)
        {

        }
    }
}
