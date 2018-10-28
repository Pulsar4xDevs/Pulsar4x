using System;
using ImGuiNET;
using Pulsar4X.ECSLib;
using System.Numerics;

namespace Pulsar4X.SDL2UI
{
    public class OrbitOrderWindow : PulsarGuiWindow// IOrderWindow
    {
       
        EntityState OrderingEntity;

        EntityState TargetEntity;
        //Vector4 _apoapsisPoint;
        //Vector4 _periapsisPoint;
        double _apoapsisKm;
        double _periapsisKM;
        double _targetRadius;
        double _peAlt { get { return _periapsisKM - _targetRadius; } }
        double _apAlt { get { return _apoapsisKm - _targetRadius; } }

        double _apMax;
        double _peMin { get { return _targetRadius; } }

        double _PreciseOrbitalSpeedKm_s = double.NaN;

        //(Vector4, TimeSpan) _intercept;

        string _displayText;
        string _tooltipText = "";
        OrbitOrderWiget _orbitWidget;
        TranslateMoveOrderWidget _moveWidget;
        bool _smMode;

        enum States: byte { NeedsEntity, NeedsTarget, NeedsInsertionPoint, NeedsActioning }
        States CurrentState;
        enum Events: byte { SelectedEntity, SelectedPosition, ClickedAction, AltClicked}
        Action[,] fsm;

        ECSLib.Vector4 _targetInsertionPoint_AU;

        private OrbitOrderWindow(EntityState entity, bool smMode = false)
        {


            OrderingEntity = entity;
            _smMode = smMode;
            IsActive = true;

            _displayText = "Orbit Order: " + OrderingEntity.Name;
            _tooltipText = "Select target to orbit";
            CurrentState = States.NeedsTarget;
            TargetEntity = new EntityState() { Name = "" };
            if (OrderingEntity.Entity.HasDataBlob<OrbitDB>())
            {
                //_orbitWidget = new OrbitOrderWiget(OrderingEntity.Entity.GetDataBlob<OrbitDB>());
                //_state.MapRendering.UIWidgets.Add(_orbitWidget);
                if (_moveWidget == null)
                {
                    _moveWidget = new TranslateMoveOrderWidget(_state, OrderingEntity.Entity);
                    _state.MapRendering.UIWidgets.Add(_moveWidget);
                }
            }

            fsm = new Action[4, 4]
            {
                //selectEntity      selectPos               clickAction     altClick
                {DoNothing,         DoNothing,              DoNothing,      AbortOrder,  },     //needsEntity
                {TargetSelected,    DoNothing,              DoNothing,      GoBackState, }, //needsTarget
                {DoNothing,         InsertionPntSelected,   DoNothing,      GoBackState, }, //needsApopapsis
                //{DoNothing,         PeriapsisPntSelected,   DoNothing,      GoBackState, }, //needsPeriapsis
                {DoNothing,         DoNothing,              ActionCmd,      GoBackState, }  //needsActoning
            };

        }

        internal static OrbitOrderWindow GetInstance(EntityState entity, bool SMMode = false)
        {
            if (!_state.LoadedWindows.ContainsKey(typeof(OrbitOrderWindow)))
            {
                return new OrbitOrderWindow(entity, SMMode);
            }
            var instance = (OrbitOrderWindow)_state.LoadedWindows[typeof(OrbitOrderWindow)];
            instance.OrderingEntity = entity;
            instance.CurrentState = States.NeedsTarget;
            return instance;
        }


        void DoNothing() { return; }
        void EntitySelected() { 
            OrderingEntity = _state.LastClickedEntity;
            CurrentState = States.NeedsTarget;
        }

