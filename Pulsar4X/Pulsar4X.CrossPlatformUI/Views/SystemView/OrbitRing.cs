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
        private PercentValue _orbitPercent = new PercentValue() {Percent = 0.75f};

        public byte Segments { private get { return _segments; } //TODO we could adjust the Segments and OrbitPercent by the size of the orbit and the zoom level to get a level of detail effect.
            set { _segments = value; UpdatePens(); OnPropertyChanged(nameof(SweepAngle)); }}
        private byte _segments = 64;

        public float SweepAngle { get { return (360f * OrbitPercent.Percent) / Segments; } }

        private List<Pen> _segmentPens = new List<Pen>();

        public Color PenColor { get { return _penColor; }
            set { _penColor = value;  UpdatePens(); OnPropertyChanged();}}
        private Color _penColor = Colors.Wheat;

        private float _orbitElipseWidth;
        private float _orbitElipseHeight;
        private double _focalDistance; //the distance between an orbits focal point to the center
        private Vector4 _focalOffsetPoint; //the focal point ralitive to the orbit. 
        private Vector4 _ecentricOffsetPoint; //because an angle on an ellipse is ralitive to the elipse. 
        //this should be the angle from the orbital reference direction, to the Argument of Periapsis, as seen from above, this sets the angle for the ecentricity.
        //ie an elipse is created from a rectangle (position, width and height), then rotated so that the ecentricity is at the right angle. 
        private float _orbitAngle;
        private double _radianAngle;
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

            _orbitAngle = (float)(_orbitDB.LongitudeOfAscendingNode + _orbitDB.ArgumentOfPeriapsis*2); //This is the LoP + AoP.
           
            //Normalize for 0-360
            _orbitAngle = _orbitAngle % 360;
            if (_orbitAngle < 0)
                _orbitAngle += 360;

            _radianAngle = _orbitAngle * Math.PI / 180;

            _orbitElipseWidth =  (float)_orbitDB.SemiMajorAxis * 2 ; //Major Axis
            _orbitElipseHeight = (float)Math.Sqrt((_orbitDB.SemiMajorAxis * _orbitDB.SemiMajorAxis) * (1 - _orbitDB.Eccentricity * _orbitDB.Eccentricity)) * 2;
            _focalDistance = _orbitDB.Eccentricity * _orbitElipseWidth * 0.5f;
            //_focalDistance = Math.Sqrt((_orbitElipseHeight * _orbitElipseHeight * 0.5) - (_orbitElipseWidth * _orbitElipseWidth * 0.5));
            //since the _focalPoint is only an X component we don't bother calculating the Y part of the matrix
            double focalX = (_focalDistance * Math.Cos(_orbitAngle * Math.PI / 180));//  - (0 * Math.Sin(_orbitAngle * Math.PI / 180));
            double focalY = (_focalDistance * Math.Sin(_orbitAngle * Math.PI / 180));// + (0 * Math.Cos(_orbitAngle * Math.PI / 180));
            _focalOffsetPoint = new Vector4(focalX, focalY, 0, 0);

            double eccentX = 0 - (_orbitElipseWidth / _orbitElipseHeight * Math.Sin(_orbitAngle * Math.PI / 180));
            double eccentY = 0 + (_orbitElipseWidth / _orbitElipseHeight * Math.Cos(_orbitAngle * Math.PI / 180)); 
            
            _ecentricOffsetPoint = new Vector4(eccentX, eccentY, 0, 0);

            myEntity = entityWithOrbit;
            UpdatePens();
        }

        private void UpdatePens()
        {
            List<Pen> newPens = new List<Pen>();
            for (int i = Segments; i > 0; i--)
            {                    
                Color penColor = new Color(_penColor.R, _penColor.G, _penColor.B, i / (float)Segments);
                Pen newpen = new Pen(penColor, 1.0f);
                //newpen.LineJoin = PenLineJoin.Bevel;
                newpen.LineCap = PenLineCap.Butt;
                newPens.Add(newpen);
            }
            _segmentPens = newPens;        
        }



        private float GetStartArcAngle()
        {

            Vector4 pos = _bodyPositionDB.AbsolutePosition - _parentPositionDB.AbsolutePosition; //adjust so moons get the right positions    
            //do a rotational matrix so the normalised position is ralitive to the ellipse.       
            double normalX = (pos.X * Math.Cos(-_radianAngle)) - (pos.Y * Math.Sin(-_radianAngle));
            double normalY = (pos.X * Math.Sin(-_radianAngle)) + (pos.Y * Math.Cos(-_radianAngle));
            normalX += _focalDistance; //adjust for focal point
            normalY *= (_orbitElipseWidth / _orbitElipseHeight); //adjust for elliptic angle. 

            return (float)(Math.Atan2(normalY, normalX) * 180 / Math.PI);
        }

        public void DrawMe(Graphics g)
        {

            PointF boundingBoxTopLeft = new PointF((float)_parentPositionDB.AbsolutePosition.X * _camera.ZoomLevel, (float)_parentPositionDB.AbsolutePosition.Y * _camera.ZoomLevel);
            PointF bodyPos = new PointF((float)_bodyPositionDB.AbsolutePosition.X * _camera.ZoomLevel, (float)_bodyPositionDB.AbsolutePosition.Y * _camera.ZoomLevel);
            SizeF elipseSize = new SizeF(_orbitElipseWidth * _camera.ZoomLevel, _orbitElipseHeight * _camera.ZoomLevel);
            RectangleF elipseBoundingBox = new RectangleF(boundingBoxTopLeft, elipseSize);
            g.SaveTransform();
            var rmatrix = Matrix.Create();
            //the distance between the center of the bounding rectangle, and one of the elipse's focal points
            float focalpoint = (float)_focalDistance * _camera.ZoomLevel;
            float halfWid = _orbitElipseWidth * 0.5f * _camera.ZoomLevel;
            float halfHei = _orbitElipseHeight * 0.5f * _camera.ZoomLevel;

            PointF focalOffset = new PointF(-halfWid - focalpoint, -halfHei) ;

            //get the offset from the camera, accounting for zoom, pan etc.
            IMatrix cameraOffset = _camera.GetViewProjectionMatrix(new PointF(0,0));
            //apply the camera offset
            g.MultiplyTransform(cameraOffset);

            //debug line, draws from the parent body to the body. 
            //if (myEntity.GetDataBlob<NameDB>().DefaultName == "Luna" || myEntity.GetDataBlob<NameDB>().DefaultName == "Earth")
            //    g.DrawLine(Colors.DeepPink, boundingBoxTopLeft, bodyPos); 

            float startArcAngle = GetStartArcAngle();

            g.TranslateTransform(focalOffset);


            // this point is from the frame of reference of the elipse.
            PointF rotatePoint = new PointF(halfWid + focalpoint, halfHei) + boundingBoxTopLeft;
            rmatrix.RotateAt(_orbitAngle, rotatePoint);           
            g.MultiplyTransform(rmatrix);

            //if (myEntity.GetDataBlob<NameDB>().DefaultName == "Mercury")
            //    g.DrawLine(Colors.DeepPink, (boundingBoxTopLeft + elipseSize) / 2, focalOffset - bodyPos );


            //debug rectangle, draws the bounding box for the rotated elipse
            //if (myEntity.GetDataBlob<NameDB>().DefaultName == "Mercury" || myEntity.GetDataBlob<NameDB>().DefaultName == "Earth")
            //    g.DrawRectangle(Colors.BlueViolet, elipseBoundingBox);



            //draw the elipse (as a number of arcs each with a different pen, this gives the fading alpha channel effect) 
            int i = 0;
                        
            foreach (var pen in _segmentPens)
            {
                g.DrawArc(pen, elipseBoundingBox, startArcAngle - (i * SweepAngle), -SweepAngle);
                i++;
            }            
            g.RestoreTransform();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
