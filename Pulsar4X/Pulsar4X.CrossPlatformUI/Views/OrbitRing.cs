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

        public byte Segments { private get { return _segments; }
            set { _segments = value; OnPropertyChanged(nameof(SweepAngle)); }}
        private byte _segments = 255;

        public float StartArcAngle { get; set; }

        public float SweepAngle { get { return (360f * OrbitPercent.Percent) / Segments; } }
        private List<Pen> _segmentPens;

        //some of this stuff will be related to the camera, still not sure how to do this.
        float TopLeftX = 0;
        float TopLeftY = 0;
        float Width;
        float Height;
        float Rotation = 0; //this should be the angle from the orbital reference direction, to the Argument of Periapsis, as seen from above.


        private Entity OrbitEntity { get; set; }

        private OrbitDB OrbitDB { get { return OrbitEntity.GetDataBlob<OrbitDB>(); }}

        private PositionDB ParentPositionDB { get { return OrbitDB.Parent.GetDataBlob<PositionDB>(); } }

        private PositionDB BodyPositionDB { get { return OrbitEntity.GetDataBlob<PositionDB>(); } }


        public OrbitRing(Entity entityWithOrbit)
        {
            OrbitEntity = entityWithOrbit;
        }

        private void UpdatePens()
        {
            if (_segmentPens.Count != Segments)
            {
                List<Pen> newPens = new List<Pen>();
                for (int i = 0; i < Segments; i++)
                {                    
                    Color penColor = new Color(255, 248, 220, i / Segments);
                    newPens.Add(new Pen(penColor, 1));
                }
                _segmentPens = newPens;
            }
        }

        public void DrawMe(Graphics g)
        {
            g.SaveTransform();
            g.RotateTransform(Rotation);
            //g.TranslateTransform();
            //g.ScaleTransform(); //for zoom?
            int i = 0;
            foreach (var pen in _segmentPens)
            {
                g.DrawArc(pen, TopLeftX, TopLeftY, Width, Height, StartArcAngle + i * SweepAngle, SweepAngle);
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
