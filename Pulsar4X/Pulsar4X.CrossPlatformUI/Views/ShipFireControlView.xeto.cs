using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ShipFireControlView : Panel
    {
        public ListBox FireControlsList;
        public ListBox ActiveBeamsList;
        public ListBox FreeBeamsList;

        public ShipFireControlView()
        {
            XamlReader.Load(this);

            FireControlsList.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            FireControlsList.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            ActiveBeamsList.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            ActiveBeamsList.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            FreeBeamsList.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            FreeBeamsList.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
        }

        public ShipFireControlView(ShipOrderVM viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
