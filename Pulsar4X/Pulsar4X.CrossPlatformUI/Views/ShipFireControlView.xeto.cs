using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ShipFireControlView : Panel
    {
        protected ListBox FireControlList;
        protected ListBox ActiveBeamList;
        protected ListBox FreeBeamList;

        public ShipFireControlView()
        {
            XamlReader.Load(this);
            FireControlList.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            FireControlList.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            ActiveBeamList.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            ActiveBeamList.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            FreeBeamList.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            FreeBeamList.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
        }

        public ShipFireControlView(ShipOrderVM viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
