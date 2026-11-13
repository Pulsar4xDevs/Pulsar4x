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
    /// The ship window's Power tab, laid out like the colony Power tab: a summary beside a short battery
    /// history, then a stacked generation bar with one compact row per source, then the Storage
    /// section's two-way charge bar, then a stacked demand bar with one compact row per load group
    /// (PowerBars). The supply ring lives on the ship Summary
    /// tab and the entity card. Reads only Api views (ShipPowerView, EnergyView); both are owner-only.
    /// </summary>
    public static class ShipPowerDisplay
    {
        static readonly Vector4 StoredColour = new(0.35f, 0.75f, 1f, 1f);
        static readonly Vector4 FlowColour = new(1f, 0.6f, 0.2f, 1f);
        static readonly Vector4 WarnColour = new(1f, 0.6f, 0.2f, 1f);

        const string HowItWorks = "Solar and RTGs carry the load first, then the reactor, then fuel cells. Warp and charging are served first; sensors and other loads run on spare output once the batteries are full.";

        public static void Display(ShipPowerView power, EnergyView? energy, Vector4 accentColour)
        {
            if (ImGui.BeginTable("ShipPowerTop", 2, ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Summary", ImGuiTableColumnFlags.WidthFixed, 250f);
                ImGui.TableSetupColumn("History", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                Summary(power);
                ImGui.TableNextColumn();
                History(energy, new Vector2(Math.Max(160f, ImGui.GetContentRegionAvail().X), 150f));
                ImGui.EndTable();
            }

            ImGui.SeparatorText("Sources");
            Sources(power);

            PowerBars.Storage("##shipStorage", power.Battery_kW, power.StoredFraction, power.StoreMax_kJ,
                power.MaxCharge_kW, power.MaxDischarge_kW, power.TimeToFullOrEmpty_s);

            ImGui.SeparatorText("Loads");
            Loads(power);
        }

        /// <summary>State words and colour for the whole ship: charging with loads waiting is amber, not "no power".</summary>
        public static (string Words, Vector4 Colour) State(ShipPowerView power)
        {
            // Charging with the loads waiting is not a shortage: the plant runs flat out into the
            // batteries, and the engine serves sensors and other loads once they are full.
            if (power.LoadsWaitOnCharge)
                return ("charging batteries", PowerRing.PartialColour);
            double share = ShipPowerTotals.Share(power);
            return (PowerRing.WordsFor(share), PowerRing.ColourFor(share));
        }

        /// <summary>
        /// The state for a narrow column (the ship Summary tab): one word or two that fit on one line,
        /// with the long form for the hover. "Charging" instead of "charging batteries".
        /// </summary>
        public static (string Words, Vector4 Colour, string Tooltip) ShortState(ShipPowerView power)
        {
            var (words, colour) = State(power);
            if (power.LoadsWaitOnCharge)
            {
                string tip = "Charging batteries: the plant runs flat out into the batteries, and sensors and other loads wait until they are full"
                    + (power.Battery_kW > 0 && power.TimeToFullOrEmpty_s is { } full ? $" (full in {Duration(full)})" : "") + ".";
                return ("Charging", colour, tip);
            }
            return (Capitalise(words), colour, Capitalise(words));
        }

        static void Summary(ShipPowerView power)
        {
            var (words, colour) = State(power);
            ImGui.PushStyleColor(ImGuiCol.Text, colour);
            ImGui.TextUnformatted(Capitalise(words));
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.TextDisabled("[?]");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(HowItWorks);
            if (power.LoadsWaitOnCharge)
                ImGui.TextDisabled("Loads wait until full" + (power.Battery_kW > 0 && power.TimeToFullOrEmpty_s is { } full ? ", " + Duration(full) : ""));
            if (!power.HasPlant)
                ImGui.TextDisabled("No power plant");

            ImGui.Columns(2, "ShipPowerSummaryCols", false);
            ImGui.SetColumnWidth(0, 96f);
            DisplayHelpers.PrintRow("Generation", Stringify.Power(power.Generation_kW), tooltipTwo: "What the sources can give now (an RTG after decay, a reactor or fuel cell only with fuel)");
            // Life support is in every total here, as in the Loads rows (the Api's demand and delivered leave it out).
            double lifeSupport = LifeSupportRows.Demand_kW(power);
            DisplayHelpers.PrintRow("Demand", Stringify.Power(ShipPowerTotals.Demand_kW(power)),
                tooltipTwo: $"Life support {Stringify.Power(lifeSupport)}, warp {Stringify.Power(power.Demand_kW)}, other loads {Stringify.Power(power.AuxDemand_kW)}");
            DisplayHelpers.PrintRow("Available", Stringify.Power(ShipPowerTotals.Served_kW(power)),
                tooltipTwo: $"What reaches the loads: life support {Stringify.Power(ShipPowerTotals.LifeSupportServed_kW(power))}, warp and other loads {Stringify.Power(ShipPowerTotals.OtherServed_kW(power))}. Charging is not counted.");
            if (Math.Abs(power.Battery_kW) > 1e-6)
                DisplayHelpers.PrintRow(power.Battery_kW > 0 ? "To batteries" : "Discharging", Stringify.Power(Math.Abs(power.Battery_kW)),
                    tooltipTwo: power.Battery_kW > 0 ? "Output over warp demand while the batteries have room" : "The batteries cover what generation does not");
            // PrintRow draws with ImGui.Text, which reads printf formats: escape the percent signs.
            DisplayHelpers.PrintRow("Share", $"{ShipPowerTotals.Share(power) * 100:0}%%", tooltipTwo: "Available over demand, life support included");
            if (power.StoreMax_kJ > 0)
            {
                double fraction = Math.Clamp(power.StoredFraction, 0, 1);
                DisplayHelpers.PrintRow("Batteries", $"{fraction * 100:0.#}%% of {Stringify.Energy(power.StoreMax_kJ, "0.#")}",
                    tooltipTwo: $"{Stringify.Energy(Math.Max(0, power.Stored_kJ))} of {Stringify.Energy(power.StoreMax_kJ)}, as of the plant's last energy step", separator: false);
            }
            ImGui.Columns(1);
            if (ShipPowerTotals.MustRun_kW(power) > power.Generation_kW && power.Stored_kJ > 0)
                ImGui.TextWrapped("Generation is below the must-run demand (warp and life support). The batteries cover the gap while they last.");
        }

        /// <summary>A stacked bar of what each source can give now, grey for decayed or idle capacity, then one row per source.</summary>
        static void Sources(ShipPowerView power)
        {
            if (power.Sources.Count == 0)
            {
                ImGui.TextDisabled("No power sources on this ship.");
                return;
            }
            double nameplate = power.Sources.Sum(s => s.Nameplate_kW);
            double output = power.Sources.Sum(s => s.Output_kW);
            var segments = new List<PowerSegment>();
            foreach (var s in power.Sources)
                segments.Add(new PowerSegment(s.Kind, s.Output_kW, 1, PowerBars.SourceColour(s.Kind),
                    $"{s.Kind}: {Stringify.Power(s.Output_kW, "0.#")} of {Stringify.Power(power.Generation_kW, "0.#")} available"));
            if (PowerBars.DischargeSegment(power.Battery_kW) is { } discharge)
                segments.Add(discharge);
            if (nameplate > output)
                segments.Add(new PowerSegment("Unavailable", nameplate - output, 0, PowerBars.UnservedColour,
                    $"{Stringify.Power(nameplate - output, "0.#")} installed but not available (decay, or no fuel)"));
            double scale = Math.Max(nameplate + Math.Max(0, -power.Battery_kW), power.TotalDemand_kW + Math.Max(0, power.Battery_kW));
            PowerBars.StackedBar("##shipGenBar", segments, scale, marker_kW: power.TotalDemand_kW > 0 ? power.TotalDemand_kW : null,
                markerTooltip: $"Demand {Stringify.Power(power.TotalDemand_kW)}");

            var rows = new List<PowerRow>();
            foreach (var s in power.Sources)
            {
                string tip = s.Name + (s.Kind == "Reactor" ? " (fission reactor or steam turbine)" : "")
                    + $"\nNow {Stringify.Power(s.Output_kW, "0.#")} of {Stringify.Power(s.Nameplate_kW, "0.#")} installed";
                if (FuelDetail(s) is { } detail)
                    tip += "\n" + detail;
                if (s.Note != null)
                    tip += "\n" + s.Note;
                rows.Add(new PowerRow
                {
                    Id = s.Kind,
                    Chip = PowerBars.SourceColour(s.Kind),
                    Name = s.Count > 1 ? $"{s.Kind} x{s.Count}" : s.Kind,
                    Value = Stringify.Power(s.Output_kW, "0.#"),
                    Percent = PowerBars.Percent(s.Output_kW, power.Generation_kW),
                    Fill = s.Nameplate_kW > 0 ? (float)Math.Clamp(s.Output_kW / s.Nameplate_kW, 0, 1) : 0f,
                    FillColour = PowerBars.SourceColour(s.Kind),
                    Trailing = s.Note ?? FuelText(s),
                    TrailingColour = s.Note != null ? WarnColour : null,
                    Tooltip = tip,
                });
            }
            PowerBars.Rows("##shipSources", rows);
        }

        /// <summary>The short cell text, e.g. "600 kg, 15,210 y" or "50% left, t1/2 88 y".</summary>
        static string FuelText(ShipPowerSourceRow s)
        {
            if (s.DecayRemaining is { } left)
                return $"{left * 100:0}% of new" + (s.HalfLife_years is { } hl ? $", t1/2 {hl:0} y" : "");
            if (s.FuelRemaining_kg is { } kg)
                return s.RunwayAtFull_s is { } runway ? $"{Stringify.Mass(kg, "0.#")}, {ShortDuration(runway)}" : Stringify.Mass(kg, "0.#");
            return s.Kind == "Solar" ? "Sunlight" : "";
        }

        /// <summary>The long form for the hover.</summary>
        static string? FuelDetail(ShipPowerSourceRow s)
        {
            if (s.DecayRemaining is { } left)
                return $"Output is {left * 100:0.#}% of new" + (s.HalfLife_years is { } hl ? $"; it halves every {hl:0.#} years" : "");
            if (s.FuelRemaining_kg is not { } kg)
                return null;
            string text = $"{s.FuelName ?? "Fuel"}: {Stringify.Mass(kg)} left";
            if (s.RunwayAtFull_s is { } runway)
                text += $", lasts {Duration(runway)} at full output";
            if (s.FuelBurnAtFull_kgPerS is { } burn)
                text += $"\nBurns {Stringify.Mass(burn * 86400.0)}/day at full output";
            return text;
        }

        /// <summary>A stacked demand bar split by load group (hatched where unmet), then one compact row per group.</summary>
        static void Loads(ShipPowerView power)
        {
            // Life support (crew plan phase 4) is a must-run load: one row, listed first as it is served first,
            // standing in for the engine's life-support group (left out of the loop below). TotalDemand_kW leaves
            // life support out, so every share here is of demand plus life support.
            double scale = LifeSupportRows.LoadScale_kW(power);
            var lifeRow = LifeSupportRows.ShipRow(power, PowerBars.GroupColour(0), scale);
            var others = LifeSupportRows.OtherGroups(power).ToList();
            if (others.Count == 0 && lifeRow == null)
            {
                ImGui.TextDisabled("Nothing on this ship draws power.");
                return;
            }

            var segments = new List<PowerSegment>();
            var rows = new List<PowerRow>();
            int index = 0;
            if (lifeRow != null)
            {
                index++;
                double lifeDemand = LifeSupportRows.Demand_kW(power);
                double lifeShare = Math.Clamp(power.LifeSupportServedFraction, 0, 1);
                segments.Add(new PowerSegment(LifeSupportRows.Kind, lifeDemand, lifeShare, lifeRow.Chip,
                    $"{LifeSupportRows.Kind} (must-run): {Stringify.Power(lifeDemand)}, {PowerRing.WordsFor(lifeShare)}"));
                rows.Add(lifeRow);
            }
            foreach (var g in others)
            {
                Vector4 chip = PowerBars.GroupColour(index++);
                Vector4 stateColour = g.State switch
                {
                    "Powered" => PowerRing.FullColour,
                    "Partial" => PowerRing.PartialColour,
                    "No power" => PowerRing.NoneColour,
                    "Charging" => PowerRing.PartialColour,
                    _ => Styles.DescriptiveColor,
                };
                double share = g.Satisfaction ?? (g.State == "Partial" ? 0.5 : g.State == "No power" ? 0 : 1);
                bool waiting = g.State == "Charging";
                // Waiting on a charge: the row's bar shows how full the batteries are, in amber.
                float fill = g.State switch
                {
                    "Idle" => 0f,
                    "Charging" => power.StoreMax_kJ > 0 ? (float)Math.Clamp(power.StoredFraction, 0, 1) : 0f,
                    _ => (float)share,
                };
                bool perDig = g.Kind == "Miners";
                string words = Words(g);
                segments.Add(new PowerSegment(g.Kind, g.Demand_kW, waiting ? 0 : share, chip,
                    $"{g.Kind}: {Stringify.Power(g.Demand_kW)}, {words}", waiting));

                string tip = $"{g.Kind}: {(perDig ? "draws per dig" : Stringify.Power(g.Demand_kW) + " demand")}, {Stringify.Power(g.Served_kW)} served, {words}";
                if (waiting && power.StoreMax_kJ > 0)
                    tip += $"\nBatteries {Math.Clamp(power.StoredFraction, 0, 1) * 100:0}% full" + (power.Battery_kW > 0 && power.TimeToFullOrEmpty_s is { } full ? $", full in {Duration(full)}" : "");
                if (g.Note != null)
                    tip += "\n" + g.Note;
                rows.Add(new PowerRow
                {
                    Id = g.Kind,
                    Chip = chip,
                    Name = g.Kind,
                    Value = perDig ? "per dig" : Stringify.Power(g.Demand_kW),
                    Percent = perDig ? "-" : PowerBars.Percent(g.Demand_kW, scale),
                    Fill = fill,
                    FillColour = stateColour,
                    Waiting = waiting,
                    Trailing = words,
                    TrailingColour = stateColour,
                    Tooltip = tip,
                    Detail = g.Loads.Select(l => ($"{l.Count} x {l.Name}", l.Draw_kW > 0 ? Stringify.Power(l.Draw_kW) : (l.Note ?? "-"))).ToList(),
                });
            }

            // Charging is shown as its own slice so the bar adds up to demand plus what goes into the batteries.
            if (PowerBars.ChargeSegment(power.Battery_kW) is { } charge)
                segments.Add(charge);
            double loadScale = scale + Math.Max(0, power.Battery_kW);
            if (loadScale > 0)
                PowerBars.StackedBar("##shipDemandBar", segments, loadScale);
            else
                ImGui.TextDisabled("Nothing draws power right now.");
            PowerBars.Rows("##shipLoads", rows);
        }

        /// <summary>The colony tiers' words; a miner that only reports "short" says so without a number.</summary>
        static string Words(ShipPowerLoadGroup g) => g.State switch
        {
            "Idle" => "idle",
            "Charging" => "charging batteries",
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
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Stored energy over the plant's last energy steps. Hover the chart for each step's net flow.");
            var history = energy?.Histogram ?? Array.Empty<EnergyHistogramPoint>();
            if (energy == null || energy.StoreMax <= 0 || history.Count < 2)
            {
                ImGui.TextDisabled(energy == null ? "No power plant, so no history." : "No battery history yet.");
                return;
            }

            double max = Math.Max(energy.StoreMax, history.Max(h => h.Stored));
            if (max <= 0) max = 1;
            float axisLeft = ImGui.CalcTextSize(Stringify.Energy(max, "0.#")).X + 8f;
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
                string label = Stringify.Energy(max * f, "0.#");
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

        /// <summary>"15,210 y", "40 d", "5 h", "12 min".</summary>
        static string ShortDuration(double seconds) => PowerBars.ShortDuration(seconds);

        static string Capitalise(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
