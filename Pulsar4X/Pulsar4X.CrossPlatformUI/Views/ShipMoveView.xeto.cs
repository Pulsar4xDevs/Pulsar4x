using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ShipMoveView : Panel
    {
        protected ListBox OrdersPossible;
        protected ListBox OrderList;
        protected StackLayout TargetArea;
        protected DropDown TargetDropDown;

        public ShipMoveView()
        {
            XamlReader.Load(this);
            TargetDropDown.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            TargetDropDown.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            OrdersPossible.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            OrdersPossible.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            OrderList.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            OrderList.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
        }

        public ShipMoveView(ShipOrderVM viewModel) :this()
        {
            DataContext = viewModel;
        }
    }
}
