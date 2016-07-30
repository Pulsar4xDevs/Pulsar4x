using Pulsar4X.ECSLib;
using Eto.Drawing;

namespace Pulsar4X.CrossPlatformUI.Views
{
    internal class TextIcon : IconBase
    {

        public float Scale { get; set; } = 8;
        Font font { get; set; }
        Color color { get; set; }
        private float Zoom { get { return _camera.ZoomLevel; } }
        private Size ViewSize { get { return _camera._viewPort.Size; } }
        private PositionDB _starSysPosition;
        private Camera2dv2 _camera;
        private string _name;

        public TextIcon(Entity entity, Camera2dv2 camera)
        {
            _camera = camera;
            _starSysPosition = entity.GetDataBlob<PositionDB>();
            _name = entity.GetDataBlob<NameDB>().DefaultName;
            font = new Font(FontFamilies.Fantasy, Scale);
            color = new Color(Colors.Black);
        }

        public void DrawMe(Graphics g)
        {

            g.SaveTransform();
            //g.MultiplyTransform(PositionTransform());
            IMatrix cameraOffset = _camera.GetViewProjectionMatrix(new PointF((float)_starSysPosition.X, (float)_starSysPosition.Y));
            //apply the camera offset
            g.MultiplyTransform(cameraOffset);

            g.DrawText(font, color, (float)_starSysPosition.X, (float)_starSysPosition.Y, _name);

            g.RestoreTransform();
        }
    }

}
