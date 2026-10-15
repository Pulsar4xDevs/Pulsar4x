using System;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Other factions, this corporation's status and grants toward each of them, and their
    /// status and grants shown read-only. Reads Galaxy.Stances. A change submits
    /// <see cref="SetFactionStanceCommand"/> or <see cref="SetFactionGrantCommand"/>;
    /// the row updates when the server pushes <see cref="GameEventType.StancesChanged"/>.
    /// </summary>
    public class FactionStanceWindow : UniquePulsarGuiWindow<FactionStanceWindow>
    {
        static readonly string[] StanceLabels = Enum.GetNames<FactionStance>();
        static readonly (FactionGrants Flag, string Label)[] GrantLabels =
        {
            (FactionGrants.Trade, "Trade"),
            (FactionGrants.Positions, "Ships"),
            (FactionGrants.Geo, "Geo"),
            (FactionGrants.Grav, "Grav"),
            (FactionGrants.Research, "Research"),
        };

        private FactionStanceWindow()
        {
            _flags = ImGuiWindowFlags.AlwaysAutoResize;
        }

        internal static FactionStanceWindow GetInstance()
        {
            if (_uiState.TryGetUniqueWindow<FactionStanceWindow>(out var window))
                return window;

            return _uiState.AddUniqueWindow(new FactionStanceWindow());
        }

        internal override void Display()
        {
            if (!IsActive) return;

            var galaxy = _uiState.GameClient?.Galaxy;
            if (Window.Begin("Factions", ref IsActive, _flags))
            {
                if (galaxy == null || galaxy.Stances.Count == 0)
                {
                    ImGui.TextColored(Styles.DescriptiveColor, "No other factions.");
                }
                else if (ImGui.BeginTable("FactionStances", 5,
                    ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                {
                    // Stretch columns collapse under AlwaysAutoResize, which clipped the combo to "Frien".
                    // Fixed widths follow the text, so the window grows to the table.
                    var style = ImGui.GetStyle();
                    float nameWidth = ImGui.CalcTextSize("Faction").X;
                    foreach (var row in galaxy.Stances)
                        nameWidth = Math.Max(nameWidth, ImGui.CalcTextSize(row.Name).X);
                    nameWidth += style.CellPadding.X * 2f;

                    float comboWidth = Math.Max(ImGui.CalcTextSize("Status").X, ImGui.CalcTextSize("Their status").X);
                    foreach (var label in StanceLabels)
                        comboWidth = Math.Max(comboWidth, ImGui.CalcTextSize(label).X);
                    comboWidth += style.FramePadding.X * 2f + ImGui.GetFrameHeight() + style.CellPadding.X * 2f;

                    float grantsWidth = Math.Max(ImGui.CalcTextSize("Their grants").X, ImGui.CalcTextSize("Grants").X);
                    grantsWidth += style.CellPadding.X * 2f;
                    foreach (var grant in GrantLabels)
                        grantsWidth += ImGui.GetFrameHeight() + style.FramePadding.X * 2f + ImGui.CalcTextSize(grant.Label).X + style.ItemSpacing.X;

                    ImGui.TableSetupColumn("Faction", ImGuiTableColumnFlags.WidthFixed, nameWidth);
                    ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, comboWidth);
                    ImGui.TableSetupColumn("Grants", ImGuiTableColumnFlags.WidthFixed, grantsWidth);
                    ImGui.TableSetupColumn("Their status", ImGuiTableColumnFlags.WidthFixed, comboWidth);
                    ImGui.TableSetupColumn("Their grants", ImGuiTableColumnFlags.WidthFixed, grantsWidth);
                    ImGui.TableHeadersRow();

                    foreach (var row in galaxy.Stances)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text(row.Name);

                        ImGui.TableNextColumn();
                        int current = (int)row.Stance;
                        ImGui.SetNextItemWidth(-1);
                        if (ImGui.Combo($"##stance-{row.FactionId}", ref current, StanceLabels, StanceLabels.Length)
                            && current != (int)row.Stance
                            && Enum.IsDefined(typeof(FactionStance), current))
                        {
                            _uiState.GameClient?.SubmitCommandAsync(
                                new SetFactionStanceCommand(_uiState.FactionId, row.FactionId, (FactionStance)current));
                        }

                        ImGui.TableNextColumn();
                        DrawGrants(row.FactionId, row.GrantedByViewer, editable: true, "mine");

                        ImGui.TableNextColumn();
                        ImGui.Text(row.TheirStance.ToString());

                        ImGui.TableNextColumn();
                        DrawGrants(row.FactionId, row.GrantedToViewer, editable: false, "theirs");
                    }

                    ImGui.EndTable();
                }
            }
            Window.End();
        }

        void DrawGrants(int factionId, FactionGrants grants, bool editable, string prefix)
        {
            for (int i = 0; i < GrantLabels.Length; i++)
            {
                if (i > 0)
                    ImGui.SameLine();
                var (flag, label) = GrantLabels[i];
                bool on = grants.HasFlag(flag);
                if (!editable)
                    ImGui.BeginDisabled();
                if (ImGui.Checkbox($"{label}##{prefix}-{flag}-{factionId}", ref on)
                    && editable
                    && on != grants.HasFlag(flag))
                {
                    _uiState.GameClient?.SubmitCommandAsync(
                        new SetFactionGrantCommand(_uiState.FactionId, factionId, flag, on));
                }
                if (!editable)
                    ImGui.EndDisabled();
            }
        }
    }
}
