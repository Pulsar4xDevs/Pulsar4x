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
        public SystemMap_DrawableView(SystemMap_DrawableVM viewModel)
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

        protected override void OnPaint(PaintEventArgs e)
        {
            // your custom drawing
            //e.Graphics.FillRectangle(Colors.Blue, e.ClipRectangle);
            if(_viewModel != null)
            foreach (var sysBody in _viewModel.SystemBodies)
            {
                Color iconcolor = new Color();
                iconcolor.A = sysBody.Icon.Pendata.Alpha;
                iconcolor.B = sysBody.Icon.Pendata.Blue;
                iconcolor.G = sysBody.Icon.Pendata.Green;
                Pen iconPen = new Pen(iconcolor, sysBody.Icon.Pendata.Thickness);
                e.Graphics.DrawEllipse(iconPen, sysBody.Icon.PosX, sysBody.Icon.PosY, sysBody.Icon.Width, sysBody.Icon.Height);

                foreach (var item in sysBody.OrbitEllipse.ArcList)
                {
                    Color orblineColor = new Color();
                    orblineColor.A = item.Pendata.Alpha;
                    orblineColor.B = item.Pendata.Blue;
                    orblineColor.G = item.Pendata.Green;
                    Pen orblinePen = new Pen(orblineColor, item.Pendata.Thickness);
                    e.Graphics.DrawArc(orblinePen, item.PosX, item.PosY, item.Width, item.Height, item.StartAngle, item.SweepAngle);
                    e.Graphics.RotateTransform(sysBody.OrbitEllipse.AngleOfPeriapsis);
                }
                
            }
        }
    }
}
