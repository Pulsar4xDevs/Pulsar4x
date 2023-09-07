using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
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
        private FleetDB selectedFleetDB = null;
        private bool selectedFleetInheritOrders = false;
        int nameCounter = 1;
        private Dictionary<Entity, bool> selectedShips = new ();
        private Dictionary<Entity, bool> selectedUnattachedShips = new ();

        private ConditionalOrder selectedOrder = null;

        private Dictionary<ConditionItem, int> orderConditionIndexes = new Dictionary<ConditionItem, int>();
        private int orderComparisonIndex = 0;
        private string[] orderComparisons;
        private int orderActionsIndex = -1;
        private int orderConditionsIndex = -1;
        private string[] orderActionDescriptions = OrderRegistry.Actions.Keys.ToArray();
        private string[] orderConditionDescriptions = OrderRegistry.Conditions.Keys.ToArray();
        private byte[] orderNameBuffer = new byte[32];

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
            SelectOrder(null);

            selectedFleet?.TryGetDatablob<FleetDB>(out selectedFleetDB);
            if(selectedFleetDB == null || selectedFleetDB.FlagShipID == Guid.Empty)
            {
                selectedFleetFlagship = null;
                selectedFleetSystem = null;
            }
            else
            {
                _uiState.Game.GlobalManager.FindEntityByGuid(selectedFleetDB.FlagShipID, out selectedFleetFlagship);
                selectedFleetFlagship.TryGetDatablob<PositionDB>(out var positionDB);
                selectedFleetSystem = positionDB.Root;
                selectedFleetInheritOrders = selectedFleetDB.InheritOrders;
            }
        }

        private void SelectOrder(ConditionalOrder order)
        {
            selectedOrder = order;

            if(selectedOrder != null)
            {
                orderNameBuffer = selectedOrder.Name.IsNullOrEmpty() ? new byte[32] : ImGuiSDL2CSHelper.BytesFromString(selectedOrder.Name, 32);
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
                            string orderName = selectedFleetDB.CurrentOrders.Count == 0 ? "None" : OrderRegistry.ActionDescriptions[selectedFleetDB.CurrentOrders.Peek().GetType()];
                            DisplayHelpers.PrintRow("Current Orders", orderName, separator: false);
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
                        var sizeAvailable = ImGui.GetContentRegionAvail();
                        DisplayHelpers.Header("Order List");
                        // if(selectedFleet.GetDataBlob<FleetDB>().Parent.Guid != factionID)
                        // {
                        //     if(ImGui.Checkbox("Inherit Orders###fleet-inherit-orders", ref selectedFleetInheritOrders))
                        //     {
                        //         var order = FleetOrder.ToggleInheritOrders(factionID, selectedFleet);
                        //         StaticRefLib.OrderHandler.HandleOrder(order);
                        //     }
                        //     if(ImGui.IsItemHovered())
                        //     {
                        //         ImGui.SetTooltip("If checked the fleet will inherit it's orders from the fleet above it in the command heirarchy.");
                        //     }
                        // }
                        if(selectedFleetDB.StandingOrders.Count > 0)
                        {
                            var count = selectedFleetDB.StandingOrders.Count;
                            var orders = selectedFleetDB.StandingOrders.ToArray();
                            for(int i = 0; i < count; i++)
                            {
                                ImGui.PushID(orders[i].GetHashCode());
                                bool isSelected = selectedOrder == orders[i];
                                var name = orders[i].Name.IsNullOrEmpty() ? "<un-named>" : orders[i].Name;
                                if(ImGui.Selectable((i + 1) + ". " + name, ref isSelected))
                                {
                                    SelectOrder(orders[i]);
                                }
                                if(ImGui.BeginPopupContextItem())
                                {
                                    if(i > 0 && ImGui.MenuItem("Move Up"))
                                    {
                                        var temp = selectedFleetDB.StandingOrders[i - 1];
                                        selectedFleetDB.StandingOrders[i - 1] = selectedFleetDB.StandingOrders[i];
                                        selectedFleetDB.StandingOrders[i] = temp;
                                    }
                                    if(i < count - 1 && ImGui.MenuItem("Move Down"))
                                    {
                                        var temp = selectedFleetDB.StandingOrders[i + 1];
                                        selectedFleetDB.StandingOrders[i + 1] = selectedFleetDB.StandingOrders[i];
                                        selectedFleetDB.StandingOrders[i] = temp;
                                    }
                                    if(ImGui.MenuItem("Delete Order"))
                                    {
                                        selectedFleetDB.StandingOrders.Remove(orders[i]);
                                        if(isSelected)
                                            SelectOrder(null);
                                    }
                                    ImGui.EndPopup();
                                }
                                ImGui.PopID();
                            }
                        }
                        else
                        {
                            ImGui.Text("No orders");
                        }

                        ImGui.SetCursorPosY(sizeAvailable.Y - 12f);
                        if(ImGui.Button("Create New Order", new Vector2(sizeAvailable.X, 0)))
                        {
                            var order = new ConditionalOrder();
                            selectedFleetDB.StandingOrders.Add(order);

                            // if this is the first order, select it
                            if(selectedFleetDB.StandingOrders.Count == 1)
                                SelectOrder(order);
                        }
                        ImGui.EndChild();
                    }
                    ImGui.SameLine();
                    if(selectedOrder != null && ImGui.BeginChild("StandingOrders-edit", secondChildSize, true))
                    {
                        var sizeAvailable = ImGui.GetContentRegionAvail();
                        DisplayHelpers.Header("Order Name");
                        ImGui.InputText("", orderNameBuffer, 32);
                        ImGui.NewLine();
                        DisplayHelpers.Header("Conditions", "If the conditions listed are true, the actions will execute.");

                        var count = selectedOrder.Condition.ConditionItems.Count;
                        var items = selectedOrder.Condition.ConditionItems.ToArray();
                        for(int i = 0; i < count; i++)
                        {
                            var conditionItem = items[i];
                            ImGui.PushID(conditionItem.Guid.ToString());
                            if(!orderConditionIndexes.ContainsKey(conditionItem)) orderConditionIndexes.Add(conditionItem, 0);
                            var index = orderConditionIndexes[conditionItem];
                            var condition = conditionItem.Condition;
                            ImGui.Button(OrderRegistry.ConditionDescriptions[conditionItem.Condition.GetType()], new Vector2(Math.Max(sizeAvailable.X * 0.4f, 128f), 0f));

                            switch(condition.DisplayType)
                            {
                                case ConditionDisplayType.Comparison:
                                    ComparisonCondition comparisonCondition = (ComparisonCondition)condition;
                                    int value = (int)comparisonCondition.Threshold;
                                    int comparisonIndex = Array.IndexOf(orderComparisons, comparisonCondition.ComparisionType.ToDescription());
                                    ImGui.SameLine();
                                    ImGui.SetNextItemWidth(Math.Max(sizeAvailable.X * 0.075f, 16f));
                                    if(ImGui.Combo("###orderComparison", ref comparisonIndex, orderComparisons, orderComparisons.Length))
                                    {
                                        comparisonCondition.ComparisionType = (ComparisonType)Enum.GetValues(typeof(ComparisonType)).GetValue(comparisonIndex);
                                    }
                                    ImGui.SameLine();
                                    ImGui.SetNextItemWidth(Math.Max(sizeAvailable.X * 0.15f, 32f));
                                    if(ImGui.InputInt(comparisonCondition.Description + "###orderValue", ref value, 1, 5))
                                    {
                                        if(value < comparisonCondition.MinValue) value = (int)comparisonCondition.MinValue;
                                        if(value > comparisonCondition.MaxValue) value = (int)comparisonCondition.MaxValue;

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
                            ImGui.SetCursorPosX(sizeAvailable.X - 12f);
                            if(ImGui.Button("x"))
                            {
                                selectedOrder.Condition.ConditionItems.Remove(conditionItem);
                            }
                            ImGui.PopID();
                        }

                        if(ImGui.Button("Add Condition"))
                        {
                            if(orderConditionsIndex >= 0 && orderConditionsIndex < orderConditionDescriptions.Length)
                            {
                                ConditionItem item = OrderRegistry.Conditions[orderConditionDescriptions[orderConditionsIndex]]();
                                selectedOrder.Condition.ConditionItems.Add(item);
                            }
                        }
                        ImGui.SameLine();
                        if(ImGui.Combo("###order-add-condition-list", ref orderConditionsIndex, orderConditionDescriptions, orderConditionDescriptions.Length))
                        {
                        }

                        ImGui.NewLine();
                        DisplayHelpers.Header("Actions", "The actions listed will execute in the order in which they are listed.");

                        foreach(var action in selectedOrder.Actions.ToArray())
                        {
                            DisplayActionItem(action);
                        }

                        if(ImGui.Button("Add Action"))
                        {
                            if(orderActionsIndex >= 0 && orderActionsIndex < orderActionDescriptions.Length)
                            {
                                IAction selectedAction = OrderRegistry.Actions[orderActionDescriptions[orderActionsIndex]]();
                                selectedOrder.Actions.Add(selectedAction);
                            }
                        }
                        ImGui.SameLine();
                        if(ImGui.Combo("###order-add-action-list", ref orderActionsIndex, orderActionDescriptions, orderActionDescriptions.Length))
                        {
                        }

                        ImGui.SetCursorPosY(sizeAvailable.Y - 12f);
                        if(ImGui.Button("Save", new Vector2(sizeAvailable.X, 0)))
                        {
                            string name = ImGuiSDL2CSHelper.StringFromBytes(orderNameBuffer);
                            if(name.IsNotNullOrEmpty())
                            {
                                selectedOrder.Name = name;
                            }
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

        private void DisplayActionItem(IAction action)
        {
            ImGui.PushID(action.GetHashCode());
            var size = ImGui.GetContentRegionAvail();
            ImGui.Text(OrderRegistry.ActionDescriptions[action.GetType()]);
            ImGui.SameLine();
            ImGui.SetCursorPosX(size.X - 12f);
            if(ImGui.Button("x"))
            {
                selectedOrder.Actions.Remove(action);
            }
            ImGui.PopID();
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