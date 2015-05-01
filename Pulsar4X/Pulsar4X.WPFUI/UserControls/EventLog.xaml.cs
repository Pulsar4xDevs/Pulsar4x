using System.Windows.Controls;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for EventLog.xaml
    /// </summary>
    public partial class EventLog : ITabControl
    {
        public string Title { get; set; }
        public EventLog()
        {
            InitializeComponent();
            Title = "Event Log";
        }
    }
}
