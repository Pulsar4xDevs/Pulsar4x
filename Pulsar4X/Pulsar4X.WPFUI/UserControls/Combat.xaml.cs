namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for Combat.xaml
    /// </summary>
    public partial class Combat : ITabControl
    {
        public string Title { get; set; }
        public Combat()
        {
            InitializeComponent();
            Title = "Combat";
        }
    }
}
