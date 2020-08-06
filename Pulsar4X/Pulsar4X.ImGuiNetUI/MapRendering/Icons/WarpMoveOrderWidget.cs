using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;
using Pulsar4X.Orbital;
using SDL2;
using static SDL2.SDL;

namespace Pulsar4X.SDL2UI
{
    public class WarpMoveOrderWidget : IDrawData
    {
        public SDL_Color TransitLineColor = new SDL_Color() { r = 0, g = 255, b = 255, a = 100 };

        Entity _movingEntity;
        OrbitDB _movingEntityCurrentOrbit;
        DateTime _currentDateTime;
        DateTime _transitLeaveDateTime;

        PositionDB _parentPositionDB;

        Entity _targetEntity;

        //List<Icon> _icons = new List<Icon>();

        GlobalUIState _state;

        TransitIcon _departIcon;
        TransitIcon _arriveIcon;

        PositionDB _targetPositionDB;

        Vector3 _transitLeavePositionrelative_m; //relative to the parentBody
        private Vector3 _transitArriverelativePos_m { get; set; }
        private Vector3 _transitArriveAbsolutePos_m { get; set; }

        SDL_Point[] _linePoints;

        public WarpMoveOrderWidget(GlobalUIState state, Entity orderingEntity)
        {
            _state = state;
            _currentDateTime = _state.PrimarySystemDateTime;

            _movingEntity = orderingEntity;

            Setup();
        }

        void Setup()
        {
            _movingEntityCurrentOrbit = _movingEntity.GetDataBlob<OrbitDB>();
            _transitLeaveDateTime = _currentDateTime;

            _parentPositionDB = Entity.GetSOIParentPositionDB(_movingEntity);
            _departIcon = TransitIcon.CreateDepartIcon(_parentPositionDB);
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



        public void SetArrivalTarget(Entity targetEntity)
        {
            _targetEntity = targetEntity;
            _targetPositionDB = _targetEntity.GetDataBlob<PositionDB>();

            _arriveIcon = TransitIcon.CreateArriveIcon(_targetPositionDB);
            //these are relative to thier respective bodies, for the initial default, copying the position shoul be fine.
            //however a better default would djust the distance from the target to get a circular orbit and
            //check if it's above minimum and that the resulting orbit is within soi 
            _arriveIcon.ProgradeAngle = _departIcon.ProgradeAngle; 
            OnPhysicsUpdate();
        }

        public void SetArrivalPosition(Vector3 relativeWorldPosition_m)
        {
            _transitArriverelativePos_m = relativeWorldPosition_m;
            _transitArriveAbsolutePos_m = _targetPositionDB.AbsolutePosition_m + _transitArriverelativePos_m;
            _arriveIcon.SetTransitPositon(_transitArriverelativePos_m);
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

            _transitLeavePositionrelative_m = Entity.GetRelativeFuturePosition(_movingEntity, _transitLeaveDateTime);

        }

        public void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            _departIcon.OnFrameUpdate(matrix, camera);
            if (_arriveIcon != null)
            {
                _arriveIcon.OnFrameUpdate(matrix, camera);
                _linePoints = new SDL_Point[2];

                var dvsp = camera.ViewCoordinate_m(_departIcon.WorldPosition_m);
                var avsp = camera.ViewCoordinate_m(_arriveIcon.WorldPosition_m);
                _linePoints[0] = dvsp;

                //SDL_Point arrive = matrix.Transform(_arriveIcon.WorldPosition_m.X, _arriveIcon.WorldPosition_m.Y);
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

                var x1 = _linePoints[0].x;
                var y1 = _linePoints[0].y;
                var x2 = _linePoints[1].x;
                var y2 = _linePoints[1].y;
                
                SDL_SetRenderDrawColor(rendererPtr, TransitLineColor.r, TransitLineColor.g, TransitLineColor.b, TransitLineColor.a);
                SDL_RenderDrawLine(rendererPtr, x1, y1, x2, y2);

            }
        }
    }



    public class TransitIcon : Icon
    {
        public SDL_Color PrimaryColour = new SDL_Color() { r = 0, g = 255, b = 0, a = 255 };
        public SDL_Color VectorColour = new SDL_Color() { r = 255, g = 0, b = 255, a = 255 };

        public double ProgradeAngle = 0;
        double _arrivePntRadius;
        PositionDB _parentPosition;
        //PositionDB /;

        //DateTime TransitDateTime;
        //Vector4 _transitPosition;
        Shape _progradeArrow;
        PointD[] _arrow;

