using System;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using SDL2;
using Point = SDL2.SDL.SDL_Point;
//using Vector2 = ImGuiNET.Vector2;

namespace Pulsar4X.SDL2UI
{


    public class Camera
    {
        internal bool IsGrabbingMap = false;
        internal int MouseFrameIncrementX;
        internal int MouseFrameIncrementY;

        internal bool IsPinnedToEntity { get; private set; }
        internal Guid PinnedEntityGuid;
        PositionDB _entityPosDB;
        ECSLib.Vector4 _camWorldPos = new ECSLib.Vector4();
        public ECSLib.Vector4 CameraWorldPosition
        {
            get
            {
                if (IsPinnedToEntity && _entityPosDB != null)
                    return new ECSLib.Vector4
                    {
                        X = _camWorldPos.X + _entityPosDB.AbsolutePosition_AU.X,
                        Y = _camWorldPos.Y + _entityPosDB.AbsolutePosition_AU.Y
                    };
                else
                    return _camWorldPos;
            }
            set
            {
                if (IsPinnedToEntity)
                {
                    IsPinnedToEntity = false;
                }
                _camWorldPos = value;
            }
        }

        //public ImVec2 WorldPosition { get { return _cameraWorldPosition; } }

        public System.Numerics.Vector2 ViewPortCenter { get { return new System.Numerics.Vector2(_viewPort.Size.X * 0.5f, _viewPort.Size.Y * 0.5f); } }

        public System.Numerics.Vector2 ViewPortSize { get { return _viewPort.Size; } }
        public float ZoomLevel { get; set; } = 200;
        public float zoomSpeed { get; set; } = 1.25f;

        public ImGuiSDL2CSWindow _viewPort;

        double MAX_ZOOMLEVEL = 1.496e+11;

        /// <summary>
        /// Construct a new Camera class within the Graphic Control Viewport. 
        /// </summary>
        public Camera(ImGuiSDL2CSWindow viewPort)
        {
            _viewPort = viewPort;
            //_viewPort.SizeChanged += _viewPort_SizeChanged;

        }

        public void PinToEntity(Entity entity)
        {
            if (entity.HasDataBlob<PositionDB>())
            {
                _entityPosDB = entity.GetDataBlob<PositionDB>();
                _camWorldPos = new ECSLib.Vector4(); //zero on it. 
                IsPinnedToEntity = true;
                PinnedEntityGuid = entity.Guid;
            }
        }

        public void CenterOnEntity(Entity entity)
        {
            if (entity.HasDataBlob<PositionDB>())
            {
                _camWorldPos = entity.GetDataBlob<PositionDB>().AbsolutePosition_AU;
            }
        }

        public Point ViewCoordinate(ECSLib.Vector4 worldCoord)
        {
            int x = (int)((worldCoord.X - CameraWorldPosition.X) * ZoomLevel + ViewPortCenter.X);
            int y = -(int)((worldCoord.Y - CameraWorldPosition.Y) * ZoomLevel - ViewPortCenter.Y);
            Point viewCoord = new Point() { x = x, y = y };

            return viewCoord;
        }

        public ECSLib.Vector4 MouseWorldCoordinate()
        {
            System.Numerics.Vector2 mouseCoord = ImGui.GetMousePos();
            double x = ((mouseCoord.X - ViewPortCenter.X) / ZoomLevel) + CameraWorldPosition.X;
            double y = -(((mouseCoord.Y - ViewPortCenter.Y) / ZoomLevel) - CameraWorldPosition.Y);
            return new ECSLib.Vector4(x, y, 0, 0);

        }

        /// <summary>
        /// returns the worldCoordinate of a given View Coordinate 
        /// </summary>
        /// <param name="viewCoordinate"></param>
        /// <returns></returns>
        public ECSLib.Vector4 WorldCoordinate(int viewCoordinateX, int viewCoordinateY)
        {
            double x = ((viewCoordinateX - ViewPortCenter.X) / ZoomLevel) + CameraWorldPosition.X;
            double y = -(((viewCoordinateY - ViewPortCenter.Y) / ZoomLevel) - CameraWorldPosition.Y);
            return new ECSLib.Vector4(x, y, 0, 0);
        }


        /// <summary>
        /// Returns the size of an object in view-Coordinates
        /// </summary>
        /// <param name="worldSize"></param>
        /// <returns></returns>
        public Vector2 ViewSize(Vector2 worldSize)
        {
            Vector2 viewSize = new Vector2(worldSize.X * ZoomLevel, worldSize.Y * ZoomLevel);
            return viewSize;
        }

