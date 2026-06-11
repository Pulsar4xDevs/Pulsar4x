using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;
// Engine using: Stringify formatting helpers, plus the deferred Production/Construction tabs below.
using Pulsar4X.Engine;

namespace Pulsar4X.Client
{
    public class ColonyManagementWindow : PulsarGuiWindow
    {
        private Dictionary<string, bool> isExpanded = new();

        // The colony is selected by entity id (+ its system) and re-resolved each frame: colonies are
        // entities in the system snapshots, which are replaced wholesale by server pushes.
        public int? SelectedColonyId { get; private set; } = null;
        private string? _selectedSystemId = null;

        internal static ColonyManagementWindow GetInstance()
        {
            ColonyManagementWindow thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(ColonyManagementWindow)))
            {
                thisitem = new ColonyManagementWindow();
            }
            thisitem = (ColonyManagementWindow)_uiState.LoadedWindows[typeof(ColonyManagementWindow)];

            return thisitem;
        }

        public void SelectColony(int colonyId, string systemId)
        {
            SelectedColonyId = colonyId;
            _selectedSystemId = systemId;
        }

        internal override void Display()
        {
            if(!IsActive) return;

            var galaxy = _uiState.GameClient?.Galaxy;
            if(galaxy == null) return;

            if(Window.Begin("Manage Colonies", ref IsActive))
            {
                Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                if(ImGui.BeginChild("Colonies", new Vector2(Styles.LeftColumnWidth, windowContentSize.Y), ImGuiChildFlags.Borders))
                {
                    DisplayHelpers.Header("Select Colony to Manage");
                    foreach(var summary in galaxy.KnownSystems)
                    {
                        var system = galaxy.GetSystem(summary.SystemId);
                        if(system == null) continue;

                        if(!isExpanded.ContainsKey(summary.SystemId)) isExpanded.Add(summary.SystemId, true);
                        ImGui.SetNextItemOpen(isExpanded[summary.SystemId], ImGuiCond.Appearing);
                        if(ImGui.TreeNode(summary.Name + "###" + summary.SystemId))
                        {
                            foreach(var colony in system.Entities.Where(e => e.Kind == BodyKind.Colony && e.Relation == OwnerRelation.Owned))
                            {
                                var population = colony.GetView<ColonyView>()?.Population ?? 0;

                                if(SelectedColonyId == colony.Id)
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.75f, 0.25f, 0.25f, 1f));
                                }
                                else
                                {
                                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0f));
                                }

                                var name = colony.GetView<NameView>()?.Name ?? "Unknown";
                                if(ImGui.SmallButton(name + " (" + Stringify.Quantity(population) + ")###colony-" + colony.Id))
                                {
                                    SelectColony(colony.Id, summary.SystemId);
                                }
                                ImGui.PopStyleColor();
                            }
                            ImGui.TreePop();
                        }
                    }
                }
                ImGui.EndChild();

                // Re-resolve the selected colony against the current push.
                var selectedSystem = _selectedSystemId == null ? null : galaxy.GetSystem(_selectedSystemId);
                var selectedColony = SelectedColonyId is { } id ? selectedSystem?.GetEntity(id) : null;

                if (selectedColony == null || selectedSystem == null)
                {
                    Window.End();
                    return;
                }

                ImGui.SameLine();

                if(ImGui.BeginChild("ColoniesTabs"))
                {
                    ImGui.BeginTabBar("EconomicsTabBar", ImGuiTabBarFlags.None);

                    if(ImGui.BeginTabItem("Summary"))
                    {
                        DisplaySummary(selectedColony, selectedSystem);
                        ImGui.EndTabItem();
                    }
                    if(ImGui.BeginTabItem("Production"))
                    {
                        ColonyProductionDisplay.GetInstance()
                            .Display(selectedColony.Id, selectedColony.GetView<IndustryView>(), _uiState);
                        ImGui.EndTabItem();
                    }
                    if(ImGui.BeginTabItem("Construction"))
                    {
                        ColonyConstructionDisplay.GetInstance()
                            .Display(selectedColony.Id, selectedColony.GetView<ConstructionView>(), _uiState);
                        ImGui.EndTabItem();
                    }
                    if(selectedColony.GetView<ColonyMiningView>() is { } mining && ImGui.BeginTabItem("Mining"))
                    {
                        DisplayMining(mining);
                        ImGui.EndTabItem();
                    }
                    if(selectedColony.GetView<NavalAcademyView>() is { } academy && ImGui.BeginTabItem("Naval Academy"))
                    {
                        DisplayNavalAcademy(selectedColony.Id, academy);
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabBar();
                }
                ImGui.EndChild();
            }
            Window.End();
        }

        private void DisplaySummary(EntitySnapshot colony, IClientSystem system)
        {
            var colonyView = colony.GetView<ColonyView>();
            var planet = colonyView?.PlanetEntityId is { } planetId ? system.GetEntity(planetId) : null;

            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            var firstChildSize = new Vector2(windowContentSize.X * 0.33f, windowContentSize.Y);
            var secondChildSize = new Vector2(windowContentSize.X * 0.33f, windowContentSize.Y);
            var thirdChildSize = new Vector2(windowContentSize.X * 0.33f - (windowContentSize.X * 0.01f), windowContentSize.Y);
            if(ImGui.BeginChild("ColonySummary1", firstChildSize, ImGuiChildFlags.Borders))
            {
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
                DisplayPopulation(colony.Id, colonyView);
                ImGui.Columns(1);

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

                if(ImGui.CollapsingHeader("Installations", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if(colony.GetView<InstallationsView>() is { } installations)
                    {
                        installations.Display(colony.Id, _uiState);
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
                        var size = ImGui.GetContentRegionAvail();
                        ImGui.PushStyleColor(ImGuiCol.Button, Styles.Theme.Button.ToImVector4());
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Styles.Theme.ButtonHovered.ToImVector4());
                        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Styles.Theme.ButtonActive.ToImVector4());
                        if(ImGui.Button("Initiate Transfer", new Vector2(size.X - 8, 18)))
                        {
                            // Deferred: CreateTransferWindow is still engine-backed, so resolve the
                            // live entity from the selected id until that window is ported.
                            if(_uiState.Game != null
                                && _uiState.Game.GlobalManager.TryGetGlobalEntityById(colony.Id, out var colonyEntity))
                            {
                                CreateTransferWindow.GetInstance().SetLeft(colonyEntity);
                                CreateTransferWindow.GetInstance().SetActive(true);
                            }
                        }
                        ImGui.PopStyleColor(3);

                        ImGui.Columns(2);
                        DisplayHelpers.PrintRow("Total Mass in Storage", Stringify.Mass(storage.TotalStoredMassKg));
                        DisplayHelpers.PrintRow("Transfer Rate", storage.TransferRateKgPerHour.ToString() + " kg/hr");
                        DisplayHelpers.PrintRow("Transfer Range", storage.TransferRangeDvMps.ToString("0.#") + " dV m/s", tooltipOne: "This is confusing as hell :D", separator: false);
                        ImGui.Columns(1);
                        storage.Display(colony.Id, _uiState, ImGuiTreeNodeFlags.None);
                    }
                }
            }
            ImGui.EndChild();
        }

        private void DisplayPopulation(int colonyId, ColonyView? colonyView)
        {
            if(colonyView == null) return;

            ImGui.PushID("###Population " + colonyId);
            ImGui.Columns(1);
            if(ImGui.CollapsingHeader("Population", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2);

                foreach(var species in colonyView.SpeciesPopulations)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.Text(species.SpeciesName);
                    ImGui.PopStyleColor();
                    ImGui.NextColumn();
                    ImGui.Text(Stringify.Quantity(species.Population, "0.##", true));
                    ImGui.NextColumn();
                }

                ImGui.Columns(1);
            }
            ImGui.PopID();
        }

        private void DisplayMining(ColonyMiningView mining)
        {
            Vector2 topSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("NumberOfMines", new Vector2(topSize.X, 28f), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Text("Number of Mines:");
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("You can build more mines on this colony using the Production tab.");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                ImGui.Text(mining.NumberOfMines.ToString());
                ImGui.PopStyleColor();
            }
            ImGui.EndChild();

            if(ImGui.BeginTable("###MineralTable", 6, ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Mineral");
                ImGui.TableSetupColumn("Stockpile");
                ImGui.TableSetupColumn("Available to Mine");
                ImGui.TableSetupColumn("Accessibility");
                ImGui.TableSetupColumn("Annual Production");
                ImGui.TableSetupColumn("Years to Depletion");
                ImGui.TableHeadersRow();

                foreach(var mineral in mining.Minerals)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(mineral.Name);
                    if(ImGui.IsItemHovered())
                        DisplayHelpers.DescriptiveTooltip(mineral.Name, "Mineral", mineral.Description);
                    ImGui.TableNextColumn();
                    ImGui.Text(mineral.Stockpile?.ToString("#,###,###,###,###,###,##0") ?? "Unavailable");
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("Amount of " + mineral.Name + " available for use in the colony stockpile.");

                    ImGui.TableNextColumn();
                    ImGui.Text(mineral.AvailableToMine?.ToString("#,###,###,###,###,###,##0") ?? "N/A");
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("Amount of " + mineral.Name + " available that can be mined from this colony.");
                    ImGui.TableNextColumn();
                    ImGui.Text(mineral.Accessibility.ToString("0.00"));
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("How easy it is to mine " + mineral.Name + " from this colony.\n\n1.0 = easiest\n0.0 = hardest");
                    ImGui.TableNextColumn();
                    if(mineral.CanMine)
                    {
                        ImGui.Text(mineral.AnnualProduction.ToString("#,###,###"));
                        if(ImGui.IsItemHovered())
                            ImGui.SetTooltip("Annual production of " + mineral.Name + " from this colony.");
                    }
                    else
                    {
                        ImGui.Text("-");
                        if(ImGui.IsItemHovered())
                            ImGui.SetTooltip("This colony is currently unable to mine " + mineral.Name + ".");
                    }
                    ImGui.TableNextColumn();
                    if(mineral.AnnualProduction > 0)
                    {
                        var amount = mineral.AvailableToMine ?? 0;
                        string yearsToDepletion = System.Math.Round((double)amount / (double)mineral.AnnualProduction, 4).ToString("#.0");
                        ImGui.Text(yearsToDepletion);
                        if(ImGui.IsItemHovered())
                            ImGui.SetTooltip("The colony will exhaust the available " + mineral.Name + " in " + yearsToDepletion + " years.");
                    }
                    else
                    {
                        ImGui.Text("-");
                    }
                }

                ImGui.EndTable();

                if(mining.Minerals.Count == 0)
                {
                    ImGui.Text("No minerals available.");
                }
            }
        }

        private void DisplayNavalAcademy(int colonyId, NavalAcademyView academy)
        {
            Vector2 topSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("NumberOfAcademies" + colonyId, new Vector2(topSize.X, 28f), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Text("Academies:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                ImGui.Text(academy.Academies.Count.ToString("0"));
                ImGui.PopStyleColor();
                ImGui.EndChild();
            }

            Vector2 sizeAvailable = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("AcademyList", new Vector2(sizeAvailable.X * .25f, sizeAvailable.Y), ImGuiChildFlags.Borders))
            {
                if(ImGui.BeginTable("AcademyListTable", 4, Styles.TableFlags))
                {
                    ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.None, 0.1f);
                    ImGui.TableSetupColumn("Class Size", ImGuiTableColumnFlags.None, 0.25f);
                    ImGui.TableSetupColumn("Length", ImGuiTableColumnFlags.None, 0.2f);
                    ImGui.TableSetupColumn("Graduation", ImGuiTableColumnFlags.None, 0.3f);
                    ImGui.TableHeadersRow();

                    for(int i = 0; i < academy.Academies.Count; i++)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text((i + 1).ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(academy.Academies[i].ClassSize.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(academy.Academies[i].TrainingPeriodMonths.ToString() + " months");
                        ImGui.TableNextColumn();
                        ImGui.Text(academy.Academies[i].GraduationDate.ToShortDateString());
                    }
                    ImGui.EndTable();
                }
                ImGui.EndChild();
            }
        }

    }
}
