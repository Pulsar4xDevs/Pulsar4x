using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;
using SDL2;
using static SDL2.SDL;

namespace Pulsar4X.SDL2UI
{
    public class TranslateMoveOrderWidget : IDrawData
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

        Vector3 _transitLeavePositionRalitive; //ralitive to the parentBody
        Vector3 _transitArrivePosition;

        SDL_Point[] _linePoints;

        public TranslateMoveOrderWidget(GlobalUIState state, Entity orderingEntity)
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

            _parentPositionDB = _movingEntityCurrentOrbit.Parent.GetDataBlob<PositionDB>();
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
            //these are ralitive to thier respective bodies, for the initial default, copying the position shoul be fine.
            //however a better default would djust the distance from the target to get a circular orbit and
            //check if it's above minimum and that the resulting orbit is within soi 
            _arriveIcon.ProgradeAngle = _departIcon.ProgradeAngle; 
            OnPhysicsUpdate();
        }

        public void SetArrivalPosition(Vector3 ralitiveWorldPosition)
        {
            _transitArrivePosition = ralitiveWorldPosition;
            _arriveIcon.SetTransitPostion(_transitArrivePosition);
        }



        public void SetDepartureProgradeAngle(double angle)
        {
            _departIcon.ProgradeAngle = angle;
            _departIcon.SetTransitPostion(_transitLeavePositionRalitive);

        }
        public void SetArivalProgradeAngle(double angle)
        {
            if (_arriveIcon != null)
            {
                _arriveIcon.ProgradeAngle = angle;
                _arriveIcon.SetTransitPostion(_transitArrivePosition);
            }
        }

        public void OnPhysicsUpdate()
        {
            _currentDateTime = _state.PrimarySystemDateTime;
            if (_transitLeaveDateTime < _currentDateTime)
                _transitLeaveDateTime = _currentDateTime;

            _transitLeavePositionRalitive = OrbitProcessor.GetPosition_AU(_movingEntityCurrentOrbit, _transitLeaveDateTime);

        }

        public void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            _departIcon.OnFrameUpdate(matrix, camera);
            if (_arriveIcon != null)
            {
                _arriveIcon.OnFrameUpdate(matrix, camera);
                _linePoints = new SDL_Point[2];

                var dvsp = camera.ViewCoordinate(_departIcon.WorldPosition);
                var avsp = camera.ViewCoordinate(_arriveIcon.WorldPosition);
                _linePoints[0] = dvsp;

                var arrive = matrix.Transform(_arriveIcon.WorldPosition.X, _arriveIcon.WorldPosition.Y);
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
                SDL_SetRenderDrawColor(rendererPtr, TransitLineColor.r, TransitLineColor.g, TransitLineColor.b, TransitLineColor.a);
                SDL.SDL_RenderDrawLine(rendererPtr, _linePoints[0].x, _linePoints[0].y, _linePoints[1].x, _linePoints[1].y);

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
        //PositionDB _myPosition;

        //DateTime TransitDateTime;
        //Vector4 _transitPosition;
        Shape _progradeArrow;
        PointD[] _arrow;

        private TransitIcon(PositionDB parentPos) : base(parentPos)
        {
            _parentPosition = parentPos;
            positionByDB = true;
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
            var rotate270 = Matrix.New270DegreeMatrix();
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
        /// <param name="transitPositionOffset">Transit position offset, this is the world position ralitive to the parent body</param>
        public void SetTransitPostion(Vector3 transitPositionOffset)
        {
            _worldPosition = transitPositionOffset;
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
            Matrix rotate = Matrix.NewRotateMatrix(ProgradeAngle);
            _progradeArrow.Points = new PointD[_arrow.Length];
            for (int i = 0; i < _arrow.Length; i++)
            {
                _progradeArrow.Points[i] = rotate.TransformD(_arrow[i]);
            }
            Shapes[0] = _progradeArrow;
            base.OnFrameUpdate(matrix, camera);
        }
    }
}
