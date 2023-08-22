using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class ComponentDesignUI : PulsarGuiWindow
    {
        // private int _designType;
        // private string[] _designTypes;
        // private List<ComponentTemplateSD> _designables;
        // private byte[] _nameInputBuffer = new byte[128];

        private ComponentDesignUI()
        {
            //_flags = ImGuiWindowFlags.NoCollapse;
        }

        internal static ComponentDesignUI GetInstance()
        {
            ComponentDesignUI thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(ComponentDesignUI)))
            {
                thisitem = new ComponentDesignUI();
            }
            thisitem = (ComponentDesignUI)_uiState.LoadedWindows[typeof(ComponentDesignUI)];

            return thisitem;
        }

        internal override void Display()
        {
            if(!IsActive) return;

            if(ImGui.Begin("Component Design", ref IsActive, _flags))
            {
                Vector2 windowContentSize = ImGui.GetContentRegionAvail();

                if(ImGui.BeginChild("ComponentDesignSelection", new Vector2(204f, windowContentSize.Y - 24f), true))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.Text("Select a Template");
                    ImGui.SameLine();
                    ImGui.Text("[?]");
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("Component Templates act as a framework for designing components.\n\n" +
                            "Select a template and then design the attributes of the component to your specification.\n" +
                            "Once the design is created it will be available to produce on the colonies with the appropriate\n" +
                            "installations.");
                    ImGui.PopStyleColor();
                    ImGui.Separator();

                    foreach(var template in StaticRefLib.StaticData.ComponentTemplates.Values)
                    {
                        var selected = ComponentDesignDisplay.GetInstance().Template?.Name.Equals(template.Name);

                        if(selected.HasValue && selected.Value)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.75f, 0.25f, 0.25f, 1f));
                        }
                        else
                        {
                            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0f));
                        }
                        if(ImGui.SmallButton(template.Name))
                        {
                            ComponentDesignDisplay.GetInstance().SetTemplate(template, _uiState);
                        }
                        ImGui.PopStyleColor();
                    }

                    ImGui.EndChild();
                }

                ImGui.BeginDisabled();
                if(ImGui.Button("Create Template", new Vector2(204f, 0f)))
                {

                }
                ImGui.EndDisabled();

                ImGui.SameLine();
                ImGui.SetCursorPosY(27f); // FIXME: this should somehow be calculated
                ComponentDesignDisplay.GetInstance().Display(_uiState);

                ImGui.End();
            }
        }

        public override void OnGameTickChange(DateTime newDate)
        {
        }

        public override void OnSystemTickChange(DateTime newDate)
        {
        }

        public override void OnSelectedSystemChange(StarSystem newStarSys)
        {
            throw new NotImplementedException();
        }
    }
}