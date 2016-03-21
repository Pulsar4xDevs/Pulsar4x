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
            get { return posx; }
            set { posx = value; OnPropertyChanged(); }
        }
        private float posx;
        /// <summary>
        /// position from 0,0
        /// </summary>
        public float PosY
        {
            get { return posy; }
            set { posy = value; OnPropertyChanged(); }
        }
        private float posy;

        /// <summary>
        /// Size of the rectangle
        /// </summary>
        public float Width { get; protected set; }
        /// <summary>
        /// Height of the rectangle
        /// </summary>
        public float Height { get; protected set; }

        public float Rotation { get; set; }

        public float Zoom
        {
            get { return _zoom; }
            set { _zoom = value; OnPropertyChanged(); }
        }
        private float _zoom = 100;

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
            //TODO positionDB is not working. 
            Vector4 position = OrbitProcessor.GetPosition(_bodyEntity.GetDataBlob<OrbitDB>(), CurrentDateTime);
            PosX = (float)position.X;//(float)PositionBlob.Position.X;
            PosY = (float)position.Y;//(float)PositionBlob.Position.Y;

        }

        public IconData(Entity bodyEntity)
        {
            PenData penData = new PenData();
            penData.Green = 255;
            Width = 6;
            Height = 6;
            _bodyEntity = bodyEntity;
            updatePosition();

            VectorPathPenPair pathPair = new VectorPathPenPair(penData, new EllipseData(PosX, PosY, Width, Height));
            PathList.Add(pathPair);
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

        public OrbitDB Orbit { get; set; }

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
        public OrbitEllipseFading(OrbitDB orbit)
        {
            //TODO:May have to create a smaller arc for the first segment, and full alpha the segment the body is at.
            Rotation = (float)(orbit.LongitudeOfAscendingNode + orbit.ArgumentOfPeriapsis);
            Width = (float)orbit.Apoapsis * 2; //TODO this could break if the orbit size is bigger than a float
            Height = (float)orbit.Periapsis * 2;
            SizeAffectedbyZoom = true;
            Orbit = orbit;
            if (orbit.Parent != null && orbit.Parent.HasDataBlob<PositionDB>())
            {
                PosX = (float)orbit.Parent.GetDataBlob<PositionDB>().X;
                PosY = (float)orbit.Parent.GetDataBlob<PositionDB>().Y;
            }
            //float x = PosX + (float)orbit.Periapsis;
            //float y = PosY + (float)orbit.Apoapsis;
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
            float angle = (float)(Orbit.LongitudeOfAscendingNode + Orbit.ArgumentOfPeriapsis + OrbitProcessor.GetTrueAnomaly(Orbit, _currentDateTime));
            float degreesPerSegment = 360 / (Convert.ToSingle(Segments));
            StartIndex = (byte)(degreesPerSegment * angle);

        }

        private void updatePosition()
        {
            //TODO positionDB is not working. 
            if (Orbit.Parent != null && Orbit.Parent.HasDataBlob<OrbitDB>())
            {
                Vector4 position = OrbitProcessor.GetPosition(Orbit.Parent.GetDataBlob<OrbitDB>(), CurrentDateTime);
                PosX = (float)position.X;//(float)PositionBlob.Position.X;
                PosY = (float)position.Y;//(float)PositionBlob.Position.Y;
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

        VectorPathPenPair(PenData pen, List<VectorShapeBase> shapes)
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
