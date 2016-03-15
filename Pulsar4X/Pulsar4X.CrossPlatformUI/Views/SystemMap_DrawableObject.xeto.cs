using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class SystemMap_DrawableObject : Drawable
    {
        ViewModel.SystemView.SystemObjectGraphicsInfo _viewModel;
        public SystemMap_DrawableObject()
        {
            XamlReader.Load(this);
        }
        public SystemMap_DrawableObject(ViewModel.SystemView.SystemObjectGraphicsInfo viewModel) :this()
        {
            _viewModel = viewModel;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // your custom drawing
            //e.Graphics.FillRectangle(Colors.Blue, e.ClipRectangle);

            Color iconcolor = new Color();
            iconcolor.A = _viewModel.Icon.Pendata.Alpha;
            iconcolor.B = _viewModel.Icon.Pendata.Blue;
            iconcolor.G = _viewModel.Icon.Pendata.Green;
            Pen iconPen = new Pen(iconcolor, _viewModel.Icon.Pendata.Thickness);
            e.Graphics.DrawEllipse(iconPen, _viewModel.Icon.PosX, _viewModel.Icon.PosY, _viewModel.Icon.Width, _viewModel.Icon.Height);

            foreach (var item in _viewModel.OrbitEllipse.ArcList)
            {
                Color orblineColor = new Color();
                orblineColor.A = item.Pendata.Alpha;
                orblineColor.B = item.Pendata.Blue;
                orblineColor.G = item.Pendata.Green;
                Pen orblinePen = new Pen(orblineColor, item.Pendata.Thickness);
                e.Graphics.DrawArc(orblinePen, item.PosX, item.PosY, item.Width, item.Height, item.StartAngle, item.SweepAngle);
                e.Graphics.RotateTransform(_viewModel.OrbitEllipse.AngleOfPeriapsis);
            }           
        }
    }
}
