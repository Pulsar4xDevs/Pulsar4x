using System;
using System.Collections.Generic;
using Pulsar4X.Interfaces;
using Pulsar4X.Orbital;
using SDL3;

namespace Pulsar4X.Client
{
    public class WarpMoveOrderWidget : IDrawData
    {
        public SDL.Color TransitLineColor = new SDL.Color() { R = 0, G = 255, B = 255, A = 100 };

        readonly GlobalUIState _state;
        readonly string _systemId;
        readonly int _movingEntityId;

        DateTime _currentDateTime;
        DateTime _transitLeaveDateTime;

        int? _targetEntityId;
        IPosition? _targetPosition;

        TransitIcon _departIcon;
        TransitIcon? _arriveIcon;

        Vector3 _transitLeavePositionrelative_m; //relative to the parentBody
        private Vector3 _transitArriverelativePos_m { get; set; }

        SDL.Point[] _linePoints = new SDL.Point[2];

        public WarpMoveOrderWidget(GlobalUIState state, string systemId, int movingEntityId, int soiParentId)
        {
            _state = state;
            _systemId = systemId;
            _movingEntityId = movingEntityId;
            _currentDateTime = _state.PrimarySystemDateTime;
            _transitLeaveDateTime = _currentDateTime;

            _departIcon = TransitIcon.CreateDepartIcon(new SnapshotPosition(state, systemId, soiParentId));
            OnPhysicsUpdate();
        }

        public void SetDepartDateTime(DateTime dateTime)
        {
            if (dateTime > _currentDateTime)
                _transitLeaveDateTime = dateTime;
            else
                _transitLeaveDateTime = _currentDateTime;
            OnPhysicsUpdate();
        }

        public void SetArrivalTarget(int targetEntityId)
        {
            _targetEntityId = targetEntityId;
            _targetPosition = new SnapshotPosition(_state, _systemId, targetEntityId);

            _arriveIcon = TransitIcon.CreateArriveIcon(_targetPosition);
            //these are relative to thier respective bodies, for the initial default, copying the position shoul be fine.
            //however a better default would djust the distance from the target to get a circular orbit and
            //check if it's above minimum and that the resulting orbit is within soi
            _arriveIcon.ProgradeAngle = _departIcon.ProgradeAngle;
            OnPhysicsUpdate();
        }

        public void SetArrivalPosition(Vector3 relativeWorldPosition_m)
        {
            _transitArriverelativePos_m = relativeWorldPosition_m;
            _arriveIcon?.SetTransitPositon(_transitArriverelativePos_m);
        }

        public void SetDepartureProgradeAngle(double angle)
        {
            _departIcon.ProgradeAngle = angle;
            _departIcon.SetTransitPositon(_transitLeavePositionrelative_m);
        }

        public void SetArivalProgradeAngle(double angle)
        {
            if (_arriveIcon != null)
            {
                _arriveIcon.ProgradeAngle = angle;
                _arriveIcon.SetTransitPositon(_transitArriverelativePos_m);
            }
        }

        public void OnPhysicsUpdate()
        {
            _currentDateTime = _state.PrimarySystemDateTime;
            if (_transitLeaveDateTime < _currentDateTime)
                _transitLeaveDateTime = _currentDateTime;

            var system = _state.GameClient?.Galaxy.GetSystem(_systemId);
            var mover = system?.GetEntity(_movingEntityId);
            if (mover != null)
                _transitLeavePositionrelative_m = mover.GetRelativeState(_transitLeaveDateTime).pos;
        }

        public void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            _departIcon.OnFrameUpdate(matrix, camera);
            if (_arriveIcon != null)
            {
                _arriveIcon.OnFrameUpdate(matrix, camera);

                var dvsp = camera.ViewCoordinate_m(_departIcon.WorldPosition_m);
                var avsp = camera.ViewCoordinate_m(_arriveIcon.WorldPosition_m);
                _linePoints[0] = dvsp;
                _linePoints[1] = avsp;
            }
        }

