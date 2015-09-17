using System.Collections.Generic;
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

                DrawOrbit(planet, leftPos, topPos);

            }
            
        }

        private void DrawOrbit(PlanetVM planet, double leftPos, double topPos)
        {
            Point arcStart = new Point(leftPos, topPos);  
            Point arcEnd = new Point(leftPos - 1, topPos - 1); 
           
            double arcRotAngle = planet.ArgumentOfPeriapsis + planet.LongitudeOfAscendingNode;

            Size arcSize = new Size(zoom * planet.Periapsis, zoom * planet.Apoapsis);

            SweepDirection sweepDirection = SweepDirection.Clockwise;
            if(topPos < canvasCenterH)
                sweepDirection = SweepDirection.Counterclockwise;

            ArcSegment orbitArc = new ArcSegment(arcEnd, arcSize, arcRotAngle, true, sweepDirection, true);

            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = arcStart;
            pathFigure.Segments.Add(orbitArc);


            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);

            Path orbitPath = new Path();
            orbitPath.Stroke = Brushes.Cornsilk;
            orbitPath.StrokeThickness = 1;
            orbitPath.Data = pathGeometry;

            MapCanvas.Children.Add(orbitPath);
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
