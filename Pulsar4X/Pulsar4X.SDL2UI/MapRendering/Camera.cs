using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using SDL2;
using Point = SDL2.SDL.SDL_Point;
namespace Pulsar4X.SDL2UI
{


    public class Camera
    {
        internal bool IsGrabbingMap = false;
        internal int MouseFrameIncrementX;
        internal int MouseFrameIncrementY;


        private ImVec2 _cameraWorldPosition = new ImVec2(0, 0);
        public ImVec2 WorldPosition { get { return _cameraWorldPosition; } }

        public ImVec2 ViewPortCenter { get { return new ImVec2(_viewPort.Size.X * 0.5f, _viewPort.Size.Y * 0.5f); }}

        public ImVec2 ViewPortSize { get { return _viewPort.Size; } }
        public float ZoomLevel { get; set; } = 1;
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

        public Point CameraViewCoordinate()
        {
            Point point = new Point();
            point.x = (int)(_cameraWorldPosition.x * (ZoomLevel) + ViewPortCenter.x);
            point.y = (int)(_cameraWorldPosition.y * (ZoomLevel) + ViewPortCenter.y);
            return point;
        }
        /// <summary>
        /// returns the viewCoordinate of a given world Coordinate 
        /// </summary>
        /// <param name="worldCoord"></param>
        /// <returns></returns>
        public Point ViewCoordinate(Vector4 worldCoord)
        {
            int x = (int)(worldCoord.X * (ZoomLevel) + ViewPortCenter.x);
            int y = (int)(worldCoord.Y * (ZoomLevel) + ViewPortCenter.y);
            Point viewCoord = new Point() {x = x ,y = y  };

            return viewCoord;
        }



        /// <summary>
        /// returns the worldCoordinate of a given View Coordinate 
        /// </summary>
        /// <param name="viewCoordinate"></param>
        /// <returns></returns>
        public Point WorldCoordinate(Vector4 viewCoordinate)
        {
            int x = (int)((viewCoordinate.X - ViewPortCenter.X) / ZoomLevel);
            int y = (int)((viewCoordinate.Y - ViewPortCenter.Y) / ZoomLevel);
            return new Point() { x = x, y = y };
        }


        /// <summary>
        /// Returns the size of an object in view-Coordinates
        /// </summary>
        /// <param name="worldSize"></param>
        /// <returns></returns>
        public ImVec2 ViewSize(ImVec2 worldSize)
        {
            ImVec2 viewSize = new ImVec2( worldSize.X * ZoomLevel, worldSize.Y * ZoomLevel);
            return viewSize;
        }

        /// <summary>
        /// Returns the Distance in view-Coordinates
        /// </summary>
        /// <param name="worldSize"></param>
        /// <returns></returns>
        public float ViewDistance(float dist)
        {
            float ViewDistance = dist * ZoomLevel;
            return ViewDistance;
        }

        /// <summary>
        /// Returns the Distance in World-Coordinates
        /// </summary>
        /// <param name="worldSize"></param>
        /// <returns></returns>
        public float WorldDistance(float dist)
        {
            float WorldDistance = dist / ZoomLevel;
            return WorldDistance;
        }

        /// <summary>
        /// Returns the size of an object in world-Coordinates
        /// </summary>
        /// <param name="viewSize"></param>
        /// <returns></returns>
        public ImVec2 WorldSize(ImVec2 viewSize)
        {
            return new ImVec2(viewSize.x / ZoomLevel, viewSize.y / ZoomLevel);
        }


        /// <summary>
        /// Offset the position of the camare i.e. Pan in world units.
        /// <param name="xOffset">Pans the camera horizontaly relative to offset</param>
        /// <param name="yOffset">Pans the camera verticaly relative to offset</param>
        /// </summary>
        public void WorldOffset(float xOffset, float yOffset)
        {
            _cameraWorldPosition.x -= ((xOffset * 1.0f / ZoomLevel));
            _cameraWorldPosition.y -= ((yOffset * 1.0f / ZoomLevel));
        }

        /// <summary>
        /// Zoom in and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="zoomCoords">The coordinates of the panel to zoom in</param>
        public void ZoomIn(ImVec2 zoomCoords)
        {
            if (ZoomLevel < MAX_ZOOMLEVEL)
            {
                ZoomLevel *= zoomSpeed;
                float xoffset = zoomCoords.x - ViewPortCenter.x - (zoomCoords.x - ViewPortCenter.x) * zoomSpeed;
                float yoffset = zoomCoords.y - ViewPortCenter.y - (zoomCoords.y - ViewPortCenter.y) * zoomSpeed;
                //this.WorldOffset(xoffset, yoffset);
            }
        }
        /// <summary>
        /// Zoom in and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="zoomCoords">The coordinates of the panel to zoom in</param>
        public void ZoomIn(int xZoomCoords, int yZoomCoords)
        {
            if (ZoomLevel < MAX_ZOOMLEVEL)
            {
                ZoomLevel *= zoomSpeed;
                float xoffset = xZoomCoords - ViewPortCenter.x - (xZoomCoords - ViewPortCenter.x) * zoomSpeed;
                float yoffset = yZoomCoords - ViewPortCenter.y - (yZoomCoords - ViewPortCenter.y) * zoomSpeed;
                //this.WorldOffset(xoffset, yoffset);
            }
        }

        /// <summary>
        /// Zoom out and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="zoomCoords">The coordinates of the panel to soom out from</param>
        public void ZoomOut(ImVec2 zoomCoords)
        {
            if (ZoomLevel > 0)
            {
                ZoomLevel /= zoomSpeed;
                float xOffset = zoomCoords.x - ViewPortCenter.x - (zoomCoords.x - ViewPortCenter.x) / zoomSpeed;
                float yOffset = zoomCoords.y - ViewPortCenter.y - (zoomCoords.y - ViewPortCenter.y) / zoomSpeed;
                //this.WorldOffset(xOffset, yOffset);
            }
        }

        /// <summary>
        /// Zoom out and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="zoomCoords">The coordinates of the panel to soom out from</param>
        public void ZoomOut(int xZoomCoords, int yZoomCoords)
        {
            if (ZoomLevel > 0)
            {
                ZoomLevel /= zoomSpeed;
                float xOffset = xZoomCoords - ViewPortCenter.x - (xZoomCoords - ViewPortCenter.x) / zoomSpeed;
                float yOffset = yZoomCoords - ViewPortCenter.y - (yZoomCoords - ViewPortCenter.y) / zoomSpeed;
                //this.WorldOffset(xOffset, yOffset);
            }
        }

        /// <summary>
        /// Returns the translation matrix for a world position, relative to the camera position
        /// </summary>
        /// <param name="position">Position in World Units</param>
        /// <returns></returns>
        public Matrix GetViewProjectionMatrix(ImVec2 position)
        {
            var transformMatrix = new Matrix();
            double x = _cameraWorldPosition.x + position.x;
            double y = _cameraWorldPosition.y + position.y;
            transformMatrix.Translate(x, y);  //ViewCoordinate(x, y));
            return transformMatrix;
        }

        /// <summary>
        /// Returns the translation matrix for 0,0, relative to the camera position
        /// </summary>
        /// <returns></returns>
        public Matrix GetViewProjectionMatrix()
        {
            var transformMatrix = new Matrix();

            transformMatrix.Translate(_cameraWorldPosition.x, _cameraWorldPosition.y);  //ViewCoordinate(x, y));
            return transformMatrix;
        }
    }
}
