using System;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Orbital;
using Vector3 = Pulsar4X.Orbital.Vector3;
using Vector2 = Pulsar4X.Orbital.Vector2;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Engine.Orders;

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
        Orbital.Vector3 _deltaV_MS; 

        KeplerElements _ke_m;
        double _apoapsis_m;
        double _periapsis_m;
        double _targetRadiusAU;
        double _targetRadius_m;
        double _peAlt { get { return _periapsis_m - _targetRadius_m; } }
        double _apAlt { get { return _apoapsis_m - _targetRadius_m; } }

        double _apMax;
        double _peMin { get { return _targetRadius_m; } }

        private double _eccentricity;
        private double Eccentricity
        {
            get { return _eccentricity; } 
            set
            {
                if (_newtonUI != null)
                    _newtonUI.Eccentricity = value;
                _eccentricity = value;
            } 
        }


        DateTime _departureDateTime;
        double _departureOrbitalSpeed_m = double.NaN;
        Orbital.Vector3 _departureOrbitalVelocity_m = Orbital.Vector3.NaN;
        double _departureAngle = double.NaN;

        double _insertionOrbitalSpeed_m = double.NaN;
        Orbital.Vector3 _insertionOrbitalVelocity_m = Orbital.Vector3.NaN;
        double _insertionAngle = double.NaN;
        //(Vector4, TimeSpan) _intercept;

        double _massOrderingEntity = double.NaN;
        double _massTargetBody = double.NaN;
        double _massCurrentBody = double.NaN;
        double _stdGravParamCurrentBody = double.NaN;
        double _stdGravParamTargetBody_m = double.NaN;


        private NewtonionOrderUI _newtonUI;

        
        
        
        string _displayText;
        string _tooltipText = "";
        OrbitOrderWidget _orbitWidget;
        WarpMoveOrderWidget _moveWidget;
        bool _smMode;

        enum States: byte { NeedsEntity, NeedsTarget, NeedsInsertionPoint, NeedsActioning }
        States CurrentState;
        enum Events: byte { SelectedEntity, SelectedPosition, ClickedAction, AltClicked}
        Action[,] fsm;

        private Vector3 _targetInsertionPoint_m { get; set; }

        private Vector3 _targetInsertionPoint_AU
        {
            get { return Distance.MToAU(_targetInsertionPoint_m); }
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
                //_orbitWidget = new OrbitOrderWiget(OrderingEntity.Entity.GetDataBlob<OrbitDB>());
                //_uiState.MapRendering.UIWidgets.Add(_orbitWidget);
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
                {TargetSelected,    DoNothing,              DoNothing,      GoBackState, }, //needsTarget
                {DoNothing,         InsertionPntSelected,   DoNothing,      GoBackState, }, //needsApopapsis
                //{DoNothing,         PeriapsisPntSelected,   DoNothing,      GoBackState, }, //needsPeriapsis
                {DoNothing,         DoNothing,              ActionCmd,      GoBackState, }  //needsActoning
            };
        }

        internal static WarpOrderWindow GetInstance(EntityState entity, bool SMMode = false)
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(WarpOrderWindow)))
            {
                return new WarpOrderWindow(entity, SMMode);
            }
            var instance = (WarpOrderWindow)_uiState.LoadedWindows[typeof(WarpOrderWindow)];
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

            if (OrderingEntityState.Entity.HasDataBlob<OrbitDB>())
            {
                _massCurrentBody = OrderingEntityState.Entity.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassDry;
            }
            else
            {
                var foo =  OrderingEntityState.Entity.GetDataBlob<NewtonMoveDB>();
                if(foo == null)
                {

                }
                else
                {
                    _massCurrentBody = foo.ParentMass;
                }
                
            }

            //else if(OrderingEntity.Entity.HasDataBlob<newton>())
            CurrentState = States.NeedsTarget;
            
            _massOrderingEntity = OrderingEntityState.Entity.GetDataBlob<MassVolumeDB>().MassTotal;
            _stdGravParamCurrentBody = UniversalConstants.Science.GravitationalConstant * (_massCurrentBody + _massOrderingEntity) / 3.347928976e33;
            if (_moveWidget == null)
            {
                _moveWidget = new WarpMoveOrderWidget(_uiState, OrderingEntityState.Entity);
                _uiState.SelectedSysMapRender.UIWidgets.Add(nameof(_moveWidget), _moveWidget);
            }
            DepartureCalcs();

            if (OrderingEntityState.Entity.HasDataBlob<NewtonThrustAbilityDB>())
            {
                var db = OrderingEntityState.Entity.GetDataBlob<NewtonThrustAbilityDB>();
                _newtonUI = new NewtonionOrderUI(db, _massOrderingEntity);
            }

            //debug code:
            //var sgpCur = _orderEntityOrbit.GravitationalParameterAU;
            //var relativeVel1 = OrbitProcessor.InstantaneousOrbitalVelocityVector_AU(_orderEntityOrbit, _departureDateTime);
            //var ralPosCBAU = OrderingEntityState.Entity.GetDataBlob<PositionDB>().RelativePosition_AU;
            //var smaCurrOrbtAU = _orderEntityOrbit.SemiMajorAxisAU;
            //var relativeVel2 = OrbitMath.PreciseOrbitalVelocityVector(_stdGravParamCurrentBody, ralPosCBAU, smaCurrOrbtAU, _orderEntityOrbit.Eccentricity, _orderEntityOrbit.LongitudeOfAscendingNode + _orderEntityOrbit.ArgumentOfPeriapsis); 
        }


        void TargetSelected() 
        { 
            TargetEntity = _uiState.LastClickedEntity;

            _uiState.Camera.PinToEntity(TargetEntity.Entity);
            _targetRadiusAU = TargetEntity.Entity.GetDataBlob<MassVolumeDB>().RadiusInAU;
            _targetRadius_m = TargetEntity.Entity.GetDataBlob<MassVolumeDB>().RadiusInM;

            var soiWorldRad_AU = TargetEntity.Entity.GetSOI_AU();
            _apMax = soiWorldRad_AU;

            float soiViewUnits = _uiState.Camera.ViewDistance(soiWorldRad_AU);


            _massTargetBody = TargetEntity.Entity.GetDataBlob<MassVolumeDB>().MassDry;
            _stdGravParamTargetBody_m = GeneralMath.StandardGravitationalParameter(_massOrderingEntity + _massTargetBody);
            
            InsertionCalcs();


            Vector2 viewPortSize = _uiState.Camera.ViewPortSize;
            float windowLen = (float)Math.Min(viewPortSize.X, viewPortSize.Y);
            if (soiViewUnits < windowLen * 0.5)
            {
                //zoom so soi fills ~3/4 screen.
                var soilenwanted = windowLen * 0.375;
                _uiState.Camera.ZoomLevel = (float)(soilenwanted / _apMax) ; 
            }


            if (_orbitWidget != null)
            {
                _orbitWidget = new OrbitOrderWidget(TargetEntity.Entity);
                _uiState.SelectedSysMapRender.UIWidgets[nameof(_orbitWidget)] = _orbitWidget;
 
            }
            else
            {
                _orbitWidget = new OrbitOrderWidget(TargetEntity.Entity);
                _uiState.SelectedSysMapRender.UIWidgets.Add(nameof(_orbitWidget), _orbitWidget);
            }
            

            OrderingEntityState.DebugOrbitOrder = _orbitWidget;
            _moveWidget.SetArrivalTarget(TargetEntity.Entity);


            _tooltipText = "Select Insertion Point";
            CurrentState = States.NeedsInsertionPoint;
        }
        void InsertionPntSelected() { 
            //var transitLeavePnt = _uiState.LastWorldPointClicked;
            //var relativeLeavePnt =  transitLeavePnt - GetTargetPosition();
            //var distanceSelectedKM = Distance.MToKm(relativeLeavePnt.Length());
            _moveWidget.SetArrivalPosition(_targetInsertionPoint_m);
            //_apoapsisKm = Math.Min(_apMax, distanceSelected);
            //_apAlt = _apoapsisKm - _targetRadius;
            _tooltipText = "Action to give order";
            CurrentState = States.NeedsActioning;
        }

        void ActionCmd() 
        {

            WarpMoveCommand.CreateCommand(
                _uiState.Faction.GetDataBlob<FactionInfoDB>().Data.CargoGoods,
                _uiState.Faction.Id,
                OrderingEntityState.Entity,
                TargetEntity.Entity,
                _targetInsertionPoint_m,
                _departureDateTime,
                _deltaV_MS,
                _massOrderingEntity);
            
            CloseWindow();
        }
        void ActionAddDB()
        {
            // FIXME:
            // _uiState.SpaceMasterVM.SMSetOrbitToEntity(
            //     OrderingEntityState.Entity, 
            //     TargetEntity.Entity, 
            //     _orbitWidget.Periapsis.Length(), 
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
            if (IsActive)
            {
                var size = new System.Numerics.Vector2(200, 100);
                var pos = new System.Numerics.Vector2(_uiState.MainWinSize.X / 2 - size.X / 2, _uiState.MainWinSize.Y / 2 - size.Y / 2);

                ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowPos(pos, ImGuiCond.FirstUseEver);

                if (ImGui.Begin(_displayText, ref IsActive, _flags))
                {
                    //put calcs that needs refreshing each frame in here. (ie calculations from mouse cursor position)
                    if (_orbitWidget != null)
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

                                        var mousePos = ImGui.GetMousePos();

                                        var mouseWorldPos = _uiState.Camera.MouseWorldCoordinate_m();
                                        _targetInsertionPoint_m = (mouseWorldPos - GetTargetPosition()); //relative to the target body

                                        _moveWidget.SetArrivalPosition(_targetInsertionPoint_m);

                                        //var velAU = OrbitProcessor.PreciseOrbitalVector(sgpCBAU, ralPosCBAU, smaCurrOrbtAU);


                                        _ke_m = OrbitMath.KeplerFromPositionAndVelocity(_stdGravParamTargetBody_m, _targetInsertionPoint_m, _insertionOrbitalVelocity_m, _departureDateTime);


                                        _orbitWidget.SetParametersFromKeplerElements(_ke_m, _targetInsertionPoint_m);
                                        _apoapsis_m = _ke_m.Apoapsis;
                                        _periapsis_m = _ke_m.Periapsis;
                                        Eccentricity = _ke_m.Eccentricity;
                                    }
                                    else
                                    {
                                        var mousePos = ImGui.GetMousePos();

                                        var mouseWorldPos = _uiState.Camera.MouseWorldCoordinate_m();
                                        _targetInsertionPoint_m = (mouseWorldPos - GetTargetPosition()); //relative to the target body

                                        _moveWidget.SetArrivalPosition(_targetInsertionPoint_m);
                                        _ke_m = OrbitMath.FromPosition(_targetInsertionPoint_m, _stdGravParamCurrentBody, _departureDateTime);
                                        _orbitWidget.SetParametersFromKeplerElements(_ke_m, _targetInsertionPoint_m);
                                        _apoapsis_m = _ke_m.Apoapsis;
                                        _periapsis_m = _ke_m.Periapsis;
                                        Eccentricity = _ke_m.Eccentricity;

                                    }

                                    break;
                                }

                            case States.NeedsActioning:
                                {
                                    if (_strictNewtonMode)
                                    {
                                        if (_newtonUI != null)
                                        {
                                            if (_newtonUI.Display())
                                                InsertionCalcs();
                                        }

                                        _ke_m = OrbitMath.KeplerFromPositionAndVelocity(_stdGravParamTargetBody_m, _targetInsertionPoint_m, _insertionOrbitalVelocity_m, _departureDateTime);

                                        _orbitWidget.SetParametersFromKeplerElements(_ke_m, _targetInsertionPoint_m);
                                        _apoapsis_m = _ke_m.Apoapsis;
                                        _periapsis_m = _ke_m.Periapsis;
                                        Eccentricity = _ke_m.Eccentricity;
                                    }
                                    else
                                    {
                                        _ke_m = OrbitMath.FromPosition(_targetInsertionPoint_m, _stdGravParamCurrentBody, _departureDateTime);
                                        _orbitWidget.SetParametersFromKeplerElements(_ke_m, _targetInsertionPoint_m);
                                        _apoapsis_m = _ke_m.Apoapsis;
                                        _periapsis_m = _ke_m.Periapsis;
                                        Eccentricity = _ke_m.Eccentricity;
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

                        ImGui.Text("Apoapsis: ");
                        ImGui.SameLine();
                        ImGui.Text(Stringify.Distance(_apoapsis_m) + " (Alt: " + Stringify.Distance(_apAlt) + ")");

                        ImGui.Text("Periapsis: ");
                        ImGui.SameLine();
                        ImGui.Text(Stringify.Distance(_periapsis_m) + " (Alt: " + Stringify.Distance(_peAlt) + ")");

                        ImGui.Text("DepartureSpeed: ");
                        //ImGui.SameLine();
                        ImGui.Text( Stringify.Distance( _departureOrbitalSpeed_m) + "/s");

                        ImGui.Text("InsertionSpeed: ");
                        //ImGui.SameLine();
                        ImGui.Text(Stringify.Distance(_insertionOrbitalSpeed_m) + "/s");




                        ImGui.Text("Departure Vector: ");
                        //ImGui.SameLine();
                        ImGui.Text("X: " + Stringify.Distance(_departureOrbitalVelocity_m.X)+ "/s");
                        ImGui.Text("Y: " + Stringify.Distance(_departureOrbitalVelocity_m.Y)+ "/s");
                        ImGui.Text("Z: " + Stringify.Distance(_departureOrbitalVelocity_m.Z)+ "/s");


                        ImGui.Text("Departure Angle: ");
                        ImGui.SameLine();
                        ImGui.Text(_departureAngle.ToString("g3") + " radians or " + Angle.ToDegrees(_departureAngle).ToString("F") + " deg ");

                        /*
                        var pc = OrbitProcessor.InstantaneousOrbitalVelocityPolarCoordinate(_orderEntityOrbit, _departureDateTime);

                        ImGui.Text("Departure Polar Coordinates: ");
                        ImGui.Text(pc.Item1.ToString() + " AU or " + Distance.AuToMt(pc.Item1).ToString("F") + " m/s");
                        ImGui.Text(pc.Item2.ToString("g3") + " radians or " + Angle.ToDegrees(pc.Item2).ToString("F") + " deg ");
                        ;
*/

                        ImGui.Text("Insertion Vector: ");
                        ImGui.Text("X: " + Stringify.Distance(_insertionOrbitalVelocity_m.X)+ "/s");
                        ImGui.Text("Y: " + Stringify.Distance(_insertionOrbitalVelocity_m.Y)+ "/s");
                        ImGui.Text("Z: " + Stringify.Distance(_insertionOrbitalVelocity_m.Z)+ "/s");

                        ImGui.Text("Insertion Position: ");
                        ImGui.Text("X: " + Stringify.Distance(_targetInsertionPoint_m.X));
                        ImGui.Text("Y: " + Stringify.Distance(_targetInsertionPoint_m.Y));
                        ImGui.Text("Z: " + Stringify.Distance(_targetInsertionPoint_m.Z));
                        
                        ImGui.Text("LoAN: ");
                        ImGui.SameLine();
                        ImGui.Text(_ke_m.LoAN.ToString("g3"));

                        ImGui.Text("AoP: ");
                        ImGui.SameLine();
                        ImGui.Text(_ke_m.AoP.ToString("g3"));

                        ImGui.Text("LoP Angle: ");
                        ImGui.SameLine();
                        ImGui.Text((_ke_m.LoAN + _ke_m.AoP).ToString("g3") + " radians or " + Angle.ToDegrees(_ke_m.LoAN + _ke_m.AoP).ToString("F") + " deg ");

                        if (_orbitWidget != null)
                            ImGui.Text("Is Retrograde " + _orbitWidget.IsRetrogradeOrbit.ToString());

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

                    ImGui.End();
                }
            }
        }

        #endregion

        #region helper calcs



        Orbital.Vector3 GetTargetPosition()
        {
            return TargetEntity.Entity.GetDataBlob<PositionDB>().AbsolutePosition;
        }
        Orbital.Vector3 GetMyPosition()
        {
            return OrderingEntityState.Entity.GetDataBlob<PositionDB>().AbsolutePosition;
        }

        void DepartureCalcs()
        {

            //OrbitProcessor.InstantaneousOrbitalVelocityPolarCoordinate()

            
            if(OrbitProcessor.UseRelativeVelocity)
                _departureOrbitalVelocity_m = OrderingEntityState.Entity.GetRelativeFutureVelocity(_departureDateTime);
            else
                _departureOrbitalVelocity_m = OrderingEntityState.Entity.GetAbsoluteFutureVelocity(_departureDateTime);
            
            _departureOrbitalSpeed_m = _departureOrbitalVelocity_m.Length();
            _departureAngle = Math.Atan2(_departureOrbitalVelocity_m.Y, _departureOrbitalVelocity_m.X);
            _moveWidget.SetDepartureProgradeAngle(_departureAngle);
            if (_newtonUI != null)
                _newtonUI.DepartureAngle = _departureAngle;
        }

        void InsertionCalcs()
        {
            _deltaV_MS = _newtonUI.DeltaV;
            OrbitDB targetOrbit = TargetEntity.Entity.GetDataBlob<OrbitDB>();
            (Vector3 position, DateTime eti) targetIntercept = OrbitProcessor.GetInterceptPosition(OrderingEntityState.Entity, TargetEntity.Entity.GetDataBlob<OrbitDB>(), _departureDateTime);

            DateTime estArivalDateTime = targetIntercept.eti; //rough calc. 
            
            Vector3 insertionVector = OrbitProcessor.GetOrbitalInsertionVector(_departureOrbitalVelocity_m, targetOrbit, estArivalDateTime);//_departureOrbitalVelocity - parentOrbitalVector;
            _insertionOrbitalVelocity_m = insertionVector;

            _insertionOrbitalVelocity_m +=  _deltaV_MS;
            _insertionOrbitalSpeed_m = _insertionOrbitalVelocity_m.Length();
            _insertionAngle = Math.Atan2(_insertionOrbitalVelocity_m.Y, _insertionOrbitalVelocity_m.X);
            _moveWidget.SetArivalProgradeAngle(_insertionAngle);

            /*
            var sgpCBAU = UniversalConstants.Science.GravitationalConstant * (_massCurrentBody + _massOrderingEntity) / 3.347928976e33;// (149597870700 * 149597870700 * 149597870700);
            var ralPosCBAU = OrderingEntity.Entity.GetDataBlob<PositionDB>().RelativePosition_AU;
            var smaCurrOrbtAU = OrderingEntity.Entity.GetDataBlob<OrbitDB>().SemiMajorAxis;
            var velAU = OrbitProcessor.PreciseOrbitalVector(sgpCBAU, ralPosCBAU, smaCurrOrbtAU);
            */
        }


        #endregion


        internal override void EntityClicked(EntityState entity, MouseButtons button)
        {
            if(button == MouseButtons.Primary)
                fsm[(byte)CurrentState, (byte)Events.SelectedEntity].Invoke();
        }
        internal override void MapClicked(Orbital.Vector3 worldPos_m, MouseButtons button)
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
            if (_orbitWidget != null)
            {
                _uiState.SelectedSysMapRender.UIWidgets.Remove(nameof(_orbitWidget));
                _orbitWidget = null;
            }
            if (_moveWidget != null)
            {
                _uiState.SelectedSysMapRender.UIWidgets.Remove(nameof(_moveWidget));
                _moveWidget = null;
            }
        }
    }
}
