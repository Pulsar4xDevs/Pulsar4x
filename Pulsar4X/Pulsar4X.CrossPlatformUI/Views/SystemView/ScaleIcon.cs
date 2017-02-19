using Pulsar4X.ECSLib;
using Eto.Drawing;
using System;
using System.Collections.Generic;

namespace Pulsar4X.CrossPlatformUI.Views
{
    /// <summary>
    /// ScaleIcon class to draw a Scale and a Text in the SystemView
    /// </summary>
    internal class ScaleIcon : IconBase
    {
        public float Scale { get; set; } = 8;
        public Font font { get; set; }
        public Color color { get; set; }
        public int thickness { get; set; } = 3;
        public Pen pen { get; set; }

        public int offsetRight { get; set; } = 10;
        public int offsetBottom { get; set; } = 10;
        public int TextOffsetBottom { get; set; } = 25;
        public int ScaleMinLength { get; set; } = 100;

        public KeyValuePair<int, string> sizeAndLabel { get; set; } 

        private Camera2dv2 _camera;

        /// <summary>
        /// Constructor of the ScaleIcon Class
        /// </summary>
        /// <param name="camera"></param>
        public ScaleIcon(Camera2dv2 camera)
        {
            _camera = camera;
            color = new Color(Colors.White);
            font = new Font(FontFamilies.Fantasy, Scale);
            pen = new Pen(color, thickness);
            sizeAndLabel = new KeyValuePair<int, string>(ScaleMinLength, _camera.WorldDistance(ScaleMinLength).ToString() + " au");
        }

        /// <summary>
        /// Calculates the Size of the ScaleLabel and the Text indicating the Scale.
        /// Logarithmically and in 1, 2, 5 steps
        /// </summary>
        public void calcLabelAndSize()
        {
            string unit = " au";
            double nearestWorldLength;
            double MinWorldLength = _camera.WorldDistance(ScaleMinLength);
            if (MinWorldLength > 0.001)
            {
                nearestWorldLength = Math.Pow(10, Math.Ceiling(Math.Log10(MinWorldLength)));
                if (nearestWorldLength / 5 > MinWorldLength)
                {
                    sizeAndLabel = new KeyValuePair<int, string>((int)_camera.ViewDistance((float)nearestWorldLength / 5), nearestWorldLength / 5 + unit);
                    return;
                }

                if (nearestWorldLength / 2 > MinWorldLength)
                {
                    sizeAndLabel = new KeyValuePair<int, string>((int)_camera.ViewDistance((float)nearestWorldLength / 2), nearestWorldLength / 2 + unit);
                    return;
                }

                sizeAndLabel = new KeyValuePair<int, string>((int)_camera.ViewDistance((float)nearestWorldLength), nearestWorldLength + unit);
                return;

            }
            else
            {
                MinWorldLength = _camera.WorldDistance(ScaleMinLength) * GameConstants.Units.KmPerAu;
                unit = " km";
                nearestWorldLength = Math.Pow(10, Math.Ceiling(Math.Log10(MinWorldLength)));
                if (nearestWorldLength / 5 > MinWorldLength)
                {
                    sizeAndLabel = new KeyValuePair<int, string>((int)_camera.ViewDistance((float)(nearestWorldLength / GameConstants.Units.KmPerAu / 5)), nearestWorldLength / 5 + unit);
                    return;
                }

                if (nearestWorldLength / 2 > MinWorldLength)
                {
                    sizeAndLabel = new KeyValuePair<int, string>((int)_camera.ViewDistance((float)(nearestWorldLength / GameConstants.Units.KmPerAu / 2)), nearestWorldLength / 2 + unit);
                    return;
                }

                sizeAndLabel = new KeyValuePair<int, string>((int)_camera.ViewDistance((float)(nearestWorldLength / GameConstants.Units.KmPerAu)), nearestWorldLength + unit);
                return;
            }
        }

        /// <summary>
        /// Draws the Scale
        /// </summary>
        /// <param name="g"></param>
        public void DrawMe(Graphics g)
        {
            calcLabelAndSize();
            g.DrawText(font, color,  _camera.ViewPortSize.Width - offsetRight - sizeAndLabel.Key, _camera.ViewPortSize.Height - TextOffsetBottom, sizeAndLabel.Value);
            g.DrawLine(pen, _camera.ViewPortSize.Width - offsetRight, _camera.ViewPortSize.Height - offsetBottom,
                _camera.ViewPortSize.Width - offsetRight - sizeAndLabel.Key, _camera.ViewPortSize.Height - offsetBottom);
        }
    }
}