        void TargetSelected() { 
            TargetEntity = _state.LastClickedEntity;

            _state.Camera.PinToEntity(TargetEntity.Entity);

            var soiWorldRad_AU = GMath.GetSOI(TargetEntity.Entity);
            _apMax = soiWorldRad_AU;

            float soiViewUnits = _state.Camera.ViewDistance(soiWorldRad_AU);

            Vector2 viewPortSize = _state.Camera.ViewPortSize;
            float windowLen = Math.Min(viewPortSize.X, viewPortSize.Y);
            if (soiViewUnits < windowLen * 0.5)
            {
                //zoom so soi fills ~3/4 screen.
                var soilenwanted = windowLen * 0.375;
                _state.Camera.ZoomLevel = (float)(soilenwanted / _apMax) ; 
            }


            if (_orbitWidget != null)
            {
                int index = _state.MapRendering.UIWidgets.IndexOf(_orbitWidget);
                _orbitWidget = new OrbitOrderWiget(TargetEntity.Entity);
                if (index != -1)
                    _state.MapRendering.UIWidgets[index] = _orbitWidget;
                else
                    _state.MapRendering.UIWidgets.Add(_orbitWidget);
            }
            else
            {
                _orbitWidget = new OrbitOrderWiget(TargetEntity.Entity);
                _state.MapRendering.UIWidgets.Add(_orbitWidget);
            }
            if (_moveWidget == null)
            {
                _moveWidget = new TranslateMoveOrderWidget(_state, OrderingEntity.Entity);
                _state.MapRendering.UIWidgets.Add(_moveWidget);
            }

            _moveWidget.SetArrivalTarget(TargetEntity.Entity);

            _targetRadius = TargetEntity.Entity.GetDataBlob<MassVolumeDB>().RadiusInKM;
            //_intercept = InterceptCalcs.FTLIntercept(OrderingEntity.Entity, TargetEntity.Entity.GetDataBlob<OrbitDB>(), TargetEntity.Entity.Manager.ManagerSubpulses.SystemLocalDateTime);
            //_intercept = InterceptCalcs.GetInterceptPosition(OrderingEntity.Entity, TargetEntity.Entity.GetDataBlob<OrbitDB>(), _state.CurrentSystemDateTime); 
            _tooltipText = "Select Insertion Point";
            CurrentState = States.NeedsInsertionPoint;
        }
        void InsertionPntSelected() { 
            var transitLeavePnt = _state.LastWorldPointClicked;
            var ralitiveLeavePnt =  transitLeavePnt - GetTargetPosition();
            var distanceSelectedKM = Distance.AuToKm(ralitiveLeavePnt.Length());
            _moveWidget.SetArrivalPosition(_targetInsertionPoint_AU);
            //_apoapsisKm = Math.Min(_apMax, distanceSelected);
            //_apAlt = _apoapsisKm - _targetRadius;
            _tooltipText = "Action to give order";
            CurrentState = States.NeedsActioning;
        }
        /*
        void PeriapsisPntSelected() { 
            //var periapsisPoint = _state.LastWorldPointClicked;
            //var distanceSelected = Distance.AuToKm((GetTargetPosition() - periapsisPoint).Length());
            //_periapsisKM = Math.Min(Math.Max(_peMin, distanceSelected), _apoapsisKm); 
            //_peAlt = _periapsisKM - _targetRadius;
            _tooltipText = "Action to give order";
            CurrentState = States.NeedsActioning;
        }*/
        void ActionCmd() 
        {
            /*
            OrbitBodyCommand.CreateOrbitBodyCommand(
                _state.Game,
                _state.Faction,
                OrderingEntity.Entity,
                TargetEntity.Entity,
                _apoapsisKm,//PointDFunctions.Length(_orbitWidget.Apoapsis), 
                _periapsisKM);//PointDFunctions.Length(_orbitWidget.Periapsis));
            */
            TransitToOrbitCommand.CreateTransitCmd(
                _state.Game,
                _state.Faction,
                OrderingEntity.Entity,
                TargetEntity.Entity,
                _targetInsertionPoint_AU,
                _state.CurrentSystemDateTime);
            CloseWindow();
        }
        void ActionAddDB()
        {
            _state.SpaceMasterVM.SMSetOrbitToEntity(OrderingEntity.Entity, TargetEntity.Entity, PointDFunctions.Length(_orbitWidget.Periapsis), _state.CurrentSystemDateTime);
            CloseWindow();
        }

