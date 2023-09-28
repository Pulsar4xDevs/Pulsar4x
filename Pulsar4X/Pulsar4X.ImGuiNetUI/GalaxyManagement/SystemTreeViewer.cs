using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

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
                if (_uiState.StarSystemStates.ContainsKey(_uiState.SelectedStarSysGuid))
                {
                    SystemState _StarSystemState = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
                    List<EntityState> _NamedEntityStates = _StarSystemState.EntityStatesWithPosition.Values.OrderBy(x => x.Position.AbsolutePosition).ToList();
                    List<EntityState> _Stars = new List<EntityState>();

                    foreach (EntityState Body in _NamedEntityStates)
                    {
                        if (Body.IsStar())
                        {
                            if (_uiState.LastClickedEntity != null)
                            {
                                TreeGen(Body.Entity, _uiState.LastClickedEntity.Entity);
                            }
                            else
                            {
                                TreeGen(Body.Entity, Body.Entity);
                            }
                        }
                    }
                }
            }
        }

        void TreeGen(Entity _CurrentBody, Entity _SelectedBody)
        {
            SystemState _StarSystemState = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
            var _NamedEntityStates = _StarSystemState.EntityStatesWithPosition;

            if (_NamedEntityStates.ContainsKey(_CurrentBody.Guid))
            {
                var _ChildList = _CurrentBody.GetDataBlob<PositionDB>().Children;

                if (_ChildList.Count > 0)
                {
                    ImGuiTreeNodeFlags _TreeFlags = ImGuiTreeNodeFlags.OpenOnArrow;
                    if (_CurrentBody == _SelectedBody)
                    {
                        _TreeFlags = ImGuiTreeNodeFlags.Selected | _TreeFlags;
                    }

                    bool _Opened = ImGui.TreeNodeEx(_NamedEntityStates[_CurrentBody.Guid].Name, _TreeFlags);

                    if (ImGui.IsItemClicked())
                    {
                        _uiState.EntityClicked(_CurrentBody.Guid, _uiState.SelectedStarSysGuid, MouseButtons.Primary);
                    }

                    if (_Opened)
                    {
                        _ChildList = _ChildList.OrderBy(x => x.GetDataBlob<PositionDB>().AbsolutePosition).ToList();
                        foreach (Entity _ChildBody in _ChildList)
                        {
                            TreeGen(_ChildBody, _SelectedBody);
                        }
                        ImGui.TreePop();
                    }
                }
                else
                {
                    if (ImGui.Selectable(_NamedEntityStates[_CurrentBody.Guid].Name, _CurrentBody == _SelectedBody))
                    {
                        _uiState.EntityClicked(_CurrentBody.Guid, _uiState.SelectedStarSysGuid, MouseButtons.Primary);
                    }
                }

            }
        }
    }
}
