using System.Windows.Controls;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for SystemView.xaml
    /// </summary>
    public partial class SystemView : ITabControl
    {
        public string Title { get; set; }
        public SystemView()
        {
            InitializeComponent();
            Title = "System View";
        }
    }
}
