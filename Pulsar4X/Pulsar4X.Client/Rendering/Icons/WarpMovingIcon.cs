using System;
using Pulsar4X.Orbital;
using SDL2;
using Pulsar4X.Movement;

namespace Pulsar4X.SDL2UI
{
    public class WarpMovingIcon : Icon
    {
        //PositionDB ParentPositionDB;
        Vector3 _translateStartPoint = new Vector3();
        Vector3 _translateEndPoint = new Vector3();
        Vector3 _currentPosition = new Vector3();
        Vector3 _relativeEndPoint = new Vector3();
        private Vector3 _currentRelativeEndPoint = new Vector3();
        private PositionDB? _targetParentPos;

        private Vector2 _bzsp;
        private Vector2 _bzsp2;
        private Vector2 _bzep;
        private Vector2 _bzep2;
        
        public byte Red = 255;
        public byte Grn = 255;
        public byte Blu = 0;
        byte alpha = 100;
        //SDL.SDL_Point[] _drawPoints = new SDL.SDL_Point[2];
        private Vector2[] _bezierCurve;
        SDL.SDL_Point[] _bezierDrawPoints = new SDL.SDL_Point[10];
        public WarpMovingIcon(WarpMovingDB warpMovingDB, PositionDB positionDB): base(new Vector3())
        {
            _translateStartPoint = warpMovingDB.EntryPointAbsolute;
            _translateEndPoint = warpMovingDB.ExitPointAbsolute;
            _relativeEndPoint = warpMovingDB.ExitPointrelative;
            _targetParentPos = warpMovingDB.GetTargetPosDB;
            _positionDB = positionDB;
            this.OnPhysicsUpdate();
        }

        public override void OnPhysicsUpdate()
        {
            _currentPosition = _positionDB.AbsolutePosition;
            if(_targetParentPos != null)
                _currentRelativeEndPoint = _targetParentPos.AbsolutePosition + _relativeEndPoint;
            
            Vector2 spos = (Vector2)_currentPosition;
            Vector2 rpos = (Vector2)_currentRelativeEndPoint;
            Vector2 epos = (Vector2)_translateEndPoint;
            var ang = Angle.RadiansFromVector2(spos - epos);
            var deg = Angle.ToDegrees(ang);
            var range = (spos - epos).Length();
            var spMult = range * 0.75;
            var epMult = range * 0.25;
            _bzsp = new Vector2(spos.X, spos.Y);
            _bzsp2 = spos -  Angle.PositionFromAngle(ang, spMult);
            _bzep2 = rpos +  Angle.PositionFromAngle(ang, epMult);
            _bzep = new Vector2(rpos.X, rpos.Y);
            
            _bezierCurve = CreatePrimitiveShapes.BezierPoints(_bzsp, _bzsp2, _bzep2, _bzep, 0.025f);
            if(_bezierDrawPoints.Length != _bezierCurve.Length)
                _bezierDrawPoints = new SDL.SDL_Point[_bezierCurve.Length];
            
        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            /*
            ViewScreenPos = camera.ViewCoordinate_m(WorldPosition_m);
            
            _drawPoints = new SDL.SDL_Point[3];

            var spos = camera.ViewCoordinateV2_m(_currentPosition);
            _drawPoints[0] = new SDL.SDL_Point(){x = (int)spos.X, y = (int)spos.Y};

            var epos = camera.ViewCoordinateV2_m(_translateEndPoint);
            _drawPoints[1] = new SDL.SDL_Point(){x = (int)epos.X, y = (int)epos.Y};

            var rpos = camera.ViewCoordinateV2_m(_currentRelativeEndPoint);
            _drawPoints[2] = new SDL.SDL_Point(){x = (int)rpos.X, y = (int)rpos.Y};
            */
            for (int index = 0; index < _bezierCurve.Length; index++)
            {
                var pos = camera.ViewCoordinateV2_m(_bezierCurve[index]);
                _bezierDrawPoints[index] = new SDL.SDL_Point(){x = Convert.ToInt32(pos.X), y = Convert.ToInt32(pos.Y)};
            }
        }


        public override void Draw(IntPtr rendererPtr, Camera camera)
        {

                SDL.SDL_SetRenderDrawColor(rendererPtr, Red, Grn, Blu, alpha);
                //SDL.SDL_RenderDrawLine(rendererPtr, _drawPoints[0].x, _drawPoints[0].y, _drawPoints[1].x, _drawPoints[1].y);
                //SDL.SDL_RenderDrawLine(rendererPtr, _drawPoints[0].x, _drawPoints[0].y, _drawPoints[2].x, _drawPoints[2].y);


                SDL.SDL_RenderDrawLines(rendererPtr, _bezierDrawPoints, _bezierDrawPoints.Length );
                int lp = _bezierDrawPoints.Length -1;
                //SDL.SDL_RenderDrawLine(rendererPtr, _drawPoints[2].x, _drawPoints[2].y, _bezierDrawPoints[lp].x, _bezierDrawPoints[lp].y);
                
        }
    }
}