        void AbortOrder() { CloseWindow(); }
        void GoBackState() { CurrentState -= 1; }

        ECSLib.Vector4 GetTargetPosition()
        {
            return TargetEntity.Entity.GetDataBlob<PositionDB>().AbsolutePosition_AU;
        }
        ECSLib.Vector4 GetMyPosition()
        {
            return OrderingEntity.Entity.GetDataBlob<PositionDB>().AbsolutePosition_AU;
        }
        internal override void Display()
        {
            if (IsActive)
            {
                Vector2 size = new Vector2(200, 100);
                Vector2 pos = new Vector2(_state.MainWinSize.X / 2 - size.X / 2, _state.MainWinSize.Y / 2 - size.Y / 2);

                ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(pos, ImGuiCond.FirstUseEver);

                if (ImGui.Begin(_displayText, ref IsActive, _flags))
                {

                    ImGui.SetTooltip(_tooltipText);
                    ImGui.Text("Target: ");
                    ImGui.SameLine();
                    ImGui.Text( TargetEntity.Name);

                    ImGui.Text("Apoapsis: ");
                    ImGui.SameLine();
                    ImGui.Text(_apoapsisKm.ToString("g3") + " (Alt: " + _apAlt.ToString("g3") + ")");

                    ImGui.Text("Periapsis: ");
                    ImGui.SameLine();
                    ImGui.Text(_periapsisKM.ToString("g3") + " (Alt: " + _peAlt.ToString("g3") + ")");

                    ImGui.Text("OrbitalVelocity: ");
                    ImGui.SameLine();
                    ImGui.Text(_PreciseOrbitalSpeedKm_s.ToString());

                    if (ImGui.Button("Action Order"))
                        fsm[(byte)CurrentState, (byte)Events.ClickedAction].Invoke();

                    if (_smMode)
                    {
                        ImGui.SameLine();
                        if (ImGui.Button("Add OrbitDB"))
                        {
                            ActionAddDB();
                        }
                    }

                    if (_orbitWidget != null)
                    {

                        switch (CurrentState)
                        {
                            case States.NeedsEntity:

                                break;
                            case States.NeedsTarget:

                                break;
                            case States.NeedsInsertionPoint:
                                {
                                                                   
                                    var mousePos = ImGui.GetMousePos();

                                    var mouseWorldPos = _state.Camera.MouseWorldCoordinate();
                                    _targetInsertionPoint_AU = (mouseWorldPos - GetTargetPosition());

                                    //var intercept = InterceptCalcs.GetInterceptPosition(OrderingEntity.Entity, TargetEntity.Entity.GetDataBlob<OrbitDB>(), TargetEntity.Entity.Manager.ManagerSubpulses.SystemLocalDateTime);

                                    //var distanceAU = (GetTargetPosition() - mouseWorldPos).Length();
                                    //var distanceSelectedKM = Distance.AuToKm(distanceAU);
                                    //var d1 = Math.Min(_apMax, distanceSelectedKM); //can't be higher than SOI
                                    //_apoapsisKm = Math.Max(d1, _peMin); //cant be lower than the body radius

                                    //_moveWidget.SetArrivalRadius(distanceAU);
                                    _moveWidget.SetArrivalPosition(_targetInsertionPoint_AU);

                                    var massCurrBody = OrderingEntity.Entity.GetDataBlob<OrbitDB>().Parent.GetDataBlob<MassVolumeDB>().Mass;
                                    var massTargetBody = TargetEntity.Entity.GetDataBlob<MassVolumeDB>().Mass;
                                    var mymass = OrderingEntity.Entity.GetDataBlob<MassVolumeDB>().Mass;

                                    var ralPosCBAU = OrderingEntity.Entity.GetDataBlob<PositionDB>().RelativePosition_AU;
                                    var smaCurrOrbtAU = OrderingEntity.Entity.GetDataBlob<OrbitDB>().SemiMajorAxis;

                                    var sgpCBAU = GameConstants.Science.GravitationalConstant * (massCurrBody + mymass) / 3.347928976e33;// (149597870700 * 149597870700 * 149597870700);
                                    var velAU = OrbitProcessor.PreciseOrbitalVector(sgpCBAU, ralPosCBAU, smaCurrOrbtAU);
                                    var sgpTBAU = GameConstants.Science.GravitationalConstant * (massTargetBody + mymass) / 3.347928976e33;

                                    var ke = OrbitMath.KeplerFromVelocityAndPosition(sgpTBAU, _targetInsertionPoint_AU, velAU);

                                    _PreciseOrbitalSpeedKm_s = Distance.AuToKm(velAU.Length());

                                    _orbitWidget.SetParametersFromKeplerElements(ke, _targetInsertionPoint_AU);
                                    _apoapsisKm = Distance.AuToKm(ke.Apoapsis);
                                    _periapsisKM = Distance.AuToKm(ke.Periapsis);
                                    break;
                                }
                                /*
                            case States.NeedsSecondApsis:
                                {
                                     TODO: when we've got newtonion engines, allow second apsis choise and expend Dv.
                                    var mousePos = ImGui.GetMousePos();

                                    var mouseWorldPos = _state.Camera.MouseWorldCoordinate();

                                    var ralitivePos = (GetTargetPosition() - mouseWorldPos);
                                    _orbitWidget.SetPeriapsis(ralitivePos.X, ralitivePos.Y);

                                    //_periapsisKM = Distance.AuToKm((GetTargetPosition() - mouseWorldPos).Length());
                                    var distanceSelected = Distance.AuToKm((GetTargetPosition() - mouseWorldPos).Length());
                                    var d1 = Math.Max(_peMin, distanceSelected); //can't be lower than body radius
                                    _periapsisKM = Math.Min(d1, _apoapsisKm);  //can't be higher than apoapsis. 

                                    break;
                                }*/
                            case States.NeedsActioning:
                                break;
                            default:
                                break;
                        }
                    }



                    ImGui.End();
                }
            }
        }

