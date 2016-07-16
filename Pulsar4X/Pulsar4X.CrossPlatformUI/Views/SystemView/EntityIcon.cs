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
        private float Zoom { get { return _camera.ZoomLevel; } }
        private Size ViewSize { get { return _camera._viewPort.Size; } }
        private PositionDB _starSysPosition; 
        private Camera2dv2 _camera;

        public EntityIcon(Entity entity, Camera2dv2 camera)
        {
            _camera = camera;
            _starSysPosition = entity.GetDataBlob<PositionDB>();
            foreach (var item in entity.DataBlobs)
            {
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

        void HasStarInfo(StarInfoDB db)
        {
            //TODO: change pen colour depending on star temp and lum?
            double temp = db.Temperature;
            double lum = db.Luminosity;

            Pen starPen = new Pen(Colors.DarkOrange);

            float width = 12;
            float height = 12;
            float hw = width * 0.5f;
            float hh = height * 0.5f;

            GraphicsPath starPath = new GraphicsPath();

            PointF start = new PointF(0, -height);
            PointF c1 = new PointF(-width, 0);
            PointF c2 = new PointF(-hw, -hh);
            PointF end = new PointF(-hw, -hh);
            starPath.AddBezier(start, c1, c2, end);

            start = new PointF(-width, 0);
            c1 = new PointF(0, height);
            c2 = new PointF(-hw, hh);
            end = new PointF(-hw, hh);
            starPath.AddBezier(start, c1, c2, end);

            start = new PointF(0, height);
            c1 = new PointF(width, 0);
            c2 = new PointF(hw, hh);
            end = new PointF(hw, hh);
            starPath.AddBezier(start, c1, c2, end);

            start = new PointF(width, 0);
            c1 = new PointF(0, -height);
            c2 = new PointF(hw, -hh);
            end = new PointF(hw, -hh);
            starPath.AddBezier(start, c1, c2, end);

            PenPathPair starPathPair = new PenPathPair() { Pen = starPen, Path = starPath };
            _shapes.Add(starPathPair);
        }

        void HasSysBodyInfo(SystemBodyDB db)
        {
        }

        void HasPosition(PositionDB db)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(-2, -2, 2, 2);
            PenPathPair circle = new PenPathPair() { Pen = new Pen(Colors.Blue), Path = path };
            _shapes.Add(circle);
        }

        void HasPropulsionDB(PropulsionDB db)
        {

            int maxFuel = db.FuelStorageCapicity / 10;

            int maxSpeed = db.MaximumSpeed / 10;
            int totalEP = db.TotalEnginePower / 10;
            PointF currentSpeed = new PointF((float)db.CurrentSpeed.X, (float)db.CurrentSpeed.Y);

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
            
            Pen thrustPen = new Pen(Colors.OrangeRed);
            GraphicsPath thrustPath = new GraphicsPath();
            thrustPath.AddLine(-totalEP * 0.5f, maxFuel + maxSpeed, 0, currentSpeed.Length / 10);
            thrustPath.AddLine(0, currentSpeed.Length / 10, totalEP * 0.5f, maxFuel + maxSpeed);
            _shapes.Add(new PenPathPair() { Pen = thrustPen, Path = thrustPath });                      
        }

        public void DrawMe(Graphics g)
        {
            //if(_camera.ViewCoordinate(_starSysPosition.Position) > _camera.ViewPortCenter)

            g.SaveTransform();

            IMatrix cameraOffset = _camera.GetViewProjectionMatrix(new PointF((float)_starSysPosition.X, (float)_starSysPosition.Y));
            //apply the camera offset
            g.MultiplyTransform(cameraOffset);

            foreach (var item in _shapes)
            {
                g.DrawPath(item.Pen, item.Path);
            }

            g.RestoreTransform();
        }

        struct PenPathPair
        {
            internal Pen Pen;
            internal GraphicsPath Path;
        }
    }
}
