using Eto.Drawing;
using Eto.Forms;

namespace Pulsar4X.CrossPlatformUI
{
    public class Camera2D
    {

        private PointF _worldPosition = new PointF(0, 0);
        public PointF WorldPosition { get { return _worldPosition; } }

        public Size ViewPortCenter { get { return _viewPort.Size / 2; }}

        private Drawable _viewPort;

        private float _zoomLevel = 1.0f;          // Current Zoom level
        private float _lastZoomLevel = 1.0f;
        private const float MAX_ZOOMLEVEL = 100;       // Maximum level of zoom. How much each level Zooom in determined in ZoomFactor
        private const int MAX_MapRadius = 50000;      // Used as Radius, I figured 500AU axis will be more than enough. currently unused

        /// <summary>
        /// Construct a new Camera class within the Graphic Control Viewport. 
        /// </summary>
        public Camera2D(Drawable viewPort)
        {
            _viewPort = viewPort;
            _viewPort.SizeChanged += _viewPort_SizeChanged;
        }

        private void _viewPort_SizeChanged(object sender, System.EventArgs e)
        {
            //
        }

        /// <summary>
        /// returns teh viewCoordinate of a given world Coordinate 
        /// </summary>
        /// <param name="worldCoord"></param>
        /// <returns></returns>
        public Point ViewCoordinate(PointF worldCoord)
        {
            Point viewCoord = (Point)(worldCoord + _worldPosition * ZoomLevel) + ViewPortCenter;
            return viewCoord ;
        }

        /// <summary>
        /// returns the world coordinate of a given point in the viewscreen
        /// </summary>
        /// <param name="viewCoord"></param>
        /// <returns></returns>
        public PointF WorldCoordinate(PointF viewCoord)
        {
            PointF worldCoord = (_worldPosition + viewCoord * ZoomLevel);// - ViewPortCenter;            
            return worldCoord;
        }


        /// <summary>
        /// Center on a view position
        /// </summary>
        /// <param name="newViewPosition"></param>
        public void CenterOn(Point newViewPosition)
        {
            _worldPosition = WorldCoordinate(newViewPosition);
        }
        /// <summary>
        /// Offset the position of the camare i.e. Pan 
        /// </summary>
        public void Offset(Point offset)
        {
            _worldPosition.Offset(offset);
            LimitOffsets();
        }

        public void CenterOn(MouseEventArgs e)
        {
            //Point loc = (Point)e.Location - _viewportSize / 2;
            //_viewportCenter -= loc;
            _worldPosition = WorldCoordinate(e.Location);
        }

        /// <summary>
        /// Offset the X position of the camare i.e. Pan X 
        /// </summary>
        /// <param name="dx">The horizontal difference in pixels</param>
        public void OffsetX(int dx)
        {
            _worldPosition.X += dx;
            LimitOffsets();
        }
        /// <summary>
        /// Offset the Y position of the camare i.e. Pan Y
        /// </summary>
        /// <param name="dy">The vertical difference in pixels</param>
        public void OffsetY(int dy)
        {
            _worldPosition.Y += dy;
            LimitOffsets();
        }
        /// <summary>
        /// Limits how far we pan the map. Making sure we never exceed “map” bounds.
        /// </summary>
        public void LimitOffsets()
        {
            int maxXOffest = MAX_MapRadius; //(int)(MAX_MapRadius * ZoomFactor() - _viewportCenter.X);
            int maxYOffest = MAX_MapRadius;//(int)(MAX_MapRadius * ZoomFactor() - _viewportCenter.Y);

            if (maxXOffest > 0)
            {
                if (_worldPosition.X > maxXOffest) // We panned to much, nothing to see here folks.
                    _worldPosition.X = maxXOffest;
                else if (-_worldPosition.X > maxXOffest)
                    _worldPosition.X = -maxXOffest;
            }
            else _worldPosition.X = 0;  //Our viewport is larger than the map.  

            if (maxYOffest > 0)
            {
                if (_worldPosition.Y > maxYOffest)
                    _worldPosition.Y = maxYOffest;
                else if (- _worldPosition.Y > maxYOffest)
                    _worldPosition.Y = -maxYOffest;
            }
            else _worldPosition.Y = 0;
        }

        /// <summary>
        /// Gets or sets the zoom of the Camera. 0..MAX_ZOOMLEVEL)
        /// </summary>
        public float ZoomLevel
        {
            get { return _zoomLevel; }
            set
            {
                if ((value > 0.0) && (value < MAX_ZOOMLEVEL))
                {
                    _zoomLevel = value;
                    LimitOffsets();
                }
            }
        }

        /// <summary>
        /// Zoom in and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="x">The X coordinate within the viewport</param>
        /// <param name="y">The Y coordinate within the viewport</param>
        public void ZoomIn(Size size)
        {
            if (ZoomLevel < MAX_ZOOMLEVEL)
                readjustZoom(size,1.1f);
        }

        /// <summary>
        /// Zoom out and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="size"></param>
		public void ZoomOut(Size size)
        {
            if (ZoomLevel > 0)
                readjustZoom(size,0.9f);
        }

        /// <summary>
        /// Return the zoom\scale factor. Equal to 2^zoomLevel.
        /// </summary>
        public float ZoomFactor()
        {
            return ( _zoomLevel );
        }

        /// <param name="x">The X mouse coordinate</param>
        /// <param name="y">The Y mouse coordinate</param>
        /// <param name="newZoomLevel">The new zoom level.</param>
        private void readjustZoom(Size size,float zoomAdjust)
        {
            _zoomLevel = _zoomLevel * zoomAdjust;
            LimitOffsets();
        }

        /// <summary>
        ///  Create a matrix to offset everything we draw, accounts for viewport size, and user pan/zoom
        /// </summary>
        public IMatrix GetViewProjectionMatrix(bool scaleWithZoom = true)
        {
            var transformMatrix = Matrix.Create();
            //transformMatrix.Translate(_viewportCenter);  // Adjust point of view from top left corner to center. 
            transformMatrix.Translate(ViewCoordinate(_worldPosition) );        // Adjust offest position i.e. how far panned from the center.
            if(scaleWithZoom) 
                transformMatrix.Scale(ZoomFactor());         // Adjust scale of the item based on the zoom


            return transformMatrix;

            //IIRC this should be the same as the above. 
            //return Matrix.Create(_zoom,0,0,_zoom, Width/2 + offsetX, Height/2 + offsetY);
        }
    }
}
