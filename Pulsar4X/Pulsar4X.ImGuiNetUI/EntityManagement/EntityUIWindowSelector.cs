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

                    void NewButton(Type T,  string PictureString, string TooltipText, List<ToolbuttonData> ButtonList) {
                        if (EntityUIWindows.checkIfCanOpenWindow(T, _entityState))
                        {
                            btn = new ToolbuttonData()
                            {   
                                Picture = _state.SDLImageDictionary[PictureString],
                                TooltipText = TooltipText,
                                ClickType = T
                                //Opens up the componet design menu
                            };
                            ButtonList.Add(btn);
                        }
                    }
                    void NewCondtionalButton(Type T, string PictureString, string TooltipText) {
                        NewButton(T, PictureString, TooltipText, CondtionalButtons);
                    }
                    void NewStandardButton(Type T, string PictureString, string TooltipText) {
                        NewButton(T, PictureString, TooltipText, StandardButtons);
                    }


                    NewStandardButton(typeof(SelectPrimaryBlankMenuHelper), "Select", "Selects the entity");
                    NewStandardButton(typeof(PinCameraBlankMenuHelper), "Pin", "Focuses camera");
                    NewStandardButton(typeof(RenameWindow), "Rename", "Renames the entity");

                    NewCondtionalButton(typeof(PowerGen), "Power", "Shows power stats");
                    NewCondtionalButton(typeof(CargoTransfer), "Cargo", "Shows cargo");
                    NewCondtionalButton(typeof(ColonyPanel), "Industry", "Opens Industry menu");
                    NewCondtionalButton(typeof(WeaponTargetingControl), "Firecon", "Opens firecontrol menu");
                   
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

                    CondtionalButtons = new List<ToolbuttonData>();
                    StandardButtons = new List<ToolbuttonData>();

                    void ActionButton(Type T)
                    {
                        if (EntityUIWindows.checkIfCanOpenWindow(T,_entityState))
                        {
                            bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[T]);
                            EntityUIWindows.openUIWindow(T, _entityState, _state, buttonresult);
                            if (ImGui.IsItemHovered())
                                ImGui.SetTooltip(GlobalUIState.namesForMenus[T]);
                        }
                    }

                    ActionButton(typeof(GotoSystemBlankMenuHelper));
                    ActionButton(typeof(OrbitOrderWindow));
                    ActionButton(typeof(ChangeCurrentOrbitWindow));
                    ActionButton(typeof(PlanetaryWindow));


                }
                ImGui.End();
            }
        }
    }
}
