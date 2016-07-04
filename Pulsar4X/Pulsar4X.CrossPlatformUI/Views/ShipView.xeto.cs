using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ShipView : Panel
    {
        protected DropDown Systems;
        protected DropDown ShipList;
        protected ListBox OrdersPossible;
        protected ListBox OrderList;
        protected StackLayout TargetArea;
        protected DropDown TargetDropDown;

        protected Button AddOrder;

        public ShipView()
        {
            XamlReader.Load(this);
        }

        public ShipView(ShipOrderVM viewModel) :this()
        {
            DataContext = viewModel;

            Systems.DataContext = viewModel.StarSystems;
            Systems.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            Systems.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            TargetDropDown.DataContext = viewModel.TargetList;
            TargetDropDown.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            TargetDropDown.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            ShipList.DataContext = viewModel.ShipList;
            ShipList.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            ShipList.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            OrdersPossible.DataContext = viewModel.OrdersPossible;
            OrdersPossible.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            OrdersPossible.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            OrderList.DataContext = viewModel.OrderList;
            OrderList.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            OrderList.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            AddOrder.Command = viewModel.AddOrder;
        }
    }
}
