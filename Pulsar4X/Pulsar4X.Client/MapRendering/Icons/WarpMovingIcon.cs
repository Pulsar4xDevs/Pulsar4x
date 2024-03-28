using System;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Orbital;
using SDL2;

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
        private PositionDB _targetParentPos;
        
        public byte Red = 255;
        public byte Grn = 255;
        public byte Blu = 0;
        byte alpha = 100;
        SDL.SDL_Point[] _drawPoints = new SDL.SDL_Point[2];
        public WarpMovingIcon(Entity entity): base(new Vector3())
        {
            if (entity.HasDataBlob<WarpMovingDB>())
            {
                var db = entity.GetDataBlob<WarpMovingDB>();
                _translateStartPoint = db.EntryPointAbsolute;
                _translateEndPoint = db.ExitPointAbsolute;
                _relativeEndPoint = db.ExitPointrelative;
                _targetParentPos = db.GetTargetPosDB;
            }
            if (entity.HasDataBlob<OrderableDB>())
            {
                var orderable = entity.GetDataBlob<OrderableDB>();
            }
            _positionDB = entity.GetDataBlob<PositionDB>();


        }

        public override void OnPhysicsUpdate()
        {
            _currentPosition = _positionDB.AbsolutePosition;
            _currentRelativeEndPoint = _targetParentPos.AbsolutePosition + _relativeEndPoint;
        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            ViewScreenPos = camera.ViewCoordinate_m(WorldPosition_m);
            _drawPoints = new SDL.SDL_Point[3];
            
            var spos = camera.ViewCoordinateV2_m(_currentPosition);
            _drawPoints[0] = new SDL.SDL_Point(){x = (int)spos.X, y = (int)spos.Y};
            
            var epos = camera.ViewCoordinateV2_m(_translateEndPoint);
            _drawPoints[1] = new SDL.SDL_Point(){x = (int)epos.X, y = (int)epos.Y};
            
            var rpos = camera.ViewCoordinateV2_m(_currentRelativeEndPoint);
            _drawPoints[2] = new SDL.SDL_Point(){x = (int)rpos.X, y = (int)rpos.Y};
        }


        public override void Draw(IntPtr rendererPtr, Camera camera)
        {

                SDL.SDL_SetRenderDrawColor(rendererPtr, Red, Grn, Blu, alpha);
                SDL.SDL_RenderDrawLine(rendererPtr, _drawPoints[0].x, _drawPoints[0].y, _drawPoints[1].x, _drawPoints[1].y);
                SDL.SDL_RenderDrawLine(rendererPtr, _drawPoints[0].x, _drawPoints[0].y, _drawPoints[2].x, _drawPoints[2].y);

        }
    }
}
