using System;
using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class EntityState
    {
        public Entity Entity;
        public string Name;

        public NameIcon NameIcon;
        public OrbitIcon OrbitIcon;
        public OrbitOrderWiget DebugOrbitOrder;

        public Dictionary<Type,BaseDataBlob> DataBlobs = new Dictionary<Type,BaseDataBlob>();

        public CommandReferences CmdRef;

        public EntityState(Entity entity)
        {
            Entity = entity;
            foreach (var db in entity.DataBlobs)
            {
                DataBlobs.Add(db.GetType(), db);
            }
            entity.ChangeEvent += On_entityChangeEvent;
        }

        void On_entityChangeEvent(EntityChangeData.EntityChangeType changeType, BaseDataBlob db)
        {
            switch (changeType)
            {
                case EntityChangeData.EntityChangeType.DBAdded:
                    DataBlobs.Add(db.GetType(), db);
                    break;
                case EntityChangeData.EntityChangeType.DBRemoved:
                    DataBlobs.Remove(db.GetType());
                    break;
                default:
                    break;
            }
        }

    }

    public class EntityContextMenu
    {

        GlobalUIState _state;
        internal Entity ActiveEntity; //interacting with/ordering this entity
        ImVec2 buttonSize = new ImVec2(100, 12);

        EntityState _entityState;

        public EntityContextMenu(GlobalUIState state, Guid entityGuid)
        {
            _state = state;
            //_state.OpenWindows.Add(this);
            //IsActive = true;
            _entityState = state.MapRendering.IconEntityStates[entityGuid];

        }

        internal void Display()
        {
            ActiveEntity = _entityState.Entity;
            ImGui.BeginGroup();

            if (ImGui.SmallButton("Pin Camera"))
            {
                _state.Camera.PinToEntity(_entityState.Entity);
                ImGui.CloseCurrentPopup();
            }

            //if entity can move
            if (_entityState.Entity.HasDataBlob<PropulsionAbilityDB>())
            {
                if (ImGui.SmallButton("Translate to a new orbit"))
                {
                    OrbitOrderWindow.GetInstance(_entityState).IsActive = true;
                    _state.ActiveWindow = OrbitOrderWindow.GetInstance(_entityState);
                }
                if(ImGui.SmallButton("Change current orbit"))
                {
                    ChangeCurrentOrbitWindow.GetInstance(_entityState).IsActive = true;
                    _state.ActiveWindow = ChangeCurrentOrbitWindow.GetInstance(_entityState);
                }
            }
            if (_entityState.Entity.HasDataBlob<FireControlAbilityDB>())
            {
                if (ImGui.SmallButton("Fire Control"))
                {
                    var instance = WeaponTargetingControl.GetInstance(_entityState);
                    instance.SetOrderEntity(_entityState);
                    instance.IsActive = true;
                    _state.ActiveWindow = instance;
                }
            }
            if (ImGui.SmallButton("Rename"))
            {
                RenameWindow.GetInstance(_entityState).IsActive = true;
                _state.ActiveWindow = RenameWindow.GetInstance(_entityState);
                ImGui.CloseCurrentPopup();
            }
            //if entity can target

            if (_entityState.Entity.HasDataBlob<CargoStorageDB>())
            {
                if (ImGui.SmallButton("Cargo"))
                {
                    var instance = ColonyPanel.GetInstance(_entityState);
                    instance.IsActive = true;
                    _state.ActiveWindow = instance;
                }
            }
            //if entity can mine || refine || build
            //econOrderwindow

            ImGui.EndGroup();

        }
    }
}
