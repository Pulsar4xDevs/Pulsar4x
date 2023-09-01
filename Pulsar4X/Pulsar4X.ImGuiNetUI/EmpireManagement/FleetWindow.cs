using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class FleetWindow : PulsarGuiWindow
    {
        private readonly NavyDB factionRoot;
        private readonly Guid factionID;
        private Entity dragEntity = Entity.InvalidEntity;
        private Entity selectedFleet = null;
        int nameCounter = 1;
        private Dictionary<Entity, bool> selectedShips = new ();
        private Dictionary<Entity, bool> selectedUnattachedShips = new ();

        private FleetWindow()
        {
            factionID = _uiState.Faction.Guid;
            factionRoot = _uiState.Faction.GetDataBlob<NavyDB>();
        }
        internal static FleetWindow GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(FleetWindow)))
            {
                return new FleetWindow();
            }
            return (FleetWindow)_uiState.LoadedWindows[typeof(FleetWindow)];
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
                            DisplayHelpers.PrintRow("Ships", selectedFleet.GetDataBlob<NavyDB>().GetChildren().Where(x => !x.HasDataBlob<NavyDB>()).Count().ToString());
                            DisplayHelpers.PrintRow("Current Orders", "TODO", separator: false);
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
                                foreach(var ship in selectedFleet.GetDataBlob<NavyDB>().GetChildren())
                                {
                                    // Only display ships
                                    if(ship.HasDataBlob<NavyDB>()) continue;

                                    if(!selectedShips.ContainsKey(ship))
                                    {
                                        selectedShips.Add(ship, false);
                                    }

                                    if(ImGui.Selectable(Name(ship), selectedShips[ship], ImGuiSelectableFlags.SpanAllColumns))
                                    {
                                        selectedShips[ship] = !selectedShips[ship];
                                    }
                                    DisplayShipContextMenu(selectedShips, ship);
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

                if(ImGui.BeginTabItem("Standing Orders"))
                {
                    ImGui.Text("Standing orders");
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

                foreach(var fleet in factionRoot.GetChildren())
                {
                    DisplayFleetItem(fleet);
                }

                var sizeLeft = ImGui.GetContentRegionAvail();
                ImGui.InvisibleButton("invis-droptarget", new Vector2(sizeLeft.X, 32f));
                DisplayEmptyDropTarget();

                if(factionRoot.GetChildren().Where(x => !x.HasDataBlob<NavyDB>()).Count() > 0)
                {
                    DisplayHelpers.Header("Unattached Ships");

                    foreach(var ship in factionRoot.GetChildren())
                    {
                        if(ship.HasDataBlob<NavyDB>()) continue;

                        if(!selectedUnattachedShips.ContainsKey(ship))
                        {
                            selectedUnattachedShips.Add(ship, false);
                        }

                        if(ImGui.Selectable(Name(ship), selectedUnattachedShips[ship]))
                        {
                            selectedUnattachedShips[ship] = !selectedUnattachedShips[ship];
                        }
                        DisplayShipContextMenu(selectedUnattachedShips, ship, isUnattached: true);
                    }
                }

                ImGui.EndChild();
            }

            if(ImGui.Button("Create New Fleet", new Vector2(Styles.LeftColumnWidthLg, 0f)))
            {
                string name = "auto-gen names pls " + nameCounter++;
                var order = FleetOrder.CreateFleetOrder(name, _uiState.Faction);
                StaticRefLib.OrderHandler.HandleOrder(order);
            }
        }

        private void DisplayFleetItem(Entity fleet)
        {
            if(!fleet.TryGetDatablob<NavyDB>(out var fleetInfo))
            {
                return;
            }

            ImGui.PushID(fleet.Guid.ToString());
            string name = Name(fleet);
            var flags = ImGuiTreeNodeFlags.DefaultOpen;

            if(fleetInfo.GetChildren().Where(x => x.HasDataBlob<NavyDB>()).Count() == 0)
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
                foreach(var child in fleetInfo.GetChildren())
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
                if(factionRoot.GetChildren().Where(x => x.HasDataBlob<NavyDB>()).Count() <= 1)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.Text("Unable to Disband");
                    ImGui.PopStyleColor();
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.BadColor);
                    ImGui.Text("Must have at least one fleet");
                    ImGui.PopStyleColor();
                }
                else
                {
                    if(ImGui.MenuItem("Disband###delete-" + fleet.Guid))
                    {
                        var order = FleetOrder.DisbandFleet(factionID, fleet);
                        StaticRefLib.OrderHandler.HandleOrder(order);
                        SelectFleet(null);
                    }
                }

                ImGui.EndPopup();
            }
        }

        private void DisplayShipContextMenu(Dictionary<Entity, bool> selected, Entity ship, bool isUnattached = false)
        {
            if(ImGui.BeginPopupContextItem())
            {
                if(ImGui.MenuItem("View Ship"))
                {
                    _uiState.EntityClicked(ship.Guid, _uiState.SelectedStarSysGuid, MouseButtons.Primary);
                }
                ImGui.Separator();

                if(ImGui.BeginMenu("Re-assign ships"))
                {
                    ImGui.Text("Re-assign ships to:");
                    ImGui.Separator();
                    foreach(var fleet in factionRoot.GetChildren())
                    {
                        DisplayShipAssignmentOption(selected, ship, fleet, isUnattached: isUnattached);
                    }
                    ImGui.EndMenu();
                }
                ImGui.EndPopup();
            }
        }

        private void DisplayShipAssignmentOption(Dictionary<Entity, bool> selected, Entity ship, Entity fleet, int depth = 0, bool isUnattached = false)
        {
            if(!fleet.HasDataBlob<NavyDB>()) return;

            for(int i = 0; i < depth; i++)
            {
                ImGui.InvisibleButton("invis", new Vector2(8, 8));
                ImGui.SameLine();
            }

            if(fleet == selectedFleet && !isUnattached)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.Text(Name(fleet));
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushID(fleet.Guid.ToString());
                if(ImGui.MenuItem(Name(fleet)))
                {
                    if(!selected.Any(x => x.Value))
                    {
                        var unassignFrom = factionRoot.Children.Contains(ship) ? factionRoot.OwningEntity : selectedFleet;
                        var unassignOrder = FleetOrder.UnassignShip(factionID, unassignFrom, ship);
                        StaticRefLib.OrderHandler.HandleOrder(unassignOrder);

                        var assignOrder = FleetOrder.AssignShip(factionID, fleet, ship);
                        StaticRefLib.OrderHandler.HandleOrder(assignOrder);
                    }
                    else
                    {
                        foreach(var (selectedShip, isSelected) in selected)
                        {
                            if(!isSelected) continue;

                            var unassignFrom = factionRoot.Children.Contains(selectedShip) ? factionRoot.OwningEntity : selectedFleet;
                            var unassignOrder = FleetOrder.UnassignShip(factionID, unassignFrom, selectedShip);
                            StaticRefLib.OrderHandler.HandleOrder(unassignOrder);

                            var assignOrder = FleetOrder.AssignShip(factionID, fleet, selectedShip);
                            StaticRefLib.OrderHandler.HandleOrder(assignOrder);
                        }
                        // Clean up the selections
                        selected.Clear();
                    }
                }
                ImGui.PopID();
            }

            foreach(var child in fleet.GetDataBlob<NavyDB>().GetChildren())
            {
                DisplayShipAssignmentOption(selected, ship, child, depth + 1, isUnattached);
            }
        }

        private void DisplayEmptyDropTarget()
        {
            if(ImGui.BeginDragDropTarget())
            {
                ImGui.AcceptDragDropPayload("FLEET", ImGuiDragDropFlags.None);
                if(ImGui.IsMouseReleased(ImGuiMouseButton.Left) && dragEntity != Entity.InvalidEntity)
                {
                    var order = FleetOrder.ChangeParent(factionID, dragEntity, factionRoot.OwningEntity);
                    StaticRefLib.OrderHandler.HandleOrder(order);
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
                    var order = FleetOrder.ChangeParent(factionID, dragEntity, fleet);
                    StaticRefLib.OrderHandler.HandleOrder(order);
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