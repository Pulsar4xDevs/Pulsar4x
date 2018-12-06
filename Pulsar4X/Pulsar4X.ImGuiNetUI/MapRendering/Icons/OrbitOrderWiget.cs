using System;
using Pulsar4X.ECSLib;
using SDL2;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.SDL2UI
{

    /// <summary>
    /// Orbit draw data.
    /// How this works:
    /// First, we set up all the static non changing variables from the entites datablobs.
    /// On Setup we create a list of points for the full ellipse, as if it was orbiting around 0,0 world coordinates. (focalPoint).
    /// as well as the orbitAngle (Longitude of the periapsis, which should be the Argument of Periapsis + Longdidtude of the Accending Node, in 2d orbits we just add these together and use the LoP)  
    /// On Update we calculate the angle from the center of the ellipse to the orbiting entity. TODO: (this *should* only be called when the game updates, but is currently called each frame) 
    /// On Draw we translate the points to correct for the position in world view, and for the viewscreen and camera positions as well as zoom.
    /// We then find the index in the Point Array (created in Setup) that will be where the orbiting entity is, using the angle from the center of the ellipse to the orbiting entity. 
    /// Using this index we create a tempory array of only the points which will be in the drawn portion of the ellipse (UserOrbitSettings.EllipseSweepRadians) which start from where the entity should be.  
    /// We start drawing segments from where the planet will be, and decrease the alpha channel for each segment.
    /// On ajustments to settings from the user, we re-calculate needed info for that. (if the number of segments change, we have to recreate the point indiex so we run setup in that case) 
    /// Currently we're not distingishing between clockwise and counter clockwise orbits, not sure if the engine even does counterclockwise, will have to check that and fix. 
    /// </summary>
    public class OrbitOrderWiget : Icon
    {

        enum SetModes
        {
            Nothing,
            SetMajor,
            SetMinor,
            SetLoP,//Longditude of Periapsis
        }

        #region Static properties

        PositionDB _bodyPositionDB;

        internal PointD Apoapsis;
        internal PointD Periapsis;
        internal double OrbitEllipseSemiMaj;
        internal double OrbitEllipseSemiMinor;

        internal double OrbitAngleRadians; //the orbit is an ellipse which is rotated arround one of the focal points. 

        double _linearEccentricity; //distance from the center of the ellpse to one of the focal points. 

        PointD[] _points; //we calculate points around the ellipse and add them here. when we draw them we translate all the points. 
        SDL.SDL_Point[] _drawPoints;
        //sphere of influance radius, if the entity is outside this, then this is affected by the parent (or other) gravitational body
        double _soiWorldRadius; 
        float _soiViewRadius;
        //this is the size of the planet that we're trying to orbit, 
        //if the entity is inside this... currently nothing happens, 
        //but it shoudl be bad. we should not allow translations inside this radius, and warn if the orbit goes within this radius. 
        double _targetWorldRadius;
        float _targetViewRadius;
        #endregion

        #region Dynamic Properties
        //change each game update
        float _ellipseStartArcAngleRadians;
        int _index;
        bool IsClockwiseOrbit = true;
        public float EllipseSweepRadians = 4.71239f;

        //32 is a good low number, slightly ugly.  180 is a little overkill till you get really big orbits. 
        public byte NumberOfArcSegments = 180; 

        public byte Red = 0;
        public byte Grn = 255;
        public byte Blu = 0;
        public byte MaxAlpha = 255;
        public byte MinAlpha = 0; 

        //change after user makes adjustments:
        byte _numberOfArcSegments = 180; //how many segments in a complete 360 degree ellipse. this is set in UserOrbitSettings, localy adjusted because the whole point array needs re-creating when it changes. 
        int _numberOfDrawSegments; //this is now many segments get drawn in the ellipse, ie if the _ellipseSweepAngle or _numberOfArcSegments are less, less will be drawn.
        float _segmentArcSweepRadians; //how large each segment in the drawn portion of the ellipse.  
        float _alphaChangeAmount;

        double _eccentricity;
        Vector4 _position;

        #endregion

        internal OrbitOrderWiget(Entity targetEntity) : base(targetEntity.GetDataBlob<PositionDB>())
        {

            _bodyPositionDB = targetEntity.GetDataBlob<PositionDB>();

            OrbitEllipseSemiMaj = 20000;
            OrbitEllipseSemiMinor = 20000;

            _linearEccentricity = 0;

            _soiWorldRadius = GMath.GetSOI(targetEntity);
            _targetWorldRadius = targetEntity.GetDataBlob<MassVolumeDB>().Radius;
            Setup();

        }

        public OrbitOrderWiget(OrbitDB orbitDB): base(orbitDB.Parent.GetDataBlob<PositionDB>())
        {
            var targetEntity = orbitDB.Parent;
            _bodyPositionDB = targetEntity.GetDataBlob<PositionDB>();

            OrbitEllipseSemiMaj = (float)orbitDB.SemiMajorAxis;
            _eccentricity = orbitDB.Eccentricity;
            EllipseMath.SemiMinorAxis(OrbitEllipseSemiMaj, _eccentricity);
            _linearEccentricity = (float)(orbitDB.Eccentricity * OrbitEllipseSemiMaj);

            _soiWorldRadius =  GMath.GetSOI(targetEntity);
            _targetWorldRadius = targetEntity.GetDataBlob<MassVolumeDB>().Radius;
            Setup();
        }


        void Setup()
        {
            _numberOfArcSegments = NumberOfArcSegments;
            CreatePointArray();

            _segmentArcSweepRadians = (float)(Math.PI * 2.0 / _numberOfArcSegments);
            _numberOfDrawSegments = (int)Math.Max(1, (EllipseSweepRadians / _segmentArcSweepRadians));
            _alphaChangeAmount = ((float)MaxAlpha - MinAlpha) / _numberOfDrawSegments;
            _ellipseStartArcAngleRadians = (float)OrbitAngleRadians;



            CreatePointArray();

            OnPhysicsUpdate();

        }


        public void SetParametersFromKeplerElements(KeplerElements ke, Vector4 position)
        {

            _eccentricity = ke.Eccentricity;
            _linearEccentricity = ke.LinierEccentricity;


            OrbitEllipseSemiMaj = ke.SemiMajorAxis; 
            OrbitEllipseSemiMinor = ke.SemiMinorAxis;

            Periapsis = new PointD()
            {
                X = Math.Sin(ke.TrueAnomaly) * ke.Periapsis,
                Y = Math.Cos(ke.TrueAnomaly) * ke.Periapsis

            };
            Apoapsis = new PointD() {
                X = Math.Sin(ke.TrueAnomaly) * ke.Apoapsis,
                Y = Math.Cos(ke.TrueAnomaly) * ke.Apoapsis
            };

            if (ke.Inclination > Math.PI * 0.5 && ke.Inclination < Math.PI * 1.5) //ke inclination is in radians.
            {
                IsClockwiseOrbit = false;
                OrbitAngleRadians = ke.LoAN - ke.AoP;
            }
            else
            {

                OrbitAngleRadians = ke.LoAN + ke.AoP;
            }
            _position = position;
            OnPhysicsUpdate();
        }

        void CreatePointArray()
        {
            _points = new PointD[_numberOfArcSegments + 1];
            double angle = 0;
            for (int i = 0; i < _numberOfArcSegments + 1; i++)
            {

                double x1 = OrbitEllipseSemiMaj * Math.Sin(angle) - _linearEccentricity; //we add the linearEccentricity so the focal point is "center"
                double y1 = OrbitEllipseSemiMinor * Math.Cos(angle);

                //rotates the points to allow for the LongditudeOfPeriapsis. 
                double x2 = (x1 * Math.Cos(OrbitAngleRadians)) - (y1 * Math.Sin(OrbitAngleRadians));
                double y2 = (x1 * Math.Sin(OrbitAngleRadians)) + (y1 * Math.Cos(OrbitAngleRadians));
                angle += _segmentArcSweepRadians;
                _points[i] = new PointD() { X = x2, Y = y2 };
            }
        }




        /*
         * this gets the index by attempting to find the angle between the body and the center of the ellipse. possibly faster, but math is hard. 
        public override void OnPhysicsUpdate()
        {

            //adjust so moons get the right positions  
            Vector4 pos = _bodyPositionDB.AbsolutePosition;// - _positionDB.AbsolutePosition;   
            PointD pointD = new PointD() { x = pos.X, y = pos.Y };

             
            //adjust for focal point
            pos.X += _focalDistance; 

            //rotate to the LonditudeOfPeriapsis. 
            double x2 = (pos.X * Math.Cos(-_orbitAngleRadians)) - (pos.Y * Math.Sin(-_orbitAngleRadians));
            double y2 = (pos.X * Math.Sin(-_orbitAngleRadians)) + (pos.Y * Math.Cos(-_orbitAngleRadians));

            _ellipseStartArcAngleRadians = (float)(Math.Atan2(y2, x2));  //Atan2 returns a value between -180 and 180; 

            //PointD pnt = _points.OrderBy(p => CalcDistance(p, new PointD() {x = pos.X, y = pos.Y })).First();

            //get the indexPosition in the point array we want to start drawing from: this should be the segment where the planet is. 
            double unAdjustedIndex = (_ellipseStartArcAngleRadians / _segmentArcSweepRadians);
            while (unAdjustedIndex < 0)
            {
                unAdjustedIndex += (2 * Math.PI);
            }
            _index = (int)unAdjustedIndex;

        }
*/

        public override void OnPhysicsUpdate()
        {

  

            PointD pointD = new PointD() { X = _position.X, Y = _position.Y };

            double minDist = CalcDistance(pointD, _points[_index]);

            for (int i = 0; i < _points.Count(); i++)
            {
                double dist = CalcDistance(pointD, _points[i]);
                if (dist < minDist)
                {
                    minDist = dist;
                    _index = i;
                }
            }
        }

        double CalcDistance(PointD p1, PointD p2)
        {
            return PointDFunctions.Length(PointDFunctions.Sub(p1, p2));
        }


        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            CreatePointArray();
            ViewScreenPos = matrix.Transform(WorldPosition.X, WorldPosition.Y);//sets the zoom position. 


            _soiViewRadius = camera.ViewDistance(_soiWorldRadius);
            _targetViewRadius = camera.ViewDistance(_targetWorldRadius);
            int index = _index;
            var camerapoint = camera.CameraViewCoordinate();
            //ViewScreenPos += camerapoint;

            var vsp = new SDL.SDL_Point()
            {
                x = ViewScreenPos.x + camerapoint.x,
                y = ViewScreenPos.y + camerapoint.y
            };
            ViewScreenPos = vsp;
            PointD translated;
            _drawPoints = new SDL.SDL_Point[_numberOfDrawSegments];
            for (int i = 0; i < _numberOfDrawSegments; i++)
            {

                if (IsClockwiseOrbit)
                {
                    if (index < _numberOfArcSegments - 1)

                        index++;
                    else
                        index = 0;
                }
                else if (index > 0)
                {
                    index--;
                }
                else
                    index = _numberOfArcSegments - 1;

                translated = matrix.TransformD(_points[index].X, _points[index].Y); //add zoom transformation. 

                int x = (int)(ViewScreenPos.x + translated.X);
                int y = (int)(ViewScreenPos.y + translated.Y);

                _drawPoints[i] = new SDL.SDL_Point() { x = x, y = y };
            }
        }



        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            //now we draw a line between each of the points in the translatedPoints[] array.
            float alpha = MaxAlpha;
            for (int i = 0; i < _numberOfDrawSegments - 1; i++)
            {
                SDL.SDL_SetRenderDrawColor(rendererPtr, Red, Grn, Blu, (byte)alpha);//we cast the alpha here to stop rounding errors creaping up. 
                SDL.SDL_RenderDrawLine(rendererPtr, _drawPoints[i].x, _drawPoints[i].y, _drawPoints[i + 1].x, _drawPoints[i + 1].y);
                alpha -= _alphaChangeAmount;
            }

            SDL.SDL_SetRenderDrawColor(rendererPtr, 0, 50, 100, 100);
            //DrawPrimitive.DrawFilledCircle(rendererPtr ,ViewScreenPos.x , ViewScreenPos.y, (int)_soiViewRadius);
            DrawPrimitive.DrawEllipse(rendererPtr, ViewScreenPos.x, ViewScreenPos.y, _soiViewRadius, _soiViewRadius);
            SDL.SDL_SetRenderDrawColor(rendererPtr, 100, 0, 0, 100);
            DrawPrimitive.DrawEllipse(rendererPtr, ViewScreenPos.x, ViewScreenPos.y, _targetViewRadius, _targetViewRadius);
        }
    }
}
