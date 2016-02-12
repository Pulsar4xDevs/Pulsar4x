namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for TaskGroups.xaml
    /// </summary>
    public partial class TaskGroups : ITabControl
    {
        public string Title { get; set; }
        public TaskGroups()
        {
            InitializeComponent();
            Title = "Task Groups";
        }
    }
}
