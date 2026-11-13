using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client.Interface.Widgets;

/// <summary>
/// When the crew buttons may send their commands, from Api views only. Each refusal is the sentence the
/// server would give (or a plainer one), shown on the disabled button's hover; null means enabled.
/// </summary>
public static class CrewActions
{
    public const string NotInOrbit = "Must be in orbit of a colony.";

    /// <summary>Most trainees one crew course can take now: free crew seats, capped by the colony's people.</summary>
    public static int TrainMax(NavalAcademyClassView row, long population)
    {
        int free = Math.Max(0, row.CrewSeats - row.CrewSeatsInUse);
        return (int)Math.Max(0, Math.Min(free, population));
    }

    /// <summary>The count input's value: the free seats by default, clamped to 1..max (0 when nothing fits).</summary>
    public static int ClampTrain(int? count, int max)
        => max <= 0 ? 0 : Math.Clamp(count ?? max, 1, max);

    /// <summary>Null when a crew course can start at this academy.</summary>
    public static string? TrainRefusal(NavalAcademyClassView row, long population)
    {
        if (row.FundsReason.Length > 0)
            return row.FundsReason;
        if (row.CrewSeats <= 0 || row.CrewCourseLengthMonths <= 0)
            return "This academy has no crew seats.";
        if (row.CrewSeats - row.CrewSeatsInUse <= 0)
            return "This academy has no free crew seats.";
        if (row.CrewCourseReason.Length > 0)
            return row.CrewCourseReason;
        if (population <= 0)
            return "The colony has no people to train.";
        return null;
    }

    /// <summary>
    /// The owned colony on the body the ship orbits, or null. The engine moves crew only when the ship's
    /// position parent is the colony's planet.
    /// </summary>
    public static EntitySnapshot? OrbitedColony(EntitySnapshot? ship, IEnumerable<EntitySnapshot>? entities)
    {
        if (ship?.GetView<PositionView>()?.ParentId is not int parent || entities == null)
            return null;
        return entities.FirstOrDefault(e => e.Kind == BodyKind.Colony
            && e.Relation == OwnerRelation.Owned
            && e.GetView<ColonyView>()?.PlanetEntityId == parent);
    }

    /// <summary>Crew the ship can still take aboard: min(required, berths) less those aboard.</summary>
    public static int Room(ShipView ship)
        => Math.Max(0, Math.Min(ship.CrewRequired, ship.CrewBerths) - ship.CrewAboard);

    /// <summary>Null when Top up may fill the ship from <paramref name="colony"/>'s pool.</summary>
    public static string? TopUpRefusal(ShipView ship, EntitySnapshot? colony)
    {
        if (colony == null)
            return NotInOrbit;
        if (!ship.CrewTracked)
            return "This ship does not carry crew.";
        if (Room(ship) <= 0)
            return "The ship has a full crew.";
        if ((colony.GetView<ColonyView>()?.CrewPool ?? 0) <= 0)
            return "The colony's crew pool is empty.";
        return null;
    }

    /// <summary>Null when To pool may send everyone aboard down to <paramref name="colony"/>'s pool.</summary>
    public static string? ToPoolRefusal(ShipView ship, EntitySnapshot? colony)
    {
        if (colony == null)
            return NotInOrbit;
        if (!ship.CrewTracked)
            return "This ship does not carry crew.";
        if (ship.CrewAboard <= 0)
            return "Nobody is aboard.";
        return null;
    }
}

/// <summary>
/// The crew pieces of the ship, designer, academy and colony screens, in the Power tab look: compact
/// <see cref="PowerRow"/>s with a chip, a value, a share and a thin bar, detail on hover or click, an amber
/// badge for under-crewed ships and amber warning lines.
/// </summary>
public static class CrewBars
{
    public static readonly Vector4 RequiredColour = new(0.35f, 0.62f, 1f, 1f);
    public static readonly Vector4 BerthColour = new(0.72f, 0.5f, 1f, 1f);
    public static readonly Vector4 AboardColour = new(0.3f, 0.82f, 0.85f, 1f);
    public static readonly Vector4 EngineColour = new(0.85f, 0.76f, 0.5f, 1f);
    public static readonly Vector4 ShortColour = PowerRing.PartialColour;