        private TransitIcon(PositionDB parentPos) : base(parentPos)
        {
            _parentPosition = parentPos;
            positionByDB = true;
            //InMeters = true;
            Setup();
        }

        public static TransitIcon CreateArriveIcon(PositionDB targetPosition)
        {
            var icon = new TransitIcon(targetPosition);
            icon.CreateCheverons(0, -13);
            return icon;
        }

        public static TransitIcon CreateDepartIcon(PositionDB targetPosition)
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

            //Shapes[0] = vectorArrow; 
            //Shapes[1] = dot;
            //Shapes[2] = circle;
            //Shapes[3] = chevron;
            //Shapes[4] = chevron2;
            Shapes.Add(dot);
            Shapes.Add(circle);

        }

        void CreateCheverons(int x, int y)
        {
            PointD[] chevronPoints1 = new PointD[3];
            chevronPoints1[0] = new PointD() { X = x - 4, Y = y + 3 };
            chevronPoints1[1] = new PointD() { X = x + 0, Y = y - 3 };
            chevronPoints1[2] = new PointD() { X = x + 4, Y = y + 3 };
            Shape chevron = new Shape()
            {
                Points = chevronPoints1,
                Color = PrimaryColour
            };
            PointD[] chevronPoints2 = new PointD[3];
            chevronPoints2[0] = new PointD() { X = x - 4, Y = y + 7 };
            chevronPoints2[1] = new PointD() { X = x + 0, Y = y + 1 };
            chevronPoints2[2] = new PointD() { X = x + 4, Y = y + 7 };
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
            PointD[] arrowPoints = CreatePrimitiveShapes.CreateArrow(24);
            /*
            List<PointD> arrowPoints = new List<PointD>(pnts.Length);
            foreach (var point in pnts)
            {
                double x = point.X * Math.Cos(ProgradeAngle) - point.Y * Math.Sin(ProgradeAngle);
                double y = point.X * Math.Sin(ProgradeAngle) + point.Y * Math.Cos(ProgradeAngle);
                arrowPoints.Add(new PointD() { X = x, Y = y });
            }
            */
            var rotate270 = Matrix.IDRotate270Deg();
            _arrow = new PointD[arrowPoints.Length];
            for (int i = 0; i < _arrow.Length; i++)
            {
                _arrow[i] = rotate270.TransformD(arrowPoints[i]);
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

        /// <summary>
        /// Sets the transit position from the prograde Angle and distance from the body. 
        /// </summary>
        /// <param name="progradeAngle">Prograde angle.</param>
        /// <param name="radius_AU">Radius au.</param>
        /*
        public void SetTransitPostion(double progradeAngle, double radius_AU)
        {
            ProgradeAngle = progradeAngle;
            _arrivePntRadius = radius_AU;
            var theta = progradeAngle;// + Math.PI * 0.5;
            _worldPosition.X = Math.Sin(theta) * radius_AU;
            _worldPosition.Y = Math.Cos(theta) * radius_AU;
            OnPhysicsUpdate();
        }
        */
        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            //rotate the progradeArrow.
            Matrix rotate = Matrix.IDRotate(ProgradeAngle);
            _progradeArrow.Points = new PointD[_arrow.Length];
            for (int i = 0; i < _arrow.Length; i++)
            {
                _progradeArrow.Points[i] = rotate.TransformD(_arrow[i]);
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
                PointD[] drawPoints = new PointD[shape.Points.Length];
                
                for (int i2 = 0; i2 < shape.Points.Length; i2++)
                {
                    int x;
                    int y;

                    var tranlsatedPoint = nonZoomMatrix.TransformD( shape.Points[i2].X,  shape.Points[i2].Y);
                    x = (int)(ViewScreenPos.x + tranlsatedPoint.X );
                    y = (int)(ViewScreenPos.y + tranlsatedPoint.Y );
                    drawPoints[i2] = new PointD() { X = x, Y = y };
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
                SDL.SDL_SetRenderDrawColor(rendererPtr, shape.Color.r, shape.Color.g, shape.Color.b, shape.Color.a);

                for (int i = 0; i < shape.Points.Length - 1; i++)
                {
                    var x1 = Convert.ToInt32(shape.Points[i].X);
                    var y1 = Convert.ToInt32(shape.Points[i].Y);
                    var x2 = Convert.ToInt32(shape.Points[i+1].X);
                    var y2 = Convert.ToInt32(shape.Points[i+1].Y);
                    SDL.SDL_RenderDrawLine(rendererPtr, x1, y1, x2, y2);
                }
            }

        }
    }
}
