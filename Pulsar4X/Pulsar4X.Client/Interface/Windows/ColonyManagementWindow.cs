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

        /// <summary>The colony power grid: demand by facility against generation. Advisory only.</summary>
        private static void DisplayPower(ColonyPowerView power)
        {
            ImGui.TextWrapped("Advisory: the grid is not enforced yet. Nothing is slowed by a shortfall, no fuel is burnt and batteries are not charged.");
            ImGui.Separator();

            if (ImGui.BeginTable("PowerSummary", 2, ImGuiTableFlags.SizingFixedFit))
            {
                DisplayHelpers.PrintRow("Demand", Stringify.Power(power.Demand_kW));
                DisplayHelpers.PrintRow("Generation", Stringify.Power(power.Generation_kW));
                DisplayHelpers.PrintRow("Delivered", Stringify.Power(power.Delivered_kW),
                    tooltipTwo: $"After {(1 - power.GridEfficiency) * 100:0.#}% grid loss");
                DisplayHelpers.PrintRow("Balance", (power.Balance_kW < 0 ? "-" : "+") + Stringify.Power(Math.Abs(power.Balance_kW)));
                if (power.StoreMax_kJ > 0)
                    DisplayHelpers.PrintRow("Grid storage", Stringify.Energy(power.StoreMax_kJ));
                ImGui.EndTable();
            }
            if (power.Balance_kW < 0)
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), "Generation does not cover demand. Lower tiers would go short first.");

            ImGui.SeparatorText("Demand by facility");
            if (ImGui.BeginTable("PowerConsumers", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Facility");
                ImGui.TableSetupColumn("Tier");
                ImGui.TableSetupColumn("Count");
                ImGui.TableSetupColumn("Draw");
                ImGui.TableHeadersRow();
                foreach (var c in power.Consumers)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(c.Name);
                    if (c.DesignId == "life-support" && ImGui.IsItemHovered())
                        ImGui.SetTooltip($"{power.LifeSupportColonists:N0} colonists: a base draw per person, more on hostile worlds");
                    ImGui.TableNextColumn();
                    ImGui.Text(c.Tier);
                    ImGui.TableNextColumn();
                    ImGui.Text(c.DesignId == "life-support" ? "" : c.Count.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(Stringify.Power(c.Draw_kW));
                }
                ImGui.EndTable();
            }

            ImGui.SeparatorText("Generation and storage");
            if (power.Plants.Count == 0)
                ImGui.TextDisabled("No power plants.");
            else if (ImGui.BeginTable("PowerPlants", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Plant");
                ImGui.TableSetupColumn("Kind");
                ImGui.TableSetupColumn("Count");
                ImGui.TableSetupColumn("Output");
                ImGui.TableSetupColumn("Fuel");
                ImGui.TableHeadersRow();
                foreach (var p in power.Plants)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(p.Name);
                    ImGui.TableNextColumn();
                    ImGui.Text(p.Kind);
                    ImGui.TableNextColumn();
                    ImGui.Text(p.Count.ToString());
                    ImGui.TableNextColumn();
                    if (p.Kind == "Battery")
                        ImGui.Text(Stringify.Energy(p.Store_kJ));
                    else
                        ImGui.Text($"{Stringify.Power(p.Output_kW)} / {Stringify.Power(p.Nameplate_kW)}");
                    if (p.Note != null)
                    {
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), "(!)");
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(p.Note);
                    }
                    ImGui.TableNextColumn();
                    ImGui.Text(p.FuelName == null ? "" : $"{p.FuelName}, {Stringify.Mass(p.FuelAtFull_kgPerDay)}/day");
                }
                ImGui.EndTable();
            }

            ImGui.SeparatorText("If power ran short");
            if (ImGui.BeginTable("PowerTiers", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Tier (served in order)");
                ImGui.TableSetupColumn("Demand");
                ImGui.TableSetupColumn("Would get");
                ImGui.TableHeadersRow();
                foreach (var t in power.Tiers.Where(t => t.Demand_kW > 0))
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(t.Tier);
                    ImGui.TableNextColumn();
                    ImGui.Text(Stringify.Power(t.Demand_kW));
                    ImGui.TableNextColumn();
                    ImGui.Text($"{t.Satisfaction * 100:0.#}%");
                }
                ImGui.EndTable();
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

            _academyRecruits[row.ComponentId] = recruits;
            _academyOfficer[row.ComponentId] = selected;
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
