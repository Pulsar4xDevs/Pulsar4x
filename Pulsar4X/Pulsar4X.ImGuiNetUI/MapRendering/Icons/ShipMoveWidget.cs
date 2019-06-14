using System;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class ShipMoveWidget : Icon
    {
        //PositionDB _positionDB;
        Vector4 _translateStartPoint = new Vector4();
        Vector4 _translateEndPoint = new Vector4();
        Vector4 _currentPosition = new Vector4();
        public byte Red = 255;
        public byte Grn = 255;
        public byte Blu = 0;
        byte alpha = 100;
        SDL.SDL_Point[] _drawPoints = new SDL.SDL_Point[2];
        public ShipMoveWidget(Entity entity): base(new Vector4())
        {
            if (entity.HasDataBlob<TranslateMoveDB>())
            {
                var db = entity.GetDataBlob<TranslateMoveDB>();
                _translateStartPoint = db.TranslateEntryPoint_AU;
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
            ViewScreenPos = camera.ViewCoordinate(WorldPosition);
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
                DrawTools.DrawLine(rendererPtr, _drawPoints[0].x, _drawPoints[0].y, _drawPoints[1].x, _drawPoints[1].y);

        }
    }
}
