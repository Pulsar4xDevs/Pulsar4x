using System;
using ImGuiNET;
using Pulsar4X.ECSLib;
using System.Numerics;

namespace Pulsar4X.SDL2UI
{
    /// <summary>
    /// Orbit order window - this whole thing is a somewhat horrible state machine
    /// </summary>
    public class OrbitOrderWindow : PulsarGuiWindow// IOrderWindow
    {
       
        EntityState OrderingEntity;
        OrbitDB _orderEntityOrbit;
        EntityState TargetEntity;
        //Vector4 _apoapsisPoint;
        //Vector4 _periapsisPoint;
        float _maxDV;
        float _progradeDV;
        float _radialDV;

        ECSLib.Vector4 _deltaV_MS; 

        KeplerElements _ke;
        double _apoapsisKm;
        double _periapsisKM;
        double _targetRadiusAU;
        double _targetRadiusKM;
        double _peAlt { get { return _periapsisKM - _targetRadiusKM; } }
        double _apAlt { get { return _apoapsisKm - _targetRadiusKM; } }

        double _apMax;
        double _peMin { get { return _targetRadiusKM; } }

        double _eccentricity = double.NaN;


        DateTime _departureDateTime;
        double _departureOrbitalSpeed = double.NaN;
        ECSLib.Vector4 _departureOrbitalVelocity = ECSLib.Vector4.NaN;
        double _departureAngle = double.NaN;

        double _insertionOrbitalSpeed = double.NaN;
        ECSLib.Vector4 _insertionOrbitalVelocity = ECSLib.Vector4.NaN;
        double _insertionAngle = double.NaN;
        //(Vector4, TimeSpan) _intercept;

        double _massOrderingEntity = double.NaN;
        double _massTargetBody = double.NaN;
        double _massCurrentBody = double.NaN;
        double _stdGravParamCurrentBody = double.NaN;
        double _stdGravParamTargetBody = double.NaN;

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
            if(OrderingEntity.Entity.HasDataBlob<PropulsionAbilityDB>())
            {
                var propDB = OrderingEntity.Entity.GetDataBlob<PropulsionAbilityDB>();
                _maxDV = propDB.RemainingDV_MS;
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
            instance._departureDateTime = _state.CurrentSystemDateTime;
            _state.ActiveSystem.ManagerSubpulses.SystemDateChangedEvent += instance.OnSystemDateTimeChange;
            instance.EntitySelected();
            return instance;
        }

        #region Stuff that gets calculated when the state changes.
        void DoNothing() { return; }
        void EntitySelected() 
        { 
            OrderingEntity = _state.LastClickedEntity;
            _orderEntityOrbit = OrderingEntity.Entity.GetDataBlob<OrbitDB>();

            CurrentState = States.NeedsTarget;
            _massCurrentBody = _orderEntityOrbit.Parent.GetDataBlob<MassVolumeDB>().Mass;
            _massOrderingEntity = OrderingEntity.Entity.GetDataBlob<MassVolumeDB>().Mass;
            _stdGravParamCurrentBody = GameConstants.Science.GravitationalConstant * (_massCurrentBody + _massOrderingEntity) / 3.347928976e33;
            if (_moveWidget == null)
            {
                _moveWidget = new TranslateMoveOrderWidget(_state, OrderingEntity.Entity);
                _state.MapRendering.UIWidgets.Add(_moveWidget);
            }
            DepartureCalcs();

            //debug code:
            var sgpCur = _orderEntityOrbit.GravitationalParameterAU;
            var ralitiveVel1 = OrbitProcessor.PreciseOrbitalVelocityVector(_orderEntityOrbit, _departureDateTime);
            var ralPosCBAU = OrderingEntity.Entity.GetDataBlob<PositionDB>().RelativePosition_AU;
            var smaCurrOrbtAU = _orderEntityOrbit.SemiMajorAxis;
            var ralitiveVel2 = OrbitMath.PreciseOrbitalVelocityVector(_stdGravParamCurrentBody, ralPosCBAU, smaCurrOrbtAU, _orderEntityOrbit.Eccentricity, _orderEntityOrbit.LongitudeOfAscendingNode + _orderEntityOrbit.ArgumentOfPeriapsis); 
        }


