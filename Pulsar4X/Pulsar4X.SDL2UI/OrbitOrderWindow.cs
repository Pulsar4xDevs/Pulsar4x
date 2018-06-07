using System;
using ImGuiNET;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public interface IOrderWindow
    {
        void TargetEntity(EntityState entity);
        void SDLEvent(SDL.SDL_Event e);
    }


    public class OrbitOrderWindow : PulsarGuiWindow, IOrderWindow
    {
        public enum States : byte
        {
            NeedEntity,
            NeedTarget,
            NeedInsertion,
            NeedEccentricity,
            NeedActioning
        }
        public States CurrentState;
        public enum Events : byte
        {
            MouseClickEntity,
            MouseClickMap,
            MouseClickActionButton,
            MouseAltClick
        }

        Action[,] fsm;

        EntityState OrderingEntity;
        EntityState TargetEntity;
        PositionDB _targetPositionDB;
        GlobalUIState _state;
        //SDL.SDL_Point _targetViewscreenPos;
        Vector4 _insertionPoint;
        string _displayText;

        double _kmFromTgtToApoapsis;
        double _focalDistanceKm;
        double _kmFromTgtToPeriapsis;

        OrbitOrderWidgetIcon _orbitWidget;

        public OrbitOrderWindow(GlobalUIState state, EntityState entity)
        {
            _state = state;
            OrderingEntity = entity;
            _state.OpenWindows.Add(this);
            _displayText = "Order: " + OrderingEntity.Name;


            fsm = new Action[5, 4]
            {
                //ClickEntity   clickMap        clickAction,    altClick   

                {EntitySel,     DoNothing,      DoNothing,      Disregard,},      //NeedEntity
                {TgtSelect,     DoNothing,      DoNothing,      Disregard,},      //NeedTarget
                {TgtSelect,     SetInsert,      DoNothing,      EntitySel,},      //NeedInsertion
                {TgtSelect,     SetEcentr,      DoNothing,      SetInsert,},      //NeedEccentricity
                {TgtSelect,     DoNothing,      Actioned,       Disregard},       //NeedActioning
            };
            CurrentState = States.NeedTarget;
        }

        void EntitySel()
        {
            CurrentState = States.NeedTarget;
        }
        void TgtSelect() 
        {
            _targetPositionDB = TargetEntity.Entity.GetDataBlob<PositionDB>();
            _orbitWidget = new OrbitOrderWidgetIcon(_targetPositionDB);
            _state.MapRendering.UIWidgets.Add(_orbitWidget);
            CurrentState = States.NeedInsertion; 
        }
        void SetInsert()
        {
            CurrentState = States.NeedEccentricity;
        }
        void SetEcentr() { CurrentState = States.NeedActioning; }
        void Actioned()
        {
            //CurrentState = States.Closed; 
        }
        void Disregard() 
        { 
            DestroySelf(); 
        }
        void DoNothing() { return; }

        void NeedEntityState()
        {

        }
        void NeedTgtState()
        {
            ImGui.SetTooltip("Select Body to Orbit");
            ImGui.Text("Target: ");
        }

        void NeedInsertion()
        {

            var mousePos = ImGui.IO.MousePosition;
            _insertionPoint = _state.Camera.MouseWorldCoordinate();//_targetPositionDB.AbsolutePosition - _state.Camera.MouseWorldCoordinate(); 

            _orbitWidget.InsertionPoint = _insertionPoint;
            _orbitWidget.SetPointArray();
            _orbitWidget.PhysicsUpdate();
            _kmFromTgtToApoapsis = Distance.AuToKm(_orbitWidget._orbitEllipseSemiMinor);





            ImGui.Text("Target: ");
            ImGui.SameLine();
            ImGui.Text(TargetEntity.Name);

            ImGui.SetTooltip("Set Insertion Point");
            ImGui.Text("Appoapsis: ");
            ImGui.SameLine();
            ImGui.Text(_kmFromTgtToApoapsis.ToString() + "Km");
            //ImGui.Text(_orbitWidget._orbitEllipseSemiMaj.ToString());
            ImGui.Text("x: " + _insertionPoint.X);
            ImGui.SameLine();
            ImGui.Text("y: " + _insertionPoint.Y);
        
        }

        void NeedEcentricity()
        {
            var focalPoint = _targetPositionDB.AbsolutePosition - _state.Camera.MouseWorldCoordinate();
            var focalDistance = focalPoint.Length();

            _orbitWidget._focalDistance = focalDistance;
            _orbitWidget.PhysicsUpdate();
            _focalDistanceKm = Distance.AuToKm(focalDistance);
            _kmFromTgtToPeriapsis = _orbitWidget._orbitEllipseSemiMinor;

            ImGui.Text("Target: ");
            ImGui.SameLine();
            ImGui.Text(TargetEntity.Name);

            ImGui.Text("Appoapsis: ");
            ImGui.SameLine();
            ImGui.Text(_kmFromTgtToApoapsis.ToString());

            ImGui.SetTooltip("Set Eccentricity");
            ImGui.Text("Linear Eccentricity: ");
            ImGui.SameLine();
            ImGui.Text(_focalDistanceKm.ToString());
        }

        void NeedAction()
        {
            ImGui.Text("Target: ");
            ImGui.SameLine();
            ImGui.Text(TargetEntity.Name);

            ImGui.Text("Appoapsis: ");
            ImGui.SameLine();
            ImGui.Text(_kmFromTgtToApoapsis.ToString());

            ImGui.Text("Linear Eccentricity: ");
            ImGui.SameLine();
            ImGui.Text(_focalDistanceKm.ToString());

            ImGui.Text("Periapsis: ");
            ImGui.SameLine();
            ImGui.Text(_kmFromTgtToPeriapsis.ToString());

            if (ImGui.Button("Action Order"))
            {
                var sysDateTime = OrderingEntity.Entity.Manager.ManagerSubpulses.SystemLocalDateTime;
                OrbitBodyCommand.CreateOrbitBodyCommand(_state.Game, sysDateTime, _state.Faction.Guid, this.OrderingEntity.Entity.Guid, TargetEntity.Entity.Guid, _kmFromTgtToApoapsis, _kmFromTgtToPeriapsis);
                DestroySelf();
            }
        }
        internal override void Display()
        {
            ImVec2 size = new ImVec2(200, 100);
            ImVec2 pos = new ImVec2(_state.MainWinSize.x / 2 - size.x / 2, _state.MainWinSize.y / 2 - size.y / 2);

            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(pos, ImGuiCond.FirstUseEver);

            ImGui.Begin(_displayText, ref IsActive, _flags);

            switch (CurrentState)
            {
                case States.NeedEntity:
                    break;
                case States.NeedTarget:
                    NeedTgtState();
                    break;
                case States.NeedInsertion:
                    NeedInsertion();
                    break;
                case States.NeedEccentricity:
                    NeedEcentricity();
                    break;
                case States.NeedActioning:
                    NeedAction();
                    break;
            }



            ImGui.End();
        }

        void DestroySelf()
        {
            if(_orbitWidget != null)
                _state.MapRendering.UIWidgets.Remove(_orbitWidget);
            _state.OpenWindows.Remove(this);

        }

        void IOrderWindow.TargetEntity(EntityState entity)
        {
            TargetEntity = entity;
            fsm[(byte)CurrentState, (byte)Events.MouseClickEntity].Invoke();
        }

        public void SDLEvent(SDL.SDL_Event e)
        {
            if (ImGui.IsMouseClicked(0))
            {
                fsm[(byte)CurrentState, (byte)Events.MouseClickEntity].Invoke();
            }
            if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP && !ImGui.IO.WantCaptureMouse)
            {
                if (e.button.button == 1)
                    fsm[(byte)CurrentState, (byte)Events.MouseClickMap].Invoke();
                if (e.button.button == 3)
                    fsm[(byte)CurrentState, (byte)Events.MouseAltClick].Invoke();
            }

        }
    }
}
