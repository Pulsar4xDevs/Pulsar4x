using Pulsar4X.ECSLib;
using Eto.Drawing;
using Eto.Forms;

namespace Pulsar4X.CrossPlatformUI.Views
{
    internal class Camera2dv2
    {
        private PointF _cameraWorldPosition = new PointF(0, 0);
        public PointF WorldPosition { get { return _cameraWorldPosition; } }

        public Size ViewPortCenter { get { return _viewPort.Size / 2; } }
        public float ZoomLevel { get; set; } = 200;
        public float zoomSpeed { get; set;} = 1.25f;

        public Drawable _viewPort;

        double MAX_ZOOMLEVEL = 1.496e+11;

        /// <summary>
        /// Construct a new Camera class within the Graphic Control Viewport. 
        /// </summary>
        public Camera2dv2(Drawable viewPort)
        {
            _viewPort = viewPort;
            //_viewPort.SizeChanged += _viewPort_SizeChanged;
        }

        /// <summary>
        /// returns the viewCoordinate of a given world Coordinate 
        /// </summary>
        /// <param name="worldCoord"></param>
        /// <returns></returns>
        public Point ViewCoordinate(PointF worldCoord)
        {
            Point viewCoord = (Point)(worldCoord * (ZoomLevel) + ViewPortCenter);
            return viewCoord;
        }

        /// <summary>
        /// returns the viewCoordinate of a given world Coordinate 
        /// </summary>
        /// <param name="worldCoord"></param>
        /// <returns></returns>
        public Point ViewCoordinate(Vector4 worldCoord)
        {
            PointF coord = new PointF((float)worldCoord.X, (float)worldCoord.Y);
            return ViewCoordinate(coord);
        }

        /// <summary>
        /// Offset the position of the camare i.e. Pan in world units.
        /// <param name="offset">Pans the camera relative to offset</param>
        /// </summary>
        public void WorldOffset(PointF offset)
        {
            _cameraWorldPosition.Offset((offset * 1.0f / ZoomLevel));

        }

        /// <summary>
        /// Zoom in and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="zoomCoords">The coordinates of the panel to zoom in</param>
        public void ZoomIn(PointF zoomCoords)
        {
            if (ZoomLevel < MAX_ZOOMLEVEL)
            {
                ZoomLevel *= zoomSpeed;
                this.WorldOffset(zoomCoords  - ViewPortCenter - (zoomCoords - ViewPortCenter) * zoomSpeed);
            }
        }

        /// <summary>
        /// Zoom out and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="zoomCoords">The coordinates of the panel to soom out from</param>
		public void ZoomOut(PointF zoomCoords)
        {
            if (ZoomLevel > 0)
            {
                ZoomLevel /= zoomSpeed;
                this.WorldOffset(zoomCoords - ViewPortCenter - (zoomCoords - ViewPortCenter) / zoomSpeed);
            }

        }

        public IMatrix GetViewProjectionMatrix(PointF position)
        {
            var transformMatrix = Matrix.Create();
            transformMatrix.Translate(ViewCoordinate(_cameraWorldPosition));
            position *= ZoomLevel;
            transformMatrix.Translate(position);
            return transformMatrix;
        }
    }
}
