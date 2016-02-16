namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for Ships.xaml
    /// </summary>
    public partial class Ships : ITabControl
    {
        public string Title { get; set; }
        public Ships()
        {
            InitializeComponent();
            Title = "Ships";
        }
    }
}
