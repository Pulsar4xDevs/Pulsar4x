using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;
using Eto.Drawing;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pulsar4X.CrossPlatformUI.Views
{
    class OrbitRing : INotifyPropertyChanged, IconBase
    {
        public float Scale { get; set; } = 1;

        public PercentValue OrbitPercent { private get { return _orbitPercent; }
            set { _orbitPercent = value; OnPropertyChanged(nameof(SweepAngle)); } }
        private PercentValue _orbitPercent = new PercentValue() {Percent = 1};

        public byte Segments { private get { return _segments; } //TODO we could adjust the Segments and OrbitPercent by the size of the orbit and the zoom level to get a level of detail effect.
            set { _segments = value; UpdatePens(); OnPropertyChanged(nameof(SweepAngle)); }}
        private byte _segments = 128;

        public float StartArcAngle { get { return (float)(Math.Atan2(_bodyPositionDB.Y, _bodyPositionDB.X) * 180 / Math.PI); }}

        public float SweepAngle { get { return (360f * OrbitPercent.Percent) / Segments; } }

        private List<Pen> _segmentPens = new List<Pen>();

        public Color PenColor { get { return _penColor; }
            set { _penColor = value;  UpdatePens(); OnPropertyChanged();}}
        private Color _penColor = Colors.Wheat;
  
        private float TopLeftX { get { return (float)_parentPositionDB.Position.X * _camera.ZoomLevel; }}//+ _width / 2; }}
        private float TopLeftY { get { return (float)_parentPositionDB.Position.Y * _camera.ZoomLevel; }}//+ _height / 2; }}
        private float _width;
        private float _height;
        private float _focalPoint;
        //this should be the angle from the orbital reference direction, to the Argument of Periapsis, as seen from above, this sets the angle for the ecentricity.
        //ie an elipse is created from a rectangle (position, width and height), then rotated so that the ecentricity is at the right angle. 
        private float _rotation; 
        private Camera2dv2 _camera;

        private OrbitDB _orbitDB; 

        private PositionDB _parentPositionDB;

        private PositionDB _bodyPositionDB;

        public static int drawCount=0;

        private Entity myEntity;

        public OrbitRing(Entity entityWithOrbit, Camera2dv2 camera)
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


            _width =  (float)_orbitDB.SemiMajorAxis * 2 ; //Major Axis
            _height = (float)Math.Sqrt((_orbitDB.SemiMajorAxis * _orbitDB.SemiMajorAxis) * (1 - _orbitDB.Eccentricity * _orbitDB.Eccentricity)) * 2;
            _focalPoint = (float)_orbitDB.Eccentricity * _width /2;

            myEntity = entityWithOrbit;
            UpdatePens();
        }

        private void UpdatePens()
        {
            List<Pen> newPens = new List<Pen>();
            for (int i = 0; i < Segments; i++)
            {                    
                Color penColor = new Color(_penColor.R, _penColor.G, _penColor.B, i / (float)Segments);
                newPens.Add(new Pen(penColor, 1.0f));
            }
            _segmentPens = newPens;        
        }

        public void DrawMe(Graphics g)
        {
            g.SaveTransform();
            var rmatrix = Matrix.Create();

            //the distance between the top left of the bounding rectangle, and one of the elipse's focal points
            float focalpoint = _focalPoint * _camera.ZoomLevel;
            float halfWid = _width * 0.5f * _camera.ZoomLevel;
            float halfHei = _height * 0.5f * _camera.ZoomLevel;
            //PointF focalOffset = new PointF(-_width / 2 - _focalPoint, -_height / 2);
            PointF focalOffset = new PointF(-halfWid - focalpoint, -halfHei);
            //offset to the focal point
            g.TranslateTransform(focalOffset);

            //get the offset from the camera, accounting for zoom, pan etc.
            IMatrix cameraOffset = _camera.GetViewProjectionMatrix(new PointF((float)TopLeftX, (float)TopLeftY));

            //apply the camera offset
            g.MultiplyTransform(cameraOffset);


            //rotate

            //The RotatePoint must consider the parent position, or else it will rotate any moon about the sun and not about the planet it orbits, hence TopLeftX,TopLeftY in this 
            //calculation.
            PointF rotatePoint = new PointF(((halfWid) + focalpoint + TopLeftX), ((halfHei) + TopLeftY));
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
                //float OriginalThickness = pen.Thickness;
                //pen.Thickness = pen.Thickness * (1.0f / _camera.ZoomLevel);
                g.DrawArc(pen, TopLeftX, TopLeftY, _width * _camera.ZoomLevel, _height * _camera.ZoomLevel, StartArcAngle - (AngleAdd) - _rotation + (i * SweepAngle), SweepAngle);
                i++;

                //pen.Thickness = OriginalThickness;
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
