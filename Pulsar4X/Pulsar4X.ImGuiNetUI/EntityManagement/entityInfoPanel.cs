﻿using ImGuiNET;
using Pulsar4X.ECSLib;
using System.Numerics;

namespace Pulsar4X.SDL2UI
{
    public class EntityInfoPanel : PulsarGuiWindow
    {
	    private EntityInfoPanel()
	    {
	        _flags =  ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize;
        
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
           
            ImGui.SetNextWindowSize(new Vector2(175, 225), ImGuiCond.Once);
            if (ImGui.Begin("Currently selected", _flags))
            {

                if (_state.LastClickedEntity != null && _state.StarSystemStates.ContainsKey(_state.SelectedStarSysGuid)){

                    EntityState _SelectedEntityState = _state.LastClickedEntity;
                    Entity _SelectedEntity = _SelectedEntityState.Entity;


                    if (_state.PrimaryEntity != null)
                    {
                        //ImGui.Text("Primary: " + _state.PrimaryEntity.Name);
                    }
                    else
                    {
                        //ImGui.Text("(Select primary...)");
                    }



                    

                    ImGui.Text("Subject: " +  _SelectedEntityState.Name);
                    
                    //ImGui.Text(""+_state.LastClickedEntity.);
                    //gets all children and parent nodes, displays their names and makes them clickable to navigate towards them.

                    

                    if(_SelectedEntity.HasDataBlob<PositionDB>()){
                        ImGui.Text("Parent entity: ");

                        var _parentEntity = _SelectedEntity.GetDataBlob<PositionDB>().Parent;
                        bool _hasParentEntity = false;
                        SystemState _StarSystemState = _state.StarSystemStates[_state.SelectedStarSysGuid];
                        if (_parentEntity != null){
                            //checks if parent exists in the selected star system and has a name
                            //notice that parent can be any bodyType(ex. asteroid, comet, planet etc), unlike childrenEntities, which are more selectively displayed...
                            if(_StarSystemState.EntityStatesWithNames.ContainsKey(_parentEntity.Guid)){
                                var tempEntityState = _StarSystemState.EntityStatesWithNames[_parentEntity.Guid];
                                _hasParentEntity = true;
                                if(ImGui.SmallButton(tempEntityState.Name)){
                                //if(ImGui.SmallButton(parentEntity.GetDataBlob<NameDB>().GetName(_state.Faction.ID))){
                                    _state.EntityClicked(_parentEntity.Guid, _state.SelectedStarSysGuid, MouseButtons.Primary);
                                //}
                                }
                            }
                        }
                        if(!_hasParentEntity)
                        {
                            ImGui.Text("(...No parent entity)");
                        }
                        bool hasChildrenEntities = false;
                        ImGui.Text("Children entities: ");
                        foreach(var childEntity in _SelectedEntity.GetDataBlob<PositionDB>().Children){
                            //checks if child exists in the seclted star system and has name
                            if(_StarSystemState.EntityStatesWithNames.ContainsKey(childEntity.Guid)){
                                var tempEntityState = _StarSystemState.EntityStatesWithNames[childEntity.Guid];
                                //only show child entities that arent comets or asteroids if the lastClickedEntity(parent entity) isnt either, if LastClickedEntity(parent entity) is either, then show them always
                                if(_SelectedEntityState.BodyType == UserOrbitSettings.OrbitBodyType.Asteroid || _SelectedEntityState.BodyType == UserOrbitSettings.OrbitBodyType.Comet ||(tempEntityState.BodyType != UserOrbitSettings.OrbitBodyType.Asteroid && tempEntityState.BodyType != UserOrbitSettings.OrbitBodyType.Comet))
                                {
                                    hasChildrenEntities = true;
                                    if(ImGui.SmallButton(tempEntityState.Name)){
                                        _state.EntityClicked(childEntity.Guid, _state.SelectedStarSysGuid, MouseButtons.Primary);
                                    }
                                }

                            }
                        }
                        if(!hasChildrenEntities){
                            ImGui.Text("(...No children entities)");
                        }
                    }
                }else
                {

                    ImGui.Text("(select subject...)");
                }

                if (ImGui.Button("see all small bodies"))
                {
                    SmallBodyEntityInfoPanel.GetInstance().SetActive();
                }
                ImGui.End();
            }
        }
    }
}
