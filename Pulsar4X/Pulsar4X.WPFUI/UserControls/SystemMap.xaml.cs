using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Pulsar4X.ECSLib;
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

        private double canvasCenterH
        {
            get { return _canvas.ActualHeight / 2; }
        }

        private double canvasCenterW
        {
            get { return _canvas.ActualWidth / 2; }
        }

        private double zoom = 10;

        public SystemMap()
        {
            InitializeComponent();
            Title = "System Map";
            _canvas = MapCanvas;
            SystemSelection.ItemsSource = App.Current.GameVM.StarSystems;
            SystemSelection.DisplayMemberPath = "Name";
            MapCanvas.Background = new SolidColorBrush(Brushes.DarkBlue.Color);

        }

        private void SystemSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            systemVM = (SystemVM)SystemSelection.SelectedItem;
            //systemVM.PropertyChanged += system_PropertyChanged;
            DrawSystem();
        }

        private void planet_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            MapCanvas.UpdateLayout();
        }

        private void system_PropertyChanged(object sender, System.ComponentModel.PropertyChangingEventArgs e)
        {
            DrawSystem();
        }

        private void DrawSystem()
        {
            if (systemVM == null)
                return;
            MapCanvas.Children.Clear();
            foreach (var star in systemVM.Stars)
            {
                double leftPos = zoom * star.Position.X + canvasCenterW;
                double topPos = zoom * star.Position.Y + canvasCenterH;
                DrawBody(20, Brushes.DarkOrange, leftPos, topPos);
                foreach (var planet in star.ChildPlanets)
                {

                    double planetLeftPos = zoom * (star.Position.X + planet.Position.X) + canvasCenterW;
                    double planetTopPos = zoom * (star.Position.Y + planet.Position.Y) + canvasCenterH;
                    DrawBody(10, Brushes.DarkGreen, planetLeftPos, planetTopPos);

                    DrawOrbit(planetLeftPos, planetTopPos, planet);

                    //DrawDebugLines(leftPos, topPos, planetLeftPos, planetTopPos, planet);
                }
            }
        }


        private void DrawOrbit(double leftPos, double topPos, PlanetVM planet)
        {

            Point arcStart = new Point(leftPos, topPos);
            Point arcEnd = new Point(leftPos + 1, topPos);

            double arcRotAngle = planet.ArgumentOfPeriapsis + planet.LongitudeOfAscendingNode; // if inclination is 0

            Size arcSize = new Size(zoom * planet.Periapsis, zoom * planet.Apoapsis);

            SweepDirection sweepDirection = SweepDirection.Clockwise;

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
            Canvas.SetLeft(bodyEllipse, leftPos - size / 2);
            Canvas.SetTop(bodyEllipse, topPos - size / 2);
        }

        private void DrawDebugLines(double starLeftPos, double starTopPos, double leftPos, double topPos, PlanetVM planet)
        {
            Line trueAnomoly = new Line();
            trueAnomoly.Stroke = Brushes.Magenta;
            trueAnomoly.X1 = starLeftPos;
            trueAnomoly.Y1 = starTopPos;
            trueAnomoly.X2 = leftPos;
            trueAnomoly.Y2 = topPos;
            trueAnomoly.StrokeThickness = 1;
            MapCanvas.Children.Add(trueAnomoly);


            Line periapsis = new Line();
            periapsis.Stroke = Brushes.Cyan;
            periapsis.X1 = starLeftPos;
            periapsis.Y1 = starTopPos;
            double arcRotAngle = planet.ArgumentOfPeriapsis + planet.LongitudeOfAscendingNode;

            periapsis.X2 = starLeftPos + Math.Sin(arcRotAngle) * zoom * planet.Periapsis;
            periapsis.Y2 = starTopPos + Math.Cos(arcRotAngle) * zoom * planet.Periapsis;
            periapsis.StrokeThickness = 1;
            MapCanvas.Children.Add(periapsis);

        }
    }
}