    /// <summary>Filled share of <paramref name="need"/>, 0..1; 1 when nothing is needed.</summary>
    public static float Fill(long have, long need) => need <= 0 ? 1f : (float)Math.Clamp((double)have / need, 0, 1);

    /// <summary>"28 crew, 30 berths" for the designer and the fleet header.</summary>
    public static string RequiredVsBerths(int required, int berths)
        => $"{required:N0} crew, {berths:N0} {(berths == 1 ? "berth" : "berths")}";

    /// <summary>"18 berths short" when the parts house fewer than the crew they need, else empty.</summary>
    public static string BerthShortfall(int required, int berths)
        => berths < required ? $"{required - berths:N0} {(required - berths == 1 ? "berth" : "berths")} short" : "";

    /// <summary>
    /// The Fleet window Summary's one-line crew indicator: "9/17 aboard" once the engine tracks crew aboard,
    /// "17 needed, 12 berths" before that. The full crew view is the Crew tab.
    /// </summary>
    public static string Indicator(ShipView ship)
        => ship.CrewTracked
            ? $"{ship.CrewAboard:N0}/{ship.CrewRequired:N0} aboard"
            : $"{ship.CrewRequired:N0} needed, {ship.CrewBerths:N0} {(ship.CrewBerths == 1 ? "berth" : "berths")}";

    /// <summary>Crew Tab detail: one row per component design, crew per copy x copies, its share of the crew.</summary>
    public static List<PowerRow> CrewByComponentRows(ShipView ship)
        => ship.CrewByComponent.Select((r, i) => new PowerRow
        {
            Id = "crew-" + r.DesignId,
            Chip = PowerBars.GroupColour(i),
            Name = r.Name,
            Value = r.Crew.ToString("N0"),
            Percent = PowerBars.Percent(r.Crew, ship.CrewRequired),
            Fill = ship.CrewRequired > 0 ? Fill(r.Crew, ship.CrewRequired) : 0f,
            FillColour = PowerBars.GroupColour(i),
            Tooltip = r.Name + ": " + r.CrewPerCopy.ToString("N0") + " crew x " + r.Count.ToString("N0") + " = " + r.Crew.ToString("N0") + ".",
        }).ToList();

    /// <summary>Crew tab detail: one row per design that gives berths, its share of the berths.</summary>
    public static List<PowerRow> BerthsByComponentRows(ShipView ship)
        => ship.BerthsByComponent.Select((r, i) => new PowerRow
        {
            Id = "berth-" + r.DesignId,
            Chip = PowerBars.GroupColour(i),
            Name = r.Name,
            Value = r.Berths.ToString("N0"),
            Percent = PowerBars.Percent(r.Berths, ship.CrewBerths),
            Fill = ship.CrewBerths > 0 ? Fill(r.Berths, ship.CrewBerths) : 0f,
            FillColour = PowerBars.GroupColour(i),
            Tooltip = r.Name + ": " + r.BerthsPerCopy.ToString("N0") + " berths x " + r.Count.ToString("N0") + " = " + r.Berths.ToString("N0") + ".",
        }).ToList();

