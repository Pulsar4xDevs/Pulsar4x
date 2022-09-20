using System;
using System.Collections.Generic;
using System.Threading;
using Pulsar4X.ECSLib;
using Pulsar4X.Orbital;
using SDL2;

namespace Pulsar4X.SDL2UI
{

    public interface IDrawData
    {
        void OnFrameUpdate(Matrix matrix, Camera camera);
        void OnPhysicsUpdate();
        void Draw(IntPtr rendererPtr, Camera camera);
    }

    public interface IUpdateUserSettings
    {
        void UpdateUserSettings();
    }

    /// <summary>
    /// A Collection of Shapes which will make up an icon. 
    /// </summary>
    public class Icon : IDrawData
    {
        internal bool DebugShowCenter = false;
        
        protected ECSLib.IPosition _positionDB;
        protected Orbital.Vector3 _worldPosition_m { get; set; }
        public Orbital.Vector3 WorldPosition_AU
        {
            get { return Distance.MToAU(WorldPosition_m); }
        }
        public Orbital.Vector3 WorldPosition_m
        {
            get 
            { 
                if (positionByDB) 
                    return _positionDB.AbsolutePosition + _worldPosition_m; 
                else 
                    return _worldPosition_m; 
            }
            set 
            { 
                _worldPosition_m = value; 
            }
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
        public bool InMeters = false;
        public Icon(ECSLib.IPosition positionDB)
        {
            _positionDB = positionDB;
            positionByDB = true;
        }
        public Icon(Vector3 position_m)
        {
            _worldPosition_m = position_m;
            positionByDB = false;
        }

        public void ResetPositionDB(ECSLib.IPosition positionDB)
        {
            _positionDB = positionDB;
            positionByDB = true;
        }

        public virtual void OnPhysicsUpdate()
        {
            
        }

        public virtual void OnFrameUpdate(Matrix matrix, Camera camera)
        {


            ViewScreenPos = camera.ViewCoordinate_m(WorldPosition_m);
            var pos = camera.ViewCoordinateV2_m(WorldPosition_m);
            var mirrorMtx = Matrix.IDMirror(true, false);
            var scaleMtx = Matrix.IDScale(Scale, Scale);
            var posMtx = Matrix.IDTranslate(pos.X, pos.Y);
            Matrix mtx = mirrorMtx * scaleMtx * posMtx;
            
            int shapeCount = Shapes.Count;
            int dsi = 0;
            DrawShapes = new Shape[shapeCount];
            
            if (DebugShowCenter)
            {
                dsi = 3;
                DrawShapes = new Shape[shapeCount+dsi];
                var mtxb = Matrix.IDTranslate(ViewScreenPos.x, ViewScreenPos.y);
                DrawShapes[0] = CreatePrimitiveShapes.CenterWidget(mtxb);

                var abspos = camera.ViewCoordinateV2_m(_positionDB.AbsolutePosition);
                Shape absCtr = new Shape();
                absCtr.Points = CreatePrimitiveShapes.Crosshair();
                byte r = 150;
                byte g = 50;
                byte b = 200;
                byte a = 255;
                SDL.SDL_Color colour = new SDL.SDL_Color() {r = r, g = g, b = b, a = a};
                absCtr.Color = colour;
                DrawShapes[1] = absCtr;
                
                var ralpos = camera.ViewCoordinateV2_m(_positionDB.RelativePosition_m + _worldPosition_m);
                Shape ralCtr = new Shape();
                ralCtr.Points = CreatePrimitiveShapes.Crosshair();
                 r = 200;
                 g = 50;
                 b = 150;
                 a = 255;
                colour = new SDL.SDL_Color() {r = r, g = g, b = b, a = a};
                ralCtr.Color = colour;
                DrawShapes[1] = ralCtr;

            }
            
            for (int i = 0; i < shapeCount; i++)
            {
                var shape = Shapes[i];
                var manipulatedShape = new Shape();
                manipulatedShape.Points = mtx.TransformToVector2(shape.Points);
                manipulatedShape.Color = shape.Color;
                DrawShapes[i+dsi] = manipulatedShape;
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
                    //if the point is within int32 range, convert(round) else use max or min. 
                    int x1; 
                    
                    if (shape.Points[i].X > int.MaxValue)
                        x1 = int.MaxValue;
                    else if ((shape.Points[i].X < int.MinValue))
                        x1 = int.MinValue;
                    else
                        x1 = Convert.ToInt32(shape.Points[i].X);
                    
                    int y1; 
                    
                    if (shape.Points[i].Y > int.MaxValue)
                        y1 = int.MaxValue;
                    else if ((shape.Points[i].Y < int.MinValue))
                        y1 = int.MinValue;
                    else
                        y1 = Convert.ToInt32(shape.Points[i].Y);
     
                    
                    int x2; 
                    
                    if (shape.Points[i+1].X > int.MaxValue)
                        x2 = int.MaxValue;
                    else if ((shape.Points[i+1].X < int.MinValue))
                        x2 = int.MinValue;
                    else
                        x2 = Convert.ToInt32(shape.Points[i+1].X);
                    
                    int y2; 
                    
                    if (shape.Points[i+1].Y > int.MaxValue)
                        y2 = int.MaxValue;
                    else if ((shape.Points[i+1].Y < int.MinValue))
                        y2 = int.MinValue;
                    else
                        y2 = Convert.ToInt32(shape.Points[i+1].Y);

                    SDL.SDL_RenderDrawLine(rendererPtr, x1, y1, x2, y2);
                }
            }

        }

    }


