using System;
using ImGuiNET;
using Pulsar4X.ECSLib;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

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
           
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(264, 325), ImGuiCond.Once);
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
            
            /// <summary>
            /// returns a 2d array[i][j] where i is the component design, and j is the componentInstance 
            /// </summary>
            /// <param name="selectedEntity"></param>
            /// <returns></returns>
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

            private static int _selectedIndex = 0;
            private static float[] _textwidth = new float[2];
            public static void DisplayComplex(ComponentInstance[][] instancesArray)
            {
                List<string> names = new List<string>();
                List<ComponentInstance> flatInstances = new List<ComponentInstance>();
                List<IComponentDesignAttribute> attrubutes = new List<IComponentDesignAttribute>();
                float xwid = 100;
                int c = 0;
                for (int i = 0; i < instancesArray.Length; i++)
                {
                    for (int j = 0; j < instancesArray[i].Length; j++)
                    {
                        var instance = instancesArray[i][j];
                        string name = instance.Name;
                        float health = 100 * instance.HealthPercent();
                        names.Add(name);
                        flatInstances.Add(instance);
                        foreach (var atb in instance.GetAttributes().Values)
                        {
                            
                        }

                        c++;
                    }
                }
                
                
                //BorderListOptions.Begin("Components", names.ToArray(), ref _selectedIndex, 256);
                //left selectable box
                if (BorderListOptions.Begin("Components", names.ToArray(), ref _selectedIndex, 184))
                {
                    _textwidth = new float[2];
                    attrubutes = new List<IComponentDesignAttribute>();
                }

                
                
                Dictionary<Type, ComponentAbilityState> states = flatInstances[_selectedIndex].GetAllStates();
                foreach (var state in states.Values)
                {
                    ImGui.Text(state.Name);
                }
                
                foreach (var atb in flatInstances[_selectedIndex].GetAttributes().Values)
                {
                    attrubutes.Add(atb);
                }
                
                

                for (int atbi = 0; atbi < attrubutes.Count; atbi++)
                {
                    var atbName = attrubutes[atbi].AtbName();
                    ImGui.Text(atbName);
                    
                    string[] atbAry = attrubutes[atbi].AtbDescription().Split("\n");
                    
                    //we're assuming the first item is a title of sorts
                    var strline = atbAry[0];
                    ImGui.Text(strline);
                    if(xwid < ImGui.GetItemRectSize().X)
                        xwid = ImGui.GetItemRectSize().X;
                    
                    for (int strlinei = 1; strlinei < atbAry.Length; strlinei++)
                    {
                        strline = atbAry[strlinei];
                        
                        string[] tabSplit = strline.Split("\t"); //split if there are tabs.
                        var xpos = ImGui.GetCursorPosX();
                        for (int i = 0; i < tabSplit.Length; i++) 
                        {
                            if(i > 0) //after the tab char
                                ImGui.SetCursorPosX(xpos + _textwidth[i-1] + 12);//allign second row
                        
                            ImGui.Text(tabSplit[i]); //display the text
                            if (_textwidth[i] < ImGui.GetItemRectSize().X) //check the size
                                _textwidth[i] = ImGui.GetItemRectSize().X; //expand the size for the next frame
                            if (i < tabSplit.Length - 1) //put the next item on the same line if there is another item in the array
                                ImGui.SameLine(); //at least this bit shouldnt break if there's more than one tab
                        }
                        
                    }
                    
                    
                    //ImGui.Text(attrubutes[i].AtbDescription());
                }
                

                float ycount = flatInstances.Count;
                float yhight = ImGui.GetTextLineHeightWithSpacing() * ycount;
                
                //float ycount = abilites[_selectedIndex].AbilityDescription().Split("\n").Length -1;
                //float yhight = ImGui.GetTextLineHeightWithSpacing() * ycount;
                if(xwid < _textwidth[0] + _textwidth[1] + 12)
                    xwid = _textwidth[0] + _textwidth[1] + 12;
                ImGui.Columns(2);
                BorderListOptions.End(new Vector2(xwid,yhight));
                
                //BorderListOptions.End(new Vector2(184,yhight));
                
            }

        }

        public static class AbilitesDisplay
        {
            private static int _selectedIndex = 0;
            private static Entity _entity;
            private static float[] _textwidth = new float[2];
            
            
            public static void Display(Entity entity)
            {
                float xwid = 100;
                List<string> names = new List<string>();
                List<IAbilityDescription> abilites = new List<IAbilityDescription>();
                if (_entity != entity)
                {
                    _selectedIndex = 0;
                    _entity = entity;
                    _textwidth = new float[2];
                }

                foreach (var db in entity.DataBlobs)
                {
                    if (db is IAbilityDescription)
                    {
                        IAbilityDescription dbdesc = (IAbilityDescription)db;
                        names.Add(dbdesc.AbilityName());
                        abilites.Add(dbdesc);
                    }
                }  
                
                //left selectable box
                if (BorderListOptions.Begin("Abilites", names.ToArray(), ref _selectedIndex, 184))
                {
                    _textwidth = new float[2];
                }



                //right box of sub items
                string[] abilitiesAry = abilites[_selectedIndex].AbilityDescription().Split("\n");
                
                //we're assuming the first item is a title of sorts
                var strline = abilitiesAry[0];
                ImGui.Text(strline);
                if(xwid < ImGui.GetItemRectSize().X)
                    xwid = ImGui.GetItemRectSize().X;
                
                
                //we're expecting subsequent items to have a(one) tab char, and we want to allign the righthand side
                //this whole bit of code could break in intersting ways if there's more than one tab char,
                //however the strings are hardcode, so we'll ignore that problem untill it happens.
                for (int strlinei = 1; strlinei < abilitiesAry.Length; strlinei++)
                {
                    strline = abilitiesAry[strlinei];
                    if(strlinei > 0)
                    {
                        string[] tabSplit = strline.Split("\t"); //split if there are tabs.
                        var xpos = ImGui.GetCursorPosX();
                        for (int i = 0; i < tabSplit.Length; i++) 
                        {
                            if(i > 0) //after the tab char
                                ImGui.SetCursorPosX(xpos + _textwidth[i-1] + 12);//allign second row
                            
                            ImGui.Text(tabSplit[i]); //display the text
                            if (_textwidth[i] < ImGui.GetItemRectSize().X) //check the size
                                _textwidth[i] = ImGui.GetItemRectSize().X; //expand the size for the next frame
                            if (i < tabSplit.Length - 1) //put the next item on the same line if there is another item in the array
                                ImGui.SameLine(); //at least this bit shouldnt break if there's more than one tab
                        }
                    }
                }
                
                float ycount = abilites[_selectedIndex].AbilityDescription().Split("\n").Length -1;
                float yhight = ImGui.GetTextLineHeightWithSpacing() * ycount;
                if(xwid < _textwidth[0] + _textwidth[1] + 12)
                   xwid = _textwidth[0] + _textwidth[1] + 12;
                ImGui.Columns(2);
                BorderListOptions.End(new Vector2(xwid,yhight));
            }
        }
    }
}
