using System;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Engine;
using Pulsar4X.Orbital;
using Vector3 = Pulsar4X.Orbital.Vector3;
using Vector2 = Pulsar4X.Orbital.Vector2;
using Pulsar4X.Extensions;
using Pulsar4X.Orbits;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;

namespace Pulsar4X.SDL2UI
{
    /// <summary>
    /// Orbit order window - this whole thing is a somewhat horrible state machine
    /// </summary>
    public class WarpOrderWindow : PulsarGuiWindow// IOrderWindow
    {

        EntityState OrderingEntityState;
        EntityState TargetEntity;
        //Vector4 _apoapsisPoint;
        //Vector4 _periapsisPoint;
        float _maxDV;
        float _progradeDV;
        float _radialDV;
        private bool _strictNewtonMode = true;


        double _apoapsis_m { get { return _endpointTargetOrbit.Apoapsis; }
        }
        double _periapsis_m { get { return _endpointTargetOrbit.Periapsis; }
        }
        double _targetRadiusAU;
        double _targetRadius_m;
        double _peAlt { get { return _periapsis_m - _targetRadius_m; } }
        double _apAlt { get { return _apoapsis_m - _targetRadius_m; } }

        double _apMax;
        double _peMin { get { return _targetRadius_m; } }


        DateTime _departureDateTime;

        private (Vector3 pos, Vector3 vel) _departureState;
        double _departureOrbitalSpeed_m { get { return _departureState.vel.Length(); }}
        double _departureProgradeAngle {get{return Math.Atan2(_departureState.vel.Y, _departureState.vel.X);}}


        double _massOrderingEntity = double.NaN;
        double _massTargetBody = double.NaN;
        double _massCurrentBody = double.NaN;
        double _stdGravParamCurrentBody = double.NaN;
        double _stdGravParamTargetBody_m = double.NaN;


        private NewtonionRadialOrderUI _newtonUI;

        string _displayText;
        string _tooltipText = "";

        WarpMoveOrderWidget _moveWidget;
        bool _smMode;

        enum States: byte { NeedsEntity, NeedsTarget, NeedsInsertionPoint, NeedsActioning }
        States CurrentState;
        enum Events: byte { SelectedEntity, SelectedPosition, ClickedAction, AltClicked}
        Action[,] fsm;


        private OrbitDB _targetEntityOrbitDB;
        private (Vector3 position, DateTime eti) _targetIntercept;
        private Vector3 _perpVec;
        private Vector3 _endpointInsertionPoint_m { get; set; } = new Vector3();

        Vector3 _endpointInitalVelocity_m = Vector3.NaN;
        Vector3 _endpointTargetVelocity_m = Vector3.NaN;

        double _endpointInitalSpeed_m {get{return _endpointInitalVelocity_m.Length();}}
        double _endpointTargetSpeed_m {get{return _endpointTargetVelocity_m.Length();}}

        double _endpointInitalAngle {get{return Math.Atan2(_endpointInitalVelocity_m.Y, _endpointInitalVelocity_m.X);}}
        double _endpointTargetAngle {get{return Math.Atan2(_endpointTargetVelocity_m.Y, _endpointTargetVelocity_m.X);}}
        private KeplerElements _endpointInitialOrbit { get; set; }
        private KeplerElements _endpointTargetOrbit { get; set; }


        OrbitOrderIcon _endpointTargetOrbitWidget;
        OrbitOrderIcon _endpointInitalOrbitWidget;

        Vector3 _endpointDeltaVToSpend
        {
            get { return _endpointInitalVelocity_m - _endpointTargetVelocity_m; }
        }


