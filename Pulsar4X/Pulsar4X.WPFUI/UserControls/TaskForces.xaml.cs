using System.Windows.Controls;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for TaskForces.xaml
    /// </summary>
    public partial class TaskForces : ITabControl
    {
        public string Title { get; set; }
        public TaskForces()
        {
            InitializeComponent();
            Title = "Task Forces";
        }
    }
}
