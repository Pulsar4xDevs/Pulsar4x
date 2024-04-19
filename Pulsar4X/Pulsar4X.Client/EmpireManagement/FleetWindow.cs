using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using ImGuiSDL2CS;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine.Orders;
using Pulsar4X.DataStructures;
using Pulsar4X.Extensions;

namespace Pulsar4X.SDL2UI
{
    public class FleetWindow : PulsarGuiWindow
    {
        private enum IssueOrderType
        {
            MoveTo,
            GeoSurvey,
            JPSurvey,
            Jump,
        }

        private IssueOrderType selectedIssueOrderType = IssueOrderType.MoveTo;

        private FleetDB? factionRoot;
        private int factionID;
        private Entity dragEntity = Entity.InvalidEntity;
        public Entity? SelectedFleet { get; private set; } = null;
        private Entity? selectedFleetFlagship = null;
        private Entity? selectedFleetSystem = null;
        private FleetDB? selectedFleetDB = null;
        private bool selectedFleetInheritOrders = false;
        int nameCounter = 1;
        private Dictionary<Entity, bool> selectedShips = new ();
        private Dictionary<Entity, bool> selectedUnattachedShips = new ();

        private ConditionalOrder? selectedOrder = null;

        private Dictionary<ConditionItem, int> orderConditionIndexes = new Dictionary<ConditionItem, int>();
        private int orderComparisonIndex = 0;
        private string[] orderComparisons;
        private int orderActionsIndex = 0;
        private int orderConditionsIndex = 0;
        private string[] orderActionDescriptions = OrderRegistry.Actions.Keys.ToArray();
        private string[] orderConditionDescriptions = OrderRegistry.Conditions.Keys.ToArray();
        private byte[] orderNameBuffer = new byte[32];

        private FleetWindow()
        {
            FactionChanged(_uiState);

            _uiState.OnFactionChanged += FactionChanged;

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

        private void FactionChanged(GlobalUIState uiState)
        {
            factionID = uiState.Faction.Id;
            factionRoot = uiState.Faction.GetDataBlob<FleetDB>();

            if(factionRoot.Children.Count > 0)
            {
                SelectFleet(factionRoot.Children.Where(c => c.HasDataBlob<FleetDB>()).First());
            }
            else
            {
                SelectFleet(null);
            }
        }

        public void SelectFleet(Entity? fleet)
        {
            SelectedFleet = fleet;
            selectedShips = new ();
            SelectOrder(null);

            SelectedFleet?.TryGetDatablob<FleetDB>(out selectedFleetDB);
            if(selectedFleetDB == null || selectedFleetDB.FlagShipID == -1)
            {
                selectedFleetFlagship = null;
                selectedFleetSystem = null;
            }
            else
            {
                selectedFleetDB.OwningEntity?.Manager?.TryGetEntityById(selectedFleetDB.FlagShipID, out selectedFleetFlagship);
                if(selectedFleetFlagship != null && selectedFleetFlagship.TryGetDatablob<PositionDB>(out var positionDB))
                {
                    selectedFleetSystem = positionDB?.Root;
                }
                selectedFleetInheritOrders = selectedFleetDB.InheritOrders;
            }
        }

        private void SelectOrder(ConditionalOrder? order)
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

                if(SelectedFleet == null) return;

                ImGui.SameLine();
                ImGui.SetCursorPosY(27f);

                DisplayShips();

                ImGui.SameLine();
                ImGui.SetCursorPosY(27f);

                DisplayTabs();

                ImGui.End();
            }
        }

