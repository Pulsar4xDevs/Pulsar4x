using System;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class ShipMoveWidget : Icon
    {
        //PositionDB _positionDB;
        Vector3 _translateStartPoint = new Vector3();
        Vector3 _translateEndPoint = new Vector3();
        Vector3 _currentPosition = new Vector3();
        public byte Red = 255;
        public byte Grn = 255;
        public byte Blu = 0;
        byte alpha = 100;
        SDL.SDL_Point[] _drawPoints = new SDL.SDL_Point[2];
        public ShipMoveWidget(Entity entity): base(new Vector3())
        {
            if (entity.HasDataBlob<WarpMovingDB>())
            {
                var db = entity.GetDataBlob<WarpMovingDB>();
                _translateStartPoint = db.TranslateEntryAbsolutePoint_AU;
                _translateEndPoint = db.TranslateExitPoint_AU;
            }
            if (entity.HasDataBlob<OrderableDB>())
            {
                var orderable = entity.GetDataBlob<OrderableDB>();
                var lst = orderable.GetActionList();
            }
            _positionDB = entity.GetDataBlob<PositionDB>();


        }

        public override void OnPhysicsUpdate()
        {
            _currentPosition = _positionDB.AbsolutePosition_AU; 

        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            ViewScreenPos = camera.ViewCoordinate(WorldPosition_AU);
            _drawPoints = new SDL.SDL_Point[2];


            var translated = matrix.Transform(_currentPosition.X, _currentPosition.Y);
            int x = (int)(ViewScreenPos.x + translated.x);
            int y = (int)(ViewScreenPos.y + translated.y);
            _drawPoints[0] = new SDL.SDL_Point() { x = x, y = y };

            translated = matrix.Transform(_translateEndPoint.X, _translateEndPoint.Y);
            x = (int)(ViewScreenPos.x + translated.x );
            y = (int)(ViewScreenPos.y + translated.y );
            _drawPoints[1] = new SDL.SDL_Point() { x = x, y = y };

        }


        public override void Draw(IntPtr rendererPtr, Camera camera)
        {

                SDL.SDL_SetRenderDrawColor(rendererPtr, Red, Grn, Blu, alpha); 
                SDL.SDL_RenderDrawLine(rendererPtr, _drawPoints[0].x, _drawPoints[0].y, _drawPoints[1].x, _drawPoints[1].y);

        }
    }
}
