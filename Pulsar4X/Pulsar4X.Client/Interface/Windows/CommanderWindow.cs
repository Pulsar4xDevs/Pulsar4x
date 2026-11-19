using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client
{
    /// <summary>
    /// The corporation's personnel roster: every commander in the faction's service on the left,
    /// with a details pane for the selected person on the right. Reads the API galaxy model
    /// (Galaxy.Commanders, pushed on connect, clock advances, and after accepted commands).
    /// </summary>
    public class CommanderWindow : UniquePulsarGuiWindow<CommanderWindow>
    {
        // Selection is by commander id, re-resolved each frame: the roster is replaced wholesale
        // by server pushes.
        private int? _selectedId = null;

        private CommanderWindow()
        {
        }

        internal static CommanderWindow GetInstance()
        {
            if(_uiState.TryGetUniqueWindow<CommanderWindow>(out var window))
            {
                return window;
            }

            return _uiState.AddUniqueWindow(new CommanderWindow());
        }

        internal override void Display()
        {
            if(!IsActive) return;

            var galaxy = _uiState.GameClient?.Galaxy;

            ImGui.SetNextWindowSize(new Vector2(960, 500), ImGuiCond.FirstUseEver);
            using (var w = Window.Scope("Commanders", ref IsActive, _flags))
            {
                if (w.Visible)
                {
                    if(galaxy != null)
                    {
                        var people = galaxy.Commanders;

                        DisplayRecruitButton(galaxy);

                        // Keep the selection valid, defaulting to the first person so the window is
                        // immediately usable without an extra click.
                        CommanderSnapshot? selected = null;
                        if(people.Count > 0)
                        {
                            selected = people.FirstOrDefault(p => p.Id == _selectedId) ?? people[0];
                        }
                        _selectedId = selected?.Id;

                        Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                        var listSize = new Vector2(windowContentSize.X - Styles.LeftColumnWidthLg - 8, windowContentSize.Y);
                        var detailSize = new Vector2(Styles.LeftColumnWidthLg, windowContentSize.Y);

                        if(ImGui.BeginChild("PeopleList", listSize, ImGuiChildFlags.Borders))
                        {
                            DisplayHelpers.Header("Personnel", "Everyone in the corporation's service");
                            DisplayPeopleList(people, galaxy.Time.GameDateTime);
                        }
                        ImGui.EndChild();

                        ImGui.SameLine();
                        if(ImGui.BeginChild("PersonDetail", detailSize, ImGuiChildFlags.Borders))
                        {
                            if(selected != null)
                                DisplayPersonDetail(selected, galaxy.Time.GameDateTime);
                            else
                                ImGui.TextColored(Styles.DescriptiveColor, "No personnel in service.");
                        }
                        ImGui.EndChild();
                    }
            
                }
            }
        }

        // Hiring lives on each colony's Recruitment tab (Manage Colonies). This button only opens
        // that tab on an owned colony, preferring one that has candidates right now.
        private void DisplayRecruitButton(IClientGalaxy galaxy)
        {
            (int colonyId, string systemId)? target = null;
            bool hasCandidates = false;
            foreach (var summary in galaxy.KnownSystems)
            {
                var system = galaxy.GetSystem(summary.SystemId);
                if (system == null) continue;
                foreach (var entity in system.Entities)
                {
                    if (entity.Kind != BodyKind.Colony || entity.Relation != OwnerRelation.Owned) continue;
                    if (entity.GetView<RecruitmentView>() is not { } view) continue;
                    if (view.Candidates.Count > 0)
                    {
                        target = (entity.Id, summary.SystemId);
                        hasCandidates = true;
                        break;
                    }
                    target ??= (entity.Id, summary.SystemId);
                }
                if (hasCandidates) break;
            }

            bool enabled = target != null;
            if (!enabled) ImGui.BeginDisabled();
            if (ImGui.Button("Recruit") && target is { } t)
                ColonyManagementWindow.GetInstance().OpenRecruitment(t.colonyId, t.systemId);
            if (!enabled) ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(!enabled
                    ? "No owned colony offers recruitment."
                    : hasCandidates
                        ? "Open the colony Staffing tab to hire."
                        : "Open the colony Staffing tab. No candidates right now.");
        }

        private void DisplayPeopleList(IReadOnlyList<CommanderSnapshot> people, DateTime now)
        {
            if(people.Count == 0)
            {
                ImGui.TextColored(Styles.DescriptiveColor, "No personnel in service.");
                return;
            }

            if(ImGui.BeginTable("PeopleTable", 6, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 0.26f);
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.None, 0.12f);
                ImGui.TableSetupColumn("Rank", ImGuiTableColumnFlags.None, 0.16f);
                ImGui.TableSetupColumn("Assignment", ImGuiTableColumnFlags.None, 0.26f);
                // Fixed to fit the header (and the widest value), so "Yrs of Service" isn't cut to "Yrs of S...".
                float pad = 2 * ImGui.GetStyle().CellPadding.X + ImGui.GetStyle().ItemSpacing.X;
                float valueWidth = ImGui.CalcTextSize(YearsValueSample).X;
                ImGui.TableSetupColumn(ServiceHeader, ImGuiTableColumnFlags.WidthFixed,
                    FitColumnWidth(ImGui.CalcTextSize(ServiceHeader).X, valueWidth, pad));
                ImGui.TableSetupColumn(RankHeader, ImGuiTableColumnFlags.WidthFixed,
                    FitColumnWidth(ImGui.CalcTextSize(RankHeader).X, valueWidth, pad));
                ImGui.TableHeadersRow();

                foreach(var person in people)
                {
                    ImGui.TableNextColumn();
                    PortraitRenderer.For(_uiState).DrawPerson(person, PortraitRenderer.SmallSize);
                    ImGui.SameLine();
                    if(ImGui.Selectable($"{person.Name}###{person.Id}", _selectedId == person.Id,
                        ImGuiSelectableFlags.SpanAllColumns))
                    {
                        _selectedId = person.Id;
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text(person.Kind.ToString());

                    ImGui.TableNextColumn();
                    ImGui.Text(RankDisplay(person));

                    ImGui.TableNextColumn();
                    if(person.AssignmentName != null)
                        ImGui.Text(person.AssignmentName);
                    else
                        ImGui.TextColored(Styles.DescriptiveColor, "Unassigned");

                    ImGui.TableNextColumn();
                    ImGui.Text(YearsText(person.CommissionedOn, now));
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("Commissioned on: " + DateDisplay.FormatDate(person.CommissionedOn));

                    ImGui.TableNextColumn();
                    ImGui.Text(YearsText(person.RankedOn, now));
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("Promoted on: " + DateDisplay.FormatDate(person.RankedOn));
                }

                ImGui.EndTable();
            }
        }

        private void DisplayPersonDetail(CommanderSnapshot person, DateTime now)
        {
            DisplayHelpers.PersonCard(_uiState, person, now, PortraitRenderer.LargeSize);
        }

        private static string RankDisplay(CommanderSnapshot person)
            => person.RankName ?? $"Rank {person.Rank}";

        public const string ServiceHeader = "Yrs of Service";
        public const string RankHeader = "Yrs in Rank";
        /// <summary>The widest years value a cell should show ("100.0").</summary>
        public const string YearsValueSample = "100.0";

        /// <summary>Width of a fixed column that shows both its header and its widest value in full.</summary>
        public static float FitColumnWidth(float headerWidth, float valueWidth, float padding)
            => Math.Max(headerWidth, valueWidth) + Math.Max(0f, padding);

        /// <summary>Elapsed years from <paramref name="from"/> to <paramref name="to"/> (365.25-day years), never negative.</summary>
        public static double YearsBetween(DateTime from, DateTime to)
            => Math.Max(0, (to - from).TotalDays / 365.25);

        /// <summary>
        /// "Yrs of Service" / "Yrs in Rank" cell: elapsed years to one decimal ("10.0", "0.0" for today).
        /// An unrecorded date (DateTime.MinValue, from older saves) shows "-" rather than a calendar-sized number.
        /// </summary>
        public static string YearsText(DateTime from, DateTime now)
            => from == default ? "-" : YearsBetween(from, now).ToString("0.0", CultureInfo.InvariantCulture);
    }
}
