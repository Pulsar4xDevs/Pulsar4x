using System;
using ImGuiNET;
using Pulsar4X.ECSLib;
using System.Numerics;
using System.Collections.Generic;

namespace Pulsar4X.SDL2UI
{
    public class EntityInfoPanel : PulsarGuiWindow
    {
        private ComponentInstance[][] _componentInstances = new ComponentInstance[0][];
        private Entity _selectedEntity;
	    private EntityInfoPanel()
	    {
	        _flags =  ImGuiWindowFlags.NoCollapse;// | ImGuiWindowFlags.AlwaysAutoResize;
        
        }

        internal static EntityInfoPanel GetInstance() {

            EntityInfoPanel thisItem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(EntityInfoPanel)))
            {
                thisItem = new EntityInfoPanel();
            }
            else
            {
                thisItem = (EntityInfoPanel)_uiState.LoadedWindows[typeof(EntityInfoPanel)];
            }
             

            return thisItem;


        }
        //displays selected entity info
        internal override void Display()
        {
           
            ImGui.SetNextWindowSize(new Vector2(264, 325), ImGuiCond.Once);
            if (ImGui.Begin("Currently selected", _flags))
            {

                if (_uiState.LastClickedEntity != null && _uiState.StarSystemStates.ContainsKey(_uiState.SelectedStarSysGuid)){

                    EntityState _SelectedEntityState = _uiState.LastClickedEntity;
                    if(_selectedEntity == null || _selectedEntity != _SelectedEntityState.Entity)
                    {
                        _selectedEntity = _SelectedEntityState.Entity;
                        _componentInstances = ComponentsDisplay.CreateNewInstanceArray(_selectedEntity);
                    }


                    if (_uiState.PrimaryEntity != null)
                    {
                        //ImGui.Text("Primary: " + _uiState.PrimaryEntity.Name);
                    }
                    else
                    {
                        //ImGui.Text("(Select primary...)");
                    }


                    if (_selectedEntity.HasDataBlob<MassVolumeDB>())
                    {
                        ImGui.Text("Volume: " + Stringify.Volume(_selectedEntity.GetDataBlob<MassVolumeDB>().Volume_m3 ));
                    }


                    ImGui.Text("Subject: " +  _SelectedEntityState.Name);
                    
                    //ImGui.Text(""+_uiState.LastClickedEntity.);
                    //gets all children and parent nodes, displays their names and makes them clickable to navigate towards them.

                    if(_selectedEntity.HasDataBlob<PositionDB>()){
                        ImGui.Text("Parent entity: ");

                        var _parentEntity = _selectedEntity.GetDataBlob<PositionDB>().Parent;
                        bool _hasParentEntity = false;
                        SystemState _StarSystemState = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
                        Dictionary<Guid, EntityState> _NamedEntityStates = _StarSystemState.EntityStatesWithNames;
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
                                    //if(ImGui.SmallButton(parentEntity.GetDataBlob<NameDB>().GetName(_uiState.Faction.ID))){
                                    _uiState.EntityClicked(_parentEntity.Guid, _uiState.SelectedStarSysGuid, MouseButtons.Primary);
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
                        foreach(var childEntity in _selectedEntity.GetDataBlob<PositionDB>().Children)
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
                                        _uiState.EntityClicked(childEntity.Guid, _uiState.SelectedStarSysGuid, MouseButtons.Primary);
                                    }
                                }

                            }
                        }
                        if(!hasChildrenEntities){
                            ImGui.Text("(...No children entities)");
                        }

                        if (_selectedEntity.HasDataBlob<ComponentInstancesDB>())
                        {
                            ComponentsDisplay.Display(_componentInstances);
                            ImGui.NewLine();
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

        public static class ComponentsDisplay
        {
            
            
            public static ComponentInstance[][] CreateNewInstanceArray(Entity selectedEntity)
            {
                var instancesDB = selectedEntity.GetDataBlob<ComponentInstancesDB>();
                if(instancesDB == null)
                    return new ComponentInstance[0][];
                var componentsDict = instancesDB.ComponentsByDesign;
                var instancesArray = new ComponentInstance[componentsDict.Count][];
                int i = 0;
                foreach (var desgnsLists in componentsDict.Values)
                {
                    instancesArray[i] = new ComponentInstance[desgnsLists.Count];
                    for (int j = 0; j < desgnsLists.Count; j++)
                    {
                        instancesArray[i][j] = desgnsLists[j];
                    }

                    i++;
                }

                return instancesArray;
            }


            public static void Display(ComponentInstance[][] instancesArray)
            {
                BorderGroup.Begin("Components:");
                ImGui.Columns(3);
                ImGui.SetColumnWidth(0, 164);
                ImGui.SetColumnWidth(1, 42);
                ImGui.SetColumnWidth(2, 42);
                for (int i = 0; i < instancesArray.Length; i++)
                {
                    for (int j = 0; j < instancesArray[i].Length; j++)
                    {
                        var instance = instancesArray[i][j];
                        string name = instance.Name;
                        float health = 100 * instance.HealthPercent();
                        
                        ImGui.Text(name); 
                        ImGui.NextColumn();
                        
                        ImGui.Text(health + "%%");
                        
                        ImGui.NextColumn();
                        if(instance.IsEnabled)
                            ImGui.Text("On");
                        else
                            ImGui.Text("Off");
                        ImGui.NextColumn();
                    }
                }
                ImGui.Columns(1);
                BorderGroup.End();
            }
        }
    }
}