        private WarpOrderWindow(EntityState entityState, bool smMode = false)
        {
            _flags = ImGuiWindowFlags.AlwaysAutoResize;

            OrderingEntityState = entityState;
            _smMode = smMode;
            _strictNewtonMode = entityState.Entity.Manager.Game.Settings.StrictNewtonion;
            _displayText = "Warp Order: " + OrderingEntityState.Name;
            _tooltipText = "Select target to orbit";
            CurrentState = States.NeedsTarget;
            //TargetEntity = new EntityState(Entity.InvalidEntity) { Name = "" };
            if (OrderingEntityState.Entity.HasDataBlob<OrbitDB>())
            {
                //_endpointTargetOrbitWidget = new OrbitOrderWiget(OrderingEntity.Entity.GetDataBlob<OrbitDB>());
                //_uiState.MapRendering.UIWidgets.Add(_endpointTargetOrbitWidget);
                if (_moveWidget == null)
                {
                    _moveWidget = new WarpMoveOrderWidget(_uiState, OrderingEntityState.Entity);
                    _uiState.SelectedSysMapRender.UIWidgets.Add(nameof(_moveWidget), _moveWidget);

                }
            }
            if(OrderingEntityState.Entity.HasDataBlob<NewtonThrustAbilityDB>())
            {
                var newtDB = OrderingEntityState.Entity.GetDataBlob<NewtonThrustAbilityDB>();
                _maxDV = (float)newtDB.DeltaV;
            }

            fsm = new Action[4, 4]
            {
                //selectEntity      selectPos               clickAction     altClick
                {DoNothing,         DoNothing,              DoNothing,      AbortOrder,  },     //needsEntity
                {TargetSelected,    DoNothing,              DoNothing,      GoBackState, },     //needsTarget
                {DoNothing,         InsertionPntSelected,   DoNothing,      GoBackState, },     //needsApopapsis
                //{DoNothing,         PeriapsisPntSelected,   DoNothing,      GoBackState, },   //needsPeriapsis
                {DoNothing,         DoNothing,              ActionCmd,      GoBackState, }      //needsActoning
            };
        }

