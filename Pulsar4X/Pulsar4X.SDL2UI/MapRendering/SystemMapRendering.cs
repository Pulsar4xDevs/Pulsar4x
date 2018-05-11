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
            if (sysMap == null)
            {
                SDL.SDL_SetRenderDrawColor(rendererPtr, 255, 255, 255, 0);
                SDL.SDL_RenderDrawLine(rendererPtr, 50, 50, 200, 200);
                SDL.SDL_RenderDrawLine(rendererPtr, 200, 200, 200, 250);
                SDL.SDL_RenderDrawLine(rendererPtr, 150, 50, 200, 200);
                DrawPrimitivs.DrawEllipse(rendererPtr, 100, 100, 200, 300);

                DrawPrimitivs.DrawArc(rendererPtr, 300, 300, 50, 50, 0, 1.5708);

                DrawPrimitivs.DrawAlphaFadeArc(rendererPtr, 300, 400, 150, 100, 0, 4.71, 255, 0); 
            }
            else
            {
                foreach (var entity in sysMap.IconableEntitys)
                {

                    if (entity.HasDataBlob<OrbitDB>())
                    {
                        OrbitDrawData orbitData = new OrbitDrawData(entity);
                        orbitData.Update();
                        orbitData.Draw(rendererPtr);
                        
                    }
                    if (entity.HasDataBlob<NameDB>())
                    {
                        ImGui.Begin("", ImGuiWindowFlags.NoTitleBar);
                        ImGui.Text(entity.GetDataBlob<NameDB>().GetName(_faction));
                        ImGui.End();
                    }
                }
            }
        }
    }



    public class OrbitDrawData
    {
        #region Static properties
        OrbitDB _orbitDB;
        PositionDB _bodyPositionDB;
        PositionDB _parentPositionDB;
        float _orbitElipseMajor;
        float _orbitElipseSemiMaj;
        float _orbitElipseMinor;
        float _orbitElipseSemiMinor;
        float _orbitAngle;
        float _radianAngle;
        float _focalDistance;
        #endregion

        #region Dynamic Properties
        //focal point of the elipse this is the point the entity is orbiting around - this will change if the parent is inturn orbiting something.
        double _focalX;
        double _focalY;
        double _ctrPosX;
        double _ctrPosY;
        float _startArcAngle;
        float _endArcAngle;
        //user adjustable
        float _sweepAngle;
        byte _numberOfAlphaSegments = 255;
        byte _numberOfArcSegments = 255;
        #endregion
        internal OrbitDrawData(Entity entity)
        {
            _orbitDB = entity.GetDataBlob<OrbitDB>();
            _bodyPositionDB = entity.GetDataBlob<PositionDB>();
            _parentPositionDB = _orbitDB.Parent.GetDataBlob<PositionDB>();
            _orbitElipseMajor = (float)_orbitDB.SemiMajorAxis * 2; //Major Axis
            _orbitElipseSemiMaj = _orbitElipseMajor * 0.5f;
            _orbitElipseMinor = (float)Math.Sqrt((_orbitDB.SemiMajorAxis * _orbitDB.SemiMajorAxis) * (1 - _orbitDB.Eccentricity * _orbitDB.Eccentricity)) * 2;
            _orbitElipseSemiMinor = _orbitElipseMinor * 0.5f;
            _orbitAngle = (float)(_orbitDB.LongitudeOfAscendingNode + _orbitDB.ArgumentOfPeriapsis * 2); //This is the LoP + AoP.
            _radianAngle = (float)(_orbitAngle * Math.PI / 180);
            _focalDistance = (float)(_orbitDB.Eccentricity * _orbitElipseMajor * 0.5f); //linear ecentricity
        }

        internal void Update()
        {
            _focalX = _parentPositionDB.X;
            _focalY = _parentPositionDB.Y;
            _ctrPosX = _focalX - _focalDistance;
            _ctrPosY = _focalY;
            Vector4 pos = _bodyPositionDB.AbsolutePosition - _parentPositionDB.AbsolutePosition; //adjust so moons get the right positions    
            double normalX = (pos.X * Math.Cos(-_radianAngle)) - (pos.Y * Math.Sin(-_radianAngle));
            double normalY = (pos.X * Math.Sin(-_radianAngle)) + (pos.Y * Math.Cos(-_radianAngle));
            normalX += _focalDistance; //adjust for focal point
            normalY *= (_orbitElipseMajor / _orbitElipseMinor); //adjust for elliptic angle. 

            float startAngle = (float)(Math.Atan2(normalY, normalX) * 180 / Math.PI);
        }

        internal void Draw(IntPtr renderer)
        {
            double angle = (Math.PI * 2.0) / (_numberOfArcSegments);
            int lastX = (int)(_ctrPosX + Math.Round(_orbitElipseSemiMaj * Math.Sin(angle)));
            int lastY = (int)(_ctrPosY + Math.Round(_orbitElipseSemiMinor * Math.Cos(angle)));
            int drawX;
            int drawY;
            for (int i = 0; i < _numberOfArcSegments; i++)
            {
                drawX = (int)(_ctrPosX + Math.Round(_orbitElipseSemiMaj * Math.Sin(angle * i)));
                drawY = (int)(_ctrPosY + Math.Round(_orbitElipseSemiMinor * Math.Cos(angle * i)));
                SDL.SDL_RenderDrawPoint(renderer, drawX, drawY);
                //SDL.SDL_RenderDrawLine(renderer, lastX, lastY, drawX, drawY);
                lastX = drawX;
                lastY = drawY;
            }
        }

        ImVec2 elipsePeremeterPoint(double angle)
        {
            ImVec2 point = new ImVec2();
            point.x = (float)(_orbitElipseSemiMaj * Math.Sin(angle));
            point.y = (float)(_orbitElipseSemiMinor * Math.Cos(angle));
            return point;
        }
    }


    public static class DrawPrimitivs
    {
        public static void DrawEllipse(IntPtr renderer, int posX, int posY, double xWidth, double yWidth)
        {
            byte _numberOfArcSegments = 255;

            double angle = (Math.PI * 2.0) / (_numberOfArcSegments);

            int lastX = posX + (int)Math.Round(xWidth * Math.Sin(angle));
            int lastY = posY + (int)Math.Round(yWidth * Math.Cos(angle));
            int drawX;
            int drawY;
            for (int i = 0; i < _numberOfArcSegments + 1; i++)
            {
                drawX = posX + (int)Math.Round(xWidth * Math.Sin(angle * i));
                drawY = posY + (int)Math.Round(yWidth * Math.Cos(angle * i));
                //SDL.SDL_RenderDrawPoint(renderer, drawX, drawY);
                SDL.SDL_RenderDrawLine(renderer, lastX, lastY, drawX, drawY);
                lastX = drawX;
                lastY = drawY;
            }
        }

        public static void DrawArc(IntPtr renderer, int posX, int posY, double xWidth, double yWidth, double startAngleRadians, double arcAngleRadians)
        {
            byte _numberOfArcSegments = 255;

            double incrementAngle = (Math.PI * 2.0) / (_numberOfArcSegments);

            int lastX = posX + (int)Math.Round(xWidth * Math.Sin(startAngleRadians));
            int lastY = posY + (int)Math.Round(yWidth * Math.Cos(startAngleRadians));
            int drawX;
            int drawY;
            int totalSegments = (int)(arcAngleRadians / incrementAngle);

            /*
            SDL.SDL_Point[] points = new SDL.SDL_Point[_numberOfArcSegments];

            for (int i = 1; i < totalSegments; i++)
            {
                double nextAngle = startAngleRadians + incrementAngle * i;
                drawX = posX + (int)Math.Round(xWidth * Math.Sin(nextAngle));
                drawY = posY + (int)Math.Round(yWidth * Math.Cos(nextAngle));
                points[i] = new SDL.SDL_Point() { x = drawX, y = drawY };
            }
*/
            for (int i = 1; i < totalSegments; i++)
            {
                double nextAngle = startAngleRadians + incrementAngle * i;
                drawX = posX + (int)Math.Round(xWidth * Math.Sin(nextAngle));
                drawY = posY + (int)Math.Round(yWidth * Math.Cos(nextAngle));
                //SDL.SDL_RenderDrawPoint(renderer, drawX, drawY);
                SDL.SDL_RenderDrawLine(renderer, lastX, lastY, drawX, drawY);
                //SDL.SDL_RenderDrawLines(renderer, points, _numberOfArcSegments);
                lastX = drawX;
                lastY = drawY;
            }

        }


        public static void DrawAlphaFadeArc(IntPtr rendererPtr, int posX, int posY, double xWidth, double yWidth, double startAngleRadians, double arcAngleRadians, byte startAlpha, byte endAlpha)
        {
            byte r, g, b, a;
            SDL.SDL_GetRenderDrawColor(rendererPtr, out r, out g, out b, out a);

            SDL.SDL_BlendMode blendMode;
            SDL.SDL_GetRenderDrawBlendMode(rendererPtr, out blendMode);

            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
            byte _numberOfArcSegments = 254;


            byte alpha = endAlpha;

            double incrementAngle = (Math.PI * 2.0) / (_numberOfArcSegments);

            int lastX = posX + (int)Math.Round(xWidth * Math.Sin(startAngleRadians));
            int lastY = posY + (int)Math.Round(yWidth * Math.Cos(startAngleRadians));
            int drawX;
            int drawY;
            int totalSegments = (int)(arcAngleRadians / incrementAngle);
            double alphaIncrement = (startAlpha - endAlpha) / totalSegments;
            for (int i = 1; i < totalSegments; i++)
            {
                alpha += (byte)(alphaIncrement);
                SDL.SDL_SetRenderDrawColor(rendererPtr, r, g, b, alpha);

                double nextAngle = startAngleRadians + incrementAngle * i;
                drawX = posX + (int)Math.Round(xWidth * Math.Sin(nextAngle));
                drawY = posY + (int)Math.Round(yWidth * Math.Cos(nextAngle));
                SDL.SDL_RenderDrawLine(rendererPtr, lastX, lastY, drawX, drawY);
                lastX = drawX;
                lastY = drawY;
            }

            SDL.SDL_SetRenderDrawColor(rendererPtr, r, g, b, a); //set the colour back to what it was origionaly
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, blendMode);
        }

        /// <summary>
        /// Draws an arc segment.
        /// </summary>
        /// <param name="renderer">Renderer.</param>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <param name="rad">The radius pixels of the arc</param>
        /// <param name="start">Starting radius in degrees of the arc. 0 degrees is down, increasing counterclockwise.</param>
        /// <param name="end">Ending radius in degrees of the arc. 0 degrees is down, increasing counterclockwise.</param>
        /// <param name="r">The red component.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
        /// <param name="a">The alpha component.</param>
        public static void DrawArcSegment(IntPtr renderer, short x, short y, short rad, short start, short end, byte r, byte g, byte b, byte a)
        {

            SDL.SDL_RenderDrawPoint(renderer, x, y);
        }
    }
}
