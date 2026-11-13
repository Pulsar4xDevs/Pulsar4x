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
    /// The ship window's Power tab, laid out like the colony Power tab: a summary with the supply ring
    /// beside a short battery history, then the sources, then the loads by kind with a supply bar each.
    /// Reads only Api views (ShipPowerView, EnergyView); both are owner-only.
    /// </summary>
    public static class ShipPowerDisplay
    {
        static readonly Vector4 StoredColour = new(0.35f, 0.75f, 1f, 1f);
        static readonly Vector4 FlowColour = new(1f, 0.6f, 0.2f, 1f);
        static readonly Vector4 WarnColour = new(1f, 0.6f, 0.2f, 1f);

        public static void Display(ShipPowerView power, EnergyView? energy, Vector4 accentColour)
        {
            ImGui.TextWrapped("Solar and RTGs carry the load first, then the reactor, then fuel cells. Warp and charging are served first; sensors and other loads run on spare output once the batteries are full.");
            ImGui.Separator();

            if (ImGui.BeginTable("ShipPowerTop", 2, ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Summary", ImGuiTableColumnFlags.WidthFixed, 280f);
                ImGui.TableSetupColumn("History", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                Summary(power, accentColour);
                ImGui.TableNextColumn();
                History(energy, new Vector2(Math.Max(200f, ImGui.GetContentRegionAvail().X), 150f));
                ImGui.EndTable();
            }

            ImGui.SeparatorText("Sources (in dispatch order)");
            Sources(power);

            ImGui.SeparatorText("Loads by kind");
            Loads(power);
        }

        static void Summary(ShipPowerView power, Vector4 accentColour)
        {
            const float radius = 26f, thickness = 5f;
            var cursor = ImGui.GetCursorScreenPos();
            var centre = new Vector2(cursor.X + radius + thickness, cursor.Y + radius + thickness);
            string tooltip = power.TotalDemand_kW > 0
                ? $"Delivered {Stringify.Power(power.Delivered_kW)} of {Stringify.Power(power.TotalDemand_kW)} demand, {PowerRing.WordsFor(power.SupplyShare)}"
                : power.HasPlant ? "Nothing draws power" : "No power plant. Nothing draws power";
            PowerRing.DrawAt(ImGui.GetWindowDrawList(), centre, radius, thickness, power.SupplyShare, accentColour, tooltip);
            ImGui.InvisibleButton("##shipPowerRing", new Vector2((radius + thickness) * 2f, (radius + thickness) * 2f + ImGui.GetTextLineHeight() + 2f));
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.PushStyleColor(ImGuiCol.Text, PowerRing.ColourFor(power.SupplyShare));
            ImGui.TextUnformatted(Capitalise(PowerRing.WordsFor(power.SupplyShare)));
            ImGui.PopStyleColor();
            if (!power.HasPlant)
                ImGui.TextDisabled("No power plant");
            ImGui.EndGroup();

            ImGui.Columns(2, "ShipPowerSummaryCols", false);
            DisplayHelpers.PrintRow("Generation", Stringify.Power(power.Generation_kW), tooltipTwo: "What the sources can give now (an RTG after decay, a reactor or fuel cell only with fuel)");
            DisplayHelpers.PrintRow("Demand", Stringify.Power(power.TotalDemand_kW),
                tooltipTwo: $"Warp {Stringify.Power(power.Demand_kW)}, other loads {Stringify.Power(power.AuxDemand_kW)}");
            DisplayHelpers.PrintRow("Delivered", Stringify.Power(power.Delivered_kW), tooltipTwo: "Warp up to generation, plus the other loads served from spare output");
            // PrintRow draws with ImGui.Text, which reads printf formats: escape the percent signs.
            DisplayHelpers.PrintRow("Share", $"{power.SupplyShare * 100:0}%%");
            if (power.StoreMax_kJ > 0)
            {
                double fraction = Math.Clamp(power.Stored_kJ / power.StoreMax_kJ, 0, 1);
                DisplayHelpers.PrintRow("Batteries", $"{fraction * 100:0.#}%%  {Stringify.Energy(Math.Max(0, power.Stored_kJ))} / {Stringify.Energy(power.StoreMax_kJ)}", separator: false);
            }
            ImGui.Columns(1);
            if (power.Demand_kW > power.Generation_kW && power.Stored_kJ > 0)
                ImGui.TextWrapped("Generation is below warp demand. The batteries cover the gap while they last.");
            if (power.AuxDemand_kW > 0 && power.AuxServed_kW < power.AuxDemand_kW && power.StoreMax_kJ > 0 && power.Stored_kJ < power.StoreMax_kJ)
                ImGui.TextWrapped("Sensors and other loads wait until the batteries are full.");
        }

        static void Sources(ShipPowerView power)
        {
            if (power.Sources.Count == 0)
            {
                ImGui.TextDisabled("No power sources on this ship.");
                return;
            }
            if (!ImGui.BeginTable("ShipPowerSources", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
                return;
            ImGui.TableSetupColumn("Source");
            ImGui.TableSetupColumn("Kind");
            ImGui.TableSetupColumn("Count");
            ImGui.TableSetupColumn("Output now / installed");
            ImGui.TableSetupColumn("Fuel or decay");
            ImGui.TableHeadersRow();
            foreach (var s in power.Sources)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(s.Name);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(s.Kind == "Reactor" ? "Reactor / turbine" : s.Kind);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(s.Count.ToString());
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{Stringify.Power(s.Output_kW)} / {Stringify.Power(s.Nameplate_kW)}");
                if (s.Note != null)
                {
                    ImGui.SameLine();
                    ImGui.TextColored(WarnColour, "(!)");
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(s.Note);
                }
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(FuelText(s));
                if (s.FuelBurnAtFull_kgPerS is { } burn && ImGui.IsItemHovered())
                    ImGui.SetTooltip($"Burns {Stringify.Mass(burn * 86400.0)}/day at full output");
            }
            ImGui.EndTable();
        }

        static string FuelText(ShipPowerSourceRow s)
        {
            if (s.DecayRemaining is { } left)
                return $"{left * 100:0.#}% of new" + (s.HalfLife_years is { } hl ? $", half-life {hl:0.#} years" : "");
            if (s.FuelRemaining_kg is { } kg)
            {
                string text = $"{s.FuelName ?? "Fuel"}: {Stringify.Mass(kg)} left";
                if (s.RunwayAtFull_s is { } runway)
                    text += $", {Duration(runway)} at full output";
                return text;
            }
            return s.Kind == "Solar" ? "Sunlight" : "";
        }

        static void Loads(ShipPowerView power)
        {
            if (power.LoadGroups.Count == 0)
            {
                ImGui.TextDisabled("Nothing on this ship draws power.");
                return;
            }
            foreach (var g in power.LoadGroups)
            {
                double share = g.Satisfaction ?? (g.State == "Partial" ? 0.5 : g.State == "No power" ? 0 : 1);
                Vector4 colour = g.State switch
                {
                    "Powered" => PowerRing.FullColour,
                    "Partial" => PowerRing.PartialColour,
                    "No power" => PowerRing.NoneColour,
                    _ => Styles.DescriptiveColor,
                };
                ImGui.PushStyleColor(ImGuiCol.PlotHistogram, colour);
                ImGui.ProgressBar(g.State == "Idle" ? 0f : (float)share, new Vector2(80f, 0f), "");
                ImGui.PopStyleColor();
                ImGui.SameLine();
                string demand = g.Kind == "Miners" ? "per dig" : Stringify.Power(g.Demand_kW);
                ImGui.TextUnformatted($"{g.Kind} · {demand} · ");
                ImGui.SameLine(0f, 0f);
                ImGui.PushStyleColor(ImGuiCol.Text, colour);
                ImGui.TextUnformatted(Words(g));
                ImGui.PopStyleColor();
                if (g.Note != null)
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled("[?]");
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(g.Note);
                }

                if (g.Loads.Count == 0)
                    continue;
                if (ImGui.BeginTable("ShipLoads" + g.Kind, 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
                {
                    ImGui.TableSetupColumn("Component");
                    ImGui.TableSetupColumn("Count");
                    ImGui.TableSetupColumn("Draw");
                    ImGui.TableHeadersRow();
                    foreach (var l in g.Loads)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(l.Name);
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(l.Count.ToString());
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(l.Draw_kW > 0 ? Stringify.Power(l.Draw_kW) : (l.Note ?? "-"));
                        if (l.Draw_kW > 0 && l.Note != null && ImGui.IsItemHovered())
                            ImGui.SetTooltip(l.Note);
                    }
                    ImGui.EndTable();
                }
            }
        }

        /// <summary>The colony tiers' words; a miner that only reports "short" says so without a number.</summary>
        static string Words(ShipPowerLoadGroup g) => g.State switch
        {
            "Idle" => "idle",
            "Partial" when g.Satisfaction is null => "running short",
            _ => PowerRing.WordsFor(g.Satisfaction ?? (g.State == "No power" ? 0 : 1)),
        };

        /// <summary>
        /// The plant's own history (EnergyGenAbilityDB.Histogram via EnergyView): stored energy per
        /// energy step, oldest first. Hover shows the step's net flow into the batteries.
        /// </summary>
        static void History(EnergyView? energy, Vector2 size)
        {
            ImGui.TextColored(StoredColour, "- Batteries");
            ImGui.SameLine();
            ImGui.TextDisabled("(last energy steps; hover for the net flow)");
            var history = energy?.Histogram ?? Array.Empty<EnergyHistogramPoint>();
            if (energy == null || energy.StoreMax <= 0 || history.Count < 2)
            {
                ImGui.TextDisabled(energy == null ? "No power plant, so no history." : "No battery history yet.");
                return;
            }

            double max = Math.Max(energy.StoreMax, history.Max(h => h.Stored));
            if (max <= 0) max = 1;
            float axisLeft = ImGui.CalcTextSize(Stringify.Energy(max)).X + 8f;
            const float axisBottom = 18f;
            var draw = ImGui.GetWindowDrawList();
            var origin = ImGui.GetCursorScreenPos();
            ImGui.InvisibleButton("ShipPowerHistoryPlot", size);
            bool hovered = ImGui.IsItemHovered();
            float x0 = origin.X + axisLeft, x1 = origin.X + size.X - 4f;
            float y0 = origin.Y + 4f, y1 = origin.Y + size.Y - axisBottom;
            int n = history.Count;
            float X(int i) => x0 + (x1 - x0) * i / (n - 1);
            float Y(double kJ) => y1 - (float)(Math.Clamp(kJ, 0, max) / max) * (y1 - y0);

            uint axis = ImGui.GetColorU32(ImGuiCol.TextDisabled);
            uint grid = ImGui.GetColorU32(ImGuiCol.Border);
            draw.AddRect(new Vector2(x0, y0), new Vector2(x1, y1), grid);
            foreach (float f in new[] { 0f, 0.5f, 1f })
            {
                float y = Y(max * f);
                draw.AddLine(new Vector2(x0, y), new Vector2(x1, y), grid);
                string label = Stringify.Energy(max * f);
                var ls = ImGui.CalcTextSize(label);
                draw.AddText(new Vector2(x0 - ls.X - 4f, y - ls.Y / 2), axis, label);
            }
            string steps = n + " steps";
            draw.AddText(new Vector2((x0 + x1 - ImGui.CalcTextSize(steps).X) / 2, y1 + 2f), axis, steps);
            uint stored = ImGui.GetColorU32(StoredColour);
            for (int i = 0; i < n - 1; i++)
                draw.AddLine(new Vector2(X(i), Y(history[i].Stored)), new Vector2(X(i + 1), Y(history[i + 1].Stored)), stored, 1.5f);

            if (hovered)
            {
                int i = Math.Clamp((int)Math.Round((ImGui.GetIO().MousePos.X - x0) / (x1 - x0) * (n - 1)), 0, n - 1);
                draw.AddLine(new Vector2(X(i), y0), new Vector2(X(i), y1), ImGui.GetColorU32(FlowColour));
                var h = history[i];
                string flow = h.Output > 0.001 ? "+" + Stringify.Power(h.Output) : h.Output < -0.001 ? "-" + Stringify.Power(-h.Output) : "0 kW";
                ImGui.SetTooltip($"Stored: {Stringify.Energy(Math.Max(0, h.Stored))}\nNet flow: {flow}");
            }
        }

        static string Duration(double seconds)
        {
            if (!double.IsFinite(seconds)) return "-";
            double days = seconds / 86400.0;
            if (days >= 730) return $"{days / 365.25:N1} years";
            if (days >= 2) return $"{days:N0} days";
            double hours = seconds / 3600.0;
            return hours >= 1 ? $"{hours:N1} hours" : $"{seconds / 60.0:N0} min";
        }

        static string Capitalise(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
