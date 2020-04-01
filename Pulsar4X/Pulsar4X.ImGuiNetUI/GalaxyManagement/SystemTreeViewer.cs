using ImGuiNET;
using Pulsar4X.ECSLib;
using System.Numerics;
using System.Collections.Generic;

namespace Pulsar4X.SDL2UI
{
    public class SystemTreeViewer : PulsarGuiWindow
    {
	    private SystemTreeViewer()
	    {
	        _flags =  ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize;
        
        }

        internal static SystemTreeViewer GetInstance() {

            SystemTreeViewer thisItem;
            if (!_state.LoadedWindows.ContainsKey(typeof(SystemTreeViewer)))
            {
                thisItem = new SystemTreeViewer();
            }
            else
            {
                thisItem = (SystemTreeViewer)_state.LoadedWindows[typeof(SystemTreeViewer)];
            }
             

            return thisItem;


        }
        //displays selected entity info
        internal override void Display()
        {
           
            ImGui.SetNextWindowSize(new Vector2(175, 225), ImGuiCond.Once);
            if (ImGui.Begin("Objects In system", _flags))
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


                    if (_SelectedEntity.HasDataBlob<MassVolumeDB>())
                    {
                        ImGui.Text("Volume: " + _SelectedEntity.GetDataBlob<MassVolumeDB>().Volume_km3 + " KM^3");
                    }


                    ImGui.Text("Subject: " +  _SelectedEntityState.Name);
                    
                    //ImGui.Text(""+_state.LastClickedEntity.);
                    //gets all children and parent nodes, displays their names and makes them clickable to navigate towards them.

                    

                    if(_SelectedEntity.HasDataBlob<PositionDB>()){
                        ImGui.Text("Parent entity: ");

                        var _parentEntity = _SelectedEntity.GetDataBlob<PositionDB>().Parent;
                        bool _hasParentEntity = false;
                        SystemState _StarSystemState = _state.StarSystemStates[_state.SelectedStarSysGuid];
                        Dictionary<System.Guid, EntityState> _NamedEntityStates = _StarSystemState.EntityStatesWithNames;
                        if (_parentEntity != null)
                        {
                            //checks if parent exists in the selected star system and has a name
                            //notice that parent can be any bodyType(ex. asteroid, comet, planet etc), unlike childrenEntities, which are more selectively displayed...
                            if(_NamedEntityStates.ContainsKey(_parentEntity.Guid))
                            {
                                var tempEntityState = _NamedEntityStates[_parentEntity.Guid];
                                _hasParentEntity = true;
                                if(ImGui.SmallButton(tempEntityState.Name))
                                {
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
                        foreach(var childEntity in _SelectedEntity.GetDataBlob<PositionDB>().Children)
                        {
                            //checks if child exists in the seclted star system and has name
                            if(_NamedEntityStates.ContainsKey(childEntity.Guid))
                            {
                                var tempEntityState = _NamedEntityStates[childEntity.Guid];
                                //only show child entities that arent comets or asteroids if the lastClickedEntity(parent entity) isnt either, if LastClickedEntity(parent entity) is either, then show them always
                                if(_SelectedEntityState.IsSmallBody() && !tempEntityState.IsSmallBody())
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
