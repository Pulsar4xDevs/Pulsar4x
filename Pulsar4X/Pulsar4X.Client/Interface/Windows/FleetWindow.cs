using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client
{
    public class FleetWindow : UniquePulsarGuiWindow<FleetWindow>
    {
        private enum IssueOrderType
        {
            MoveTo,
            GeoSurvey,
            GravSurvey,
            MineAsteroids,
            Jump,
            RefuelAt,
            Trade,
            Haul,
            HaulContract,
            MoveGoods,
            Bootstrap,
            LoadGoods,
            UnloadGoods,
            PlotWarpTo,
            PlotCircularise,
            PlotChangeAltitude,
            PlotMatchOrbit,
        }

        private enum OrderBand
        {
            Ship,
            Well,
            System,
        }

        private IssueOrderType selectedIssueOrderType = IssueOrderType.MoveTo;
        private OrderBand selectedBand = OrderBand.Ship;
        // The selected row is a manual plot. A goal row clears it. The agent does not replan these.
        private bool plotting;
        private int goodsSourceId = -1;
        private int goodsDestId = -1;
        private int bootSourceId = -1;
        private int bootDestId = -1;
        private readonly Dictionary<string, int> bootCounts = new();
        private int loadColonyId = -1;
        private int unloadColonyId = -1;
        private int goodsSeedSource = int.MinValue;
        private int loadSeedSource = int.MinValue;
        private int unloadSeedShip = int.MinValue;
        private readonly Dictionary<int, int> goodsAmounts = new();
        private readonly Dictionary<int, int> loadAmounts = new();
        private readonly Dictionary<int, int> unloadAmounts = new();
        private double plotAltitudeM;

        private int? selectedFleetId = null;
        // The ship whose orders the right-hand pane is editing. Null means the selected fleet.
        private int? orderShipId = null;
        // Re-selects the first root fleet after connect/faction change, mirroring the old default selection.
        private bool autoSelectFirstFleet = true;
        private int dragFleetId = -1;
        private readonly List<int> dragShipIds = new();
        private Dictionary<int, bool> selectedShips = new ();
        private Dictionary<int, bool> selectedUnattachedShips = new ();

        /// <summary>The id of the fleet this window is managing, or null when none is selected.</summary>
        public int? SelectedFleetId => selectedFleetId;

        // The snapshot of the selected fleet, re-resolved each frame from the galaxy model (fleet
        // pushes replace the whole tree, so cached FleetSnapshot references go stale).
        private FleetSnapshot? selectedFleet = null;
        private ShipSnapshot? orderShip = null;
        // The ship whose captain chair the assign modal is editing. A fleet commander is the flagship's captain.
        private int? captainChooserShipId = null;
        private ColonyConstructionDisplay? _shipConstruction;
        // Log tab scrolls to the latest line when it opens or the book grows.
        private int _logShownId = -1;
        private int _logShownCount = -1;

        // ----- Standing Orders editor -----
        // The editor works on a local copy of the fleet's StandingOrders snapshot; Save replaces
        // the fleet's whole list with one SetStandingOrdersCommand.

        private sealed class StandingOrderEdit
        {
            public byte[] NameBuffer = new byte[32];
            public List<StandingOrderConditionEdit> Conditions = new();
            public List<string> Actions = new();
        }

        private sealed class StandingOrderConditionEdit
        {
            public string ConditionType = "";
            public StandingOrderComparison Comparison;
            public float Threshold;
            /// <summary>How this condition combines with the next one.</summary>
            public StandingOrderLogic Logic = StandingOrderLogic.And;
        }

        // Display registry for the contract's StandingOrderTypes ids.
        private static readonly (string Id, string Label)[] StandingOrderActionTypes =
        {
            (StandingOrderTypes.MoveToNearestColony, "Move to Nearest Colony"),
            (StandingOrderTypes.MoveToNearestGeoSurvey, "Move to Nearest Geo Survey"),
            (StandingOrderTypes.MoveToNearestAnomaly, "Move to Nearest Anomaly"),
            (StandingOrderTypes.Refuel, "Refuel"),
            (StandingOrderTypes.Resupply, "Resupply"),
        };

        private static readonly (string Id, string Label, string Description, float Min, float Max)[] StandingOrderConditionTypes =
        {
            (StandingOrderTypes.FuelCondition, "Fuel (Fleet Avg)", "percent", 0, 100),
        };

        private static readonly string[] orderComparisons = { "<", "<=", "=", ">", ">=" };

        private List<StandingOrderEdit>? editedOrders;
        private IReadOnlyList<StandingOrder>? editedOrdersSource;
        private bool standingOrdersDirty;
        private int selectedOrderIndex = -1;
        private int orderActionsIndex = 0;
        private int orderConditionsIndex = 0;

        private FleetWindow()
        {
            _uiState.OnFactionChanged += FactionChanged;
        }
        internal static FleetWindow GetInstance()
        {
            if(_uiState.TryGetUniqueWindow<FleetWindow>(out var window))
            {
                return window;
            }

            return _uiState.AddUniqueWindow(new FleetWindow());
        }

        private void FactionChanged(GlobalUIState uiState)
        {
            SelectFleet(null);
            autoSelectFirstFleet = true;
        }

        public void SelectFleet(int? fleetId)
        {
            selectedFleetId = fleetId;
            selectedShips = new ();
            orderShipId = null;
            autoSelectFirstFleet = false;
            editedOrders = null;
            editedOrdersSource = null;
            standingOrdersDirty = false;
            selectedOrderIndex = -1;
            captainChooserShipId = null;
        }

        private static FleetSnapshot? FindFleet(IReadOnlyList<FleetSnapshot> fleets, int fleetId)
        {
            foreach(var fleet in fleets)
            {
                if(fleet.Id == fleetId) return fleet;
                if(FindFleet(fleet.SubFleets, fleetId) is { } nested) return nested;
            }
            return null;
        }

        /// <summary>
        /// The pane follows this ship. The fleet row is no longer the selection, so an unassigned
        /// ship is not shown beside a fleet that is also selected.
        /// </summary>
        internal void OrderShip(int shipId)
        {
            orderShipId = shipId;
            selectedFleetId = null;
            autoSelectFirstFleet = false;
            captainChooserShipId = null;
            plotAltitudeM = 0;
            SetActive(true);
        }

        private static ShipSnapshot? FindShip(IClientGalaxy galaxy, int shipId)
        {
            foreach (var ship in galaxy.UnattachedShips)
            {
                if (ship.Id == shipId)
                    return ship;
            }
            return FindShip(galaxy.Fleets, shipId);
        }

        private static ShipSnapshot? FindShip(IReadOnlyList<FleetSnapshot> fleets, int shipId)
        {
            foreach (var fleet in fleets)
            {
                foreach (var ship in fleet.Ships)
                {
                    if (ship.Id == shipId)
                        return ship;
                }
                if (FindShip(fleet.SubFleets, shipId) is { } nested)
                    return nested;
            }
            return null;
        }

        static bool ShipHasCaptain(ShipSnapshot ship) => ship.CommanderId != null;

        static string CaptainLabel(ShipSnapshot ship)
        {
            if (!ShipHasCaptain(ship))
                return "None";
            return string.IsNullOrEmpty(ship.CommanderName) ? "Unknown" : ship.CommanderName!;
        }

        static string CaptainTooltip(ShipSnapshot ship)
            => ShipHasCaptain(ship)
                ? "Replace this captain, or choose None and plot the ship's actions yourself."
                : "No captain. Choose an officer, or leave None and plot this ship's actions yourself.";

        static bool HasGoal(GoalSnapshot? goal) => goal != null && goal.Name.Length > 0;

        static bool CanCancelGoal(GoalSnapshot? goal)
        {
            if (goal == null || goal.PlayerCancelled || goal.Name.Length == 0)
                return false;
            return goal.Status is "Planning" or "Active" or "Holding" or "Failing";
        }

        internal override void Display()
        {
            if(!IsActive) return;

            var galaxy = _uiState.GameClient?.Galaxy;
            if(galaxy == null) return;

            orderShip = orderShipId is { } shipId ? FindShip(galaxy, shipId) : null;
            if (orderShipId != null && orderShip == null)
                orderShipId = null;

            if(autoSelectFirstFleet && orderShip == null && galaxy.Fleets.Count > 0)
            {
                SelectFleet(galaxy.Fleets[0].Id);
            }

            // Resolve the selection against the current push; a disbanded fleet drops the selection.
            selectedFleet = selectedFleetId is { } id ? FindFleet(galaxy.Fleets, id) : null;

            ImGui.SetNextWindowSize(new Vector2(900, 600), ImGuiCond.FirstUseEver);
            if(Window.Begin("Fleets and Ships", ref IsActive, _flags))
            {
                DisplayFleetList(galaxy);

                if(selectedFleet != null || orderShip != null)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosY(27f);
                    DisplayTabs(galaxy);
                }

                DisplayCaptainChooser(galaxy);
            }
            Window.End();
        }

        private void DisplayTabs(IClientGalaxy galaxy)
        {
            if(selectedFleet == null && orderShip == null) return;

            if(ImGui.BeginChild("FleetTabs"))
            {
                ImGui.BeginTabBar("FleetTabBar", ImGuiTabBarFlags.None);

                if(ImGui.BeginTabItem("Summary"))
                {
                    if (orderShip != null)
                    {
                        DisplayShipSummary(galaxy);
                    }
                    else if (selectedFleet != null)
                    {
                        Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                        var firstChildSize = new Vector2(windowContentSize.X * 0.99f, windowContentSize.Y);
                        if (ImGui.BeginChild("FleetSummary1", firstChildSize, ImGuiChildFlags.Borders))
                        {
                            if (ImGui.CollapsingHeader("Fleet Information", ImGuiTreeNodeFlags.DefaultOpen))
                            {
                                ImGui.Columns(2);
                                DisplayHelpers.PrintRow("Name", selectedFleet.Name);
                                DisplayHelpers.PrintRow("Flagship", selectedFleet.FlagshipName ?? "-");
                                DisplayFleetCommanderRow();
                                DisplayHelpers.PrintRow("Command span", SpanLine(),
                                    tooltipTwo: "The narrower of the flagship's bridge and its captain's rank.");

                                // Current system
                                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                                ImGui.Text("Current System");
                                ImGui.PopStyleColor();
                                ImGui.NextColumn();
                                if (ImGui.SmallButton(selectedFleet.SystemName ?? "Unknown"))
                                {
                                    if(selectedFleet.SystemId != null)
                                        _uiState.SetActiveSystem(selectedFleet.SystemId);
                                }
                                ImGui.NextColumn();
                                ImGui.Separator();

                                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                                ImGui.Text("Orbiting");
                                ImGui.PopStyleColor();
                                ImGui.NextColumn();
                                // The server already resolved this to the nearest faction-visible ancestor
                                // (hidden entities like un-surveyed anomalies are skipped).
                                if (ImGui.SmallButton(selectedFleet.OrbitingName ?? "Unknown"))
                                {
                                    if(selectedFleet.OrbitingEntityId is { } orbitingId && selectedFleet.SystemId != null)
                                        _uiState.EntityClicked(orbitingId, selectedFleet.SystemId, MouseButtons.Primary);
                                }
                                ImGui.NextColumn();
                                ImGui.Separator();
                                DisplayHelpers.PrintRow("Ships", selectedFleet.Ships.Count.ToString());
                            }
                            ImGui.Columns(1);
                        }
                        ImGui.EndChild();
                    }
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Orders"))
                {
                    var size = ImGui.GetContentRegionAvail();
                    var column = new Vector2(size.X * 0.33f, size.Y);
                    var formSize = new Vector2(size.X * 0.33f - (size.X * 0.01f), size.Y);
                    if (ImGui.BeginChild("Orders-Queue", column, ImGuiChildFlags.Borders))
                        DisplayOrders();
                    ImGui.EndChild();
                    ImGui.SameLine();
                    if (ImGui.BeginChild("IssueOrders-List", column, ImGuiChildFlags.Borders))
                        DisplayOrderChoices(galaxy);
                    ImGui.EndChild();
                    ImGui.SameLine();
                    IssueOrdersDisplay(galaxy, formSize);
                    ImGui.EndTabItem();
                }

                if (orderShip != null
                    && SubjectEntity(galaxy)?.GetView<ConstructionView>() is { IsShip: true } construction
                    && ImGui.BeginTabItem("Construction"))
                {
                    _shipConstruction ??= new ColonyConstructionDisplay();
                    _shipConstruction.Display(orderShip.Id, construction, _uiState);
                    ImGui.EndTabItem();
                }

                if (orderShip == null)
                    DisplayStandingOrdersTab();

                if (ImGui.BeginTabItem("Log"))
                {
                    ShipLogDisplay.Draw(LogSubject(galaxy), ref _logShownId, ref _logShownCount);
                    ImGui.EndTabItem();
                }
                else
                {
                    _logShownId = -1;
                }

                ImGui.EndTabBar();
            }
            ImGui.EndChild();
        }

        private void DisplayShipSummary(IClientGalaxy galaxy)
        {
            if (orderShip == null) return;
            var ship = orderShip;
            string systemId = string.IsNullOrEmpty(ship.SystemId) ? _uiState.SelectedStarSystemId : ship.SystemId;
            string systemName = string.IsNullOrEmpty(systemId) ? "Unknown" : galaxy.GetSystem(systemId)?.Name ?? "Unknown";
            var subject = SubjectEntity(galaxy);

            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            var firstChildSize = new Vector2(windowContentSize.X * 0.33f, windowContentSize.Y);
            var secondChildSize = new Vector2(windowContentSize.X * 0.33f, windowContentSize.Y);
            var thirdChildSize = new Vector2(windowContentSize.X * 0.33f - (windowContentSize.X * 0.01f), windowContentSize.Y);

            if (ImGui.BeginChild("ShipSummary1", firstChildSize, ImGuiChildFlags.Borders))
            {
                if (ImGui.CollapsingHeader("Ship", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Columns(2);
                    DisplayHelpers.PrintRow("Name", ship.Name);
                    DisplayHelpers.PrintRow("Design", string.IsNullOrEmpty(ship.DesignName) ? "-" : ship.DesignName);
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.Text("Captain");
                    ImGui.PopStyleColor();
                    ImGui.NextColumn();
                    DisplayCaptainButton(ship.Id, CaptainLabel(ship), CaptainTooltip(ship));
                    ImGui.NextColumn();
                    ImGui.Separator();
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.Text("Current System");
                    ImGui.PopStyleColor();
                    ImGui.NextColumn();
                    if (ImGui.SmallButton(systemName))
                    {
                        if (!string.IsNullOrEmpty(systemId))
                            _uiState.SetActiveSystem(systemId);
                    }
                    ImGui.NextColumn();
                    ImGui.Columns(1);
                }

                if (ImGui.CollapsingHeader("Crew", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    int required = subject?.GetView<ShipView>()?.CrewRequired ?? 0;
                    ImGui.Columns(2);
                    DisplayHelpers.PrintRow("Required", required.ToString("N0"), separator: false);
                    ImGui.Columns(1);
                }

                // Output is the net added to the batteries this step. The old power window's Load
                // is max output divided by that net, so it is not a percentage and is not shown.
                if (subject?.GetView<EnergyView>() is { } energy
                    && ImGui.CollapsingHeader("Power", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    bool full = energy.StoreMax > 0 && energy.Stored >= energy.StoreMax * 0.999;
                    // The last charge step can leave Output positive after the battery is already full.
                    // Nothing further is stored until a warp runs the energy step again.
                    string status = "Holding";
                    if (energy.Output < -0.001)
                        status = "Draining";
                    else if (full)
                        status = "Full";
                    else if (energy.Output > 0.001)
                        status = "Charging";

                    double fraction = energy.StoreMax > 0 ? energy.Stored / energy.StoreMax : 0;
                    if (fraction < 0) fraction = 0;
                    if (fraction > 1) fraction = 1;
                    string batteries = (fraction * 100).ToString("0.#") + "%  "
                        + Stringify.Energy(energy.Stored < 0 ? 0 : energy.Stored) + " / "
                        + Stringify.Energy(energy.StoreMax < 0 ? 0 : energy.StoreMax);
                    double flowKw = full && energy.Output > 0 ? 0 : energy.Output;
                    string flow = "0 kW";
                    if (flowKw > 0.001)
                        flow = "+" + Stringify.Power(flowKw);
                    else if (flowKw < -0.001)
                        flow = "-" + Stringify.Power(-flowKw);

                    ImGui.ProgressBar((float)fraction, new Vector2(ImGui.GetContentRegionAvail().X, 16), batteries);
                    ImGui.Columns(2);
                    DisplayHelpers.PrintRow("Status", status, "Charging fills the batteries. Draining means demand is above generation.");
                    DisplayHelpers.PrintRow("Flow", flow, "Spare or shortfall added to the batteries this step.");
                    DisplayHelpers.PrintRow("Demand", Stringify.Power(energy.Demand < 0 ? 0 : energy.Demand), "What the ship's components are drawing.");
                    DisplayHelpers.PrintRow("Generation", Stringify.Power(energy.MaxOutput < 0 ? 0 : energy.MaxOutput), "Reactor and solar capacity.", separator: false);
                    ImGui.Columns(1);
                }
            }
            ImGui.EndChild();

            ImGui.SameLine();
            if (ImGui.BeginChild("ShipSummary2", secondChildSize, ImGuiChildFlags.Borders))
            {
                if (ImGui.CollapsingHeader("Components", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    var components = subject?.GetView<InstallationsView>();
                    if (components == null || components.Installations.Count == 0)
                        ImGui.Text("No components on this ship.");
                    else
                        components.Display(ship.Id, _uiState);
                }
            }
            ImGui.EndChild();

            ImGui.SameLine();
            if (ImGui.BeginChild("ShipSummary3", thirdChildSize, ImGuiChildFlags.Borders))
            {
                if (subject?.GetView<CargoStorageView>() is { } cargo && cargo.Stores.Count > 0)
                {
                    if (!string.IsNullOrEmpty(ship.SystemId))
                    {
                        var size = ImGui.GetContentRegionAvail();
                        ImGui.PushStyleColor(ImGuiCol.Button, Styles.Theme.Button.ToImVector4());
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Styles.Theme.ButtonHovered.ToImVector4());
                        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Styles.Theme.ButtonActive.ToImVector4());
                        if (ImGui.Button("Initiate Transfer", new Vector2(size.X - 8, 18)))
                        {
                            var transfer = CreateTransferWindow.GetInstance();
                            transfer.SetLeft(ship.Id, ship.SystemId);
                            transfer.SetActive(true);
                        }
                        ImGui.PopStyleColor(3);
                    }
                    cargo.Display(ship.Id, _uiState);
                }
            }
            ImGui.EndChild();
        }

        private void DisplayOrderChoices(IClientGalaxy galaxy)
        {
            if (orderShip != null && !ShipHasCaptain(orderShip))
            {
                DisplayHelpers.Header("Actions");
                ImGui.TextWrapped("No captain. You plot this ship's actions. The queue does not replan.");
                DisplayManualActions(galaxy);
                return;
            }

            bool geo = orderShip != null ? orderShip.CanGeoSurvey : selectedFleet?.CanGeoSurvey == true;
            bool grav = orderShip != null ? orderShip.CanGravSurvey : selectedFleet?.CanGravSurvey == true;
            bool mine = orderShip != null ? orderShip.CanMineAsteroids : selectedFleet?.CanMineAsteroids == true;
            bool manual = orderShip != null && plotting;

            DisplayBand(OrderBand.Ship, () =>
            {
                ImGui.TextWrapped(FreightSentence(OrderBand.Ship));
                GoalRow("Move to ...", IssueOrderType.MoveTo, OrderBand.Ship, manual);
                GoalRow("Refuel at ...", IssueOrderType.RefuelAt, OrderBand.Ship, manual);
                GoalRow("Jump...", IssueOrderType.Jump, OrderBand.Ship, manual);
                GoalRow("Trade ...", IssueOrderType.Trade, OrderBand.Ship, manual);
                GoalRow("Haul ...", IssueOrderType.Haul, OrderBand.Ship, manual);
                GoalRow("Move goods ...", IssueOrderType.MoveGoods, OrderBand.Ship, manual);
                if (BootstrapShip)
                    GoalRow("Bootstrap colony ...", IssueOrderType.Bootstrap, OrderBand.Ship, manual);
                if (mine && ImGui.Selectable("Mine asteroids###mine-ship", selectedIssueOrderType == IssueOrderType.MineAsteroids && !manual && selectedBand == OrderBand.Ship))
                    Choose(IssueOrderType.MineAsteroids, plot: false, OrderBand.Ship);
                if (geo)
                    GoalRow("Geo Survey ...", IssueOrderType.GeoSurvey, OrderBand.Ship, manual);
                if (grav)
                    GoalRow("Grav Survey ...", IssueOrderType.GravSurvey, OrderBand.Ship, manual);
                if (orderShip != null && ImGui.CollapsingHeader("Actions"))
                {
                    ImGui.TextWrapped("Queue a plot on this ship. The captain does not replan it.");
                    DisplayManualActions(galaxy);
                }
            });
            DisplayBand(OrderBand.Well, () =>
            {
                ImGui.TextWrapped(FreightSentence(OrderBand.Well));
                if (geo)
                    GoalRow("Geo Survey ...", IssueOrderType.GeoSurvey, OrderBand.Well, manual);
                if (grav)
                    GoalRow("Grav Survey ...", IssueOrderType.GravSurvey, OrderBand.Well, manual);
                GoalRow("Trade ...", IssueOrderType.Trade, OrderBand.Well, manual);
                GoalRow("Haul ...", IssueOrderType.Haul, OrderBand.Well, manual);
                GoalRow("Move goods ...", IssueOrderType.MoveGoods, OrderBand.Well, manual);
                if (BootstrapShip)
                    GoalRow("Bootstrap colony ...", IssueOrderType.Bootstrap, OrderBand.Well, manual);
            });
            DisplayBand(OrderBand.System, () =>
            {
                ImGui.TextWrapped(FreightSentence(OrderBand.System));
                if (geo)
                    GoalRow("Geo Survey ...", IssueOrderType.GeoSurvey, OrderBand.System, manual);
                if (grav)
                    GoalRow("Grav Survey ...", IssueOrderType.GravSurvey, OrderBand.System, manual);
                GoalRow("Trade ...", IssueOrderType.Trade, OrderBand.System, manual);
                GoalRow("Haul ...", IssueOrderType.Haul, OrderBand.System, manual);
                GoalRow("Move goods ...", IssueOrderType.MoveGoods, OrderBand.System, manual);
                if (BootstrapShip)
                    GoalRow("Bootstrap colony ...", IssueOrderType.Bootstrap, OrderBand.System, manual);
            });
        }

        void DisplayBand(OrderBand band, Action body)
        {
            bool openBand = BandOpen(band);
            if (!openBand)
                ImGui.BeginDisabled();
            ImGui.SetNextItemOpen(true, ImGuiCond.Once);
            bool open = ImGui.CollapsingHeader(BandTitle(band) + "###band-" + band);
            if (!openBand && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(SpanLine());
            if (open)
                body();
            if (!openBand)
                ImGui.EndDisabled();
        }

        static string BandTitle(OrderBand band) => band switch
        {
            OrderBand.Well => "This well",
            OrderBand.System => "This system",
            _ => "This ship",
        };

        void GoalRow(string label, IssueOrderType type, OrderBand band, bool manual)
        {
            bool selected = !manual && selectedIssueOrderType == type && selectedBand == band;
            if (ImGui.Selectable(label + "###" + type + "-" + band, selected))
                Choose(type, plot: false, band);
        }

        void Choose(IssueOrderType type, bool plot, OrderBand band = OrderBand.Ship)
        {
            selectedIssueOrderType = type;
            plotting = plot;
            if (!plot)
                selectedBand = band;
        }

        // Bootstrap colony is one ship's order: a captained hull with a Construction Bay. A fleet never gets it.
        bool BootstrapShip => orderShip != null && ShipHasCaptain(orderShip) && orderShip.HasConstructionBay;

        bool ManualPlot => orderShip != null && (!ShipHasCaptain(orderShip) || plotting);

        void DisplayManualActions(IClientGalaxy galaxy)
        {
            var entity = SubjectEntity(galaxy);
            bool warp = entity?.HasView<WarpAbilityView>() == true;
            bool thrust = entity?.HasView<ThrustView>() == true;
            bool plotMove = ManualPlot && selectedIssueOrderType == IssueOrderType.MoveTo;
            bool plotGeo = ManualPlot && selectedIssueOrderType == IssueOrderType.GeoSurvey;
            bool plotGrav = ManualPlot && selectedIssueOrderType == IssueOrderType.GravSurvey;
            bool plotJump = ManualPlot && selectedIssueOrderType == IssueOrderType.Jump;
            bool plotGoods = ManualPlot && selectedIssueOrderType == IssueOrderType.MoveGoods;
            bool plotRefuel = ManualPlot && selectedIssueOrderType == IssueOrderType.RefuelAt;
            if (ImGui.Selectable("Go to ...", plotMove))
                Choose(IssueOrderType.MoveTo, plot: true);
            if (warp && ImGui.Selectable("Warp to ...", selectedIssueOrderType == IssueOrderType.PlotWarpTo))
                Choose(IssueOrderType.PlotWarpTo, plot: true);
            if (thrust && ImGui.Selectable("Circularise", selectedIssueOrderType == IssueOrderType.PlotCircularise))
                Choose(IssueOrderType.PlotCircularise, plot: true);
            if (thrust && ImGui.Selectable("Change altitude ...", selectedIssueOrderType == IssueOrderType.PlotChangeAltitude))
                Choose(IssueOrderType.PlotChangeAltitude, plot: true);
            if (thrust && ImGui.Selectable("Match orbit ...", selectedIssueOrderType == IssueOrderType.PlotMatchOrbit))
                Choose(IssueOrderType.PlotMatchOrbit, plot: true);
            var geoSite = CurrentGeoSurveySite(galaxy);
            if (geoSite != null && ImGui.Selectable("Geo survey " + NameOf(geoSite), plotGeo))
                Choose(IssueOrderType.GeoSurvey, plot: true);
            var gravSite = CurrentGravSurveySite(galaxy);
            if (gravSite != null && ImGui.Selectable("Grav survey " + NameOf(gravSite), plotGrav))
                Choose(IssueOrderType.GravSurvey, plot: true);
            // Jump on a captained ship is the Goals row. The same command, so it is not listed again here.
            var ship = orderShip;
            if (ship != null && !ShipHasCaptain(ship) && ImGui.Selectable("Jump...", plotJump))
                Choose(IssueOrderType.Jump, plot: true);
            if (ImGui.Selectable("Move goods (plot) ...", plotGoods))
                Choose(IssueOrderType.MoveGoods, plot: true);
            if (ImGui.Selectable("Load at colony in range", selectedIssueOrderType == IssueOrderType.LoadGoods))
                Choose(IssueOrderType.LoadGoods, plot: true);
            if (ImGui.Selectable("Unload at colony in range", selectedIssueOrderType == IssueOrderType.UnloadGoods))
                Choose(IssueOrderType.UnloadGoods, plot: true);
            if (HasFuel(galaxy) && ImGui.Selectable("Refuel", plotRefuel))
                Choose(IssueOrderType.RefuelAt, plot: true);
            if (orderShip != null && warp && ImGui.Selectable("Warp plotter..."))
                OpenPlotter(orderShip, warp: true);
            if (orderShip != null && thrust && ImGui.Selectable("Nav plotter..."))
                OpenPlotter(orderShip, warp: false);
        }

        private EntitySnapshot? LogSubject(IClientGalaxy galaxy)
        {
            int? id = orderShip?.Id ?? selectedFleet?.Id;
            string? systemId = SubjectSystemId();
            if (id == null || string.IsNullOrEmpty(systemId))
                return null;
            return galaxy.GetSystem(systemId)?.GetEntity(id.Value);
        }

        private EntitySnapshot? SubjectEntity(IClientGalaxy galaxy)
        {
            string? systemId = SubjectSystemId();
            if (string.IsNullOrEmpty(systemId)) return null;
            int? id = orderShip?.Id ?? selectedFleet?.Id;
            // The fleet entity is not what warp and nav plot. A ship subject resolves the hull.
            if (orderShip == null) return null;
            return galaxy.GetSystem(systemId)?.GetEntity(id!.Value);
        }

        const double GravSurveyRangeM = 100_000;

        EntitySnapshot? CurrentGeoSurveySite(IClientGalaxy galaxy)
        {
            if (orderShip is not { CanGeoSurvey: true }) return null;
            var entity = SubjectEntity(galaxy);
            var system = galaxy.GetSystem(SubjectSystemId() ?? "");
            if (entity == null || system == null) return null;
            var parent = entity.GetSoiParent(system);
            return parent?.GetView<GeoSurveyView>() is { IsSurveyComplete: false } ? parent : null;
        }

        EntitySnapshot? CurrentGravSurveySite(IClientGalaxy galaxy)
        {
            if (orderShip is not { CanGravSurvey: true }) return null;
            var entity = SubjectEntity(galaxy);
            var system = galaxy.GetSystem(SubjectSystemId() ?? "");
            if (entity == null || system == null) return null;
            var parent = entity.GetSoiParent(system);
            if (parent?.GetView<GravSurveyView>() is { IsSurveyComplete: false })
                return parent;
            var shipPos = entity.GetView<PositionView>();
            if (shipPos == null) return null;
            EntitySnapshot? best = null;
            double bestD = GravSurveyRangeM;
            foreach (var candidate in system.Entities)
            {
                if (candidate.GetView<GravSurveyView>() is not { IsSurveyComplete: false })
                    continue;
                var pos = candidate.GetView<PositionView>();
                if (pos == null) continue;
                double d = DistanceM(shipPos.AbsolutePosition, pos.AbsolutePosition);
                if (d < bestD)
                {
                    bestD = d;
                    best = candidate;
                }
            }
            return best;
        }

        static double DistanceM(Vec3 a, Vec3 b)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y, dz = a.Z - b.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        void DisplayPlotSurveyHere(IClientGalaxy galaxy, int shipId, bool geo)
        {
            var site = geo ? CurrentGeoSurveySite(galaxy) : CurrentGravSurveySite(galaxy);
            if (site == null)
            {
                ImGui.TextWrapped(geo
                    ? "Not in orbit of an unsurveyed body."
                    : "Not at an unsurveyed gravitational anomaly.");
                return;
            }
            string name = NameOf(site);
            ImGui.TextWrapped("Survey " + name + " from here. The queue does not replan.");
            if (ImGui.Button(geo ? "Queue geo survey" : "Queue grav survey"))
            {
                if (geo)
                    SubmitFleetCommand(new GeoSurveyCommand(shipId, site.Id));
                else
                    SubmitFleetCommand(new GravSurveyCommand(shipId, site.Id));
            }
        }

        private string? SubjectSystemId()
        {
            if (orderShip != null)
                return string.IsNullOrEmpty(orderShip.SystemId) ? _uiState.SelectedStarSystemId : orderShip.SystemId;
            return selectedFleet?.SystemId;
        }

        private void OpenPlotter(ShipSnapshot ship, bool warp)
        {
            string systemId = string.IsNullOrEmpty(ship.SystemId) ? _uiState.SelectedStarSystemId : ship.SystemId;
            var snapshot = _uiState.GameClient?.Galaxy.GetSystem(systemId)?.GetEntity(ship.Id);
            if (snapshot == null || string.IsNullOrEmpty(systemId)) return;
            var state = new EntityState(snapshot, systemId);
            if (warp)
            {
                var window = WarpOrderWindow.GetInstance(state);
                window.SetActive(true);
                _uiState.ActiveWindow = window;
            }
            else
            {
                var window = NavWindow.GetInstance(state);
                window.SetActive(true);
                _uiState.ActiveWindow = window;
            }
        }

        private void IssueOrdersDisplay(IClientGalaxy galaxy, Vector2 size)
        {
            if(ImGui.BeginChild("IssueOrders", size, ImGuiChildFlags.Borders))
            {
                int? commanded = orderShip?.Id ?? selectedFleet?.Id;
                var systemId = SubjectSystemId();
                var system = string.IsNullOrEmpty(systemId) ? null : galaxy.GetSystem(systemId);
                if(commanded == null || system == null || _uiState.GameClient == null)
                {
                    ImGui.EndChild();
                    return;
                }

                DisplayCommandLine();

                bool plot = ManualPlot;
                if (orderShip != null)
                {
                    bool atGeo = CurrentGeoSurveySite(galaxy) != null;
                    bool atGrav = CurrentGravSurveySite(galaxy) != null;
                    bool shipGeo = orderShip.CanGeoSurvey && selectedIssueOrderType == IssueOrderType.GeoSurvey
                        && (!plot || atGeo);
                    bool shipGrav = orderShip.CanGravSurvey && selectedIssueOrderType == IssueOrderType.GravSurvey
                        && (!plot || atGrav);
                    bool shipMine = orderShip.CanMineAsteroids
                        && selectedIssueOrderType == IssueOrderType.MineAsteroids
                        && !plot;
                    bool shipPlot = plot && (selectedIssueOrderType == IssueOrderType.PlotWarpTo
                        || selectedIssueOrderType == IssueOrderType.PlotCircularise
                        || selectedIssueOrderType == IssueOrderType.PlotChangeAltitude
                        || selectedIssueOrderType == IssueOrderType.PlotMatchOrbit);
                    bool shipGoods = selectedIssueOrderType == IssueOrderType.MoveGoods
                        || (plot && (selectedIssueOrderType == IssueOrderType.LoadGoods
                            || selectedIssueOrderType == IssueOrderType.UnloadGoods));
                    bool shipRefuel = selectedIssueOrderType == IssueOrderType.RefuelAt;
                    bool shipBootstrap = selectedIssueOrderType == IssueOrderType.Bootstrap
                        && !plot && BootstrapShip;
                    bool shipRun = !plot && (selectedIssueOrderType == IssueOrderType.Trade
                        || selectedIssueOrderType == IssueOrderType.Haul);
                    bool shipGoal = selectedIssueOrderType == IssueOrderType.MoveTo
                        || selectedIssueOrderType == IssueOrderType.Jump
                        || shipGeo || shipGrav || shipMine || shipPlot || shipGoods
                        || shipRefuel || shipRun || shipBootstrap;
                    if (!shipGoal)
                    {
                        plotting = false;
                        plot = false;
                        selectedIssueOrderType = IssueOrderType.MoveTo;
                    }
                }
                else if (plotting
                    || selectedIssueOrderType == IssueOrderType.Bootstrap
                    || selectedIssueOrderType == IssueOrderType.LoadGoods
                    || selectedIssueOrderType == IssueOrderType.UnloadGoods
                    || selectedIssueOrderType == IssueOrderType.PlotWarpTo
                    || selectedIssueOrderType == IssueOrderType.PlotCircularise
                    || selectedIssueOrderType == IssueOrderType.PlotChangeAltitude
                    || selectedIssueOrderType == IssueOrderType.PlotMatchOrbit)
                {
                    plotting = false;
                    plot = false;
                    selectedIssueOrderType = IssueOrderType.MoveTo;
                }

                // Mirror the old EntityFilter.Friendly | EntityFilter.Neutral read: hostiles aren't targets.
                var candidates = system.Entities.Where(e => e.Relation != OwnerRelation.Hostile);

                if (!plot && !BandOpen(selectedBand))
                {
                    ImGui.TextWrapped(SpanLine());
                    ImGui.EndChild();
                    return;
                }

                bool bandWell = selectedBand != OrderBand.Ship;
                bool bandSystem = selectedBand == OrderBand.System;
                string bandSpan = BandSpanLabel(selectedBand);

                switch(selectedIssueOrderType)
                {
                    case IssueOrderType.MoveTo:
                        if (plot)
                        {
                            ImGui.TextWrapped("Queues a one-shot plot. The ship will not rethink it.");
                            DisplayTargetTree(candidates,
                                e => e.HasView<BodyView>() && e.HasView<PositionView>(),
                                "movement-button",
                                id => SubmitFleetCommand(new GoToBodyCommand(commanded.Value, id)),
                                starTargetsSystem: false);
                        }
                        else
                        {
                            DisplayTargetTree(candidates,
                                e => e.HasView<BodyView>() && e.HasView<PositionView>(),
                                "movement-button",
                                id => SubmitFleetCommand(new MoveToBodyCommand(commanded.Value, id)),
                                starTargetsSystem: false);
                        }
                        break;
                    case IssueOrderType.PlotWarpTo:
                        ImGui.TextWrapped("Warp to the body and circularise. Same drop-in as a captain's Move to.");
                        DisplayTargetTree(candidates,
                            e => e.HasView<BodyView>() && e.HasView<PositionView>(),
                            "warp-to-button",
                            id => SubmitFleetCommand(new WarpToBodyCommand(commanded.Value, id)),
                            starTargetsSystem: false);
                        break;
                    case IssueOrderType.PlotCircularise:
                        ImGui.TextWrapped("Circularise around the current parent at the current radius.");
                        if (ImGui.Button("Queue circularise"))
                            SubmitFleetCommand(new CirculariseCommand(commanded.Value));
                        break;
                    case IssueOrderType.PlotChangeAltitude:
                        DisplayChangeAltitude(galaxy, commanded.Value);
                        break;
                    case IssueOrderType.PlotMatchOrbit:
                        ImGui.TextWrapped("Rendezvous onto this body's orbit around the shared parent.");
                        DisplayTargetTree(candidates,
                            e => e.HasView<BodyView>() && e.HasView<PositionView>(),
                            "match-orbit-button",
                            id => SubmitFleetCommand(new MatchOrbitCommand(commanded.Value, id)),
                            starTargetsSystem: false);
                        break;
                    case IssueOrderType.GeoSurvey:
                        if (plot)
                            DisplayPlotSurveyHere(galaxy, commanded.Value, geo: true);
                        else
                            DisplayTargetTree(candidates,
                                e => e.GetView<GeoSurveyView>() is { IsSurveyComplete: false },
                                "geosurvey-button",
                                id => SubmitFleetCommand(new GeoSurveyCommand(commanded.Value, id)),
                                starTargetsSystem: bandWell,
                                keepParentWhenChildrenRemain: bandWell,
                                markDone: e => e.GetView<GeoSurveyView>()?.IsSurveyComplete == true,
                                spanLabel: bandSpan);
                        break;
                    case IssueOrderType.GravSurvey:
                        if (plot)
                            DisplayPlotSurveyHere(galaxy, commanded.Value, geo: false);
                        else
                            DisplayTargetTree(candidates,
                                e => e.GetView<GravSurveyView>() is { IsSurveyComplete: false },
                                "gravsurvey-button",
                                id => SubmitFleetCommand(new GravSurveyCommand(commanded.Value, id)),
                                starTargetsSystem: bandWell,
                                keepParentWhenChildrenRemain: bandWell,
                                markDone: e => e.GetView<GravSurveyView>()?.IsSurveyComplete == true,
                                spanLabel: bandSpan);
                        break;
                    case IssueOrderType.MineAsteroids:
                        ImGui.TextWrapped("Mine surveyed asteroids in this system. The ship fills its hold, then unloads at the nearest colony that can take the ore.");
                        if (ImGui.Button("Mine asteroids"))
                            SubmitFleetCommand(new MineAsteroidsCommand(commanded.Value));
                        break;
                    case IssueOrderType.Jump:
                        // The server only projects a JumpPointView once this faction has discovered it.
                        DisplayTargetTree(candidates,
                            e => e.HasView<JumpPointView>(),
                            "jump-gate-button",
                            id => SubmitFleetCommand(new JumpCommand(commanded.Value, id)),
                            starTargetsSystem: false);
                        break;
                    case IssueOrderType.RefuelAt:
                        if (plot)
                        {
                            DisplayRefuelHere(galaxy, candidates, commanded.Value);
                            break;
                        }
                        DisplayTargetTree(candidates,
                            e => e.Kind == BodyKind.Colony && e.HasView<CargoStorageView>(),
                            "refuelAt-button",
                            id => SubmitFleetCommand(new RefuelAtCommand(commanded.Value, id)),
                            starTargetsSystem: false);
                        break;
                    case IssueOrderType.Trade:
                        if (orderShip != null)
                            DisplayFreightPairs(galaxy, candidates, trade: true);
                        else if (selectedFleet != null)
                            DisplayFleetTrade(galaxy, candidates);
                        break;
                    case IssueOrderType.Haul:
                    case IssueOrderType.HaulContract:
                        DisplayFreightPairs(galaxy, candidates, trade: false);
                        break;
                    case IssueOrderType.MoveGoods:
                        DisplayMoveGoods(galaxy, candidates, commanded.Value, plot ? null : selectedBand);
                        break;
                    case IssueOrderType.Bootstrap:
                        if (BootstrapShip)
                            DisplayBootstrap(galaxy, candidates, orderShip!, selectedBand);
                        break;
                    case IssueOrderType.LoadGoods:
                        DisplayLoadGoods(candidates, commanded.Value);
                        break;
                    case IssueOrderType.UnloadGoods:
                        DisplayUnloadGoods(galaxy, candidates, commanded.Value);
                        break;
                }
            }
            ImGui.EndChild();
        }

        private string FleetCommanderLabel()
        {
            if(selectedFleet == null || selectedFleet.FlagshipName == null)
                return "-";
            return string.IsNullOrEmpty(selectedFleet.CommanderName) ? "None" : selectedFleet.CommanderName;
        }

        private void DisplayFleetCommanderRow()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.Text("Fleet commander");
            ImGui.PopStyleColor();
            ImGui.NextColumn();
            DisplayFleetCommanderValue();
            ImGui.NextColumn();
            ImGui.Separator();
        }

        private void DisplayFleetCommanderValue()
        {
            if (selectedFleet?.FlagshipId is not int flagshipId)
            {
                ImGui.Text("-");
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("A fleet commander is the flagship's captain. This fleet has no flagship.");
                return;
            }

            string name = selectedFleet.FlagshipName ?? "the flagship";
            DisplayCaptainButton(flagshipId, FleetCommanderLabel(),
                "Seats a navy officer on " + name + ". That captain commands this fleet.");
        }

        private void DisplayCaptainButton(int shipId, string label, string tooltip)
        {
            if (ImGui.SmallButton(label + "###captain-" + shipId))
                captainChooserShipId = shipId;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
        }

        private void DisplayCaptainChooser(IClientGalaxy galaxy)
        {
            if (captainChooserShipId is not int shipId)
                return;

            var ship = FindShip(galaxy, shipId);
            if (ship == null)
            {
                captainChooserShipId = null;
                return;
            }

            var navy = galaxy.Commanders
                .Where(commander => commander.Kind == CommanderKind.Navy)
                .OrderBy(commander => commander.Name, StringComparer.Ordinal)
                .ToList();
            int current = ship.CommanderId ?? -1;
            ResultModal.GetInstance().DisplayCustomButtons(
                "Assign Captain",
                () => captainChooserShipId = null,
                close =>
                {
                    int selected = DisplayHelpers.PeopleChooser(
                        _uiState,
                        navy,
                        current,
                        "captain_" + shipId,
                        close,
                        OfficerRow,
                        320f);
                    if (selected != current)
                    {
                        SubmitFleetCommand(new AssignCaptainCommand(shipId, selected));
                        close();
                    }
                });
        }

        private static string OfficerRow(CommanderSnapshot person)
        {
            string rank = string.IsNullOrEmpty(person.RankName) ? "" : person.RankName + " ";
            string post = string.IsNullOrEmpty(person.AssignmentName) ? "" : ", " + person.AssignmentName;
            return rank + person.Name + post;
        }

        private string SubjectSpanLabel()
        {
            if(orderShip != null && !string.IsNullOrEmpty(orderShip.CommandSpan))
                return orderShip.CommandSpan;
            if(selectedFleet != null && !string.IsNullOrEmpty(selectedFleet.CommandSpan))
                return selectedFleet.CommandSpan;
            return CommandSpanLabels.Body;
        }

        private string SpanLine()
        {
            if(orderShip != null && !string.IsNullOrEmpty(orderShip.SpanLine))
                return orderShip.SpanLine;
            if(selectedFleet != null && !string.IsNullOrEmpty(selectedFleet.SpanLine))
                return selectedFleet.SpanLine;
            return "Body. This bridge covers this ship. No officer is seated.";
        }

        static int SpanRank(string label)
        {
            if(label == CommandSpanLabels.Well)
                return 1;
            if(label == CommandSpanLabels.System || label == CommandSpanLabels.Neighbor)
                return 2;
            return 0;
        }

        bool BandOpen(OrderBand band) => SpanRank(SubjectSpanLabel()) >= (int)band;

        static string BandSpanLabel(OrderBand band) => band switch
        {
            OrderBand.Well => CommandSpanLabels.Well,
            OrderBand.System => CommandSpanLabels.System,
            _ => CommandSpanLabels.Body,
        };

        static string FreightSentence(OrderBand band) => band switch
        {
            OrderBand.Well => "Haul any two colonies in this star system.",
            OrderBand.System => "One end may sit in a system one jump away.",
            _ => "Haul two bodies in this well.",
        };

        private IReadOnlyList<string> LogisticsIds()
        {
            if(orderShip?.LogisticsSystemIds is { Count: > 1 } shipIds)
                return shipIds;
            if(selectedFleet?.LogisticsSystemIds is { Count: > 1 } fleetIds)
                return fleetIds;
            return Array.Empty<string>();
        }

        private IEnumerable<EntitySnapshot> LogisticsCandidates(IClientGalaxy galaxy, IEnumerable<EntitySnapshot> here)
        {
            var ids = LogisticsIds();
            if(ids.Count <= 1)
                return here;

            var list = new List<EntitySnapshot>();
            var seen = new HashSet<int>();
            foreach(var id in ids)
            {
                var system = galaxy.GetSystem(id);
                if(system == null)
                    continue;
                foreach(var entity in system.Entities)
                {
                    if(entity.Relation == OwnerRelation.Hostile || !seen.Add(entity.Id))
                        continue;
                    list.Add(entity);
                }
            }
            return list.Count > 0 ? list : here;
        }

        private void DisplayCommandLine()
        {
            if (orderShip != null)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.Text("Captain");
                ImGui.PopStyleColor();
                ImGui.SameLine();
                DisplayCaptainButton(orderShip.Id, CaptainLabel(orderShip), CaptainTooltip(orderShip));
            }
            else
            {
                ImGui.AlignTextToFramePadding();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.Text("Fleet commander");
                ImGui.PopStyleColor();
                ImGui.SameLine();
                DisplayFleetCommanderValue();
            }
            ImGui.TextWrapped(SpanLine());
            ImGui.Separator();
        }

        private static string NameOf(EntitySnapshot entity) => entity.GetView<NameView>()?.Name ?? "";

        private static int? ParentIdOf(EntitySnapshot body)
        {
            if(body.GetView<OrbitView>()?.ParentId is int orbitParent && orbitParent != body.Id)
                return orbitParent;
            if(body.GetView<PositionView>()?.ParentId is int positionParent && positionParent != body.Id)
                return positionParent;
            if(body.GetView<ColonyView>()?.PlanetEntityId is int planetId && planetId != body.Id)
                return planetId;
            return null;
        }

        /// <summary>
        /// One row in the order tree. Bands are not entities: they only fold asteroids that orbit a star.
        /// </summary>
        private sealed class OrderTreeNode
        {
            public EntitySnapshot? Entity;
            public int Id;
            public int StarId;
            public bool IsBand;
            public string Label = "";
            /// <summary>Orbital slot in km for a band. Entity rows compute their slot when sorted.</summary>
            public double SortKey;
        }

        private void DisplayChangeAltitude(IClientGalaxy galaxy, int shipId)
        {
            ImGui.TextWrapped("Hohmann to a circular orbit around the current parent. Circularise first if the leftover is eccentric.");
            var entity = SubjectEntity(galaxy);
            var system = galaxy.GetSystem(SubjectSystemId() ?? "");
            var parent = entity != null && system != null ? entity.GetSoiParent(system) : null;
            if (plotAltitudeM <= 0 && parent != null)
                plotAltitudeM = parent.LowOrbitRadiusM();
            float km = (float)(plotAltitudeM / 1000.0);
            if (ImGui.InputFloat("Orbit radius (km)", ref km))
                plotAltitudeM = km * 1000.0;
            if (parent != null)
            {
                ImGui.SameLine();
                if (ImGui.SmallButton("Low orbit"))
                    plotAltitudeM = parent.LowOrbitRadiusM();
            }
            if (ImGui.Button("Queue change altitude") && plotAltitudeM > 0)
                SubmitFleetCommand(new ChangeAltitudeCommand(shipId, plotAltitudeM));
        }

        /// <summary>
        /// Star at the root, then targets nested by orbit. Siblings are ordered by distance from
        /// their sun. Asteroids that orbit a star fold into one branch per gap between that star's
        /// planets. The star is a button only for orders that use it as a whole-system anchor,
        /// and only when the flagship bridge covers the system. Parentless targets (grav rings,
        /// jump points) hang under the nearest star. When <paramref name="keepParentWhenChildrenRemain"/>
        /// is set, a finished body stays in the tree while anything under it is still a target,
        /// and stays a button when the flagship bridge covers that well or the whole system.
        /// </summary>
        private void DisplayTargetTree(
            IEnumerable<EntitySnapshot> candidates,
            Func<EntitySnapshot, bool> isTarget,
            string buttonPrefix,
            Action<int> submit,
            bool starTargetsSystem,
            bool keepParentWhenChildrenRemain = false,
            Func<EntitySnapshot, bool>? markDone = null,
            int? selectedId = null,
            string? spanLabel = null,
            Func<EntitySnapshot, string?>? blocked = null)
        {
            string span = spanLabel ?? SubjectSpanLabel();
            var byId = new Dictionary<int, EntitySnapshot>();
            foreach(var entity in candidates)
                byId[entity.Id] = entity;

            var included = new Dictionary<int, EntitySnapshot>();
            foreach(var entity in byId.Values)
            {
                if(entity.Kind == BodyKind.Star || isTarget(entity))
                    IncludeWithAncestors(entity, byId, included);
            }

            var stars = new List<EntitySnapshot>();
            foreach(var entity in included.Values)
            {
                if(entity.Kind == BodyKind.Star)
                    stars.Add(entity);
            }

            var entityChildren = new Dictionary<int, List<EntitySnapshot>>();
            var parentOf = new Dictionary<int, int>();
            var rootEntities = new List<EntitySnapshot>();
            foreach(var entity in included.Values)
            {
                int? parentId = ParentIdOf(entity);
                if(parentId is int pid && pid != entity.Id && included.ContainsKey(pid))
                {
                    AddTreeChild(entityChildren, parentOf, pid, entity);
                }
                else if(parentId is null && entity.Kind != BodyKind.Star && stars.Count > 0)
                {
                    var star = NearestStar(entity, stars);
                    AddTreeChild(entityChildren, parentOf, star.Id, entity);
                }
                else
                {
                    rootEntities.Add(entity);
                }
            }

            var children = new Dictionary<int, List<OrderTreeNode>>();
            foreach(var pair in entityChildren)
            {
                var nodes = new List<OrderTreeNode>(pair.Value.Count);
                foreach(var entity in pair.Value)
                    nodes.Add(new OrderTreeNode { Entity = entity, Id = entity.Id });
                children[pair.Key] = nodes;
            }

            var roots = new List<OrderTreeNode>(rootEntities.Count);
            foreach(var entity in rootEntities)
                roots.Add(new OrderTreeNode { Entity = entity, Id = entity.Id });

            FoldAsteroidBands(children, parentOf, stars);

            var coversWork = new HashSet<int>();
            if(keepParentWhenChildrenRemain)
            {
                foreach(var entity in included.Values)
                {
                    if(!isTarget(entity))
                        continue;
                    var current = entity.Id;
                    while(parentOf.TryGetValue(current, out var parent))
                    {
                        if(!coversWork.Add(parent))
                            break;
                        current = parent;
                    }
                }
            }

            foreach(var root in SortNodes(roots, stars, null))
                DisplayTargetNode(root, children, stars, isTarget, coversWork, buttonPrefix, submit, starTargetsSystem, markDone, selectedId, span, blocked);
        }

        private static void AddTreeChild(Dictionary<int, List<EntitySnapshot>> children, Dictionary<int, int> parentOf, int parentId, EntitySnapshot child)
        {
            if(!children.TryGetValue(parentId, out var list))
            {
                list = new List<EntitySnapshot>();
                children[parentId] = list;
            }
            list.Add(child);
            parentOf[child.Id] = parentId;
        }

        /// <summary>
        /// Pull asteroids that orbit a star out of that star's child list and into one closed
        /// branch per gap between its planets. A gap with a single asteroid stays a normal row.
        /// Membership uses semi-major axis, so a rock does not change branch as it orbits.
        /// </summary>
        private static void FoldAsteroidBands(
            Dictionary<int, List<OrderTreeNode>> children,
            Dictionary<int, int> parentOf,
            List<EntitySnapshot> stars)
        {
            foreach(var star in stars)
            {
                if(!children.TryGetValue(star.Id, out var kids) || kids.Count == 0)
                    continue;

                var asteroids = new List<OrderTreeNode>();
                var rest = new List<OrderTreeNode>();
                foreach(var kid in kids)
                {
                    if(kid.Entity?.Kind == BodyKind.Asteroid)
                        asteroids.Add(kid);
                    else
                        rest.Add(kid);
                }
                if(asteroids.Count < 2)
                    continue;

                var planets = new List<(OrderTreeNode node, double slot)>();
                foreach(var kid in rest)
                {
                    if(kid.Entity?.Kind == BodyKind.Planet)
                        planets.Add((kid, OrbitSlotKm(kid.Entity, star)));
                }
                planets.Sort((a, b) => a.slot.CompareTo(b.slot));

                var groups = new Dictionary<int, List<OrderTreeNode>>();
                foreach(var rock in asteroids)
                {
                    int gap = GapIndex(OrbitSlotKm(rock.Entity!, star), planets);
                    if(!groups.TryGetValue(gap, out var group))
                    {
                        group = new List<OrderTreeNode>();
                        groups[gap] = group;
                    }
                    group.Add(rock);
                }

                var folded = new List<OrderTreeNode>(rest);
                foreach(var pair in groups)
                {
                    var members = pair.Value;
                    if(members.Count < 2)
                    {
                        folded.AddRange(members);
                        continue;
                    }

                    var band = new OrderTreeNode
                    {
                        Id = BandId(star.Id, pair.Key),
                        IsBand = true,
                        StarId = star.Id,
                        Label = BandLabel(pair.Key, planets, members.Count),
                        SortKey = MedianSlotKm(members, star),
                    };
                    folded.Add(band);
                    children[band.Id] = members;
                    parentOf[band.Id] = star.Id;
                    foreach(var member in members)
                        parentOf[member.Id] = band.Id;
                }
                children[star.Id] = folded;
            }
        }

        private static int GapIndex(double slotKm, List<(OrderTreeNode node, double slot)> planets)
        {
            for(int i = 0; i < planets.Count; i++)
            {
                if(slotKm < planets[i].slot)
                    return i;
            }
            return planets.Count;
        }

        private static string BandLabel(int gap, List<(OrderTreeNode node, double slot)> planets, int count)
        {
            string name;
            if(planets.Count == 0)
                name = "Asteroids";
            else if(gap <= 0)
                name = "Inside " + DisplayName(planets[0].node.Entity!);
            else if(gap >= planets.Count)
                name = "Beyond " + DisplayName(planets[planets.Count - 1].node.Entity!);
            else
                name = DisplayName(planets[gap - 1].node.Entity!) + " – " + DisplayName(planets[gap].node.Entity!);
            return name + " (" + count + ")";
        }

        private static string DisplayName(EntitySnapshot entity)
        {
            string name = NameOf(entity);
            return name.Length == 0 ? "Unknown" : name;
        }

        private static double MedianSlotKm(List<OrderTreeNode> members, EntitySnapshot star)
        {
            var slots = new List<double>(members.Count);
            foreach(var member in members)
                slots.Add(OrbitSlotKm(member.Entity!, star));
            slots.Sort();
            return slots[slots.Count / 2];
        }

        /// <summary>Negative, stable for a star and gap. Entity ids start at 0.</summary>
        private static int BandId(int starId, int gap)
        {
            long packed = ((long)starId + 1L) * 1024L + gap + 1L;
            if(packed > int.MaxValue)
                packed = packed % int.MaxValue + 1L;
            return (int)-packed;
        }

        /// <summary>
        /// Semi-major axis when the body orbits this star. Otherwise its current distance.
        /// </summary>
        private static double OrbitSlotKm(EntitySnapshot entity, EntitySnapshot star)
        {
            if(entity.GetView<OrbitView>() is { } orbit
                && orbit.ParentId == star.Id
                && orbit.SemiMajorAxisKm > 0)
                return orbit.SemiMajorAxisKm;

            if(entity.GetView<PositionView>() is { } pos
                && star.GetView<PositionView>() is { } starPos)
                return Math.Sqrt(DistanceSquared(pos.AbsolutePosition, starPos.AbsolutePosition)) / 1000.0;

            return double.MaxValue;
        }

        private static void IncludeWithAncestors(EntitySnapshot entity, Dictionary<int, EntitySnapshot> byId, Dictionary<int, EntitySnapshot> included)
        {
            var current = entity;
            for(int guard = 0; guard < 32 && included.TryAdd(current.Id, current); guard++)
            {
                if(ParentIdOf(current) is not int parentId || parentId == current.Id)
                    break;
                if(!byId.TryGetValue(parentId, out var parent) || !IsOrbitalBody(parent))
                    break;
                current = parent;
            }
        }

        private static bool IsOrbitalBody(EntitySnapshot entity)
            => entity.Kind is BodyKind.Star or BodyKind.Planet or BodyKind.DwarfPlanet
                or BodyKind.Moon or BodyKind.Asteroid or BodyKind.Comet;

        private static EntitySnapshot NearestStar(EntitySnapshot entity, List<EntitySnapshot> stars)
        {
            if(stars.Count == 1 || entity.GetView<PositionView>() is not { } pos)
                return stars[0];

            var best = stars[0];
            double bestDistance = double.MaxValue;
            foreach(var star in stars)
            {
                if(star.Id == entity.Id || star.GetView<PositionView>() is not { } starPos)
                    continue;
                double distance = DistanceSquared(pos.AbsolutePosition, starPos.AbsolutePosition);
                if(distance < bestDistance)
                {
                    bestDistance = distance;
                    best = star;
                }
            }
            return best;
        }

        private static double DistanceFromSun(EntitySnapshot entity, List<EntitySnapshot> stars)
        {
            if(entity.GetView<PositionView>() is not { } pos)
                return entity.Kind == BodyKind.Star ? 0 : double.MaxValue;

            double best = double.MaxValue;
            bool found = false;
            foreach(var star in stars)
            {
                if(star.Id == entity.Id || star.GetView<PositionView>() is not { } starPos)
                    continue;
                double distance = DistanceSquared(pos.AbsolutePosition, starPos.AbsolutePosition);
                if(distance < best)
                {
                    best = distance;
                    found = true;
                }
            }
            if(found)
                return best;
            return entity.Kind == BodyKind.Star
                ? 0
                : DistanceSquared(pos.AbsolutePosition, default);
        }

        private static double DistanceSquared(Vec3 a, Vec3 b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            double dz = a.Z - b.Z;
            return dx * dx + dy * dy + dz * dz;
        }

        private static List<OrderTreeNode> SortNodes(List<OrderTreeNode> nodes, List<EntitySnapshot> stars, EntitySnapshot? aroundStar)
            => nodes
                .OrderBy(n => n.Entity?.Kind == BodyKind.Star ? 0 : 1)
                .ThenBy(n => n.IsBand
                    ? n.SortKey
                    : aroundStar != null
                        ? OrbitSlotKm(n.Entity!, aroundStar)
                        : DistanceFromSun(n.Entity!, stars))
                .ThenBy(n => n.IsBand ? n.Label : NameOf(n.Entity!), StringComparer.Ordinal)
                .ToList();

        private void DisplayTargetNode(
            OrderTreeNode node,
            Dictionary<int, List<OrderTreeNode>> children,
            List<EntitySnapshot> stars,
            Func<EntitySnapshot, bool> isTarget,
            HashSet<int> coversWork,
            string buttonPrefix,
            Action<int> submit,
            bool starTargetsSystem,
            Func<EntitySnapshot, bool>? markDone,
            int? selectedId,
            string span,
            Func<EntitySnapshot, string?>? blocked = null)
        {
            // Refuel, load, and unload nest the colony under its planet. That planet starts closed,
            // which hides the only button. Keep the path open so the colony stays on screen.
            bool revealColony = buttonPrefix is "refuel-from" or "load-from" or "unload-to";
            if(node.IsBand)
            {
                if(revealColony)
                    ImGui.SetNextItemOpen(true, ImGuiCond.Always);
                bool bandOpen = ImGui.TreeNodeEx($"{node.Label}###{buttonPrefix}-band-{node.Id}");
                if(bandOpen)
                {
                    if(children.TryGetValue(node.Id, out var bandKids))
                    {
                        EntitySnapshot? star = null;
                        foreach(var candidate in stars)
                        {
                            if(candidate.Id == node.StarId)
                            {
                                star = candidate;
                                break;
                            }
                        }
                        foreach(var child in SortNodes(bandKids, stars, star))
                            DisplayTargetNode(child, children, stars, isTarget, coversWork, buttonPrefix, submit, starTargetsSystem, markDone, selectedId, span, blocked);
                    }
                    ImGui.TreePop();
                }
                return;
            }

            EntitySnapshot entity = node.Entity!;
            List<OrderTreeNode>? childList = null;
            if(children.TryGetValue(node.Id, out var found) && found.Count > 0)
                childList = found;

            // A finished body stays in the tree while work remains under it. It is a button only
            // when this bridge includes those bodies: Well is the body's own children, System is
            // everything under it. Body span surveys the clicked body alone, which is already done.
            bool systemSpan = span == CommandSpanLabels.System || span == CommandSpanLabels.Neighbor;
            bool reachesChildren = systemSpan || span == CommandSpanLabels.Well;
            bool starClickable = entity.Kind == BodyKind.Star && starTargetsSystem && systemSpan;
            bool childWork = coversWork.Contains(entity.Id);
            bool showDone = childWork && !isTarget(entity) && markDone != null && markDone(entity);
            string? block = blocked?.Invoke(entity);
            bool clickable = block == null && (starClickable || (entity.Kind != BodyKind.Star && (isTarget(entity) || (showDone && reachesChildren))));

            var flags = ImGuiTreeNodeFlags.OpenOnArrow;
            if(entity.Kind == BodyKind.Star || showDone)
                flags |= ImGuiTreeNodeFlags.DefaultOpen;
            if(childList == null)
                flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
            if(revealColony)
                ImGui.SetNextItemOpen(true, ImGuiCond.Always);

            bool open = ImGui.TreeNodeEx($"##{buttonPrefix}-node-{entity.Id}", flags);
            ImGui.SameLine();
            string name = DisplayName(entity);
            string doneTooltip = systemSpan
                ? "This body is surveyed. Unsurveyed bodies under it are still included."
                : "This body is surveyed. Unsurveyed bodies in its well are still included.";
            if(clickable)
            {
                int colors = 0;
                if(showDone)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.22f, 0.28f, 0.34f, 1f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.30f, 0.40f, 0.48f, 1f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.18f, 0.32f, 0.42f, 1f));
                    colors = 3;
                }
                else if(selectedId == node.Id)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, Styles.HighlightColor);
                    colors = 1;
                }
                if(ImGui.SmallButton($"{name}###{buttonPrefix}-{node.Id}"))
                    submit(node.Id);
                if(showDone && ImGui.IsItemHovered())
                    ImGui.SetTooltip(doneTooltip);
                if(colors > 0)
                    ImGui.PopStyleColor(colors);
                if(showDone)
                {
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.Text("surveyed");
                    ImGui.PopStyleColor();
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip(doneTooltip);
                }
            }
            else
            {
                ImGui.TextDisabled(name);
                bool nameHovered = ImGui.IsItemHovered();
                if(showDone)
                {
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.Text("surveyed");
                    ImGui.PopStyleColor();
                    if(nameHovered || ImGui.IsItemHovered())
                        ImGui.SetTooltip("This body is surveyed. This bridge only covers the body you click, so pick an unsurveyed body under it.");
                }
                else if(nameHovered && !string.IsNullOrEmpty(block))
                    ImGui.SetTooltip(block);
                else if(nameHovered)
                    ImGui.SetTooltip(SpanLine());
            }

            if(childList != null && open)
            {
                EntitySnapshot? around = entity.Kind == BodyKind.Star ? entity : null;
                foreach(var child in SortNodes(childList, stars, around))
                    DisplayTargetNode(child, children, stars, isTarget, coversWork, buttonPrefix, submit, starTargetsSystem, markDone, selectedId, span, blocked);
                ImGui.TreePop();
            }
        }

        private void DisplayMoveGoods(IClientGalaxy galaxy, IEnumerable<EntitySnapshot> candidates, int commandedId, OrderBand? band)
        {
            ImGui.TextWrapped("One trip. Goods that do not fit stay behind.");
            bool widen = band == OrderBand.System;
            var pool = widen ? LogisticsCandidates(galaxy, candidates) : candidates;
            var colonies = OwnedWarehouses(pool);
            if(colonies.Count == 0)
            {
                ImGui.Text("No owned colonies with a warehouse in reach.");
                return;
            }

            var placed = Place(galaxy, pool, widen);
            bool InBand(EntitySnapshot colony, int otherId)
                => band == null || HasPartner(colony, otherId, band.Value, placed);

            if(!colonies.Any(colony => colony.Id == goodsSourceId && InBand(colony, goodsDestId)))
                goodsSourceId = -1;
            if(goodsDestId == goodsSourceId || !colonies.Any(colony => colony.Id == goodsDestId && InBand(colony, goodsSourceId)))
                goodsDestId = -1;

            string? Block(EntitySnapshot colony, int otherId)
                => band != null && OwnedWarehouse(colony) && !InBand(colony, otherId) ? SpanLine() : null;

            DisplayHelpers.Header("From");
            DisplayTargetTree(pool,
                e => OwnedWarehouse(e),
                "goods-from",
                id =>
                {
                    goodsSourceId = id;
                    if(goodsDestId == id)
                        goodsDestId = -1;
                },
                starTargetsSystem: false,
                selectedId: goodsSourceId,
                blocked: e => Block(e, goodsDestId));

            DisplayHelpers.Header("To");
            DisplayTargetTree(pool,
                e => e.Id != goodsSourceId && OwnedWarehouse(e),
                "goods-to",
                id => goodsDestId = id,
                starTargetsSystem: false,
                selectedId: goodsDestId,
                blocked: e => Block(e, goodsSourceId));

            var source = colonies.FirstOrDefault(colony => colony.Id == goodsSourceId);
            var dest = colonies.FirstOrDefault(colony => colony.Id == goodsDestId);
            if(source == null || dest == null)
                return;

            var piles = Piles(source);
            SyncAmounts(goodsAmounts, ref goodsSeedSource, source.Id, piles);
            DisplayHelpers.Header("Cargo");
            if(piles.Count == 0)
            {
                ImGui.Text("Nothing in the warehouse.");
                return;
            }
            DrawPileAmounts(goodsAmounts, piles);
            if(!ImGui.Button("Move goods"))
                return;
            var items = Chosen(goodsAmounts, piles);
            if(items.Count == 0)
                return;
            if(orderShip == null)
                SubmitFleetCommand(new FleetMoveGoodsCommand(commandedId, source.Id, dest.Id, items));
            else if(ManualPlot)
                SubmitFleetCommand(new QueueMoveGoodsCommand(commandedId, source.Id, dest.Id, items));
            else
                SubmitFleetCommand(new MoveGoodsCommand(commandedId, source.Id, dest.Id, items));
        }

        // Source, destination and crates for one captained ship. The From and To fields are Move goods':
        // the same owned-warehouse trees, the same span blocking. The option lists and checks live in
        // BootstrapPicker so they run without a window.
        private void DisplayBootstrap(IClientGalaxy galaxy, IEnumerable<EntitySnapshot> candidates, ShipSnapshot ship, OrderBand band)
        {
            ImGui.TextWrapped("One trip. The ship loads finished crates at the source, flies to the destination and mounts them from its hold with its Construction Bay.");
            bool widen = band == OrderBand.System;
            var pool = widen ? LogisticsCandidates(galaxy, candidates) : candidates;
            var colonies = pool.Where(BootstrapPicker.IsPickable).OrderBy(e => NameOf(e), StringComparer.Ordinal).ToList();
            if(colonies.Count == 0)
            {
                ImGui.Text("No owned colonies with a warehouse in reach.");
                return;
            }

            var placed = Place(galaxy, pool, widen);
            bool InBand(EntitySnapshot colony, int otherId) => HasPartner(colony, otherId, band, placed);

            if(!colonies.Any(colony => colony.Id == bootSourceId && InBand(colony, bootDestId)))
                bootSourceId = -1;
            if(bootDestId == bootSourceId || !colonies.Any(colony => colony.Id == bootDestId && InBand(colony, bootSourceId)))
                bootDestId = -1;

            string? Block(EntitySnapshot colony, int otherId)
                => BootstrapPicker.IsPickable(colony) && !InBand(colony, otherId) ? SpanLine() : null;

            DisplayHelpers.Header("From");
            DisplayTargetTree(pool,
                e => BootstrapPicker.IsPickable(e),
                "bootstrap-from",
                id =>
                {
                    bootSourceId = id;
                    if(bootDestId == id)
                        bootDestId = -1;
                },
                starTargetsSystem: false,
                selectedId: bootSourceId,
                blocked: e => Block(e, bootDestId));

            DisplayHelpers.Header("To");
            DisplayTargetTree(pool,
                e => BootstrapPicker.IsPickableDestination(e, bootSourceId),
                "bootstrap-to",
                id => bootDestId = id,
                starTargetsSystem: false,
                selectedId: bootDestId,
                blocked: e => Block(e, bootSourceId));

            var source = colonies.FirstOrDefault(colony => colony.Id == bootSourceId);
            var dest = colonies.FirstOrDefault(colony => colony.Id == bootDestId);
            if(source == null || dest == null)
                return;

            var options = BootstrapPicker.CrateOptions(source, SubjectEntity(galaxy));
            BootstrapPicker.SyncCounts(bootCounts, options);
            DisplayHelpers.Header("Crates");
            if(options.Count == 0)
            {
                ImGui.TextWrapped("No finished crates at the source or in the hold.");
                return;
            }
            foreach(var option in options)
            {
                int count = bootCounts[option.DesignId];
                int cap = option.Available > int.MaxValue ? int.MaxValue : (int)option.Available;
                ImGui.Text(option.Name);
                ImGui.SameLine();
                ImGui.Text(Stringify.Quantity(option.Available));
                ImGui.SetNextItemWidth(120);
                ImGui.InputInt("###bootstrap-" + option.DesignId, ref count);
                ImGui.SameLine();
                if(ImGui.SmallButton("All###bootstrap-all-" + option.DesignId))
                    count = cap;
                bootCounts[option.DesignId] = Math.Clamp(count, 0, cap);
            }

            var lines = BootstrapPicker.Requests(options, bootCounts);
            string? problem = BootstrapPicker.Validate(ship, source.Id, dest.Id, lines, InBand(source, dest.Id));
            if(problem != null)
                ImGui.TextWrapped(problem);
            if(problem != null)
                ImGui.BeginDisabled();
            bool send = ImGui.Button("Bootstrap colony");
            if(problem != null)
                ImGui.EndDisabled();
            if(send && problem == null)
                SubmitFleetCommand(new BootstrapColonyCommand(ship.Id, source.Id, dest.Id, lines));
        }

        bool HasFuel(IClientGalaxy galaxy)
        {
            var fuelName = SubjectEntity(galaxy)?.GetView<ThrustView>()?.FuelName;
            return !string.IsNullOrEmpty(fuelName);
        }

        private void DisplayRefuelHere(IClientGalaxy galaxy, IEnumerable<EntitySnapshot> candidates, int shipId)
        {
            string fuelName = SubjectEntity(galaxy)?.GetView<ThrustView>()?.FuelName ?? "";
            if (string.IsNullOrEmpty(fuelName))
            {
                ImGui.Text("This ship has no fuel.");
                return;
            }

            ImGui.TextWrapped("Fill the tank from a colony in cargo range. Loads " + fuelName + ".");
            var stores = SubjectEntity(galaxy)?.GetView<CargoStorageView>()?.Stores;
            var tank = stores?.FirstOrDefault(store => store.TypeId == "fuel-storage");
            if (tank == null)
            {
                ImGui.Text("This ship has no fuel tank.");
                return;
            }
            if (tank.FreeVolume <= 0)
            {
                var held = tank.Items.Where(item => item.Units > 0).Select(item => item.Name).ToList();
                string contents = held.Count == 0 ? "other cargo" : string.Join(", ", held);
                ImGui.TextWrapped("No room in the tank for " + fuelName + ". It holds " + contents + ".");
                return;
            }
            var colonies = OwnedWarehouses(candidates)
                .Where(colony => InCargoRange(colony) && FuelPile(colony, fuelName) != null)
                .ToList();
            if (colonies.Count == 0)
            {
                ImGui.Text("No colony in cargo range has " + fuelName + ".");
                return;
            }
            if (colonies.All(colony => colony.Id != loadColonyId))
                loadColonyId = colonies[0].Id;

            DisplayHelpers.Header("From");
            DisplayTargetTree(candidates,
                e => OwnedWarehouse(e) && InCargoRange(e) && FuelPile(e, fuelName) != null,
                "refuel-from",
                id => loadColonyId = id,
                starTargetsSystem: false,
                selectedId: loadColonyId);

            var source = colonies.FirstOrDefault(colony => colony.Id == loadColonyId);
            var pile = source == null ? null : FuelPile(source, fuelName);
            if (source == null || pile == null)
                return;
            ImGui.Text(pile.Name);
            ImGui.SameLine();
            ImGui.Text(Stringify.Quantity(pile.Units));
            if (!ImGui.Button("Refuel###refuel-here"))
                return;
            SubmitFleetCommand(new LoadGoodsCommand(
                shipId, source.Id, new[] { new CargoTransferItem(pile.Id, pile.Units) }));
        }

        static CargoItemView? FuelPile(EntitySnapshot colony, string fuelName)
        {
            foreach (var item in Piles(colony))
            {
                if (item.Units > 0 && string.Equals(item.Name, fuelName, StringComparison.Ordinal))
                    return item;
            }
            return null;
        }

        private void DisplayLoadGoods(IEnumerable<EntitySnapshot> candidates, int shipId)
        {
            ImGui.TextWrapped("Load from a colony in cargo range. Goods that do not fit stay behind.");
            var colonies = OwnedWarehouses(candidates).Where(InCargoRange).ToList();
            if(colonies.Count == 0)
            {
                ImGui.Text("No colony in cargo range.");
                return;
            }
            if(colonies.All(colony => colony.Id != loadColonyId))
                loadColonyId = colonies[0].Id;

            DisplayHelpers.Header("From");
            DisplayTargetTree(candidates,
                e => OwnedWarehouse(e) && InCargoRange(e),
                "load-from",
                id => loadColonyId = id,
                starTargetsSystem: false,
                selectedId: loadColonyId);

            var source = colonies.FirstOrDefault(colony => colony.Id == loadColonyId);
            if(source == null)
                return;
            var piles = Piles(source);
            SyncAmounts(loadAmounts, ref loadSeedSource, source.Id, piles);
            DisplayHelpers.Header("Cargo");
            if(piles.Count == 0)
            {
                ImGui.Text("Nothing in the warehouse.");
                return;
            }
            DrawPileAmounts(loadAmounts, piles);
            if(!ImGui.Button("Load"))
                return;
            var items = Chosen(loadAmounts, piles);
            if(items.Count == 0)
                return;
            SubmitFleetCommand(new LoadGoodsCommand(shipId, source.Id, items));
        }

        private void DisplayUnloadGoods(IClientGalaxy galaxy, IEnumerable<EntitySnapshot> candidates, int shipId)
        {
            ImGui.TextWrapped("Unload onto a colony in cargo range. Goods that do not fit stay aboard.");
            var ship = SubjectEntity(galaxy);
            var colonies = OwnedWarehouses(candidates).Where(InCargoRange).ToList();
            if(colonies.Count == 0)
            {
                ImGui.Text("No colony in cargo range.");
                return;
            }
            if(colonies.All(colony => colony.Id != unloadColonyId))
                unloadColonyId = colonies[0].Id;

            DisplayHelpers.Header("To");
            DisplayTargetTree(candidates,
                e => OwnedWarehouse(e) && InCargoRange(e),
                "unload-to",
                id => unloadColonyId = id,
                starTargetsSystem: false,
                selectedId: unloadColonyId);

            var dest = colonies.FirstOrDefault(colony => colony.Id == unloadColonyId);
            if(dest == null || ship == null)
                return;
            var piles = Piles(ship);
            SyncAmounts(unloadAmounts, ref unloadSeedShip, ship.Id, piles);
            DisplayHelpers.Header("Cargo");
            if(piles.Count == 0)
            {
                ImGui.Text("Nothing aboard.");
                return;
            }
            DrawPileAmounts(unloadAmounts, piles);
            if(!ImGui.Button("Unload"))
                return;
            var items = Chosen(unloadAmounts, piles);
            if(items.Count == 0)
                return;
            SubmitFleetCommand(new UnloadGoodsCommand(shipId, dest.Id, items));
        }

        bool InCargoRange(EntitySnapshot colony)
        {
            if(orderShip == null)
                return false;
            foreach(var id in orderShip.ColoniesInCargoRange)
            {
                if(id == colony.Id)
                    return true;
            }
            return false;
        }

        static bool OwnedWarehouse(EntitySnapshot entity)
            => entity.Relation == OwnerRelation.Owned && entity.Kind == BodyKind.Colony && entity.HasView<CargoStorageView>();

        static List<EntitySnapshot> OwnedWarehouses(IEnumerable<EntitySnapshot> candidates)
            => candidates.Where(OwnedWarehouse).OrderBy(e => NameOf(e), StringComparer.Ordinal).ToList();

        static List<CargoItemView> Piles(EntitySnapshot entity)
        {
            var piles = new List<CargoItemView>();
            if(entity.GetView<CargoStorageView>() is not { } storage)
                return piles;
            foreach(var store in storage.Stores)
            {
                foreach(var item in store.Items)
                {
                    if(item.Units > 0)
                        piles.Add(item);
                }
            }
            piles.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            return piles;
        }

        static void SyncAmounts(Dictionary<int, int> amounts, ref int seededFrom, int sourceKey, List<CargoItemView> piles)
        {
            if(seededFrom != sourceKey)
            {
                amounts.Clear();
                seededFrom = sourceKey;
            }
            var live = new HashSet<int>();
            foreach(var item in piles)
            {
                live.Add(item.Id);
                int cap = item.Units > int.MaxValue ? int.MaxValue : (int)item.Units;
                // A new pile starts at zero. The player sets the lines they want. All fills them.
                if(!amounts.ContainsKey(item.Id))
                    amounts[item.Id] = 0;
                else if(amounts[item.Id] > cap)
                    amounts[item.Id] = cap;
                else if(amounts[item.Id] < 0)
                    amounts[item.Id] = 0;
            }
            var drop = new List<int>();
            foreach(var id in amounts.Keys)
            {
                if(!live.Contains(id))
                    drop.Add(id);
            }
            foreach(var id in drop)
                amounts.Remove(id);
        }

        static void DrawPileAmounts(Dictionary<int, int> amounts, List<CargoItemView> piles)
        {
            if(ImGui.SmallButton("All###piles-all"))
            {
                foreach(var item in piles)
                    amounts[item.Id] = item.Units > int.MaxValue ? int.MaxValue : (int)item.Units;
            }
            ImGui.SameLine();
            if(ImGui.SmallButton("None###piles-none"))
            {
                foreach(var item in piles)
                    amounts[item.Id] = 0;
            }
            ImGui.SameLine();
            ImGui.Text("Amounts start at zero.");

            foreach(var item in piles)
            {
                if(!amounts.TryGetValue(item.Id, out var amount))
                    continue;
                int cap = item.Units > int.MaxValue ? int.MaxValue : (int)item.Units;
                ImGui.Text(item.Name);
                ImGui.SameLine();
                ImGui.Text(Stringify.Quantity(item.Units));
                ImGui.SetNextItemWidth(120);
                ImGui.InputInt("###pile-" + item.Id, ref amount);
                ImGui.SameLine();
                if(ImGui.SmallButton("All###pile-all-" + item.Id))
                    amount = cap;
                if(amount < 0)
                    amount = 0;
                if(amount > cap)
                    amount = cap;
                amounts[item.Id] = amount;
            }
        }

        static List<CargoTransferItem> Chosen(Dictionary<int, int> amounts, List<CargoItemView> piles)
        {
            var items = new List<CargoTransferItem>();
            foreach(var item in piles)
            {
                if(!amounts.TryGetValue(item.Id, out var amount) || amount <= 0)
                    continue;
                long units = amount;
                if(units > item.Units)
                    units = item.Units;
                items.Add(new CargoTransferItem(item.Id, units));
            }
            return items;
        }

        private void DisplayFleetTrade(IClientGalaxy galaxy, IEnumerable<EntitySnapshot> here)
        {
            if(selectedFleet == null)
                return;
            int fleetId = selectedFleet.Id;
            ImGui.TextWrapped("Ships buy and sell around the body you pick.");
            bool widen = selectedBand == OrderBand.System;
            var pool = widen ? LogisticsCandidates(galaxy, here) : here;
            DisplayTargetTree(pool,
                e => e.HasView<BodyView>() && e.HasView<PositionView>(),
                "trade-" + selectedBand,
                id => SubmitFleetCommand(new FleetTradeCommand(fleetId, id)),
                starTargetsSystem: selectedBand == OrderBand.System,
                spanLabel: BandSpanLabel(selectedBand));
        }

        private void DisplayFreightPairs(IClientGalaxy galaxy, IEnumerable<EntitySnapshot> here, bool trade)
        {
            bool widen = selectedBand == OrderBand.System;
            var pool = widen ? LogisticsCandidates(galaxy, here) : here;
            var placed = Place(galaxy, pool, widen);
            var colonies = pool
                .Where(e => e.Kind == BodyKind.Colony && e.HasView<MarketView>() && (trade || e.Relation == OwnerRelation.Owned))
                .OrderBy(e => NameOf(e), StringComparer.Ordinal)
                .ToList();
            ImGui.TextWrapped(trade
                ? "One buy and one sell inside this band."
                : "One posted haul inside this band. A fleet splits that load across its holds.");

            bool any = false;
            for(int i = 0; i < colonies.Count; i++)
            {
                for(int j = 0; j < colonies.Count; j++)
                {
                    if(i == j)
                        continue;
                    var source = colonies[i];
                    var dest = colonies[j];
                    if(PairBand(source, dest.Id, placed) != selectedBand)
                        continue;
                    var sourceBook = source.GetView<MarketView>();
                    var destBook = dest.GetView<MarketView>();
                    if(sourceBook == null || destBook == null)
                        continue;
                    foreach(var sell in sourceBook.Goods.Where(good => good.SellQuantity > 0).OrderBy(good => good.Name, StringComparer.Ordinal))
                    {
                        var buy = destBook.Goods.FirstOrDefault(good => good.CargoId == sell.CargoId && good.BuyQuantity > 0);
                        if(buy == null)
                            continue;
                        any = true;
                        long units = Math.Min(sell.SellQuantity, buy.BuyQuantity);
                        string id = "pair-" + source.Id + "-" + dest.Id + "-" + sell.CargoId;
                        if(!ImGui.Button(NameOf(source) + " to " + NameOf(dest) + ": " + sell.Name + " (" + units + ")###" + id))
                            continue;
                        if(trade && orderShip != null)
                            SubmitFleetCommand(new TradePairCommand(orderShip.Id, source.Id, dest.Id, sell.CargoId));
                        else if(!trade && orderShip != null)
                            SubmitFleetCommand(new HaulPairCommand(orderShip.Id, source.Id, dest.Id, sell.CargoId));
                        else if(!trade && selectedFleet != null)
                            SubmitFleetCommand(new FleetHaulContractCommand(selectedFleet.Id, source.Id, dest.Id, sell.CargoId));
                    }
                }
            }

            if(!any)
                ImGui.Text(trade ? "No posted trade in this reach." : "No posted haul in this reach.");
        }

        sealed class Placed
        {
            public string Home = "";
            public Dictionary<int, string> SystemOf = new();
            public Dictionary<int, EntitySnapshot> ById = new();
        }

        Placed Place(IClientGalaxy galaxy, IEnumerable<EntitySnapshot> pool, bool widen)
        {
            var placed = new Placed { Home = SubjectSystemId() ?? "" };
            if(!widen)
            {
                foreach(var entity in pool)
                {
                    placed.ById[entity.Id] = entity;
                    placed.SystemOf[entity.Id] = placed.Home;
                }
                return placed;
            }

            var ids = LogisticsIds();
            if(ids.Count <= 1)
                ids = string.IsNullOrEmpty(placed.Home) ? ids : new[] { placed.Home };
            foreach(var systemId in ids)
            {
                var system = galaxy.GetSystem(systemId);
                if(system == null)
                    continue;
                foreach(var entity in system.Entities)
                {
                    if(entity.Relation == OwnerRelation.Hostile)
                        continue;
                    placed.ById[entity.Id] = entity;
                    placed.SystemOf[entity.Id] = system.SystemId;
                }
            }
            foreach(var entity in pool)
            {
                placed.ById[entity.Id] = entity;
                if(!placed.SystemOf.ContainsKey(entity.Id))
                    placed.SystemOf[entity.Id] = placed.Home;
            }
            return placed;
        }

        OrderBand? PairBand(EntitySnapshot colony, int otherId, Placed placed)
        {
            if(otherId >= 0 && otherId != colony.Id && placed.ById.TryGetValue(otherId, out var other))
                return BandOf(colony, other, placed);
            return null;
        }

        bool HasPartner(EntitySnapshot colony, int otherId, OrderBand band, Placed placed)
        {
            if(otherId >= 0 && otherId != colony.Id)
                return PairBand(colony, otherId, placed) == band;
            foreach(var candidate in placed.ById.Values)
            {
                if(candidate.Id == colony.Id || candidate.Kind != BodyKind.Colony)
                    continue;
                if(!candidate.HasView<CargoStorageView>() && !candidate.HasView<MarketView>())
                    continue;
                if(BandOf(colony, candidate, placed) == band)
                    return true;
            }
            return false;
        }

        static OrderBand? BandOf(EntitySnapshot left, EntitySnapshot right, Placed placed)
        {
            string home = placed.Home;
            string leftSystem = placed.SystemOf.TryGetValue(left.Id, out var leftId) ? leftId : home;
            string rightSystem = placed.SystemOf.TryGetValue(right.Id, out var rightId) ? rightId : home;
            if(leftSystem != home || rightSystem != home)
                return OrderBand.System;
            if(ShareWell(left, right, placed.ById))
                return OrderBand.Ship;
            return OrderBand.Well;
        }

        static bool ShareWell(EntitySnapshot left, EntitySnapshot right, Dictionary<int, EntitySnapshot> byId)
        {
            int a = BodyId(left);
            int b = BodyId(right);
            if(a == b)
                return true;
            if(!byId.TryGetValue(a, out var bodyA) || !byId.TryGetValue(b, out var bodyB))
                return false;
            int? parentA = ParentIdOf(bodyA);
            int? parentB = ParentIdOf(bodyB);
            if(parentB == a || parentA == b)
                return true;
            if(parentA == null || parentB == null || parentA != parentB)
                return false;
            return byId.TryGetValue(parentA.Value, out var parent) && ParentIdOf(parent) != null;
        }

        static int BodyId(EntitySnapshot colony)
            => colony.GetView<ColonyView>()?.PlanetEntityId ?? colony.Id;

        private void SubmitFleetCommand(GameCommand command) => _uiState.GameClient?.SubmitCommandAsync(command);

        private void DisplayOrders()
        {
            if(selectedFleet == null && orderShip == null)
                return;

            bool shipActionsOnly = orderShip != null && !ShipHasCaptain(orderShip);
            string header = orderShip != null ? orderShip.Name : "Fleet orders";
            GoalSnapshot? goal = orderShip != null ? orderShip.Goal : selectedFleet?.Goal;
            IReadOnlyList<OrderSnapshot> orders = orderShip != null
                ? orderShip.Orders
                : selectedFleet?.Orders ?? Array.Empty<OrderSnapshot>();
            int holderId = orderShip?.Id ?? selectedFleet!.Id;

            DisplayHelpers.Header(header);
            if (!shipActionsOnly && HasGoal(goal))
            {
                if (ImGui.BeginTable("SubjectGoal", 2, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
                {
                    ImGui.TableSetupColumn("Goal", ImGuiTableColumnFlags.None, 0.4f);
                    ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.None, 0.6f);
                    ImGui.TableHeadersRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(goal!.Name);
                    ImGui.TableNextColumn();
                    ImGui.Text(goal.Status);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(goal.Message);
                    ImGui.EndTable();
                }
                if (CanCancelGoal(goal))
                {
                    if (ImGui.Button("Cancel now"))
                        SubmitFleetCommand(new CancelGoalCommand(holderId, CancelTiming.Now));
                    ImGui.SameLine();
                    if (ImGui.Button("Cancel when convenient"))
                        SubmitFleetCommand(new CancelGoalCommand(holderId, CancelTiming.WhenConvenient));
                }
            }

            if (orders.Count == 0)
            {
                ImGui.Text("None");
            }
            else if (ImGui.BeginTable("SubjectOrders", 4, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.None, 0.1f);
                ImGui.TableSetupColumn("Order", ImGuiTableColumnFlags.None, 0.55f);
                ImGui.TableSetupColumn("Pause", ImGuiTableColumnFlags.None, 0.15f);
                ImGui.TableSetupColumn("Cancel", ImGuiTableColumnFlags.None, 0.2f);
                ImGui.TableHeadersRow();

                for (int i = 0; i < orders.Count; i++)
                {
                    var order = orders[i];
                    ImGui.TableNextColumn();
                    ImGui.Text((i + 1).ToString());
                    ImGui.TableNextColumn();
                    if (order.IsEditableManeuver && orderShip != null && orderShip.SystemId.Length > 0)
                    {
                        if (ImGui.Selectable(order.Name + "##edit-" + i))
                            _uiState.OpenManeuverPanelForOrder(orderShip.Id, orderShip.SystemId, order);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip("Click to edit or delete this order");
                    }
                    else
                    {
                        ImGui.Text(order.Name);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(order.IsRunning ? "Running" : order.IsFinished ? "Finished" : order.Details);
                    }
                    ImGui.TableNextColumn();
                    if (order.OrderId.Length > 0)
                    {
                        bool pause = order.PauseOnAction;
                        if (ImGui.Checkbox("##pause-" + order.OrderId, ref pause))
                        {
                            SubmitFleetCommand(new SetOrderPauseCommand(holderId, order.OrderId, pause));
                        }
                    }
                    ImGui.TableNextColumn();
                    if (order.OrderId.Length > 0 && ImGui.SmallButton("Cancel##" + order.OrderId))
                        SubmitFleetCommand(new CancelOrderCommand(holderId, order.OrderId));
                }

                ImGui.EndTable();
            }
        }

        private void DisplayFleetList(IClientGalaxy galaxy)
        {
            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("FleetListSelection", new Vector2(Styles.LeftColumnWidthLg, windowContentSize.Y - 24f), ImGuiChildFlags.Borders))
            {
                DisplayHelpers.Header("Fleets", "Select a fleet to manage it.");

                // We need a drop target here so nested items can be un-nested to the root of the tree
                DisplayEmptyDropTarget();

                foreach(var fleet in galaxy.Fleets)
                {
                    DisplayFleetItem(fleet);
                }

                DisplayUnassignedBranch(galaxy.UnattachedShips);

                var sizeLeft = ImGui.GetContentRegionAvail();
                ImGui.InvisibleButton("invis-droptarget", new Vector2(sizeLeft.X, 32f));
                DisplayEmptyDropTarget();
            }
            ImGui.EndChild();

            if(ImGui.Button("Create New Fleet", new Vector2(Styles.LeftColumnWidthLg, 0f)))
            {
                if(_uiState.GameClient != null && !string.IsNullOrEmpty(_uiState.SelectedStarSystemId))
                {
                    // The fleet is created (and named) server-side; the FleetsChanged push adds it here.
                    SubmitFleetCommand(new CreateFleetCommand(_uiState.GameClient.Session.FactionId, _uiState.SelectedStarSystemId));
                }
            }
        }

        private void DisplayFleetItem(FleetSnapshot fleet)
        {
            ImGui.PushID(fleet.Id.ToString());
            string name = fleet.Name;
            bool hasChildren = fleet.SubFleets.Count > 0 || fleet.Ships.Count > 0;
            // The arrow, or a double-click, opens the fleet. A single click on the name only selects it.
            var flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
            if(!hasChildren)
                flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;

            // A ship subject owns the pane. The fleet row stays unselected until it is clicked again.
            if(selectedFleetId == fleet.Id && orderShipId == null)
            {
                flags |= ImGuiTreeNodeFlags.Selected;
            }

            string description = "";

            if(fleet.Orders.Count == 0)
            {
                description = "No Orders";
            }
            else
            {
                foreach(var order in fleet.Orders)
                {
                    description += order.Name + "\n";
                }
            }

            bool isTreeOpen = ImGui.TreeNodeEx(name, flags);
            if(ImGui.IsItemHovered())
                DisplayHelpers.DescriptiveTooltip(name, "Fleet", description);

            if(ImGui.IsItemClicked())
                SelectFleet(fleet.Id);
            DisplayContextMenu(fleet);
            DisplayDropSource(fleet.Id, name);
            DisplayFleetDropTarget(fleet);

            if(hasChildren && isTreeOpen)
            {
                foreach(var ship in fleet.Ships)
                    DisplayFleetShip(fleet, ship);
                foreach(var subFleet in fleet.SubFleets)
                    DisplayFleetItem(subFleet);
                ImGui.TreePop();
            }
            ImGui.PopID();
        }

        private void DisplayFleetShip(FleetSnapshot fleet, ShipSnapshot ship)
        {
            ImGui.PushID("ship-" + ship.Id);
            var flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
            bool checkedForDrag = selectedFleetId == fleet.Id && selectedShips.TryGetValue(ship.Id, out var selected) && selected;
            if(checkedForDrag || orderShipId == ship.Id)
                flags |= ImGuiTreeNodeFlags.Selected;

            string name = ship.Id == fleet.FlagshipId ? "(F) " + ship.Name : ship.Name;
            ImGui.TreeNodeEx($"{name}###fleet-ship-{ship.Id}", flags);
            if(ImGui.IsItemClicked(ImGuiMouseButton.Left) && !ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                if(selectedFleetId != fleet.Id)
                    SelectFleet(fleet.Id);
                if(!selectedShips.ContainsKey(ship.Id))
                    selectedShips[ship.Id] = false;
                selectedShips[ship.Id] = !selectedShips[ship.Id];
                // SelectFleet clears the ship subject. Put it back: the pane follows the ship.
                orderShipId = ship.Id;
                autoSelectFirstFleet = false;
            }
            DisplayHelpers.ShipTooltip(ship);
            DisplayShipContextMenu(selectedShips, ship, fleet);
            DisplayShipDropSource(ship, selectedFleetId == fleet.Id ? selectedShips : null);
            // A ship row accepts ships only. A fleet dropped here must not nest under this fleet.
            DisplayShipDropTarget(fleet);
            ImGui.PopID();
        }

        private void DisplayUnassignedBranch(IReadOnlyList<ShipSnapshot> ships)
        {
            var flags = ImGuiTreeNodeFlags.DefaultOpen;
            if(ships.Count == 0)
                flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;

            bool open = ImGui.TreeNodeEx("Unassigned###unassigned-ships", flags);
            DisplayUnassignedDropTarget();
            if(ships.Count > 0 && open)
            {
                foreach(var ship in ships)
                    DisplayUnassignedShip(ship);
                ImGui.TreePop();
            }
        }

        private void DisplayUnassignedShip(ShipSnapshot ship)
        {
            ImGui.PushID("loose-" + ship.Id);
            if(!selectedUnattachedShips.ContainsKey(ship.Id))
                selectedUnattachedShips[ship.Id] = false;

            var flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
            if(selectedUnattachedShips[ship.Id] || orderShipId == ship.Id)
                flags |= ImGuiTreeNodeFlags.Selected;

            ImGui.TreeNodeEx($"{ship.Name}###loose-ship-{ship.Id}", flags);
            if(ImGui.IsItemClicked(ImGuiMouseButton.Left) && !ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                selectedUnattachedShips[ship.Id] = !selectedUnattachedShips[ship.Id];
                OrderShip(ship.Id);
            }
            DisplayHelpers.ShipTooltip(ship);
            DisplayShipContextMenu(selectedUnattachedShips, ship, owner: null);
            DisplayShipDropSource(ship, selectedUnattachedShips);
            DisplayUnassignedDropTarget();
            ImGui.PopID();
        }

        private void DisplayContextMenu(FleetSnapshot fleet)
        {
            if(ImGui.BeginPopupContextItem())
            {
                if(ImGui.MenuItem("Rename"))
                {
                    RenameWindow.GetInstance().SetTarget(fleet.Id, fleet.Name);
                    RenameWindow.GetInstance().SetActive(true);
                }
                ImGui.Separator();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.TerribleColor);
                if(ImGui.MenuItem("Disband###delete-" + fleet.Id))
                {
                    SubmitFleetCommand(new DisbandFleetCommand(fleet.Id));
                    SelectFleet(null);
                }
                ImGui.PopStyleColor();
                ImGui.EndPopup();
            }
        }

        private void DisplayShipContextMenu(Dictionary<int, bool> selected, ShipSnapshot ship, FleetSnapshot? owner)
        {
            var galaxy = _uiState.GameClient?.Galaxy;
            if(galaxy == null) return;

            if(ImGui.BeginPopupContextItem())
            {
                if(ImGui.MenuItem("Give orders"))
                    OrderShip(ship.Id);
                if(ImGui.MenuItem("View Ship"))
                {
                    var systemId = string.IsNullOrEmpty(ship.SystemId) ? _uiState.SelectedStarSystemId : ship.SystemId;
                    _uiState.EntityClicked(ship.Id, systemId, MouseButtons.Primary);
                }
                if(owner != null)
                {
                    bool isFlagship = ship.Id == owner.FlagshipId;
                    if(isFlagship)
                    {
                        ImGui.BeginDisabled();
                    }
                    if(ImGui.MenuItem("Promote to Flagship"))
                    {
                        SubmitFleetCommand(new SetFlagshipCommand(owner.Id, ship.Id));
                    }
                    if(isFlagship)
                    {
                        ImGui.EndDisabled();
                    }
                    if(ImGui.MenuItem("Remove from fleet"))
                        SubmitFleetCommand(new DetachShipCommand(ship.Id));
                }
                ImGui.Separator();

                if(ImGui.BeginMenu("Re-assign ships"))
                {
                    ImGui.Text("Re-assign ships to:");
                    ImGui.Separator();
                    foreach(var fleet in galaxy.Fleets)
                    {
                        DisplayShipAssignmentOption(selected, ship, fleet, currentFleetId: owner?.Id);
                    }
                    ImGui.EndMenu();
                }
                ImGui.EndPopup();
            }
        }

        private void DisplayShipAssignmentOption(Dictionary<int, bool> selected, ShipSnapshot ship, FleetSnapshot fleet, int depth = 0, int? currentFleetId = null)
        {
            for(int i = 0; i < depth; i++)
            {
                ImGui.InvisibleButton("invis", new Vector2(8, 8));
                ImGui.SameLine();
            }

            if(fleet.Id == currentFleetId)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.Text(fleet.Name);
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushID(fleet.Id.ToString());
                if(ImGui.MenuItem(fleet.Name))
                {
                    // The server detaches each ship from whichever fleet (or the faction root)
                    // currently holds it, so no unassign bookkeeping is needed here.
                    if(!selected.Any(x => x.Value))
                    {
                        SubmitFleetCommand(new ReassignShipCommand(ship.Id, fleet.Id));
                    }
                    else
                    {
                        foreach(var (selectedShipId, isSelected) in selected)
                        {
                            if(!isSelected) continue;
                            SubmitFleetCommand(new ReassignShipCommand(selectedShipId, fleet.Id));
                        }
                        // Clean up the selections
                        selected.Clear();
                    }
                }
                ImGui.PopID();
            }

            foreach(var subFleet in fleet.SubFleets)
            {
                DisplayShipAssignmentOption(selected, ship, subFleet, depth + 1, currentFleetId);
            }
        }

        private void DisplayEmptyDropTarget()
        {
            if(ImGui.BeginDragDropTarget())
            {
                ImGui.AcceptDragDropPayload("FLEET", ImGuiDragDropFlags.None);
                if(ImGui.IsMouseReleased(ImGuiMouseButton.Left) && dragFleetId != -1)
                {
                    if(_uiState.GameClient != null)
                    {
                        // Dropping on empty space re-parents to the faction root.
                        SubmitFleetCommand(new ChangeFleetParentCommand(dragFleetId, _uiState.GameClient.Session.FactionId));
                        dragFleetId = -1;
                    }
                }
                ImGui.EndDragDropTarget();
            }
        }

        private void DisplayFleetDropTarget(FleetSnapshot fleet)
        {
            if(!ImGui.BeginDragDropTarget())
                return;

            ImGui.AcceptDragDropPayload("FLEET", ImGuiDragDropFlags.None);
            ImGui.AcceptDragDropPayload("SHIP", ImGuiDragDropFlags.None);
            if(ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                if(dragFleetId != -1 && dragFleetId != fleet.Id)
                {
                    SubmitFleetCommand(new ChangeFleetParentCommand(dragFleetId, fleet.Id));
                    dragFleetId = -1;
                }
                else if(dragShipIds.Count > 0)
                {
                    DropShipsOnFleet(fleet);
                }
            }
            ImGui.EndDragDropTarget();
        }

        private void DisplayShipDropTarget(FleetSnapshot fleet)
        {
            if(!ImGui.BeginDragDropTarget())
                return;

            ImGui.AcceptDragDropPayload("SHIP", ImGuiDragDropFlags.None);
            if(ImGui.IsMouseReleased(ImGuiMouseButton.Left) && dragShipIds.Count > 0)
                DropShipsOnFleet(fleet);
            ImGui.EndDragDropTarget();
        }

        private void DisplayUnassignedDropTarget()
        {
            if(!ImGui.BeginDragDropTarget())
                return;

            ImGui.AcceptDragDropPayload("SHIP", ImGuiDragDropFlags.None);
            if(ImGui.IsMouseReleased(ImGuiMouseButton.Left) && dragShipIds.Count > 0)
            {
                var loose = new HashSet<int>();
                var galaxy = _uiState.GameClient?.Galaxy;
                if(galaxy != null)
                {
                    foreach(var ship in galaxy.UnattachedShips)
                        loose.Add(ship.Id);
                }
                int focusId = dragShipIds[dragShipIds.Count - 1];
                foreach(var shipId in dragShipIds)
                {
                    if(!loose.Contains(shipId))
                        SubmitFleetCommand(new DetachShipCommand(shipId));
                }
                dragShipIds.Clear();
                selectedShips.Clear();
                selectedUnattachedShips.Clear();
                // The ship that just left the fleet is what the pane should show.
                OrderShip(focusId);
            }
            ImGui.EndDragDropTarget();
        }

        private void DropShipsOnFleet(FleetSnapshot fleet)
        {
            var already = new HashSet<int>();
            foreach(var ship in fleet.Ships)
                already.Add(ship.Id);
            foreach(var shipId in dragShipIds)
            {
                if(!already.Contains(shipId))
                    SubmitFleetCommand(new ReassignShipCommand(shipId, fleet.Id));
            }
            dragShipIds.Clear();
            selectedShips.Clear();
            selectedUnattachedShips.Clear();
        }

        private void DisplayShipDropSource(ShipSnapshot ship, Dictionary<int, bool>? selected)
        {
            if(!ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoDisableHover))
                return;

            dragFleetId = -1;
            dragShipIds.Clear();
            if(selected != null && selected.TryGetValue(ship.Id, out var on) && on)
            {
                foreach(var (id, isSelected) in selected)
                {
                    if(isSelected)
                        dragShipIds.Add(id);
                }
            }
            if(!dragShipIds.Contains(ship.Id))
                dragShipIds.Add(ship.Id);

            ImGui.SetDragDropPayload("SHIP", IntPtr.Zero, 0);
            ImGui.Text(dragShipIds.Count == 1 ? ship.Name : dragShipIds.Count + " ships");
            ImGui.EndDragDropSource();
        }

        private void DisplayDropSource(int fleetId, string name)
        {
            // Begin drag source
            if(ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoDisableHover))
            {
                dragFleetId = fleetId;
                dragShipIds.Clear();

                ImGui.SetDragDropPayload("FLEET", IntPtr.Zero, 0);
                ImGui.Text(name);
                ImGui.EndDragDropSource();
            }
        }

        #region Standing Orders editor

        /// <summary>The local working copy, (re)loaded from the snapshot when nothing is being
        /// edited; player edits are kept until saved or the fleet selection changes.</summary>
        private List<StandingOrderEdit> EditedOrders(FleetSnapshot fleet)
        {
            if (editedOrders != null
                && (standingOrdersDirty || ReferenceEquals(editedOrdersSource, fleet.StandingOrders)))
                return editedOrders;

            editedOrders = new List<StandingOrderEdit>(fleet.StandingOrders.Count);
            foreach (var order in fleet.StandingOrders)
            {
                var edit = new StandingOrderEdit
                {
                    NameBuffer = string.IsNullOrEmpty(order.Name) ? new byte[32] : Utils.BytesFromString(order.Name, 32),
                    Actions = order.Actions.ToList(),
                };
                foreach (var condition in order.Conditions)
                {
                    edit.Conditions.Add(new StandingOrderConditionEdit
                    {
                        ConditionType = condition.ConditionType,
                        Comparison = condition.Comparison,
                        Threshold = condition.Threshold,
                        Logic = condition.Logic ?? StandingOrderLogic.And,
                    });
                }
                editedOrders.Add(edit);
            }

            editedOrdersSource = fleet.StandingOrders;
            standingOrdersDirty = false;
            if (selectedOrderIndex >= editedOrders.Count)
                selectedOrderIndex = -1;
            return editedOrders;
        }

        private void SaveStandingOrders(int fleetId, List<StandingOrderEdit> orders)
        {
            var payload = new List<StandingOrder>(orders.Count);
            foreach (var edit in orders)
            {
                var conditions = new List<StandingOrderCondition>(edit.Conditions.Count);
                for (int i = 0; i < edit.Conditions.Count; i++)
                {
                    var condition = edit.Conditions[i];
                    conditions.Add(new StandingOrderCondition(
                        condition.ConditionType,
                        condition.Comparison,
                        condition.Threshold,
                        i < edit.Conditions.Count - 1 ? condition.Logic : null));
                }
                payload.Add(new StandingOrder(Utils.StringFromBytes(edit.NameBuffer), conditions, edit.Actions.ToList()));
            }

            _uiState.GameClient?.SubmitCommandAsync(new SetStandingOrdersCommand(fleetId, payload));
            // Keep the local copy on screen until the refreshed fleet snapshot is pushed back.
            standingOrdersDirty = false;
            editedOrdersSource = null;
        }

        private void DisplayStandingOrdersTab()
        {
            if(selectedFleetId is not { } fleetId || selectedFleet == null)
                return;

            if(ImGui.BeginTabItem("Standing Orders"))
            {
                var orders = EditedOrders(selectedFleet);

                var size = ImGui.GetContentRegionAvail();
                var firstChildSize = new Vector2(size.X * 0.33f, size.Y);
                var secondChildSize = new Vector2(size.X * 0.67f - (size.X * 0.01f), size.Y);
                if(ImGui.BeginChild("StandingOrders-List", firstChildSize, ImGuiChildFlags.Borders))
                {
                    var sizeAvailable = ImGui.GetContentRegionAvail();
                    DisplayHelpers.Header("Order List");
                    if(orders.Count > 0)
                    {
                        for(int i = 0; i < orders.Count; i++)
                        {
                            ImGui.PushID("###" + i);
                            bool isSelected = selectedOrderIndex == i;
                            string name = Utils.StringFromBytes(orders[i].NameBuffer);
                            if(string.IsNullOrEmpty(name)) name = "<un-named>";
                            if(ImGui.Selectable((i + 1) + ". " + name, ref isSelected))
                            {
                                selectedOrderIndex = i;
                            }
                            if(ImGui.BeginPopupContextItem())
                            {
                                if(i > 0 && ImGui.MenuItem("Move Up"))
                                {
                                    (orders[i - 1], orders[i]) = (orders[i], orders[i - 1]);
                                    if(selectedOrderIndex == i) selectedOrderIndex = i - 1;
                                    else if(selectedOrderIndex == i - 1) selectedOrderIndex = i;
                                    standingOrdersDirty = true;
                                }
                                if(i < orders.Count - 1 && ImGui.MenuItem("Move Down"))
                                {
                                    (orders[i + 1], orders[i]) = (orders[i], orders[i + 1]);
                                    if(selectedOrderIndex == i) selectedOrderIndex = i + 1;
                                    else if(selectedOrderIndex == i + 1) selectedOrderIndex = i;
                                    standingOrdersDirty = true;
                                }
                                if(ImGui.MenuItem("Delete Order"))
                                {
                                    orders.RemoveAt(i);
                                    if(selectedOrderIndex == i) selectedOrderIndex = -1;
                                    else if(selectedOrderIndex > i) selectedOrderIndex--;
                                    standingOrdersDirty = true;
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
                        orders.Add(new StandingOrderEdit());
                        standingOrdersDirty = true;

                        // if this is the first order, select it
                        if(orders.Count == 1)
                            selectedOrderIndex = 0;
                    }
                }
                ImGui.EndChild();
                ImGui.SameLine();
                if(ImGui.BeginChild("StandingOrders-edit", secondChildSize, ImGuiChildFlags.Borders)
                    && selectedOrderIndex >= 0 && selectedOrderIndex < orders.Count)
                {
                    var selectedOrder = orders[selectedOrderIndex];
                    var sizeAvailable = ImGui.GetContentRegionAvail();
                    DisplayHelpers.Header("Order Name");
                    if(ImGui.InputText("###order-name-input", selectedOrder.NameBuffer, 32))
                    {
                        standingOrdersDirty = true;
                    }
                    ImGui.NewLine();
                    DisplayHelpers.Header("Conditions", "If the conditions listed are true, the actions will execute.");

                    var conditions = selectedOrder.Conditions;
                    for(int i = 0; i < conditions.Count; i++)
                    {
                        var condition = conditions[i];
                        var conditionType = StandingOrderConditionTypes.FirstOrDefault(t => t.Id == condition.ConditionType);
                        ImGui.PushID(i);
                        ImGui.Button(conditionType.Label ?? condition.ConditionType, new Vector2(Math.Max(sizeAvailable.X * 0.4f, 128f), 0f));

                        int value = (int)condition.Threshold;
                        int comparisonIndex = (int)condition.Comparison;
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(Math.Max(sizeAvailable.X * 0.075f, 16f));
                        if(ImGui.Combo("###orderComparison", ref comparisonIndex, orderComparisons, orderComparisons.Length))
                        {
                            condition.Comparison = (StandingOrderComparison)comparisonIndex;
                            standingOrdersDirty = true;
                        }
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(Math.Max(sizeAvailable.X * 0.15f, 32f));
                        if(ImGui.InputInt(conditionType.Description + "###orderValue", ref value, 1, 5))
                        {
                            if(value < conditionType.Min) value = (int)conditionType.Min;
                            if(value > conditionType.Max) value = (int)conditionType.Max;

                            condition.Threshold = value;
                            standingOrdersDirty = true;
                        }

                        // Show the logical operators UI on all but the last item
                        ImGui.SameLine();
                        var position = ImGui.GetCursorPos();
                        if(i < conditions.Count - 1)
                        {
                            ImGui.SetCursorPosY(position.Y + 12f);
                            if(condition.Logic == StandingOrderLogic.And)
                            {
                                ImGui.SetCursorPosX(sizeAvailable.X - 82f);
                                if(ImGui.Button("AND"))
                                {
                                    condition.Logic = StandingOrderLogic.Or;
                                    standingOrdersDirty = true;
                                }
                            }
                            else
                            {
                                ImGui.SetCursorPosX(sizeAvailable.X - 48f);
                                if(ImGui.Button("OR"))
                                {
                                    condition.Logic = StandingOrderLogic.And;
                                    standingOrdersDirty = true;
                                }
                            }
                        }
                        ImGui.SameLine();
                        ImGui.SetCursorPos(position);
                        ImGui.SetCursorPosX(sizeAvailable.X - 12f);
                        if(ImGui.Button("x"))
                        {
                            conditions.RemoveAt(i);
                            standingOrdersDirty = true;
                            ImGui.PopID();
                            break;
                        }
                        ImGui.PopID();
                    }

                    if(ImGui.Button("Add Condition"))
                    {
                        if(orderConditionsIndex >= 0 && orderConditionsIndex < StandingOrderConditionTypes.Length)
                        {
                            var conditionType = StandingOrderConditionTypes[orderConditionsIndex];
                            conditions.Add(new StandingOrderConditionEdit
                            {
                                ConditionType = conditionType.Id,
                                Comparison = StandingOrderComparison.LessThan,
                                Threshold = 30f,
                            });
                            standingOrdersDirty = true;
                        }
                    }
                    ImGui.SameLine();
                    var conditionLabels = StandingOrderConditionTypes.Select(t => t.Label).ToArray();
                    if(ImGui.Combo("###order-add-condition-list", ref orderConditionsIndex, conditionLabels, conditionLabels.Length))
                    {
                    }

                    ImGui.NewLine();
                    DisplayHelpers.Header("Actions", "The actions listed will execute in the order in which they are listed.");

                    for(int i = 0; i < selectedOrder.Actions.Count; i++)
                    {
                        ImGui.PushID("action" + i);
                        var actionSize = ImGui.GetContentRegionAvail();
                        var actionLabel = StandingOrderActionTypes.FirstOrDefault(t => t.Id == selectedOrder.Actions[i]).Label;
                        ImGui.Text(actionLabel ?? selectedOrder.Actions[i]);
                        ImGui.SameLine();
                        ImGui.SetCursorPosX(actionSize.X - 12f);
                        if(ImGui.Button("x"))
                        {
                            selectedOrder.Actions.RemoveAt(i);
                            standingOrdersDirty = true;
                            ImGui.PopID();
                            break;
                        }
                        ImGui.PopID();
                    }

                    if(ImGui.Button("Add Action"))
                    {
                        if(orderActionsIndex >= 0 && orderActionsIndex < StandingOrderActionTypes.Length)
                        {
                            selectedOrder.Actions.Add(StandingOrderActionTypes[orderActionsIndex].Id);
                            standingOrdersDirty = true;
                        }
                    }
                    ImGui.SameLine();
                    var actionLabels = StandingOrderActionTypes.Select(t => t.Label).ToArray();
                    if(ImGui.Combo("###order-add-action-list", ref orderActionsIndex, actionLabels, actionLabels.Length))
                    {
                    }

                    ImGui.SetCursorPosY(sizeAvailable.Y - 12f);
                    if(ImGui.Button(standingOrdersDirty ? "Save*" : "Save", new Vector2(sizeAvailable.X, 0)))
                    {
                        SaveStandingOrders(fleetId, orders);
                    }
                }
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
        }

        #endregion
    }
}
