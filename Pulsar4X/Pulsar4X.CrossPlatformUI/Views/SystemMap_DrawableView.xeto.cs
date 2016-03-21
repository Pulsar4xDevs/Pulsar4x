using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel.SystemView;
using Pulsar4X.ViewModel;

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

    public class PathData
    {
        public Pen EtoPen { get; set; }
        public PenData PenData { get; set; }
        public GraphicsPath EtoPath { get; set; }
        public PathData(Pen etoPen, PenData penData, GraphicsPath etoPath)
        {
            EtoPen = etoPen;
            PenData = penData;
            EtoPath = etoPath;
        }
    }

    public class DrawableObject
    {

        private Drawable _parent;
        float _zoom { get { return _objectData.Zoom; } }
        private VectorGraphicDataBase _objectData;

        private List<PathData> _pathDataList = new List<PathData>();

        public DrawableObject(Drawable parent, VectorGraphicDataBase objectInfo)
        {
            _parent = parent;
            _objectData = objectInfo;
           
            foreach (var pathPenDataPair in _objectData.PathList)
            {
                GraphicsPath path = new GraphicsPath();
                if (_objectData is IconData)
                    
                    foreach (var shape in pathPenDataPair.VectorShapes)
                    {
                        if (shape is EllipseData)
                            path.AddEllipse(shape.X1, shape.Y1, shape.X2, shape.Y2);
                        else if (shape is RectangleData)
                            path.AddRectangle(shape.X1, shape.Y1, shape.X2, shape.Y2);
                        else if (shape is ArcData)
                        {
                            ArcData arcData = (ArcData)pathPenDataPair.VectorShapes[0];
                            path.AddArc(shape.X1, shape.Y1, shape.X2, shape.Y2, arcData.StartAngle, arcData.SweepAngle);
                        }
                    }
                    

                else if (_objectData is OrbitEllipseFading)
                { 
                    ArcData arcData = (ArcData)pathPenDataPair.VectorShapes[0];
                    path.AddArc(arcData.X1, arcData.X2, arcData.Width * _zoom, arcData.Height * _zoom, arcData.StartAngle, arcData.SweepAngle);
                    
                }
                Color iconcolor = new Color();
                iconcolor.Ab = pathPenDataPair.Pen.Alpha;
                iconcolor.Rb = pathPenDataPair.Pen.Red;
                iconcolor.Bb = pathPenDataPair.Pen.Blue;
                iconcolor.Gb = pathPenDataPair.Pen.Green;

                Pen pen = new Pen(iconcolor, pathPenDataPair.Pen.Thickness);

                PathData pathData = new PathData(pen, pathPenDataPair.Pen, path);
                _pathDataList.Add(pathData);
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

        private Pen UpdatePen(PenData penData, Pen penEto)
        {
            Color newColor = new Color();
            newColor.Ab = penData.Alpha;
            newColor.Rb = penData.Red;
            newColor.Bb = penData.Blue;
            newColor.Gb = penData.Green;

            penEto.Color = newColor;
            penEto.Thickness = penData.Thickness;
            return penEto;
        }

        public void DrawMe(Graphics g)
        {
            foreach (var pathData in _pathDataList)
            {
                UpdatePen(pathData.PenData, pathData.EtoPen);

                g.SaveTransform();

                g.MultiplyTransform(Matrix.FromRotationAt(_objectData.Rotation, PosXViewAdjusted, PosYViewAdjusted));
                g.TranslateTransform(ViewPosX, ViewPosY);
                
                g.DrawPath(pathData.EtoPen, pathData.EtoPath);
                g.RestoreTransform();
            }  
        }
    }
}
