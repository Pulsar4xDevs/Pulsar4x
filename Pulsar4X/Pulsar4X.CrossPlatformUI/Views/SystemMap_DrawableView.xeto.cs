using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;
using Pulsar4X.ViewModel.SystemView;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class SystemMap_DrawableView : Drawable
    {
        SystemMap_DrawableVM _viewModel;

        

        private bool IsMouseDown;
        public Point LastLoc;
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

            IsMouseDown = false;
            LastLoc.X = -1;
            LastLoc.Y = -1;
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

        private void SystemMap_DrawableView_MouseWheel(object sender, MouseEventArgs e)
        {
            if ((int)e.Delta.Height == 1)
            {
                _camera2.ZoomIn();
                Invalidate();
            }
            else if ((int)e.Delta.Height == -1)
            {
                _camera2.ZoomOut();
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

        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SystemMap_DrawableVM.IconableEntitys))
                IconEntitys_CollectionChanged();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Colors.DarkBlue, e.ClipRectangle);

            _iconCollection.DrawMe(e.Graphics);

            }
        }
    

}
