using System;
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

            ImGui.SetNextWindowSize(new Vector2(900, 600), ImGuiCond.FirstUseEver);
            if(Window.Begin("Commanders", ref IsActive, _flags))
            {
                if(galaxy != null)
                {
                    var people = galaxy.Commanders;

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
            Window.End();
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
                ImGui.TableSetupColumn("Yrs of Service", ImGuiTableColumnFlags.None, 0.1f);
                ImGui.TableSetupColumn("Yrs in Rank", ImGuiTableColumnFlags.None, 0.1f);
                ImGui.TableHeadersRow();

                foreach(var person in people)
                {
                    ImGui.TableNextColumn();
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
                    ImGui.Text(YearsBetween(person.CommissionedOn, now).ToString("F0"));
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("Commissioned on: " + person.CommissionedOn.ToShortDateString());

                    ImGui.TableNextColumn();
                    ImGui.Text(YearsBetween(person.RankedOn, now).ToString("F0"));
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("Promoted on: " + person.RankedOn.ToShortDateString());
                }

                ImGui.EndTable();
            }
        }

        private void DisplayPersonDetail(CommanderSnapshot person, DateTime now)
        {
            DisplayHelpers.PersonCard(_uiState, person, now);
        }

        private static string RankDisplay(CommanderSnapshot person)
            => person.RankName ?? $"Rank {person.Rank}";

        private static double YearsBetween(DateTime from, DateTime to)
            => (to - from).TotalDays / 365.25;
    }
}
