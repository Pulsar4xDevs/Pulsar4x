using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class ShipDesignUI : PulsarGuiWindow
    {
        private ComponentDesign[] _componentDesigns;
        private string[] _componentNames;
        private int _selectedComponent;
        
        Dictionary<ComponentDesign, int> ShipDesign = new Dictionary<ComponentDesign, int>(); 
        
        private ShipDesignUI()
        {
            _flags = ImGuiWindowFlags.NoCollapse;
            _componentDesigns = _state.Faction.GetDataBlob<FactionInfoDB>().ComponentDesigns.Values.ToArray();
            _componentNames = new string[_componentDesigns.Length];
            for (int i = 0; i < _componentDesigns.Length; i++)
            {
                _componentNames[i] = _componentDesigns[i].Name;
            }
        }

        internal static ShipDesignUI GetInstance()
        {
            ShipDesignUI thisitem;
            if (!_state.LoadedWindows.ContainsKey(typeof(ShipDesignUI)))
            {
                thisitem = new ShipDesignUI();
            }
            thisitem = (ShipDesignUI)_state.LoadedWindows[typeof(ShipDesignUI)];


            return thisitem;
        }


        internal override void Display()
        {
            if (IsActive && ImGui.Begin("Ship Design", ref IsActive, _flags))
            {
                ImGui.ListBox("Components", ref _selectedComponent, _componentNames, _componentNames.Length);

                var selectedComponent = _componentDesigns[_selectedComponent];
                
                ImGui.Text(selectedComponent.Name);
                ImGui.Text(selectedComponent.Description);

                if (ImGui.Button("Add"))
                {
                    if (!ShipDesign.ContainsKey(selectedComponent))
                        ShipDesign.Add(selectedComponent, 1);
                    else
                        ShipDesign[selectedComponent]++;
                }
                
                
                


            }

        }
    }
}