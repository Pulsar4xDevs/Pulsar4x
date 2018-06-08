using System;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class OrbitOrderWindow : PulsarGuiWindow// IOrderWindow
    {
        GlobalUIState _state;
        EntityState OrderingEntity;

        EntityState TargetEntity;
        Vector4 InsertionPoint;
        Vector4 PeriapsisPoint;
        double _semiMajorKm;
        double _semiMinorKm;

        enum States: byte { NeedsEntity, NeedsTarget, NeedsApoapsis, NeedsPeriapsis, NeedsActioning }
        States CurrentState;

        enum Events: byte { SelectedEntity, SelectedPosition, ClickedAction, AltClicked }

        Action[,] fsm;

        string _displayText;
        string _tooltipText = "";
        public OrbitOrderWindow(GlobalUIState state, EntityState entity)
        {
            _state = state;
            OrderingEntity = entity;
            _state.OpenWindows.Add(this);
            _displayText = "Orbit Order: " + OrderingEntity.Name;
            _tooltipText = "Select target to orbit";
            CurrentState = States.NeedsTarget;

            fsm = new Action[5, 4]
            {
                //selectEntity      selectPos               clickAction     altClick
                {DoNothing,    DoNothing,              DoNothing,      Disregard},     //needsEntity
                {TargetSelected,    DoNothing,              DoNothing,      GoBackState,}, //needsTarget
                {DoNothing,         InsertionPntSelected,   DoNothing,      GoBackState,}, //needsApopapsis
                {DoNothing,         PeriapsisPntSelected,   DoNothing,      GoBackState,}, //needsPeriapsis
                {DoNothing,         DoNothing,              ActionCmd,      GoBackState,}  //needsActoning
            };

        }

        void DoNothing() { return; }
        void EntitySelected() { 
            OrderingEntity = _state.LastClickedEntity;
            CurrentState = States.NeedsTarget;
        }
        void TargetSelected() { 
            TargetEntity = _state.LastClickedEntity;
            _tooltipText = "Select Apoapsis point";
            CurrentState = States.NeedsApoapsis;
        }
        void InsertionPntSelected() { 
            InsertionPoint = _state.LastWorldPointClicked;
            _semiMajorKm = (GetTargetPosition() - InsertionPoint).Length();
            _tooltipText = "Select Periapsis point";
            CurrentState = States.NeedsPeriapsis;
        }
        void PeriapsisPntSelected() { 
            PeriapsisPoint = _state.LastWorldPointClicked;
            _semiMinorKm = (GetTargetPosition() - PeriapsisPoint).Length();
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
                _semiMajorKm, 
                _semiMinorKm);
            _state.OpenWindows.Remove(this); 
        }
        void Disregard() { _state.OpenWindows.Remove(this); }
        void GoBackState() {/*???*/}

        Vector4 GetTargetPosition()
        {
            return TargetEntity.Entity.GetDataBlob<PositionDB>().AbsolutePosition;
        }

        internal override void Display()
        {
            ImVec2 size = new ImVec2(200, 100);
            ImVec2 pos = new ImVec2(_state.MainWinSize.x / 2 - size.x / 2, _state.MainWinSize.y / 2 - size.y / 2);

            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(pos, ImGuiCond.FirstUseEver);

            ImGui.Begin(_displayText, ref IsActive, _flags);

            ImGui.SetTooltip(_tooltipText);
            ImGui.Text("Target: ");
            ImGui.SameLine();
            ImGui.Text(TargetEntity.Name);

            ImGui.Text("Apoapsis: ");
            ImGui.SameLine();
            ImGui.Text(_semiMajorKm.ToString());

            ImGui.Text("Periapsis: ");
            ImGui.SameLine();
            ImGui.Text(_semiMinorKm.ToString());

            if (ImGui.Button("Action Order"))
                fsm[(byte)CurrentState, (byte)Events.ClickedAction].Invoke();

            /*
            switch (CurrentState)
            {
                case States.NeedsEntity:
                    
                break;
                case States.NeedsTarget:
                    DisplayStateNeedsTarget();
                break;
                case States.NeedsApoapsis:
                    DisplayStateNeedsInsertionPnt();
                break;
                case States.NeedsPeriapsis:
                break;
                case States.NeedsActioning:
                break;
                default:
                break;
            }*/



            ImGui.End();
        }

        internal override void EntityClicked(Entity entity, MouseButtons button)
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

        //void IOrderWindow.TargetEntity(EntityState entity)
        //{
        //    TargetEntity = entity;
        //}
    }
}