    /// <summary>
    /// The Fleet window's rows, narrow enough for its summary column: a chip, a name, a value and a thin
    /// bar, with the share and what it limits in the hover. Aboard and engine crew show only while
    /// <see cref="ShipView.CrewTracked"/>; before crew plan phase 3 they are defaults and would read as a
    /// full or an empty ship.
    /// </summary>
    public static List<PowerRow> ShipRows(ShipView ship)
    {
        int required = ship.CrewRequired;
        var rows = new List<PowerRow>
        {
            new()
            {
                Id = "required",
                Chip = RequiredColour,
                Name = "Needed",
                Value = required.ToString("N0"),
                Tooltip = "Each component's crew per copy, times its copies.",
                Detail = ship.CrewByComponent.Select(r => (r.Name + "  " + r.CrewPerCopy.ToString("N0") + " x " + r.Count.ToString("N0"), r.Crew.ToString("N0"))).ToList(),
            },
        };

        bool berthShort = ship.CrewBerths < required;
        string shortfall = BerthShortfall(required, ship.CrewBerths);
        rows.Add(new PowerRow
        {
            Id = "berths",
            Chip = BerthColour,
            Name = "Berths",
            Value = ship.CrewBerths.ToString("N0"),
            Fill = Fill(ship.CrewBerths, required),
            FillColour = berthShort ? ShortColour : PowerRing.FullColour,
            Tooltip = "Crew quarters plus bridge berths: " + PowerBars.Percent(ship.CrewBerths, required) + " of the crew needed."
                + (shortfall.Length > 0 ? " " + shortfall + "." : "") + " A ship takes aboard at most its berths.",
            Detail = ship.BerthsByComponent.Select(r => (r.Name + "  " + r.BerthsPerCopy.ToString("N0") + " x " + r.Count.ToString("N0"), r.Berths.ToString("N0"))).ToList(),
        });

        if (!ship.CrewTracked)
            return rows;

        bool shortAboard = ship.UnderCrewed || ship.CrewFactor < 1;
        rows.Add(new PowerRow
        {
            Id = "aboard",
            Chip = AboardColour,
            Name = "Aboard",
            Value = ship.CrewAboard.ToString("N0") + "/" + required.ToString("N0"),
            Fill = (float)Math.Clamp(ship.CrewFactor, 0, 1),
            FillColour = shortAboard ? ShortColour : PowerRing.FullColour,
            Tooltip = "Crew fraction " + PowerBars.Percent(ship.CrewFactor, 1) + " (aboard / required)."
                + " Surveyors, miners, shuttlebays and construction bays run at its square: " + WorkRate(ship.CrewFactor) + ".",
        });
        bool engineShort = ship.EngineCrewFactor < 1;
        rows.Add(new PowerRow
        {
            Id = "engines",
            Chip = EngineColour,
            Name = "Drives",
            Value = PowerBars.Percent(ship.EngineCrewFactor, 1),
            Fill = (float)Math.Clamp(ship.EngineCrewFactor, 0, 1),
            FillColour = engineShort ? ShortColour : PowerRing.FullColour,
            Tooltip = "Crew fill the engines first. Below the engines' own crew, engine power scales with the share filled"
                + (engineShort ? ": engines at " + PowerBars.Percent(ship.EngineCrewFactor, 1) + "." : "."),
        });
        return rows;
    }

    /// <summary>Work-part rate at crew fraction F: F squared, as a percent ("25%" at F = 0.5).</summary>
    public static string WorkRate(double crewFactor)
    {
        double f = Math.Clamp(crewFactor, 0, 1);
        return PowerBars.Percent(f * f, 1);
    }

