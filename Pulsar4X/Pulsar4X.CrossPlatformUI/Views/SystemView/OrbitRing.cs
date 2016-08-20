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

        //public float StartArcAngle { get { return (float)(Math.Atan2(_bodyPositionDB.Y, _bodyPositionDB.X) * 180 / Math.PI); }}

        public float SweepAngle { get { return (360f * OrbitPercent.Percent) / Segments; } }

        private List<Pen> _segmentPens = new List<Pen>();

        public Color PenColor { get { return _penColor; }
            set { _penColor = value;  UpdatePens(); OnPropertyChanged();}}
        private Color _penColor = Colors.Wheat;
  
        //private float TopLeftX { get { return (float)_parentPositionDB.AbsolutePosition.X * _camera.ZoomLevel; }}//+ _width / 2; }}
        //private float TopLeftY { get { return (float)_parentPositionDB.AbsolutePosition.Y * _camera.ZoomLevel; }}//+ _height / 2; }}

        private float _orbitElipseWidth;
        private float _orbitElipseHeight;
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
            _rotation = _rotation % 360;
            if (_rotation < 0)
                _rotation += 360;


            _orbitElipseWidth =  (float)_orbitDB.SemiMajorAxis * 2 ; //Major Axis
            _orbitElipseHeight = (float)Math.Sqrt((_orbitDB.SemiMajorAxis * _orbitDB.SemiMajorAxis) * (1 - _orbitDB.Eccentricity * _orbitDB.Eccentricity)) * 2;
            _focalPoint = (float)_orbitDB.Eccentricity * _orbitElipseWidth /2;

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

        private float GetStartArcAngle()
        {
            //since teh _focalPoint is only an X component we dont' bother calculating the Y part of the matrix
            double focalX = (_focalPoint * Math.Cos(_rotation * Math.PI / 180)); // - 0 * sin(rotation) 
            double focalY = (_focalPoint * Math.Sin(_rotation * Math.PI / 180)); // + 0 * cos(rotation)
            //addt the body posistion
            Vector4 offsetPoint = new Vector4(focalX, focalY, 0, 0) + _bodyPositionDB.RelativePosition;
            //find the angle to the offset point
            float angle = (float)(Math.Atan2(offsetPoint.Y, offsetPoint.X) * 180 / Math.PI);
            //subtract the _rotation, since this angle needs to be ralitive to the elipse, and the elipse gets _rotated
            angle -= _rotation;
            //and finaly, normalise it useing modulo arrithmatic.
            angle = angle % 360;
            if (angle < 0)
                angle += 360;
            return angle;
        }

        public void DrawMe(Graphics g)
        {

            PointF boundingBoxTopLeft = new PointF((float)_parentPositionDB.AbsolutePosition.X * _camera.ZoomLevel, (float)_parentPositionDB.AbsolutePosition.Y * _camera.ZoomLevel);
            PointF bodyPos = new PointF((float)_bodyPositionDB.RelativePosition.X * _camera.ZoomLevel, (float)_bodyPositionDB.RelativePosition.Y * _camera.ZoomLevel);
            SizeF elipseSize = new SizeF(_orbitElipseWidth * _camera.ZoomLevel, _orbitElipseHeight * _camera.ZoomLevel);
            RectangleF elipseBoundingBox = new RectangleF(boundingBoxTopLeft, elipseSize);
            g.SaveTransform();
            var rmatrix = Matrix.Create();
            //the distance between the top left of the bounding rectangle, and one of the elipse's focal points
            float focalpoint = _focalPoint * _camera.ZoomLevel;
            float halfWid = _orbitElipseWidth * 0.5f * _camera.ZoomLevel;
            float halfHei = _orbitElipseHeight * 0.5f * _camera.ZoomLevel;

            PointF focalOffset = new PointF(-halfWid - focalpoint, -halfHei);

            //get the offset from the camera, accounting for zoom, pan etc.
            IMatrix cameraOffset = _camera.GetViewProjectionMatrix(boundingBoxTopLeft);
            //apply the camera offset
            g.MultiplyTransform(cameraOffset);

            //g.DrawLine(Colors.DeepPink, 0, 0, bodyPos.X, bodyPos.Y);

            float startArcAngle = GetStartArcAngle();

            int SweepCorrection = -1;
            float lowestDistance = 0;
            int lowestSegment = -1;
            float lowestX = 0;
            float lowestY = 0;

            for (int angle = 0; angle < _segments; angle++)//my segment and the ones drawn are different in number I think.
            {
                float myAngle = (angle * SweepAngle) * ((float)Math.PI / 180.0f);
                /// <summary>
                /// HalfWid and halfHei are the ellipse dimensions, x must have the focal point subtracted, and this number * the Cosine or Sine of the angle is
                /// where x or y is respectively.
                /// </summary>
                float x = halfWid * (float)Math.Cos(myAngle) - focalpoint;
                float y = halfHei * (float)Math.Sin(myAngle);

                var myMatrix = Matrix.Create();
                PointF rP = new Point(0, 0);
                myMatrix.RotateAt(_rotation, rP);
                PointF nP = new PointF(x, y);
                PointF myPoint = myMatrix.TransformPoint(nP);

                float pX = (float)(_bodyPositionDB.AbsolutePosition.X * _camera.ZoomLevel);
                float pY = (float)(_bodyPositionDB.AbsolutePosition.Y * _camera.ZoomLevel);
                float xDistance = (myPoint.X - pX);
                float yDistance = (myPoint.Y - pY);

                float distSq = (xDistance * xDistance) + (yDistance * yDistance);

                if (lowestSegment == -1 || lowestDistance > distSq)
                {
                    lowestSegment = angle;
                    lowestDistance = distSq;
                    lowestX = myPoint.X;
                    lowestY = myPoint.Y;
                }

                /*g.DrawLine(Colors.Yellow, nP, nP);
                g.DrawLine(Colors.Green, myPoint, myPoint);

                if(angle == 0)
                {
                    PointF lp1 = new PointF(myPoint.X-10, myPoint.Y-10);
                    PointF lp2 = new PointF(myPoint.X + 10, myPoint.Y + 10);

                    g.DrawLine(Colors.Black, lp1, lp2);

                    PointF p1 = new PointF(nP.X - 10, nP.Y - 10);
                    PointF p2 = new PointF(nP.X + 10, nP.Y + 10);

                    g.DrawLine(Colors.Black, p1, p2);
                }
                if (angle == 1)
                {
                    PointF lp1 = new PointF(myPoint.X - 10, myPoint.Y - 10);
                    PointF lp2 = new PointF(myPoint.X + 10, myPoint.Y + 10);

                    g.DrawLine(Colors.White, lp1, lp2);

                    PointF p1 = new PointF(nP.X - 10, nP.Y - 10);
                    PointF p2 = new PointF(nP.X + 10, nP.Y + 10);

                    g.DrawLine(Colors.White, p1, p2);
                }
                if (angle == 2)
                {
                    PointF lp1 = new PointF(myPoint.X - 10, myPoint.Y - 10);
                    PointF lp2 = new PointF(myPoint.X + 10, myPoint.Y + 10);

                    g.DrawLine(Colors.Green, lp1, lp2);

                    PointF p1 = new PointF(nP.X - 10, nP.Y - 10);
                    PointF p2 = new PointF(nP.X + 10, nP.Y + 10);

                    g.DrawLine(Colors.Green, p1, p2);
                }
                if (angle == 3)
                {
                    PointF lp1 = new PointF(myPoint.X - 10, myPoint.Y - 10);
                    PointF lp2 = new PointF(myPoint.X + 10, myPoint.Y + 10);

                    g.DrawLine(Colors.Yellow, lp1, lp2);

                    PointF p1 = new PointF(nP.X - 10, nP.Y - 10);
                    PointF p2 = new PointF(nP.X + 10, nP.Y + 10);

                    g.DrawLine(Colors.Yellow, p1, p2);
                }*/
            }

            /*PointF lowP = new PointF(lowestX-10, lowestY-10);
            PointF lowP2 = new PointF(lowestX + 10, lowestY + 10);
            g.DrawLine(Colors.Red, lowP, lowP2);

            String Entry = String.Format("{0} {1} {2} {3} {4} {5}", lowestDistance, lowestSegment, lowestX, lowestY, (_bodyPositionDB.AbsolutePosition.X * _camera.ZoomLevel),
                                                                    (_bodyPositionDB.AbsolutePosition.Y * _camera.ZoomLevel));
            PointF tDraw = new PointF(10,10);
            Font NF = new Font(FontFamilies.Monospace, 10.0f);
            g.DrawText(NF, Colors.White, tDraw, Entry);*/
            SweepCorrection = lowestSegment;

            g.TranslateTransform(focalOffset);

            // this point is from the frame of reference of the elipse.
            PointF rotatePoint = new PointF(halfWid + focalpoint, halfHei);
            rmatrix.RotateAt(_rotation, rotatePoint);
            
            g.MultiplyTransform(rmatrix);

            //g.DrawRectangle(Colors.BlueViolet, elipseBoundingBox);
            //draw the elipse (as a number of arcs each with a different pen, this gives the fading alpha channel effect) 
            int i = 0;
                        
            foreach (var pen in _segmentPens)
            {
                if(SweepCorrection != -1)
                    g.DrawArc(pen, elipseBoundingBox, ((i+SweepCorrection) * SweepAngle), SweepAngle);
                else
                    g.DrawArc(pen, elipseBoundingBox, startArcAngle + (i * SweepAngle), SweepAngle);
                i++;
            }            
            g.RestoreTransform();

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
