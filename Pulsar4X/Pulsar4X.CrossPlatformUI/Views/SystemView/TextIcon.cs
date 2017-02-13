using Pulsar4X.ECSLib;
using System.Collections.Generic;
using Eto.Drawing;
using System;

namespace Pulsar4X.CrossPlatformUI.Views
{
    internal class TextIcon : IconBase, IComparable<TextIcon>
    {
        public float Scale { get; set; } = 8;
        public Font font { get; set; }
        public Color color { get; set; }
        public int padding { get; set; } = 1;

        public PointF ViewPosition { get { return _camera.ViewCoordinate(new PointF((float)worldPosition.X, (float)worldPosition.Y)) + ViewOffset; } }
        public SizeF ViewNameSize { get { return new SizeF(font.Size * name.Length + padding, font.LineHeight + padding); } }
        public PointF DefaultViewOffset { get; set; }
        public PointF ViewOffset { get; set; } = new PointF(0, 0);
        public Rectangle ViewDisplayRect { get { return new Rectangle((Point)ViewPosition, (Size)ViewNameSize); } }

        public PositionDB worldPosition { get; }

        public string name { get; set; }

        private Camera2dv2 _camera;

        /// <summary>
        /// Constructor for a new Texticon
        /// </summary>
        /// <param name="entity">The Entity that stores the textstring and position</param>
        /// <param name="camera">The Cameraobject where the Texticon is drawn</param>
        public TextIcon(Entity entity, Camera2dv2 camera)
        {
            _camera = camera;
            worldPosition = entity.GetDataBlob<PositionDB>();
            name = entity.GetDataBlob<NameDB>().DefaultName;
            font = new Font(FontFamilies.Fantasy, Scale);
            color = new Color(Colors.White);
            DefaultViewOffset = new PointF(8, -font.LineHeight/2);
        }


        /// <summary>
        /// Default comparer, based on worldposition.
        /// Sorts Bottom to top, left to right, then alphabetically
        /// </summary>
        /// <param name="compareIcon"></param>
        /// <returns></returns>
        public int CompareTo(TextIcon compareIcon)
        {
            if (this.worldPosition.Y > compareIcon.worldPosition.Y) return -1;
            else if (this.worldPosition.Y < compareIcon.worldPosition.Y) return 1;
            else
            {
                if (this.worldPosition.X > compareIcon.worldPosition.X) return 1;
                else if (this.worldPosition.X < compareIcon.worldPosition.X) return -1;
                else return -this.name.CompareTo(compareIcon.name);
            }
        }

        /// <summary>
        /// Draws the Name of the Item
        /// </summary>
        /// <param name="g"></param>
        public void DrawMe(Graphics g)
        {
            g.SaveTransform();
            IMatrix cameraOffset = _camera.GetViewProjectionMatrix(new PointF((float)worldPosition.X, (float)worldPosition.Y));
            g.MultiplyTransform(cameraOffset);
            g.TranslateTransform(ViewOffset);
            g.DrawText(font, color, (float)worldPosition.X, (float)worldPosition.Y, name);

            g.RestoreTransform();
        }
    }
}