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

                if (_state.StarSystemStates.ContainsKey(_state.SelectedStarSysGuid)){

                    SystemState _StarSystemState = _state.StarSystemStates[_state.SelectedStarSysGuid];
                    Dictionary<System.Guid, EntityState> _NamedEntityStates = _StarSystemState.EntityStatesWithNames;

                    List<EntityState> _Stars = new List<EntityState>();

                    foreach (KeyValuePair<System.Guid, EntityState> Body in _NamedEntityStates)
                    {
                        if (Body.Value.IsStar())
                            TreeGen(Body.Value.Entity);

                    }                  

                }

            }

            
        }

        void TreeGen(Entity _CurrentBody)
        {
            SystemState _StarSystemState = _state.StarSystemStates[_state.SelectedStarSysGuid];
            Dictionary<System.Guid, EntityState> _NamedEntityStates = _StarSystemState.EntityStatesWithNames;
            if (_NamedEntityStates.ContainsKey(_CurrentBody.Guid))
            {

                var _ChildList = _CurrentBody.GetDataBlob<PositionDB>().Children;

                if (_ChildList.Count > 0)
                {
                    if (ImGui.TreeNodeEx(_NamedEntityStates[_CurrentBody.Guid].Name))
                    {
                        foreach (Entity _ChildBody in _ChildList)
                            TreeGen(_ChildBody);
                        ImGui.TreePop();
                    }
                }
                else
                {
                    if (ImGui.SmallButton(_NamedEntityStates[_CurrentBody.Guid].Name))
                        _state.EntityClicked(_CurrentBody.Guid, _state.SelectedStarSysGuid, MouseButtons.Primary);

                }

            }
        }
    }
}