        internal static WarpOrderWindow GetInstance(EntityState entity, bool SMMode = false)
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(WarpOrderWindow)))
            {
                return new WarpOrderWindow(entity, SMMode);
            }
            var instance = (WarpOrderWindow)_uiState.LoadedWindows[typeof(WarpOrderWindow)];
            if (instance.OrderingEntityState != entity)
            {
                return new WarpOrderWindow(entity);
            }
            
            instance.OrderingEntityState = entity;
            instance.CurrentState = States.NeedsTarget;
            instance._departureDateTime = _uiState.PrimarySystemDateTime;
            instance.EntitySelected();
            return instance;
        }

        #region Stuff that gets calculated when the state changes.
        void DoNothing() { return; }
        void EntitySelected()
        {
            OrderingEntityState = _uiState.LastClickedEntity;
            PositionDB pdb = OrderingEntityState.Entity.GetDataBlob<PositionDB>();

            _massCurrentBody = pdb.Parent.GetDataBlob<MassVolumeDB>().MassTotal;

            CurrentState = States.NeedsTarget;

            _massOrderingEntity = OrderingEntityState.Entity.GetDataBlob<MassVolumeDB>().MassTotal;
            _stdGravParamCurrentBody = UniversalConstants.Science.GravitationalConstant * (_massCurrentBody + _massOrderingEntity) / 3.347928976e33;
            if (_moveWidget == null)
            {
                _moveWidget = new WarpMoveOrderWidget(_uiState, OrderingEntityState.Entity);
                _uiState.SelectedSysMapRender.UIWidgets.Add(nameof(_moveWidget), _moveWidget);
            }
            DepartureCalcs();

        }


        void TargetSelected()
        {
            TargetEntity = _uiState.LastClickedEntity;
            _targetEntityOrbitDB = TargetEntity.Entity.GetDataBlob<OrbitDB>();
            _targetIntercept  = WarpMath.GetInterceptPosition(OrderingEntityState.Entity, TargetEntity.Entity.GetDataBlob<OrbitDB>(), _departureDateTime);
            _uiState.Camera.PinToEntity(TargetEntity.Entity);
            _targetRadiusAU = TargetEntity.Entity.GetDataBlob<MassVolumeDB>().RadiusInAU;
            _targetRadius_m = TargetEntity.Entity.GetDataBlob<MassVolumeDB>().RadiusInM;
            Vector3 insertionVector = OrbitProcessor.GetOrbitalInsertionVector(_departureState.vel, _targetEntityOrbitDB, _targetIntercept.eti);
            _endpointInitalVelocity_m = insertionVector;
            _apMax = TargetEntity.Entity.GetSOI_m();
            var soiAU = TargetEntity.Entity.GetSOI_AU();
            float soiViewUnits = _uiState.Camera.ViewDistance(soiAU);


            _massTargetBody = TargetEntity.Entity.GetDataBlob<MassVolumeDB>().MassDry;
            _stdGravParamTargetBody_m = GeneralMath.StandardGravitationalParameter(_massOrderingEntity + _massTargetBody);


            if (OrderingEntityState.Entity.HasDataBlob<NewtonThrustAbilityDB>())
            {
                var db = OrderingEntityState.Entity.GetDataBlob<NewtonThrustAbilityDB>();
                _newtonUI = new NewtonionRadialOrderUI(db, _massOrderingEntity, (float)_peMin, (float)_apMax);
                _newtonUI.ProgradeAngle = _departureProgradeAngle;
            }


            Vector2 viewPortSize = _uiState.Camera.ViewPortSize;
            float windowLen = (float)Math.Min(viewPortSize.X, viewPortSize.Y);
            if (soiViewUnits < windowLen * 0.5)
            {
                //zoom so soi fills ~3/4 screen.
                var soilenwanted = windowLen * 0.375;
                _uiState.Camera.ZoomLevel = (float)(soilenwanted / soiAU) ;
            }


            _endpointInitalOrbitWidget = new OrbitOrderIcon(TargetEntity.Entity);
            if (_endpointInitalOrbitWidget != null)
            {
                _uiState.SelectedSysMapRender.UIWidgets[nameof(_endpointInitalOrbitWidget)+"initOrbit"] = _endpointInitalOrbitWidget;
            }
            else
            {
                _uiState.SelectedSysMapRender.UIWidgets.Add(nameof(_endpointInitalOrbitWidget)+"initOrbit", _endpointInitalOrbitWidget);
            }
            //_endpointInitalOrbitWidget.SetParametersFromKeplerElements(_endpointInitialOrbit, _endpointInsertionPoint_m);
            _endpointInitalOrbitWidget.Red = 100;

            _endpointTargetOrbitWidget = new OrbitOrderIcon(TargetEntity.Entity);
            if (_endpointTargetOrbitWidget != null)
            {
                _uiState.SelectedSysMapRender.UIWidgets[nameof(_endpointTargetOrbitWidget)+"tgtOrbit"] = _endpointTargetOrbitWidget;
            }
            else
            {
                _uiState.SelectedSysMapRender.UIWidgets.Add(nameof(_endpointTargetOrbitWidget)+"tgtOrbit", _endpointTargetOrbitWidget);
            }
            //_endpointTargetOrbitWidget.SetParametersFromKeplerElements(_endpointTargetOrbit, _endpointInsertionPoint_m);


            OrderingEntityState.DebugOrbitOrder = _endpointTargetOrbitWidget;
            _moveWidget.SetArrivalTarget(TargetEntity.Entity);
            InitialPlacement();
            InsertionCalcs();

            _tooltipText = "Select Insertion Point";
            CurrentState = States.NeedsInsertionPoint;
        }
        void InsertionPntSelected() {
            _moveWidget.SetArrivalPosition(_endpointInsertionPoint_m);
            _tooltipText = "Action to give order";
            CurrentState = States.NeedsActioning;
        }

        void ActionCmd()
        {

            WarpMoveCommand.CreateCommand(
                OrderingEntityState.Entity,
                TargetEntity.Entity,
                _departureDateTime,
                _endpointTargetOrbit,
                _endpointInsertionPoint_m);

            CloseWindow();
        }
        void ActionAddDB()
        {
            // FIXME:
            // _uiState.SpaceMasterVM.SMSetOrbitToEntity(
            //     OrderingEntityState.Entity,
            //     TargetEntity.Entity,
            //     _endpointTargetOrbitWidget.Periapsis.Length(),
            //     _uiState.PrimarySystemDateTime);
            CloseWindow();
        }

        void AbortOrder() { CloseWindow(); }
        void GoBackState() { CurrentState -= 1; }


        #endregion

        #region Stuff that happens when the system date changes goes here

        public override void OnSystemTickChange(DateTime newDate)
        {

            if (_departureDateTime < newDate)
                _departureDateTime = newDate;

            switch (CurrentState)
            {
                case States.NeedsEntity:

                    break;
                case States.NeedsTarget:
                    {

                        DepartureCalcs();

                        //var ralPosCBAU = OrderingEntityState.Entity.GetDataBlob<PositionDB>().RelativePosition_AU;
                        //var smaCurrOrbtAU = _orderEntityOrbit.SemiMajorAxisAU;

                    }

                    break;
                case States.NeedsInsertionPoint:
                    {
                        DepartureCalcs();
                        //rough calc, this calculates direct to the target.
                        InsertionCalcs();
                        break;
                    }

                case States.NeedsActioning:
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Stuff that happens each frame goes here

        internal override void Display()
        {
            if (!IsActive)
                return;

            var size = new System.Numerics.Vector2(200, 100);
            var pos = new System.Numerics.Vector2(_uiState.MainWinSize.X / 2 - size.X / 2, _uiState.MainWinSize.Y / 2 - size.Y / 2);

            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(pos, ImGuiCond.FirstUseEver);

            if (Window.Begin(_displayText, ref IsActive, _flags))
            {
                //put calcs that needs refreshing each frame in here. (ie calculations from mouse cursor position)
                if (_endpointTargetOrbitWidget != null)
                {
                    switch (CurrentState)
                    {
                        case States.NeedsEntity:

                            break;
                        case States.NeedsTarget:
                            {

                            }

                            break;
                        case States.NeedsInsertionPoint:
                            {

                                if (_strictNewtonMode)
                                {
                                    if (_newtonUI != null)
                                    {
                                        if (_newtonUI.Display())
                                            InsertionCalcs();
                                    }
                                    CurrentState = States.NeedsActioning;
                                }
                                else
                                {
                                    var mouseWorldPos = _uiState.Camera.MouseWorldCoordinate_m();
                                    _endpointInsertionPoint_m = mouseWorldPos - MoveMath.GetAbsolutePosition(TargetEntity.Entity); //relative to the target body

                                    _moveWidget.SetArrivalPosition(_endpointInsertionPoint_m);
                                    _endpointTargetOrbit = OrbitMath.KeplerFromPositionAndVelocity(_stdGravParamTargetBody_m, _endpointInsertionPoint_m, _endpointInitalVelocity_m, _departureDateTime);
                                    _endpointTargetOrbitWidget.SetParametersFromKeplerElements(_endpointTargetOrbit, _endpointInsertionPoint_m);
                                }

                                break;
                            }

                        case States.NeedsActioning:
                            {
                                if (_strictNewtonMode && _newtonUI != null)
                                {
                                    if (_newtonUI.Display())
                                        InsertionCalcs();
                                }
                                else
                                {
                                    _endpointTargetOrbit = OrbitMath.KeplerCircularFromPosition(_stdGravParamCurrentBody, _endpointInsertionPoint_m, _departureDateTime);
                                    _endpointTargetOrbitWidget.SetParametersFromKeplerElements(_endpointTargetOrbit, _endpointInsertionPoint_m);
                                }

                                break;
                            }
                        default:
                            break;
                    }
                }


                ImGui.SetTooltip(_tooltipText);
                ImGui.Text("Target: ");
                if (TargetEntity != null)
                {
                    ImGui.SameLine();
                    ImGui.Text(TargetEntity.Name);
                }

                //ImGui.Text("Eccentricity: " + _eccentricity.ToString("g3"));

                if (ImGui.CollapsingHeader("Orbit Data"))
                {

                    ImGui.Text("InsertionSpeed: ");
                    //ImGui.SameLine();
                    ImGui.Text("Initial: "+Stringify.Distance(_endpointInitalSpeed_m) + "/s");
                    ImGui.Text("Target: " + Stringify.Distance(_endpointTargetSpeed_m) + "/s");

                    ImGui.Text("Eccentricity: ");
                    //ImGui.SameLine();
                    ImGui.Text("Initial: "+Stringify.Quantity(_endpointInitialOrbit.Eccentricity));
                    ImGui.Text("Target: "+Stringify.Quantity(_endpointTargetOrbit.Eccentricity));


                    ImGui.Text("Apoapsis: ");
                    ImGui.SameLine();
                    ImGui.Text(Stringify.Distance(_endpointTargetOrbit.Apoapsis) + " (Alt: " + Stringify.Distance(_apAlt) + ")");

                    ImGui.Text("Periapsis: ");
                    ImGui.SameLine();
                    ImGui.Text(Stringify.Distance(_endpointTargetOrbit.Periapsis) + " (Alt: " + Stringify.Distance(_peAlt) + ")");

                    ImGui.Text("DepartureSpeed: ");
                    //ImGui.SameLine();
                    ImGui.Text( Stringify.Distance( _departureOrbitalSpeed_m) + "/s");

                    ImGui.Text("Departure Vector: ");
                    //ImGui.SameLine();
                    ImGui.Text("X: " + Stringify.Distance(_departureState.vel.X)+ "/s");
                    ImGui.Text("Y: " + Stringify.Distance(_departureState.vel.Y)+ "/s");

                    ImGui.Text("Departure Angle: ");
                    ImGui.SameLine();
                    ImGui.Text(_departureProgradeAngle.ToString("g3") + " radians or " + Angle.ToDegrees(_departureProgradeAngle).ToString("F") + " deg ");

                    /*
                    var pc = OrbitProcessor.InstantaneousOrbitalVelocityPolarCoordinate(_orderEntityOrbit, _departureDateTime);

                    ImGui.Text("Departure Polar Coordinates: ");
                    ImGui.Text(pc.Item1.ToString() + " AU or " + Distance.AuToMt(pc.Item1).ToString("F") + " m/s");
                    ImGui.Text(pc.Item2.ToString("g3") + " radians or " + Angle.ToDegrees(pc.Item2).ToString("F") + " deg ");
                    ;
*/

                    ImGui.Text("Insertion Vector: ");
                    ImGui.Text("X: " + Stringify.Distance(_endpointInitalVelocity_m.X)+ "/s");
                    ImGui.Text("Y: " + Stringify.Distance(_endpointInitalVelocity_m.Y)+ "/s");
                    ImGui.Text("Z: " + Stringify.Distance(_endpointInitalVelocity_m.Z)+ "/s");

                    ImGui.Text("Insertion RelativePosition: ");
                    ImGui.Text("X: " + Stringify.Distance(_endpointInsertionPoint_m.X));
                    ImGui.Text("Y: " + Stringify.Distance(_endpointInsertionPoint_m.Y));
                    ImGui.Text("Z: " + Stringify.Distance(_endpointInsertionPoint_m.Z));

                    ImGui.Text("LoAN: ");
                    ImGui.SameLine();
                    ImGui.Text(_endpointTargetOrbit.LoAN.ToString("g3"));

                    ImGui.Text("AoP: ");
                    ImGui.SameLine();
                    ImGui.Text(_endpointTargetOrbit.AoP.ToString("g3"));

                    ImGui.Text("LoP Angle: ");
                    ImGui.SameLine();
                    ImGui.Text((_endpointTargetOrbit.LoAN + _endpointTargetOrbit.AoP).ToString("g3") + " radians or " + Angle.ToDegrees(_endpointTargetOrbit.LoAN + _endpointTargetOrbit.AoP).ToString("F") + " deg ");

                    if (_endpointTargetOrbitWidget != null)
                        ImGui.Text("Is Retrograde " + _endpointTargetOrbitWidget.IsRetrogradeOrbit.ToString());

                }

                //if (CurrentState != States.NeedsActioning) //use alpha on the button if it's not useable.
                //ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f);
                if (ImGui.Button("Action Order") && CurrentState == States.NeedsActioning) //only do suff if clicked if it's usable.
                {
                    fsm[(byte)CurrentState, (byte)Events.ClickedAction].Invoke();
                    //ImGui.PopStyleVar();
                }

                if (_smMode)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Add OrbitDB"))
                    {
                        ActionAddDB();
                    }
                }

                Window.End();
            }

        }

        #endregion

        #region helper calcs


        void DepartureCalcs()
        {

            //OrbitProcessor.InstantaneousOrbitalVelocityPolarCoordinate()


            if(_uiState.Game.Settings.UseRelativeVelocity)
            {
                _departureState = MoveMath.GetRelativeFutureState(OrderingEntityState.Entity, _departureDateTime);
            }
            else
                _departureState = MoveMath.GetAbsoluteState(OrderingEntityState.Entity, _departureDateTime);

            _moveWidget.SetDepartureProgradeAngle(_departureProgradeAngle);


            _perpVec = Vector3.Normalise(new Vector3(_departureState.vel.Y * -1, _departureState.vel.X, 0));
            var rangeToTarget = (_targetIntercept.position - _departureState.pos).Length();
            var rangeToVec = (_targetIntercept.position - (_departureState.pos + _perpVec)).Length();
            if(rangeToTarget > rangeToVec)
                _perpVec = new Vector3(_perpVec.X * -1, _perpVec.Y * -1, 0);
        }

        void InsertionCalcs()
        {

            _moveWidget.SetArivalProgradeAngle(_endpointInitalAngle);


            _endpointInsertionPoint_m = (_perpVec * _newtonUI.Radius);

            _moveWidget.SetArrivalPosition(_endpointInsertionPoint_m);
            _endpointTargetVelocity_m = _endpointInitalVelocity_m + _newtonUI.DeltaV;
            _endpointTargetOrbit = OrbitMath.KeplerFromPositionAndVelocity(_stdGravParamTargetBody_m, _endpointInsertionPoint_m, _endpointTargetVelocity_m, _departureDateTime);
            _endpointTargetOrbitWidget.SetParametersFromKeplerElements(_endpointTargetOrbit, _endpointInsertionPoint_m);
            _newtonUI.Eccentricity = (float)_endpointTargetOrbit.Eccentricity;
        }

        void InitialPlacement()
        {
            var lowOrbitRadius = OrbitMath.LowOrbitRadius(TargetEntity.Entity);
            var lowOrbitPos = _perpVec * lowOrbitRadius;
            var lowOrbit = OrbitMath.KeplerCircularFromPosition(_stdGravParamTargetBody_m, lowOrbitPos, _targetIntercept.eti);
            var lowOrbitState = OrbitMath.GetStateVectors(lowOrbit, _targetIntercept.eti);

            _endpointTargetOrbit = lowOrbit;
            _endpointTargetVelocity_m = (Vector3)lowOrbitState.velocity;
            _newtonUI.Radius = (float)lowOrbitState.position.Length();
            _newtonUI.SetDeltaV((Vector3)lowOrbitState.velocity - _endpointInitalVelocity_m);
            _newtonUI.Eccentricity = (float)_endpointTargetOrbit.Eccentricity;

            _endpointInsertionPoint_m = (_perpVec * _newtonUI.Radius); //relative to the target body
            _endpointTargetOrbitWidget.SetParametersFromKeplerElements(_endpointTargetOrbit, _endpointInsertionPoint_m);

            _endpointInitialOrbit = OrbitMath.KeplerFromPositionAndVelocity(_stdGravParamTargetBody_m, _endpointInsertionPoint_m, _endpointInitalVelocity_m, _targetIntercept.eti);
            _endpointInitalOrbitWidget.SetParametersFromKeplerElements(_endpointInitialOrbit, _endpointInsertionPoint_m);
        }


        #endregion


        internal override void EntityClicked(EntityState entity, MouseButtons button)
        {
            if (entity == OrderingEntityState)
                return;
            ImGuiIOPtr io = ImGui.GetIO();

            if (button == MouseButtons.Primary && !io.KeyShift )
            {
                var cmd = WarpMoveCommand.CreateCommandEZ(
                    OrderingEntityState.Entity,
                    _uiState.LastClickedEntity.Entity,
                    _departureDateTime);
                if (cmd.EndpointTargetExpendDeltaV.Length() < _maxDV)
                {
                    _uiState.Game.OrderHandler.HandleOrder(cmd);
                    CloseWindow();
                }
                else
                {
                    fsm[(byte)CurrentState, (byte)Events.SelectedEntity].Invoke();
                }

            }
            else if(button == MouseButtons.Primary && io.KeyShift)
            {
                fsm[(byte)CurrentState, (byte)Events.SelectedEntity].Invoke();
            }
        }
        internal override void MapClicked(Vector3 worldPos_m, MouseButtons button)
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
            this.SetActive(false);
            CurrentState = States.NeedsEntity;
            _progradeDV = 0;
            _radialDV = 0;
            if (_endpointInitalOrbitWidget != null)
            {
                _uiState.SelectedSysMapRender.UIWidgets.Remove(nameof(_endpointInitalOrbitWidget)+"initOrbit");
                _endpointInitalOrbitWidget = null;
            }
            if (_endpointTargetOrbitWidget != null)
            {
                _uiState.SelectedSysMapRender.UIWidgets.Remove(nameof(_endpointTargetOrbitWidget)+"tgtOrbit");
                _endpointTargetOrbitWidget = null;
            }
            if (_moveWidget != null)
            {
                _uiState.SelectedSysMapRender.UIWidgets.Remove(nameof(_moveWidget));
                _moveWidget = null;
            }
        }
    }
}
