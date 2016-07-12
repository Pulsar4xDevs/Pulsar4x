using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using Eto.Drawing;
using Eto.Forms;

namespace Pulsar4X.CrossPlatformUI
{
    internal class Camera2dv2
    {
        private PointF _cameraWorldPosition = new PointF(0, 0);
        public PointF WorldPosition { get { return _cameraWorldPosition; } }

        public Size ViewPortCenter { get { return _viewPort.Size / 2; } }
        public float ZoomLevel { get; set; } = 200;

        public Drawable _viewPort;


        /// <summary>
        /// Construct a new Camera class within the Graphic Control Viewport. 
        /// </summary>
        public Camera2dv2(Drawable viewPort)
        {
            _viewPort = viewPort;
            //_viewPort.SizeChanged += _viewPort_SizeChanged;
        }

        /// <summary>
        /// returns the viewCoordinate of a given world Coordinate 
        /// </summary>
        /// <param name="worldCoord"></param>
        /// <returns></returns>
        public Point ViewCoordinate(PointF worldCoord)
        {
            Point viewCoord = (Point)(worldCoord * (ZoomLevel) + ViewPortCenter);
            return viewCoord;
        }

        public Point ViewCoordinate(Vector4 worldCoord)
        {
            PointF coord = new PointF((float)worldCoord.X, (float)worldCoord.Y);
            return ViewCoordinate(coord);
        }

        public IMatrix GetViewProjectionMatrix(bool scaleWithZoom = true)
        {
            var transformMatrix = Matrix.Create();            
            transformMatrix.Translate(ViewCoordinate(_cameraWorldPosition));

            return transformMatrix;
        }
    }

    internal class IconCollection
    {
        List<IconBase> Icons { get; } = new List<IconBase>();

        //sets the distance between icons. 

        public void DrawMe(Graphics g)
        {
            foreach (var item in Icons)
            {
                item.DrawMe(g);
            }
        }
    }

    internal interface IconBase
    {
        //sets the size of the icons
        float Scale { get; set; } 

        void DrawMe(Graphics g);        
    }

    internal class EntityIcon : IconBase
    {
        public float Scale { get; set; } = 1;
        List<PenPathPair> _shapes = new List<PenPathPair>();
        private float Zoom { get { return _camera.ZoomLevel; } }
        private Size ViewSize { get { return _camera._viewPort.Size; } }
        private PositionDB _starSysPosition; 
        private Camera2dv2 _camera;

        EntityIcon(Camera2dv2 camera, Entity entity)
        {
            _camera = camera;
            _starSysPosition = entity.GetDataBlob<PositionDB>();
        }

        IMatrix PositionTransform()
        {
            var positionMatrix = Matrix.Create();
            PointF position = new PointF((float)_starSysPosition.X, (float)_starSysPosition.Y);
            positionMatrix.Translate(position);

            return positionMatrix;
        }

        void HasPropulsionDB(PropulsionDB db)
        {

            int maxFuel = db.FuelStorageCapicity / 100;

            int maxSpeed = db.MaximumSpeed / 100;
            int totalEP = db.TotalEnginePower / 100;
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
            thrustPath.AddLine(-totalEP * 0.5f, maxFuel + maxSpeed, 0, currentSpeed.Length / 100);
            thrustPath.AddLine(0, currentSpeed.Length / 100, totalEP * 0.5f, maxFuel + maxSpeed);
            _shapes.Add(new PenPathPair() { Pen = thrustPen, Path = thrustPath });                      
        }

        public void DrawMe(Graphics g)
        {
            //if(_camera.ViewCoordinate(_starSysPosition.Position) > _camera.ViewPortCenter)

            g.SaveTransform();
            g.MultiplyTransform(PositionTransform());
            IMatrix cameraOffset = _camera.GetViewProjectionMatrix();
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
