using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
namespace Pulsar4X.CrossPlatformUI.Views.CargoView
{
    
    public class CargoStorageView : Scrollable
    {
        protected StackLayout CargoTypes;
        public CargoStorageView()
        {
            XamlReader.Load(this);
            DataContextChanged += CargoStorageView_DataContextChanged;
        }

        private void CargoStorageView_DataContextChanged(object sender, EventArgs e)
        {
            if (DataContext is CargoStorageVM)
            {
                CargoTypes.Items.Clear();
                CargoStorageVM dc = (CargoStorageVM)DataContext;
                foreach (var item in dc.CargoStore)
                {
                    CargoTypeStoreView typesView = new CargoTypeStoreView();
                    typesView.DataContext = item;
                    CargoTypes.Items.Add(typesView);
                }
            }
        }
        public void SetDataContextFrom(ShipOrderVM shipOrderVM)
        {
            CargoStorageVM vm = new CargoStorageVM(shipOrderVM.GameVM);
            vm.Initialise(shipOrderVM.SelectedShip);
            DataContext = vm;
        }
    }
}
