using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using Eto.Drawing;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pulsar4X.CrossPlatformUI.Views
{
    class OrbitRing : INotifyPropertyChanged
    {
        public PercentValue OrbitPercent { private get { return _orbitPercent; }
            set { _orbitPercent = value; OnPropertyChanged(nameof(SweepAngle)); } }
        private PercentValue _orbitPercent = new PercentValue();

        public byte Segments { private get { return _segments; } //TODO we could adjust the Segments and OrbitPercent by the size of the orbit and the zoom level to get a level of detail effect.
            set { _segments = value; UpdatePens(); OnPropertyChanged(nameof(SweepAngle)); }}
        private byte _segments = 255;

        public float StartArcAngle { get; set; }

        public float SweepAngle { get { return (360f * OrbitPercent.Percent) / Segments; } }

        private List<Pen> _segmentPens = new List<Pen>();

        public Color PenColor { get { return _penColor; }
            set { _penColor = value;  UpdatePens(); OnPropertyChanged();}}
        private Color _penColor;
  
        private float TopLeftX { get { return (float)ParentPositionDB.Position.X + _width / 2; }}
        private float TopLeftY { get { return (float)ParentPositionDB.Position.Y + _height / 2; }}
        private float _width;
        private float _height;
        //this should be the angle from the orbital reference direction, to the Argument of Periapsis, as seen from above, this sets the angle for the ecentricity.
        //ie an elipse is created from a rectangle (position, width and height), then rotated so that the ecentricity is at the right angle. 
        private float _rotation; 
        private Camera2D _camera;

        private Entity OrbitEntity { get; set; }

        private OrbitDB OrbitDB { get { return OrbitEntity.GetDataBlob<OrbitDB>(); }}

        private PositionDB ParentPositionDB { get { return OrbitDB.Parent.GetDataBlob<PositionDB>(); } }

        private PositionDB BodyPositionDB { get { return OrbitEntity.GetDataBlob<PositionDB>(); } }

        public OrbitRing(Entity entityWithOrbit, Camera2D camera)
        {
            _camera = camera;
            OrbitEntity = entityWithOrbit;
            _rotation = (float)(OrbitDB.LongitudeOfAscendingNode + OrbitDB.ArgumentOfPeriapsis);
            _width = (float)OrbitDB.SemiMajorAxis * 2; //Major Axis
            _height = (float)Math.Sqrt(OrbitDB.SemiMajorAxis * Math.Sqrt(1 - OrbitDB.Eccentricity * OrbitDB.Eccentricity) 
                * OrbitDB.SemiMajorAxis * (1 - OrbitDB.Eccentricity * OrbitDB.Eccentricity)) * 2;   //minor Axis
        }

        private void UpdatePens()
        {
            List<Pen> newPens = new List<Pen>();
            for (int i = 0; i < Segments; i++)
            {                    
                Color penColor = new Color(_penColor.R, _penColor.G, _penColor.B, i / Segments);
                newPens.Add(new Pen(penColor, 1));
            }
            _segmentPens = newPens;        
        }

        public void DrawMe(Graphics g)
        {
            g.SaveTransform();
            g.RotateTransform(_rotation);
            g.MultiplyTransform(_camera.GetViewProjectionMatrix());
            int i = 0;
            foreach (var pen in _segmentPens)
            {
                g.DrawArc(pen, TopLeftX, TopLeftY, _width, _height, StartArcAngle + i * SweepAngle, SweepAngle);
                i++;
            }
            g.RestoreTransform();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
