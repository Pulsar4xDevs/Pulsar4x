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
        private List<DrawableObject> _shapesList = new List<DrawableObject>();
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
                _shapesList.Add(new DrawableObject(this, item.Icon));

                if (item.OrbitEllipse != null)
                {
                    item.OrbitEllipse.PropertyChanged += ViewModel_PropertyChanged;

                    foreach (var arc in item.OrbitEllipse.ArcList)
                    {
                        _shapesList.Add(new DrawableObject(this, arc));
                    }
                }
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Invalidate();
        }


        private float _zoom = 100;
        protected override void OnPaint(PaintEventArgs e)
        {
            // your custom drawing
            e.Graphics.FillRectangle(Colors.DarkBlue, e.ClipRectangle);

            if (_viewModel != null)
            {
                foreach (var item in _shapesList)
                {
                    item.DrawMe(e.Graphics);
                }
            }
        }
    }

    public class DrawableObject
    {
        private Pen _pen;
        private Drawable _parent;
        float _zoom { get { return _objectData.Zoom; } }
        private VectorGraphicDataBase _objectData;
        public DrawableObject(Drawable parent, VectorGraphicDataBase objectInfo)
        {
            _parent = parent;
            _objectData = objectInfo;
            Color iconcolor = new Color();
            iconcolor.Ab = objectInfo.Pendata.Alpha;
            iconcolor.Rb = objectInfo.Pendata.Red;
            iconcolor.Bb = objectInfo.Pendata.Blue;
            iconcolor.Gb = objectInfo.Pendata.Green;
            _pen = new Pen(iconcolor, objectInfo.Pendata.Thickness);

        }
        private float ViewPosX(float sysPos)
        {
            float posAdjust = sysPos * _zoom + _parent.Width / 2;   //adjust position for viewscreen.
            float sizeAdjust = _objectData.Width / 2;               //adjust position for size
            if (_objectData.SizeAffectedbyZoom)                     //if the size of the vectorimage should be affected by zooming. 
                sizeAdjust *= _zoom;
            return  posAdjust - sizeAdjust;
        }
        private float ViewPosY(float sysPos)
        {
            float posAdjust = sysPos * _zoom + _parent.Height / 2;
            float sizeAdjust = _objectData.Height / 2;
            if (_objectData.SizeAffectedbyZoom)
                sizeAdjust *= _zoom;
            return posAdjust - sizeAdjust;
        }
        public void DrawMe(Graphics g)
        {
            if (_objectData is IconData)
                g.DrawEllipse(_pen, ViewPosX(_objectData.PosX), ViewPosY(_objectData.PosY), _objectData.Width, _objectData.Height);
            else if (_objectData is ArcData)
            {
                ArcData arcData = (ArcData)_objectData;
                g.DrawArc(_pen, ViewPosX(_objectData.PosX) , ViewPosY(_objectData.PosY), _objectData.Width * _zoom, _objectData.Height * _zoom, arcData.StartAngle, arcData.SweepAngle);
            }
        }
    }
}
