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
        protected Expander Expanderer;
        protected Label ExpanderHeader;
        protected GridView CargoGrid;
        public CargoTypeStoreView()
        {
            XamlReader.Load(this);
            Expanderer.Header = new Label {DataContext = "HeaderText" };
            
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
        }
    }
}
