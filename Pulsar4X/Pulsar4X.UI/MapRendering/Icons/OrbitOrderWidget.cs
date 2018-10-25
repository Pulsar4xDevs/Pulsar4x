using System;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.VeldridUI
{


    public class OrbitWidgetIconData
    {
        internal IconData IconData;


        internal double _orbitEllipseSemiMaj;
        internal double _orbitEllipseSemiMinor;
        internal Vector4 InsertionPoint;
        internal double _focalDistance; //distance from the center of the ellpse to one of the focal points. 

        //OrbitDB _orbitDB;

        internal double _eccentricity;
        internal float _orbitEllipseMajor;
        //float _orbitEllipseMinor;

        //float _orbitAngleDegrees; //the orbit is an ellipse which is rotated arround one of the focal points. 
        internal double _orbitAngleRadians; //the orbit is an ellipse which is rotated arround one of the focal points. 

        internal PointD[] _points; //we calculate points around the ellipse and add them here. when we draw them we translate all the points. 

        //change each game update
        //internal float _ellipseStartArcAngleRadians;
        internal int _index;



        internal float EllipseSweepRadians = (float)(Math.PI * 2);
        internal byte Red = 255, Grn = 255, Blu = 255;
        internal byte MaxAlpha = 255, MinAlpha = 0;
        internal byte _numberOfArcSegments = 180; //how many segments in a complete 360 degree ellipse. this is set in UserOrbitSettings, localy adjusted because the whole point array needs re-creating when it changes. 
        internal int _numberOfDrawSegments; //this is now many segments get drawn in the ellipse, ie if the _ellipseSweepAngle or _numberOfArcSegments are less, less will be drawn.
        internal float _segmentArcSweepRadians; //how large each segment in the drawn portion of the ellipse.  
        internal float _alphaChangeAmount;

    }

    public class OrbitOrderWidgetIcon 
    {



  

        internal static OrbitWidgetIconData  CreateIcon(PositionDB parentPosition)
        {

            OrbitWidgetIconData icon = new OrbitWidgetIconData();

            icon.IconData.PositionDB = parentPosition;
            icon.IconData.ShouldUpdateFromPositionDB = true;
            icon.IconData.WorldPosition = parentPosition.AbsolutePosition;

            icon._orbitEllipseSemiMaj = 20000;
            icon._orbitEllipseMajor = (float)icon._orbitEllipseSemiMaj * 2; //Major Axis
            icon._orbitEllipseSemiMinor = 10000;
            //_orbitEllipseMinor = 


            icon._segmentArcSweepRadians = (float)(Math.PI * 2.0 / icon._numberOfArcSegments);
            icon._numberOfDrawSegments = (int)Math.Max(1, (icon.EllipseSweepRadians / icon._segmentArcSweepRadians));
            icon._alphaChangeAmount = ((float)icon.MaxAlpha - icon.MinAlpha) / icon._numberOfDrawSegments;

            return icon;
        }


        internal void SetPointArray(OrbitWidgetIconData icon)
        {
            icon._points = new PointD[icon._numberOfArcSegments + 1];
            double angle = 0;//_orbitAngleRadians;
            for (int i = 0; i < icon._numberOfArcSegments + 1; i++)
            {

                double x1 = icon._orbitEllipseSemiMaj * Math.Sin(angle) - icon._focalDistance; //we add the focal distance so the focal point is "center"
                double y1 = icon._orbitEllipseSemiMinor * Math.Cos(angle);

                //rotates the points to allow for the LongditudeOfPeriapsis. 
                double x2 = (x1 * Math.Cos(icon._orbitAngleRadians)) - (y1 * Math.Sin(icon._orbitAngleRadians));
                double y2 = (x1 * Math.Sin(icon._orbitAngleRadians)) + (y1 * Math.Cos(icon._orbitAngleRadians));
                angle += icon._segmentArcSweepRadians;
                icon._points[i] = new PointD() { x = x2, y = y2 };
            }
        }


        public static void PhysicsUpdate(OrbitWidgetIconData icon)
        {
 
            var ralitivePoint = (icon.IconData.WorldPosition - icon.InsertionPoint);
            icon._orbitEllipseSemiMaj = (ralitivePoint).Length();

            icon._orbitAngleRadians = Math.Atan2(ralitivePoint.Y, ralitivePoint.X);
            icon._eccentricity = icon._focalDistance / icon._orbitEllipseSemiMaj;
            icon._orbitEllipseSemiMinor = icon._orbitEllipseSemiMaj * Math.Sqrt(1 - icon._eccentricity * icon._eccentricity);
            SetPointArray();

            PointD pointD = new PointD() { x = icon.InsertionPoint.X, y = icon.InsertionPoint.Y }; //may make this ralitive

            double minDist = PointDFunctions.CalcDistance(pointD, icon._points[icon._index]);

            for (int i = 0; i < icon._points.Length; i++)
            {
                double dist = PointDFunctions.CalcDistance(pointD, icon._points[i]);
                if (dist < minDist)
                {
                    minDist = dist;
                    _index = i;
                }
            }
        }


        public override void Draw(IntPtr rendererPtr, Camera camera)
        {

            PhysicsUpdate();
            base.Draw(rendererPtr, camera);
            byte oR, oG, oB, oA;
            SDL.SDL_GetRenderDrawColor(rendererPtr, out oR, out oG, out oB, out oA);
            SDL.SDL_BlendMode blendMode;
            SDL.SDL_GetRenderDrawBlendMode(rendererPtr, out blendMode);
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);


            //get matrix transformations for zoom
            Matrix matrix = new Matrix();
            matrix.Scale(camera.ZoomLevel);

            int index = _index;
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
            float alpha = MaxAlpha;
            for (int i = 0; i < _numberOfDrawSegments - 1; i++)
            {
                SDL.SDL_SetRenderDrawColor(rendererPtr, Red, Grn, Blu, (byte)alpha);//we cast the alpha here to stop rounding errors creaping up. 
                SDL.SDL_RenderDrawLine(rendererPtr, translatedPoints[i].x, translatedPoints[i].y, translatedPoints[i + 1].x, translatedPoints[i + 1].y);
                alpha -= _alphaChangeAmount;
            }
            SDL.SDL_SetRenderDrawColor(rendererPtr, oR, oG, oB, oA);
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, blendMode);
        }

    }
}
