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
    /// A collection of points and a single color.
    /// </summary>
    public struct Shape
    {
        public SDL.SDL_Color Color;    //could change due to entity changes. 
        public PointD[] Points; //ralitive to the IconPosition. could change with entity changes. 
    }
    
    /// <summary>
    /// A Collection of Shapes which will make up an icon. 
    /// </summary>
    public class Icon : IDrawData
    {
        protected ECSLib.PositionDB _positionDB;
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
        public bool ShapesScaleWithZoom = false; //this possibly could change if you're zoomed in enough? normaly though, false for entity icons, true for orbit rings 

        public float Scale = 1;


        public Icon(ECSLib.PositionDB positionDB)
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
            var camerapoint = camera.CameraViewCoordinate();

            ViewScreenPos = matrix.Transform(WorldPosition.X, WorldPosition.Y);

            //matrix.Translate(WorldPosition.X + camerapoint.x, WorldPosition.Y + camerapoint.y);

            //todo: proper matrix transformations might clean this code up a bit. I'm failing to get it working properly though. 
            //Matrix matrix2 = new Matrix();
            //matrix2.Translate(WorldPosition.X, WorldPosition.Y);
            //matrix2.Scale(camera.ZoomLevel);
            //matrix2.Translate(camerapoint.x, camerapoint.y);


            float zoomLevel = 1;

            if (ShapesScaleWithZoom)
                zoomLevel = camera.ZoomLevel;
            DrawShapes = new Shape[this.Shapes.Count];
            for (int i = 0; i < Shapes.Count; i++)
            {
                var shape = Shapes[i];
                PointD[] drawPoints = new PointD[shape.Points.Length];//matrix.Transform(shape.Points);
                for (int i2 = 0; i2 < shape.Points.Length; i2++)
                {

                    int x = (int)(ViewScreenPos.x + (shape.Points[i2].X + camerapoint.x) * zoomLevel);
                    int y = (int)(ViewScreenPos.y + (shape.Points[i2].Y + camerapoint.y) * zoomLevel);

                    //SDL.SDL_Point pnt = matrix2.Transform(shape.Points[i2].x, shape.Points[i2].y);
                    //int x1 = (int)(pnt.x * zoomLevel);
                    //int y1 = (int)(pnt.y * zoomLevel);
                    drawPoints[i2] = new PointD() { X = x, Y = y };
                }
                DrawShapes[i] = (new Shape() { Points = drawPoints, Color = shape.Color });
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
                    SDL.SDL_RenderDrawLine(rendererPtr, Convert.ToInt32(shape.Points[i].X), Convert.ToInt32(shape.Points[i].Y), Convert.ToInt32(shape.Points[i + 1].X), Convert.ToInt32(shape.Points[i + 1].Y));
                }
            }

        }

    }
}
