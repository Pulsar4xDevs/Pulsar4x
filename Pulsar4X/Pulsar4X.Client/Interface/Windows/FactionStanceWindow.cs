using System;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Other factions and the stance this corporation has stored toward each of them.
    /// Reads Galaxy.Stances. A combo change submits <see cref="SetFactionStanceCommand"/>;
    /// the row updates when the server pushes <see cref="GameEventType.StancesChanged"/>.
    /// </summary>
    public class FactionStanceWindow : UniquePulsarGuiWindow<FactionStanceWindow>
    {
        static readonly string[] StanceLabels = Enum.GetNames<FactionStance>();

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
                else if (ImGui.BeginTable("FactionStances", 2,
                    ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                {
                    // Stretch columns collapse under AlwaysAutoResize, which clipped the combo to "Frien".
                    // Fixed widths follow the text, so the window grows to the table.
                    var style = ImGui.GetStyle();
                    float nameWidth = ImGui.CalcTextSize("Faction").X;
                    foreach (var row in galaxy.Stances)
                        nameWidth = Math.Max(nameWidth, ImGui.CalcTextSize(row.Name).X);
                    nameWidth += style.CellPadding.X * 2f;

                    float comboWidth = ImGui.CalcTextSize("Stance").X;
                    foreach (var label in StanceLabels)
                        comboWidth = Math.Max(comboWidth, ImGui.CalcTextSize(label).X);
                    comboWidth += style.FramePadding.X * 2f + ImGui.GetFrameHeight() + style.CellPadding.X * 2f;

                    ImGui.TableSetupColumn("Faction", ImGuiTableColumnFlags.WidthFixed, nameWidth);
                    ImGui.TableSetupColumn("Stance", ImGuiTableColumnFlags.WidthFixed, comboWidth);
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
                    }

                    ImGui.EndTable();
                }
            }
            Window.End();
        }
    }
}
