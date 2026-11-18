using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Colony Workforce tab (multi-faction phases 5–6): required / employed / commuters / civilians / target,
    /// staffing by tier and facility, and same-body bloc contracts. Formatters and action reasons are pure for tests.
    /// </summary>
    public static class ColonyWorkforceDisplay
    {
        /// <summary>Signing fee to turn one commuter into a resident, cr (engine: RelocateCommutersCommand).</summary>
        public const decimal RelocateFee = 100m;

        sealed class Draft
        {
            public int ColonyId;
            public int Target;
            public readonly Dictionary<int, int> Counts = new();
        }

        static readonly Dictionary<int, Draft> _drafts = new();

        public static string StaffedPercent(double staffed)
            => (Math.Clamp(staffed, 0, 1) * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%";

        public static long OpenPosts(WorkforceView view)
            => Math.Max(0, view.Target - view.Employed - view.Commuters);

        public static string? ContractReason(WorkforceView view, WorkforceContractRow row, long count)
        {
            if (count < 1)
                return "Enter a count of at least 1.";
            if (!row.CanContract)
                return "Relations must be Friendly both ways.";
            if (row.LabourPool <= 0)
                return "No labour available in this bloc's pool.";
            if (OpenPosts(view) <= 0)
                return "No open posts (raise Target to hire).";
            return null;
        }

        public static string? EndReason(WorkforceContractRow row, long count)
        {
            if (count < 1)
                return "Enter a count of at least 1.";
            if (row.Contracted <= 0)
                return "No commuters under contract.";
            return null;
        }

        public static string? RelocateReason(WorkforceContractRow row, long count, decimal funds)
        {
            if (count < 1)
                return "Enter a count of at least 1.";
            if (row.Contracted <= 0)
                return "No commuters to relocate.";
            decimal cost = RelocateFee * Math.Min(count, row.Contracted);
            if (funds < cost)
                return "Need " + cost.ToString("0", CultureInfo.InvariantCulture) + " cr (" + RelocateFee.ToString("0", CultureInfo.InvariantCulture) + " cr each).";
            return null;
        }

        public static string RelocateButtonLabel() => "Relocate (" + RelocateFee.ToString("0", CultureInfo.InvariantCulture) + " cr each)";

        /// <summary>One consumption good, so later goods (consumer, medical, luxuries) can append to the same table.</summary>
        public readonly record struct NeedGoodRow(string Name, double PerDay, long Stock, double? DaysOfStock, bool Short);

        public static NeedGoodRow[] Goods(ColonyNeedsView needs) => new[]
        {
            new NeedGoodRow("Food", needs.FoodPerDay, needs.FoodStock, needs.FoodDaysOfStock, needs.FoodShort),
            new NeedGoodRow("Water", needs.WaterPerDay, needs.WaterStock, needs.WaterDaysOfStock, needs.WaterShort),
        };

        public static string FormatPerDay(double amount)
            => amount.ToString("0.##", CultureInfo.InvariantCulture) + " a day";

        public static string FormatStock(long amount)
            => amount.ToString("N0", CultureInfo.InvariantCulture);

        /// <summary>"—" when the colony draws nothing from stock (a habitable world); otherwise days at today's use.</summary>
        public static string FormatDaysOfStock(double? days)
            => days is double d ? d.ToString("0.#", CultureInfo.InvariantCulture) + " days" : "—";

        /// <summary>The first short day, or null when supplied. <paramref name="now"/> is the current game date.</summary>
        public static DateTime? ShortageStartedOn(DateTime now, int? shortageDay)
            => shortageDay is int d ? now.Date.AddDays(-d) : null;

        public static string ShortageText(ColonyNeedsView needs, DateTime? now)
        {
            if (needs.ShortageDay is not int day)
                return string.IsNullOrEmpty(needs.ShortageState) ? "Supplied" : needs.ShortageState;
            if (now is DateTime date && ShortageStartedOn(date, day) is DateTime started)
                return needs.ShortageState + " since " + DateDisplay.FormatDate(started);
            return needs.ShortageState + " (day " + day.ToString(CultureInfo.InvariantCulture) + ")";
        }

        public static string SatisfactionText(ColonyNeedsView needs)
            => needs.Satisfaction.ToString("0.#", CultureInfo.InvariantCulture) + " / "
                + needs.SatisfactionTarget.ToString("0.#", CultureInfo.InvariantCulture);

        public static string FactorText(double factor)
            => factor.ToString("0.##", CultureInfo.InvariantCulture) + "×";

        public static void Display(int colonyId, WorkforceView view, GlobalUIState uiState, ColonyNeedsView? needs = null, DateTime? now = null)
        {
            if (!_drafts.TryGetValue(colonyId, out var draft) || draft.ColonyId != colonyId)
            {
                draft = new Draft { ColonyId = colonyId, Target = (int)Math.Clamp(view.Target, 0, int.MaxValue) };
                _drafts[colonyId] = draft;
            }

            if (ImGui.BeginTable("##workforce-summary", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg))
            {
                SummaryRow("Required", view.Required.ToString("N0"), "Staff the enabled installations need (CrewReq × count).");
                SummaryRow("Employed", view.Employed.ToString("N0"), "Own residents at work.");
                SummaryRow("Commuters", view.Commuters.ToString("N0"), "Contract staff from blocs on this body. They stay bloc residents.");
                SummaryRow("Civilians", view.Civilians.ToString("N0"), "Residents not at work. The academy, recruitment and NPC crews draw only from these.");
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextColored(Styles.DescriptiveColor, view.TargetSet ? "Target (set)" : "Target");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(120f);
                ImGui.InputInt("##staff-target", ref draft.Target);
                if (draft.Target < 0)
                    draft.Target = 0;
                ImGui.SameLine();
                if (ImGui.Button("Apply"))
                    uiState.GameClient?.SubmitCommandAsync(new SetStaffTargetCommand(colonyId, draft.Target));
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Wanted headcount (own staff plus commuters). Lay-offs apply at once; hiring ramps daily.");
                ImGui.SameLine();
                if (!view.TargetSet)
                    ImGui.BeginDisabled();
                if (ImGui.Button("Reset"))
                {
                    uiState.GameClient?.SubmitCommandAsync(new SetStaffTargetCommand(colonyId, null));
                    draft.Target = (int)Math.Clamp(view.Required, 0, int.MaxValue);
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Reset the target to Required.");
                if (!view.TargetSet)
                    ImGui.EndDisabled();
                ImGui.EndTable();
            }

            if (view.StaffWagePerDay > 0 || view.StaffWagesPerMonth > 0)
            {
                ImGui.TextUnformatted("Staff wages " + view.StaffWagesPerMonth.ToString("C2", CultureInfo.CurrentCulture)
                    + " a month (" + view.StaffWagePerDay.ToString("C2", CultureInfo.CurrentCulture) + " a day each).");
            }

            if (needs != null)
                DrawConsumption(needs, now);

            ImGui.Spacing();
            ImGui.TextUnformatted("Tiers");
            if (ImGui.BeginTable("##workforce-tiers", 4, Styles.TableFlags | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Tier");
                ImGui.TableSetupColumn("Rank", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Required");
                ImGui.TableSetupColumn("Staffed");
                ImGui.TableHeadersRow();
                foreach (var tier in view.Tiers)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(tier.Tier);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(tier.TierRank.ToString(CultureInfo.InvariantCulture));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(tier.Required.ToString("N0"));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(StaffedPercent(tier.Staffed));
                }
                ImGui.EndTable();
                if (view.Tiers.Count == 0)
                    ImGui.TextDisabled("No tiers.");
            }

            ImGui.Spacing();
            ImGui.TextUnformatted("Facilities");
            if (ImGui.BeginTable("##workforce-facilities", 5, Styles.TableFlags | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY,
                    new Vector2(0, Math.Min(220f, ImGui.GetContentRegionAvail().Y * 0.4f))))
            {
                ImGui.TableSetupColumn("Facility");
                ImGui.TableSetupColumn("Count", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Tier");
                ImGui.TableSetupColumn("Required");
                ImGui.TableSetupColumn("Staffed");
                ImGui.TableHeadersRow();
                foreach (var facility in view.Facilities)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(facility.Name);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(facility.Count.ToString("N0"));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(facility.Tier);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(facility.Required.ToString("N0"));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(StaffedPercent(facility.Staffed));
                }
                ImGui.EndTable();
                if (view.Facilities.Count == 0)
                    ImGui.TextDisabled("No facilities.");
            }

            ImGui.Spacing();
            ImGui.TextUnformatted("Contracts");
            decimal funds = 0;
            var faction = uiState.GameClient?.Galaxy.Faction;
            if (faction != null)
                funds = faction.Funds - faction.AccruedThisMonth;

            if (ImGui.BeginTable("##workforce-contracts", 7, Styles.TableFlags | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Bloc");
                ImGui.TableSetupColumn("Contracted");
                ImGui.TableSetupColumn("Labour pool");
                ImGui.TableSetupColumn("Count", ImGuiTableColumnFlags.WidthFixed, 90);
                ImGui.TableSetupColumn("Contract", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("End", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Relocate", ImGuiTableColumnFlags.WidthFixed, 160);
                ImGui.TableHeadersRow();
                foreach (var row in view.Contracts)
                {
                    if (!draft.Counts.TryGetValue(row.ColonyId, out int count))
                        count = 1;
                    ImGui.PushID(row.ColonyId);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(row.FactionName);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(row.Contracted.ToString("N0"));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(row.LabourPool.ToString("N0"));
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(80f);
                    ImGui.InputInt("##count", ref count);
                    if (count < 1)
                        count = 1;
                    draft.Counts[row.ColonyId] = count;

                    ImGui.TableNextColumn();
                    ActionButton("Contract", ContractReason(view, row, count), () =>
                        uiState.GameClient?.SubmitCommandAsync(new ContractStaffCommand(colonyId, row.ColonyId, count)));

                    ImGui.TableNextColumn();
                    ActionButton("End", EndReason(row, count), () =>
                        uiState.GameClient?.SubmitCommandAsync(new ContractStaffCommand(
                            colonyId, row.ColonyId, -Math.Min(count, row.Contracted))));

                    ImGui.TableNextColumn();
                    ActionButton(RelocateButtonLabel(), RelocateReason(row, count, funds), () =>
                        uiState.GameClient?.SubmitCommandAsync(new RelocateCommutersCommand(
                            colonyId, row.ColonyId, Math.Min(count, row.Contracted))));
                    ImGui.PopID();
                }
                ImGui.EndTable();
                if (view.Contracts.Count == 0)
                    ImGui.TextDisabled("No other factions on this body.");
            }
        }

        static void DrawConsumption(ColonyNeedsView needs, DateTime? now)
        {
            ImGui.Spacing();
            ImGui.TextUnformatted("Consumption");
            if (ImGui.BeginTable("##workforce-needs", 5, Styles.TableFlags | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Good");
                ImGui.TableSetupColumn("Use");
                ImGui.TableSetupColumn("Stock");
                ImGui.TableSetupColumn("Days of stock");
                ImGui.TableSetupColumn("Status");
                ImGui.TableHeadersRow();
                foreach (var good in Goods(needs))
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(good.Name);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(FormatPerDay(good.PerDay));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(FormatStock(good.Stock));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(FormatDaysOfStock(good.DaysOfStock));
                    ImGui.TableNextColumn();
                    if (good.Short)
                        ImGui.PushStyleColor(ImGuiCol.Text, Styles.BadColor);
                    ImGui.TextUnformatted(good.Short ? "Short" : "Supplied");
                    if (good.Short)
                        ImGui.PopStyleColor();
                }
                ImGui.EndTable();
            }

            if (ImGui.BeginTable("##workforce-needs-summary", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg))
            {
                bool shorting = needs.ShortageState != "Supplied" && needs.ShortageDay != null;
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextColored(Styles.DescriptiveColor, "Shortage");
                ImGui.TableNextColumn();
                if (shorting)
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.BadColor);
                ImGui.TextUnformatted(ShortageText(needs, now));
                if (shorting)
                    ImGui.PopStyleColor();
                SummaryRow("Deaths", needs.DeathsTotal.ToString("N0"), "Deaths from shortage since the clock started.");
                SummaryRow("Leavers", needs.LeftTotal.ToString("N0"), "People who left because of the shortage.");
                SummaryRow("Satisfaction", SatisfactionText(needs), "Current satisfaction and the target it moves toward (20% every 30 days).");
                SummaryRow("Growth", FactorText(needs.GrowthFactor), "Population growth multiplier. 0 while short.");
                SummaryRow("Output", FactorText(needs.OutputFactor), "Industry, mining and research multiplier.");
                ImGui.EndTable();
            }
        }

        static void SummaryRow(string name, string value, string tip)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextColored(Styles.DescriptiveColor, name);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(value);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(tip);
        }

        static void ActionButton(string label, string? reason, Action submit)
        {
            if (reason != null)
                ImGui.BeginDisabled();
            if (ImGui.Button(label))
                submit();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) && reason != null)
                ImGui.SetTooltip(reason);
            if (reason != null)
                ImGui.EndDisabled();
        }
    }
}
