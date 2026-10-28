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

        internal override void Display()
        {
            if(!IsActive) return;

            var galaxy = _uiState.GameClient?.Galaxy;
            if(galaxy == null) return;

            if(Window.Begin("Manage Colonies", ref IsActive))
            {
                // A missing selection, or one the latest push no longer has, opens on the first owned colony.
                // SelectColony already stored an id, so a colony that resolves is left alone.
                var selectedSystem = _selectedSystemId == null ? null : galaxy.GetSystem(_selectedSystemId);
                var selectedColony = SelectedColonyId is { } openId ? selectedSystem?.GetEntity(openId) : null;
                if (selectedColony == null)
                {
                    foreach (var summary in galaxy.KnownSystems)
                    {
                        var system = galaxy.GetSystem(summary.SystemId);
                        var first = system?.Entities.FirstOrDefault(e => e.Kind == BodyKind.Colony && e.Relation == OwnerRelation.Owned);
                        if (first == null) continue;
                        SelectColony(first.Id, summary.SystemId);
                        break;
                    }
                }

                Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                if(ImGui.BeginChild("Colonies", new Vector2(Styles.LeftColumnWidthLg, windowContentSize.Y), ImGuiChildFlags.Borders))
                {
                    DisplayHelpers.Header("Colonies", "Select a colony to manage it.");
                    foreach(var summary in galaxy.KnownSystems)
                    {
                        var system = galaxy.GetSystem(summary.SystemId);
                        if(system == null) continue;

                        bool anyOwned = false;
                        foreach (var entity in system.Entities)
                        {
                            if (entity.Kind == BodyKind.Colony && entity.Relation == OwnerRelation.Owned)
                            {
                                anyOwned = true;
                                break;
                            }
                        }
                        if (!anyOwned) continue;

                        if(!isExpanded.ContainsKey(summary.SystemId)) isExpanded.Add(summary.SystemId, true);
                        ImGui.SetNextItemOpen(isExpanded[summary.SystemId], ImGuiCond.Appearing);
                        if(ImGui.TreeNode(summary.Name + "###" + summary.SystemId))
                        {
                            foreach(var colony in system.Entities.Where(e => e.Kind == BodyKind.Colony && e.Relation == OwnerRelation.Owned))
                            {
                                var population = colony.GetView<ColonyView>()?.Population ?? 0;
                                var name = colony.GetView<NameView>()?.Name ?? "Unknown";
                                var flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
                                if (SelectedColonyId == colony.Id)
                                    flags |= ImGuiTreeNodeFlags.Selected;

                                ImGui.TreeNodeEx(name + " (" + Stringify.Quantity(population) + ")###colony-" + colony.Id, flags);
                                if (ImGui.IsItemClicked())
                                    SelectColony(colony.Id, summary.SystemId);
                            }
                            ImGui.TreePop();
                        }
                    }
                }
                ImGui.EndChild();

                // Re-resolve after the list so a click this frame opens that colony.
                selectedSystem = _selectedSystemId == null ? null : galaxy.GetSystem(_selectedSystemId);
                selectedColony = SelectedColonyId is { } id ? selectedSystem?.GetEntity(id) : null;

                if (selectedColony != null && selectedSystem != null)
                {
                    ImGui.SameLine();
                    DisplaySelectedColony(selectedSystem, selectedColony);
                }
            }
            Window.End();
        }

        private void DisplaySelectedColony(IClientSystem selectedSystem, EntitySnapshot selectedColony)
        {
            if (ImGui.BeginChild("ColoniesTabs"))
            {
                ImGui.BeginTabBar("EconomicsTabBar", ImGuiTabBarFlags.None);

                if (ImGui.BeginTabItem("Summary"))
                {
                    DisplaySummary(selectedColony, selectedSystem);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Production"))
                {
                    _productionDisplay ??= new ColonyProductionDisplay();
                    _productionDisplay.Display(selectedColony.Id, selectedColony.GetView<IndustryView>(), _uiState);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Construction"))
                {
                    _constructionDisplay ??= new ColonyConstructionDisplay();
                    _constructionDisplay.Display(selectedColony.Id, selectedColony.GetView<ConstructionView>(), _uiState);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Market"))
                {
                    MarketBarsDisplay.Display(selectedColony, selectedColony.GetView<MarketView>(), _uiState);
                    IntelDisplay.Display(selectedColony, selectedColony.GetView<IntelBookView>(), selectedSystem.SystemId, _uiState);
                    ImGui.EndTabItem();
                }
                if (selectedColony.GetView<ColonyMiningView>() is { } mining && ImGui.BeginTabItem("Mining"))
                {
                    mining.Display();
                    ImGui.EndTabItem();
                }
                if (selectedColony.GetView<NavalAcademyView>() is { } academy && ImGui.BeginTabItem("Naval Academy"))
                {
                    DisplayNavalAcademy(selectedColony.Id, academy);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Log"))
                {
                    ShipLogDisplay.Draw(selectedColony, ref _logShownId, ref _logShownCount);
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
                colonyView?.Display(colony.Id);
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
                        if(ImGui.Button("Initiate Transfer", new Vector2(size.X - 8, 18)) && _selectedSystemId != null)
                        {
                            CreateTransferWindow.GetInstance().SetLeft(colony.Id, _selectedSystemId);
                            CreateTransferWindow.GetInstance().SetActive(true);
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

        private void DisplayNavalAcademy(int colonyId, NavalAcademyView academy)
        {
            ImGui.Text("The colony has " + Stringify.Quantity(academy.Population) + " people.");

            var enrolled = new HashSet<int>();
            foreach (var row in academy.Academies)
            {
                if (row.Course == AcademyCourseKind.Command && row.OfficerId >= 0)
                    enrolled.Add(row.OfficerId);
            }

            var galaxy = _uiState.GameClient?.Galaxy;
            for (int i = 0; i < academy.Academies.Count; i++)
            {
                var row = academy.Academies[i];
                ImGui.Separator();
                ImGui.Text("Academy " + (i + 1));
                ImGui.SameLine();
                ImGui.TextColored(Styles.DescriptiveColor,
                    "Class of " + row.ClassSize + ", " + row.TrainingPeriodMonths + " months. Mean cap " + row.MeanCap + ".");

                if (row.Course == AcademyCourseKind.Idle)
                    DisplayIdleAcademy(colonyId, academy, row, galaxy, enrolled);
                else
                    DisplayRunningCourse(colonyId, row, galaxy);
            }
        }

        private void DisplayIdleAcademy(int colonyId, NavalAcademyView academy, NavalAcademyClassView row, IClientGalaxy? galaxy, HashSet<int> enrolled)
        {
            long need = (long)row.ClassSize * academy.PeoplePerEnsign;
            string train = "Train a class of " + row.ClassSize + ". Needs " + Stringify.Quantity(need)
                + " people. The colony has " + Stringify.Quantity(academy.Population) + ".";
            bool shortOfPeople = academy.Population < need;
            if (shortOfPeople)
                ImGui.BeginDisabled();
            if (ImGui.Button(train + "###train-" + row.ComponentId))
                _uiState.GameClient?.SubmitCommandAsync(new TrainAcademyClassCommand(colonyId, row.ComponentId));
            if (shortOfPeople)
            {
                ImGui.EndDisabled();
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    ImGui.SetTooltip("The colony has " + academy.Population + " people. This class needs " + need + ".");
            }

            if (galaxy == null)
                return;

            bool anyNavy = false;
            foreach (var officer in galaxy.Commanders)
            {
                if (officer.Kind != CommanderKind.Navy)
                    continue;
                anyNavy = true;
                string? refusal = CommandCourseRefusal(officer, row.MeanCap, enrolled);
                string label = OfficerLine(officer) + ". " + (refusal ?? CommandCourseLabel(officer, row.MeanCap));
                if (refusal != null)
                    ImGui.BeginDisabled();
                if (ImGui.Button(label + "###course-" + row.ComponentId + "-" + officer.Id))
                    _uiState.GameClient?.SubmitCommandAsync(new AcademyCommandCourseCommand(colonyId, row.ComponentId, officer.Id));
                if (refusal != null)
                {
                    ImGui.EndDisabled();
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                        ImGui.SetTooltip(refusal);
                }
            }
            if (!anyNavy)
                ImGui.TextColored(Styles.DescriptiveColor, "No navy officers.");
        }

        private void DisplayRunningCourse(int colonyId, NavalAcademyClassView row, IClientGalaxy? galaxy)
        {
            string date = row.Graduation?.ToShortDateString() ?? "";
            if (row.Course == AcademyCourseKind.Class)
            {
                ImGui.Text("Training a class of " + row.ClassSize + ". Finishes " + date + ".");
            }
            else
            {
                string who = "An officer";
                var officer = galaxy?.Commanders.FirstOrDefault(person => person.Id == row.OfficerId);
                if (officer != null)
                    who = OfficerLine(officer);
                ImGui.Text("Command course for " + who + ". Finishes " + date + ".");
            }
            if (ImGui.Button("Cancel###cancel-" + row.ComponentId))
                _uiState.GameClient?.SubmitCommandAsync(new CancelAcademyCourseCommand(colonyId, row.ComponentId));
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