        /// <summary>
        /// Returns the Distance in view-Coordinates
        /// </summary>
        /// <param name="dist"></param>
        /// <returns></returns>
        public float ViewDistance(double dist)
        {
            return (float)(dist * ZoomLevel);
        }

        /// <summary>
        /// Returns the Distance in World-Coordinates
        /// </summary>
        /// <param name="dist"></param>
        /// <returns></returns>
        public double WorldDistance(float dist)
        {
            return dist / ZoomLevel;
        }

        /// <summary>
        /// Returns the size of an object in world-Coordinates
        /// </summary>
        /// <param name="viewSize"></param>
        /// <returns></returns>
        public System.Numerics.Vector2 WorldSize(System.Numerics.Vector2 viewSize)
        {
            return new System.Numerics.Vector2(viewSize.X / ZoomLevel, viewSize.Y / ZoomLevel);
        }


        /// <summary>
        /// Offset the position of the camare i.e. Pan in world units.
        /// <param name="xOffset">Pans the camera horizontaly relative to offset</param>
        /// <param name="yOffset">Pans the camera verticaly relative to offset</param>
        /// </summary>
        public void WorldOffset(double xOffset, double yOffset)
        {
            if (IsPinnedToEntity)
            {
                _camWorldPos.X += (float)(xOffset * 1.0f / ZoomLevel);
                _camWorldPos.Y += (float)(-yOffset * 1.0f / ZoomLevel);
            }
            else
            {
                _camWorldPos.X += (float)(xOffset * 1.0f / ZoomLevel);
                _camWorldPos.Y += (float)(-yOffset * 1.0f / ZoomLevel);
            }
        }


        /// <summary>
        /// Zoom in and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="zoomCoords">The coordinates of the panel to zoom in</param>
        public void ZoomIn(int mouseX, int mouseY)
        {
            var worldCoord = WorldCoordinate(mouseX, mouseY);
            if (ZoomLevel < MAX_ZOOMLEVEL)
            {
                ZoomLevel *= zoomSpeed;
                double xOffset = mouseX - ViewPortCenter.X - (mouseX - ViewPortCenter.X) * zoomSpeed;
                double yOffset = mouseY - ViewPortCenter.Y - (mouseY - ViewPortCenter.Y) * zoomSpeed;
                WorldOffset(-xOffset, -yOffset);
            }
        }


        /// <summary>
        /// Zoom out and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="zoomCoords">The coordinates of the panel to soom out from</param>
        public void ZoomOut(int mouseX, int mouseY)
        {
            var worldCoord = WorldCoordinate(mouseX, mouseY);

            if (ZoomLevel > 0)
            {
                ZoomLevel /= zoomSpeed;
                double xOffset = mouseX - ViewPortCenter.X - (mouseX - ViewPortCenter.X) / zoomSpeed;
                double yOffset = mouseY - ViewPortCenter.Y - (mouseY - ViewPortCenter.Y) / zoomSpeed;
                WorldOffset(-xOffset, -yOffset);
            }
        }

        /// <summary>
        /// returns a matrix scaled to zoom. is not translated to camera position. 
        /// </summary>
        /// <returns>The zoom matrix.</returns>
        public Matrix GetZoomMatrix()
        {
            var mirrorMatrix = Matrix.NewMirrorMatrix(true, false);
            var scaleMtx = Matrix.NewScaleMatrix(ZoomLevel, ZoomLevel);
            return mirrorMatrix * scaleMtx;
        }


    }

    /// <summary>
    /// Cursor crosshair.
    /// Primarily made to debug a problem with getting the world coordinate of the mouse cursor. 
    /// </summary>
    class CursorCrosshair : Icon
    {
        public CursorCrosshair(ECSLib.Vector4 position) : base(position)
        {
            var colour = new SDL.SDL_Color() { r = 0, g = 255, b = 0, a = 255 };

            PointD point0 = new PointD() { X = -5, Y = 0 };
            PointD point1 = new PointD() { X = +5, Y = 0 };
            Shape shape0 = new Shape() { Points = new PointD[2] { point0, point1 }, Color = colour };

            PointD point2 = new PointD() { X = 0, Y = -5 };
            PointD point3 = new PointD() { X = 0, Y = +5 };
            Shape shape1 = new Shape() { Points = new PointD[2] { point2, point3 }, Color = colour };

            this.Shapes = new System.Collections.Generic.List<Shape>() { shape0, shape1 };
        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            WorldPosition = camera.MouseWorldCoordinate();
            base.OnFrameUpdate(matrix, camera);
        }

    }
}
