using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;


namespace Pulsar4X.ViewModel.SystemView
{
    public class SystemMap_DrawableVM : ViewModelBase
    {

        public List<SystemObjectGraphicsInfo> SystemBodies { get; set; } = new List<SystemObjectGraphicsInfo>();

        public Camera camera;
        public List<float> scale_data;

        public SystemMap_DrawableVM(StarSystem starSys, AuthenticationToken authToken, List<float> scale_data, Camera camera)
        {
            this.scale_data = scale_data;
            this.camera = camera;
            foreach (var item in starSys.SystemManager.GetAllEntitiesWithDataBlob<StarInfoDB>(authToken))
            {
                SystemBodies.Add(new SystemObjectGraphicsInfo(item));
            }
            foreach (var item in starSys.SystemManager.GetAllEntitiesWithDataBlob<SystemBodyDB>(authToken))
            {
                SystemBodies.Add(new SystemObjectGraphicsInfo(item));
            }
            OnPropertyChanged();
        }
        
    }

    public class SystemObjectGraphicsInfo
    {
        private Entity item;
        public IconData Icon { get; set; }
        public OrbitEllipseFading OrbitEllipse { get; set; }
        public SystemObjectGraphicsInfo(Entity item)
        {
            Icon = new IconData(item.GetDataBlob<PositionDB>());
            if(item.HasDataBlob<OrbitDB>())
                OrbitEllipse = new OrbitEllipseFading(item.GetDataBlob<OrbitDB>());
        }
    }

    /// <summary>
    /// generic vector graphics data for an icon
    /// TODO: expand this to be a vector graphics path. 
    /// </summary>
    public class IconData : ViewModelBase
    {
        public PenData Pendata { get; set; }
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
        public float Width { get; private set; }
        /// <summary>
        /// Height of the rectangle
        /// </summary>
        public float Height { get; private set; }

        public IconData(PositionDB position)
        {
            Pendata = new PenData();
            Pendata.Green = 255;
            Width = 6;
            Height = 6;
            PosX = (float)position.Position.X;
            PosY = (float)position.Position.Y;
        }
    }

    /// <summary>
    /// generic data for drawing an OrbitEllipse which fades towards the tail
    /// </summary>
    public class OrbitEllipseFading : ViewModelBase
    {
        /// <summary>
        /// number of segments in the orbit, this is mostly for an increasing alpha chan.
        /// </summary>
        public byte Segments { get; set; } = 255;
        /// <summary>
        /// each of the arcs are stored here
        /// </summary>
        public List<ArcData> ArcList { get; } = new List<ArcData>();
        /// <summary>
        /// This is the index that the body is currently at. (or maybe the next one..)
        /// </summary>
        public byte StartIndex { get; set; }
        /// <summary>
        /// position from 0,0
        /// </summary>
        public float PosX {
            get { return posx; }
            set { posx = value; OnPropertyChanged(); } }
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
        public float Width { get; private set; }
        /// <summary>
        /// Height of the rectangle
        /// </summary>
        public float Height { get; private set; }

        /// <summary>
        /// rotation of the rectangle
        /// </summary>
        public float AngleOfPeriapsis { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orbit"></param>
        public OrbitEllipseFading(OrbitDB orbit)
        {
            //TODO:May have to create a smaller arc for the first segment, and full alpha the segment the body is at.
            AngleOfPeriapsis = (float)orbit.ArgumentOfPeriapsis;
            Width = (float)orbit.Periapsis * 2; //TODO this could break if the orbit size is bigger than a float
            Height = (float)orbit.Apoapsis * 2;
            if (orbit.Parent != null && orbit.Parent.HasDataBlob<PositionDB>())
            {
                PosX = (float)orbit.Parent.GetDataBlob<PositionDB>().X;
                PosY = (float)orbit.Parent.GetDataBlob<PositionDB>().Y;
            }
            float x = PosX + (float)orbit.Periapsis;
            float y = PosY + (float)orbit.Apoapsis;
            float start = 0;
            for (int i = 0; i < Segments; i++)
            {
                PenData pen = new PenData();
                pen.Red = 0;
                pen.Green = 0;
                pen.Blue = 255;
                ArcData arc = new ArcData(pen, x, y, Width, Height, start, 360 / Segments);
                ArcList.Add(arc);
            }
        }

        public void updateAlphaFade()
        {
            byte i = 0;
            foreach (var item in ArcList)
            {
                item.Pendata.Alpha = (byte)(255 - StartIndex + i);
                i++; 
            }
        }
    }

    /// <summary>
    /// generic data for an arc segment
    /// </summary>
    public class ArcData
    {
        public PenData Pendata { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float StartAngle { get; set; }
        public float SweepAngle { get; set; }
        public ArcData(PenData pen, float x, float y, float width, float height, float start, float sweep)
        {
            Pendata = pen;
            PosX = x;
            PosY = y;
            Width = width;
            Height = height;
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
        public byte Alpha { get; set; } = 0;
        public byte Red { get; set; } = 0;
        public byte Green { get; set; } = 0;
        public byte Blue { get; set; } = 0;
        public float Thickness { get; set; } = 1f;
    }
}
