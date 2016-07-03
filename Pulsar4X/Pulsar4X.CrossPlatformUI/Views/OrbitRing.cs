using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;
using Eto.Drawing;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pulsar4X.CrossPlatformUI.Views
{
    class OrbitRing : INotifyPropertyChanged
    {
        public PercentValue OrbitPercent { private get { return _orbitPercent; }
            set { _orbitPercent = value; OnPropertyChanged(nameof(SweepAngle)); } }
        private PercentValue _orbitPercent = new PercentValue();

        public byte Segments { private get { return _segments; } //TODO we could adjust the Segments and OrbitPercent by the size of the orbit and the zoom level to get a level of detail effect.
            set { _segments = value; UpdatePens(); OnPropertyChanged(nameof(SweepAngle)); }}
        private byte _segments = 255;

        public float StartArcAngle { get { return (float)(Math.Atan2(_bodyPositionDB.Y, _bodyPositionDB.X) * 180 / Math.PI); }}

        public float SweepAngle { get { return (360f * OrbitPercent.Percent) / Segments; } }

        private List<Pen> _segmentPens;// = new List<Pen>();

        public Color PenColor { get { return _penColor; }
            set { _penColor = value;  UpdatePens(); OnPropertyChanged();}}
        private Color _penColor;
  
        private float TopLeftX { get { return (float)_parentPositionDB.Position.X * 200.0f;}}//+ _width / 2; }}
        private float TopLeftY { get { return (float)_parentPositionDB.Position.Y * 200.0f; }}//+ _height / 2; }}
        private float _width;
        private float _height;
        private float _focalPoint;
        //this should be the angle from the orbital reference direction, to the Argument of Periapsis, as seen from above, this sets the angle for the ecentricity.
        //ie an elipse is created from a rectangle (position, width and height), then rotated so that the ecentricity is at the right angle. 
        private float _rotation; 
        private Camera2D _camera;

        private OrbitDB _orbitDB; 

        private PositionDB _parentPositionDB;

        private PositionDB _bodyPositionDB;

        public static int drawCount=0;

        private Entity myEntity;

        public OrbitRing(Entity entityWithOrbit, Camera2D camera)
        {
            _camera = camera;

            _orbitDB = entityWithOrbit.GetDataBlob<OrbitDB>();
            _parentPositionDB = _orbitDB.Parent.GetDataBlob<PositionDB>();
            _bodyPositionDB = entityWithOrbit.GetDataBlob<PositionDB>();

            _rotation = (float)(_orbitDB.LongitudeOfAscendingNode + _orbitDB.ArgumentOfPeriapsis*2); //This is the LoP + AoP.

            //Normalize for 0-360
            while (_rotation < 0.0f)
                _rotation = _rotation + 360.0f;

            while (_rotation > 360.0f)
                _rotation = _rotation - 360.0f;


            _width = 200 * (float)_orbitDB.SemiMajorAxis * 2 ; //Major Axis
            _height = 200 * (float)Math.Sqrt((_orbitDB.SemiMajorAxis * _orbitDB.SemiMajorAxis) * (1 - _orbitDB.Eccentricity * _orbitDB.Eccentricity)) * 2;
            _focalPoint = (float)_orbitDB.Eccentricity * _width /2;

            myEntity = entityWithOrbit;
        }

        private void UpdatePens()
        {
            List<Pen> newPens = new List<Pen>();
            for (int i = 0; i < Segments; i++)
            {                    
                Color penColor = new Color(_penColor.R, _penColor.G, _penColor.B, i / (float)Segments);
                newPens.Add(new Pen(penColor, 1));
            }
            _segmentPens = newPens;        
        }

        public void DrawMe(Graphics g)
        {
            g.SaveTransform();
            var rmatrix = Matrix.Create();

            //the distance between the top left of the bounding rectangle, and one of the elipse's focal points
            PointF focalOffset = new PointF(-_width / 2 - _focalPoint, -_height / 2);


            //get the offset from the camera, this is the distance from the top left of the viewport to the center of the viewport, accounting for zoom, pan etc.
            IMatrix cameraOffset = _camera.GetViewProjectionMatrix();

            //apply the camera offset
            g.MultiplyTransform(cameraOffset);

            //offset to the focal point
            g.TranslateTransform(focalOffset);
            //rotate

            PointF rotatePoint = new PointF(((_width / 2) + _focalPoint + TopLeftX), ((_height / 2) + TopLeftY));
            rmatrix.RotateAt(_rotation, rotatePoint);

            g.MultiplyTransform(rmatrix);

            /*RectangleF MyRect = new RectangleF(TopLeftX, TopLeftY, _width, _height);
            g.DrawRectangle(Colors.White, MyRect);*/


            //public float StartArcAngle { get { return (float)(Math.Atan2(_bodyPositionDB.Y, _bodyPositionDB.X) * 180 / Math.PI); } }

            //Ok so the arcs are lagging or advancing past the planet by a set amount, and this corrects that by adding or subtracting a value.
            //first StartArcAngle must be corrected for rotation, to normalize the actual angle the planet is at.
            float ActualAngle = StartArcAngle - _rotation;
            if (ActualAngle < 0)
            { 
                ActualAngle = ActualAngle + 360.0f;
            }

            //Second get the sin of that angle, since it will have the right sign and magnitude for the correction.
            float AngleAdd = (float)Math.Sin(ActualAngle * (Math.PI / 180.0f));
            //last figure out the length correction, it is related to eccentricity in some way, but the 53.5 is a close guess on my part. I'm not
            //sure which number should get plugged in there, but once that is found planet orbits should work. I got the 53.5 by getting mercury's proper
            //correction of 11, then working with eccentricity found that 53.5 * mercury's eccentricity is roughly the value I want. this is also correctish for
            //mars, earth and venus, so I'm close to whatever the right answer is.
            AngleAdd = AngleAdd  * (53.5f * (float)_orbitDB.Eccentricity);

            //draw the elipse (as a number of arcs each with a different pen, this gives the fading alpha channel effect) 
            int i = 0;
            foreach (var pen in _segmentPens)
            {
                g.DrawArc(pen, TopLeftX, TopLeftY, _width, _height, StartArcAngle - (AngleAdd) - _rotation + (i * SweepAngle), SweepAngle);
                i++;
            }
            g.RestoreTransform();

            Font lastFont = new Font(FontFamilies.MonospaceFamilyName, 10.0f);
            if(drawCount == 2)
            {
                g.SaveTransform();
                String Entry = String.Format("{0} {1} {2} {3} {4} {5}", _rotation, StartArcAngle, ActualAngle, AngleAdd, _bodyPositionDB.X, _bodyPositionDB.Y);
                g.DrawText(lastFont, Colors.White, 10, 10, Entry);

                Entry = String.Format("Values: {0} {1} {2} {3} {4} {5}", TopLeftX, TopLeftY, _width, _height,rotatePoint.X,rotatePoint.Y);
                g.DrawText(lastFont, Colors.White, 10, 30, Entry);


                g.RestoreTransform();
            }
            if (drawCount == 3)
            {
                g.SaveTransform();

                String Entry = String.Format("{0} {1} {2} {3} {4} {5}", _rotation, StartArcAngle, ActualAngle, AngleAdd, _bodyPositionDB.X, _bodyPositionDB.Y);
                g.DrawText(lastFont, Colors.White, 10, 50, Entry);

                Entry = String.Format("Values: {0} {1} {2} {3} {4} {5}", TopLeftX, TopLeftY, _width, _height, rotatePoint.X, rotatePoint.Y);
                g.DrawText(lastFont, Colors.White, 10, 70, Entry);


               g.RestoreTransform();
            }
            drawCount++;
            if (drawCount == 5)
                drawCount = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
