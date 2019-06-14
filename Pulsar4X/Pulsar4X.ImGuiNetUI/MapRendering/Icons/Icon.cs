using System;
using System.Collections.Generic;
using SDL2;

namespace Pulsar4X.SDL2UI
{

    public interface IDrawData
    {
        void OnFrameUpdate(Matrix matrix, Camera camera);
        void OnPhysicsUpdate();
        void Draw(IntPtr rendererPtr, Camera camera);
    }



    /// <summary>
    /// A Collection of Shapes which will make up an icon. 
    /// </summary>
    public class Icon : IDrawData
    {
        protected ECSLib.IPosition _positionDB;
        protected ECSLib.Vector4 _worldPosition;
        public ECSLib.Vector4 WorldPosition
        {
            get { if (positionByDB) return _positionDB.AbsolutePosition_AU + _worldPosition; else return _worldPosition; }
            set { _worldPosition = value; }
        }
        /// <summary>
        /// If this is true, WorldPosition will be the sum of the PositionDB and any value given to WorldPosition
        /// </summary>
        protected bool positionByDB;
        public SDL.SDL_Point ViewScreenPos;
        public List<Shape> Shapes = new List<Shape>(); //these could change with entity changes. 
        public Shape[] DrawShapes;
        //public bool ShapesScaleWithZoom = false; //this possibly could change if you're zoomed in enough? normaly though, false for entity icons, true for orbit rings
        public float Scale = 1;
        public float Heading = 0;

        public Icon(ECSLib.IPosition positionDB)
        {
            _positionDB = positionDB;
            positionByDB = true;
        }
        public Icon(ECSLib.Vector4 position)
        {
            _worldPosition = position;
            positionByDB = false;
        }

        public virtual void OnPhysicsUpdate()
        {
            
        }

        public virtual void OnFrameUpdate(Matrix matrix, Camera camera)
        {


            ViewScreenPos = camera.ViewCoordinate(WorldPosition);
            Matrix nonZoomMatrix = new Matrix();
            nonZoomMatrix.Mirror(true, false);
            nonZoomMatrix.Scale(Scale);
  
            DrawShapes = new Shape[this.Shapes.Count];
            for (int i = 0; i < Shapes.Count; i++)
            {
                var shape = Shapes[i];
                PointD[] drawPoints = new PointD[shape.Points.Length];
                
                for (int i2 = 0; i2 < shape.Points.Length; i2++)
                {
                    int x;
                    int y;

                    var tranlsatedPoint = nonZoomMatrix.TransformD(shape.Points[i2].X, shape.Points[i2].Y);
                    x = (int)(ViewScreenPos.x + tranlsatedPoint.X );
                    y = (int)(ViewScreenPos.y + tranlsatedPoint.Y );
                    drawPoints[i2] = new PointD() { X = x, Y = y };
                }
                DrawShapes[i] = new Shape() { Points = drawPoints, Color = shape.Color };
            }
        }

        public virtual void Draw(IntPtr rendererPtr, Camera camera)
        {
            if (DrawShapes == null)
                return;
            foreach (var shape in DrawShapes)
            {
                SDL.SDL_SetRenderDrawColor(rendererPtr, shape.Color.r, shape.Color.g, shape.Color.b, shape.Color.a);

                for (int i = 0; i < shape.Points.Length - 1; i++)
                {
                    DrawTools.DrawLine(rendererPtr, Convert.ToInt32(shape.Points[i].X), Convert.ToInt32(shape.Points[i].Y), Convert.ToInt32(shape.Points[i + 1].X), Convert.ToInt32(shape.Points[i + 1].Y));
                }
            }

        }

    }


    public class SimpleCircle : IDrawData
    {
        Shape _shape;
        Shape _drawShape;
        protected ECSLib.IPosition _positionDB;
        protected ECSLib.Vector4 _worldPosition;
        public SDL.SDL_Point ViewScreenPos;

        bool positionByDB;

        public ECSLib.Vector4 WorldPosition
        {
            get { if (positionByDB) return _positionDB.AbsolutePosition_AU + _worldPosition; else return _worldPosition; }
            set { _worldPosition = value; }
        }

