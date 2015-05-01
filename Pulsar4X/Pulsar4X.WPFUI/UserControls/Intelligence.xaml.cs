using System.Windows.Controls;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for Intelligence.xaml
    /// </summary>
    public partial class Intelligence : ITabControl
    {
        public string Title { get; set; }
        public Intelligence()
        {
            InitializeComponent();
            Title = "Intelligence";
        }
    }
}
