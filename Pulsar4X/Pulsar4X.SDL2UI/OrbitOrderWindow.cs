using System;
using ImGuiNET;
using Pulsar4X.ECSLib;

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

        (Vector4, TimeSpan) _intercept;

        string _displayText;
        string _tooltipText = "";
        OrbitOrderWiget _orbitWidget;
        bool _smMode;

        enum States: byte { NeedsEntity, NeedsTarget, NeedsApoapsis, NeedsPeriapsis, NeedsActioning }
        States CurrentState;
        enum Events: byte { SelectedEntity, SelectedPosition, ClickedAction, AltClicked}
        Action[,] fsm;


        private OrbitOrderWindow(EntityState entity, bool smMode = false)
        {
            

            OrderingEntity = entity;
            _smMode = smMode;
            IsActive = true;

            _displayText = "Orbit Order: " + OrderingEntity.Name;
            _tooltipText = "Select target to orbit";
            CurrentState = States.NeedsTarget;

            if (OrderingEntity.Entity.HasDataBlob<OrbitDB>())
            {
                _orbitWidget = new OrbitOrderWiget(OrderingEntity.Entity.GetDataBlob<OrbitDB>());
                _state.MapRendering.UIWidgets.Add(_orbitWidget);
            }

            fsm = new Action[5, 4]
            {
                //selectEntity      selectPos               clickAction     altClick
                {DoNothing,         DoNothing,              DoNothing,      AbortOrder,  },     //needsEntity
                {TargetSelected,    DoNothing,              DoNothing,      GoBackState, }, //needsTarget
                {DoNothing,         InsertionPntSelected,   DoNothing,      GoBackState, }, //needsApopapsis
                {DoNothing,         PeriapsisPntSelected,   DoNothing,      GoBackState, }, //needsPeriapsis
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
            _targetRadius = TargetEntity.Entity.GetDataBlob<MassVolumeDB>().RadiusInKM;
            _apMax = GMath.GetSOI(TargetEntity.Entity);
            _intercept = InterceptCalcs.FTLIntercept(OrderingEntity.Entity, TargetEntity.Entity.GetDataBlob<OrbitDB>(), TargetEntity.Entity.Manager.ManagerSubpulses.SystemLocalDateTime);
            _tooltipText = "Select Apoapsis height";
            CurrentState = States.NeedsApoapsis;
        }
        void InsertionPntSelected() { 
            //var apoapsisPoint = _state.LastWorldPointClicked;
            //var distanceSelected = Distance.AuToKm((GetTargetPosition() - apoapsisPoint).Length());
            //_apoapsisKm = Math.Min(_apMax, distanceSelected);
            //_apAlt = _apoapsisKm - _targetRadius;
            _tooltipText = "Select Periapsis height";
            CurrentState = States.NeedsPeriapsis;
        }
        void PeriapsisPntSelected() { 
            //var periapsisPoint = _state.LastWorldPointClicked;
            //var distanceSelected = Distance.AuToKm((GetTargetPosition() - periapsisPoint).Length());
            //_periapsisKM = Math.Min(Math.Max(_peMin, distanceSelected), _apoapsisKm); 
            //_peAlt = _periapsisKM - _targetRadius;
            _tooltipText = "Action to give order";
            CurrentState = States.NeedsActioning;
        }
        void ActionCmd() 
        {
            OrbitBodyCommand.CreateOrbitBodyCommand(
                _state.Game,
                _state.Faction,
                OrderingEntity.Entity,
                TargetEntity.Entity,
                _apoapsisKm,//PointDFunctions.Length(_orbitWidget.Apoapsis), 
                _periapsisKM);//PointDFunctions.Length(_orbitWidget.Periapsis));
            CloseWindow();
        }
        void ActionAddDB()
        {
            _state.SpaceMasterVM.SMSetOrbitToEntity(OrderingEntity.Entity, TargetEntity.Entity, PointDFunctions.Length(_orbitWidget.Periapsis), _state.CurrentSystemDateTime);
            CloseWindow();
        }

        void AbortOrder() { CloseWindow(); }
        void GoBackState() { CurrentState -= 1; }

        Vector4 GetTargetPosition()
        {
            return TargetEntity.Entity.GetDataBlob<PositionDB>().AbsolutePosition_AU;
        }

        internal override void Display()
        {
            if (IsActive)
            {
                ImVec2 size = new ImVec2(200, 100);
                ImVec2 pos = new ImVec2(_state.MainWinSize.x / 2 - size.x / 2, _state.MainWinSize.y / 2 - size.y / 2);

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
                            case States.NeedsApoapsis:
                                {
                                    var mousePos = ImGui.GetMousePos();

                                    var mouseWorldPos = _state.Camera.MouseWorldCoordinate();
                                    var ralitivePos = (GetTargetPosition() - mouseWorldPos);
                                    _orbitWidget.SetApoapsis(ralitivePos.X, ralitivePos.Y);

                                    //_apoapsisKm = Distance.AuToKm((GetTargetPosition() - mouseWorldPos).Length());
                                    var distanceSelected = Distance.AuToKm((GetTargetPosition() - mouseWorldPos).Length());
                                    var d1 = Math.Min(_apMax, distanceSelected); //can't be higher than SOI
                                    _apoapsisKm = Math.Max(d1, _peMin); //cant be lower than the body radius


                                    //_orbitWidget.OrbitEllipseSemiMaj = (float)_semiMajorKm;
                                    break;
                                }
                            case States.NeedsPeriapsis:
                                {
                                    var mousePos = ImGui.GetMousePos();

                                    var mouseWorldPos = _state.Camera.MouseWorldCoordinate();

                                    var ralitivePos = (GetTargetPosition() - mouseWorldPos);
                                    _orbitWidget.SetPeriapsis(ralitivePos.X, ralitivePos.Y);

                                    //_periapsisKM = Distance.AuToKm((GetTargetPosition() - mouseWorldPos).Length());
                                    var distanceSelected = Distance.AuToKm((GetTargetPosition() - mouseWorldPos).Length());
                                    var d1 = Math.Max(_peMin, distanceSelected); //can't be lower than body radius
                                    _periapsisKM = Math.Min(d1, _apoapsisKm);  //can't be higher than apoapsis. 

                                    break;
                                }
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



        internal override void EntityClicked(EntityState entity, MouseButtons button)
        {
            if(button == MouseButtons.Primary)
                fsm[(byte)CurrentState, (byte)Events.SelectedEntity].Invoke();
        }
        internal override void MapClicked(Vector4 worldPos, MouseButtons button)
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


            if(_orbitWidget != null)
                _state.MapRendering.UIWidgets.Remove(_orbitWidget);
        }
    }
}
