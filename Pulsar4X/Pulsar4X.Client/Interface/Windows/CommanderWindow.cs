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
    public class CommanderWindow : PulsarGuiWindow
    {
        // Selection is by commander id, re-resolved each frame: the roster is replaced wholesale
        // by server pushes.
        private int? _selectedId = null;

        private CommanderWindow()
        {
        }

        internal static CommanderWindow GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(CommanderWindow)))
            {
                return new CommanderWindow();
            }
            return (CommanderWindow)_uiState.LoadedWindows[typeof(CommanderWindow)];
        }

        internal override void Display()
        {
            if(!IsActive) return;

            var galaxy = _uiState.GameClient?.Galaxy;

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
            // Portrait and name header with background (same style as the people chooser).
            float portraitSize = 32f;
            float headerPadding = 4f;
            float headerHeight = portraitSize + headerPadding * 2;

            var drawList = ImGui.GetWindowDrawList();
            Vector2 headerMin = ImGui.GetCursorScreenPos();
            Vector2 headerMax = new Vector2(
                headerMin.X + ImGui.GetContentRegionAvail().X,
                headerMin.Y + headerHeight);
            uint headerBgColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.2f, 0.25f, 1.0f));
            drawList.AddRectFilled(headerMin, headerMax, headerBgColor, 4.0f);

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + headerPadding);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + headerPadding);

            IntPtr portraitTexture = _uiState.Img_Character();
            if(portraitTexture != IntPtr.Zero)
            {
                ImGui.Image(portraitTexture.ToTextureRef(), new Vector2(portraitSize, portraitSize));
                ImGui.SameLine();
            }

            float textHeight = ImGui.GetTextLineHeight();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (portraitSize - textHeight) / 2);
            ImGui.Text(person.Name);

            ImGui.SetCursorPosY(headerMin.Y - ImGui.GetWindowPos().Y + headerHeight + headerPadding);

            ImGui.Columns(2, "PersonDetailsColumns", false);
            ImGui.SetColumnWidth(0, 110);

            DisplayHelpers.PrintFormattedCell("Type:");
            DisplayHelpers.PrintCell(person.Kind.ToString());

            DisplayHelpers.PrintFormattedCell("Rank:");
            DisplayHelpers.PrintCell(RankDisplay(person));

            DisplayHelpers.PrintFormattedCell("Commissioned:");
            DisplayHelpers.PrintCell(person.CommissionedOn.ToShortDateString(),
                $"{YearsBetween(person.CommissionedOn, now):F1} years of service");

            DisplayHelpers.PrintFormattedCell("Promoted:");
            DisplayHelpers.PrintCell(person.RankedOn.ToShortDateString(),
                $"{YearsBetween(person.RankedOn, now):F1} years in rank");

            DisplayHelpers.PrintFormattedCell("Experience:");
            DisplayHelpers.PrintCell($"{person.Experience} / {person.ExperienceCap}");

            DisplayHelpers.PrintFormattedCell("Status:");
            ImGui.PushStyleColor(ImGuiCol.Text, person.IsAssigned ? Styles.OkColor : Styles.GoodColor);
            DisplayHelpers.PrintCell(person.IsAssigned ? "Assigned" : "Available");
            ImGui.PopStyleColor();

            if(person.AssignmentName != null)
            {
                DisplayHelpers.PrintFormattedCell("Assignment:");
                DisplayHelpers.PrintCell(person.AssignmentName);
            }

            ImGui.Columns(1);

            if(person.Bonuses.Count > 0)
            {
                ImGui.NewLine();
                DisplayHelpers.Header("Bonuses");

                foreach(var bonus in person.Bonuses)
                {
                    string valueStr = bonus.IsPercentage
                        ? $"{bonus.Value * 100:+0.#;-0.#}%"
                        : $"{bonus.Value:+0.#;-0.#}";

                    string bonusText = bonus.Name;
                    if(!string.IsNullOrEmpty(bonus.FilterName))
                    {
                        bonusText += $" ({bonus.FilterName})";
                    }

                    ImGui.PushStyleColor(ImGuiCol.Text, bonus.Value >= 0 ? Styles.GoodColor : Styles.BadColor);
                    ImGui.TextUnformatted(valueStr);
                    ImGui.PopStyleColor();
                    ImGui.SameLine();
                    ImGui.Text(bonusText);
                }
            }
        }

        private static string RankDisplay(CommanderSnapshot person)
            => person.RankName ?? $"Rank {person.Rank}";

        private static double YearsBetween(DateTime from, DateTime to)
            => (to - from).TotalDays / 365.25;
    }
}
