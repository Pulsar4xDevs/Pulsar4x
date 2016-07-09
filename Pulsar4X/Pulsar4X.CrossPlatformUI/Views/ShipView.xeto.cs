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

        public ShipView()
        {
            XamlReader.Load(this);
            Systems.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            Systems.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
            
            ShipList.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            ShipList.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            TargetDropDown.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            TargetDropDown.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            OrdersPossible.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            OrdersPossible.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            OrderList.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            OrderList.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

        }

        public ShipView(ShipOrderVM viewModel) :this()
        {
            DataContext = viewModel;

            //ShipList.DataContext = viewModel.ShipList;
            //Systems.DataContext = viewModel.StarSystems;

            //TargetDropDown.DataContext = viewModel.TargetList;

            //OrdersPossible.DataContext = viewModel.OrdersPossible;

            //OrderList.DataContext = viewModel.OrderList;


        }
    }
}
