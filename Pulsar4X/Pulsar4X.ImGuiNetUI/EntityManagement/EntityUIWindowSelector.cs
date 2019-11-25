using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.ImGuiNetUI.EntityManagement;

namespace Pulsar4X.SDL2UI
{
    //basically an always open context menu for the currently selected entity.
    public class EntityUIWindowSelector : PulsarGuiWindow
    {

        public Vector2 BtnSizes = new Vector2(32, 32);
        private List<ToolbuttonData> StandardButtons = new List<ToolbuttonData>();
        private List<ToolbuttonData> CondtionalButtons = new List<ToolbuttonData>();
        //data for a toolbar button, requires an SDL image(for Picture), a PulsarGuiWindow`s SetActive function or equivalent/similar(for OnClick) and
        //the tool tip text to be displayed when the button is hovered(for TooltipText)
        public struct ToolbuttonData
        {
            public IntPtr Picture;
            public string TooltipText;
            public Action OnClick;
            public Type ClickType;
            
        }
        private EntityUIWindowSelector()
        {
            _flags =  ImGuiWindowFlags.NoCollapse;

        }

        internal static EntityUIWindowSelector GetInstance()
        {

            EntityUIWindowSelector thisItem;
            if (!_state.LoadedWindows.ContainsKey(typeof(EntityUIWindowSelector)))
            {
                thisItem = new EntityUIWindowSelector();
            }
            else
            {
                thisItem = (EntityUIWindowSelector)_state.LoadedWindows[typeof(EntityUIWindowSelector)];
            }


            return thisItem;


        }
        //displays selected entity info
        internal override void Display()
        {
            ImGui.SetNextWindowSize(new Vector2(150, 200), ImGuiCond.Once);
            if (ImGui.Begin("Actions", _flags))
            {
                //check if ANY entity has been clicked
                //if true, display all possible toolbar menu icons for it
                if (_state.LastClickedEntity != null)
                {
                    var _entityState = _state.LastClickedEntity;

                    ToolbuttonData btn;

                    if (EntityUIWindows.checkIfCanOpenWindow<SelectPrimaryBlankMenuHelper>(_entityState))
                    {
                        btn = new ToolbuttonData()
                        {
                            Picture = _state.SDLImageDictionary["Select"],
                            TooltipText = "Selects the entity",
                            ClickType = typeof(SelectPrimaryBlankMenuHelper)
                            //Opens up the componet design menu
                        };
                        StandardButtons.Add(btn);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<PinCameraBlankMenuHelper>(_entityState))
                    {
                        btn = new ToolbuttonData()
                        {
                            Picture = _state.SDLImageDictionary["Pin"],
                            TooltipText = "Focuses camera",
                            ClickType = typeof(PinCameraBlankMenuHelper)
                            //Opens up the componet design menu
                        };
                        StandardButtons.Add(btn);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<RenameWindow>(_entityState))
                    {
                        btn = new ToolbuttonData()
                        {
                            Picture = _state.SDLImageDictionary["Rename"],
                            TooltipText = "Renames the entity",
                            ClickType = typeof(RenameWindow)
                            //Opens up the componet design menu
                        };
                        StandardButtons.Add(btn);
                    }

                    if (EntityUIWindows.checkIfCanOpenWindow<PowerGen>(_entityState))
                    {
                        btn = new ToolbuttonData()
                        {
                            Picture = _state.SDLImageDictionary["Power"],
                            TooltipText = "Shows power stats",
                            ClickType = typeof(PowerGen)
                            //Opens the power menu if the player has a body with power selected
                        };
                        CondtionalButtons.Add(btn);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<CargoTransfer>(_entityState))
                    {
                        btn = new ToolbuttonData()
                        {
                            Picture = _state.SDLImageDictionary["Cargo"],
                            TooltipText = "Shows cargo",
                            ClickType = typeof(CargoTransfer)
                            //Opens the power menu if the player has a body with power selected
                        };
                        CondtionalButtons.Add(btn);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<ColonyPanel>(_entityState))
                    {
                        btn = new ToolbuttonData()
                        {
                            Picture = _state.SDLImageDictionary["Industry"],
                            TooltipText = "Opens Industry menu",
                            ClickType = typeof(ColonyPanel)
                            //Opens the power menu if the player has a body with power selected
                        };
                        CondtionalButtons.Add(btn);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<WeaponTargetingControl>(_entityState))
                    {
                        btn = new ToolbuttonData()
                        {
                            Picture = _state.SDLImageDictionary["Firecon"],
                            TooltipText = "Opens firecontrol menu",
                            ClickType = typeof(WeaponTargetingControl)
                            //Opens the power menu if the player has a body with power selected
                        };
                        CondtionalButtons.Add(btn);
                    }


                    //displays the default toolbar menu icons
                    uint iterations = 0;

                    foreach (var button in StandardButtons)
                    {
                        ImGui.SameLine();
                        ImGui.PushID(iterations.ToString());
                        if (ImGui.ImageButton(button.Picture, BtnSizes))
                            EntityUIWindows.openUIWindow(button.ClickType ,_entityState, _state);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(button.TooltipText);


                        ImGui.PopID();
                        iterations++;
                    }

                    ImGui.NewLine();
                    iterations = 0;

                    foreach (var button in CondtionalButtons)
                    {
                        ImGui.SameLine();
                        ImGui.PushID(iterations.ToString());
                        if (ImGui.ImageButton(button.Picture, BtnSizes))
                            EntityUIWindows.openUIWindow(button.ClickType, _entityState, _state);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(button.TooltipText);


                        ImGui.PopID();
                        iterations++; 
                    }

                    ImGui.NewLine();
                    CondtionalButtons = new List<ToolbuttonData>();
                    StandardButtons = new List<ToolbuttonData>();

                    if (EntityUIWindows.checkIfCanOpenWindow<PinCameraBlankMenuHelper>(_entityState))
                    {
                        bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(GotoSystemBlankMenuHelper)]);
                        EntityUIWindows.openUIWindow(typeof(PinCameraBlankMenuHelper), _entityState, _state, buttonresult);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(GlobalUIState.namesForMenus[typeof(PinCameraBlankMenuHelper)]);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<GotoSystemBlankMenuHelper>(_entityState))
                    {
                        bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(GotoSystemBlankMenuHelper)]);
                        EntityUIWindows.openUIWindow(typeof(GotoSystemBlankMenuHelper), _entityState, _state, buttonresult);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(GlobalUIState.namesForMenus[typeof(GotoSystemBlankMenuHelper)]);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<SelectPrimaryBlankMenuHelper>(_entityState))
                    {
                        bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(SelectPrimaryBlankMenuHelper)]);
                        EntityUIWindows.openUIWindow(typeof(SelectPrimaryBlankMenuHelper), _entityState, _state, buttonresult);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(GlobalUIState.namesForMenus[typeof(SelectPrimaryBlankMenuHelper)]);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<OrbitOrderWindow>(_entityState))
                    {
                        bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(OrbitOrderWindow)]);
                        EntityUIWindows.openUIWindow(typeof(OrbitOrderWindow), _entityState, _state, buttonresult);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(GlobalUIState.namesForMenus[typeof(OrbitOrderWindow)]);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<ChangeCurrentOrbitWindow>(_entityState))
                    {
                        bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(ChangeCurrentOrbitWindow)]);
                        EntityUIWindows.openUIWindow(typeof(ChangeCurrentOrbitWindow), _entityState, _state, buttonresult);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(GlobalUIState.namesForMenus[typeof(ChangeCurrentOrbitWindow)]);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<WeaponTargetingControl>(_entityState))
                    {
                        bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(WeaponTargetingControl)]);
                        EntityUIWindows.openUIWindow(typeof(WeaponTargetingControl), _entityState, _state, buttonresult);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(GlobalUIState.namesForMenus[typeof(WeaponTargetingControl)]);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<RenameWindow>(_entityState))
                    {
                        bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(RenameWindow)]);
                        EntityUIWindows.openUIWindow(typeof(RenameWindow), _entityState, _state, buttonresult);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(GlobalUIState.namesForMenus[typeof(RenameWindow)]);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<CargoTransfer>(_entityState))
                    {
                        bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(CargoTransfer)]);
                        EntityUIWindows.openUIWindow(typeof(CargoTransfer), _entityState, _state, buttonresult);
                        ImGui.SetTooltip(GlobalUIState.namesForMenus[typeof(CargoTransfer)]);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<ColonyPanel>(_entityState))
                    {
                        bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(ColonyPanel)]);
                        EntityUIWindows.openUIWindow(typeof(ColonyPanel), _entityState, _state, buttonresult);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(GlobalUIState.namesForMenus[typeof(ColonyPanel)]);
                    }

                }
                ImGui.End();
            }
        }
    }
}
