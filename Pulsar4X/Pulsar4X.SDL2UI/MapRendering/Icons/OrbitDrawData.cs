using System;
using Pulsar4X.ECSLib;
using SDL2;
using ImGuiNET;
using System.Collections.Generic;

namespace Pulsar4X.SDL2UI
{
    public interface IDrawData
    {
        void Update();
        void Draw(IntPtr rendererPtr, Matrix matrix);
    }


    /// <summary>
    /// Orbit draw data.
    /// How this works:
    /// First, we set up all the static non changing variables from the entites datablobs
    /// On Setup we create a list of points for the full ellipse, as if it was at 0,0 world coordinates. 
    /// On Update we calculate where the elipse should be painted in world coordinates, and recalc any user configerable variables.
    /// On Draw we translate the points both to correct for the position in world view, and for the viewscreen. 
    /// We start drawing segments from where the planet will be, and decrease the alpha channel for each segment. only drawing the number of segments required. 
    /// </summary>
    public class OrbitDrawData : Icon
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
        List<SDL.SDL_Point> points = new List<SDL.SDL_Point>();
        #endregion

        #region Dynamic Properties
        //focal point of the elipse this is the point the entity is orbiting around - this will change if the parent is inturn orbiting something.
        double _focalX;
        double _focalY;
        double _ctrPosX;
        double _ctrPosY;
        float _elipseStartArcAngle;
        float _segmentArcSweepAngle;
        int _numberOfDrawSegments;
        //user adjustable
        float _ellipseSweepAngle;
        byte _numberOfArcSegments = 255; //this maybe needs adjusting for zoom level. 
        SDL.SDL_Color Colour = new SDL.SDL_Color() { r = 0, g = 0, b = 0, a = 255 };
        byte _alphaChangeAmount;
        #endregion

        internal OrbitDrawData(Entity entity): base(entity.GetDataBlob<PositionDB>())
        {
            _orbitDB = entity.GetDataBlob<OrbitDB>();
            _bodyPositionDB = entity.GetDataBlob<PositionDB>();
            _parentPositionDB = _orbitDB.Parent.GetDataBlob<PositionDB>();
            _positionDB = _parentPositionDB;
            Setup();
        }

        internal OrbitDrawData(OrbitDB orbitDB, PositionDB positionDB, PositionDB parentPosDB): base(parentPosDB)
        {
            _orbitDB = orbitDB;
            _bodyPositionDB = positionDB;
            _parentPositionDB = parentPosDB;
            Setup();
        }

        private void Setup()
        {
            _orbitElipseMajor = (float)_orbitDB.SemiMajorAxis * 2; //Major Axis
            _orbitElipseSemiMaj = _orbitElipseMajor * 0.5f;
            _orbitElipseMinor = (float)Math.Sqrt((_orbitDB.SemiMajorAxis * _orbitDB.SemiMajorAxis) * (1 - _orbitDB.Eccentricity * _orbitDB.Eccentricity)) * 2;
            _orbitElipseSemiMinor = _orbitElipseMinor * 0.5f;
            _orbitAngle = (float)(_orbitDB.LongitudeOfAscendingNode + _orbitDB.ArgumentOfPeriapsis * 2); //This is the LoP + AoP.
            _radianAngle = (float)(_orbitAngle * Math.PI / 180);
            _focalDistance = (float)(_orbitDB.Eccentricity * _orbitElipseMajor * 0.5f); //linear ecentricity

            _focalX = 0;
            _focalY = 0;

            Update();

            for (int i = 0; i < _numberOfArcSegments; i++)
            {
                double angle = (Math.PI * 2.0) / (_numberOfArcSegments);
                SDL.SDL_Point point = new SDL.SDL_Point();
                point.x = (int)(_ctrPosX + Math.Round(_orbitElipseSemiMaj * Math.Sin(angle * i)));
                point.y = (int)(_ctrPosY + Math.Round(_orbitElipseSemiMinor * Math.Cos(angle * i)));
                points.Add(point);
            }


        }



        public void Update()
        {
            //update world position
            _focalX = _parentPositionDB.X;
            _focalY = _parentPositionDB.Y;
            _ctrPosX = _focalX - _focalDistance;
            _ctrPosY = _focalY;

            //calculate the angle to the entity's position
            Vector4 pos = _bodyPositionDB.AbsolutePosition - _parentPositionDB.AbsolutePosition; //adjust so moons get the right positions    
            double normalX = (pos.X * Math.Cos(-_radianAngle)) - (pos.Y * Math.Sin(-_radianAngle));
            double normalY = (pos.X * Math.Sin(-_radianAngle)) + (pos.Y * Math.Cos(-_radianAngle));
            normalX += _focalDistance; //adjust for focal point
            normalY *= (_orbitElipseMajor / _orbitElipseMinor); //adjust for elliptic angle. 
            _elipseStartArcAngle = (float)(Math.Atan2(normalY, normalX) * 180 / Math.PI);

            //calculate anything that could have changed fom the user. 
            _segmentArcSweepAngle = (float)(Math.PI * 2.0) / (_numberOfArcSegments);
            _numberOfDrawSegments = (int)(_numberOfArcSegments / _ellipseSweepAngle);
            _alphaChangeAmount = (byte)(255 / _numberOfDrawSegments);
        }

        public void Draw(IntPtr renderer, Matrix matrix)
        {

            byte oR, oG, oB, oA;
            SDL.SDL_GetRenderDrawColor(renderer, out oR, out oG, out oB, out oA);
            SDL.SDL_BlendMode blendMode;
            SDL.SDL_GetRenderDrawBlendMode(renderer, out blendMode);
            SDL.SDL_SetRenderDrawBlendMode(renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

            //do matrix transformations for camera position and zoom
            int xTranslate = (int)(_focalX); //+ camera position
            int yTranslate = (int)(_focalY); //+ camera position

            int index = (int)(_elipseStartArcAngle / _segmentArcSweepAngle); //get the position in the point array we want to start drawing from
            byte alpha = 255;
            for (int i = 0; i < _numberOfArcSegments; i++)
            {
                SDL.SDL_SetRenderDrawColor(renderer, Colour.r, Colour.g, Colour.b, alpha);
                SDL.SDL_RenderDrawLine(renderer, points[index].x + xTranslate, points[index].y + yTranslate, points[index + 1].x + xTranslate, points[index + 1].y + yTranslate);

                if (index < _numberOfArcSegments)
                    index++;
                else
                    index = 0;
                alpha -= _alphaChangeAmount; 

            }
            SDL.SDL_SetRenderDrawColor(renderer, oR, oG, oB, oA);
            SDL.SDL_SetRenderDrawBlendMode(renderer, blendMode);
        }
    }
}
