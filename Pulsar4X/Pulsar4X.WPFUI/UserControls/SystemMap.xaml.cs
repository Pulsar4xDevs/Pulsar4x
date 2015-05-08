using System.Windows.Controls;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for SystemWindow.xaml
    /// </summary>
    public partial class SystemMap : ITabControl
    {
        public string Title { get; set; }
        public SystemMap()
        {
            InitializeComponent();
            Title = "System Map";
        }
    }
}
