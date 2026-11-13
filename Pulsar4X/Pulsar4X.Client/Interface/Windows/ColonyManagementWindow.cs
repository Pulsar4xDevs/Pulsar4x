using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;
// Engine using: Stringify formatting helpers, plus the deferred Production/Construction tabs below.

namespace Pulsar4X.Client
{
    public class ColonyManagementWindow : UniquePulsarGuiWindow<ColonyManagementWindow>
    {
        private Dictionary<string, bool> isExpanded = new();

        // The colony is selected by entity id (+ its system) and re-resolved each frame: colonies are
        // entities in the system snapshots, which are replaced wholesale by server pushes.
        public int? SelectedColonyId { get; private set; } = null;
        private string? _selectedSystemId = null;

        private ColonyProductionDisplay? _productionDisplay;
        private ColonyConstructionDisplay? _constructionDisplay;
        private int _logShownId = -1;
        private int _logShownCount = -1;
        private readonly Dictionary<int, int> _academyRecruits = new();
        private readonly Dictionary<int, int> _academyOfficer = new();
        // The colony whose administrator chair the assign modal is editing.
        private int? _administratorChooserColonyId = null;

        internal static ColonyManagementWindow GetInstance()
        {
            if(_uiState.TryGetUniqueWindow<ColonyManagementWindow>(out var window))
            {
                return window;
            }

            return _uiState.AddUniqueWindow(new ColonyManagementWindow());
        }

        public void SelectColony(int colonyId, string systemId)
        {
            SelectedColonyId = colonyId;
            _selectedSystemId = systemId;
        }

        // Set by OpenRecruitment so the Recruitment tab is brought forward once.
        private bool _focusRecruitmentTab;

        // ImGui.NET only exposes the flags overload together with p_open, which adds a close
        // button; call the native entry with a null p_open instead.
        private static unsafe bool BeginTabItem(string label, ImGuiTabItemFlags flags)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(label + "\0");
            fixed (byte* text = bytes)
                return ImGuiNative.igBeginTabItem(text, null, flags) != 0;
        }

        /// <summary>Open this window on a colony with its Recruitment tab selected.</summary>
        public void OpenRecruitment(int colonyId, string systemId)
        {
            SelectColony(colonyId, systemId);
            _focusRecruitmentTab = true;
            SetActive(true);
        }

        internal override void Display()
        {
            if(!IsActive) return;

            var galaxy = _uiState.GameClient?.Galaxy;
            if(galaxy == null) return;

            ImGui.SetNextWindowSize(new Vector2(1200, 780), ImGuiCond.FirstUseEver);
            if(Window.Begin("Manage Colonies", ref IsActive))
            {
                // A missing selection, or one the latest push no longer has, opens on the first owned colony.
                // A selection that resolves, owned or not, is left alone.
                var resolved = ForeignEntities.ResolveColonySelection(
                    SelectedColonyId,
                    _selectedSystemId,
                    galaxy.KnownSystems.Select(s => (s.SystemId, (IEnumerable<EntitySnapshot>?)galaxy.GetSystem(s.SystemId)?.Entities)));
                if (resolved is { } pick)
                {
                    if (pick.ColonyId != SelectedColonyId || pick.SystemId != _selectedSystemId)
                        SelectColony(pick.ColonyId, pick.SystemId);
                }
                else
                {
                    SelectedColonyId = null;
                    _selectedSystemId = null;
                }

                Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                if(ImGui.BeginChild("Colonies", new Vector2(Styles.LeftColumnWidthLg, windowContentSize.Y), ImGuiChildFlags.Borders))
                {
                    var systems = new List<(string SystemId, string SystemName, IEnumerable<EntitySnapshot>? Entities)>();
                    foreach (var summary in galaxy.KnownSystems)
                    {
                        var system = galaxy.GetSystem(summary.SystemId);
                        systems.Add((summary.SystemId, summary.Name, system?.Entities));
                    }
                    DisplayColonySections(
                        ForeignEntities.OwnedBySystem(systems),
                        ForeignEntities.ForeignByFaction(systems, galaxy.Stances));
                }
                ImGui.EndChild();

                // Re-resolve after the list so a click this frame opens that colony.
                var selectedSystem = _selectedSystemId == null ? null : galaxy.GetSystem(_selectedSystemId);
                var selectedColony = SelectedColonyId is { } id ? selectedSystem?.GetEntity(id) : null;

                if (selectedColony != null && selectedSystem != null)
                {
                    ImGui.SameLine();
                    DisplaySelectedColony(selectedSystem, selectedColony);
                }

                DisplayAdministratorChooser(galaxy);
            }
            Window.End();
        }

        private void DisplayColonySections(
            IReadOnlyList<ForeignEntities.SystemColonies> owned,
            IReadOnlyList<ForeignEntities.ForeignFactionGroup> foreign)
        {
            if (owned.Count > 0 && ImGui.CollapsingHeader("Your colonies###your-colonies", ImGuiTreeNodeFlags.DefaultOpen))
            {
                foreach (var system in owned)
                {
                    if (!isExpanded.ContainsKey(system.SystemId))
                        isExpanded.Add(system.SystemId, true);
                    ImGui.SetNextItemOpen(isExpanded[system.SystemId], ImGuiCond.Appearing);
                    if (ImGui.TreeNode(system.SystemName + "###" + system.SystemId))
                    {
                        foreach (var colony in system.Colonies)
                            DisplayColonyLeaf(colony, system.SystemId);
                        ImGui.TreePop();
                    }
                }
            }

            if (foreign.Count > 0 && ImGui.CollapsingHeader("Other factions###other-factions", ImGuiTreeNodeFlags.DefaultOpen))
            {
                foreach (var faction in foreign)
                {
                    ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
                    if (ImGui.TreeNode(faction.FactionName + "###faction-" + faction.FactionId))
                    {
                        if (faction.Systems.Count == 1)
                        {
                            var only = faction.Systems[0];
                            foreach (var colony in only.Colonies)
                                DisplayColonyLeaf(colony, only.SystemId);
                        }
                        else
                        {
                            foreach (var system in faction.Systems)
                            {
                                string key = "faction-" + faction.FactionId + "-sys-" + system.SystemId;
                                if (!isExpanded.ContainsKey(key))
                                    isExpanded.Add(key, true);
                                ImGui.SetNextItemOpen(isExpanded[key], ImGuiCond.Appearing);
                                if (ImGui.TreeNode(system.SystemName + "###" + key))
                                {
                                    foreach (var colony in system.Colonies)
                                        DisplayColonyLeaf(colony, system.SystemId);
                                    ImGui.TreePop();
                                }
                            }
                        }
                        ImGui.TreePop();
                    }
                }
            }
        }

