using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ShipView : Panel
    {
        protected DropDown Systems;
        protected DropDown ShipList;
        protected ShipMoveView shipMoveView;
        protected ShipFireControlView shipFCView;
        protected ShipAttackView shipAttackView;
        protected CargoView.CargoStorageView cargoView;
        protected TabControl shipview_tabs;
        protected ComponentListView.ComponentDesignsListView componentsView = new ComponentListView.ComponentDesignsListView();

        public ShipView()
        {
            XamlReader.Load(this);
            Systems.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            Systems.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
            
            ShipList.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            ShipList.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
        }

        public ShipView(ShipOrderVM viewModel) :this()
        {
            DataContext = viewModel;
            shipMoveView = new ShipMoveView(viewModel);
            shipFCView = new ShipFireControlView(viewModel);
            shipAttackView = new ShipAttackView(viewModel);
            cargoView = new CargoView.CargoStorageView();
            cargoView.SetDataContextFrom(viewModel);
            componentsView.DataContext = new ComponentDesignsListVM(viewModel.SelectedShip);

            viewModel.ShipList.SelectionChangedEvent += ShipList_SelectionChangedEvent;

            TabPage tpMove = new TabPage();
            tpMove.Content = shipMoveView;
            tpMove.Text = "Move Orders";
            shipview_tabs.Pages.Add(tpMove);

            TabPage tpFC = new TabPage();
            tpFC.Content = shipFCView;
            tpFC.Text = "Fire Control Configuration";
            shipview_tabs.Pages.Add(tpFC);

            TabPage tpAttack = new TabPage();
            tpAttack.Content = shipAttackView;
            tpAttack.Text = "Attack Orders";
            shipview_tabs.Pages.Add(tpAttack);

            TabPage tpCargo = new TabPage();
            tpCargo.Content = cargoView;
            tpCargo.Text = "Cargo";
            shipview_tabs.Pages.Add(tpCargo);

            TabPage tpComponents = new TabPage();
            tpComponents.Content = componentsView;
            tpComponents.Text = "Components";
            shipview_tabs.Pages.Add(tpComponents);

        }

        private void ShipList_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            var vm = (ShipOrderVM)DataContext;
            componentsView.DataContext = new ComponentDesignsListVM(vm.ShipList.SelectedKey);
        }
    }
}
