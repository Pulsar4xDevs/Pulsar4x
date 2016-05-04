using Eto.Drawing;
using Eto.Forms;

namespace Pulsar4X.CrossPlatformUI
{
    public class Camera2D
    {
        private Point _position = Point.Empty; // Position of the ~Camera within the viewport in pixels.
        public Point ViewPortCenter { get { return _viewportCenter; } set { _viewportCenter = value; } }
        private Point _viewportCenter = Point.Empty; // Center of our viewport "window" in pixels.
        private Size _viewportSize = Size.Empty;

        private float _zoomLevel = 1.0f;          // Current Zoom level
        private float _lastZoomLevel = 1.0f;
        private const float MAX_ZOOMLEVEL = 100;       // Maximum level of zoom. How much each level Zooom in determined in ZoomFactor
        private const int MAX_MapRadius = 50000;      // Used as Radius, I figured 500AU axis will be more than enough. currently unused

        /// <summary>
        /// Construct a new Camera class within the Graphic Control Viewport. 
        /// </summary>
        public Camera2D(Size viewport)
        {
            _viewportSize = viewport;
            UpdateViewPort(viewport);
            readjustZoom(viewport,1.0f);
        }


        /// <summary>
        /// Gets or sets the position of the camera.
        /// </summary>
        public void UpdateViewPort(Size viewport)
        {
            _viewportSize = viewport;
            _viewportCenter.X = viewport.Width / 2;
            _viewportCenter.Y = viewport.Height / 2;
        }


        /// <summary>
        /// Center the camera on specific pixel coordinates
        /// </summary>
        public void CenterOn(Point newPosition)
        {
            _position = newPosition;
        }
        /// <summary>
        /// Offset the position of the camare i.e. Pan 
        /// </summary>
        public void Offset(Point offset)
        {
            _position.Offset(offset);
            LimitOffsets();
        }

        public void CenterOn(MouseEventArgs e)
        {
            Point loc = (Point)e.Location - _viewportSize / 2;
            _viewportCenter -= loc;
        }

        /// <summary>
        /// Offset the X position of the camare i.e. Pan X 
        /// </summary>
        /// <param name="dx">The horizontal difference in pixels</param>
        public void OffsetX(int dx)
        {
            _position.X += dx;
            LimitOffsets();
        }
        /// <summary>
        /// Offset the Y position of the camare i.e. Pan Y
        /// </summary>
        /// <param name="dy">The vertical difference in pixels</param>
        public void OffsetY(int dy)
        {
            _position.Y += dy;
            LimitOffsets();
        }
        /// <summary>
        /// Limits how far we pan the map. Making sure we never exceed “map” bounds.
        /// </summary>
        public void LimitOffsets()
        {
            int maxXOffest = (int)(MAX_MapRadius * ZoomFactor() - _viewportCenter.X);
            int maxYOffest = (int)(MAX_MapRadius * ZoomFactor() - _viewportCenter.Y);

            if (maxXOffest > 0)
            {
                if (_position.X > maxXOffest) // We panned to much, nothing to see here folks.
                    _viewportCenter.X = maxXOffest;
                else if (-_position.X > maxXOffest)
                    _viewportCenter.X = -maxXOffest;
            }
            else _viewportCenter.X = 0;  //Our viewport is larger than the map.  

            if (maxYOffest > 0)
            {
                if (_position.Y > maxYOffest)
                    _position.Y = maxYOffest;
                else if (-_position.Y > maxYOffest)
                    _position.Y = -maxYOffest;
            }
            else _viewportCenter.Y = 0;
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
            //Adjust the zoom level itself.
            _zoomLevel = _zoomLevel * zoomAdjust;

            //recalculate the viewport center, it will have changed by a factor of the difference in zoomLevel * half of width or height.
            if (_zoomLevel > 1.0f)
            {
                _viewportCenter.X = _viewportCenter.X + (int)(( _zoomLevel - _lastZoomLevel) * (-0.5f) * (float)size.Width);
                _viewportCenter.Y = _viewportCenter.Y + (int)((_zoomLevel - _lastZoomLevel) * (-0.5f) * (float)size.Height);
            }
            else if(_zoomLevel < 1.0f)
            {
                _viewportCenter.X = _viewportCenter.X + (int)((_lastZoomLevel - _zoomLevel) * (0.5f) * (float)size.Width);
                _viewportCenter.Y = _viewportCenter.Y + (int)((_lastZoomLevel - _zoomLevel) * (0.5f) * (float)size.Height);
            }
            else if(_zoomLevel == 1.0f)
            {
                if (_lastZoomLevel > 1.0f)
                {
                    _viewportCenter.X = _viewportCenter.X + (int)((_zoomLevel - _lastZoomLevel) * (-0.5f) * (float)size.Width);
                    _viewportCenter.Y = _viewportCenter.Y + (int)((_zoomLevel - _lastZoomLevel) * (-0.5f) * (float)size.Height);
                }
                else if(_lastZoomLevel < 1.0f)
                {
                    _viewportCenter.X = _viewportCenter.X + (int)((_lastZoomLevel - _zoomLevel) * (0.5f) * (float)size.Width);
                    _viewportCenter.Y = _viewportCenter.Y + (int)((_lastZoomLevel - _zoomLevel) * (0.5f) * (float)size.Height);
                }
            }
            //record the current zoomLevel as the last zoomlevel so that the next time zoom changes we have this value to check against.
            _lastZoomLevel = _zoomLevel;
            //don't know if this still matters
            LimitOffsets();
        }

        /// <summary>
        ///  Create a matrix to offset everything we draw, accounts for viewport size, and user pan/zoom
        /// </summary>
        public IMatrix GetViewProjectionMatrix()
        {
            var transformMatrix = Matrix.Create();
            transformMatrix.Translate(_viewportCenter);  // Adjust point of view from top left corner to center. 
            transformMatrix.Translate(_position);        // Adjust offest position i.e. how far panned from the center.
            transformMatrix.Scale(ZoomFactor());         // Adjust based on the 


            return transformMatrix;

            //IIRC this should be the same as the above. 
            //return Matrix.Create(_zoom,0,0,_zoom, Width/2 + offsetX, Height/2 + offsetY);
        }
    }
}
