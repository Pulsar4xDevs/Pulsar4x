using System.Windows.Controls;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for GalaticMap.xaml
    /// </summary>
    public partial class GalaticMap : ITabControl
    {
        public string Title { get; set; }
        public GalaticMap()
        {
            InitializeComponent();
            Title = "Galatic Map";
        }
    }
}
