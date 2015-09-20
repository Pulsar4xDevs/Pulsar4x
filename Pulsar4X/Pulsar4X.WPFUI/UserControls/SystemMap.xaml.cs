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
        void planet_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            MapCanvas.UpdateLayout();
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
                    DrawPlanetMap(star.Position, planet.Position, planet.ArgumentOfPeriapsis, planet.LongitudeOfAscendingNode, planet.Apoapsis, planet.Periapsis);                   
                }
            } 
        }

        private void DrawPlanetMap(Vector4 parentPosition, Vector4 thisPosition, double argumentOfPeriapsis, double longitudeOfAscendingNode, double apoapsis, double periapsis)
        {
            double planetLeftPos = zoom * (parentPosition.X + thisPosition.X) + canvasCenterW;
            double planetTopPos = zoom * (parentPosition.Y + thisPosition.Y) + canvasCenterH;
            DrawBody(10, Brushes.DarkGreen, planetLeftPos, planetTopPos);

            DrawOrbit(planetLeftPos, planetTopPos, argumentOfPeriapsis, longitudeOfAscendingNode, apoapsis, periapsis);       
        }

        

        private void DrawOrbit( double leftPos, double topPos, double argumentOfPeriapsis, double longitudeOfAscendingNode, double apoapsis, double periapsis )
        {
            Point arcStart = new Point(leftPos, topPos);  
            Point arcEnd = new Point(leftPos + 1, topPos); 
           
            double arcRotAngle = argumentOfPeriapsis + longitudeOfAscendingNode;

            //wrong math:
            //double twoDimentionalPeriapsis = Math.Cos(Angle.ToRadians(planet.Inclination))  * planet.Periapsis;//adjust for inclination.



            Size arcSize = new Size(zoom * periapsis, zoom * apoapsis); 

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
    }
}
