using System;
using Pulsar4X.ECSLib;
using SDL2;
using ImGuiNET;
using System.Collections.Generic;

namespace Pulsar4X.SDL2UI
{

    /// <summary>
    /// Orbit draw data.
    /// How this works:
    /// First, we set up all the static non changing variables from the entites datablobs.
    /// On Setup we create a list of points for the full ellipse, as if it was orbiting around 0,0 world coordinates. (focalPoint).
    /// as well as the orbitAngle (Longitude of the periapsis, which should be the Argument of Periapsis + Longdidtude of the Accending Node, in 2d orbits we just add these together and use the LoP)  
    /// On Update we calculate the angle from the center of the ellipse to the planet position. TODO: (this *should* only be called when the game updates, but is currently called each frame) 
    /// On Draw we translate the points to correct for the position in world view, and for the viewscreen and camera positions as well as zoom.
    /// creating a tempory array of only the viewscreen point locations, which start from where the entity should be.  
    /// We start drawing segments from where the planet will be, and decrease the alpha channel for each segment.
    /// On asjustments to settings from the user, we re-calculate needed info for that. 
    /// </summary>
    public class OrbitIcon : Icon
    {

        
        #region Static properties
        OrbitDB _orbitDB;
        PositionDB _bodyPositionDB;
        PositionDB _parentPositionDB;
        float _orbitEllipseMajor;
        float _orbitEllipseSemiMaj;
        float _orbitEllipseMinor;
        float _orbitEllipseSemiMinor;
        float _orbitAngleDegrees; //the orbit is an ellipse which is rotated arround one of the focal points. 
        float _orbitAngleRadians; //the orbit is an ellipse which is rotated arround one of the focal points. 
        float _focalDistance; //distance from the center of the ellpse to one of the focal points. 
        PointD[] _points; //we calculate points around the ellipse and add them here. when we draw them we translate all the points. 
        #endregion

        #region Dynamic Properties
        //change each game update
        float _ellipseStartArcAngleRadians;
        float _segmentArcSweepRadians;


        //user adjustable variables:
        UserOrbitSettings _userSettings; 


        //change after user makes adjustments:
        byte _numberOfArcSegments = 180; //how many segments in a complete 360 degree ellipse. this is set in UserOrbitSettings, localy adjusted because the whole point array needs re-creating when it changes. 
        int _numberOfDrawSegments; //this is now many segments get drawn in the ellipse, ie if the _ellipseSweepAngle or _numberOfArcSegments are less, less will be drawn.
        float _alphaChangeAmount;


        #endregion

        internal OrbitIcon(Entity entity, UserOrbitSettings settings): base(entity.GetDataBlob<PositionDB>())
        {
            _userSettings = settings;
            _orbitDB = entity.GetDataBlob<OrbitDB>();
            _bodyPositionDB = entity.GetDataBlob<PositionDB>();
            if (_orbitDB.Parent == null)
            {
                _positionDB = _bodyPositionDB;
                _parentPositionDB = _positionDB;
            }
            else
            {
                _parentPositionDB = _orbitDB.Parent.GetDataBlob<PositionDB>();
                _positionDB = _parentPositionDB;
            }
            Setup();
        }

        internal OrbitIcon(OrbitDB orbitDB, PositionDB positionDB, PositionDB parentPosDB): base(parentPosDB)
        {
            _orbitDB = orbitDB;
            _bodyPositionDB = positionDB;
            _parentPositionDB = parentPosDB;
            Setup();
        }

        private void Setup()
        {
            //ShapesScaleWithZoom = true;
            _orbitEllipseSemiMaj = (float)_orbitDB.SemiMajorAxis;
            _orbitEllipseMajor = _orbitEllipseSemiMaj * 2; //Major Axis
            _orbitEllipseMinor = (float)Math.Sqrt((_orbitDB.SemiMajorAxis * _orbitDB.SemiMajorAxis) * (1 - _orbitDB.Eccentricity * _orbitDB.Eccentricity)) * 2;
            _orbitEllipseSemiMinor = _orbitEllipseMinor * 0.5f;
            _orbitAngleDegrees = (float)Angle.NormaliseDegrees(_orbitDB.LongitudeOfAscendingNode + _orbitDB.ArgumentOfPeriapsis * 2); //This is the LoP + AoP.
            _orbitAngleRadians = (float)Angle.NormaliseRadians( Angle.ToRadians(_orbitAngleDegrees));
            _focalDistance = (float)(_orbitDB.Eccentricity * _orbitEllipseMajor * 0.5f); //linear ecentricity


            _points = new PointD[_numberOfArcSegments + 1];
            double angle = 0;//_orbitAngleRadians;
            double incrementAngle = Math.PI * 2 / _numberOfArcSegments;

            for (int i = 0; i < _numberOfArcSegments + 1; i++)
            {
                
                double x1 = _orbitEllipseSemiMaj * Math.Sin(angle) - _focalDistance; //we add the focal distance so the focal point is "center"
                double y1 = _orbitEllipseSemiMinor * Math.Cos(angle);

                double x2 = (x1 * Math.Cos(_orbitAngleRadians)) - (y1 * Math.Sin(_orbitAngleRadians));
                double y2 = (x1 * Math.Sin(_orbitAngleRadians)) + (y1 * Math.Cos(_orbitAngleRadians));
                angle += incrementAngle;
                _points[i] = new PointD() { x = x2, y = y2 };
            }


            UpdateUserSettings();
            Update();

        }


