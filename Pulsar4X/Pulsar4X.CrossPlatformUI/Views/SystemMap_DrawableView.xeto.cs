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
                    _shapesList.Add(new DrawableObject(this, item.OrbitEllipse));
                }
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Colors.DarkBlue, e.ClipRectangle);

            foreach (var item in _shapesList)
            {
                item.DrawMe(e.Graphics);
            }
        }
    }

    public class DrawableObject
    {

        private Drawable _parent;
        float _zoom { get { return _objectData.Zoom; } }
        private VectorGraphicDataBase _objectData;

        private Dictionary<GraphicsPath, Pen> _pathDictionary = new Dictionary<GraphicsPath, Pen>();

        public DrawableObject(Drawable parent, VectorGraphicDataBase objectInfo)
        {
            _parent = parent;
            _objectData = objectInfo;

            foreach (var pathPenPair in _objectData.PathList)
            {
                    GraphicsPath path = new GraphicsPath();
                if (_objectData is IconData)
                    foreach (var shape in pathPenPair.VectorShapes)
                    {
                        if (shape is EllipseData)
                            path.AddEllipse(shape.X1, shape.Y1, shape.X2, shape.Y2);
                        else if (shape is RectangleData)
                            path.AddRectangle(shape.X1, shape.Y1, shape.X2, shape.Y2);
                        else if (shape is ArcData)
                        {
                            ArcData arcData = (ArcData)pathPenPair.VectorShapes[0];
                            path.AddArc(shape.X1, shape.Y1, shape.X2, shape.Y2, arcData.StartAngle, arcData.SweepAngle);
                        }
                    }
                    

                else if (_objectData is OrbitEllipseFading)
                { 
                    ArcData arcData = (ArcData)pathPenPair.VectorShapes[0];
                    path.AddArc(arcData.X1, arcData.X2, arcData.Width * _zoom, arcData.Height * _zoom, arcData.StartAngle, arcData.SweepAngle);
                    
                }
                Color iconcolor = new Color();
                iconcolor.Ab = pathPenPair.Pen.Alpha;
                iconcolor.Rb = pathPenPair.Pen.Red;
                iconcolor.Bb = pathPenPair.Pen.Blue;
                iconcolor.Gb = pathPenPair.Pen.Green;

                _pathDictionary.Add(path, new Pen(iconcolor, pathPenPair.Pen.Thickness));               
            }
        }

        private float PosXViewAdjusted { get { return _objectData.PosX * _zoom + _parent.Width / 2; } }

        private float ViewPosX
        {
            get
            {
                float sizeAdjust = _objectData.Width / 2;               //adjust position for size
                if (_objectData.SizeAffectedbyZoom)                     //if the size of the vectorimage should be affected by zooming. 
                    sizeAdjust *= _zoom;
                return PosXViewAdjusted - sizeAdjust;
            }
        }

        private float PosYViewAdjusted { get { return _objectData.PosY * _zoom + _parent.Height / 2; } }

        private float ViewPosY
        {
            get
            {                
                float sizeAdjust = _objectData.Height / 2;
                if (_objectData.SizeAffectedbyZoom)
                    sizeAdjust *= _zoom;
                return PosYViewAdjusted - sizeAdjust;                
            }
        }

        public void DrawMe(Graphics g)
        {
            foreach (var item in _pathDictionary)
            {
                g.SaveTransform();
                g.TranslateTransform(ViewPosX, ViewPosY);
                g.MultiplyTransform(Matrix.FromRotationAt(_objectData.Rotation, PosXViewAdjusted, PosYViewAdjusted));
                g.DrawPath(item.Value, item.Key);
                g.RestoreTransform();
            }  
        }
    }
}