        private void DisplayColonyLeaf(EntitySnapshot colony, string systemId)
        {
            var population = colony.GetView<ColonyView>()?.Population ?? 0;
            var name = colony.GetView<NameView>()?.Name ?? "Unknown";
            var flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
            if (SelectedColonyId == colony.Id)
                flags |= ImGuiTreeNodeFlags.Selected;

            ImGui.TreeNodeEx(name + " (" + Stringify.Quantity(population) + ")###colony-" + colony.Id, flags);
            if (ImGui.IsItemClicked())
                SelectColony(colony.Id, systemId);
        }

        private void DisplaySelectedColony(IClientSystem selectedSystem, EntitySnapshot selectedColony)
        {
            var tabs = ForeignEntities.ColonyTabsFor(selectedColony);
            if (ImGui.BeginChild("ColoniesTabs"))
            {
                ImGui.BeginTabBar("EconomicsTabBar", ImGuiTabBarFlags.None);

                if (tabs.HasFlag(ForeignEntities.ColonyTabs.Summary) && ImGui.BeginTabItem("Summary"))
                {
                    DisplaySummary(selectedColony, selectedSystem);
                    ImGui.EndTabItem();
                }
                if (tabs.HasFlag(ForeignEntities.ColonyTabs.Production) && ImGui.BeginTabItem("Production"))
                {
                    _productionDisplay ??= new ColonyProductionDisplay();
                    _productionDisplay.Display(selectedColony.Id, selectedColony.GetView<IndustryView>(), _uiState);
                    ImGui.EndTabItem();
                }
                if (tabs.HasFlag(ForeignEntities.ColonyTabs.Construction) && ImGui.BeginTabItem("Construction"))
                {
                    _constructionDisplay ??= new ColonyConstructionDisplay();
                    _constructionDisplay.Display(selectedColony.Id, selectedColony.GetView<ConstructionView>(), _uiState);
                    ImGui.EndTabItem();
                }
                if (tabs.HasFlag(ForeignEntities.ColonyTabs.Market) && ImGui.BeginTabItem("Market"))
                {
                    var marketView = selectedColony.GetView<MarketView>();
                    float bottomReserve = 0f;
                    if (ForeignEntities.IsReadOnly(selectedColony))
                        bottomReserve = MarketBarsDisplay.BottomReserve(marketView, selectedSystem);
                    MarketTabDisplay.Display(
                        selectedColony,
                        marketView,
                        selectedColony.GetView<MarketHistoryView>(),
                        selectedColony.GetView<IntelBookView>(),
                        selectedSystem.SystemId,
                        selectedSystem.DateTime,
                        _uiState,
                        false,
                        bottomReserve);
                    if (ForeignEntities.IsReadOnly(selectedColony))
                        MarketBarsDisplay.DisplayBuyAndShip(selectedColony, marketView, selectedSystem, _uiState);
                    ImGui.EndTabItem();
                }
                if (tabs.HasFlag(ForeignEntities.ColonyTabs.Shipping) && ImGui.BeginTabItem("Shipping"))
                {
                    ShippingDisplay.Display(
                        selectedColony,
                        selectedColony.GetView<MarketView>(),
                        selectedColony.GetView<IntelBookView>(),
                        selectedSystem,
                        _uiState);
                    ImGui.EndTabItem();
                }
                if (tabs.HasFlag(ForeignEntities.ColonyTabs.Mining)
                    && selectedColony.GetView<ColonyMiningView>() is { } mining && ImGui.BeginTabItem("Mining"))
                {
                    mining.Display();
                    ImGui.EndTabItem();
                }
                if (tabs.HasFlag(ForeignEntities.ColonyTabs.NavalAcademy)
                    && selectedColony.GetView<NavalAcademyView>() is { } academy && ImGui.BeginTabItem("Naval Academy"))
                {
                    DisplayNavalAcademy(selectedColony.Id, academy);
                    ImGui.EndTabItem();
                }
                if (tabs.HasFlag(ForeignEntities.ColonyTabs.Recruitment)
                    && selectedColony.GetView<RecruitmentView>() is { } recruitment
                    && BeginTabItem("Recruitment", _focusRecruitmentTab ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
                {
                    _focusRecruitmentTab = false;
                    DisplayRecruitment(selectedColony.Id, recruitment);
                    ImGui.EndTabItem();
                }
                if (tabs.HasFlag(ForeignEntities.ColonyTabs.Power)
                    && selectedColony.GetView<ColonyPowerView>() is { } power && ImGui.BeginTabItem("Power"))
                {
                    DisplayPower(power);
                    ImGui.EndTabItem();
                }
                if (tabs.HasFlag(ForeignEntities.ColonyTabs.Log))
                {
                    if (ImGui.BeginTabItem("Log"))
                    {
                        ShipLogDisplay.Draw(selectedColony, ref _logShownId, ref _logShownCount);
                        ImGui.EndTabItem();
                    }
                    else
                    {
                        _logShownId = -1;
                    }
                }
                else
                {
                    _logShownId = -1;
                }
                ImGui.EndTabBar();
            }
            ImGui.EndChild();
        }

        static readonly Vector4 PowerDemandColour = new(1f, 0.6f, 0.2f, 1f);
        static readonly Vector4 PowerGenerationColour = new(0.35f, 0.75f, 1f, 1f);
        static readonly Vector4 PowerDeliveredColour = PowerRing.FullColour;
        static readonly Vector4 PowerShortfallColour = new(0.9f, 0.2f, 0.2f, 0.35f);

        const string ColonyPowerHowItWorks = "Plants burn fuel from the colony stockpile once a day. In a shortfall, higher tiers are served first and industry, mining and research slow to their tier's share. Grid batteries charge from spare power and cover a shortfall before any tier goes short.";

        /// <summary>
        /// The colony power grid: a summary beside 90 days of history, then a stacked demand bar by
        /// tier with one compact row per tier, then a stacked generation bar with one row per plant, then
        /// the Storage section's two-way charge bar (PowerBars, shared with the ship Power tab).
        /// </summary>
        private static void DisplayPower(ColonyPowerView power)
        {
            if (ImGui.BeginTable("PowerTop", 2, ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Summary", ImGuiTableColumnFlags.WidthFixed, 250f);
                ImGui.TableSetupColumn("History", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                PowerSummary(power);
                ImGui.TableNextColumn();
                PowerHistoryChart(power.History, new Vector2(Math.Max(200f, ImGui.GetContentRegionAvail().X), 150f));
                ImGui.EndTable();
            }

            ImGui.SeparatorText("Demand by tier (served top first)");
            PowerTiers(power);

            ImGui.SeparatorText("Generation");
            PowerPlants(power);

            var batteries = power.Plants.Where(p => p.Kind == "Battery").ToList();
            PowerBars.Storage("##colonyStorage", power.Battery_kW, power.StoredFraction, power.StoreMax_kJ,
                power.MaxCharge_kW, power.MaxDischarge_kW, power.TimeToFullOrEmpty_s,
                batteries.Select(b => (b.Count > 1 ? $"{b.Count} x {b.Name}" : b.Name, Stringify.Energy(b.Store_kJ) + (b.Note != null ? " (" + b.Note + ")" : ""))).ToList());
        }

        private static void PowerSummary(ColonyPowerView power)
        {
            double share = PowerRing.Share(power);
            ImGui.PushStyleColor(ImGuiCol.Text, PowerRing.ColourFor(share));
            string words = PowerRing.WordsFor(share);
            ImGui.TextUnformatted(char.ToUpperInvariant(words[0]) + words.Substring(1));
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.TextDisabled("[?]");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(ColonyPowerHowItWorks);

            // PrintRow uses the Columns API (a nested table would not show it).
            ImGui.Columns(2, "PowerSummaryCols", false);
            ImGui.SetColumnWidth(0, 96f);
            DisplayHelpers.PrintRow("Demand", Stringify.Power(power.Demand_kW));
            DisplayHelpers.PrintRow("Generation", Stringify.Power(power.Generation_kW),
                tooltipTwo: "What the plants can give now");
            if (power.HasDispatched)
                DisplayHelpers.PrintRow("Running at", Stringify.Power(power.Dispatched_kW),
                    tooltipTwo: "Average plant output in the last daily pass"
                        + (power.LastDispatch is { } last ? $" ({last:yyyy-MM-dd HH:mm})" : ""));
            else
                DisplayHelpers.PrintRow("Running at", "awaiting daily pass",
                    tooltipTwo: "The plants have not had their first daily pass yet");
            DisplayHelpers.PrintRow("Available", Stringify.Power(power.Delivered_kW),
                tooltipTwo: $"After grid loss ({(1 - power.GridEfficiency) * 100:0.#}%%)");
            DisplayHelpers.PrintRow("Balance", (power.Balance_kW < 0 ? "-" : "+") + Stringify.Power(Math.Abs(power.Balance_kW)),
                separator: power.StoreMax_kJ > 0);
            if (power.StoreMax_kJ > 0)
                DisplayHelpers.PrintRow("Grid storage", $"{Math.Clamp(power.StoredFraction, 0, 1) * 100:0}%% of {Stringify.Energy(power.StoreMax_kJ, "0.#")}",
                    tooltipTwo: "Charge level and capacity; see Storage below", separator: false);
            ImGui.Columns(1);
            if (power.Balance_kW < 0)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, PowerDemandColour);
                ImGui.PushTextWrapPos(0f);
                ImGui.TextUnformatted("Generation does not cover demand. Lower tiers go short first.");
                ImGui.PopTextWrapPos();
                ImGui.PopStyleColor();
            }
        }

        /// <summary>One stacked bar of demand by tier (hatched where unmet), then one compact row per tier; facilities on hover or click.</summary>
        private static void PowerTiers(ColonyPowerView power)
        {
            var segments = new List<PowerSegment>();
            var rows = new List<PowerRow>();
            foreach (var tier in power.Tiers.OrderBy(t => t.TierRank))
            {
                var consumers = power.Consumers.Where(c => c.TierRank == tier.TierRank).ToList();
                if (tier.Demand_kW <= 0 && consumers.Count == 0)
                    continue;

                double share = Math.Clamp(tier.Satisfaction, 0, 1);
                Vector4 chip = PowerBars.GroupColour(tier.TierRank - 1);
                Vector4 state = PowerRing.ColourFor(share);
                string words = PowerRing.WordsFor(share);
                segments.Add(new PowerSegment(tier.Tier, tier.Demand_kW, share, chip,
                    $"{tier.Tier}: {Stringify.Power(tier.Demand_kW)}, {words}"));

                string tip = $"{tier.Tier}: {Stringify.Power(tier.Demand_kW)} demand, {words} (tier {tier.TierRank})";
                if (consumers.Any(c => c.DesignId == "life-support"))
                    tip += $"\n{power.LifeSupportColonists:N0} colonists: a base draw per person, more on hostile worlds";
                if (consumers.Any(c => c.Nameplate_kW > c.Draw_kW + 1e-6))
                    tip += "\nProduction draws its jobs' build energy, up to the nameplate. Idle, it draws a tenth of the nameplate.";
                if (consumers.Any(c => c.DesignId == "scrapping"))
                    tip += "\nScrapping: facility upgrades taking a site-built facility apart.";
                rows.Add(new PowerRow
                {
                    Id = tier.TierRank.ToString(),
                    Chip = chip,
                    Name = tier.Tier,
                    Value = Stringify.Power(tier.Demand_kW),
                    Percent = PowerBars.Percent(tier.Demand_kW, power.Demand_kW),
                    Fill = (float)share,
                    FillColour = state,
                    Trailing = words,
                    TrailingColour = state,
                    Tooltip = tip,
                    Detail = consumers.Select(c => (
                        c.DesignId == "life-support" ? c.Name : $"{c.Count} x {c.Name}",
                        Stringify.Power(c.Draw_kW) + (c.Nameplate_kW > c.Draw_kW + 1e-6 ? " of " + Stringify.Power(c.Nameplate_kW) : ""))).ToList(),
                });
            }
            if (rows.Count == 0)
            {
                ImGui.TextDisabled("Nothing on this colony draws power.");
                return;
            }
            // Charging is its own slice so the bar adds up to demand plus what goes into the batteries.
            if (PowerBars.ChargeSegment(power.Battery_kW) is { } charge)
                segments.Add(charge);
            PowerBars.StackedBar("##colonyDemandBar", segments, power.Demand_kW + Math.Max(0, power.Battery_kW));
            PowerBars.Rows("##colonyTiers", rows);
        }

        /// <summary>A stacked bar of plant output (grey for idle capacity) with a demand marker, then one row per plant with its fuel inline.</summary>
        private static void PowerPlants(ColonyPowerView power)
        {
            if (power.Plants.Count == 0)
            {
                ImGui.TextDisabled("No power plants.");
                return;
            }
            var generators = power.Plants.Where(p => p.Kind != "Battery").ToList();
            double nameplate = generators.Sum(p => p.Nameplate_kW);
            double output = generators.Sum(p => p.Output_kW);
            var segments = new List<PowerSegment>();
            foreach (var p in generators)
                segments.Add(new PowerSegment(p.Name, p.Output_kW, 1, PowerBars.SourceColour(p.Kind),
                    $"{p.Name}: {Stringify.Power(p.Output_kW)} of {Stringify.Power(p.Nameplate_kW)}"));
            if (PowerBars.DischargeSegment(power.Battery_kW) is { } discharge)
                segments.Add(discharge);
            if (nameplate > output)
                segments.Add(new PowerSegment("Idle", nameplate - output, 0, PowerBars.UnservedColour,
                    $"{Stringify.Power(nameplate - output)} installed but not available now"));
            if (generators.Count > 0)
                PowerBars.StackedBar("##colonyGenBar", segments,
                    Math.Max(nameplate + Math.Max(0, -power.Battery_kW), power.Demand_kW + Math.Max(0, power.Battery_kW)),
                    marker_kW: power.Demand_kW > 0 ? power.Demand_kW : null, markerTooltip: $"Demand {Stringify.Power(power.Demand_kW)}");

            var rows = new List<PowerRow>();
            var shownFuel = new HashSet<string>();
            foreach (var p in power.Plants)
            {
                string name = p.Count > 1 ? $"{p.Name} x{p.Count}" : p.Name;
                // Batteries have their own Storage section below.
                if (p.Kind == "Battery")
                    continue;
                var fuel = p.FuelName == null ? null : power.Fuel.FirstOrDefault(f => f.FuelName == p.FuelName);
                if (fuel != null)
                    shownFuel.Add(fuel.FuelId);
                string tip = $"{p.Name} ({p.Kind}): {Stringify.Power(p.Output_kW)} of {Stringify.Power(p.Nameplate_kW)}";
                if (p.FuelName != null)
                    tip += $"\n{p.FuelName}: {Stringify.Mass(p.FuelAtFull_kgPerDay)}/day at full output";
                if (fuel != null)
                    tip += "\n" + FuelLongText(fuel);
                if (p.Note != null)
                    tip += "\n" + p.Note;
                rows.Add(new PowerRow
                {
                    Id = p.DesignId,
                    Chip = PowerBars.SourceColour(p.Kind),
                    Name = name,
                    Value = Stringify.Power(p.Output_kW),
                    Percent = PowerBars.Percent(p.Output_kW, power.Generation_kW),
                    Fill = p.Nameplate_kW > 0 ? (float)Math.Clamp(p.Output_kW / p.Nameplate_kW, 0, 1) : 0f,
                    FillColour = PowerBars.SourceColour(p.Kind),
                    Trailing = p.Note ?? (fuel != null ? FuelShortText(fuel) : p.Kind == "Solar" ? "sunlight" : p.FuelName ?? ""),
                    TrailingColour = p.Note != null ? PowerDemandColour : null,
                    Tooltip = tip,
                });
            }
            // A stocked fuel no plant names (e.g. after the plant was removed).
            foreach (var f in power.Fuel.Where(f => !shownFuel.Contains(f.FuelId)))
            {
                rows.Add(new PowerRow
                {
                    Id = "fuel-" + f.FuelId,
                    Chip = Styles.DescriptiveColor,
                    Name = f.FuelName,
                    Value = Stringify.Mass(f.Stock_kg),
                    Trailing = "in stock",
                    Tooltip = FuelLongText(f),
                });
            }
            PowerBars.Rows("##colonyPlants", rows);
        }

        /// <summary>"Fissile fuels, 359 kg, 730 d" for the row.</summary>
        private static string FuelShortText(ColonyPowerFuelRow f)
            => $"{f.FuelName}, {Stringify.Mass(f.Stock_kg, "0.#")}" + (f.RunwayDays is { } d ? $", {d:N0} d" : "");

        private static string FuelLongText(ColonyPowerFuelRow f)
            => $"In stock {Stringify.Mass(f.Stock_kg)}; burnt last day {Stringify.Mass(f.BurntLastDay_kg)}; "
               + (f.RunwayDays is { } d ? $"lasts {d:N0} days at that burn" : "nothing burnt, so no runway");

        /// <summary>
        /// Demand, generation and delivered power over the stored days, drawn with the window draw list.
        /// Where delivered falls below demand the gap is shaded. Hover shows that day's values.
        /// </summary>
        private static void PowerHistoryChart(IReadOnlyList<ColonyPowerSampleView> history, Vector2 size)
        {
            ImGui.TextColored(PowerDemandColour, "- Demand");
            ImGui.SameLine();
            ImGui.TextColored(PowerGenerationColour, "- Generation");
            ImGui.SameLine();
            ImGui.TextColored(PowerDeliveredColour, "- Available");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.9f, 0.3f, 0.3f, 1f), "# Shortfall");

            if (history.Count < 2)
            {
                ImGui.TextDisabled("History builds daily");
                return;
            }

            const float axisLeft = 70f;
            const float axisBottom = 18f;
            var draw = ImGui.GetWindowDrawList();
            var origin = ImGui.GetCursorScreenPos();
            ImGui.InvisibleButton("PowerHistoryPlot", size);
            bool hovered = ImGui.IsItemHovered();

            float x0 = origin.X + axisLeft;
            float x1 = origin.X + size.X - 4f;
            float y0 = origin.Y + 4f;
            float y1 = origin.Y + size.Y - axisBottom;

            double max = 0;
            foreach (var h in history)
                max = Math.Max(max, Math.Max(h.Demand_kW, Math.Max(h.Generation_kW, h.Delivered_kW)));
            if (max <= 0)
                max = 1;

            int n = history.Count;
            float X(int i) => x0 + (x1 - x0) * i / (n - 1);
            float Y(double kW) => y1 - (float)(kW / max) * (y1 - y0);

            uint axisColour = ImGui.GetColorU32(ImGuiCol.TextDisabled);
            uint gridColour = ImGui.GetColorU32(ImGuiCol.Border);
            draw.AddRect(new Vector2(x0, y0), new Vector2(x1, y1), gridColour);
            foreach (float f in new[] { 0f, 0.5f, 1f })
            {
                float y = Y(max * f);
                draw.AddLine(new Vector2(x0, y), new Vector2(x1, y), gridColour);
                string label = Stringify.Power(max * f);
                var labelSize = ImGui.CalcTextSize(label);
                draw.AddText(new Vector2(x0 - labelSize.X - 4f, y - labelSize.Y / 2), axisColour, label);
            }
            string firstDay = history[0].Date.ToString("yyyy-MM-dd");
            string lastDay = history[n - 1].Date.ToString("yyyy-MM-dd");
            draw.AddText(new Vector2(x0, y1 + 2f), axisColour, firstDay);
            draw.AddText(new Vector2(x1 - ImGui.CalcTextSize(lastDay).X, y1 + 2f), axisColour, lastDay);
            string daysLabel = n + " days";
            draw.AddText(new Vector2((x0 + x1 - ImGui.CalcTextSize(daysLabel).X) / 2, y1 + 2f), axisColour, daysLabel);

            uint shortfall = ImGui.GetColorU32(PowerShortfallColour);
            for (int i = 0; i < n - 1; i++)
            {
                var a = history[i];
                var b = history[i + 1];
                if (a.Delivered_kW >= a.Demand_kW && b.Delivered_kW >= b.Demand_kW)
                    continue;
                // The top edge never dips below delivered, so the quad does not fold over itself.
                draw.AddQuadFilled(
                    new Vector2(X(i), Y(Math.Max(a.Demand_kW, a.Delivered_kW))),
                    new Vector2(X(i + 1), Y(Math.Max(b.Demand_kW, b.Delivered_kW))),
                    new Vector2(X(i + 1), Y(b.Delivered_kW)),
                    new Vector2(X(i), Y(a.Delivered_kW)),
                    shortfall);
            }

            uint demandColour = ImGui.GetColorU32(PowerDemandColour);
            uint generationColour = ImGui.GetColorU32(PowerGenerationColour);
            uint deliveredColour = ImGui.GetColorU32(PowerDeliveredColour);
            for (int i = 0; i < n - 1; i++)
            {
                var a = history[i];
                var b = history[i + 1];
                draw.AddLine(new Vector2(X(i), Y(a.Generation_kW)), new Vector2(X(i + 1), Y(b.Generation_kW)), generationColour, 1.5f);
                draw.AddLine(new Vector2(X(i), Y(a.Demand_kW)), new Vector2(X(i + 1), Y(b.Demand_kW)), demandColour, 1.5f);
                draw.AddLine(new Vector2(X(i), Y(a.Delivered_kW)), new Vector2(X(i + 1), Y(b.Delivered_kW)), deliveredColour, 1.5f);
            }

            if (hovered)
            {
                float mouseX = ImGui.GetIO().MousePos.X;
                int day = (int)Math.Round((mouseX - x0) / (x1 - x0) * (n - 1));
                day = Math.Clamp(day, 0, n - 1);
                draw.AddLine(new Vector2(X(day), y0), new Vector2(X(day), y1), axisColour);
                var h = history[day];
                string tip = h.Date.ToString("yyyy-MM-dd")
                    + "\nDemand: " + Stringify.Power(h.Demand_kW)
                    + "\nGeneration: " + Stringify.Power(h.Generation_kW)
                    + "\nAvailable: " + Stringify.Power(h.Delivered_kW);
                if (h.Delivered_kW < h.Demand_kW)
                    tip += "\nShort: " + Stringify.Power(h.Demand_kW - h.Delivered_kW);
                ImGui.SetTooltip(tip);
            }
        }

