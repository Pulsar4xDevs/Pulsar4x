using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ComponentsListView : Panel
    {
        protected StackLayout StackLayout { get; set; }


        public ComponentsListView()
        {
            XamlReader.Load(this);
        }
        public ComponentsListView(ComponentListVM viewModel) : this()
        {
            foreach (var item in viewModel.Engines)
            {
                StackLayout.Items.Add(new ComponentListComponentView(item));
            }
        }
    }
}