    public class SimpleCircle : IDrawData
    {
        Shape _shape;
        Shape _drawShape;
        protected IPosition _positionDB;
        protected Vector3 _worldPosition;
        public SDL.SDL_Point ViewScreenPos;

        bool positionByDB;

        public Vector3 WorldPosition
        {
            get { if (positionByDB) return _positionDB.AbsolutePosition + _worldPosition; else return _worldPosition; }
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
                SDL.SDL_RenderDrawLine(rendererPtr, x0, y0, x1, y1);
            }
        }

        public void OnFrameUpdate(Matrix matrix, Camera camera)
        {


            ViewScreenPos = camera.ViewCoordinate_m(WorldPosition);
            var vsp = new Vector2
            {
                X = ViewScreenPos.x ,
                Y = ViewScreenPos.y
            };
            Orbital.Vector2[] drawPoints = new Orbital.Vector2[_shape.Points.Length];

            for (int i2 = 0; i2 < _shape.Points.Length; i2++)
            {           
                var translatedPoint = matrix.TransformD(_shape.Points[i2].X, _shape.Points[i2].Y);
                int x = (int)(vsp.X + translatedPoint.X);
                int y = (int)(vsp.Y + translatedPoint.Y);
                drawPoints[i2] = new Orbital.Vector2() { X = x, Y = y };
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
        protected Orbital.Vector3 _worldPosition;
        public SDL.SDL_Point ViewScreenPos;

        bool positionByDB;

        public Orbital.Vector3 WorldPosition
        {
            get { if (positionByDB) return _positionDB.AbsolutePosition + _worldPosition; else return _worldPosition; }
            set { _worldPosition = value; }
        }

        public SimpleLine(ECSLib.IPosition positionDB, Orbital.Vector2 toPoint, SDL.SDL_Color colour)
        {
            _positionDB = positionDB;
            positionByDB = true;
            Orbital.Vector2 p0 = new Orbital.Vector2() { X = 0, Y = 0 };

            _shape = new Shape()
            {
                Points = new Orbital.Vector2[] {p0, toPoint },
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
                SDL.SDL_RenderDrawLine(rendererPtr, x0, y0, x1, y1);
            }
        }

        public void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            ViewScreenPos = camera.ViewCoordinate_m(WorldPosition);
            var vsp = new Orbital.Vector2()
            {
                X = ViewScreenPos.x,
                Y = ViewScreenPos.y
            };
            Orbital.Vector2[] drawPoints = new Orbital.Vector2[_shape.Points.Length];

            for (int i2 = 0; i2 < _shape.Points.Length; i2++)
            {
                var translatedPoint = matrix.TransformD(_shape.Points[i2].X, _shape.Points[i2].Y);
                int x = (int)(vsp.X + translatedPoint.X);
                int y = (int)(vsp.Y + translatedPoint.Y);
                drawPoints[i2] = new Orbital.Vector2() { X = x, Y = y };
            }
            _drawShape = new Shape() { Points = drawPoints, Color = _shape.Color };
        }

        public void OnPhysicsUpdate()
        {

        }
    }
}
