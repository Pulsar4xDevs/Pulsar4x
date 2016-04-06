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
        private Camera2D _camera; 
        public SystemMap_DrawableView()
        {
            XamlReader.Load(this);
            _camera = new Camera2D(this.Size);
        }


        public void SetViewmodel(SystemMap_DrawableVM viewModel) 
        {
            _viewModel = viewModel;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            _shapesList.Add(new DrawableObject(this, viewModel.BackGroundHud, _camera));

            SystemBodies_CollectionChanged();
        }

        private void SystemBodies_CollectionChanged()
        {
            List<DrawableObject> newShapelist = new List<DrawableObject>();

            foreach (var item in _viewModel.SystemBodies)
            {
                item.Icon.PropertyChanged += ViewModel_PropertyChanged;
                newShapelist.Add(new DrawableObject(this, item.Icon, _camera));

                if (item.OrbitEllipse != null)
                {
                    //item.OrbitEllipse.PropertyChanged += ViewModel_PropertyChanged;
                    //_shapesList.Add(new DrawableObject(this, item.OrbitEllipse, _camera));
                }
                if (item.SimpleOrbitEllipse != null)
                {
                    //_shapesList.Add(new DrawableObject(this, item.SimpleOrbitEllipse, _camera));
                }
                if (item.SimpleOrbitEllipseFading != null)
                {
                    item.OrbitEllipse.PropertyChanged += ViewModel_PropertyChanged;
                    newShapelist.Add(new DrawableObject(this, item.SimpleOrbitEllipseFading, _camera));
                }
            }
            _shapesList = newShapelist;

        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SystemMap_DrawableVM.SystemBodies))
                SystemBodies_CollectionChanged();
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
        float _zoom { get { return _objectData.Scale; } }
        private VectorGraphicDataBase _objectData;
        private Camera2D _camera;
        private List<PathData> _pathDataList = new List<PathData>();
        private List<TextData> _textData = new List<TextData>();
        public DrawableObject(Drawable parent, VectorGraphicDataBase objectInfo, Camera2D camera)
        {
            _parent = parent;
            _objectData = objectInfo;
            _camera = camera;
            foreach (var pathPenDataPair in _objectData.PathList)
            {
                GraphicsPath path = new GraphicsPath();



                if (_objectData is OrbitEllipseFading)
                {
                    ArcData arcData = (ArcData)pathPenDataPair.VectorShapes[0];
                    path.AddArc(arcData.X1, arcData.X2, arcData.Width * _zoom, arcData.Height * _zoom, arcData.StartAngle, arcData.SweepAngle);

                }

                else
                {
                    foreach (var shape in pathPenDataPair.VectorShapes)
                    {
                        if (shape is EllipseData)
                            path.AddEllipse(shape.X1, shape.Y1, shape.X2, shape.Y2);
                        else if (shape is LineData)
                            path.AddLine(shape.X1 * _zoom, shape.Y1 * _zoom, shape.X2 * _zoom, shape.Y2 * _zoom);
                        else if (shape is RectangleData)
                            path.AddRectangle(shape.X1, shape.Y1, shape.X2, shape.Y2);
                        else if (shape is ArcData)
                        {
                            ArcData arcData = (ArcData)shape;
                            path.AddArc(shape.X1, shape.Y1, shape.X2, shape.Y2, arcData.StartAngle, arcData.SweepAngle);
                        }
                        else if (shape is BezierData)
                        {
                            BezierData bezData = (BezierData)shape;
                            PointF start = new PointF(bezData.X1, bezData.Y1);
                            PointF end = new PointF(bezData.X2, bezData.Y2);
                            PointF control1 = new PointF(bezData.ControlX1, bezData.ControlY1);
                            PointF control2 = new PointF(bezData.ControlX2, bezData.ControlY2);
                            path.AddBezier(start, control1, control2, end);
                        }
                        else if (shape is TextData)
                        {
                            _textData.Add((TextData)shape);

                        }
                    }
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

        private float PosXViewAdjusted { get { return _objectData.PosX * _zoom + _parent.Width * 0.5f; } }


        private float PosYViewAdjusted { get { return _objectData.PosY * _zoom + _parent.Height * 0.5f; } }


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
                g.MultiplyTransform(Matrix.FromRotationAt(_objectData.Rotation, _parent.Width * 0.5f, _parent.Height * 0.5f));
                g.TranslateTransform(PosXViewAdjusted, PosYViewAdjusted);
                
                

                g.DrawPath(pathData.EtoPen, pathData.EtoPath);
                g.RestoreTransform();
            }
            foreach (var item in _textData)
            {
                g.SaveTransform();
                g.TranslateTransform(PosXViewAdjusted, PosYViewAdjusted);

                Font font = new Font(item.Font.FontFamily.ToString(), item.Y2);
                Color color = new Color(item.Color.R, item.Color.G, item.Color.B);
                g.DrawText(font, color, item.X1, item.X2, item.Text);
                
                g.RestoreTransform();
            } 
        }
    }
}
