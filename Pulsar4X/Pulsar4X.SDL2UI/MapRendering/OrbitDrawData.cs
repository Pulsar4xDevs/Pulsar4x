using System;
using Pulsar4X.ECSLib;
using SDL2;
using ImGuiNET;
using System.Collections.Generic;

namespace Pulsar4X.SDL2UI
{
    public interface DrawData
    {
        void Update();
        void Draw(IntPtr rendererPtr);
    }
    public class OrbitDrawData : DrawData
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
        float _startArcAngle;
        float _endArcAngle;
        //user adjustable
        float _sweepAngle;
        byte _numberOfAlphaSegments = 255;
        byte _numberOfArcSegments = 255; //this maybe needs adjusting for zoom level. 
        #endregion

        internal OrbitDrawData(Entity entity)
        {
            _orbitDB = entity.GetDataBlob<OrbitDB>();
            _bodyPositionDB = entity.GetDataBlob<PositionDB>();
            _parentPositionDB = _orbitDB.Parent.GetDataBlob<PositionDB>();
            Setup();
        }

        internal OrbitDrawData(OrbitDB orbitDB, PositionDB positionDB, PositionDB parentPosDB)
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

            /* I can maybe increase the performance by creating a list of points, then in the Draw loop, do a translation for the position to match the parent's position. 
             * would still need to find which arc the entity is in and adjust the colour alpha though.   
            Update();
            for (int i = 0; i < _numberOfArcSegments; i++)
            {
                double angle = (Math.PI * 2.0) / (_numberOfArcSegments);
                SDL.SDL_Point point = new SDL.SDL_Point();
                point.x = (int)(_ctrPosX + Math.Round(_orbitElipseSemiMaj * Math.Sin(angle * i)));
                point.y = (int)(_ctrPosY + Math.Round(_orbitElipseSemiMinor * Math.Cos(angle * i)));
                points.Add(point);
            }
            */
        }

        public void Update()
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

        public void Draw(IntPtr renderer)
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
    }

    class AtmoDrawData : DrawData
    {
        public void Draw(IntPtr rendererPtr)
        {
            byte oR, oG, oB, oA;
            SDL.SDL_GetRenderDrawColor(rendererPtr, out oR, out oG, out oB, out oA);



            SDL.SDL_SetRenderDrawColor(rendererPtr, oR, oG, oB, oA);
        }

        public void Update()
        {
            throw new NotImplementedException();
        }
    }

    class StarDrawData : DrawData
    {
        double _tempK;

        SDL.SDL_Color _color;

        PositionDB position;
        int posX;
        int posY;

        List<Shape> Shapes = new List<Shape>();

        public StarDrawData(Entity entity)
        {
            position = entity.GetDataBlob<PositionDB>();
            StarInfoDB starInfo = entity.GetDataBlob<StarInfoDB>();
            _tempK = starInfo.Temperature + 273.15;

            double calcTemp = GMath.Clamp(_tempK, 1000, 40000);
            calcTemp = calcTemp / 100;

            //Red
            if (calcTemp <= 660)
                _color.r = 255;
            else 
            {
                _color.r = (byte)(329.698727446 * Math.Pow(calcTemp - 60, -0.1332047592));
            }

            //Green
            if (calcTemp <= 66)
            {
                _color.g = (byte)(99.4708025861 * Math.Log(calcTemp) - 161.1195681661);
            }
            else 
            {
                _color.g = (byte)(288.1221695283 * Math.Pow(calcTemp - 60, -0.0755148492));
            }

            //Blue
            if (calcTemp >= 66)
                _color.b = 255;
            else if (calcTemp <= 19)
                _color.b = 0;
            else
            {
                _color.b = (byte)(138.5177312231 * Math.Log(calcTemp - 10) - 305.0447927307);
            }
            _color.a = 255;

    
            byte spikes = (byte)(starInfo.SpectralType + 4) ;
            byte spikeDepth = 8;
            double arc = (2 * Math.PI) / spikes;
            double startAngle = 1.5708 - arc / 2;
            List<SDL.SDL_Point> shapePoints = new List<SDL.SDL_Point>();
            for (int i = 0; i < spikes; i++)
            {
                //need rotation transform to rotate it at i * arc; 

                shapePoints.AddRange(CreatePrimitiveShapes.CreateArc(32, 0, 32 - spikeDepth, 32 + spikeDepth, startAngle, arc, 32)); //32 segments is probilby way overkill maybe adjust this by the camera zoom level?
                startAngle += arc;
            }


            Shapes.Add(new Shape(){ Color = _color, Points = shapePoints.ToArray()});

        }

        public void Update()
        {
            posX = (int)position.AbsolutePosition.X;
            posY = (int)position.AbsolutePosition.Y;
        }

        public void Draw(IntPtr rendererPtr)
        {
            byte oR, oG, oB, oA;
            SDL.SDL_GetRenderDrawColor(rendererPtr, out oR, out oG, out oB, out oA);

            foreach (var shape in Shapes)
            {
                SDL.SDL_SetRenderDrawColor(rendererPtr, shape.Color.r, shape.Color.g, shape.Color.b, shape.Color.a);

                for (int i = 0; i < shape.Points.Length - 1; i++)
                {
                    SDL.SDL_RenderDrawLine(rendererPtr, shape.Points[i].x, shape.Points[i].y, shape.Points[i + 1].x, shape.Points[i + 1].y);
                }
            }
            SDL.SDL_SetRenderDrawColor(rendererPtr, oR, oG, oB, oA);
        }
    }
}
