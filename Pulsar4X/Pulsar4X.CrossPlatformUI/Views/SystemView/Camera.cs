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

        public Point ViewCoordinate(Vector4 worldCoord)
        {
            PointF coord = new PointF((float)worldCoord.X, (float)worldCoord.Y);
            return ViewCoordinate(coord);
        }

        /// <summary>
        /// Offset the position of the camare i.e. Pan in world units.
        /// </summary>
        public void WorldOffset(PointF offset)
        {
            _cameraWorldPosition.Offset((offset * 1.0f / ZoomLevel));

        }

        /// <summary>
        /// Zoom in and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="x">The X coordinate within the viewport</param>
        /// <param name="y">The Y coordinate within the viewport</param>
        public void ZoomIn()
        {
            if (ZoomLevel < MAX_ZOOMLEVEL)
                ZoomLevel *= 1.1f;
        }

        /// <summary>
        /// Zoom out and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="size"></param>
		public void ZoomOut()
        {
            if (ZoomLevel > 0)
                ZoomLevel *= 0.9f;
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
