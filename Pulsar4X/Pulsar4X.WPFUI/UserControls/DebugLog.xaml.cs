using System.Windows;
using System.Windows.Controls;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for DebugLog.xaml
    /// </summary>
    public partial class DebugLog : ITabControl
    {

        public string Title { get; set; }
        public DebugLog()
        {
            InitializeComponent();
            Title = "Debug Log";
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