        /// <summary>
        ///calculate anything that could have changed from the users input. 
        /// </summary>
        public void UpdateUserSettings()
        {
            //if this happens, we need to rebuild the whole set of points. 
            if (_userSettings.NumberOfArcSegments != _numberOfArcSegments)
            {
                _numberOfArcSegments = _userSettings.NumberOfArcSegments;
                Setup(); 
            }

            _segmentArcSweepRadians = (float)(Math.PI * 2.0 / _numberOfArcSegments);
            _numberOfDrawSegments = (int)Math.Max(1, (_userSettings.EllipseSweepRadians / _segmentArcSweepRadians));
            _alphaChangeAmount = ((float)_userSettings.MaxAlpha - _userSettings.MinAlpha) / _numberOfDrawSegments;

        }

        public override void Update()
        {

    
            //adjust so moons get the right positions  
            Vector4 pos = _bodyPositionDB.AbsolutePosition - _parentPositionDB.AbsolutePosition;   
            //adjust for focal point
            pos.X -= _focalDistance; 
            //get the angle to the position.
            _ellipseStartArcAngleRadians = (float)Math.Atan2(pos.Y, pos.X);   


        }

        void debugUpdate() //an attempt at drawing some lines and points to help figuring out the bugs. 
        { 
            
            //calculate the angle to the entity's position
            Vector4 pos = _bodyPositionDB.AbsolutePosition - _parentPositionDB.AbsolutePosition; //adjust so moons get the right positions    

            List<SDL.SDL_Point> points = new List<SDL.SDL_Point>();
            SDL.SDL_Color colour;
            /*
            points.Add(new SDL.SDL_Point() { x = (int)(pos.X - 5), y = (int)(pos.Y) });
            points.Add(new SDL.SDL_Point() { x = (int)(pos.X + 5), y = (int)(pos.Y) });
            points.Add(new SDL.SDL_Point() { x = (int)(pos.X), y = (int)(pos.Y - 5) });
            points.Add(new SDL.SDL_Point() { x = (int)(pos.X), y = (int)(pos.Y + 5) });
            colour = new SDL.SDL_Color() { r = 0, g = 255, b = 0, a = 255 };
            Shapes.Add(new Shape() { Color = colour, Points = points.ToArray() });
            */

            pos.X -= _focalDistance; //adjust for focal point

            //this doesn't work because world positions too small and doubles -> int loss. 
            points = new List<SDL.SDL_Point>(); 
            points.Add(new SDL.SDL_Point() { x = (int)(pos.X - 5), y = (int)(pos.Y) });
            points.Add(new SDL.SDL_Point() { x = (int)(pos.X + 5), y = (int)(pos.Y) });
            points.Add(new SDL.SDL_Point() { x = (int)(pos.X), y = (int)(pos.Y - 5) });
            points.Add(new SDL.SDL_Point() { x = (int)(pos.X), y = (int)(pos.Y + 5) });
            colour = new SDL.SDL_Color() { r = 255, g = 255, b = 0, a = 255 };
            Shapes.Add(new Shape() { Color = colour, Points = points.ToArray() });

            _ellipseStartArcAngleRadians = (float)Angle.NormaliseRadians(Math.Atan2(pos.Y, pos.X));    

 
        }

        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            //debugUpdate();
            Update();
            base.Draw(rendererPtr, camera);
            byte oR, oG, oB, oA;
            SDL.SDL_GetRenderDrawColor(rendererPtr, out oR, out oG, out oB, out oA);
            SDL.SDL_BlendMode blendMode;
            SDL.SDL_GetRenderDrawBlendMode(rendererPtr, out blendMode);
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);


            //get matrix transformations for zoom
            Matrix matrix = new Matrix();
            matrix.Scale(camera.ZoomLevel);

            //get the indexPosition in the point array we want to start drawing from: this should be the segment where the planet is. 
            double normIndex = (_ellipseStartArcAngleRadians / _segmentArcSweepRadians);
            while (normIndex < 0)
            {
                normIndex += (2 * Math.PI);
            }
            int index = (int)normIndex;

            var camerapoint = camera.CameraViewCoordinate();
            var translatedPoints = new SDL.SDL_Point[_numberOfDrawSegments];
            for (int i = 0; i < _numberOfDrawSegments; i++)
            {                   

                if (index < _numberOfArcSegments - 1)
                    index++;
                else
                    index = 0;
                
                var translated = matrix.Transform(_points[index].x, _points[index].y); //add zoom transformation. 

                //translate everything to viewscreen & camera positions
                int x = (int)(ViewScreenPos.x + translated.x + camerapoint.x); 
                int y = (int)(ViewScreenPos.y + translated.y + camerapoint.y);

                translatedPoints[i] = new SDL.SDL_Point() { x = x, y = y };
            }

            //now we draw a line between each of the points in the translatedPoints[] array.
            float alpha = _userSettings.MaxAlpha;
            for (int i = 0; i < _numberOfDrawSegments - 1; i++)
            {
                SDL.SDL_SetRenderDrawColor(rendererPtr, _userSettings.Red, _userSettings.Grn, _userSettings.Blu, (byte)alpha);//we cast the alpha here to stop rounding errors creaping up. 
                SDL.SDL_RenderDrawLine(rendererPtr, translatedPoints[i].x, translatedPoints[i].y, translatedPoints[i + 1].x, translatedPoints[i +1].y);
                alpha -= _alphaChangeAmount; 
            }
            SDL.SDL_SetRenderDrawColor(rendererPtr, oR, oG, oB, oA);
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, blendMode);
        }
    }
}
