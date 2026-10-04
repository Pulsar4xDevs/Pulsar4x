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
            Jump,
            RefuelAt,
            Trade,
            Haul,
            HaulContract,
            PlotWarpTo,
            PlotCircularise,
            PlotChangeAltitude,
            PlotMatchOrbit,
        }

        private IssueOrderType selectedIssueOrderType = IssueOrderType.MoveTo;
        private int haulSourceId = -1;
        private int haulDestId = -1;
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
            haulSourceId = -1;
            haulDestId = -1;
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

            if(Window.Begin("Fleets and Ships", ref IsActive, _flags))
            {
                DisplayFleetList(galaxy);

                if(selectedFleet != null || orderShip != null)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosY(27f);
                    DisplayOrders();

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
                    Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                    var firstChildSize = new Vector2(windowContentSize.X * 0.99f, windowContentSize.Y);
                    if (ImGui.BeginChild("FleetSummary1", firstChildSize, ImGuiChildFlags.Borders))
                    {
                        if (orderShip != null)
                            DisplayShipSummary(galaxy);
                        else if (selectedFleet != null && ImGui.CollapsingHeader("Fleet Information", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            ImGui.Columns(2);
                            DisplayHelpers.PrintRow("Name", selectedFleet.Name);
                            DisplayHelpers.PrintRow("Flagship", selectedFleet.FlagshipName ?? "-");
                            DisplayFleetCommanderRow();
                            DisplayHelpers.PrintRow("Command span", FleetSpanLabel(),
                                tooltipTwo: "How far this fleet's orders reach. It comes from the flagship's bridge.");

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
                    ImGui.SameLine();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Issue Orders"))
                {
                    var size = ImGui.GetContentRegionAvail();
                    var firstChildSize = new Vector2(size.X * 0.27f, size.Y);
                    var secondChildSize = new Vector2(size.X * 0.73f - (size.X * 0.01f), size.Y);
                    if(ImGui.BeginChild("IssueOrders-List", firstChildSize, ImGuiChildFlags.Borders))
                    {
                        DisplayOrderChoices(galaxy);
                    }
                    ImGui.EndChild();
                    ImGui.SameLine();
                    IssueOrdersDisplay(galaxy, secondChildSize);
                    ImGui.EndTabItem();
                }

                if (orderShip == null)
                    DisplayStandingOrdersTab();

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

            if (!ImGui.CollapsingHeader("Ship", ImGuiTreeNodeFlags.DefaultOpen))
                return;

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

        private void DisplayOrderChoices(IClientGalaxy galaxy)
        {
            if (orderShip != null && !ShipHasCaptain(orderShip))
            {
                DisplayHelpers.Header("Actions");
                ImGui.TextWrapped("No captain. You plot this ship's actions. The queue does not replan.");
                var entity = SubjectEntity(galaxy);
                bool warp = entity?.HasView<WarpAbilityView>() == true;
                bool thrust = entity?.HasView<ThrustView>() == true;
                if (ImGui.Selectable("Go to ...", selectedIssueOrderType == IssueOrderType.MoveTo))
                    selectedIssueOrderType = IssueOrderType.MoveTo;
                if (warp && ImGui.Selectable("Warp to ...", selectedIssueOrderType == IssueOrderType.PlotWarpTo))
                    selectedIssueOrderType = IssueOrderType.PlotWarpTo;
                if (thrust && ImGui.Selectable("Circularise", selectedIssueOrderType == IssueOrderType.PlotCircularise))
                    selectedIssueOrderType = IssueOrderType.PlotCircularise;
                if (thrust && ImGui.Selectable("Change altitude ...", selectedIssueOrderType == IssueOrderType.PlotChangeAltitude))
                    selectedIssueOrderType = IssueOrderType.PlotChangeAltitude;
                if (thrust && ImGui.Selectable("Match orbit ...", selectedIssueOrderType == IssueOrderType.PlotMatchOrbit))
                    selectedIssueOrderType = IssueOrderType.PlotMatchOrbit;
                var geoSite = CurrentGeoSurveySite(galaxy);
                if (geoSite != null && ImGui.Selectable("Geo survey " + NameOf(geoSite), selectedIssueOrderType == IssueOrderType.GeoSurvey))
                    selectedIssueOrderType = IssueOrderType.GeoSurvey;
                var gravSite = CurrentGravSurveySite(galaxy);
                if (gravSite != null && ImGui.Selectable("Grav survey " + NameOf(gravSite), selectedIssueOrderType == IssueOrderType.GravSurvey))
                    selectedIssueOrderType = IssueOrderType.GravSurvey;
                if (ImGui.Selectable("Jump...", selectedIssueOrderType == IssueOrderType.Jump))
                    selectedIssueOrderType = IssueOrderType.Jump;
                if (warp && ImGui.Selectable("Warp plotter..."))
                    OpenPlotter(orderShip, warp: true);
                if (thrust && ImGui.Selectable("Nav plotter..."))
                    OpenPlotter(orderShip, warp: false);
                return;
            }

            DisplayHelpers.Header(orderShip != null ? "Goals" : "Available Orders");
            if (orderShip != null)
            {
                string captain = string.IsNullOrEmpty(orderShip.CommanderName) ? "The captain" : orderShip.CommanderName;
                ImGui.TextWrapped("Captain " + captain + " turns a goal into a plot.");
            }

            bool geo = orderShip != null ? orderShip.CanGeoSurvey : selectedFleet?.CanGeoSurvey == true;
            bool grav = orderShip != null ? orderShip.CanGravSurvey : selectedFleet?.CanGravSurvey == true;
            bool fleetOrders = orderShip == null;

            if (ImGui.Selectable("Move to ...", selectedIssueOrderType == IssueOrderType.MoveTo))
                selectedIssueOrderType = IssueOrderType.MoveTo;
            if (fleetOrders && ImGui.Selectable("Refuel at ...", selectedIssueOrderType == IssueOrderType.RefuelAt))
                selectedIssueOrderType = IssueOrderType.RefuelAt;
            if (geo && ImGui.Selectable("Geo Survey ...", selectedIssueOrderType == IssueOrderType.GeoSurvey))
                selectedIssueOrderType = IssueOrderType.GeoSurvey;
            if (grav && ImGui.Selectable("Grav Survey ...", selectedIssueOrderType == IssueOrderType.GravSurvey))
                selectedIssueOrderType = IssueOrderType.GravSurvey;
            if (ImGui.Selectable("Jump...", selectedIssueOrderType == IssueOrderType.Jump))
                selectedIssueOrderType = IssueOrderType.Jump;
            if (fleetOrders && ImGui.Selectable("Trade ...", selectedIssueOrderType == IssueOrderType.Trade))
                selectedIssueOrderType = IssueOrderType.Trade;
            if (fleetOrders && ImGui.Selectable("Haul ...", selectedIssueOrderType == IssueOrderType.Haul))
                selectedIssueOrderType = IssueOrderType.Haul;
            if (fleetOrders && ImGui.Selectable("Haul contract ...", selectedIssueOrderType == IssueOrderType.HaulContract))
                selectedIssueOrderType = IssueOrderType.HaulContract;
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

                bool emptyChair = orderShip != null && !ShipHasCaptain(orderShip);
                if (orderShip != null)
                {
                    bool atGeo = CurrentGeoSurveySite(galaxy) != null;
                    bool atGrav = CurrentGravSurveySite(galaxy) != null;
                    bool shipGeo = orderShip.CanGeoSurvey && selectedIssueOrderType == IssueOrderType.GeoSurvey
                        && (!emptyChair || atGeo);
                    bool shipGrav = orderShip.CanGravSurvey && selectedIssueOrderType == IssueOrderType.GravSurvey
                        && (!emptyChair || atGrav);
                    bool shipPlot = emptyChair && (selectedIssueOrderType == IssueOrderType.PlotWarpTo
                        || selectedIssueOrderType == IssueOrderType.PlotCircularise
                        || selectedIssueOrderType == IssueOrderType.PlotChangeAltitude
                        || selectedIssueOrderType == IssueOrderType.PlotMatchOrbit);
                    bool shipGoal = selectedIssueOrderType == IssueOrderType.MoveTo
                        || selectedIssueOrderType == IssueOrderType.Jump
                        || shipGeo || shipGrav || shipPlot;
                    if (!shipGoal)
                        selectedIssueOrderType = IssueOrderType.MoveTo;
                }

                // A ship order is that one body. Command span widens a fleet order.
                bool fleetSpan = orderShip == null;

                // Mirror the old EntityFilter.Friendly | EntityFilter.Neutral read: hostiles aren't targets.
                var candidates = system.Entities.Where(e => e.Relation != OwnerRelation.Hostile);

                switch(selectedIssueOrderType)
                {
                    case IssueOrderType.MoveTo:
                        if (emptyChair)
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
                        if (emptyChair)
                            DisplayPlotSurveyHere(galaxy, commanded.Value, geo: true);
                        else
                            DisplayTargetTree(candidates,
                                e => e.GetView<GeoSurveyView>() is { IsSurveyComplete: false },
                                "geosurvey-button",
                                id => SubmitFleetCommand(new GeoSurveyCommand(commanded.Value, id)),
                                starTargetsSystem: fleetSpan,
                                keepParentWhenChildrenRemain: fleetSpan,
                                markDone: e => e.GetView<GeoSurveyView>()?.IsSurveyComplete == true);
                        break;
                    case IssueOrderType.GravSurvey:
                        if (emptyChair)
                            DisplayPlotSurveyHere(galaxy, commanded.Value, geo: false);
                        else
                            DisplayTargetTree(candidates,
                                e => e.GetView<GravSurveyView>() is { IsSurveyComplete: false },
                                "gravsurvey-button",
                                id => SubmitFleetCommand(new GravSurveyCommand(commanded.Value, id)),
                                starTargetsSystem: fleetSpan,
                                keepParentWhenChildrenRemain: fleetSpan,
                                markDone: e => e.GetView<GravSurveyView>()?.IsSurveyComplete == true);
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
                        if (selectedFleet == null)
                            break;
                        {
                            int fleetId = selectedFleet.Id;
                            DisplayTargetTree(candidates,
                                e => e.Kind == BodyKind.Colony && e.HasView<CargoStorageView>(),
                                "refuelAt-button",
                                id => SubmitFleetCommand(new RefuelAtCommand(fleetId, id)),
                                starTargetsSystem: false);
                        }
                        break;
                    case IssueOrderType.Trade:
                        if (selectedFleet == null)
                            break;
                        {
                            int fleetId = selectedFleet.Id;
                            ImGui.TextWrapped("Ships buy and sell around this body. The flagship's bridge sets how far they look.");
                            DisplayTargetTree(candidates,
                                e => e.HasView<BodyView>() && e.HasView<PositionView>(),
                                "trade-button",
                                id => SubmitFleetCommand(new FleetTradeCommand(fleetId, id)),
                                starTargetsSystem: true);
                        }
                        break;
                    case IssueOrderType.Haul:
                        if (selectedFleet == null)
                            break;
                        {
                            int fleetId = selectedFleet.Id;
                            ImGui.TextWrapped("Ships move posted goods between our colonies around this body. The flagship's bridge sets how far they look.");
                            DisplayTargetTree(candidates,
                                e => e.HasView<BodyView>() && e.HasView<PositionView>(),
                                "haul-button",
                                id => SubmitFleetCommand(new FleetFreighterCommand(fleetId, id)),
                                starTargetsSystem: true);
                        }
                        break;
                    case IssueOrderType.HaulContract:
                        DisplayHaulContract(candidates);
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

        private string FleetSpanLabel()
        {
            if(selectedFleet == null || string.IsNullOrEmpty(selectedFleet.CommandSpan))
                return CommandSpanLabels.Body;
            return selectedFleet.CommandSpan;
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
                ImGui.Separator();
                return;
            }

            ImGui.AlignTextToFramePadding();
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.Text("Fleet commander");
            ImGui.PopStyleColor();
            ImGui.SameLine();
            DisplayFleetCommanderValue();
            ImGui.SameLine(0, 28f);
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.Text("Command span");
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.Text(FleetSpanLabel());
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("How far this fleet's orders reach. It comes from the flagship's bridge.");
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
            int? selectedId = null)
        {
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
                DisplayTargetNode(root, children, stars, isTarget, coversWork, buttonPrefix, submit, starTargetsSystem, markDone, selectedId);
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
            int? selectedId)
        {
            if(node.IsBand)
            {
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
                            DisplayTargetNode(child, children, stars, isTarget, coversWork, buttonPrefix, submit, starTargetsSystem, markDone, selectedId);
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
            string span = orderShip != null ? CommandSpanLabels.Body : FleetSpanLabel();
            bool systemSpan = span == CommandSpanLabels.System;
            bool reachesChildren = systemSpan || span == CommandSpanLabels.Well;
            bool starClickable = entity.Kind == BodyKind.Star && starTargetsSystem && systemSpan;
            bool childWork = coversWork.Contains(entity.Id);
            bool showDone = childWork && !isTarget(entity) && markDone != null && markDone(entity);
            bool clickable = starClickable || (entity.Kind != BodyKind.Star && (isTarget(entity) || (showDone && reachesChildren)));

            var flags = ImGuiTreeNodeFlags.OpenOnArrow;
            if(entity.Kind == BodyKind.Star || showDone)
                flags |= ImGuiTreeNodeFlags.DefaultOpen;
            if(childList == null)
                flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;

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
                else if(nameHovered && entity.Kind == BodyKind.Star && starTargetsSystem && !systemSpan)
                {
                    ImGui.SetTooltip("The flagship's bridge does not cover the whole system.");
                }
            }

            if(childList != null && open)
            {
                EntitySnapshot? around = entity.Kind == BodyKind.Star ? entity : null;
                foreach(var child in SortNodes(childList, stars, around))
                    DisplayTargetNode(child, children, stars, isTarget, coversWork, buttonPrefix, submit, starTargetsSystem, markDone, selectedId);
                ImGui.TreePop();
            }
        }

        private void DisplayHaulContract(IEnumerable<EntitySnapshot> candidates)
        {
            if(selectedFleet == null)
                return;

            ImGui.TextWrapped("One posted good, from one of our colonies to another. The fleet splits that load across its holds.");

            var colonies = candidates
                .Where(e => e.Relation == OwnerRelation.Owned && e.Kind == BodyKind.Colony && e.HasView<MarketView>())
                .OrderBy(e => NameOf(e), StringComparer.Ordinal)
                .ToList();
            if(colonies.Count == 0)
            {
                ImGui.Text("No owned colonies with a market in this system.");
                return;
            }

            if(colonies.All(colony => colony.Id != haulSourceId))
                haulSourceId = -1;
            if(haulDestId == haulSourceId || colonies.All(colony => colony.Id != haulDestId))
                haulDestId = -1;

            DisplayHelpers.Header("From");
            DisplayTargetTree(candidates,
                e => e.Relation == OwnerRelation.Owned && e.Kind == BodyKind.Colony && e.HasView<MarketView>(),
                "haul-from",
                id =>
                {
                    haulSourceId = id;
                    if(haulDestId == id)
                        haulDestId = -1;
                },
                starTargetsSystem: false,
                selectedId: haulSourceId);

            DisplayHelpers.Header("To");
            DisplayTargetTree(candidates,
                e => e.Id != haulSourceId && e.Relation == OwnerRelation.Owned && e.Kind == BodyKind.Colony && e.HasView<MarketView>(),
                "haul-to",
                id => haulDestId = id,
                starTargetsSystem: false,
                selectedId: haulDestId);

            var source = colonies.FirstOrDefault(colony => colony.Id == haulSourceId);
            var dest = colonies.FirstOrDefault(colony => colony.Id == haulDestId);
            if(source == null || dest == null)
                return;

            var sourceBook = source.GetView<MarketView>();
            var destBook = dest.GetView<MarketView>();
            DisplayHelpers.Header("Cargo");
            bool any = false;
            if(sourceBook != null && destBook != null)
            {
                foreach(var sell in sourceBook.Goods.Where(good => good.SellQuantity > 0).OrderBy(good => good.Name, StringComparer.Ordinal))
                {
                    var buy = destBook.Goods.FirstOrDefault(good => good.CargoId == sell.CargoId && good.BuyQuantity > 0);
                    if(buy == null)
                        continue;
                    any = true;
                    long units = Math.Min(sell.SellQuantity, buy.BuyQuantity);
                    if(ImGui.Button($"{sell.Name} ({units})###haul-cargo-{sell.CargoId}"))
                    {
                        SubmitFleetCommand(new FleetHaulContractCommand(selectedFleet.Id, source.Id, dest.Id, sell.CargoId));
                    }
                }
            }

            if(!any)
                ImGui.Text("No posted haul between these colonies.");
        }

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

            var xPosition = ImGui.GetCursorPosX();
            Vector2 windowContentSize = ImGui.GetContentRegionAvail();

            if (ImGui.BeginChild("Fleet Orders", new Vector2(Styles.LeftColumnWidthLg, windowContentSize.Y), ImGuiChildFlags.Borders))
            {
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
                }

                if (orders.Count == 0)
                {
                    ImGui.Text("None");
                }
                else if (ImGui.BeginTable("SubjectOrders", 3, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
                {
                    ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.None, 0.1f);
                    ImGui.TableSetupColumn("Order", ImGuiTableColumnFlags.None, 0.7f);
                    ImGui.TableSetupColumn("Pause", ImGuiTableColumnFlags.None, 0.2f);
                    ImGui.TableHeadersRow();

                    for (int i = 0; i < orders.Count; i++)
                    {
                        var order = orders[i];
                        ImGui.TableNextColumn();
                        ImGui.Text((i + 1).ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(order.Name);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(order.IsRunning ? "Running" : order.IsFinished ? "Finished" : order.Details);
                        ImGui.TableNextColumn();
                        if (order.OrderId.Length > 0)
                        {
                            bool pause = order.PauseOnAction;
                            if (ImGui.Checkbox("##pause-" + order.OrderId, ref pause))
                            {
                                SubmitFleetCommand(new SetOrderPauseCommand(holderId, order.OrderId, pause));
                            }
                        }
                    }

                    ImGui.EndTable();
                }
            }
            ImGui.EndChild();
            ImGui.SetCursorPosX(xPosition);
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
            var flags = ImGuiTreeNodeFlags.DefaultOpen;
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
