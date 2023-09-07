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
        private readonly FleetDB factionRoot;
        private readonly Guid factionID;
        private Entity dragEntity = Entity.InvalidEntity;
        private Entity selectedFleet = null;
        private Entity selectedFleetFlagship = null;
        private Entity selectedFleetSystem = null;
        private bool selectedFleetInheritOrders = false;
        int nameCounter = 1;
        private Dictionary<Entity, bool> selectedShips = new ();
        private Dictionary<Entity, bool> selectedUnattachedShips = new ();

        private ConditionalOrder selectedOrder = new ConditionalOrder();

        private Dictionary<ConditionItem, int> orderConditionIndexes = new Dictionary<ConditionItem, int>();
        private int orderComparisonIndex = 0;
        private string[] orderComparisons;
        private int orderValue = 0;

        private readonly Dictionary<string, ICondition> orderConditions = new ();

        private FleetWindow()
        {
            factionID = _uiState.Faction.Guid;
            factionRoot = _uiState.Faction.GetDataBlob<FleetDB>();

            orderComparisons = new string[5];
            orderComparisons[0] = ComparisonType.LessThan.ToDescription();
            orderComparisons[1] = ComparisonType.LessThanOrEqual.ToDescription();
            orderComparisons[2] = ComparisonType.EqualTo.ToDescription();
            orderComparisons[3] = ComparisonType.GreaterThan.ToDescription();
            orderComparisons[4] = ComparisonType.GreaterThanOrEqual.ToDescription();

            orderConditions.Add("Fuel", new FuelCondition(30f, ComparisonType.LessThan));
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

            FleetDB navyDB = null;
            selectedFleet?.TryGetDatablob<FleetDB>(out navyDB);
            if(navyDB == null || navyDB.FlagShipID == Guid.Empty)
            {
                selectedFleetFlagship = null;
                selectedFleetSystem = null;
            }
            else
            {
                _uiState.Game.GlobalManager.FindEntityByGuid(navyDB.FlagShipID, out selectedFleetFlagship);
                selectedFleetFlagship.TryGetDatablob<PositionDB>(out var positionDB);
                selectedFleetSystem = positionDB.Root;
                selectedFleetInheritOrders = navyDB.InheritOrders;
            }
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

                            DisplayHelpers.PrintRow("Commander", "TODO");
                            if(selectedFleetFlagship != null)
                                DisplayHelpers.PrintRow("Flagship", Name(selectedFleetFlagship));
                            else
                                DisplayHelpers.PrintRow("Flagship", "-");

                            // Current system
                            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                            ImGui.Text("Current System");
                            ImGui.PopStyleColor();
                            ImGui.NextColumn();
                            if(selectedFleetFlagship != null && selectedFleetSystem != null && selectedFleetFlagship.TryGetDatablob<PositionDB>(out var positionDB))
                            {
                                if(ImGui.SmallButton(Name(selectedFleetSystem)))
                                {
                                    _uiState.EntityClicked(selectedFleetSystem.Guid, selectedFleetSystem.Guid, MouseButtons.Primary);
                                }
                                ImGui.NextColumn();
                                ImGui.Separator();

                                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                                ImGui.Text("Orbiting");
                                ImGui.PopStyleColor();
                                ImGui.NextColumn();
                                if(ImGui.SmallButton(Name(positionDB.Parent)))
                                {
                                    _uiState.EntityClicked(positionDB.Parent.Guid, positionDB.SystemGuid, MouseButtons.Primary);
                                }
                            }
                            else
                            {
                                ImGui.Text("Unknown");
                                ImGui.NextColumn();
                                ImGui.Separator();
                                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                                ImGui.Text("Orbiting");
                                ImGui.PopStyleColor();
                                ImGui.NextColumn();
                                ImGui.Text("Unknown");
                            }
                            ImGui.NextColumn();
                            ImGui.Separator();
                            DisplayHelpers.PrintRow("Ships", selectedFleet.GetDataBlob<FleetDB>().GetChildren().Where(x => !x.HasDataBlob<FleetDB>()).Count().ToString());
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
                                var fleet = selectedFleet.GetDataBlob<FleetDB>();
                                foreach(var ship in fleet.GetChildren())
                                {
                                    // Only display ships
                                    if(ship.HasDataBlob<FleetDB>()) continue;

                                    if(!selectedShips.ContainsKey(ship))
                                    {
                                        selectedShips.Add(ship, false);
                                    }

                                    string name = Name(ship);
                                    if(fleet.FlagShipID == ship.Guid)
                                    {
                                        name = "(F) " + name;
                                    }
                                    if(ImGui.Selectable(name, selectedShips[ship], ImGuiSelectableFlags.SpanAllColumns))
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
                    var size = ImGui.GetContentRegionAvail();
                    var firstChildSize = new Vector2(size.X * 0.33f, size.Y);
                    var secondChildSize = new Vector2(size.X * 0.67f - (size.X * 0.01f), size.Y);
                    if(ImGui.BeginChild("StandingOrders-List", firstChildSize, true))
                    {
                        DisplayHelpers.Header("Order List");
                        if(selectedFleet.GetDataBlob<FleetDB>().Parent.Guid != factionID)
                        {
                            if(ImGui.Checkbox("Inherit Orders###fleet-inherit-orders", ref selectedFleetInheritOrders))
                            {
                                var order = FleetOrder.ToggleInheritOrders(factionID, selectedFleet);
                                StaticRefLib.OrderHandler.HandleOrder(order);
                            }
                            if(ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("If checked the fleet will inherit it's orders from the fleet above it in the command heirarchy.");
                            }
                        }
                        ImGui.Text("standing orders");
                        ImGui.EndChild();
                    }
                    ImGui.SameLine();
                    if(ImGui.BeginChild("StandingOrders-edit", secondChildSize, true))
                    {
                        DisplayHelpers.Header("Order Condition");

                        var sizeAvailable = ImGui.GetContentRegionAvail();
                        var count = selectedOrder.Condition.ConditionItems.Count;
                        var items = selectedOrder.Condition.ConditionItems.ToArray();
                        for(int i = 0; i < count; i++)
                        {
                            var conditionItem = items[i];
                            ImGui.PushID(conditionItem.Guid.ToString());
                            if(!orderConditionIndexes.ContainsKey(conditionItem)) orderConditionIndexes.Add(conditionItem, 0);
                            var index = orderConditionIndexes[conditionItem];
                            ImGui.SetNextItemWidth(sizeAvailable.Y * 0.5f);
                            if(ImGui.Combo("###orderCondition" + conditionItem.Guid, ref index, orderConditions.Keys.ToArray(), orderConditions.Keys.Count))
                            {
                                orderConditionIndexes[conditionItem] = index;
                            }

                            // TODO: this looks horrible
                            var condition = conditionItem.Condition;
                            switch(condition.DisplayType)
                            {
                                case ConditionDisplayType.Comparison:
                                    ComparisonCondition comparisonCondition = (ComparisonCondition)condition;
                                    int value = (int)comparisonCondition.Threshold;
                                    int comparisonIndex = Array.IndexOf(orderComparisons, comparisonCondition.ComparisionType.ToDescription());
                                    ImGui.SameLine();
                                    ImGui.SetNextItemWidth(sizeAvailable.Y * 0.1f);
                                    if(ImGui.Combo("###orderComparison", ref comparisonIndex, orderComparisons, orderComparisons.Length))
                                    {
                                        comparisonCondition.ComparisionType = (ComparisonType)Enum.GetValues(typeof(ComparisonType)).GetValue(comparisonIndex);
                                    }
                                    ImGui.SameLine();
                                    ImGui.SetNextItemWidth(sizeAvailable.Y * 0.2f);
                                    if(ImGui.InputInt(comparisonCondition.Description + "###orderValue", ref value, 1, 5))
                                    {
                                        if(value < comparisonCondition.MinValue) orderValue = (int)comparisonCondition.MinValue;
                                        if(value > comparisonCondition.MaxValue) orderValue = (int)comparisonCondition.MaxValue;

                                        comparisonCondition.Threshold = value;
                                    }
                                    break;
                            }

                            // Show the logical operators UI on all but the last item
                            ImGui.SameLine();
                            var position = ImGui.GetCursorPos();
                            if(i < count - 1)
                            {
                                if(conditionItem.LogicalOperation == null)
                                    conditionItem.LogicalOperation = LogicalOperation.And;

                                ImGui.SetCursorPosY(position.Y + 12f);
                                if(conditionItem.LogicalOperation == LogicalOperation.And)
                                {
                                    ImGui.SetCursorPosX(sizeAvailable.X - 82f);
                                    if(ImGui.Button("AND"))
                                    {
                                        conditionItem.LogicalOperation = LogicalOperation.Or;
                                    }
                                }
                                else
                                {
                                    ImGui.SetCursorPosX(sizeAvailable.X - 48f);
                                    if(ImGui.Button("OR"))
                                    {
                                        conditionItem.LogicalOperation = LogicalOperation.And;
                                    }
                                }
                            }
                            ImGui.SameLine();
                            ImGui.SetCursorPos(position);
                            ImGui.SetCursorPosX(sizeAvailable.X - 8f);
                            if(ImGui.Button("X"))
                            {
                                selectedOrder.Condition.ConditionItems.Remove(conditionItem);
                            }
                            ImGui.PopID();
                        }

                        if(ImGui.Button("Add Condition"))
                        {
                            selectedOrder.Condition.ConditionItems.Add(new ConditionItem(new FuelCondition(30f, ComparisonType.LessThan)));
                        }

                        DisplayHelpers.Header("Order Actions");
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

                foreach(var fleet in factionRoot.GetChildren())
                {
                    DisplayFleetItem(fleet);
                }

                var sizeLeft = ImGui.GetContentRegionAvail();
                ImGui.InvisibleButton("invis-droptarget", new Vector2(sizeLeft.X, 32f));
                DisplayEmptyDropTarget();

                if(factionRoot.GetChildren().Where(x => !x.HasDataBlob<FleetDB>()).Count() > 0)
                {
                    DisplayHelpers.Header("Unattached Ships");

                    foreach(var ship in factionRoot.GetChildren())
                    {
                        if(ship.HasDataBlob<FleetDB>()) continue;

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
            if(!fleet.TryGetDatablob<FleetDB>(out var fleetInfo))
            {
                return;
            }

            ImGui.PushID(fleet.Guid.ToString());
            string name = Name(fleet);
            var flags = ImGuiTreeNodeFlags.DefaultOpen;

            if(fleetInfo.GetChildren().Where(x => x.HasDataBlob<FleetDB>()).Count() == 0)
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
                if(ImGui.MenuItem("Disband###delete-" + fleet.Guid))
                {
                    var order = FleetOrder.DisbandFleet(factionID, fleet);
                    StaticRefLib.OrderHandler.HandleOrder(order);
                    SelectFleet(null);
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
                if(!isUnattached)
                {
                    if(ship.Guid == selectedFleetFlagship.Guid)
                    {
                        ImGui.BeginDisabled();
                    }
                    if(ImGui.MenuItem("Promote to Flagship"))
                    {
                        var setFlagshipOrder = FleetOrder.SetFlagShip(factionID, selectedFleet, ship);
                        StaticRefLib.OrderHandler.HandleOrder(setFlagshipOrder);
                        SelectFleet(selectedFleet);
                    }
                    if(ship.Guid == selectedFleetFlagship.Guid)
                    {
                        ImGui.EndDisabled();
                    }
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
            if(!fleet.HasDataBlob<FleetDB>()) return;

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

            foreach(var child in fleet.GetDataBlob<FleetDB>().GetChildren())
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

        private string Name(StarSystem system)
        {
            return system.NameDB.GetName(factionID);
        }
    }
}