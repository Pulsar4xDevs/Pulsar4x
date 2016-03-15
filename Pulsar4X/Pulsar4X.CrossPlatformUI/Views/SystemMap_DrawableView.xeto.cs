using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel.SystemView;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class SystemMap_DrawableView : Drawable
    {
        SystemMap_DrawableVM _viewModel;
        public SystemMap_DrawableView()
        {
            XamlReader.Load(this);
        }


        public void SetViewmodel(SystemMap_DrawableVM viewModel)
        {
            _viewModel = viewModel;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            foreach (var item in viewModel.SystemBodies)
            {
                item.Icon.PropertyChanged += ViewModel_PropertyChanged;
                item.OrbitEllipse.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Invalidate();
        }

        private float ViewPosX(float sysPos)
        {
            return (sysPos * _zoom) + (this.Width / 2);
        }
        private float ViewPosY(float sysPos)
        {
            return (sysPos * _zoom) + (this.Height / 2) ;
        }
        private float _zoom = 100;
        protected override void OnPaint(PaintEventArgs e)
        {
            // your custom drawing
            e.Graphics.FillRectangle(Colors.Blue, e.ClipRectangle);

            if (_viewModel != null)
            {

                foreach (var sysBody in _viewModel.SystemBodies)
                {
                    Color iconcolor = new Color();
                    iconcolor.A = sysBody.Icon.Pendata.Alpha;
                    iconcolor.B = sysBody.Icon.Pendata.Blue;
                    iconcolor.G = sysBody.Icon.Pendata.Green;
                    
                    Pen iconPen = new Pen(Colors.Green, sysBody.Icon.Pendata.Thickness);
                    e.Graphics.DrawEllipse(iconPen, ViewPosX(sysBody.Icon.PosX), ViewPosY(sysBody.Icon.PosY), sysBody.Icon.Width, sysBody.Icon.Height);

                    foreach (var item in sysBody.OrbitEllipse.ArcList)
                    {
                        Color orblineColor = new Color();
                        orblineColor.A = item.Pendata.Alpha;
                        orblineColor.B = item.Pendata.Blue;
                        orblineColor.G = item.Pendata.Green;
                        Pen orblinePen = new Pen(Colors.White, item.Pendata.Thickness);
                        e.Graphics.DrawArc(orblinePen, ViewPosX(item.PosX), ViewPosY(item.PosY), item.Width, item.Height, item.StartAngle, item.SweepAngle);
                        //e.Graphics.RotateTransform(sysBody.OrbitEllipse.AngleOfPeriapsis);
                    }

                }
            }
        }
    }
}