        /// <summary>
        /// Calculates distance/s on an orbit by calculating positions now and second in the future. 
        /// </summary>
        /// <returns>the distance traveled in a second</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDatetime">At datetime.</param>
        double hackspeed(OrbitDB orbit, DateTime atDatetime)
        {
            var pos1 = OrbitProcessor.GetPosition_AU(orbit, atDatetime);
            var pos2 = OrbitProcessor.GetPosition_AU(orbit, atDatetime + TimeSpan.FromSeconds(1));

            return Distance.DistanceBetween(pos1, pos2);
        }

        internal override void EntityClicked(EntityState entity, MouseButtons button)
        {
            if(button == MouseButtons.Primary)
                fsm[(byte)CurrentState, (byte)Events.SelectedEntity].Invoke();
        }
        internal override void MapClicked(ECSLib.Vector4 worldPos, MouseButtons button)
        {
            if (button == MouseButtons.Primary)
            {
                fsm[(byte)CurrentState, (byte)Events.SelectedPosition].Invoke();
            }
            if (button == MouseButtons.Alt)
            {
                fsm[(byte)CurrentState, (byte)Events.AltClicked].Invoke();
            }
        }

        void CloseWindow()
        {
            IsActive = false;
            CurrentState = States.NeedsEntity;


            if (_orbitWidget != null)
            {
                _state.MapRendering.UIWidgets.Remove(_orbitWidget);
                _orbitWidget = null;
            }
            if (_moveWidget != null)
            {
                _state.MapRendering.UIWidgets.Remove(_moveWidget);
                _moveWidget = null;
            }
        }
    }
}
