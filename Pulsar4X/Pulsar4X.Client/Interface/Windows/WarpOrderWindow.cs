using System;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Orbital;
using Vector3 = Pulsar4X.Orbital.Vector3;
using Vector2 = Pulsar4X.Orbital.Vector2;
using SDL3;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Orbit order window - this whole thing is a somewhat horrible state machine
    /// </summary>
    public class WarpOrderWindow : UniquePulsarGuiWindow<WarpOrderWindow> // IOrderWindow
    {
        int _entityId;
        string _systemId = "";
        int? _targetId;
        int? _orbitTargetId;

        private bool _strictNewtonMode = true;

        double _apoapsis_m { get { return _endpointTargetOrbit.Apoapsis; } }
        double _periapsis_m { get { return _endpointTargetOrbit.Periapsis; } }
        double _targetRadius_m;
        double _peAlt { get { return _periapsis_m - _targetRadius_m; } }
        double _apAlt { get { return _apoapsis_m - _targetRadius_m; } }

        double _apMax;

        DateTime _departureDateTime;

        private (Vector3 pos, Vector3 vel) _departureState;
        double _departureOrbitalSpeed_m { get { return _departureState.vel.Length(); }}
        double _departureProgradeAngle {get{return Math.Atan2(_departureState.vel.Y, _departureState.vel.X);}}

        double _massOrderingEntity = double.NaN;
        double _massTargetBody = double.NaN;
        double _massCurrentBody = double.NaN;
        double _stdGravParamCurrentBody = double.NaN;
        double _stdGravParamTargetBody_m = double.NaN;

        private NewtonionRadialOrderUI? _newtonUI;

        string _displayText;
        string _tooltipText = "";

        WarpMoveOrderWidget? _moveWidget;

        enum States: byte { NeedsEntity, NeedsTarget, NeedsInsertionPoint, NeedsActioning }
        States CurrentState;
        enum Events: byte { SelectedEntity, SelectedPosition, ClickedAction, AltClicked}
        Action[,] fsm;

        private (Vector3 position, DateTime eti) _targetIntercept;
        private Vector3 _perpVec;
        private Vector3 _endpointInsertionPoint_m { get; set; } = new Vector3();

        Vector3 _endpointInitalVelocity_m = Vector3.NaN;
        Vector3 _endpointTargetVelocity_m = Vector3.NaN;

        double _endpointInitalSpeed_m {get{return _endpointInitalVelocity_m.Length();}}
        double _endpointTargetSpeed_m {get{return _endpointTargetVelocity_m.Length();}}

        double _endpointInitalAngle {get{return Math.Atan2(_endpointInitalVelocity_m.Y, _endpointInitalVelocity_m.X);}}
        private KeplerElements _endpointInitialOrbit { get; set; }
        private KeplerElements _endpointTargetOrbit { get; set; }

        OrbitOrderIcon? _endpointTargetOrbitWidget;
        OrbitOrderIcon? _endpointInitalOrbitWidget;

        private IClientSystem? System => _uiState.GameClient?.Galaxy.GetSystem(_systemId);
        private EntitySnapshot? OrderingEntity => System?.GetEntity(_entityId);
        private EntitySnapshot? TargetEntity => _targetId is int id ? System?.GetEntity(id) : null;
        private EntitySnapshot? OrbitTarget => _orbitTargetId is int id ? System?.GetEntity(id) : null;
        private bool UseRelativeVelocity => _uiState.GameInfo?.UseRelativeVelocity ?? true;

        private WarpOrderWindow(int entityId, string systemId)
        {
            _flags = ImGuiWindowFlags.AlwaysAutoResize;

            _entityId = entityId;
            _systemId = systemId;
            _strictNewtonMode = _uiState.GameInfo?.StrictNewtonian ?? true;
            _departureDateTime = _uiState.PrimarySystemDateTime;
            _displayText = "Warp Order: " + (OrderingEntity?.GetView<NameView>()?.Name ?? "Unknown");
            _tooltipText = "Select target to orbit";
            CurrentState = States.NeedsTarget;

            CreateMoveWidget();

            fsm = new Action[4, 4]
            {
                //selectEntity      selectPos               clickAction     altClick
                {DoNothing,         DoNothing,              DoNothing,      AbortOrder,  },     //needsEntity
                {TargetSelected,    DoNothing,              DoNothing,      GoBackState, },     //needsTarget
                {DoNothing,         InsertionPntSelected,   DoNothing,      GoBackState, },     //needsApopapsis
                {DoNothing,         DoNothing,              ActionCmd,      GoBackState, }      //needsActoning
            };

            var mainWin = (PulsarMainWindow)_uiState.ViewPort;
            mainWin.MouseButtonUpOccured += (object sender, SDL.Event e) => {
                if (e.Button.Button == 1)
                    fsm[(byte)CurrentState, (byte)Events.SelectedPosition].Invoke();
                else if (e.Button.Button == 3)
                    fsm[(byte)CurrentState, (byte)Events.AltClicked].Invoke();
            };
        }

        internal static WarpOrderWindow GetInstance(EntityState entity, bool SMMode = false)
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(WarpOrderWindow)))
            {
                return new WarpOrderWindow(entity.Id, entity.StarSystemId!);
            }
            var instance = (WarpOrderWindow)_uiState.LoadedWindows[typeof(WarpOrderWindow)];
            if (instance._entityId != entity.Id)
            {
                return new WarpOrderWindow(entity.Id, entity.StarSystemId!);
            }

            instance.CurrentState = States.NeedsTarget;
            instance._departureDateTime = _uiState.PrimarySystemDateTime;
            instance.EntitySelected();
            return instance;
        }

        #region Stuff that gets calculated when the state changes.
        void DoNothing() { return; }
        void EntitySelected()
        {
            var clicked = _uiState.LastClickedEntity;
            if (clicked == null || clicked.StarSystemId == null)
                return;
            _entityId = clicked.Id;
            _systemId = clicked.StarSystemId;
            _displayText = "Warp Order: " + (OrderingEntity?.GetView<NameView>()?.Name ?? "Unknown");

            var ordering = OrderingEntity;
            var system = System;
            if (ordering == null || system == null)
                return;

            _massOrderingEntity = ordering.GetView<MassVolumeView>()?.MassKg ?? double.NaN;
            _massCurrentBody = ordering.GetSoiParent(system)?.GetView<MassVolumeView>()?.MassKg ?? double.NaN;

            CurrentState = States.NeedsTarget;

            _stdGravParamCurrentBody = UniversalConstants.Science.GravitationalConstant * (_massCurrentBody + _massOrderingEntity) / 3.347928976e33;
            CreateMoveWidget();
            DepartureCalcs();
        }

        void CreateMoveWidget()
        {
            var ordering = OrderingEntity;
            var system = System;
            if (_moveWidget != null || ordering == null || system == null)
                return;
            if (!ordering.HasView<OrbitView>())
                return;
            if (ordering.GetSoiParent(system) is not { } soiParent)
                return;

            _moveWidget = new WarpMoveOrderWidget(_uiState, _systemId, _entityId, soiParent.Id);
            _uiState.SelectedSysMapRender?.UIWidgets.Add(nameof(_moveWidget), _moveWidget);
        }

        void TargetSelected()
        {
            var clicked = _uiState.LastClickedEntity;
            var system = System;
            var ordering = OrderingEntity;
            if (clicked == null || system == null || ordering == null)
                return;

            // Determine the orbit target - if the selected entity doesn't have an orbit,
            // try to use its position parent (e.g., for colonies on a planet)
            var target = system.GetEntity(clicked.Id);
            if (target == null)
                return;

            var orbitTarget = target;
            if (!orbitTarget.HasView<OrbitView>())
            {
                if (target.GetView<PositionView>()?.ParentId is int posParentId
                    && system.GetEntity(posParentId) is { } posParent
                    && posParent.HasView<OrbitView>())
                {
                    orbitTarget = posParent;
                }
                else
                {
                    return;
                }
            }

            var massVolume = orbitTarget.GetView<MassVolumeView>();
            var thrust = ordering.GetView<ThrustView>();
            if (massVolume == null || thrust == null)
                return;

            _targetId = target.Id;
            _orbitTargetId = orbitTarget.Id;

            var moverAbsPos = ordering.GetAbsoluteState(system, _departureDateTime).pos;
            double warpSpeed = ordering.GetView<WarpAbilityView>()?.MaxSpeedMps ?? 0;
            _targetIntercept = SnapshotMoves.GetInterceptPosition(moverAbsPos, warpSpeed, orbitTarget, system, _departureDateTime);
            _uiState.Camera.PinToEntity(orbitTarget.Id, _systemId, _uiState);

            _targetRadius_m = massVolume.RadiusMetres;
            _endpointInitalVelocity_m = SnapshotMoves.GetOrbitalInsertionVector(
                _departureState.vel, orbitTarget, system, _targetIntercept.eti, UseRelativeVelocity);

            double soi_m = orbitTarget.SoiRadiusM();
            _apMax = double.IsInfinity(soi_m) ? float.MaxValue : soi_m;

            _massTargetBody = massVolume.DryMassKg;
            _stdGravParamTargetBody_m = GeneralMath.StandardGravitationalParameter(_massOrderingEntity + _massTargetBody);

            _newtonUI = new NewtonionRadialOrderUI((float)(_targetRadius_m), (float)_apMax);
            _newtonUI.ProgradeAngle = _departureProgradeAngle;

            if (!double.IsInfinity(soi_m))
            {
                var soiAU = Distance.MToAU(soi_m);
                float soiViewUnits = _uiState.Camera.ViewDistance(soiAU);
                Vector2 viewPortSize = _uiState.Camera.ViewPortSize;
                float windowLen = (float)Math.Min(viewPortSize.X, viewPortSize.Y);
                if (soiViewUnits < windowLen * 0.5)
                {
                    //zoom so soi fills ~3/4 screen.
                    var soilenwanted = windowLen * 0.375;
                    _uiState.Camera.ZoomLevel = (float)(soilenwanted / soiAU);
                }
            }

            var orbitTargetPosition = new SnapshotPosition(_uiState, _systemId, orbitTarget.Id);
            double widgetSoi = double.IsInfinity(soi_m) ? 0 : soi_m;

            _endpointInitalOrbitWidget = new OrbitOrderIcon(orbitTargetPosition, widgetSoi, _targetRadius_m);
            _endpointInitalOrbitWidget.Red = 100;
            if (_uiState.SelectedSysMapRender != null)
                _uiState.SelectedSysMapRender.UIWidgets[nameof(_endpointInitalOrbitWidget)+"initOrbit"] = _endpointInitalOrbitWidget;

            _endpointTargetOrbitWidget = new OrbitOrderIcon(orbitTargetPosition, widgetSoi, _targetRadius_m);
            if (_uiState.SelectedSysMapRender != null)
                _uiState.SelectedSysMapRender.UIWidgets[nameof(_endpointTargetOrbitWidget)+"tgtOrbit"] = _endpointTargetOrbitWidget;

            _moveWidget?.SetArrivalTarget(orbitTarget.Id);
            InitialPlacement();
            InsertionCalcs();

            _tooltipText = "Select Insertion Point";
            CurrentState = States.NeedsInsertionPoint;
        }
        void InsertionPntSelected() {
            _moveWidget?.SetArrivalPosition(_endpointInsertionPoint_m);
            _tooltipText = "Action to give order";
            CurrentState = States.NeedsActioning;
        }

        void ActionCmd()
        {
            if (_orbitTargetId is int targetId)
            {
                _uiState.GameClient?.SubmitCommandAsync(new Pulsar4X.Api.WarpMoveCommand(
                    _entityId,
                    targetId,
                    new Vec3(_endpointInsertionPoint_m.X, _endpointInsertionPoint_m.Y, _endpointInsertionPoint_m.Z)));
            }

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

            if (OrderingEntity == null)
            {
                CloseWindow();
                return;
            }

            var size = new System.Numerics.Vector2(200, 100);
            var pos = new System.Numerics.Vector2(
                    _uiState.ViewPort.Size.Width / 2 - size.X / 2,
                    _uiState.ViewPort.Size.Height / 2 - size.Y / 2);

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
                                        if (NewtonUIDisplay())
                                            InsertionCalcs();
                                    }
                                    CurrentState = States.NeedsActioning;
                                }
                                else
                                {
                                    var system = System;
                                    var orbitTarget = OrbitTarget;
                                    if (system != null && orbitTarget != null)
                                    {
                                        var mouseWorldPos = _uiState.Camera.MouseWorldCoordinate_m();
                                        var targetAbsPos = orbitTarget.AbsolutePositionM(system, _uiState.PrimarySystemDateTime);
                                        _endpointInsertionPoint_m = mouseWorldPos - targetAbsPos; //relative to the target body

                                        _moveWidget?.SetArrivalPosition(_endpointInsertionPoint_m);
                                        _endpointTargetOrbit = OrbitalMath.KeplerFromPositionAndVelocity(_stdGravParamTargetBody_m, _endpointInsertionPoint_m, _endpointInitalVelocity_m, _departureDateTime);
                                        _endpointTargetOrbitWidget.SetParametersFromKeplerElements(_endpointTargetOrbit, _endpointInsertionPoint_m);
                                    }
                                }

                                break;
                            }

                        case States.NeedsActioning:
                            {
                                if (_strictNewtonMode && _newtonUI != null)
                                {
                                    if (NewtonUIDisplay())
                                        InsertionCalcs();
                                }
                                else
                                {
                                    _endpointTargetOrbit = OrbitalMath.KeplerCircularFromPosition(_stdGravParamCurrentBody, _endpointInsertionPoint_m, _departureDateTime);
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
                    ImGui.Text(TargetEntity.GetView<NameView>()?.Name ?? "Unknown");
                }

                if (ImGui.CollapsingHeader("Orbit Data"))
                {

                    ImGui.Text("InsertionSpeed: ");
                    ImGui.Text("Initial: "+Stringify.Distance(_endpointInitalSpeed_m) + "/s");
                    ImGui.Text("Target: " + Stringify.Distance(_endpointTargetSpeed_m) + "/s");

                    ImGui.Text("Eccentricity: ");
                    ImGui.Text("Initial: "+Stringify.Quantity(_endpointInitialOrbit.Eccentricity));
                    ImGui.Text("Target: "+Stringify.Quantity(_endpointTargetOrbit.Eccentricity));


                    ImGui.Text("Apoapsis: ");
                    ImGui.SameLine();
                    ImGui.Text(Stringify.Distance(_endpointTargetOrbit.Apoapsis) + " (Alt: " + Stringify.Distance(_apAlt) + ")");

                    ImGui.Text("Periapsis: ");
                    ImGui.SameLine();
                    ImGui.Text(Stringify.Distance(_endpointTargetOrbit.Periapsis) + " (Alt: " + Stringify.Distance(_peAlt) + ")");

                    ImGui.Text("DepartureSpeed: ");
                    ImGui.Text( Stringify.Distance( _departureOrbitalSpeed_m) + "/s");

                    ImGui.Text("Departure Vector: ");
                    ImGui.Text("X: " + Stringify.Distance(_departureState.vel.X)+ "/s");
                    ImGui.Text("Y: " + Stringify.Distance(_departureState.vel.Y)+ "/s");

                    ImGui.Text("Departure Angle: ");
                    ImGui.SameLine();
                    ImGui.Text(_departureProgradeAngle.ToString("g3") + " radians or " + Angle.ToDegrees(_departureProgradeAngle).ToString("F") + " deg ");

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

                if (ImGui.Button("Action Order") && CurrentState == States.NeedsActioning) //only do suff if clicked if it's usable.
                {
                    fsm[(byte)CurrentState, (byte)Events.ClickedAction].Invoke();
                }

                Window.End();
            }

        }

        /// <summary>The radial-order sliders, fed fresh propulsion/mass data from the snapshot.</summary>
        bool NewtonUIDisplay()
        {
            var ordering = OrderingEntity;
            var thrust = ordering?.GetView<ThrustView>();
            var massVolume = ordering?.GetView<MassVolumeView>();
            if (_newtonUI == null || thrust == null || massVolume == null)
                return false;

            return _newtonUI.Display(thrust.DeltaVMps, thrust.ExhaustVelocityMps, thrust.FuelBurnRateKgPerSec, massVolume.MassKg);
        }

        #endregion

        #region helper calcs


        void DepartureCalcs()
        {
            var ordering = OrderingEntity;
            var system = System;
            if (ordering == null || system == null)
                return;

            if (UseRelativeVelocity)
                _departureState = ordering.GetRelativeState(_departureDateTime);
            else
                _departureState = ordering.GetAbsoluteState(system, _departureDateTime);

            _moveWidget?.SetDepartureProgradeAngle(_departureProgradeAngle);

            _perpVec = Vector3.Normalise(new Vector3(_departureState.vel.Y * -1, _departureState.vel.X, 0));
            var rangeToTarget = (_targetIntercept.position - _departureState.pos).Length();
            var rangeToVec = (_targetIntercept.position - (_departureState.pos + _perpVec)).Length();
            if(rangeToTarget > rangeToVec)
                _perpVec = new Vector3(_perpVec.X * -1, _perpVec.Y * -1, 0);
        }

        void InsertionCalcs()
        {
            if (_newtonUI == null)
                return;

            _moveWidget?.SetArivalProgradeAngle(_endpointInitalAngle);

            _endpointInsertionPoint_m = (_perpVec * _newtonUI.Radius);

            _moveWidget?.SetArrivalPosition(_endpointInsertionPoint_m);
            _endpointTargetVelocity_m = _endpointInitalVelocity_m + _newtonUI.DeltaV;
            _endpointTargetOrbit = OrbitalMath.KeplerFromPositionAndVelocity(_stdGravParamTargetBody_m, _endpointInsertionPoint_m, _endpointTargetVelocity_m, _departureDateTime);
            _endpointTargetOrbitWidget?.SetParametersFromKeplerElements(_endpointTargetOrbit, _endpointInsertionPoint_m);
            _newtonUI.Eccentricity = (float)_endpointTargetOrbit.Eccentricity;
        }

        void InitialPlacement()
        {
            var ordering = OrderingEntity;
            var orbitTarget = OrbitTarget;
            var thrust = ordering?.GetView<ThrustView>();
            var massVolume = ordering?.GetView<MassVolumeView>();
            if (_newtonUI == null || orbitTarget == null || thrust == null || massVolume == null)
                return;

            var lowOrbitRadius = orbitTarget.LowOrbitRadiusM();
            var lowOrbitPos = _perpVec * lowOrbitRadius;
            var lowOrbit = OrbitalMath.KeplerCircularFromPosition(_stdGravParamTargetBody_m, lowOrbitPos, _targetIntercept.eti);
            var lowOrbitState = OrbitalMath.GetStateVectors(lowOrbit, _targetIntercept.eti);

            _endpointTargetOrbit = lowOrbit;
            _endpointTargetVelocity_m = (Vector3)lowOrbitState.velocity;
            _newtonUI.Radius = (float)lowOrbitState.position.Length();
            _newtonUI.SetDeltaV((Vector3)lowOrbitState.velocity - _endpointInitalVelocity_m,
                thrust.ExhaustVelocityMps, massVolume.MassKg);
            _newtonUI.Eccentricity = (float)_endpointTargetOrbit.Eccentricity;

            _endpointInsertionPoint_m = (_perpVec * _newtonUI.Radius); //relative to the target body
            _endpointTargetOrbitWidget?.SetParametersFromKeplerElements(_endpointTargetOrbit, _endpointInsertionPoint_m);

            _endpointInitialOrbit = OrbitalMath.KeplerFromPositionAndVelocity(_stdGravParamTargetBody_m, _endpointInsertionPoint_m, _endpointInitalVelocity_m, _targetIntercept.eti);
            _endpointInitalOrbitWidget?.SetParametersFromKeplerElements(_endpointInitialOrbit, _endpointInsertionPoint_m);
        }


        #endregion


        internal override void EntityClicked(EntityState entity, MouseButtons button)
        {
            if (entity.Id == _entityId)
                return;
            ImGuiIOPtr io = ImGui.GetIO();

            if (button == MouseButtons.Primary && !io.KeyShift )
            {
                // Quick path: estimate the ΔV the default low-orbit insertion would cost; if the
                // ship can afford it, send the order (the server computes the actual orbit).
                if (TryEstimateEzDeltaV(entity.Id, out double dvEstimate, out int targetId)
                    && dvEstimate < (OrderingEntity?.GetView<ThrustView>()?.DeltaVMps ?? 0))
                {
                    _uiState.GameClient?.SubmitCommandAsync(new Pulsar4X.Api.WarpMoveCommand(_entityId, targetId));
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

        /// <summary>The ΔV needed to circularise into low orbit after a default warp (the engine's
        /// CreateCommandEZ math over snapshots). False when the maths can't be done client-side;
        /// targets without an orbit (jump points) cost nothing.</summary>
        bool TryEstimateEzDeltaV(int clickedId, out double deltaV, out int targetId)
        {
            deltaV = 0;
            targetId = clickedId;

            var system = System;
            var ordering = OrderingEntity;
            if (system == null || ordering == null)
                return false;

            var target = system.GetEntity(clickedId);
            if (target == null)
                return false;

            //if target is a colony, just make the target the parent planet.
            if (target.Kind == BodyKind.Colony
                && target.GetView<ColonyView>()?.PlanetEntityId is int planetId
                && system.GetEntity(planetId) is { } planet)
            {
                target = planet;
                targetId = planet.Id;
            }

            var targetOrbit = target.ResolveOrbit();
            if (targetOrbit == null || targetOrbit.StandardGravParameter <= 0)
                return true; // static target (jump point, anomaly): no insertion burn needed

            var targetMass = target.GetView<MassVolumeView>();
            var orderingMass = ordering.GetView<MassVolumeView>();
            double warpSpeed = ordering.GetView<WarpAbilityView>()?.MaxSpeedMps ?? 0;
            if (targetMass == null || orderingMass == null || warpSpeed <= 0)
                return false;

            var departureState = UseRelativeVelocity
                ? ordering.GetRelativeState(_departureDateTime)
                : ordering.GetAbsoluteState(system, _departureDateTime);

            var sgp = GeneralMath.StandardGravitationalParameter(targetMass.MassKg + orderingMass.MassKg);
            var lowOrbitRadius = target.LowOrbitRadiusM();
            var perpVec = Vector3.Normalise(new Vector3(departureState.vel.Y * -1, departureState.vel.X, 0));
            var lowOrbitPos = perpVec * lowOrbitRadius;

            var moverAbsPos = ordering.GetAbsoluteState(system, _departureDateTime).pos;
            var targetIntercept = SnapshotMoves.GetInterceptPosition(moverAbsPos, warpSpeed, target, system, _departureDateTime, lowOrbitPos);
            var lowOrbit = OrbitalMath.KeplerCircularFromPosition(sgp, lowOrbitPos, targetIntercept.eti);
            var lowOrbitState = OrbitalMath.GetStateVectors(lowOrbit, targetIntercept.eti);
            var insertionVector = SnapshotMoves.GetOrbitalInsertionVector(
                departureState.vel, target, system, targetIntercept.eti, UseRelativeVelocity);

            deltaV = (insertionVector - (Vector3)lowOrbitState.velocity).Length();
            return true;
        }

        void CloseWindow()
        {
            this.SetActive(false);
            CurrentState = States.NeedsEntity;
            _targetId = null;
            _orbitTargetId = null;
            if (_endpointInitalOrbitWidget != null)
            {
                _uiState.SelectedSysMapRender?.UIWidgets.Remove(nameof(_endpointInitalOrbitWidget)+"initOrbit");
                _endpointInitalOrbitWidget = null;
            }
            if (_endpointTargetOrbitWidget != null)
            {
                _uiState.SelectedSysMapRender?.UIWidgets.Remove(nameof(_endpointTargetOrbitWidget)+"tgtOrbit");
                _endpointTargetOrbitWidget = null;
            }
            if (_moveWidget != null)
            {
                _uiState.SelectedSysMapRender?.UIWidgets.Remove(nameof(_moveWidget));
                _moveWidget = null;
            }
        }
    }
}
