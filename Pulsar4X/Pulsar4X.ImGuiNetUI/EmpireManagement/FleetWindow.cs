using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class FleetWindow : PulsarGuiWindow
    {
        private FactionInfoDB factionInfoDB;
        private Guid factionID;
        private Entity dragEntity = Entity.InvalidEntity;
        private Entity selectedFleet = null;
        int nameCounter = 1;
        private Dictionary<Entity, bool> selectedShips = new ();

        private FleetWindow()
        {
            factionInfoDB = _uiState.Faction.GetDataBlob<FactionInfoDB>();
            factionID = _uiState.Faction.Guid;
        }
        internal static FleetWindow GetInstance()
        {
            FleetWindow thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(FleetWindow)))
            {
                thisitem = new FleetWindow();
            }
            thisitem = (FleetWindow)_uiState.LoadedWindows[typeof(FleetWindow)];

            return thisitem;
        }

        private void SelectFleet(Entity fleet)
        {
            selectedFleet = fleet;
            selectedShips = new ();
        }

        internal override void Display()
        {
            if(!IsActive) return;

            if(ImGui.Begin("Fleet Management", ref IsActive, _flags))
            {
                DisplayFleetList();

                if(selectedFleet == null) return;

                ImGui.SameLine();
                ImGui.SetCursorPosY(27f);

                DisplayTabs();

                ImGui.End();
            }
        }

        private void DisplayTabs()
        {
            if(ImGui.BeginChild("FleetTabs"))
            {
                ImGui.BeginTabBar("FleetTabBar", ImGuiTabBarFlags.None);

                if(ImGui.BeginTabItem("Summary"))
                {
                    Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                    var firstChildSize = new Vector2(windowContentSize.X * 0.5f, windowContentSize.Y);
                    var secondChildSize = new Vector2(windowContentSize.X * 0.5f - (windowContentSize.X * 0.01f), windowContentSize.Y);
                    if(ImGui.BeginChild("FleetSummary1", firstChildSize, true))
                    {
                        if(ImGui.CollapsingHeader("Fleet Information", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            ImGui.Columns(2);
                            DisplayHelpers.PrintRow("Name", Name(selectedFleet));

                            // Current system
                            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                            ImGui.Text("Current System");
                            ImGui.PopStyleColor();
                            ImGui.NextColumn();
                            if(ImGui.SmallButton("TODO"))
                            {
                                // open the system?
                            }
                            ImGui.NextColumn();
                            ImGui.Separator();

                            // Orbiting
                            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                            ImGui.Text("Orbiting");
                            ImGui.PopStyleColor();
                            ImGui.NextColumn();
                            if(ImGui.SmallButton("TODO"))
                            {
                                // open the entity
                            }
                            ImGui.NextColumn();
                            ImGui.Separator();
                            DisplayHelpers.PrintRow("Commander", "TODO");
                            DisplayHelpers.PrintRow("Ships", selectedFleet.GetDataBlob<FleetDB>().Ships.Count.ToString());
                            DisplayHelpers.PrintRow("Orders", "TODO", separator: false);
                        }
                        ImGui.EndChild();
                    }
                    ImGui.SameLine();
                    if(ImGui.BeginChild("FleetSummary2", secondChildSize, true))
                    {
                        if(ImGui.CollapsingHeader("Assigned Ships", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            ImGui.PushStyleColor(ImGuiCol.FrameBg, Styles.InvisibleColor);
                            if(ImGui.BeginListBox("###assigned-ships", ImGui.GetContentRegionAvail()))
                            {
                                ImGui.Columns(2, "assigned-ships-list", true);
                                foreach(var ship in selectedFleet.GetDataBlob<FleetDB>().Ships.ToArray())
                                {
                                    if(!selectedShips.ContainsKey(ship))
                                    {
                                        selectedShips.Add(ship, false);
                                    }

                                    if(ImGui.Selectable(Name(ship), selectedShips[ship], ImGuiSelectableFlags.SpanAllColumns))
                                    {
                                        selectedShips[ship] = !selectedShips[ship];
                                    }
                                    DisplayShipContextMenu();
                                    ImGui.NextColumn();
                                    ImGui.Text("TODO: Commander Name");
                                    ImGui.NextColumn();
                                    ImGui.Separator();
                                }
                                ImGui.Columns(1);
                                ImGui.EndListBox();
                            }
                            ImGui.PopStyleColor();
                        }
                        ImGui.EndChild();
                    }
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
                ImGui.EndChild();
            }
        }

        private void DisplayFleetList()
        {
            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("FleetListSelection", new Vector2(Styles.LeftColumnWidthLg, windowContentSize.Y - 24f), true))
            {
                DisplayHelpers.Header("Fleets", "Select a fleet to manage it.");

                // We need a drop target here so nested items can be un-nested to the root of the tree
                DisplayEmptyDropTarget();

                foreach(var fleet in factionInfoDB.Fleets.ToArray())
                {
                    DisplayFleetItem(fleet);
                }

                var sizeLeft = ImGui.GetContentRegionAvail();
                ImGui.InvisibleButton("invis-droptarget", sizeLeft);
                DisplayEmptyDropTarget();

                ImGui.EndChild();
            }

            if(ImGui.Button("Create New Fleet", new Vector2(Styles.LeftColumnWidthLg, 0f)))
            {
                string name = "auto-gen names pls " + nameCounter++;
                Entity fleet = FleetFactory.Create(_uiState.Game.GlobalManager, factionID, name);

                factionInfoDB.Fleets.Add(fleet);
            }
        }

        private void DisplayFleetItem(Entity fleet)
        {
            ImGui.PushID(fleet.Guid.ToString());
            string name = Name(fleet);
            var fleetInfo = fleet.GetDataBlob<FleetDB>();
            var flags = ImGuiTreeNodeFlags.DefaultOpen;

            if(fleetInfo.Children.Count == 0)
            {
                flags |= ImGuiTreeNodeFlags.Leaf;
            }

            if(selectedFleet == fleet)
            {
                flags |= ImGuiTreeNodeFlags.Selected;
            }

            bool isTreeOpen = ImGui.TreeNodeEx(name, flags);

            if(isTreeOpen)
            {
                if(ImGui.IsItemClicked())
                {
                    SelectFleet(fleet);
                }
                DisplayContextMenu(fleet);
                DisplayDropSource(fleet, name);
                DisplayDropTarget(fleet);
                foreach(var child in fleetInfo.Children.ToArray())
                {
                    DisplayFleetItem(child);
                }
                ImGui.TreePop();
            }

            if(!isTreeOpen)
            {
                DisplayContextMenu(fleet);
                DisplayDropSource(fleet, name);
                DisplayDropTarget(fleet);
            }
            ImGui.PopID();
        }

        private void DisplayContextMenu(Entity fleet)
        {
            if(ImGui.BeginPopupContextItem())
            {
                if(ImGui.MenuItem("Disband###delete-" + fleet.Guid))
                {
                    // FIXME: needs some checking to make sure the fleet has no ships
                    // FIXME: needs to make sure the entity is removed from the game
                    if(factionInfoDB.Fleets.Contains(fleet))
                    {
                        factionInfoDB.Fleets.Remove(fleet);
                    }
                    else
                    {
                        var fleetInfo = fleet.GetDataBlob<FleetDB>();
                        fleetInfo.ParentDB.Children.Remove(fleet);
                    }
                }
                ImGui.EndPopup();
            }
        }

        private void DisplayShipContextMenu()
        {
            if(ImGui.BeginPopupContextItem())
            {
                ImGui.Text("Re-assign selected ships to:");
                ImGui.Separator();

                foreach(var fleet in factionInfoDB.Fleets.ToArray())
                {
                    if(fleet == selectedFleet)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                        ImGui.Text(Name(fleet));
                        ImGui.PopStyleColor();
                        continue;
                    }

                    ImGui.PushID(fleet.Guid.ToString());
                    if(ImGui.MenuItem(Name(fleet)))
                    {
                        // FIXME: we probably want some logic that doesn't instantly re-assign the ship
                        foreach(var (ship, selected) in selectedShips)
                        {
                            if(!selected) continue;

                            // Remove the ship from the current fleet
                            selectedFleet.GetDataBlob<FleetDB>().Ships.Remove(ship);

                            // Add it to the new fleet
                            fleet.GetDataBlob<FleetDB>().Ships.Add(ship);
                        }
                    }
                    ImGui.PopID();
                }
                ImGui.EndPopup();
            }
        }

        private void DisplayEmptyDropTarget()
        {
            if(ImGui.BeginDragDropTarget())
            {
                ImGui.AcceptDragDropPayload("FLEET", ImGuiDragDropFlags.None);
                if(ImGui.IsMouseReleased(ImGuiMouseButton.Left) && dragEntity != Entity.InvalidEntity)
                {
                    // Remove the dragEntity from the parent tree or the facction Fleets
                    var sourceFleetInfo = dragEntity.GetDataBlob<FleetDB>();

                    // Check if nested
                    if(sourceFleetInfo.Root != dragEntity)
                    {
                        sourceFleetInfo.ParentDB.Children.Remove(dragEntity);
                        sourceFleetInfo.ClearParent();
                    }

                    // Drop the dragEntity
                    if(!factionInfoDB.Fleets.Contains(dragEntity))
                    {
                        factionInfoDB.Fleets.Add(dragEntity);
                    }
                    dragEntity = Entity.InvalidEntity;
                }
                ImGui.EndDragDropTarget();
            }
        }

        private void DisplayDropTarget(Entity fleet)
        {
            // Begin Drag Target
            if (ImGui.BeginDragDropTarget())
            {
                ImGui.AcceptDragDropPayload("FLEET", ImGuiDragDropFlags.None);
                if(ImGui.IsMouseReleased(ImGuiMouseButton.Left) && dragEntity != Entity.InvalidEntity)
                {
                    // Remove the dragEntity from the parent tree or the facction Fleets
                    var sourceFleetInfo = dragEntity.GetDataBlob<FleetDB>();

                    // Check if nested
                    if(sourceFleetInfo.Root != dragEntity)
                    {
                        sourceFleetInfo.ParentDB.Children.Remove(dragEntity);
                        sourceFleetInfo.ClearParent();
                    }

                    if(factionInfoDB.Fleets.Contains(dragEntity))
                    {
                        factionInfoDB.Fleets.Remove(dragEntity);
                    }

                    // Drop the dragEntity
                    sourceFleetInfo.SetParent(fleet);
                    dragEntity = Entity.InvalidEntity;
                }
                ImGui.EndDragDropTarget();
            }
        }

        private void DisplayDropSource(Entity fleet, string name)
        {
            // Begin drag source
            if(ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoDisableHover))
            {
                dragEntity = fleet;

                ImGui.SetDragDropPayload("FLEET", IntPtr.Zero, 0);
                ImGui.Text(name);
                ImGui.EndDragDropSource();
            }
        }

        private string Name(Entity entity)
        {
            return entity.GetDataBlob<NameDB>().GetName(factionID);
        }
    }
}