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
            _flags =  ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize;

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
                    //Gets the last clicked entity
                    var _entityState = _state.LastClickedEntity;

                    ToolbuttonData btn;

                    void NewButton(Type T,  string PictureString, string TooltipText, List<ToolbuttonData> ButtonList) {
                        //Creates a buttton if it is usuable in this situation
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

                    //Populates Buttons

                    NewStandardButton(typeof(SelectPrimaryBlankMenuHelper), "Select", "Selects the entity");
                    NewStandardButton(typeof(PinCameraBlankMenuHelper), "Pin", "Focuses camera");
                    NewStandardButton(typeof(RenameWindow), "Rename", "Renames the entity");

                    NewCondtionalButton(typeof(PowerGen), "Power", "Shows power stats");
                    NewCondtionalButton(typeof(CargoTransfer), "Cargo", "Shows cargo");
                    NewCondtionalButton(typeof(ColonyPanel), "Industry", "Opens Industry menu");
                    NewCondtionalButton(typeof(WeaponTargetingControl), "Firecon", "Opens firecontrol menu");
                   
                    //Displays all buttons in a list
                    void PrintButtonList (ref List<ToolbuttonData> PrintButtons) {
                        uint iterations = 0;
                        uint unclickedcolor;
                        uint clickedcolour;
                        ImGuiCol buttonidx = ImGuiCol.Button;
                        unsafe
                        {
                            Vector4* unclickedcolorv = ImGui.GetStyleColorVec4(ImGuiCol.Button);
                            Vector4* clickedcolorv = ImGui.GetStyleColorVec4(ImGuiCol.ButtonActive);
                            unclickedcolor = ImGui.ColorConvertFloat4ToU32(*unclickedcolorv);
                            clickedcolour = ImGui.ColorConvertFloat4ToU32(*clickedcolorv);
                        }
                        foreach (var button in PrintButtons)
                        {
                            ImGui.SameLine();
                            ImGui.PushID(iterations.ToString());
                            if (EntityUIWindows.checkopenUIWindow(button.ClickType, _entityState, _state))//If the window is open
                            {
                                ImGui.PushStyleColor(buttonidx, clickedcolour);//Have the button be "pressed"
                            }
                            else//If closed
                            {
                                ImGui.PushStyleColor(buttonidx, unclickedcolor);//Have the button be colored normally
                            }
                            if (ImGui.ImageButton(button.Picture, BtnSizes))
                            {
                                EntityUIWindows.openUIWindow(button.ClickType, _entityState, _state);
                            }

                            if (ImGui.IsItemHovered())
                                ImGui.SetTooltip(button.TooltipText);

                            ImGui.PopID();
                            iterations++;
                        }
                        
                        ImGui.NewLine();
                        ImGui.PushStyleColor(buttonidx, unclickedcolor);//Have the button be colored normally
                        PrintButtons = new List<ToolbuttonData>();
                    }
                    
                    //Prints both button lists
                    PrintButtonList(ref StandardButtons);
                    PrintButtonList(ref CondtionalButtons);

                    void ActionButton(Type T)
                    {
                    //Makes a small button if it is usable in this situation
                        if (EntityUIWindows.checkIfCanOpenWindow(T,_entityState))
                        {
                            bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[T]);
                            EntityUIWindows.openUIWindow(T, _entityState, _state, buttonresult);
                            if (ImGui.IsItemHovered())
                                ImGui.SetTooltip(GlobalUIState.namesForMenus[T]);
                        }
                    }

                    //Makes all small buttons
                    ActionButton(typeof(PlanetaryWindow));
                    ActionButton(typeof(GotoSystemBlankMenuHelper));
                    ActionButton(typeof(WarpOrderWindow));
                    ActionButton(typeof(ChangeCurrentOrbitWindow));



                }
                ImGui.End();
            }
        }
    }
}