        private void DisplayTabs()
        {
            if(SelectedFleet == null) return;

            if(ImGui.BeginChild("FleetTabs"))
            {
                ImGui.BeginTabBar("FleetTabBar", ImGuiTabBarFlags.None);

                if(ImGui.BeginTabItem("Summary"))
                {
                    Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                    var firstChildSize = new Vector2(windowContentSize.X * 0.99f, windowContentSize.Y);
                    var secondChildSize = new Vector2(windowContentSize.X * 0.5f - (windowContentSize.X * 0.01f), windowContentSize.Y);
                    if (ImGui.BeginChild("FleetSummary1", firstChildSize, true))
                    {
                        if (ImGui.CollapsingHeader("Fleet Information", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            ImGui.Columns(2);
                            DisplayHelpers.PrintRow("Name", SelectedFleet.GetName(factionID));

                            if (selectedFleetFlagship != null)
                            {
                                DisplayHelpers.PrintRow("Flagship", selectedFleetFlagship.GetName(factionID));

                                string commanderName = "None";
                                if (selectedFleetFlagship.TryGetDatablob<ShipInfoDB>(out var shipInfoDB)
                                    && shipInfoDB.CommanderID != -1)
                                {
                                    if(shipInfoDB.OwningEntity != null && shipInfoDB.OwningEntity.Manager != null)
                                    {
                                        shipInfoDB.OwningEntity.Manager.TryGetEntityById(shipInfoDB.CommanderID, out var commanderEntity);
                                        commanderName = commanderEntity.GetName(factionID);
                                    }
                                }
                                DisplayHelpers.PrintRow("Commander", commanderName);
                            }
                            else
                            {
                                DisplayHelpers.PrintRow("Flagship", "-");
                                DisplayHelpers.PrintRow("Commander", "-");
                            }

                            // Current system
                            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                            ImGui.Text("Current System");
                            ImGui.PopStyleColor();
                            ImGui.NextColumn();
                            if (selectedFleetFlagship != null && selectedFleetSystem != null && selectedFleetFlagship.TryGetDatablob<PositionDB>(out var positionDB))
                            {
                                StarSystem? starSystem = (StarSystem?)positionDB.OwningEntity?.Manager;
                                if (ImGui.SmallButton(starSystem?.NameDB.OwnersName ?? "Unknown"))
                                {
                                    if(starSystem != null)
                                        _uiState.SetActiveSystem(starSystem.ManagerGuid);
                                }
                                ImGui.NextColumn();
                                ImGui.Separator();

                                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                                ImGui.Text("Orbiting");
                                ImGui.PopStyleColor();
                                ImGui.NextColumn();
                                if (ImGui.SmallButton(positionDB.Parent?.GetName(factionID) ?? "Unknown"))
                                {
                                    if(positionDB.Parent != null)
                                        _uiState.EntityClicked(positionDB.Parent.Id, positionDB.SystemGuid, MouseButtons.Primary);
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
                            DisplayHelpers.PrintRow("Ships", SelectedFleet.GetDataBlob<FleetDB>().GetChildren().Where(x => !x.HasDataBlob<FleetDB>()).Count().ToString());
                        }
                        ImGui.Columns(1);
                        if (ImGui.CollapsingHeader("Fleet Orders", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            var orderableDB = SelectedFleet.GetDataBlob<OrderableDB>();

                            if (orderableDB.ActionList.Count == 0)
                            {
                                ImGui.Text("None");
                            }
                            else
                            {
                                if (ImGui.BeginTable("FleetOrdersTable", 2, Styles.TableFlags))
                                {
                                    ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.None, 0.1f);
                                    ImGui.TableSetupColumn("Order", ImGuiTableColumnFlags.None, 1f);
                                    ImGui.TableHeadersRow();

                                    var actions = orderableDB.ActionList.ToArray();
                                    for (int i = 0; i < actions.Length; i++)
                                    {
                                        ImGui.TableNextColumn();
                                        ImGui.Text((i + 1).ToString());
                                        ImGui.TableNextColumn();
                                        ImGui.Text(actions[i].Name);
                                        if (ImGui.IsItemHovered())
                                        {
                                            ImGui.BeginTooltip();
                                            ImGui.Text("IsRunning: " + actions[i].IsRunning);
                                            ImGui.Text("IsFinished: " + actions[i].IsFinished());
                                            ImGui.EndTooltip();
                                        }
                                    }

                                    ImGui.EndTable();
                                }
                            }
                        }
                        ImGui.EndChild();
                    }
                    ImGui.SameLine();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Issue Orders"))
                {
                    var size = ImGui.GetContentRegionAvail();
                    var firstChildSize = new Vector2(size.X * 0.27f, size.Y);
                    var secondChildSize = new Vector2(size.X * 0.73f - (size.X * 0.01f), size.Y);
                    if(ImGui.BeginChild("IssueOrders-List", firstChildSize, true))
                    {
                        DisplayHelpers.Header("Available Orders");

                        if(ImGui.Selectable("Move to ...", selectedIssueOrderType == IssueOrderType.MoveTo))
                        {
                            selectedIssueOrderType = IssueOrderType.MoveTo;
                        }
                        if(SelectedFleet.HasGeoSurveyAbility() && ImGui.Selectable("Geo Survey ...", selectedIssueOrderType == IssueOrderType.GeoSurvey))
                        {
                            selectedIssueOrderType = IssueOrderType.GeoSurvey;
                        }
                        if(SelectedFleet.HasJPSurveyAbililty() && ImGui.Selectable("Jump Point Survey ...", selectedIssueOrderType == IssueOrderType.JPSurvey))
                        {
                            selectedIssueOrderType = IssueOrderType.JPSurvey;
                        }
                        if(ImGui.Selectable("Jump...", selectedIssueOrderType == IssueOrderType.Jump))
                        {
                            selectedIssueOrderType = IssueOrderType.Jump;
                        }

                        ImGui.EndChild();
                    }
                    ImGui.SameLine();
                    IssueOrdersDisplay(secondChildSize);
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
                        if(selectedFleetDB?.StandingOrders.Count > 0)
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
                            selectedFleetDB?.StandingOrders.Add(order);

                            // if this is the first order, select it
                            if(selectedFleetDB?.StandingOrders.Count == 1)
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
                            ImGui.PushID(conditionItem.UniqueID);
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
                                        ComparisonType? comparisonType = (ComparisonType?)Enum.GetValues(typeof(ComparisonType)).GetValue(comparisonIndex);
                                        if(comparisonType != null)
                                            comparisonCondition.ComparisionType = comparisonType.Value;
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
                                var selectedAction = OrderRegistry.Actions[orderActionDescriptions[orderActionsIndex]](factionID, SelectedFleet);
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

        private void IssueOrdersDisplay(Vector2 size)
        {
            if(ImGui.BeginChild("IssueOrders", size, true))
            {
                if(SelectedFleet == null || SelectedFleet.Manager == null)
                {
                    ImGui.EndChild();
                    return;
                }

                var bodies = SelectedFleet.Manager.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>(_uiState.Faction.Id);
                switch(selectedIssueOrderType)
                {
                    case IssueOrderType.MoveTo:
                        foreach(var body in bodies)
                        {
                            var name = body.GetName(_uiState.Faction.Id);
                            if(ImGui.Button(name + "###movement-button-" + name))
                            {
                                var order = MoveToSystemBodyOrder.CreateCommand(_uiState.Faction.Id, SelectedFleet, body);
                                _uiState.Game.OrderHandler.HandleOrder(order);
                            }
                        }
                        break;
                    case IssueOrderType.GeoSurvey:
                        foreach(var body in bodies)
                        {
                            if(!body.TryGetDatablob<GeoSurveyableDB>(out var geoSurveyableDB)) continue;
                            if(geoSurveyableDB.IsSurveyComplete(_uiState.Faction.Id)) continue;

                            var name = body.GetName(_uiState.Faction.Id);
                            if(ImGui.Button(name + "###geosurvey-button-" + name))
                            {
                                var order = MoveToSystemBodyOrder.CreateCommand(_uiState.Faction.Id, SelectedFleet, body);
                                _uiState.Game.OrderHandler.HandleOrder(order);

                                var order2 = GeoSurveyOrder.CreateCommand(_uiState.Faction.Id, SelectedFleet, body);
                                _uiState.Game.OrderHandler.HandleOrder(order2);
                            }
                        }
                        break;
                    case IssueOrderType.JPSurvey:
                        var jumpPointDBs = SelectedFleet.Manager.GetAllDataBlobsOfType<JPSurveyableDB>();
                        foreach(var jpSurveyableDB in jumpPointDBs)
                        {
                            if(jpSurveyableDB.IsSurveyComplete(_uiState.Faction.Id)) continue;

                            var name = jpSurveyableDB.OwningEntity?.GetName(_uiState.Faction.Id);
                            if(ImGui.Button(name + "###jpsurvey-button-" + name))
                            {
                                if(jpSurveyableDB.OwningEntity != null)
                                {
                                    var order = MoveFleetTowardsTargetOrder.CreateCommand(SelectedFleet, jpSurveyableDB.OwningEntity);
                                    _uiState.Game.OrderHandler.HandleOrder(order);

                                    var order2 = JPSurveyOrder.CreateCommand(_uiState.Faction.Id, SelectedFleet, jpSurveyableDB.OwningEntity);
                                    _uiState.Game.OrderHandler.HandleOrder(order2);
                                }
                            }
                        }
                        break;
                    case IssueOrderType.Jump:
                        var jumpGates = SelectedFleet.Manager.GetAllDataBlobsOfType<JumpPointDB>();
                        foreach(var jumpGateDB in jumpGates)
                        {
                            if(!jumpGateDB.IsDiscovered.Contains(_uiState.Faction.Id)) continue;

                            var name = jumpGateDB.OwningEntity?.GetName(_uiState.Faction.Id);
                            if(ImGui.Button(name + "###jump-gate-button-" + name))
                            {
                                if(jumpGateDB.OwningEntity != null)
                                {
                                    var order = MoveFleetTowardsTargetOrder.CreateCommand(SelectedFleet, jumpGateDB.OwningEntity);
                                    _uiState.Game.OrderHandler.HandleOrder(order);

                                    JumpOrder.CreateAndExecute(_uiState.Game, _uiState.Faction, SelectedFleet, jumpGateDB);
                                }
                            }
                        }
                        break;
                }

                ImGui.EndChild();
            }
        }

        private void DisplayShips()
        {
            if(SelectedFleet == null) return;

            var xPosition = ImGui.GetCursorPosX();
            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            if (ImGui.BeginChild("FleetSummary2", new Vector2(Styles.LeftColumnWidthLg, windowContentSize.Y - 24f), true))
            {
                DisplayHelpers.Header("Assigned Ships");

                ImGui.PushStyleColor(ImGuiCol.FrameBg, Styles.InvisibleColor);
                if (ImGui.BeginListBox("###assigned-ships", ImGui.GetContentRegionAvail()))
                {
                    var fleet = SelectedFleet.GetDataBlob<FleetDB>();
                    foreach (var ship in fleet.GetChildren())
                    {
                        // Only display ships
                        if (ship.HasDataBlob<FleetDB>()) continue;

                        if (!selectedShips.ContainsKey(ship))
                        {
                            selectedShips.Add(ship, false);
                        }

                        string name = ship.GetName(factionID);
                        if (fleet.FlagShipID == ship.Id)
                        {
                            name = "(F) " + name;
                        }
                        if (ImGui.Selectable(name, selectedShips[ship], ImGuiSelectableFlags.SpanAllColumns))
                        {
                            selectedShips[ship] = !selectedShips[ship];
                        }
                        DisplayHelpers.ShipTooltip(ship, factionID);
                        DisplayShipContextMenu(selectedShips, ship);
                    }
                    ImGui.EndListBox();
                }
                ImGui.PopStyleColor();
                ImGui.EndChild();
            }

            ImGui.SetCursorPosY(ImGui.GetCursorPosY());
            ImGui.SetCursorPosX(xPosition);
            if(ImGui.Button("Select All/None", new Vector2(Styles.LeftColumnWidthLg, 0f)))
            {
                bool selectAll = !selectedShips.Values.Any(v => v == true);
                foreach(var (ship, selected) in selectedShips)
                {
                    selectedShips[ship] = selectAll;
                }
            }
        }

        private void DisplayFleetList()
        {
            if(factionRoot == null) return;

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

                        if(ImGui.Selectable(ship.GetName(factionID), selectedUnattachedShips[ship]))
                        {
                            selectedUnattachedShips[ship] = !selectedUnattachedShips[ship];
                        }
                        DisplayHelpers.ShipTooltip(ship, factionID);
                        DisplayShipContextMenu(selectedUnattachedShips, ship, isUnattached: true);
                    }
                }

                ImGui.EndChild();
            }

            if(ImGui.Button("Create New Fleet", new Vector2(Styles.LeftColumnWidthLg, 0f)))
            {
                string name = NameFactory.GetFleetName(_uiState.Game);
                var order = FleetOrder.CreateFleetOrder(name, _uiState.Faction, _uiState.SelectedSystem);
                _uiState.Game.OrderHandler.HandleOrder(order);
            }
        }

        private void DisplayFleetItem(Entity fleet)
        {
            if(!fleet.TryGetDatablob<FleetDB>(out var fleetInfo))
            {
                return;
            }

            ImGui.PushID(fleet.Id.ToString());
            string name = fleet.GetName(factionID);
            var flags = ImGuiTreeNodeFlags.DefaultOpen;

            if(fleetInfo.GetChildren().Where(x => x.HasDataBlob<FleetDB>()).Count() == 0)
            {
                flags |= ImGuiTreeNodeFlags.Leaf;
            }

            if(SelectedFleet == fleet)
            {
                flags |= ImGuiTreeNodeFlags.Selected;
            }

            string description = "";

            fleet.TryGetDatablob<OrderableDB>(out var orderableDB);

            if(orderableDB == null || orderableDB.ActionList.Count == 0)
            {
                description = "No Orders";
            }
            else
            {
                foreach(var order in orderableDB.ActionList)
                {
                    description += order.Name + "\n";
                }
            }

            bool isTreeOpen = ImGui.TreeNodeEx(name, flags);
            if(ImGui.IsItemHovered())
                DisplayHelpers.DescriptiveTooltip(name, "Fleet", description);

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
                if(ImGui.MenuItem("Rename"))
                {
                    RenameWindow.GetInstance().SetEntity(fleet);
                    RenameWindow.GetInstance().SetActive(true);
                }
                ImGui.Separator();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.TerribleColor);
                if(ImGui.MenuItem("Disband###delete-" + fleet.Id))
                {
                    var order = FleetOrder.DisbandFleet(factionID, fleet);
                    _uiState.Game.OrderHandler.HandleOrder(order);
                    SelectFleet(null);
                }
                ImGui.PopStyleColor();
                ImGui.EndPopup();
            }
        }

        private void DisplayShipContextMenu(Dictionary<Entity, bool> selected, Entity ship, bool isUnattached = false)
        {
            if(SelectedFleet == null || factionRoot == null) return;

            if(ImGui.BeginPopupContextItem())
            {
                if(ImGui.MenuItem("View Ship"))
                {
                    _uiState.EntityClicked(ship.Id, _uiState.SelectedStarSysGuid, MouseButtons.Primary);
                }
                if(!isUnattached)
                {
                    if(selectedFleetFlagship != null && ship.Id == selectedFleetFlagship.Id)
                    {
                        ImGui.BeginDisabled();
                    }
                    if(ImGui.MenuItem("Promote to Flagship"))
                    {
                        var setFlagshipOrder = FleetOrder.SetFlagShip(factionID, SelectedFleet, ship);
                        _uiState.Game.OrderHandler.HandleOrder(setFlagshipOrder);
                        SelectFleet(SelectedFleet);
                    }
                    if(selectedFleetFlagship != null && ship.Id == selectedFleetFlagship.Id)
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
            if(!fleet.HasDataBlob<FleetDB>()
                || factionRoot == null
                || factionRoot.OwningEntity == null
                || SelectedFleet == null)
                return;

            for(int i = 0; i < depth; i++)
            {
                ImGui.InvisibleButton("invis", new Vector2(8, 8));
                ImGui.SameLine();
            }

            if(fleet == SelectedFleet && !isUnattached)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.Text(fleet.GetName(factionID));
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushID(fleet.Id.ToString());
                if(ImGui.MenuItem(fleet.GetName(factionID)))
                {
                    if(!selected.Any(x => x.Value))
                    {
                        var unassignFrom = factionRoot.Children.Contains(ship) ? factionRoot.OwningEntity : SelectedFleet;
                        var unassignOrder = FleetOrder.UnassignShip(factionID, unassignFrom, ship);
                        _uiState.Game.OrderHandler.HandleOrder(unassignOrder);

                        var assignOrder = FleetOrder.AssignShip(factionID, fleet, ship);
                        _uiState.Game.OrderHandler.HandleOrder(assignOrder);
                    }
                    else
                    {
                        foreach(var (selectedShip, isSelected) in selected)
                        {
                            if(!isSelected) continue;

                            var unassignFrom = factionRoot.Children.Contains(selectedShip) ? factionRoot.OwningEntity : SelectedFleet;
                            var unassignOrder = FleetOrder.UnassignShip(factionID, unassignFrom, selectedShip);
                            _uiState.Game.OrderHandler.HandleOrder(unassignOrder);

                            var assignOrder = FleetOrder.AssignShip(factionID, fleet, selectedShip);
                            _uiState.Game.OrderHandler.HandleOrder(assignOrder);
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
                    if(factionRoot != null && factionRoot.OwningEntity !=null)
                    {
                        var order = FleetOrder.ChangeParent(factionID, dragEntity, factionRoot.OwningEntity);
                        _uiState.Game.OrderHandler.HandleOrder(order);
                    }
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
                    _uiState.Game.OrderHandler.HandleOrder(order);
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

        private void DisplayActionItem(EntityCommand action)
        {
            ImGui.PushID(action.GetHashCode());
            var size = ImGui.GetContentRegionAvail();
            ImGui.Text(OrderRegistry.ActionDescriptions[action.GetType()]);
            ImGui.SameLine();
            ImGui.SetCursorPosX(size.X - 12f);
            if(ImGui.Button("x"))
            {
                selectedOrder?.Actions.Remove(action);
            }
            ImGui.PopID();
        }
    }
}