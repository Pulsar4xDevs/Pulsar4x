using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ImGuiNetUI.EntityManagement;

namespace Pulsar4X.SDL2UI
{
    public class ToolBarWindow : PulsarGuiWindow
    {
        public Vector2 ButtonSize = new Vector2(32, 32);
        private uint UnClickedColour;
        private uint ClickedColour;
        private List<ToolBarOption> ToolButtons = new ();      //Stores the data for each button
        private List<ToolBarOption> SMToolButtons = new ();    //Stores the data for each button

        public class ToolBarOption
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
        private ToolBarWindow()
        {
            _flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize;

            unsafe
            {
                Vector4* unclickedcolorv = ImGui.GetStyleColorVec4(ImGuiCol.Button);
                Vector4* clickedcolorv = ImGui.GetStyleColorVec4(ImGuiCol.ButtonActive);
                UnClickedColour = ImGui.ColorConvertFloat4ToU32(*unclickedcolorv);
                ClickedColour = ImGui.ColorConvertFloat4ToU32(*clickedcolorv);
            }

            ToolBarOption btn = new ToolBarOption()
            {
                Picture = _uiState.Img_DesComponent(),
                TooltipText = "Design a new component or facility",
                OnClick = new Action(ComponentDesignWindow.GetInstance().ToggleActive),
                GetActive = new Func<bool>(ComponentDesignWindow.GetInstance().GetActive)
                //Opens up the component design menu
            };
            ToolButtons.Add(btn);

            btn =  new ToolBarOption()
            {
                Picture = _uiState.Img_DesignShip(),
                TooltipText = "Design a new Ship",
                OnClick = new Action(ShipDesignWindow.GetInstance().ToggleActive),
                GetActive = new Func<bool>(ShipDesignWindow.GetInstance().GetActive)
                //Opens up the ship design menu
            };
            ToolButtons.Add(btn);

            btn =  new ToolBarOption()
            {
                Picture = _uiState.Img_Industry(),
                TooltipText = "Colony Management",
                OnClick = new Action(EconomicsWindow.GetInstance().ToggleActive),
                GetActive = new Func<bool>(EconomicsWindow.GetInstance().GetActive)
                //Opens up the ship design menu
            };
            ToolButtons.Add(btn);

            btn =  new ToolBarOption()
            {
                Picture = _uiState.Img_Research(),
                TooltipText = "Research",
                OnClick = new Action(ResearchWindow.GetInstance().ToggleActive),
                GetActive = new Func<bool>(ResearchWindow.GetInstance().GetActive)
                //Opens up the research menu
            };
            ToolButtons.Add(btn);

            btn =  new ToolBarOption()
            {
                Picture = _uiState.Img_Select(),
                TooltipText = "Fleet Management",
                OnClick = new Action(FleetWindow.GetInstance().ToggleActive),
                GetActive = new Func<bool>(FleetWindow.GetInstance().GetActive)
                //Opens up the fleet menu
            };
            ToolButtons.Add(btn);

            btn =  new ToolBarOption()
            {
                Picture = _uiState.Img_Select(),
                TooltipText = "Commanders",
                OnClick = new Action(CommanderWindow.GetInstance().ToggleActive),
                GetActive = new Func<bool>(CommanderWindow.GetInstance().GetActive)
            };
            ToolButtons.Add(btn);

            btn = new ToolBarOption()
            {
                Picture = _uiState.Img_GalaxyMap(),
                TooltipText = "Galaxy Browser",
                OnClick = new Action(GalaxyWindow.GetInstance().ToggleActive),
                GetActive = new Func<bool>(GalaxyWindow.GetInstance().GetActive)

            };
            ToolButtons.Add(btn);

            btn = new ToolBarOption()
            {
                Picture = _uiState.Img_Ruler(),
                TooltipText = "Measure distance",
                OnClick = new Action(DistanceRuler.GetInstance().ToggleActive),
                GetActive = new Func<bool>(DistanceRuler.GetInstance().GetActive)
                //Opens the ruler menu
            };
            ToolButtons.Add(btn);

            btn = new ToolBarOption()
            {
                Picture = _uiState.Img_Tree(),
                TooltipText = "View objects in the system",
                OnClick = new Action(SystemTreeViewer.GetInstance().ToggleActive),
                GetActive = new Func<bool>(SystemTreeViewer.GetInstance().GetActive)
                //Display a tree with all objects in the system
            };
            ToolButtons.Add(btn);

            btn = new ToolBarOption()
            {
                Picture = _uiState.Img_Tree(),
                TooltipText = "Design orders and assign to entities",
                OnClick = new Action(OrderCreationUI.GetInstance().ToggleActive),
                GetActive = new Func<bool>(OrderCreationUI.GetInstance().GetActive)
                //Design orders for OrderableDB entities
            };
            ToolButtons.Add(btn);

            // btn = new ToolBarOption()
            // {
            //     Picture = _uiState.Img_Tree(),
            //     TooltipText = "Spawn ships and planets",
            //     OnClick = new Action(EntitySpawnWindow.GetInstance().ToggleActive),
            //     GetActive = new Func<bool>(EntitySpawnWindow.GetInstance().GetActive),
            //     //Display a tree with all objects in the system
            // };
            // SMToolButtons.Add(btn);

            btn = new ToolBarOption()
            {
                Picture = _uiState.Img_Tree(),
                TooltipText = "View SM debug info about a body",
                OnClick = new Action(SMPannel.GetInstance().ToggleActive),
                GetActive = new Func<bool>(SMPannel.GetInstance().GetActive),
                //Display a list of bodies with some info about them.
            };
            SMToolButtons.Add(btn);
        }

        internal static ToolBarWindow GetInstance()
        {
            if (!PulsarGuiWindow._uiState.LoadedWindows.ContainsKey(typeof(ToolBarWindow)))
            {
                return new ToolBarWindow();
            }

            return (ToolBarWindow)PulsarGuiWindow._uiState.LoadedWindows[typeof(ToolBarWindow)];
        }

        internal void SetButtons(List<ToolBarOption> buttons)
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

        void DisplayButtons(string name, List<ToolBarOption> DisplayToolButtons)
        {
            if (ImGui.Begin(name, _flags))
            {
                ImGuiCol buttonidx = ImGuiCol.Button;

                uint iterations = 0;
                //displays the default toolbar menu icons
                foreach (var button in DisplayToolButtons)//For each button
                {
                    ImGui.PushID(iterations.ToString());

                    if (button.GetActive != null)//If the windows state can be checked
                    {
                        if (button.GetActive())//If the window is open
                        {
                            ImGui.PushStyleColor(buttonidx, ClickedColour);//Have the button be "pressed"
                        }
                        else//If closed
                        {
                            ImGui.PushStyleColor(buttonidx, UnClickedColour);//Have the button be colored normally
                        }
                    }

                    if (ImGui.ImageButton(button.Picture, ButtonSize))//Make the button
                    {
                        button.OnClick();
                    }

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(button.TooltipText);


                    ImGui.PopID();
                    iterations++;
                }
                ImGui.PushStyleColor(buttonidx, UnClickedColour);

                ImGui.End();
            }
        }
    }
}