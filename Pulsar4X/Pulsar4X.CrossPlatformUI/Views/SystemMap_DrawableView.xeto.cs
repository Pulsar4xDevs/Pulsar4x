using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;
using Pulsar4X.ViewModel.SystemView;
using Pulsar4X.ViewModel;
using System.Diagnostics;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class SystemMap_DrawableView : Drawable
    {
        SystemMap_DrawableVM _viewModel;

        Stopwatch stopwatch = new Stopwatch();

        private bool IsMouseDown = false;
        public Point LastLoc = new Point(-1, -1);
        private PointF LastOffset;

        private IconCollection _iconCollection = new IconCollection();
        private Camera2dv2 _camera2;

        public SystemMap_DrawableView()
        {
            XamlReader.Load(this);
            _camera2 = new Camera2dv2(this);
            this.MouseDown += SystemMap_DrawableView_MouseDown;
            this.MouseUp += SystemMap_DrawableView_MouseUp;
            this.MouseWheel += SystemMap_DrawableView_MouseWheel;
            this.MouseMove += SystemMap_DrawableView_MouseMove;
        }

        private void SystemMap_DrawableView_MouseMove(object sender, MouseEventArgs e)
        {
            
            if (IsMouseDown == true)
            {
                _camera2.WorldOffset(e.Location - LastLoc);

                LastOffset = e.Location;
                Invalidate();
            }
            LastLoc = (Point)e.Location;
        }

        /// <summary>
        /// Zoom in and out to the mouse position using the mousewheel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SystemMap_DrawableView_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta.Height > 0)
            {
                _camera2.ZoomIn(e.Location);
                Invalidate();
            }
            else if (e.Delta.Height < 0)
            {
                _camera2.ZoomOut(e.Location);
                Invalidate();
            }
        }

        private void SystemMap_DrawableView_MouseUp(object sender, MouseEventArgs e)
        {
            IsMouseDown = false;
        }

        public void SetViewmodel(SystemMap_DrawableVM viewModel) 
        {
            _viewModel = viewModel;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            viewModel.SystemSubpulse.SystemDateChangedEvent += SystemSubpulse_SystemDateChangedEvent;
            IconEntitys_CollectionChanged();
        }

        private void SystemSubpulse_SystemDateChangedEvent(DateTime newDate)
        {
            Invalidate();
        }

        private void SystemMap_DrawableView_MouseDown(object sender, MouseEventArgs e)
        {
            IsMouseDown = true;
        }

        private void IconEntitys_CollectionChanged()
        {
            _iconCollection.Init(_viewModel.IconableEntitys, _camera2);
            Invalidate();

        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SystemMap_DrawableVM.IconableEntitys))
                IconEntitys_CollectionChanged();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            
            stopwatch.Start();
            e.Graphics.FillRectangle(Colors.DarkBlue, e.ClipRectangle);

            _iconCollection.DrawMe(e.Graphics);

            string lastDrawTime = stopwatch.ElapsedMilliseconds.ToString();
            stopwatch.Reset();
            Font font = new Font(FontFamilies.Fantasy, 8);
            Color color = new Color(Colors.White);
            PointF loc = new PointF(0, 0);
            e.Graphics.DrawText(font, color, loc, lastDrawTime);
        }
    }
    

}