        public SimpleCircle(ECSLib.IPosition positionDB, double radius, SDL.SDL_Color colour)
        {
            _positionDB = positionDB;
            positionByDB = true;
            _shape = new Shape()
            {
                Points = CreatePrimitiveShapes.Circle(0, 0, radius, 128),
                Color = colour,
            };
        }

        public void Draw(IntPtr rendererPtr, Camera camera)
        {
            SDL.SDL_SetRenderDrawColor(rendererPtr, _drawShape.Color.r, _drawShape.Color.g, _drawShape.Color.b, _drawShape.Color.a);

            for (int i = 0; i < _shape.Points.Length - 1; i++)
            {
                var x0 = Convert.ToInt32(_drawShape.Points[i].X);
                var y0 = Convert.ToInt32(_drawShape.Points[i].Y);
                var x1 = Convert.ToInt32(_drawShape.Points[i + 1].X);
                var y1 = Convert.ToInt32(_drawShape.Points[i + 1].Y);
                DrawTools.DrawLine(rendererPtr, x0, y0, x1, y1);
            }
        }

        public void OnFrameUpdate(Matrix matrix, Camera camera)
        {


            ViewScreenPos = camera.ViewCoordinate(WorldPosition);
            var vsp = new PointD
            {
                X = ViewScreenPos.x ,
                Y = ViewScreenPos.y
            };
            PointD[] drawPoints = new PointD[_shape.Points.Length];

            for (int i2 = 0; i2 < _shape.Points.Length; i2++)
            {           
                var translatedPoint = matrix.TransformD(_shape.Points[i2].X, _shape.Points[i2].Y);
                int x = (int)(vsp.X + translatedPoint.X);
                int y = (int)(vsp.Y + translatedPoint.Y);
                drawPoints[i2] = new PointD() { X = x, Y = y };
            }
            _drawShape = new Shape() { Points = drawPoints, Color = _shape.Color };
        }

        public void OnPhysicsUpdate()
        {

        }
    }

    public class SimpleLine : IDrawData
    {
        Shape _shape;
        Shape _drawShape;
        protected ECSLib.IPosition _positionDB;
        protected ECSLib.Vector4 _worldPosition;
        public SDL.SDL_Point ViewScreenPos;

        bool positionByDB;

        public ECSLib.Vector4 WorldPosition
        {
            get { if (positionByDB) return _positionDB.AbsolutePosition_AU + _worldPosition; else return _worldPosition; }
            set { _worldPosition = value; }
        }

        public SimpleLine(ECSLib.IPosition positionDB, PointD toPoint, SDL.SDL_Color colour)
        {
            _positionDB = positionDB;
            positionByDB = true;
            PointD p0 = new PointD() { X = 0, Y = 0 };

            _shape = new Shape()
            {
                Points = new PointD[] {p0, toPoint },
                Color = colour,
            };
        }

        public void Draw(IntPtr rendererPtr, Camera camera)
        {
            SDL.SDL_SetRenderDrawColor(rendererPtr, _drawShape.Color.r, _drawShape.Color.g, _drawShape.Color.b, _drawShape.Color.a);

            for (int i = 0; i < _shape.Points.Length - 1; i++)
            {
                var x0 = Convert.ToInt32(_drawShape.Points[i].X);
                var y0 = Convert.ToInt32(_drawShape.Points[i].Y);
                var x1 = Convert.ToInt32(_drawShape.Points[i + 1].X);
                var y1 = Convert.ToInt32(_drawShape.Points[i + 1].Y);
                DrawTools.DrawLine(rendererPtr, x0, y0, x1, y1);
            }
        }

        public void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            ViewScreenPos = camera.ViewCoordinate(WorldPosition);
            var vsp = new PointD
            {
                X = ViewScreenPos.x,
                Y = ViewScreenPos.y
            };
            PointD[] drawPoints = new PointD[_shape.Points.Length];

            for (int i2 = 0; i2 < _shape.Points.Length; i2++)
            {
                var translatedPoint = matrix.TransformD(_shape.Points[i2].X, _shape.Points[i2].Y);
                int x = (int)(vsp.X + translatedPoint.X);
                int y = (int)(vsp.Y + translatedPoint.Y);
                drawPoints[i2] = new PointD() { X = x, Y = y };
            }
            _drawShape = new Shape() { Points = drawPoints, Color = _shape.Color };
        }

        public void OnPhysicsUpdate()
        {

        }
    }
}
