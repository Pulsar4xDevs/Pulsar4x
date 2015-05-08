using System.Windows.Controls;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for Leaders.xaml
    /// </summary>
    public partial class Leaders : ITabControl
    {
        public string Title { get; set; }
        public Leaders()
        {
            InitializeComponent();
            Title = "Leaders";
        }
    }
}
