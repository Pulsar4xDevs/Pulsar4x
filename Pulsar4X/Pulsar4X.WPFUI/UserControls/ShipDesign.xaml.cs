using System.Windows.Controls;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for ShipDesign.xaml
    /// </summary>
    public partial class ShipDesign : ITabControl
    {
        public string Title { get; set; }
        public ShipDesign()
        {
            InitializeComponent();
            Title = "Ship Design";
        }
    }
}
