using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel.SystemView
{
    public class VectorGraphicDataBase : ViewModelBase
    {

        public List<VectorPathPenPair> PathList { get; set; } = new List<VectorPathPenPair>();

        /// <summary>
        /// position from 0,0
        /// </summary>
        public float PosX
        {
            get { return _posx; }
            set { _posx = value; OnPropertyChanged(); }
        }
        private float _posx = 0;
        /// <summary>
        /// position from 0,0
        /// </summary>
        public float PosY
        {
            get { return _posy; }
            set { _posy = value; OnPropertyChanged(); }
        }
        private float _posy = 0;

        /// <summary>
        /// Size of the rectangle
        /// </summary>
        public float Width { get; set; }
        /// <summary>
        /// Height of the rectangle
        /// </summary>
        public float Height { get; set; }

        public float Rotation { get; set; }

        public float Zoom
        {
            get { return _zoom; }
            set { _zoom = value; OnPropertyChanged(); }
        }
        private float _zoom = 200;

        /// <summary>
        /// most icons wont change size with zoom, however things like the orbit lines will. 
        /// </summary>
        public bool SizeAffectedbyZoom { get; set; } = false;
    }


    /// <summary>
    /// generic vector graphics data for an icon
    /// </summary>
    public class IconData : VectorGraphicDataBase
    {
        private Entity _bodyEntity;
        private PositionDB PositionBlob { get { return _bodyEntity.GetDataBlob<PositionDB>(); } }

        private DateTime _currentDateTime;
        public DateTime CurrentDateTime
        {
            get { return _currentDateTime; }
            set { _currentDateTime = value; updatePosition(); }
        }

        private void updatePosition()
        {
            PosX = (float)PositionBlob.Position.X;
            PosY = (float)PositionBlob.Position.Y;
        }



        public IconData(Entity entity)
        {
            _bodyEntity = entity;
            if (entity.HasDataBlob<SystemBodyDB>())
                PlanetIcon(entity);
            else if (entity.HasDataBlob<StarInfoDB>())
                StarIcon(entity);

        }

        private void FleetIcon(Entity fleet)
        {
            PenData penData = new PenData();
            penData.Green = 255;
            Width = 6;
            Height = 6;
            updatePosition();

            VectorPathPenPair pathPair = new VectorPathPenPair(penData, new EllipseData(PosX, PosY, Width, Height));
            PathList.Add(pathPair);
        }

        private void StarIcon(Entity star)
        {
            PenData penData = new PenData();
            penData.Red = 100;
            penData.Green = 100;
            penData.Blue = 0;
            Width = 8;
            Height = 8;
            updatePosition();

            float hw = Width * 0.25f;
            float hh = Height * 0.25f;

            VectorPathPenPair pathPair = new VectorPathPenPair(penData, new RectangleData(PosX, PosY, Width, Height));
            pathPair.VectorShapes.Add(new BezierData(0, -Height, -Width, 0, -hw, -hh, -hw, -hh));
            pathPair.VectorShapes.Add(new BezierData(-Width, 0, 0, Height, -hw, hh, -hw, hh));
            pathPair.VectorShapes.Add(new BezierData(0, Height, Width, 0, hw, hh, hw, hh));
            pathPair.VectorShapes.Add(new BezierData(Width, 0, 0, -Height, hw, -hh, hw, -hh));
            PathList.Add(pathPair);
        }

        private void PlanetIcon(Entity planet)
        {
            SystemBodyDB sysBody = planet.GetDataBlob<SystemBodyDB>();

            switch (sysBody.Type)
            {
                case BodyType.Asteroid:
                    { }
                    break;
                case BodyType.Comet:
                    { }
                    break;
                case BodyType.DwarfPlanet:
                    { }
                    break;
                case BodyType.GasDwarf:
                    { }
                    break;
                case BodyType.GasGiant:
                    { }
                    break;
                case BodyType.IceGiant:
                    { }
                    break;
                case BodyType.Moon:
                    { }
                    break;
                case BodyType.Terrestrial:
                    {
                        PenData penData = new PenData();
                        penData.Green = 100;
                        penData.Blue = 200;
                        Width = 6;
                        Height = 6;
                        _bodyEntity = planet;
                        updatePosition();

                        VectorPathPenPair pathPair = new VectorPathPenPair(penData, new EllipseData(PosX, PosY, Width, Height));
                        PathList.Add(pathPair);
                    }
                    break;

                default:
                    {
                        //PenData penData = new PenData();
                        //penData.Green = 255;
                        //Width = 6;
                        //Height = 6;
                        //_bodyEntity = planet;
                        //updatePosition();

                        //VectorPathPenPair pathPair = new VectorPathPenPair(penData, new EllipseData(PosX, PosY, Width, Height));
                        //PathList.Add(pathPair);
                    }
                    break;
            }
        }
    }

    public class OrbitEllipseSimple : VectorGraphicDataBase
    {
        public byte Segments { get; set; } = 255;

        public OrbitDB OrbitDB { get; set; }
        private DateTime _currentDateTime;
        public DateTime CurrentDateTime
        {
            get { return _currentDateTime; }
            set { _currentDateTime = value;}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orbit"></param>
        public OrbitEllipseSimple(OrbitDB orbit)
        {
            SizeAffectedbyZoom = true;
            OrbitDB = orbit;

            Rotation = 0;
            //Width = 2;
            //Height = 2;
            PosX = 0;
            PosY = 0;

            if (orbit.Parent != null && orbit.Parent.HasDataBlob<PositionDB>())
            {
               PosX = (float)orbit.Parent.GetDataBlob<PositionDB>().X;
               PosY = (float)orbit.Parent.GetDataBlob<PositionDB>().Y;
            }

            // setup date time etc.
            DateTime currTime = DateTime.Now;
            DateTime EndTime = currTime + OrbitDB.OrbitalPeriod;
            TimeSpan stepTime = new TimeSpan((EndTime - currTime).Ticks / 365);
            EndTime -= stepTime; // to end the loop 1 early.

            // get inital positions on orbit
            var startPos = OrbitProcessor.GetPosition(orbit, currTime);
            currTime += stepTime;
            var currPos = OrbitProcessor.GetPosition(orbit, currTime);
            var prevPos = currPos;
            currTime += stepTime;

            // create first line segment.
            PenData pen = new PenData();
            pen.Red = 255;
            pen.Green = 248;
            pen.Blue = 220;
            pen.Thickness = 2.2f;
            LineData line = new LineData((float)startPos.X, (float)startPos.Y, (float)currPos.X, (float)currPos.Y);
            VectorPathPenPair pathPenPair = new VectorPathPenPair(pen, line);
            PathList.Add(pathPenPair);

            // create rest of the lin segments.
            for (; currTime < EndTime; currTime += stepTime)
            {
                currPos = OrbitProcessor.GetPosition(orbit, currTime);

                pen = new PenData();
                pen.Red = 255;
                pen.Green = 248;
                pen.Blue = 220;
                pen.Thickness = 2.2f;
                line = new LineData((float)prevPos.X, (float)prevPos.Y, (float)currPos.X, (float)currPos.Y);
                pathPenPair = new VectorPathPenPair(pen, line);
                PathList.Add(pathPenPair);

                prevPos = currPos;
            }

            // create last line segment, hoking up the ends.
            currPos = OrbitProcessor.GetPosition(orbit, EndTime);
            pen = new PenData();
            pen.Red = 255;
            pen.Green = 248;
            pen.Blue = 220;
            pen.Thickness = 2.2f;
            line = new LineData((float)prevPos.X, (float)prevPos.Y, (float)currPos.X, (float)currPos.Y);
            pathPenPair = new VectorPathPenPair(pen, line);
            PathList.Add(pathPenPair);
        }
    }

    /// <summary>
        /// generic data for drawing an OrbitEllipse which fades towards the tail
        /// </summary>
        public class OrbitEllipseFading : VectorGraphicDataBase
    {
        /// <summary>
        /// number of segments in the orbit, this is mostly for an increasing alpha chan.
        /// </summary>
        public byte Segments { get; set; } = 255;
        /// <summary>
        /// each of the arcs are stored here
        /// </summary>
        //public List<ArcData> ArcList { get; } = new List<ArcData>();
        /// <summary>
        /// This is the index that the body is currently at. (or maybe the next one..)
        /// </summary>
        public byte StartIndex { get; set; }

        public OrbitDB OrbitDB { get; set; }
        public PositionDB PositionDB { get; set; }
        private DateTime _currentDateTime;
        public DateTime CurrentDateTime
        {
            get { return _currentDateTime; }
            set { _currentDateTime = value; updatePosition(); updateAlphaFade(); }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="orbit"></param>
        public OrbitEllipseFading(OrbitDB orbit, PositionDB positionDB)
        {
            //TODO:May have to create a smaller arc for the first segment, and full alpha the segment the body is at.
            Rotation = (float)(orbit.LongitudeOfAscendingNode + orbit.ArgumentOfPeriapsis); //TODO adjust for 3d orbits. ie if the orbit has an Z axis, this is likely to be wrong. 
            Width = (float)orbit.SemiMajorAxis * 2; //Major Axis
            Height = (float)Math.Sqrt(((orbit.SemiMajorAxis * Math.Sqrt(1 - orbit.Eccentricity * orbit.Eccentricity)) * orbit.SemiMajorAxis * (1 - orbit.Eccentricity * orbit.Eccentricity))) * 2;   //minor Axis
            SizeAffectedbyZoom = true;
            OrbitDB = orbit;
            PositionDB = positionDB;
            if (orbit.Parent != null && orbit.Parent.HasDataBlob<PositionDB>())
            {
                PosX = (float)orbit.Parent.GetDataBlob<PositionDB>().X; //TODO: adjust so focal point of ellipse is at position. 
                PosY = (float)orbit.Parent.GetDataBlob<PositionDB>().Y;
            }
            float start = 0;
            float sweep = 360.0f / Segments;
            for (int i = 0; i < Segments; i++)
            {
                PenData pen = new PenData();
                pen.Red = 255;
                pen.Green = 248;
                pen.Blue = 220;
                ArcData arc = new ArcData(PosX, PosY, Width, Height, start, sweep);
                VectorPathPenPair pathPenPair = new VectorPathPenPair(pen, arc);
                PathList.Add(pathPenPair);
                start += sweep;
            }
            updateAlphaFade();
        }

        public void SetStartPos()
        {
            float angle = (float)(OrbitDB.LongitudeOfAscendingNode + OrbitDB.ArgumentOfPeriapsis + OrbitProcessor.GetTrueAnomaly(OrbitDB, _currentDateTime));
            float trueAnomaly = (float)OrbitProcessor.GetTrueAnomaly(OrbitDB, _currentDateTime);
            Vector4 position = OrbitProcessor.GetPosition(OrbitDB, CurrentDateTime);
            float angle2 = (float)(Math.Atan2(position.Y, position.X) * 180 / Math.PI);

            float degreesPerSegment = 360 / (Convert.ToSingle(Segments));
            StartIndex = (byte)(angle2 / degreesPerSegment);

        }

        private void updatePosition()
        {
            if (OrbitDB.Parent != null && OrbitDB.Parent.HasDataBlob<OrbitDB>())
            {
                PosX = (float)PositionDB.Position.X;
                PosY = (float)PositionDB.Position.Y;
            }
        }

        public void updateAlphaFade()
        {
            SetStartPos();
            byte i = 0;
            foreach (var item in PathList)
            {
                item.Pen.Alpha = (byte)(255 - StartIndex + i);
                i++;
            }
        }
    }


    /// <summary>
    /// a list of vector shapes with a pen
    /// </summary>
    public class VectorPathPenPair
    {
        public PenData Pen { get; set; } = new PenData();
        public List<VectorShapeBase> VectorShapes { get; set; } = new List<VectorShapeBase>();
        public VectorPathPenPair(VectorShapeBase shape)
        {
            VectorShapes.Add(shape);
        }

        public VectorPathPenPair(PenData pen, VectorShapeBase shape) : this(shape)
        { Pen = pen; }

        public VectorPathPenPair(PenData pen, List<VectorShapeBase> shapes)
        {
            Pen = pen;
            foreach (var shape in shapes)
            {
                VectorShapes.Add(shape);
            }
        }
    }

    /// <summary>
    /// base class for VectorShapes
    /// </summary>
    public class VectorShapeBase : ViewModelBase
    {
        /// <summary>
        /// position from 0,0
        /// </summary>
        public float X1
        {
            get { return _x1; }
            set { _x1 = value; }
        }
        private float _x1 = 0;
        /// <summary>
        /// position from 0,0
        /// </summary>
        public float Y1
        {
            get { return _y1; }
            set { _y1 = value; }
        }
        private float _y1 = 0;

        /// <summary>
        /// position from 0,0
        /// </summary>
        public float X2
        {
            get { return _x2; }
            set { _x2 = value; }
        }
        private float _x2 = 0;
        /// <summary>
        /// position from 0,0
        /// </summary>
        public float Y2
        {
            get { return _y2; }
            set { _y2 = value; }
        }
        private float _y2 = 0;

        public float CenterX { get { return _x1 + _x2 * 0.5f; } set { _x1 = value - _x2 * 0.5f; OnPropertyChanged(); } }

        public float CenterY { get { return _y1 + _y2 * 0.5f; } set { _y1 = value - _y2 * 0.5f; OnPropertyChanged(); } }

        protected VectorShapeBase()
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="centerPosition">if true, x1 and y1 inputs are the center position</param>
        protected VectorShapeBase(float x1, float y1, float x2, float y2, bool centerPosition = true)
        {
            X2 = x2;
            Y2 = y2;
            if (centerPosition)
            {
                CenterX = x1;
                CenterY = y1;
            }
            else
            {
                X1 = x1;
                Y1 = y1;
            }

        }

    }

    public class LineData : VectorShapeBase
    {
        public float XStart { get { return X1; } set { X1 = value; OnPropertyChanged(); } }
        public float YStart { get { return Y1; } set { Y1 = value; OnPropertyChanged(); } }

        public float XEnd { get { return X2; } set { X2 = value; OnPropertyChanged(); } }
        public float YEnd { get { return Y2; } set { Y2 = value; OnPropertyChanged(); } }

        public LineData() : base()
        { }

        public LineData(float xStart, float yStart, float xEnd, float yEnd, bool centerPosition = false) : base(xStart, yStart, xEnd, yEnd, centerPosition)
        { }
    }

    /// <summary>
    /// generic data for a rectangle
    /// </summary>
    public class RectangleData : VectorShapeBase
    {
        public float X { get { return X1; } set { X1 = value; OnPropertyChanged(); } }
        public float Y { get { return Y1; } set { Y1 = value; OnPropertyChanged(); } }

        public float Width { get { return X2; } set { X2 = value; OnPropertyChanged(); } }
        public float Height { get { return Y2; } set { Y2 = value; OnPropertyChanged(); } }


        public RectangleData() : base()
        { }

        public RectangleData(float x, float y, float width, float height, bool centerPosition = true) : base(x, y, width, height, centerPosition)
        { }

    }

    /// <summary>
    /// generic data for an Ellipse
    /// </summary>
    public class EllipseData : RectangleData
    {
        public EllipseData() : base()
        { }

        public EllipseData(float x, float y, float width, float height, bool centerPosition = true) : base(x, y, width, height, centerPosition)
        { }
    }

    /// <summary>
    /// generic data for an arc segment
    /// </summary>
    public class ArcData : RectangleData
    {

        public float StartAngle
        {
            get { return _startAngle; }
            set { _startAngle = value; OnPropertyChanged(); }
        }
        private float _startAngle;
        public float SweepAngle
        {
            get { return _sweepAngle; }
            set { _sweepAngle = value; OnPropertyChanged(); }
        }
        private float _sweepAngle;


        public ArcData(float x, float y, float width, float height, float start, float sweep, bool centerPosition = true) : base(x, y, width, height, centerPosition)
        {
            StartAngle = start;
            SweepAngle = sweep;
        }
    }

    public class BezierData : VectorShapeBase
    {
        public float ControlX1 { get; set; }
        public float ControlY1 { get; set; }
        public float ControlX2 { get; set; }
        public float ControlY2 { get; set; }

        public BezierData(float startX, float startY, float endX, float endY, float controlX1, float controlY1, float controlX2, float controlY2, bool centerPosition = false) : base(startX, startY, endX, endY, centerPosition)
        {
            ControlX1 = controlX1;
            ControlY1 = controlY1;
            ControlX2 = controlX2;
            ControlY2 = controlY2;
        }
    }

    public class TextData : VectorShapeBase
    {
        public System.Drawing.Font Font { get; set; } = new System.Drawing.Font(new System.Drawing.FontFamily(System.Drawing.Text.GenericFontFamilies.SansSerif), 8);
        public System.Drawing.Color Color { get; set; } = new System.Drawing.Color();

        public string Text { get; set; }

        public TextData(string text, float x, float y, float size) : base(x, y, 0, size )
        {
            Text = text;
            
        }
    }

    /// <summary>
    /// generic data for a graphics Pen. 
    /// </summary>
    public class PenData
    {
        int ColorARGB { get; set; }
        public byte Alpha { get; set; } = 255;
        public byte Red { get; set; } = 0;
        public byte Green { get; set; } = 0;
        public byte Blue { get; set; } = 0;
        public float Thickness { get; set; } = 1f;
    }
}
