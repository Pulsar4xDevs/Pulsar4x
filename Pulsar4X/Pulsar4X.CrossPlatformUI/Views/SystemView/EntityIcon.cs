using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using Eto.Drawing;
using Eto.Forms;

namespace Pulsar4X.CrossPlatformUI.Views
{
    internal class EntityIcon : IconBase
    {
        public float Scale { get; set; } = 1;
        List<PenPathPair> _shapes = new List<PenPathPair>();
        private Entity _entity;
        private float Zoom { get { return _camera.ZoomLevel; } }
        private Size ViewSize { get { return _camera._viewPort.Size; } }
        private PositionDB _starSysPosition; 
        private Camera2dv2 _camera;

        private double _radius = 0;
        private double _minRad = 4;


        public EntityIcon(Entity entity, Camera2dv2 camera)
        {
            _entity = entity;
            _camera = camera;
            _starSysPosition = entity.GetDataBlob<PositionDB>();
            foreach (var item in entity.DataBlobs)
            {
                if (item is MassVolumeDB)
                {
                    HasMassVol((MassVolumeDB)item);
                }
                if (item is PropulsionDB)
                {
                    HasPropulsionDB((PropulsionDB)item);
                }
                if (item is PositionDB)
                {
                    HasPosition((PositionDB)item);
                }
                if (item is StarInfoDB)
                {
                    HasStarInfo((StarInfoDB)item);
                }
                if (item is SystemBodyDB)
                {
                    HasSysBodyInfo((SystemBodyDB)item);
                }
            }
        }

        void HasMassVol(MassVolumeDB db)
        {
            _radius = db.Radius;
            
        }

        void HasStarInfo(StarInfoDB db)
        {
            //TODO: change pen colour depending on star temp and lum?
            double temp = db.Temperature;
            double lum = db.Luminosity;

            Pen starPen = new Pen(Colors.DarkOrange);

            float width = 8;
            float height = 8;
            float hw = width * 0.25f;
            float hh = height * 0.25f;

            GraphicsPath starPath = new GraphicsPath();

            PointF start = new PointF(0, -height);
            PointF end = new PointF(-width, 0);
            PointF c1 = new PointF(-hw, -hh);
            PointF c2 = new PointF(-hw, -hh);
            starPath.AddBezier(start, c1, c2, end);

            start = new PointF(-width, 0);
            end = new PointF(0, height);
            c1 = new PointF(-hw, hh);
            c2 = new PointF(-hw, hh);
            starPath.AddBezier(start, c1, c2, end);

            start = new PointF(0, height);
            end = new PointF(width, 0);
            c1 = new PointF(hw, hh);
            c2 = new PointF(hw, hh);
            starPath.AddBezier(start, c1, c2, end);

            start = new PointF(width, 0);
            end = new PointF(0, -height);
            c1 = new PointF(hw, -hh);
            c2 = new PointF(hw, -hh);
            starPath.AddBezier(start, c1, c2, end);

            PenPathPair starPathPair = new PenPathPair() { Pen = starPen, Path = starPath };
            _shapes.Add(starPathPair);
        }



        void HasSysBodyInfo(SystemBodyDB db)
        {
            BodyType type = db.Type;
            float temp = db.BaseTemperature;
            
        }

        void HasAtmo(AtmosphereDB db)
        {
            short hydro = db.HydrosphereExtent;
            float albedo = db.Albedo;
        }

        void HasPosition(PositionDB db)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(-2, -2, 4, 4);
            PenPathPair circle = new PenPathPair() { Pen = new Pen(Colors.Blue), Path = path };
            _shapes.Add(circle);
        }

        void HasPropulsionDB(PropulsionDB db)
        {

            int maxFuel = 100;//db.FuelStorageCapicity / 20;

            int maxSpeed = db.MaximumSpeed / 10;
            int totalEP = db.TotalEnginePower / 25;
            PointF currentSpeed = new PointF((float)db.CurrentSpeed.X, (float)db.CurrentSpeed.Y);
            float currentSpeedLen = currentSpeed.Length / 10;

            Pen tankPen = new Pen(Colors.Aquamarine);
            GraphicsPath tankPath = new GraphicsPath();
            tankPath.AddEllipse(-maxFuel * 0.5f, 0, maxFuel, maxFuel);
            PenPathPair fueltank = new PenPathPair() { Pen = tankPen, Path = tankPath };
            _shapes.Add(fueltank);

            Pen enginePen = new Pen(Colors.DarkGray);
            GraphicsPath enginePath = new GraphicsPath();
            enginePath.AddRectangle(-totalEP * 0.5f, maxFuel, totalEP, maxSpeed);
            PenPathPair engine = new PenPathPair() { Pen = tankPen, Path = tankPath };
            _shapes.Add(engine);
            

                                              
        }

        public PenPathPair Thrust(PropulsionDB db)
        {
            //int maxFuel = db.FuelStorageCapicity / 10;

            int maxSpeed = db.MaximumSpeed / 10;
            int totalEP = db.TotalEnginePower / 25;
            PointF currentSpeed = new PointF((float)db.CurrentSpeed.X, (float)db.CurrentSpeed.Y);
            float currentSpeedLen = currentSpeed.Length / 10;

            Pen thrustPen = new Pen(Colors.OrangeRed);
            GraphicsPath thrustPath = new GraphicsPath();
            thrustPath.AddLine(-totalEP * 0.5f, maxSpeed, 0, maxSpeed + currentSpeedLen);
            thrustPath.AddLine(0, maxSpeed + currentSpeedLen, totalEP * 0.5f,  maxSpeed);
            return new PenPathPair() { Pen = thrustPen, Path = thrustPath };
        }

        public void DrawMe(Graphics g, PointF? atPosition = null)
        {
            if (atPosition == null)
                atPosition = new PointF((float)_starSysPosition.X, (float)_starSysPosition.Y);
            //if(_camera.ViewCoordinate(_starSysPosition.Position) > _camera.ViewPortCenter)

            g.SaveTransform();

            IMatrix cameraOffset = _camera.GetViewProjectionMatrix((PointF)atPosition);
            //apply the camera offset
            g.MultiplyTransform(cameraOffset);

            if (_radius * Zoom < _minRad)
                g.ScaleTransform(Scale);
            else
                g.ScaleTransform(Scale * Zoom);

            foreach (var item in _shapes)
            {
                g.DrawPath(item.Pen, item.Path);
            }

            if (_entity.HasDataBlob<PropulsionDB>()) //this seems a little hacky way to get this specific thing to redraw each update.
            {
                PenPathPair ppp = Thrust(_entity.GetDataBlob<PropulsionDB>());
                g.DrawPath(ppp.Pen, ppp.Path);
            }
            g.RestoreTransform();
        }

        public void DrawMe(Graphics g)
        {
            DrawMe(g, null);
        }

        internal struct PenPathPair
        {
            internal Pen Pen;
            internal GraphicsPath Path;
        }
    }
}
