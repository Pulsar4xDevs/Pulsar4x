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
	        _flags = ImGuiWindowFlags.AlwaysAutoResize;
        
        }

        internal static SystemTreeViewer GetInstance() {

            SystemTreeViewer thisItem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(SystemTreeViewer)))
            {
                thisItem = new SystemTreeViewer();
            }
            else
            {
                thisItem = (SystemTreeViewer)_uiState.LoadedWindows[typeof(SystemTreeViewer)];
            }
             

            return thisItem;


        }
        //displays selected entity info
        internal override void Display()
        {
           
            if (IsActive && ImGui.Begin("System Tree", _flags))
            {

                if (_uiState.StarSystemStates.ContainsKey(_uiState.SelectedStarSysGuid)){

                    SystemState _StarSystemState = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
                    Dictionary<System.Guid, EntityState> _NamedEntityStates = _StarSystemState.EntityStatesWithNames;

                    List<EntityState> _Stars = new List<EntityState>();

                    foreach (KeyValuePair<System.Guid, EntityState> Body in _NamedEntityStates)
                    {
                        if (Body.Value.IsStar()) 
                        { 
                            if(_uiState.LastClickedEntity != null)
                            {
                                TreeGen(Body.Value.Entity, _uiState.LastClickedEntity.Entity);
                            }
                            else
                            {
                                TreeGen(Body.Value.Entity, Body.Value.Entity);
                            }
                        }
                            

                    }                  

                }

            }

            
        }

        void TreeGen(Entity _CurrentBody, Entity _SelectedBody)
        {
            SystemState _StarSystemState = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
            Dictionary<System.Guid, EntityState> _NamedEntityStates = _StarSystemState.EntityStatesWithNames;
            if (_NamedEntityStates.ContainsKey(_CurrentBody.Guid))
            {

                var _ChildList = _CurrentBody.GetDataBlob<PositionDB>().Children;

                if (_ChildList.Count > 0)
                {
                    ImGuiTreeNodeFlags _TreeFlags;
                    if (_CurrentBody == _SelectedBody)
                        _TreeFlags = ImGuiTreeNodeFlags.Selected | ImGuiTreeNodeFlags.OpenOnArrow;
                    else
                        _TreeFlags = ImGuiTreeNodeFlags.OpenOnArrow;
                    bool _Opened = ImGui.TreeNodeEx(_NamedEntityStates[_CurrentBody.Guid].Name, _TreeFlags);
                    if(ImGui.IsItemClicked())
                        _uiState.EntityClicked(_CurrentBody.Guid, _uiState.SelectedStarSysGuid, MouseButtons.Primary);
                    if (_Opened)
                    {   
                        foreach (Entity _ChildBody in _ChildList)
                            TreeGen(_ChildBody, _SelectedBody);
                        ImGui.TreePop();
                    }


                }
                else
                {
                    bool selected = _CurrentBody == _SelectedBody;                   
                    if (ImGui.Selectable(_NamedEntityStates[_CurrentBody.Guid].Name, selected))
                        _uiState.EntityClicked(_CurrentBody.Guid, _uiState.SelectedStarSysGuid, MouseButtons.Primary);
                }

            }
        }
    }
}