        void TargetSelected() 
        { 
            TargetEntity = _state.LastClickedEntity;

            _state.Camera.PinToEntity(TargetEntity.Entity);
            _targetRadiusAU = TargetEntity.Entity.GetDataBlob<MassVolumeDB>().Radius;
            _targetRadiusKM = TargetEntity.Entity.GetDataBlob<MassVolumeDB>().RadiusInKM;

            var soiWorldRad_AU = GMath.GetSOI(TargetEntity.Entity);
            _apMax = soiWorldRad_AU;

            float soiViewUnits = _state.Camera.ViewDistance(soiWorldRad_AU);


            _massTargetBody = TargetEntity.Entity.GetDataBlob<MassVolumeDB>().Mass;
            _stdGravParamTargetBody = GameConstants.Science.GravitationalConstant * (_massTargetBody + _massOrderingEntity) / 3.347928976e33;
            InsertionCalcs();


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
            

            OrderingEntity.DebugOrbitOrder = _orbitWidget;
            _moveWidget.SetArrivalTarget(TargetEntity.Entity);


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

        void ActionCmd() 
        {

            TransitToOrbitCommand.CreateTransitCmd(
                _state.Game,
                _state.Faction,
                OrderingEntity.Entity,
                TargetEntity.Entity,
                _targetInsertionPoint_AU,
                _departureDateTime,
                Distance.MToAU(_deltaV_MS));
            
            CloseWindow();
        }
        void ActionAddDB()
        {
            _state.SpaceMasterVM.SMSetOrbitToEntity(OrderingEntity.Entity, TargetEntity.Entity, PointDFunctions.Length(_orbitWidget.Periapsis), _state.CurrentSystemDateTime);
            CloseWindow();
        }

        void AbortOrder() { CloseWindow(); }
        void GoBackState() { CurrentState -= 1; }


        #endregion

        #region Stuff that happens when the system date changes goes here

        void OnSystemDateTimeChange(DateTime newDate)
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

                        var ralPosCBAU = OrderingEntity.Entity.GetDataBlob<PositionDB>().RelativePosition_AU;
                        var smaCurrOrbtAU = _orderEntityOrbit.SemiMajorAxis;

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
                Vector2 size = new Vector2(200, 100);
                Vector2 pos = new Vector2(_state.MainWinSize.X / 2 - size.X / 2, _state.MainWinSize.Y / 2 - size.Y / 2);

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
                                    var maxprogradeDV = _maxDV - Math.Abs(_radialDV);
                                    var maxradialDV = _maxDV - Math.Abs(_progradeDV);
                                    if (ImGui.SliderFloat("Prograde DV", ref _progradeDV, -maxprogradeDV, maxprogradeDV))
                                    {
                                        InsertionCalcs();
                                    }
                                    if (ImGui.SliderFloat("Radial DV", ref _radialDV, -maxradialDV, maxradialDV))
                                    {
                                        InsertionCalcs();
                                    }

                                    var mousePos = ImGui.GetMousePos();

                                    var mouseWorldPos = _state.Camera.MouseWorldCoordinate();
                                    _targetInsertionPoint_AU = (mouseWorldPos - GetTargetPosition()); //ralitive to the target body

                                    _moveWidget.SetArrivalPosition(_targetInsertionPoint_AU);

                                    //var velAU = OrbitProcessor.PreciseOrbitalVector(sgpCBAU, ralPosCBAU, smaCurrOrbtAU);


                                    var ke = OrbitMath.KeplerFromVelocityAndPosition(_stdGravParamTargetBody, _targetInsertionPoint_AU, _insertionOrbitalVelocity);
                                    _ke = ke;

                                    _orbitWidget.SetParametersFromKeplerElements(ke, _targetInsertionPoint_AU);
                                    _apoapsisKm = Distance.AuToKm(ke.Apoapsis);
                                    _periapsisKM = Distance.AuToKm(ke.Periapsis);
                                    _eccentricity = ke.Eccentricity;
                                    break;
                                }
                            /*
                        case States.NeedsSecondApsis:
                            {
                                 TODO: when we've got newtonion engines, allow second apsis choice and expend Dv.
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
                                {
                                    var maxprogradeDV = _maxDV - Math.Abs(_radialDV);
                                    var maxradialDV = _maxDV - Math.Abs(_progradeDV);
                                    if (ImGui.SliderFloat("Prograde DV", ref _progradeDV, -maxprogradeDV, maxprogradeDV))
                                    {
                                        InsertionCalcs();
                                    }
                                    if (ImGui.SliderFloat("Radial DV", ref _radialDV, -maxradialDV, maxradialDV))
                                    {
                                        InsertionCalcs();
                                    }
                                    var ke = OrbitMath.KeplerFromVelocityAndPosition(_stdGravParamTargetBody, _targetInsertionPoint_AU, _insertionOrbitalVelocity);
                                    _ke = ke;
                                    _orbitWidget.SetParametersFromKeplerElements(ke, _targetInsertionPoint_AU);
                                    _apoapsisKm = Distance.AuToKm(ke.Apoapsis);
                                    _periapsisKM = Distance.AuToKm(ke.Periapsis);
                                    _eccentricity = ke.Eccentricity;
                                    break;
                                }
                            default:
                                break;
                        }
                    }


                    ImGui.SetTooltip(_tooltipText);
                    ImGui.Text("Target: ");
                    ImGui.SameLine();
                    ImGui.Text(TargetEntity.Name);

                    ImGui.Text("Apoapsis: ");
                    ImGui.SameLine();
                    ImGui.Text(_apoapsisKm.ToString("g3") + " (Alt: " + _apAlt.ToString("g3") + ")");

                    ImGui.Text("Periapsis: ");
                    ImGui.SameLine();
                    ImGui.Text(_periapsisKM.ToString("g3") + " (Alt: " + _peAlt.ToString("g3") + ")");

                    ImGui.Text("DepartureVelocity: ");
                    //ImGui.SameLine();
                    ImGui.Text(_departureOrbitalSpeed.ToString() + " AU");
                    ImGui.Text(Distance.AuToKm(_departureOrbitalSpeed).ToString() + " KM");

                    ImGui.Text("InsertionVelocity: ");
                    //ImGui.SameLine();
                    ImGui.Text(_insertionOrbitalSpeed.ToString() + " AU");
                    ImGui.Text(Distance.AuToKm(_insertionOrbitalSpeed).ToString() + " KM");

                    ImGui.Text("Eccentricity: ");
                    ImGui.Text(_eccentricity.ToString("g3"));

                    ImGui.Text("Departure Vector: ");
                    ImGui.SameLine();
                    ImGui.Text(_departureOrbitalVelocity.ToString("g3"));
                    ImGui.Text(Distance.AuToMt( _departureOrbitalVelocity).ToString("N") + "m/s");

                    ImGui.Text("Departure Angle: ");
                    ImGui.SameLine();
                    ImGui.Text(_departureAngle.ToString("g3") + " radians or " + Angle.ToDegrees(_departureAngle).ToString("F") + " deg ");

                    var pc = OrbitProcessor.PreciseOrbitalVelocityPolarCoordinate(_orderEntityOrbit, _departureDateTime);

                    ImGui.Text("Departure Polar Coordinates: ");
                    ImGui.Text(pc.Item1.ToString() + " AU or " + Distance.AuToMt(pc.Item1).ToString("F") + " m/s");
                    ImGui.Text(pc.Item2.ToString("g3") + " radians or " + Angle.ToDegrees(pc.Item2).ToString("F") + " deg ");;


                    ImGui.Text("Insertion Vector: ");
                    ImGui.SameLine();
                    ImGui.Text(_insertionOrbitalVelocity.ToString("g3"));

                    ImGui.Text("LoAN: ");
                    ImGui.SameLine();
                    ImGui.Text(_ke.LoAN.ToString("g3"));

                    ImGui.Text("AoP: ");
                    ImGui.SameLine();
                    ImGui.Text(_ke.AoP.ToString("g3"));

                    ImGui.Text("LoP Angle: ");
                    ImGui.SameLine();
                    ImGui.Text((_ke.LoAN + _ke.AoP).ToString("g3") + " radians or " + Angle.ToDegrees(_ke.LoAN + _ke.AoP).ToString("F") + " deg ");

                    if(_orbitWidget != null)
                        ImGui.Text("Clockwise " +  _orbitWidget.IsClockwiseOrbit.ToString());



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



        ECSLib.Vector4 GetTargetPosition()
        {
            return TargetEntity.Entity.GetDataBlob<PositionDB>().AbsolutePosition_AU;
        }
        ECSLib.Vector4 GetMyPosition()
        {
            return OrderingEntity.Entity.GetDataBlob<PositionDB>().AbsolutePosition_AU;
        }

        void DepartureCalcs()
        {
            _departureOrbitalVelocity = OrbitProcessor.GetOrbitalVector(_orderEntityOrbit, _departureDateTime);
            _departureOrbitalSpeed = _departureOrbitalVelocity.Length();
            _departureAngle = Math.Atan2(_departureOrbitalVelocity.X, _departureOrbitalVelocity.Y);
            _moveWidget.SetDepartureProgradeAngle(_departureAngle);
        }

        void InsertionCalcs()
        {
            OrbitDB targetOrbit = TargetEntity.Entity.GetDataBlob<OrbitDB>();
            (ECSLib.Vector4, DateTime) targetIntercept = InterceptCalcs.GetInterceptPosition(OrderingEntity.Entity, TargetEntity.Entity.GetDataBlob<OrbitDB>(), _departureDateTime);

            DateTime estArivalDateTime = targetIntercept.Item2; //rough calc. 

            double x = (_radialDV * Math.Cos(_departureAngle)) - (_progradeDV * Math.Sin(_departureAngle));
            double y = (_radialDV * Math.Sin(_departureAngle)) + (_progradeDV * Math.Cos(_departureAngle));
            _deltaV_MS = new ECSLib.Vector4(x, y, 0, 0);

            _insertionOrbitalVelocity = OrbitProcessor.GetOrbitalInsertionVector(_departureOrbitalVelocity, targetOrbit, estArivalDateTime);//_departureOrbitalVelocity - parentOrbitalVector;

            _insertionOrbitalVelocity += Distance.MToAU( _deltaV_MS);
            _insertionOrbitalSpeed = _insertionOrbitalVelocity.Length();
            _insertionAngle = Math.Atan2(_insertionOrbitalVelocity.X, _insertionOrbitalVelocity.Y);
            _moveWidget.SetArivalProgradeAngle(_insertionAngle);

            /*
            var sgpCBAU = GameConstants.Science.GravitationalConstant * (_massCurrentBody + _massOrderingEntity) / 3.347928976e33;// (149597870700 * 149597870700 * 149597870700);
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
            _state.ActiveSystem.ManagerSubpulses.SystemDateChangedEvent -= OnSystemDateTimeChange;

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