        private void DisplaySummary(EntitySnapshot colony, IClientSystem system)
        {
            var colonyView = colony.GetView<ColonyView>();
            bool readOnly = ForeignEntities.IsReadOnly(colony);
            var planet = colonyView?.PlanetEntityId is { } planetId ? system.GetEntity(planetId) : null;

            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            var firstChildSize = new Vector2(windowContentSize.X * 0.33f, windowContentSize.Y);
            var secondChildSize = new Vector2(windowContentSize.X * 0.33f, windowContentSize.Y);
            var thirdChildSize = new Vector2(windowContentSize.X * 0.33f - (windowContentSize.X * 0.01f), windowContentSize.Y);
            if(ImGui.BeginChild("ColonySummary1", firstChildSize, ImGuiChildFlags.Borders))
            {
                var colonyName = colony.GetView<NameView>()?.Name ?? "Unknown";
                if(ImGui.CollapsingHeader("Colony", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Columns(2);
                    DisplayHelpers.PrintRow("Name", colonyName);
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.Text("Administrator");
                    ImGui.PopStyleColor();
                    ImGui.NextColumn();
                    if (readOnly)
                        ImGui.TextUnformatted(string.IsNullOrEmpty(colonyView?.AdministratorName) ? "Unknown" : colonyView!.AdministratorName!);
                    else
                        DisplayAdministratorButton(colonyView, colony.Id);
                    ImGui.NextColumn();
                    ImGui.Separator();
                    ImGui.Columns(1);
                }

                colonyView?.Display(colony.Id);

                // Owner-only: the projector only sends ColonyPowerView to the owner.
                if(colony.GetView<ColonyPowerView>() is { } power
                    && ImGui.CollapsingHeader("Power", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    PowerRing.Draw("##summaryPowerRing", power, Styles.DescriptiveColor);
                    ImGui.SameLine();
                    ImGui.BeginGroup();
                    ImGui.TextUnformatted($"Available {Stringify.Power(power.Delivered_kW)}");
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("After grid loss");
                    ImGui.TextUnformatted($"Demand {Stringify.Power(power.Demand_kW)}");
                    ImGui.PushStyleColor(ImGuiCol.Text, PowerRing.ColourFor(PowerRing.Share(power)));
                    ImGui.TextUnformatted(PowerRing.WordsFor(PowerRing.Share(power)));
                    ImGui.PopStyleColor();
                    ImGui.EndGroup();
                }

                // Owner-only: CrewPool is filled for the owning faction only.
                if(!readOnly && colonyView != null
                    && ImGui.CollapsingHeader("Crew pool", ImGuiTreeNodeFlags.DefaultOpen))
                    DisplayCrewPool(colonyView);

                if(colony.GetView<InfrastructureView>() is { } infrastructure
                    && ImGui.CollapsingHeader("Infrastructure", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    bool overCapacity = infrastructure.CapacityAvailable < 0;

                    ImGui.Columns(2);
                    DisplayHelpers.PrintRow("Provided", infrastructure.CapacityProvided.ToString("N0"));
                    DisplayHelpers.PrintRow("Used", infrastructure.CapacityRequired.ToString("N0"));
                    DisplayHelpers.PrintRow("Available", infrastructure.CapacityAvailable.ToString("N0"));
                    ImGui.Columns(1);

                    // Use TextUnformatted: ImGui.Text/TextColored treat the string as a printf
                    // format, so a literal '%' would be parsed as a format specifier.
                    if(overCapacity)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.4f, 0.4f, 1f));
                        ImGui.TextUnformatted($"Over capacity - all output reduced to {infrastructure.Efficiency * 100:0}%");
                        ImGui.PopStyleColor();
                    }
                    else
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                        ImGui.TextUnformatted($"Output at {infrastructure.Efficiency * 100:0}% of capacity");
                        ImGui.PopStyleColor();
                    }
                }

                var planetName = planet?.GetView<NameView>()?.Name ?? "Unknown";
                var body = planet?.GetView<BodyView>();

                if(ImGui.CollapsingHeader(planetName + " Information", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Columns(2);
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.Text("Name");
                    ImGui.PopStyleColor();
                    ImGui.NextColumn();
                    if(ImGui.SmallButton(planetName) && planet != null)
                    {
                        _uiState.EntityClicked(planet.Id, _uiState.SelectedStarSystemId, MouseButtons.Primary);
                    }
                    ImGui.NextColumn();
                    ImGui.Separator();
                    if(body != null)
                    {
                        DisplayHelpers.PrintRow("Type", body.BodyType);
                        DisplayHelpers.PrintRow("Tectonic Activity", body.Tectonics);
                        DisplayHelpers.PrintRow("Gravity", Stringify.Velocity(body.GravityMetresPerSec2));
                        DisplayHelpers.PrintRow("Temperature", body.SurfaceTemperatureC.ToString("#.#") + " C");
                        DisplayHelpers.PrintRow("Length of Day", body.DayLength.TotalHours + " hours");
                        DisplayHelpers.PrintRow("Tilt", body.AxialTiltDegrees.ToString("#") + "°");
                        DisplayHelpers.PrintRow("Magnetic Field", body.MagneticFieldMicroTesla.ToString("#") + " μT");
                        DisplayHelpers.PrintRow("Radiation Level", body.RadiationLevel.ToString("#"));
                        DisplayHelpers.PrintRow("Atmospheric Dust", body.AtmosphericDust.ToString("#"), separator: false);
                    }
                }
                ImGui.Columns(1);
                if(planet?.GetView<AtmosphereView>() is { } atmosphere)
                {
                    atmosphere.Display();
                }
                else
                {
                    if(ImGui.CollapsingHeader("Atmosphere", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.Text("No Atmosphere");
                    }
                }
            }
            ImGui.EndChild();

            ImGui.SameLine();
            if(ImGui.BeginChild("ColonySummary2", secondChildSize, ImGuiChildFlags.Borders))
            {
                if(ImGui.CollapsingHeader("Installations", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if(colony.GetView<InstallationsView>() is { } installations)
                    {
                        installations.Display(colony.Id, _uiState, readOnly: readOnly);
                    }
                    else if(readOnly)
                    {
                        ImGui.TextDisabled("Not visible.");
                    }
                }
            }
            ImGui.EndChild();

            ImGui.SameLine();
            if(ImGui.BeginChild("ColonySummary3", thirdChildSize, ImGuiChildFlags.Borders))
            {
                if(ImGui.CollapsingHeader("Stockpile", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if(colony.GetView<CargoStorageView>() is { } storage)
                    {
                        if(!readOnly)
                        {
                            var size = ImGui.GetContentRegionAvail();
                            ImGui.PushStyleColor(ImGuiCol.Button, Styles.Theme.Button.ToImVector4());
                            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Styles.Theme.ButtonHovered.ToImVector4());
                            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Styles.Theme.ButtonActive.ToImVector4());
                            if(ImGui.Button("Initiate Transfer", new Vector2(size.X - 8, 18)) && _selectedSystemId != null)
                            {
                                CreateTransferWindow.GetInstance().SetLeft(colony.Id, _selectedSystemId);
                                CreateTransferWindow.GetInstance().SetActive(true);
                            }
                            ImGui.PopStyleColor(3);
                        }

                        ImGui.Columns(2);
                        DisplayHelpers.PrintRow("Total Mass in Storage", Stringify.Mass(storage.TotalStoredMassKg));
                        DisplayHelpers.PrintRow("Transfer Rate", storage.TransferRateKgPerHour.ToString() + " kg/hr");
                        DisplayHelpers.PrintRow("Transfer Range", storage.TransferRangeDvMps.ToString("0.#") + " dV m/s", tooltipOne: "This is confusing as hell :D", separator: false);
                        ImGui.Columns(1);
                        storage.Display(colony.Id, _uiState, ImGuiTreeNodeFlags.None, readOnly: readOnly);
                    }
                    else if(readOnly)
                    {
                        ImGui.TextDisabled("Not visible.");
                    }
                }
            }
            ImGui.EndChild();
        }

        private void DisplayAdministratorButton(ColonyView? colonyView, int colonyId)
        {
            bool hasPost = colonyView?.HasAdministrativePost == true;
            string label = string.IsNullOrEmpty(colonyView?.AdministratorName) ? "None" : colonyView!.AdministratorName!;
            if (!hasPost)
                ImGui.BeginDisabled();
            if (ImGui.SmallButton(label + "###admin-" + colonyId) && hasPost)
                _administratorChooserColonyId = colonyId;
            if (!hasPost)
                ImGui.EndDisabled();
            if (ImGui.IsItemHovered(hasPost ? ImGuiHoveredFlags.None : ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(hasPost
                    ? "Seats a civilian as this colony's administrator."
                    : "This colony has no administrative post.");
        }

        private void DisplayAdministratorChooser(IClientGalaxy galaxy)
        {
            if (_administratorChooserColonyId is not int colonyId)
                return;

            var system = _selectedSystemId == null ? null : galaxy.GetSystem(_selectedSystemId);
            var colony = system?.GetEntity(colonyId);
            var view = colony?.GetView<ColonyView>();
            if (colony == null || view is not { HasAdministrativePost: true })
            {
                _administratorChooserColonyId = null;
                return;
            }

            var civilians = galaxy.Commanders
                .Where(commander => commander.Kind == CommanderKind.Civilian)
                .OrderBy(commander => commander.Name, StringComparer.Ordinal)
                .ToList();
            int current = view.AdministratorId;
            ResultModal.GetInstance().DisplayCustomButtons(
                "Assign Administrator",
                () => _administratorChooserColonyId = null,
                close =>
                {
                    int selected = DisplayHelpers.PeopleChooser(
                        _uiState,
                        civilians,
                        current,
                        "admin_" + colonyId,
                        close,
                        CivilianRow,
                        320f);
                    if (selected != current)
                    {
                        _uiState.GameClient?.SubmitCommandAsync(new AssignAdministratorCommand(colonyId, selected));
                        close();
                        return;
                    }

                    var candidates = colony.GetView<RecruitmentView>()?.FactionCandidates;
                    if (candidates == null)
                        return;
                    ImGui.Separator();
                    ImGui.Text("Or hire a candidate:");
                    if (candidates.Count == 0)
                        ImGui.TextDisabled("No candidates anywhere right now.");
                    foreach (var candidate in candidates)
                    {
                        if (ImGui.Button("Hire###hire-admin-" + candidate.Id))
                        {
                            _uiState.GameClient?.SubmitCommandAsync(new HireCandidateCommand(
                                candidate.ColonyId, candidate.Id, HireRole.Administrator, colonyId));
                            close();
                            return;
                        }
                        ImGui.SameLine();
                        ImGui.Text(candidate.Name + ", " + candidate.Location + ", wage " + candidate.Wage);
                    }
                });
        }

        private void DisplayRecruitment(int colonyId, RecruitmentView view)
        {
            if (view.Candidates.Count == 0)
            {
                ImGui.TextDisabled(view.EmptyReason ?? "No candidates right now.");
                return;
            }
            foreach (var candidate in view.Candidates)
            {
                ImGui.Text(candidate.Name + ", age " + candidate.Age);
                ImGui.Text("Lab: " + candidate.LabCategory + ", up to " + candidate.MaxLabs + " labs. Wage " + candidate.Wage + ".");
                if (ImGui.Button("Hire as administrator###recruit-admin-" + candidate.Id))
                    _uiState.GameClient?.SubmitCommandAsync(
                        new HireCandidateCommand(colonyId, candidate.Id, HireRole.Administrator));
                ImGui.SameLine();
                if (ImGui.Button("Hire as lab technician###recruit-lab-" + candidate.Id))
                    _uiState.GameClient?.SubmitCommandAsync(
                        new HireCandidateCommand(colonyId, candidate.Id, HireRole.LabTechnician));
                ImGui.Separator();
            }
        }

        private static string CivilianRow(CommanderSnapshot person)
        {
            string post = string.IsNullOrEmpty(person.AssignmentName) ? "" : ", " + person.AssignmentName;
            return person.Name + post;
        }

        /// <summary>Trained crew waiting at this colony: a stacked bar by species, then one row per species.</summary>
        private static void DisplayCrewPool(ColonyView colonyView)
        {
            ImGui.TextUnformatted(colonyView.CrewPool.ToString("N0") + " trained crew");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Academy crew courses graduate into this pool. Launches here draw crew from it,\nand ships scrapped here return theirs.");
            if (colonyView.CrewPool <= 0 || colonyView.CrewPoolBySpecies.Count == 0)
            {
                ImGui.TextDisabled("Train crew at a naval academy.");
                return;
            }
            PowerBars.StackedBar("##crewPoolBar", CrewBars.PoolSegments(colonyView.CrewPoolBySpecies), colonyView.CrewPool, 12f);
            CrewBars.CompactRows("##crewPoolRows", CrewBars.PoolRows(colonyView.CrewPoolBySpecies));
        }

        private void DisplayNavalAcademy(int colonyId, NavalAcademyView academy)
        {
            ImGui.Text("The colony has " + Stringify.Quantity(academy.Population) + " people.");

            var enrolled = new HashSet<int>();
            foreach (var row in academy.Academies)
            {
                foreach (var cohort in row.Cohorts)
                {
                    if (cohort.Course == AcademyCourseKind.Command && cohort.OfficerId >= 0)
                        enrolled.Add(cohort.OfficerId);
                }
            }

            var galaxy = _uiState.GameClient?.Galaxy;
            for (int i = 0; i < academy.Academies.Count; i++)
                DisplayAcademy(colonyId, academy, academy.Academies[i], i, galaxy, enrolled);
        }

        private void DisplayAcademy(int colonyId, NavalAcademyView academy, NavalAcademyClassView row, int index, IClientGalaxy? galaxy, HashSet<int> enrolled)
        {
            ImGui.Separator();
            ImGui.Text("Academy " + (index + 1));
            ImGui.SameLine();
            ImGui.TextColored(Styles.DescriptiveColor,
                row.SeatsInUse + " of " + row.ClassSize + " seats in use. "
                + row.TrainingPeriodMonths + " months. Mean cap " + row.MeanCap + ".");

            foreach (var cohort in row.Cohorts)
            {
                string date = cohort.Graduation.ToShortDateString();
                if (cohort.Course == AcademyCourseKind.Crew)
                    continue; // Listed under the crew course below.
                if (cohort.Course == AcademyCourseKind.Class)
                {
                    string word = cohort.Count == 1 ? "recruit" : "recruits";
                    ImGui.Text(cohort.Count + " " + word + ". Finishes " + date + ".");
                }
                else
                {
                    string who = "An officer";
                    var person = galaxy?.Commanders.FirstOrDefault(officer => officer.Id == cohort.OfficerId);
                    if (person != null)
                        who = OfficerLine(person);
                    ImGui.Text("Command course for " + who + ". Finishes " + date + ".");
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel###cancel-" + row.ComponentId + "-" + cohort.CohortId))
                    _uiState.GameClient?.SubmitCommandAsync(
                        new CancelAcademyCourseCommand(colonyId, row.ComponentId, cohort.CohortId));
            }

            int free = row.ClassSize - row.SeatsInUse;
            if (free < 0)
                free = 0;
            int max = academy.PeoplePerEnsign <= 0
                ? 0
                : (int)Math.Min((long)free, academy.Population / academy.PeoplePerEnsign);

            if (!_academyRecruits.TryGetValue(row.ComponentId, out int recruits))
                recruits = max >= 1 ? 1 : 0;
            if (recruits > max)
                recruits = max;
            if (recruits < 0)
                recruits = 0;

            if (!_academyOfficer.TryGetValue(row.ComponentId, out int selected))
                selected = -1;
            var ready = new List<CommanderSnapshot>();
            if (galaxy != null)
            {
                foreach (var officer in galaxy.Commanders)
                {
                    if (officer.Kind != CommanderKind.Navy)
                        continue;
                    if (CommandCourseRefusal(officer, row.MeanCap, enrolled) != null)
                        continue;
                    ready.Add(officer);
                }
            }
            if (selected >= 0 && ready.All(officer => officer.Id != selected))
                selected = -1;

            if (max == 0)
                ImGui.BeginDisabled();
            if (ImGui.SliderInt("Recruits###recruits-" + row.ComponentId, ref recruits, 0, Math.Max(max, 0)) && recruits > 0)
                selected = -1;
            if (max == 0)
            {
                ImGui.EndDisabled();
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    string why = free <= 0
                        ? "This academy has no free seats."
                        : "The colony has " + Stringify.Quantity(academy.Population) + " people. A recruit needs " + Stringify.Quantity(academy.PeoplePerEnsign) + ".";
                    ImGui.SetTooltip(why);
                }
            }

            long need = (long)recruits * academy.PeoplePerEnsign;
            ImGui.TextColored(Styles.DescriptiveColor,
                "Uses " + Stringify.Quantity(academy.PeoplePerEnsign) + " people per recruit. This run needs " + Stringify.Quantity(need) + ".");

            if (ready.Count == 0)
                ImGui.TextColored(Styles.DescriptiveColor, "No officers are ready for a command course.");
            foreach (var officer in ready)
            {
                bool isSelected = selected == officer.Id;
                string label = OfficerLine(officer) + ". " + CommandCourseLabel(officer, row.MeanCap)
                    + "###officer-" + row.ComponentId + "-" + officer.Id;
                if (ImGui.Selectable(label, isSelected))
                {
                    if (isSelected)
                        selected = -1;
                    else
                    {
                        selected = officer.Id;
                        recruits = 0;
                    }
                }
            }

            bool command = selected >= 0 && recruits == 0;
            bool canRun = free > 0 && (recruits > 0 || command);
            string caption = command ? "Run command course" : "Run class";
            if (!canRun)
                ImGui.BeginDisabled();
            if (ImGui.Button(caption + "###run-" + row.ComponentId, new Vector2(-1, 0)) && canRun)
            {
                if (command)
                    _uiState.GameClient?.SubmitCommandAsync(
                        new AcademyCommandCourseCommand(colonyId, row.ComponentId, selected));
                else
                    _uiState.GameClient?.SubmitCommandAsync(
                        new TrainAcademyClassCommand(colonyId, row.ComponentId, recruits));
            }
            if (!canRun)
            {
                ImGui.EndDisabled();
                if (free <= 0 && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    ImGui.SetTooltip("This academy has no free seats.");
            }

            DisplayCrewCourse(colonyId, row);

            _academyRecruits[row.ComponentId] = recruits;
            _academyOfficer[row.ComponentId] = selected;
        }

        /// <summary>
        /// The crew (ratings) course of one academy: a seats row in the Power tab look, the running crew cohorts
        /// with Cancel (the shared cancel command takes any cohort), and Train, disabled until the engine command lands.
        /// </summary>
        private void DisplayCrewCourse(int colonyId, NavalAcademyClassView row)
        {
            PowerBars.Rows("##crewCourse-" + row.ComponentId, new[] { CrewBars.CourseRow(row) }, 56f);

            foreach (var cohort in row.Cohorts)
            {
                if (cohort.Course != AcademyCourseKind.Crew)
                    continue;
                // Button first so the buttons line up whatever the counts and dates.
                if (ImGui.SmallButton("Cancel###cancel-crew-" + row.ComponentId + "-" + cohort.CohortId))
                    _uiState.GameClient?.SubmitCommandAsync(
                        new CancelAcademyCourseCommand(colonyId, row.ComponentId, cohort.CohortId));
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Stop this course. The trainees go back to the colony.");
                ImGui.SameLine();
                ImGui.TextUnformatted(cohort.Count.ToString("N0") + " crew in training, graduating " + cohort.Graduation.ToString("yyyy-MM-dd") + " into the pool.");
            }

            if (row.CrewCourseReason.Length > 0)
                CrewBars.Warning(row.CrewCourseReason);

            string reason = CrewCommandsPending.TrainCrew
                + (row.CrewCourseReason.Length > 0 ? "\n" + row.CrewCourseReason : "");
            CrewBars.PendingButton("Train crew###train-crew-" + row.ComponentId, reason, new Vector2(-1, 0));
        }

        private static string OfficerLine(CommanderSnapshot officer)
        {
            string rank = string.IsNullOrEmpty(officer.RankName) ? "Rank " + officer.Rank : officer.RankName;
            return rank + " " + officer.Name;
        }

        /// <summary>Null when the button may order the course. The sentence matches the server refusal.</summary>
        private static string? CommandCourseRefusal(CommanderSnapshot officer, int mean, HashSet<int> enrolled)
        {
            if (officer.IsAssigned)
                return "This officer is assigned.";
            if (enrolled.Contains(officer.Id))
                return "This officer is already on a course.";
            if (officer.ExperienceCap < mean)
                return null;
            if (officer.Rank < 4)
                return officer.Experience >= 8 ? null : "Experience " + officer.Experience + ". This course needs 8.";
            if (officer.Rank <= 5)
                return officer.Experience >= 16 ? null : "Experience " + officer.Experience + ". This course needs 16.";
            return "Rank is already 6. This course cannot raise it further.";
        }

        private static string CommandCourseLabel(CommanderSnapshot officer, int mean)
        {
            if (officer.ExperienceCap < mean)
                return "Raise the experience cap to " + mean + ".";
            if (officer.Rank < 4)
                return "Promote to rank 4.";
            return "Promote to rank 6.";
        }

    }
}
