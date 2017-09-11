using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ShipAttackView : Panel
    {
        protected ListBox FireControlList;
        protected ListBox AttackTargetList;

        public ShipAttackView()
        {
            XamlReader.Load(this);
            FireControlList.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            FireControlList.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);

            AttackTargetList.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            AttackTargetList.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
        }

        public ShipAttackView(ShipOrderVM viewModel) :this()
        {
            DataContext = viewModel;
        }
    }
}
