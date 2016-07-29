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
        }
    }
}
