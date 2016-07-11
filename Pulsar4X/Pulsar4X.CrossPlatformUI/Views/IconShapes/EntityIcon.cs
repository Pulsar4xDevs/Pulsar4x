using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using Eto.Drawing;

namespace Pulsar4X.CrossPlatformUI
{
    class EntityIcon
    {

        List<PenPathPair> _shapes = new List<PenPathPair>();

        void PositionDB(PositionDB db)
        {
            
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
            foreach (var item in _shapes)
            {
                g.DrawPath(item.Pen, item.Path);
            }
        }

        struct PenPathPair
        {
            internal Pen Pen;
            internal GraphicsPath Path;
        }
    }
}
