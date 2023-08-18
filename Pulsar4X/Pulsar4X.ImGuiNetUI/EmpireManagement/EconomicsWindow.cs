using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class EconomicsWindow : PulsarGuiWindow
    {
        private Dictionary<Guid, bool> isExpanded = new();
        private EntityState selectedEntity = null;

        internal static EconomicsWindow GetInstance()
        {
            EconomicsWindow thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(EconomicsWindow)))
            {
                thisitem = new EconomicsWindow()
                {
                    // FIXME: this will crash if there are no colonies
                    selectedEntity = _uiState.StarSystemStates.First().Value.EntityStatesColonies.First().Value
                };
            }
            thisitem = (EconomicsWindow)_uiState.LoadedWindows[typeof(EconomicsWindow)];

            return thisitem;
        }

        internal override void Display()
        {
            if(!IsActive) return;

            if(ImGui.Begin("Manage Economy", ref IsActive))
            {
                Vector2 windowContentSize = ImGui.GetContentRegionAvail();

                if(ImGui.BeginChild("Colonies", new Vector2(204f, windowContentSize.Y), true))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.45f, 0.45f, 0.45f, 1f));
                    ImGui.Text("Select Colony to Manage");
                    ImGui.PopStyleColor();
                    ImGui.Separator();
                    foreach(var (id, systemState) in _uiState.StarSystemStates)
                    {
                        if(!isExpanded.ContainsKey(id)) isExpanded.Add(id, true);
                        ImGui.SetNextItemOpen(isExpanded[id], ImGuiCond.Appearing);
                        if(ImGui.TreeNode(systemState.StarSystem.NameDB.DefaultName))
                        {
                            foreach(var (c_id, colony) in systemState.EntityStatesColonies)
                            {
                                var population = colony.Entity.GetDataBlob<ColonyInfoDB>().Population.Values.Sum();

                                if(selectedEntity == colony)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.75f, 0.25f, 0.25f, 1f));
                                }
                                else
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0f));
                                }

                                if(ImGui.SmallButton(colony.Name + " (" + Stringify.Quantity(population) + ")"))
                                {
                                    selectedEntity = colony;
                                }
                                ImGui.PopStyleColor();
                            }
                            ImGui.TreePop();
                        }
                    }
                    ImGui.EndChild();
                }

                if(selectedEntity == null) return;

                ImGui.SameLine();

                if(ImGui.BeginChild("ColoniesTabs"))
                {
                    ImGui.BeginTabBar("EconomicsTabBar", ImGuiTabBarFlags.None);

                    if(ImGui.BeginTabItem("Summary"))
                    {
                        selectedEntity.Entity.DisplaySummary(selectedEntity, _uiState);
                        ImGui.EndTabItem();
                    }
                    if(ImGui.BeginTabItem("Construction"))
                    {
                        selectedEntity.Entity.DisplayIndustry(selectedEntity, _uiState);
                        ImGui.EndTabItem();
                    }
                    if(ImGui.BeginTabItem("Mining"))
                    {
                        selectedEntity.Entity.DisplayMining(_uiState);
                        ImGui.EndTabItem();
                    }
                    if(ImGui.BeginTabItem("Research"))
                    {
                        selectedEntity.Entity.DisplayResearch(selectedEntity, _uiState);
                        ImGui.EndTabItem();
                    }
                    if(ImGui.BeginTabItem("Shipyards"))
                    {
                        ImGui.Text("Shipyards");
                        ImGui.EndTabItem();
                    }
                    if(ImGui.BeginTabItem("Logistics"))
                    {
                        selectedEntity.Entity.DisplayLogistics(selectedEntity, _uiState);
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                    ImGui.EndChild();
                }

                ImGui.End();
            }
        }
    }
}