    /// <summary>The academy's crew course as one row: seats in use, length and the next graduation.</summary>
    public static PowerRow CourseRow(NavalAcademyClassView academy)
    {
        string next = academy.NextCrewGraduation is { } date
            ? "next " + date.ToString("yyyy-MM-dd") + (academy.NextCrewGraduationCount > 0 ? " (" + academy.NextCrewGraduationCount.ToString("N0") + ")" : "")
            : "none running";
        bool full = academy.CrewSeats > 0 && academy.CrewSeatsInUse >= academy.CrewSeats;
        return new PowerRow
        {
            Id = "crew-course-" + academy.ComponentId,
            Chip = AboardColour,
            Name = "Crew course",
            Value = academy.CrewSeatsInUse.ToString("N0") + " / " + academy.CrewSeats.ToString("N0"),
            Percent = PowerBars.Percent(academy.CrewSeatsInUse, academy.CrewSeats),
            Fill = academy.CrewSeats > 0 ? Fill(academy.CrewSeatsInUse, academy.CrewSeats) : 0f,
            FillColour = full ? ShortColour : RequiredColour,
            Trailing = academy.CrewCourseLengthMonths.ToString("N0") + " mo, " + next,
            Tooltip = "Crew (ratings) train in their own seats, apart from the officer classes, and graduate into the colony's crew pool.",
            Detail = new List<(string, string)>
            {
                ("Seats", academy.CrewSeats.ToString("N0")),
                ("In use", academy.CrewSeatsInUse.ToString("N0")),
                ("Course length", academy.CrewCourseLengthMonths.ToString("N0") + " months"),
                ("Next graduation", academy.NextCrewGraduation?.ToString("yyyy-MM-dd") ?? "-"),
                ("Graduating", academy.NextCrewGraduationCount.ToString("N0")),
            },
        };
    }

    /// <summary>Pool rows by species, largest first, each with its share of the pool.</summary>
    public static List<PowerRow> PoolRows(IReadOnlyList<CrewPoolRow> bySpecies)
    {
        long total = bySpecies.Sum(r => r.Trained);
        return bySpecies
            .OrderByDescending(r => r.Trained).ThenBy(r => r.SpeciesName)
            .Select((r, i) => new PowerRow
            {
                Id = "pool-" + r.SpeciesName,
                Chip = PowerBars.GroupColour(i),
                Name = r.SpeciesName,
                Value = r.Trained.ToString("N0"),
                Percent = PowerBars.Percent(r.Trained, total),
                Fill = Fill(r.Trained, total),
                FillColour = PowerBars.GroupColour(i),
                Tooltip = r.SpeciesName + ": " + r.Trained.ToString("N0") + " trained crew, " + PowerBars.Percent(r.Trained, total) + " of the pool.",
            })
            .ToList();
    }

    /// <summary>Stacked-bar segments for the pool, in the same order and colours as <see cref="PoolRows"/>.</summary>
    public static List<PowerSegment> PoolSegments(IReadOnlyList<CrewPoolRow> bySpecies)
        => bySpecies
            .OrderByDescending(r => r.Trained).ThenBy(r => r.SpeciesName)
            .Select((r, i) => new PowerSegment(r.SpeciesName, r.Trained, 1, PowerBars.GroupColour(i),
                r.SpeciesName + ": " + r.Trained.ToString("N0") + " trained crew", ShowLabel: true))
            .ToList();

