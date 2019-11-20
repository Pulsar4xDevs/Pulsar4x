using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.ImGuiNetUI.EntityManagement;

namespace Pulsar4X.SDL2UI
{
    public class EntityInfoPanel : PulsarGuiWindow
    {
	    private EntityInfoPanel()
	    {
	        _flags =  ImGuiWindowFlags.NoCollapse;
        
        }

        internal static EntityInfoPanel GetInstance() {

            EntityInfoPanel thisItem;
            if (!_state.LoadedWindows.ContainsKey(typeof(EntityInfoPanel)))
            {
                thisItem = new EntityInfoPanel();
            }
            else
            {
                thisItem = (EntityInfoPanel)_state.LoadedWindows[typeof(EntityInfoPanel)];
            }
             

            return thisItem;


        }
        //displays selected entity info
        internal override void Display()
        {
            if (ImGui.Begin("Currently selected", _flags))
            {
                if(_state.LastClickedEntity != null){
                    ImGui.Text("Name: "+_state.LastClickedEntity.Name);
                    //ImGui.Text(""+_state.LastClickedEntity.);
                    //gets all children and parent nodes, displays their names and makes them clickable to navigate towards them.

                    if(_state.LastClickedEntity.Entity.HasDataBlob<PositionDB>()){
                        ImGui.Text("Parent entity: ");
                        var parentEntity = _state.LastClickedEntity.Entity.GetDataBlob<PositionDB>().Parent;
                        bool hasParentEntity = false;
                        if(parentEntity != null){
                            if(_state.StarSystemStates[_state.SelectedStarSysGuid].EntityStatesWithNames.ContainsKey(parentEntity.Guid)){
                                hasParentEntity = true;
                                if(ImGui.SmallButton(_state.StarSystemStates[_state.SelectedStarSysGuid].EntityStatesWithNames[parentEntity.Guid].Name)){
                                //if(ImGui.SmallButton(parentEntity.GetDataBlob<NameDB>().GetName(_state.Faction.Guid))){
                                    _state.EntityClicked(parentEntity.Guid, _state.SelectedStarSysGuid, MouseButtons.Primary);
                                //}
                                }
                            }
                        }
                        if(!hasParentEntity){
                            ImGui.Text("(...No parent entity)");
                        }
                        bool hasChildrenEntities = false;
                        ImGui.Text("Children entities: ");
                        foreach(var childEntity in _state.LastClickedEntity.Entity.GetDataBlob<PositionDB>().Children){
                            if(_state.StarSystemStates[_state.SelectedStarSysGuid].EntityStatesWithNames.ContainsKey(childEntity.Guid)){
                                hasChildrenEntities = true;
                                if(ImGui.SmallButton(_state.StarSystemStates[_state.SelectedStarSysGuid].EntityStatesWithNames[childEntity.Guid].Name)){
                                    _state.EntityClicked(childEntity.Guid, _state.SelectedStarSysGuid, MouseButtons.Primary);
                                }
                            }
                        }
                        if(!hasChildrenEntities){
                            ImGui.Text("(...No children entities)");
                        }
                    }
                }
                ImGui.End();
            }
        }
    }
}