        public void Draw(IntPtr rendererPtr, Camera camera)
        {
            _departIcon.Draw(rendererPtr, camera);
            if (_arriveIcon != null)
            {
                _arriveIcon.Draw(rendererPtr, camera);
                //draw the transitLine

                var x1 = _linePoints[0].X;
                var y1 = _linePoints[0].Y;
                var x2 = _linePoints[1].X;
                var y2 = _linePoints[1].Y;

                SDL.SetRenderDrawColor(rendererPtr, TransitLineColor.R, TransitLineColor.G, TransitLineColor.B, TransitLineColor.A);
                SDL.RenderLine(rendererPtr, x1, y1, x2, y2);
            }
        }
    }

    public class TransitIcon : Icon
    {
        public SDL.Color PrimaryColour = new SDL.Color() { R = 0, G = 255, B = 0, A = 255 };
        public SDL.Color VectorColour = new SDL.Color() { R = 255, G = 0, B = 255, A = 255 };

        public double ProgradeAngle = 0;

        //DateTime TransitDateTime;
        //Vector4 _transitPosition;
        Shape _progradeArrow;
        Orbital.Vector2[] _arrow;

        private TransitIcon(IPosition parentPos) : base(parentPos)
        {
            positionByDB = true;
            //InMeters = true;
            Setup();
        }

        public static TransitIcon CreateArriveIcon(IPosition targetPosition)
        {
            var icon = new TransitIcon(targetPosition);
            icon.CreateCheverons(0, -13);
            return icon;
        }

        public static TransitIcon CreateDepartIcon(IPosition targetPosition)
        {
            var icon = new TransitIcon(targetPosition);
            icon.CreateCheverons(0, 11);
            return icon;
        }

        void Setup()
        {

            Shapes = new List<Shape>(5);
            CreateProgradeArrow();

            Shape dot = new Shape()
            {
                Points = CreatePrimitiveShapes.Circle(0, 0, 3, 6),
                Color = PrimaryColour
            };
            Shape circle = new Shape()
            {
                Points = CreatePrimitiveShapes.Circle(0, 0, 8, 12),
                Color = PrimaryColour
            };

            Shapes.Add(dot);
            Shapes.Add(circle);

        }

        void CreateCheverons(int x, int y)
        {
            Orbital.Vector2[] chevronPoints1 = new Orbital.Vector2[3];
            chevronPoints1[0] = new Orbital.Vector2() { X = x - 4, Y = y + 3 };
            chevronPoints1[1] = new Orbital.Vector2() { X = x + 0, Y = y - 3 };
            chevronPoints1[2] = new Orbital.Vector2() { X = x + 4, Y = y + 3 };
            Shape chevron = new Shape()
            {
                Points = chevronPoints1,
                Color = PrimaryColour
            };
            Orbital.Vector2[] chevronPoints2 = new Orbital.Vector2[3];
            chevronPoints2[0] = new Orbital.Vector2() { X = x - 4, Y = y + 7 };
            chevronPoints2[1] = new Orbital.Vector2() { X = x + 0, Y = y + 1 };
            chevronPoints2[2] = new Orbital.Vector2() { X = x + 4, Y = y + 7 };
            Shape chevron2 = new Shape()
            {
                Points = chevronPoints2,
                Color = PrimaryColour
            };

            Shapes.Add(chevron);
            Shapes.Add(chevron2);
        }

        void CreateProgradeArrow()
        {
            Orbital.Vector2[] arrowPoints = CreatePrimitiveShapes.CreateArrow(24);

            var rotate270 = Matrix.IDRotate270Deg();
            _arrow = new Orbital.Vector2[arrowPoints.Length];
            for (int i = 0; i < _arrow.Length; i++)
            {
                _arrow[i] = rotate270.TransformToVector2(arrowPoints[i]);
            }

            _progradeArrow = new Shape()
            {
                Points = _arrow,
                Color = VectorColour
            };

            if (Shapes.Count < 1)
                Shapes.Add(_progradeArrow);
            else
                Shapes[0] = _progradeArrow;

        }

        /// <summary>
        /// Sets the transit postion.
        /// </summary>
        /// <param name="transitPositionrelative_m">Transit position offset, this is the world position relative to the parent body</param>
        public void SetTransitPositon(Vector3 transitPositionrelative_m)
        {
            _worldPosition_m = transitPositionrelative_m;

            OnPhysicsUpdate();
        }

        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            //rotate the progradeArrow.
            Matrix rotate = Matrix.IDRotate(ProgradeAngle);
            _progradeArrow.Points = new Orbital.Vector2[_arrow.Length];
            for (int i = 0; i < _arrow.Length; i++)
            {
                _progradeArrow.Points[i] = rotate.TransformToVector2(_arrow[i]);
            }
            Shapes[0] = _progradeArrow;

            ViewScreenPos = camera.ViewCoordinate_m(WorldPosition_m);

            var mirrorMtx = Matrix.IDMirror(true, false);
            var scaleMtx = Matrix.IDScale(Scale, Scale);
            Matrix nonZoomMatrix = mirrorMtx * scaleMtx;

            DrawShapes = new Shape[this.Shapes.Count];
            for (int i = 0; i < Shapes.Count; i++)
            {
                var shape = Shapes[i];
                Orbital.Vector2[] drawPoints = new Orbital.Vector2[shape.Points.Length];

                for (int i2 = 0; i2 < shape.Points.Length; i2++)
                {
                    int x;
                    int y;

                    var tranlsatedPoint = nonZoomMatrix.TransformToVector2( shape.Points[i2].X,  shape.Points[i2].Y);
                    x = (int)(ViewScreenPos.X + tranlsatedPoint.X );
                    y = (int)(ViewScreenPos.Y + tranlsatedPoint.Y );
                    drawPoints[i2] = new Orbital.Vector2() { X = x, Y = y };
                }
                DrawShapes[i] = new Shape() { Points = drawPoints, Color = shape.Color };
            }
        }

        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            if (DrawShapes == null)
                return;
            foreach (var shape in DrawShapes)
            {
                SDL.SetRenderDrawColor(rendererPtr, shape.Color.R, shape.Color.G, shape.Color.B, shape.Color.A);

                for (int i = 0; i < shape.Points.Length - 1; i++)
                {
                    var x1 = Convert.ToInt32(shape.Points[i].X);
                    var y1 = Convert.ToInt32(shape.Points[i].Y);
                    var x2 = Convert.ToInt32(shape.Points[i+1].X);
                    var y2 = Convert.ToInt32(shape.Points[i+1].Y);
                    SDL.RenderLine(rendererPtr, x1, y1, x2, y2);
                }
            }

        }
    }
}
