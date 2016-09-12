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
        public float Scale { get; set; } = 2;
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
                    SetIconFor((MassVolumeDB)item);
                }
                if (item is PropulsionDB)
                {
                    SetIconFor((PropulsionDB)item);
                }
                if (item is PositionDB)
                {
                    SetIconFor((PositionDB)item);
                }
                if (item is StarInfoDB)
                {
                    SetIconFor((StarInfoDB)item);
                }
                if (item is SystemBodyInfoDB)
                {
                    SetIconFor((SystemBodyInfoDB)item);
                }
                if (item is CargoStorageDB)
                {
                    SetIconFor((CargoStorageDB)item);
                }
            }
        }

        void SetIconFor(MassVolumeDB db)
        {
            _radius = db.Radius;
            
        }

        void SetIconFor(StarInfoDB db)
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



        void SetIconFor(SystemBodyInfoDB db)
        {
            BodyType type = db.BodyType;
            float temp = db.BaseTemperature;
            
        }

        void SetIconFor(AtmosphereDB db)
        {
            short hydro = db.HydrosphereExtent;
            float albedo = db.Albedo;
        }

        void SetIconFor(PositionDB db)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(-2, -2, 4, 4);
            PenPathPair circle = new PenPathPair() { Pen = new Pen(Colors.Blue), Path = path };
            _shapes.Add(circle);
        }

        void SetIconFor(PropulsionDB db)
        {

            int maxSpeed = db.MaximumSpeed / 10;
            int totalEP = db.TotalEnginePower / 25;
            PointF currentSpeed = new PointF((float)db.CurrentSpeed.X, (float)db.CurrentSpeed.Y);
            float currentSpeedLen = currentSpeed.Length / 10;

            Pen enginePen = new Pen(Colors.DarkGray);
            GraphicsPath enginePath = new GraphicsPath();
            enginePath.AddRectangle(-totalEP * 0.5f, 0, totalEP, maxSpeed);
            PenPathPair engine = new PenPathPair() { Pen = enginePen, Path = enginePath };
            _shapes.Add(engine);
                                               
        }

        void SetIconFor(CargoStorageDB cargodb)
        {
            int stackHeight = 0;
            float red = 128 / 255;
            float green = 128 / 255;
            float blue = 128 / 255;
            Color colour = new Color(128 /255, 128/255, 128/255);
            foreach (var item in cargodb.CargoCapicity)
            {
                int height = (int)(item.Value / 100);
                int width = (int)(item.Value / 100);
                Pen containerPen = new Pen(new Color(red, green, blue));
                green += 20 / 255;
                GraphicsPath containerPath = new GraphicsPath();
                containerPath.AddRectangle(width / 2, stackHeight, width, height);
                PenPathPair container = new PenPathPair() { Pen = containerPen, Path = containerPath };
                _shapes.Add(container);
                stackHeight += height;
            }
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
