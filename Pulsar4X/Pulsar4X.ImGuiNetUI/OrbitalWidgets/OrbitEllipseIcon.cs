using System;
using Pulsar4X.ECSLib;
using Pulsar4X.Orbital;
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
    /// this list is stored in world coordinates since view coordinates will change freqently with zoom, pan etc. 
    /// we also store the orbitAngle (Longitude of the periapsis, which should be the Argument of Periapsis + Longdidtude of the Accending Node, in 2d orbits we just add these together and use the LoP)  
    /// On Update we calculate the angle from the center of the ellipse to the orbiting entity. TODO: (this *should* only be called when the game updates, but is currently called each frame) 
    /// On Draw we translate the points to correct for the position in world view, and for the viewscreen and camera positions as well as zoom.
    /// We then find the index in the Point Array (created in Setup) that will be where the orbiting entity is, using the angle from the center of the ellipse to the orbiting entity. 
    /// Using this index we create a tempory array of only the points which will be in the drawn portion of the ellipse (UserOrbitSettings.EllipseSweepRadians) which start from where the entity should be.  
    /// We start drawing segments from where the planet will be, and decrease the alpha channel for each segment.
    /// On ajustments to settings from the user, we re-calculate needed info for that. (if the number of segments change, we have to recreate the point indiex so we run setup in that case) 
    /// </summary>
    public class OrbitEllipseIcon : OrbitIconBase
    {



        internal OrbitEllipseIcon(EntityState entityState, List<List<UserOrbitSettings>> settings): base(entityState, settings)
        {

            TrajectoryType = UserOrbitSettings.OrbitTrajectoryType.Elliptical;


            UpdateUserSettings();
            CreatePointArray();
            OnPhysicsUpdate();

        }



        protected override void CreatePointArray()
        {
            _points = new Vector2[_numberOfArcSegments + 1];

            var loAN = -_orbitDB.LongitudeOfAscendingNode;
            var incl = _orbitDB.Inclination;
            var mtxloan = Matrix3d.IDRotateZ(loAN);
            var mtxincl = Matrix3d.IDRotateX(incl); 
            var mtxaop = Matrix3d.IDRotateZ(_aop);

            //var mtx =  mtxaop * mtxincl * mtxloan;
            var mtx =  mtxaop * mtxloan;
            double angle = 0;

            var coslop = 1 * Math.Cos(_loP_radians);
            var sinlop = 1 * Math.Sin(_loP_radians);
            //TODO: figure out propper matrix rotations for this, will be a bit more elegent. 
            for (int i = 0; i < _numberOfArcSegments + 1; i++)
            {

                double x1 = SemiMaj *  Math.Sin(angle) - _linearEccentricity; //we add the focal distance so the focal point is "center"
                double y1 = SemiMinor * Math.Cos(angle);
                
                double x2 = (x1 * coslop) - (y1 * sinlop);
                double y2 = (x1 * sinlop) + (y1 * coslop);
                
                //Vector3 pnt = new Vector3(x1, y1, 0);
                //pnt = mtx.Transform(pnt);
                //Points[i] = new PointD() {X = pnt.X, Y = pnt.Y};
                _points[i] = new Vector2() { X = x2, Y = y2 };
                angle += _segmentArcSweepRadians;
            }

            if (IsRetrogradeOrbit)
            {
                var mtxr1 = Matrix3d.IDRotateZ(-_loP_radians);
                var mtxr2 = Matrix3d.IDRotateZ(_loP_radians);
                var mtxr = mtxr1 * mtxincl * mtxr2;
                for (int i = 0; i < _points.Length; i++)
                {
                    var pnt = mtxr.Transform(new Vector3(_points[i].X, _points[i].Y, 0));
                        _points[i] = new Vector2() {X = pnt.X, Y = pnt.Y};
                }
            }
            //TODO: try a Chaikins curve for this and increase the points depending on zoom and curviture.   

        }





        /*
         * this gets the index by attempting to find the angle between the body and the center of the ellipse. possibly faster, but math is hard. 
         * TODO: try doing this using EccentricAnomaly. 
        public override void OnPhysicsUpdate()
        {

            //adjust so moons get the right positions  
            Vector4 pos = _bodyPositionDB.AbsolutePosition;// - ParentPositionDB.AbsolutePosition;   
            PointD pointD = new PointD() { x = pos.X, y = pos.Y };

             
            //adjust for focal point
            pos.X += _focalDistance; 

            //rotate to the LonditudeOfPeriapsis. 
            double x2 = (pos.X * Math.Cos(-_orbitAngleRadians)) - (pos.Y * Math.Sin(-_orbitAngleRadians));
            double y2 = (pos.X * Math.Sin(-_orbitAngleRadians)) + (pos.Y * Math.Cos(-_orbitAngleRadians));

            _ellipseStartArcAngleRadians = (float)(Math.Atan2(y2, x2));  //Atan2 returns a value between -180 and 180; 

            //PointD pnt = Points.OrderBy(p => CalcDistance(p, new PointD() {x = pos.X, y = pos.Y })).First();

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
            Vector3 pos = BodyPositionDB.RelativePosition; 
            _bodyrelativePos = new Vector2() { X = pos.X, Y = pos.Y };
            var apos = BodyPositionDB.AbsolutePosition;
            _bodyAbsolutePos = new Vector2(apos.X, apos.Y);
            
            //we find the point in the ellipse which is closest to the body so we can start drawing from the body.
            double minDist = (_bodyrelativePos - _points[_index]).Length();

            for (int i =0; i < _points.Count(); i++)
            {
                double dist = (_bodyrelativePos - _points[i]).Length();
                if (dist < minDist)
                {
                    minDist = dist;
                    _index = i;
                }
            }
        }
        
        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            //resize for zoom
            //translate to position
            
            var foo = camera.ViewCoordinateV2_m(WorldPosition_m); //camera position and zoom
            
            var trns = Matrix.IDTranslate(foo.X, foo.Y);
            var scAU = Matrix.IDScale(6.6859E-12, 6.6859E-12);
            var mtrx =  scAU * matrix * trns; //scale to au, scale for camera zoom, and move to camera position and zoom

            int index = _index;
            var spos = camera.ViewCoordinateV2_m(_bodyAbsolutePos);

            //_drawPoints[0] = mtrx.TransformToSDL_Point(_bodyrelativePos.X, _bodyrelativePos.Y);
            _drawPoints[0] = new SDL.SDL_Point(){x = (int)spos.X, y = (int)spos.Y};
            for (int i = 1; i < _numberOfDrawSegments; i++)
            {
                if (index < _numberOfArcSegments - 1)

                    index++;
                else
                    index = 0;
                
                _drawPoints[i] = mtrx.TransformToSDL_Point(_points[index].X, _points[index].Y);
            }
        }



        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            //now we draw a line between each of the points in the translatedPoints[] array.
            if (_drawPoints.Count() < _numberOfDrawSegments - 1)
                return;
            float alpha = _userSettings.MaxAlpha;
            for (int i = 0; i < _numberOfDrawSegments - 1; i++)
            {
                SDL.SDL_SetRenderDrawColor(rendererPtr, _userSettings.Red, _userSettings.Grn, _userSettings.Blu, (byte)alpha);//we cast the alpha here to stop rounding errors creaping up. 
                SDL.SDL_RenderDrawLine(rendererPtr, _drawPoints[i].x, _drawPoints[i].y, _drawPoints[i + 1].x, _drawPoints[i +1].y);
                alpha -= _alphaChangeAmount; 
            }
        }
    }


}
