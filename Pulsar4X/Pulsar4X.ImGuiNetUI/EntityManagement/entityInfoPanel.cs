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
            if (IsActive && ImGui.Begin("Currently selected", ref IsActive, _flags))
            {
                if(_state.LastClickedEntity != null){
                    ImGui.Text("name: "+_state.LastClickedEntity.Name);
                    //ImGui.Text(""+_state.LastClickedEntity.);
                } 
            }
        }
    }
}
