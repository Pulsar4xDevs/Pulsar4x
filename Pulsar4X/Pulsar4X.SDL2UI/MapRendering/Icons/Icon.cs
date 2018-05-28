using System;
using System.Collections.Generic;
using SDL2;

namespace Pulsar4X.SDL2UI
{

    public interface IDrawData
    {
        void Update();
        void Draw(IntPtr rendererPtr, Camera camera);
    }

    /// <summary>
    /// A collection of points and a single color.
    /// </summary>
    public struct Shape
    {
        public SDL.SDL_Color Color;    //could change due to entity changes. 
        public SDL.SDL_Point[] Points; //ralitive to the IconPosition. could change with entity changes. 
    }
    
    /// <summary>
    /// A Collection of Shapes which will make up an icon. 
    /// </summary>
    public class Icon : IDrawData
    {
        protected ECSLib.PositionDB _positionDB;
        public double WorldPositionX { get { return _positionDB.X; } } //this will change every game tick
        public double WorldPositionY { get { return _positionDB.Y; } } //this will change every game tick
        public SDL.SDL_Point ViewScreenPos;
        public List<Shape> Shapes = new List<Shape>(); //these could change with entity changes. 
        public bool ShapesScaleWithZoom = false; //this possibly could change if you're zoomed in enough? normaly though, false for entity icons, true for orbit rings 

        public Icon(ECSLib.PositionDB positionDB)
        {
            _positionDB = positionDB;
        }

        public virtual void Update()
        {

        }

        public virtual void Draw(IntPtr rendererPtr, Camera camera)
        {
            byte oR, oG, oB, oA;
            SDL.SDL_GetRenderDrawColor(rendererPtr, out oR, out oG, out oB, out oA);
            SDL.SDL_BlendMode blendMode;
            SDL.SDL_GetRenderDrawBlendMode(rendererPtr, out blendMode);
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
            float zoomLevel = 1;

            if (ShapesScaleWithZoom)
                zoomLevel = camera.ZoomLevel;
            List<Shape> transformedShapes = new List<Shape>();
            foreach (var shape in Shapes)
            {
                SDL.SDL_Point[] drawPoints = new SDL.SDL_Point[shape.Points.Length];//matrix.Transform(shape.Points);
                for (int i = 0; i < shape.Points.Length; i++)
                {
                    var camerapoint = camera.CameraViewCoordinate();
                    int x = (int)(ViewScreenPos.x + (shape.Points[i].x + camerapoint.x) * zoomLevel);
                    int y = (int)(ViewScreenPos.y + (shape.Points[i].y + camerapoint.y) * zoomLevel);
                    drawPoints[i] = new SDL.SDL_Point() { x = x, y = y };
                }
                transformedShapes.Add(new Shape() { Points = drawPoints, Color = shape.Color });
            }



            foreach (var shape in transformedShapes)
            {
                SDL.SDL_SetRenderDrawColor(rendererPtr, shape.Color.r, shape.Color.g, shape.Color.b, shape.Color.a);

                for (int i = 0; i < shape.Points.Length - 1; i++)
                {
                    SDL.SDL_RenderDrawLine(rendererPtr, shape.Points[i].x, shape.Points[i].y, shape.Points[i + 1].x, shape.Points[i + 1].y);
                }
            }
            SDL.SDL_SetRenderDrawColor(rendererPtr, oR, oG, oB, oA);
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, blendMode);
        }

    }
}
