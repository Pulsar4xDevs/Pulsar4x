using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace Pulsar4X.SDL2UI
{
    public class ToolBarUI : PulsarGuiWindow
    {
        public struct ToolbuttonData
        {
            public IntPtr Picture;
            public string TooltipText;
            public Action OnClick;
        }

        private ToolBarUI()
        {
            _flags = ImGuiWindowFlags.NoResize;
            ToolbuttonData btn =  new ToolbuttonData()
            {
                Picture = _state.SDLImageDictionary["DesComp"],
                TooltipText = "Design a new component or facility",
                OnClick = new Action(ComponentDesignUI.GetInstance().SetActive)
                
            };
            ToolButtons.Add(btn);
            btn =  new ToolbuttonData()
            {
                Picture = _state.SDLImageDictionary["DesShip"],
                TooltipText = "Design a new Ship",
                OnClick = new Action(ShipDesignUI.GetInstance().SetActive)
                
            };
            ToolButtons.Add(btn);
            
            
        }

        internal static ToolBarUI GetInstance()
        {
            if (!PulsarGuiWindow._state.LoadedWindows.ContainsKey(typeof(ToolBarUI)))
            {
                return new ToolBarUI();
            }
            return (ToolBarUI)PulsarGuiWindow._state.LoadedWindows[typeof(ToolBarUI)];
        }
        
        public Vector2 BtnSizes = new Vector2(32, 32);
        private List<ToolbuttonData> ToolButtons = new List<ToolbuttonData>();


        internal void SetButtons(List<ToolbuttonData> buttons)
        {
            ToolButtons = buttons;
        }

        internal override void Display()
        {
            if (ImGui.Begin("##Toolbar", _flags))
            {
                foreach (var button in ToolButtons)
                {
                    
                    if(ImGui.ImageButton(button.Picture, BtnSizes))
                        button.OnClick();
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip(button.TooltipText);
                }
                
                ImGui.End();
            }

        }
    }
}