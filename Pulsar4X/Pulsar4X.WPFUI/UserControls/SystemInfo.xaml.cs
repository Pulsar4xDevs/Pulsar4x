using System.Windows.Controls;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for SystemView.xaml
    /// </summary>
    public partial class SystemInfo : ITabControl
    {
        public string Title { get; set; }
        public SystemInfo()
        {
            InitializeComponent();
            Title = "System Information";
        }
    }
}
