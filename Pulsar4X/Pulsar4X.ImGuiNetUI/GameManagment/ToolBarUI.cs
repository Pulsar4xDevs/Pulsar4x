using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.ComponentFeatureSets.Missiles;
using Pulsar4X.ImGuiNetUI.EntityManagement;

namespace Pulsar4X.SDL2UI
{
    public class ToolBarUI : PulsarGuiWindow
    {
        private float _btnSize = 32;                                                //Button size
        public Vector2 BtnSizes = new Vector2(32, 32);                              //Button size
        private List<ToolbuttonData> ToolButtons = new List<ToolbuttonData>();      //Stores the data for each button
        private List<ToolbuttonData> SMToolButtons = new List<ToolbuttonData>();    //Stores the data for each button

        public class ToolbuttonData
        //data for a toolbar button, requires an SDL image(for Picture)
        {
            public IntPtr Picture;          //Requires an SDL image(for Picture)
            public string TooltipText;      //Tooltip text for the button
            public Action OnClick;          //A PulsarGuiWindow`s SetActive function to opens window (or similar function)

            //Checks if window is open
            //Does not need to be intialized
            public Func<bool> GetActive;
            public bool SmButton = false;
        }

        //constructs the toolbar with the given buttons
        private ToolBarUI()
        {
            _flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize;

            ToolbuttonData btn = new ToolbuttonData()
            {
                Picture = _uiState.Img_DesComponent(),
                TooltipText = "Design a new component or facility",
                OnClick = new Action(ComponentDesignUI.GetInstance().ToggleActive),
                GetActive = new Func<bool>(ComponentDesignUI.GetInstance().GetActive)
                //Opens up the component design menu
            };
            ToolButtons.Add(btn);

            btn =  new ToolbuttonData()
            {
                Picture = _uiState.Img_DesignShip(),
                TooltipText = "Design a new Ship",
                OnClick = new Action(ShipDesignUI.GetInstance().ToggleActive),
                GetActive = new Func<bool>(ShipDesignUI.GetInstance().GetActive)
                //Opens up the ship design menu
            };
            ToolButtons.Add(btn);

            btn =  new ToolbuttonData()
            {
                Picture = _uiState.Img_Research(),
                TooltipText = "Research",
                OnClick = new Action(ResearchWindow.GetInstance().ToggleActive),
                GetActive = new Func<bool>(ResearchWindow.GetInstance().GetActive)
                //Opens up the research menu
            };
            ToolButtons.Add(btn);

            btn = new ToolbuttonData()
            {
                Picture = _uiState.Img_GalaxyMap(),
                TooltipText = "Galaxy Browser",
                OnClick = new Action(GalaxyWindow.GetInstance().ToggleActive),
                GetActive = new Func<bool>(GalaxyWindow.GetInstance().GetActive)

            };
            ToolButtons.Add(btn);

            btn = new ToolbuttonData()
            {
                Picture = _uiState.Img_Ruler(),
                TooltipText = "Measure distance",
                OnClick = new Action(DistanceRuler.GetInstance().ToggleActive),
                GetActive = new Func<bool>(DistanceRuler.GetInstance().GetActive)
                //Opens the ruler menu
            };
            ToolButtons.Add(btn);

            btn = new ToolbuttonData()
            {
                Picture = _uiState.Img_Tree(),
                TooltipText = "View objects in the system",
                OnClick = new Action(SystemTreeViewer.GetInstance().ToggleActive),
                GetActive = new Func<bool>(SystemTreeViewer.GetInstance().GetActive)
                //Display a tree with all objects in the system
            };
            ToolButtons.Add(btn);

            btn = new ToolbuttonData()
            {
                Picture = _uiState.Img_Tree(),
                TooltipText = "Design orders and assign to entities",
                OnClick = new Action(OrderCreationUI.GetInstance().ToggleActive),
                GetActive = new Func<bool>(OrderCreationUI.GetInstance().GetActive)
                //Design orders for OrderableDB entities
            };
            ToolButtons.Add(btn);

            btn = new ToolbuttonData()
            {
                Picture = _uiState.Img_Tree(),
                TooltipText = "Spawn ships and planets",
                OnClick = new Action(EntitySpawnWindow.GetInstance().ToggleActive),
                GetActive = new Func<bool>(EntitySpawnWindow.GetInstance().GetActive),
                //Display a tree with all objects in the system
            };
            SMToolButtons.Add(btn);

            btn = new ToolbuttonData()
            {
                Picture = _uiState.Img_Tree(),
                TooltipText = "View SM debug info about a body",
                OnClick = new Action(SMPannel.GetInstance().ToggleActive),
                GetActive = new Func<bool>(SMPannel.GetInstance().GetActive),
                //Display a list of bodies with some info about them.
            };
            SMToolButtons.Add(btn);
        }

        internal static ToolBarUI GetInstance()
        {
            if (!PulsarGuiWindow._uiState.LoadedWindows.ContainsKey(typeof(ToolBarUI)))
            {
                return new ToolBarUI();
            }

            return (ToolBarUI)PulsarGuiWindow._uiState.LoadedWindows[typeof(ToolBarUI)];
        }

        internal void SetButtons(List<ToolbuttonData> buttons)
        {
            ToolButtons = buttons;
        }

        internal override void Display()
        {
            DisplayButtons("##Toolbar", ToolButtons);
            if (_uiState.SMenabled)
            {
                DisplayButtons("##SMToolbar", SMToolButtons);
            }
        }

        void DisplayButtons(string name, List<ToolbuttonData> DisplayToolButtons)
        {
            if (ImGui.Begin(name, _flags))
            {
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
                //Store the colors for pressed and unpressed buttons

                uint iterations = 0;
                //displays the default toolbar menu icons
                foreach (var button in DisplayToolButtons)//For each button
                {
                    string id = iterations.ToString();
                    ImGui.PushID(id);

                    if (button.GetActive != null)//If the windows state can be checked
                    {
                        if (button.GetActive())//If the window is open
                        {
                            ImGui.PushStyleColor(buttonidx, clickedcolour);//Have the button be "pressed"
                        }
                        else//If closed
                        {
                            ImGui.PushStyleColor(buttonidx, unclickedcolor);//Have the button be colored normally
                        }
                    }

                    if (ImGui.ImageButton(button.Picture, BtnSizes))//Make the button
                    {
                        button.OnClick();
                    }

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(button.TooltipText);


                    ImGui.PopID();
                    iterations++;
                }
                ImGui.PushStyleColor(buttonidx, unclickedcolor);

                ImGui.End();
            }
        }
    }
}