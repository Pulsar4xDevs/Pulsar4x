using System;
using Pulsar4X.ECSLib;
using ImGuiSDL2CS;
using SDL2;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ImGuiNET;

namespace Pulsar4X.SDL2UI
{
    internal class SystemMapRendering : PulsarGuiWindow
    {
        GlobalUIState _state;

        internal IntPtr windowPtr;
        internal IntPtr surfacePtr; 
        internal IntPtr rendererPtr;
        ImGuiSDL2CSWindow _window;
        List<DrawData> _drawableIcons;
        List<Vector4> positions = new List<Vector4>();
        List<OrbitDB> orbits = new List<OrbitDB>();
        SystemMap_DrawableVM sysMap;
        Entity _faction;
        internal SystemMapRendering(ImGuiSDL2CSWindow window, GlobalUIState state)
        {
            _state = state;
            _window = window;
            windowPtr = window.Handle;
            surfacePtr = SDL.SDL_GetWindowSurface(windowPtr);
            rendererPtr = SDL.SDL_GetRenderer(windowPtr);
        }

        internal void SetSystem(StarSystem starSys)
        {
            sysMap = new SystemMap_DrawableVM();
            sysMap.Initialise(null, starSys, _state.Faction);
            _faction = _state.Faction;
            //orbits.AddRange(sysMap.IconableEntitys

        }

        internal override void Display()
        {
            if (sysMap == null) //maybe have a map saved which we can load? for now we're using this to test stuff.
            {
                SDL.SDL_SetRenderDrawColor(rendererPtr, 255, 255, 255, 0);
                SDL.SDL_RenderDrawLine(rendererPtr, 50, 50, 200, 200);
                SDL.SDL_RenderDrawLine(rendererPtr, 200, 200, 200, 250);
                SDL.SDL_RenderDrawLine(rendererPtr, 150, 50, 200, 200);
                DrawPrimitive.DrawEllipse(rendererPtr, 100, 100, 200, 300);

                DrawPrimitive.DrawArc(rendererPtr, 500, 100, 50, 50, 0, 1.5708);

                DrawPrimitive.DrawAlphaFadeArc(rendererPtr, 300, 400, 150, 100, 0, 4.71, 255, 0);

                List<Shape> shapes = new List<Shape>();

                for (int i = 0; i < 4; i++)
                {
                    SDL.SDL_Point[] points = CreatePrimitiveShapes.CreateArc(50 + 50 * i, 400, 100, 100, 0, 4.71, 160);
                    SDL.SDL_Color color = new SDL.SDL_Color() { r =  (byte)(i * 60), g = 100, b = 100, a = 255 };
                    Shape shape = new Shape() { Points = points, Color = color };
                    shapes.Add(shape);
                }

                byte oR, oG, oB, oA;
                SDL.SDL_GetRenderDrawColor(rendererPtr, out oR, out oG, out oB, out oA);
                SDL.SDL_SetRenderDrawBlendMode(rendererPtr, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
                foreach (var shape in shapes)
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
            else
            {
                foreach (var icon in _drawableIcons)
                {
                    icon.Draw(rendererPtr);
                }
            }
        }
    }
}
