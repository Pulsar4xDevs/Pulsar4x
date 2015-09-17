using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Pulsar4X.WPFUI.ViewModels;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for SystemWindow.xaml
    /// </summary>
    public partial class SystemMap : ITabControl
    {
        public string Title { get; set; }
        private SystemVM systemVM;
        private Canvas _canvas;
        private double canvasCenterH { get { return _canvas.ActualHeight / 2; } }
        private double canvasCenterW { get { return _canvas.ActualWidth / 2; } }
        private double zoom = 10;
        public SystemMap()
        {
            InitializeComponent();
            Title = "System Map";
            _canvas = MapCanvas;
            SystemSelection.ItemsSource = App.Current.GameVM.StarSystems;
            MapCanvas.Background = new SolidColorBrush(Brushes.DarkBlue.Color);

        }

        private void SystemSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            systemVM = (SystemVM)SystemSelection.SelectedItem;
            DrawSystem();
        }

        private void DrawSystem()
        {
            if (systemVM == null)
                return;
            MapCanvas.Children.Clear();
            foreach (var star in systemVM.Stars)
            {
                double leftPos = zoom * star.Position.X + canvasCenterW - 10;
                double topPos = zoom * star.Position.Y + canvasCenterH - 10;
                DrawBody(20, Brushes.DarkOrange, leftPos, topPos);
                
            }

            foreach (var planet in systemVM.Planets)
            {
                double leftPos = zoom * planet.Position.X + canvasCenterW - 10;
                double topPos = zoom * planet.Position.Y + canvasCenterH - 10;
                DrawBody(10, Brushes.DarkGreen, leftPos, topPos);       

                ArcSegment orbitPath = new ArcSegment();
            }
            
        }

        private void DrawBody(int size, Brush color, double leftPos, double topPos)
        {
            Ellipse bodyEllipse = new Ellipse();
            bodyEllipse.Height = size;
            bodyEllipse.Width = size;
            bodyEllipse.Fill = color;

            MapCanvas.Children.Add(bodyEllipse);
            Canvas.SetLeft(bodyEllipse, leftPos);
            Canvas.SetTop(bodyEllipse, topPos);
        }
    }
}
