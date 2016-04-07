using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel.SystemView
{

    /// <summary>
    /// base class for VectorShapes. basic vector primitives should inherit from this class. (see eto graphics)
    /// </summary>
    public class VectorShapeBase : ViewModelBase
    {
        /// <summary>
        /// position from 0,0
        /// </summary>
        public float X1
        {
            get { return _x1; }
            set { _x1 = value; }
        }
        private float _x1 = 0;
        /// <summary>
        /// position from 0,0
        /// </summary>
        public float Y1
        {
            get { return _y1; }
            set { _y1 = value; }
        }
        private float _y1 = 0;

        /// <summary>
        /// position from 0,0
        /// </summary>
        public float X2
        {
            get { return _x2; }
            set { _x2 = value; }
        }
        private float _x2 = 0;
        /// <summary>
        /// position from 0,0
        /// </summary>
        public float Y2
        {
            get { return _y2; }
            set { _y2 = value; }
        }
        private float _y2 = 0;

        public float CenterX { get { return _x1 + _x2 * 0.5f; } set { _x1 = value - _x2 * 0.5f; OnPropertyChanged(); } }

        public float CenterY { get { return _y1 + _y2 * 0.5f; } set { _y1 = value - _y2 * 0.5f; OnPropertyChanged(); } }

        protected VectorShapeBase()
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="centerPosition">if true, x1 and y1 inputs are the center position</param>
        protected VectorShapeBase(float x1, float y1, float x2, float y2, bool centerPosition = true)
        {
            X2 = x2;
            Y2 = y2;
            if (centerPosition)
            {
                CenterX = x1;
                CenterY = y1;
            }
            else
            {
                X1 = x1;
                Y1 = y1;
            }

        }

    }

    /// <summary>
    /// generic data for drawing a line. 
    /// </summary>
    public class LineData : VectorShapeBase
    {
        public float XStart { get { return X1; } set { X1 = value; OnPropertyChanged(); } }
        public float YStart { get { return Y1; } set { Y1 = value; OnPropertyChanged(); } }

        public float XEnd { get { return X2; } set { X2 = value; OnPropertyChanged(); } }
        public float YEnd { get { return Y2; } set { Y2 = value; OnPropertyChanged(); } }

        public LineData() : base()
        { }

        public LineData(float xStart, float yStart, float xEnd, float yEnd, bool centerPosition = false) : base(xStart, yStart, xEnd, yEnd, centerPosition)
        { }
    }

    /// <summary>
    /// generic data for a rectangle
    /// </summary>
    public class RectangleData : VectorShapeBase
    {
        public float X { get { return X1; } set { X1 = value; OnPropertyChanged(); } }
        public float Y { get { return Y1; } set { Y1 = value; OnPropertyChanged(); } }

        public float Width { get { return X2; } set { X2 = value; OnPropertyChanged(); } }
        public float Height { get { return Y2; } set { Y2 = value; OnPropertyChanged(); } }


        public RectangleData() : base()
        { }

        public RectangleData(float x, float y, float width, float height, bool centerPosition = true) : base(x, y, width, height, centerPosition)
        { }

    }

    /// <summary>
    /// generic data for an Ellipse
    /// </summary>
    public class EllipseData : RectangleData
    {
        public EllipseData() : base()
        { }

        public EllipseData(float x, float y, float width, float height, bool centerPosition = true) : base(x, y, width, height, centerPosition)
        { }
    }

    /// <summary>
    /// generic data for an arc segment
    /// </summary>
    public class ArcData : RectangleData
    {

        public float StartAngle
        {
            get { return _startAngle; }
            set { _startAngle = value; OnPropertyChanged(); }
        }
        private float _startAngle;
        public float SweepAngle
        {
            get { return _sweepAngle; }
            set { _sweepAngle = value; OnPropertyChanged(); }
        }
        private float _sweepAngle;


        public ArcData(float x, float y, float width, float height, float start, float sweep, bool centerPosition = true) : base(x, y, width, height, centerPosition)
        {
            StartAngle = start;
            SweepAngle = sweep;
        }
    }

    /// <summary>
    /// generic data for drawing a Bezier
    /// </summary>
    public class BezierData : VectorShapeBase
    {
        public float ControlX1 { get; set; }
        public float ControlY1 { get; set; }
        public float ControlX2 { get; set; }
        public float ControlY2 { get; set; }

        public BezierData(float startX, float startY, float endX, float endY, float controlX1, float controlY1, float controlX2, float controlY2, bool centerPosition = false) : base(startX, startY, endX, endY, centerPosition)
        {
            ControlX1 = controlX1;
            ControlY1 = controlY1;
            ControlX2 = controlX2;
            ControlY2 = controlY2;
        }
    }


    /// <summary>
    /// generic data for text 
    /// </summary>
    public class TextData : VectorShapeBase
    {
        public System.Drawing.Font Font { get; set; } = new System.Drawing.Font(new System.Drawing.FontFamily(System.Drawing.Text.GenericFontFamilies.SansSerif), 8);
        public System.Drawing.Color Color { get; set; } = new System.Drawing.Color();

        public string Text { get; set; }

        public TextData(string text, float x, float y, float size) : base(x, y, 0, size )
        {
            Text = text;
            
        }
    }

    /// <summary>
    /// generic data for a graphics Pen. 
    /// </summary>
    public class PenData
    {
        int ColorARGB { get; set; }
        public byte Alpha { get; set; } = 255;
        public byte Red { get; set; } = 0;
        public byte Green { get; set; } = 0;
        public byte Blue { get; set; } = 0;
        public float Thickness { get; set; } = 1f;
    }
}