    /// <summary>
    /// <see cref="PowerRow"/>s for a narrow column (the Fleet window summary): a chip, the name, the value,
    /// then a thin bar that takes whatever width is left. Share, notes and detail go in the hover, so
    /// nothing clips when the pane shrinks or grows a scrollbar.
    /// </summary>
    public static void CompactRows(string id, IReadOnlyList<PowerRow> rows)
    {
        if (rows.Count == 0)
            return;
        float nameW = 0, valueW = 0;
        foreach (var r in rows)
        {
            nameW = Math.Max(nameW, ImGui.CalcTextSize(r.Name).X);
            valueW = Math.Max(valueW, ImGui.CalcTextSize(r.Value).X);
        }
        float line = ImGui.GetTextLineHeight();
        float gap = ImGui.GetStyle().ItemSpacing.X;
        var dim = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
        ImGui.PushID(id);
        foreach (var r in rows)
        {
            ImGui.PushID(r.Id);
            ImGui.BeginGroup();
            var p = ImGui.GetCursorScreenPos();
            var draw = ImGui.GetWindowDrawList();
            draw.AddRectFilled(new Vector2(p.X, p.Y + line * 0.5f - 4f), new Vector2(p.X + 8f, p.Y + line * 0.5f + 4f),
                ImGui.GetColorU32(r.Chip), 2f);
            ImGui.SetCursorScreenPos(new Vector2(p.X + 12f, p.Y));
            ImGui.PushStyleColor(ImGuiCol.Text, dim);
            ImGui.TextUnformatted(r.Name);
            ImGui.PopStyleColor();
            float vw = ImGui.CalcTextSize(r.Value).X;
            ImGui.SameLine(0f, 0f);
            ImGui.SetCursorScreenPos(new Vector2(p.X + 12f + nameW + gap + (valueW - vw), p.Y));
            ImGui.TextUnformatted(r.Value);
            if (r.Fill is { } fill)
            {
                ImGui.SameLine(0f, gap);
                PowerBars.InlineBar(fill, r.FillColour, Math.Max(20f, ImGui.GetContentRegionAvail().X - 2f));
            }
            ImGui.EndGroup();
            if (ImGui.IsItemHovered() && (r.Tooltip != null || r.Detail.Count > 0))
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 30f);
                if (r.Tooltip != null)
                    ImGui.TextUnformatted(r.Tooltip);
                if (r.Detail.Count > 0 && ImGui.BeginTable("##detail", 2, ImGuiTableFlags.SizingFixedFit))
                {
                    foreach (var (name, value) in r.Detail)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(name);
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(value);
                    }
                    ImGui.EndTable();
                }
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
            ImGui.PopID();
        }
        ImGui.PopID();
    }

    /// <summary>A small rounded pill, e.g. the amber "Under-crewed" badge. Hover shows the tooltip.</summary>
    public static void Badge(string text, Vector4 colour, string? tooltip = null)
    {
        var size = ImGui.CalcTextSize(text);
        var pad = new Vector2(5f, 1f);
        var p = ImGui.GetCursorScreenPos();
        float line = ImGui.GetTextLineHeight();
        var min = new Vector2(p.X, p.Y + Math.Max(0f, (line - size.Y) * 0.5f) - pad.Y);
        var max = min + size + pad * 2f;
        var draw = ImGui.GetWindowDrawList();
        draw.AddRectFilled(min, max, ImGui.GetColorU32(new Vector4(colour.X, colour.Y, colour.Z, 0.22f)), 4f);
        draw.AddRect(min, max, ImGui.GetColorU32(colour), 4f);
        draw.AddText(min + pad, ImGui.GetColorU32(colour), text);
        ImGui.Dummy(new Vector2(size.X + pad.X * 2f, line));
        if (tooltip != null && ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 28f);
            ImGui.TextUnformatted(tooltip);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    /// <summary>An amber warning line: a small chip, then the text wrapped to the column.</summary>
    public static void Warning(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;
        var draw = ImGui.GetWindowDrawList();
        var p = ImGui.GetCursorScreenPos();
        float line = ImGui.GetTextLineHeight();
        draw.AddRectFilled(new Vector2(p.X, p.Y + line * 0.5f - 4f), new Vector2(p.X + 8f, p.Y + line * 0.5f + 4f),
            ImGui.GetColorU32(ShortColour), 2f);
        ImGui.SetCursorScreenPos(new Vector2(p.X + 13f, p.Y));
        ImGui.PushStyleColor(ImGuiCol.Text, ShortColour);
        ImGui.PushTextWrapPos(0f);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
        ImGui.PopStyleColor();
    }

    /// <summary>A disabled button whose hover (allowed while disabled) says why.</summary>
    /// <summary>
    /// A button that is disabled with <paramref name="refusal"/> on hover when that is non-null, or shows
    /// <paramref name="tooltip"/> when enabled. True when clicked while enabled.
    /// </summary>
    public static bool GatedButton(string label, string? refusal, string? tooltip = null, Vector2 size = default)
    {
        bool disabled = refusal != null;
        if (disabled)
            ImGui.BeginDisabled();
        bool clicked = ImGui.Button(label, size) && !disabled;
        if (disabled)
            ImGui.EndDisabled();
        string? hover = refusal ?? tooltip;
        if (hover != null && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 28f);
            ImGui.TextUnformatted(hover);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
        return clicked;
    }
}
