namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for Populations.xaml
    /// </summary>
    public partial class Populations : ITabControl
    {
        public string Title { get; set; }
        public Populations()
        {
            InitializeComponent();
            Title = "Populations";
        }
    }
}
