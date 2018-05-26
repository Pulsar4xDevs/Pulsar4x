using System;
using Pulsar4X.ECSLib;
using SDL2;
using System.Collections.Generic;

namespace Pulsar4X.SDL2UI
{
    public class TestDrawIconData : IDrawData
    {
        List<Icon> icons = new List<Icon>();
        Camera _camera;

        public TestDrawIconData(Camera camera)
        {
            _camera = camera;
            Setup();
        }
        internal void Setup()
        {

            SDL.SDL_Point[] lpoints1 = new SDL.SDL_Point[] {
                new SDL.SDL_Point { x = 0, y = -100 },
                new SDL.SDL_Point { x = 0, y = 100 },
            };
            SDL.SDL_Point[] lpoints2 = new SDL.SDL_Point[] {
                new SDL.SDL_Point { x = -100, y = 0 },
                new SDL.SDL_Point { x = 100, y = 0 }
            };
            SDL.SDL_Color lcolor = new SDL.SDL_Color() { r = 0, g = 255, b = 0, a = 255 };
            Shape lshape1 = new Shape() { Points = lpoints1, Color = lcolor };
            Shape lshape2 = new Shape() { Points = lpoints2, Color = lcolor };
            PositionDB lpos = new PositionDB(new Vector4(0, 0, 0, 0), new Guid());

            icons.Add(new Icon(lpos) { Shapes = new Shape[2] { lshape1, lshape2 } });

            for (int i = 0; i < 4; i++)
            {
                SDL.SDL_Point[] points = CreatePrimitiveShapes.CreateArc(50 + 50 * i, 400, 100, 100, 0, 4.71, 160);
                SDL.SDL_Color color = new SDL.SDL_Color() { r = (byte)(i * 60), g = 100, b = 100, a = 255 };
                Shape shape = new Shape() { Points = points, Color = color };
                PositionDB pos = new PositionDB(new Vector4(0, 0, 0, 0), new Guid());

                icons.Add(new Icon(pos) { Shapes = new Shape[1] { shape } });
            }

        }

        public void Draw(IntPtr rendererPtr, Camera camera)
        {
            byte oR, oG, oB, oA;
            SDL.SDL_GetRenderDrawColor(rendererPtr, out oR, out oG, out oB, out oA);
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

            List<Shape> transformedShapes = new List<Shape>();
            SDL.SDL_SetRenderDrawColor(rendererPtr, 255, 255, 255, 50);
            SDL.SDL_RenderDrawLine(rendererPtr, 50, 50, 200, 200);


            foreach (var icon in icons)
            {
                foreach (var shape in icon.Shapes)
                {
                    SDL.SDL_Point[] drawPoints = new SDL.SDL_Point[shape.Points.Length];//matrix.Transform(shape.Points);
                    for (int i = 0; i < shape.Points.Length; i++)
                    {
                        var camerapoint = _camera.CameraViewCoordinate();
                        int x = (int)((shape.Points[i].x + camerapoint.x) * _camera.ZoomLevel);
                        int y = (int)((shape.Points[i].y + camerapoint.y) * _camera.ZoomLevel);
                        drawPoints[i] = new SDL.SDL_Point() { x = x, y = y };
                    }
                    transformedShapes.Add(new Shape() { Points = drawPoints, Color = shape.Color });
                }
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
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, SDL.SDL_BlendMode.SDL_BLENDMODE_NONE);
        }

        public void Update()
        {

        }
    }
}
