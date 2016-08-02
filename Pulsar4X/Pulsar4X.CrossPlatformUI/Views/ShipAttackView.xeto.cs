using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ShipAttackView : Panel
    {
        protected ListBox FireControlList;

        public ShipAttackView()
        {
            XamlReader.Load(this);
            FireControlList.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            FireControlList.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
        }

        public ShipAttackView(ShipOrderVM viewModel) :this()
        {
            DataContext = viewModel;
        }
    }
}
