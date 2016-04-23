using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.CrossPlatformUI
{
    public class Camera2D
    {
        private Point _position = Point.Empty; // Position of the ~Camera within the viewport in pixels.
        public Point ViewPortCenter { get { return _viewportCenter; } set { _viewportCenter = value; } }
        private Point _viewportCenter = Point.Empty; // Center of our viewport "window" in pixels.
        private Size _viewportSize = Size.Empty;

        private int _zoomLevel = 15;          // Current Zoom level with 0 being all zoomed out. 
        private const byte MAX_ZOOMLEVEL = 20;       // Maximum level of zoom. How much each level Zooom in determined in ZoomFactor
        private const byte MAX_MapRadius = 250;      // Used as Radius, I figured 500AU axis will be more than enough.

        /// <summary>
        /// Construct a new Camera class within the Graphic Control Viewport. 
        /// </summary>
        public Camera2D(Size viewport)
        {
            _viewportSize = viewport;
            updateViewPort(viewport);
            readjustZoom(0, 0, _zoomLevel);
        }


        /// <summary>
        /// Gets or sets the position of the camera.
        /// </summary>
        public void updateViewPort(Size viewport)
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
            limitOffsets();
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
            limitOffsets();
        }
        /// <summary>
        /// Offset the Y position of the camare i.e. Pan Y
        /// </summary>
        /// <param name="dy">The vertical difference in pixels</param>
        public void OffsetY(int dy)
        {
            _position.Y += dy;
            limitOffsets();
        }
        /// <summary>
        /// Limits how far we pan the map. Making sure we never exceed “map” bounds.
        /// </summary>
        public void limitOffsets()
        {
            int maxXOffest = MAX_MapRadius * ZoomFactor() - _viewportCenter.X;
            int maxYOffest = MAX_MapRadius * ZoomFactor() - _viewportCenter.Y;

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
        public int ZoomLevel
        {
            get { return _zoomLevel; }
            set
            {
                if ((value > 0) && (value < MAX_ZOOMLEVEL))
                {
                    _zoomLevel = value;
                    limitOffsets();
                }
            }
        }

        /// <summary>
        /// Zoom in and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="x">The X coordinate within the viewport</param>
        /// <param name="y">The Y coordinate within the viewport</param>
        public void ZoomIn(int x, int y)
        {
            if (ZoomLevel < MAX_ZOOMLEVEL)
                readjustZoom(x, y, ZoomLevel + 1);
        }

        /// <summary>
        /// Zoom out and keep try to keep the given pixel under the mouse.
        /// </summary>
        /// <param name="x">The X coordinate within the viewport</param>
        /// <param name="y">The Y coordinate within the viewport</param>
		public void ZoomOut(int x, int y)
        {
            if (ZoomLevel > 0)
                readjustZoom(x, y, ZoomLevel - 1);
        }

        /// <summary>
        /// Return the zoom\scale factor. Equal to 2^zoomLevel.
        /// </summary>
        public int ZoomFactor()
        {
            //return (1 << zoomLevel);
            return _zoomLevel * _zoomLevel;
        }

        /// <param name="x">The X mouse coordinate</param>
        /// <param name="y">The Y mouse coordinate</param>
        /// <param name="newZoomLevel">The new zoom level.</param>
        private void readjustZoom(int x, int y, int newZoomLevel)
        {

            // not tested. 

            int prevX = (_position.X - x) * ZoomFactor();
            int prevY = (_position.Y - y) * ZoomFactor();

            _zoomLevel = newZoomLevel;

            int newX = (_position.X - x) * ZoomFactor();
            int newY = (_position.Y - y) * ZoomFactor();

            _position.X = (newX - prevX) / ZoomFactor();
            _position.Y = (newX - prevX) / ZoomFactor();

            limitOffsets();
        }

        /// <summary>
        ///  Create a matrix to offset everything we draw, accounts for viewport size, and user pan/zoom
        /// </summary>
        public IMatrix GetViewProjectionMatrix()
        {
            var transformMatrix = Matrix.Create();
            transformMatrix.Translate(_viewportCenter);  // Adjust point of view from top left corner to center. 
            transformMatrix.Translate(_position);        // Adjust offest position i.e. how far panned from the center.
            //transformMatrix.Scale(ZoomFactor());         // Adjust based on the 

            return transformMatrix;

            //IIRC this should be the same as the above. 
            //return Matrix.Create(_zoom,0,0,_zoom, Width/2 + offsetX, Height/2 + offsetY);
        }
    }
}
