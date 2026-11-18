using System;
using System.Collections.Generic;
using System.Globalization;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Colony Summary crew market (multi-faction phase 8): list this colony's spare trained crew,
    /// and buy from other colonies on the same body. Formatters and buy/list helpers are pure for tests.
    /// </summary>
    public static class ColonyCrewMarketDisplay
    {
        sealed class Draft
        {
            public int ColonyId;
            public bool Listed;
            public int Reserve;
            public int Ask;
            public readonly Dictionary<(int SellerColonyId, int SpeciesId), int> Counts = new();
        }

        static readonly Dictionary<int, Draft> _drafts = new();

        /// <summary>No ask may go below the default fee (1.5× tuition).</summary>
        public static decimal ClampAsk(decimal ask, decimal defaultFee)
            => Math.Max(ask, defaultFee);

        /// <summary>An offer row can be bought when the engine left <see cref="CrewOfferView.Reason"/> empty.</summary>
        public static bool OfferEnabled(CrewOfferView offer)
            => string.IsNullOrEmpty(offer.Reason);

        public static string Credits(decimal amount)
            => amount.ToString("N0", CultureInfo.InvariantCulture) + " cr";

        public static string ShareText(decimal share)
            => (share * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%";

        public static string SellerPayout(decimal fee, decimal share)
            => Credits(Math.Round(fee * share, 2));

        /// <summary>
        /// Faction to show on the seller tooltip. Null when the faction name is the same as the seller
        /// (typical for a bloc colony), so the Faction column can stay hidden.
        /// </summary>
        public static string? FactionTip(CrewOfferView offer)
            => string.Equals(offer.SellerColonyName, offer.SellerFactionName, StringComparison.Ordinal)
                ? null
                : offer.SellerFactionName;

        /// <summary>
        /// Why Buy is disabled: the offer's engine Reason, a bad count, or not enough funds.
        /// Null when the buy can be submitted.
        /// </summary>
        public static string? BuyReason(CrewOfferView offer, long count, decimal funds)
        {
            if (!OfferEnabled(offer))
                return offer.Reason;
            if (count < 1)
                return "Enter a count of at least 1.";
            if (count > offer.Available)
                return "Only " + offer.Available.ToString("N0", CultureInfo.InvariantCulture) + " are on offer.";
            decimal cost = offer.Fee * count;
            if (funds < cost)
                return "Need " + Credits(cost) + " (" + Credits(offer.Fee) + " each).";
            return null;
        }

        public static void Display(int colonyId, CrewMarketView view, GlobalUIState uiState)
        {
            if (!_drafts.TryGetValue(colonyId, out var draft) || draft.ColonyId != colonyId)
            {
                draft = NewDraft(colonyId, view);
                _drafts[colonyId] = draft;
            }

            var avail = ImGui.GetContentRegionAvail();
            var windowBg = ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg];
            windowBg.W = 1f;
            var rowBg = windowBg;
            var rowBgAlt = new System.Numerics.Vector4(
                Math.Min(1f, windowBg.X + 0.05f),
                Math.Min(1f, windowBg.Y + 0.05f),
                Math.Min(1f, windowBg.Z + 0.06f),
                1f);
            var headerBg = ImGui.GetStyle().Colors[(int)ImGuiCol.TableHeaderBg];
            headerBg.W = 1f;

            ImGui.PushStyleColor(ImGuiCol.ChildBg, windowBg);
            ImGui.PushStyleColor(ImGuiCol.TableRowBg, rowBg);
            ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, rowBgAlt);
            ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, headerBg);

            // Clip to the content region and paint an opaque background so the system tree cannot show through.
            // A floor keeps the listing usable when this panel sits under other Crew tab sections.
            float height = Math.Max(280f, avail.Y);
            if (ImGui.BeginChild("##crew-market-panel", new System.Numerics.Vector2(avail.X, height)))
            {
                ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + ImGui.GetContentRegionAvail().X);
                ImGui.TextUnformatted("The fee is 1.5× tuition (three months of crew wage), and no ask can go below it. The seller receives "
                    + ShareText(view.SellerShare) + ". Blocs list crew above a reserve of 50.");
                ImGui.PopTextWrapPos();

                DrawListing(view, draft, colonyId, uiState);
                ImGui.Spacing();
                DrawOffers(view, draft, colonyId, uiState);
            }
            ImGui.EndChild();
            ImGui.PopStyleColor(4);
        }

        static void DrawListing(CrewMarketView view, Draft draft, int colonyId, GlobalUIState uiState)
        {
            float labelGap = ImGui.GetStyle().ItemSpacing.X * 2;
            float leftLabelWidth = Math.Max(
                Math.Max(ImGui.CalcTextSize("Pool").X, ImGui.CalcTextSize("Reserve").X),
                Math.Max(ImGui.CalcTextSize("Spare to sell").X, ImGui.CalcTextSize("Default fee").X)) + labelGap;
            float rightLabelWidth = Math.Max(
                Math.Max(ImGui.CalcTextSize("Listed").X, ImGui.CalcTextSize("Ask").X),
                Math.Max(ImGui.CalcTextSize("Fee").X, ImGui.CalcTextSize("Seller share").X)) + labelGap;

            if (ImGui.BeginTable("##crew-market-listing", 4, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("ll", ImGuiTableColumnFlags.WidthFixed, leftLabelWidth);
                ImGui.TableSetupColumn("lv", ImGuiTableColumnFlags.WidthStretch, 1f);
                ImGui.TableSetupColumn("rl", ImGuiTableColumnFlags.WidthFixed, rightLabelWidth);
                ImGui.TableSetupColumn("rv", ImGuiTableColumnFlags.WidthStretch, 1f);

                ValueCell("Pool", view.PoolTotal.ToString("N0", CultureInfo.InvariantCulture),
                    "Trained crew in this colony's pool.");
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(Styles.DescriptiveColor, "Listed");
                ImGui.TableNextColumn();
                ImGui.Checkbox("##crew-listed", ref draft.Listed);
                Tip("List spare trained crew (the pool above the reserve) for sale to other colonies on this body.");

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(Styles.DescriptiveColor, "Reserve");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(120f);
                ImGui.InputInt("##crew-reserve", ref draft.Reserve);
                if (draft.Reserve < 0)
                    draft.Reserve = 0;
                Tip("Crew this colony keeps back from the market.");

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(Styles.DescriptiveColor, "Ask");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(120f);
                ImGui.InputInt("##crew-ask", ref draft.Ask, 100, 1000);
                if (!ImGui.IsItemActive())
                    draft.Ask = (int)Math.Round(ClampAsk(draft.Ask, view.DefaultFee));
                ImGui.SameLine();
                ImGui.TextUnformatted("cr");
                Tip("Fee per crew. The minimum is the default fee (" + Credits(view.DefaultFee) + ").");

                ValueCell("Spare to sell", view.SpareToSell.ToString("N0", CultureInfo.InvariantCulture),
                    "Crew this colony would sell now (listed pool above the reserve).");
                ValueCell("Fee", Credits(view.Fee),
                    "This colony's fee per crew when it sells (the default, or its higher ask).");
                ValueCell("Default fee", Credits(view.DefaultFee),
                    "The lowest fee anyone may ask: 1.5× tuition (" + Credits(view.TuitionEquivalent) + ").");
                ValueCell("Tuition", Credits(view.TuitionEquivalent),
                    "Three months of crew wage: what an academy graduate costs to make.");

                ValueCell("Seller share", ShareText(view.SellerShare) + " (" + SellerPayout(view.Fee, view.SellerShare) + " per crew)",
                    "Share of the fee the seller banks. The rest is the transfer levy.");
                ImGui.TableNextColumn();
                if (ImGui.Button("Update listing"))
                {
                    decimal ask = ClampAsk(draft.Ask, view.DefaultFee);
                    draft.Ask = (int)Math.Round(ask);
                    uiState.GameClient?.SubmitCommandAsync(
                        new ListCrewCommand(colonyId, draft.Listed, draft.Reserve, ask));
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.TextUnformatted("Apply the listed, reserve and ask settings.");
                    ImGui.EndTooltip();
                }
                ImGui.TableNextColumn();

                ImGui.EndTable();
            }
        }

        static void DrawOffers(CrewMarketView view, Draft draft, int colonyId, GlobalUIState uiState)
        {
            ImGui.TextUnformatted("Offers");

            decimal funds = 0;
            var faction = uiState.GameClient?.Galaxy.Faction;
            if (faction != null)
                funds = faction.Funds - faction.AccruedThisMonth;

            float tableHeight = Math.Max(90f, ImGui.GetContentRegionAvail().Y - (view.Offers.Count == 0 ? 24f : 0f));
            float tableWidth = ImGui.GetContentRegionAvail().X;
            if (ImGui.BeginTable("##crew-market-offers", 7,
                    Styles.TableFlags | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Borders,
                    new System.Numerics.Vector2(tableWidth, tableHeight)))
            {
                ImGui.TableSetupColumn("Seller", ImGuiTableColumnFlags.WidthStretch, 1.6f);
                ImGui.TableSetupColumn("Species", ImGuiTableColumnFlags.WidthStretch, 0.8f);
                ImGui.TableSetupColumn("Available", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Fee", ImGuiTableColumnFlags.WidthFixed, 90);
                ImGui.TableSetupColumn("Count", ImGuiTableColumnFlags.WidthFixed, 90);
                ImGui.TableSetupColumn("Buy", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Reason", ImGuiTableColumnFlags.WidthStretch, 1.2f);
                ImGui.TableHeadersRow();

                foreach (var offer in view.Offers)
                {
                    var key = (offer.SellerColonyId, offer.SpeciesId);
                    if (!draft.Counts.TryGetValue(key, out int count))
                        count = 1;
                    ImGui.PushID(offer.SellerColonyId);
                    ImGui.PushID(offer.SpeciesId);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    SellerCell(offer);
                    ImGui.TableNextColumn();
                    NameCell(offer.SpeciesName);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(offer.Available.ToString("N0", CultureInfo.InvariantCulture));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(Credits(offer.Fee));

                    ImGui.TableNextColumn();
                    bool rowEnabled = OfferEnabled(offer);
                    if (!rowEnabled)
                        ImGui.BeginDisabled();
                    ImGui.SetNextItemWidth(80f);
                    ImGui.InputInt("##buy-count", ref count);
                    if (count < 1)
                        count = 1;
                    draft.Counts[key] = count;
                    if (!rowEnabled)
                        ImGui.EndDisabled();

                    ImGui.TableNextColumn();
                    string? reason = BuyReason(offer, count, funds);
                    if (reason != null)
                        ImGui.BeginDisabled();
                    if (ImGui.Button("Buy"))
                    {
                        uiState.GameClient?.SubmitCommandAsync(
                            new BuyCrewCommand(colonyId, offer.SellerColonyId, offer.SpeciesId, count));
                    }
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) && reason != null)
                    {
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted(reason);
                        ImGui.EndTooltip();
                    }
                    if (reason != null)
                        ImGui.EndDisabled();

                    ImGui.TableNextColumn();
                    if (!string.IsNullOrEmpty(offer.Reason))
                        NameCell(offer.Reason);

                    ImGui.PopID();
                    ImGui.PopID();
                }
                ImGui.EndTable();
            }

            if (view.Offers.Count == 0)
                ImGui.TextDisabled("No crew on offer from other colonies on this body.");
        }

        static Draft NewDraft(int colonyId, CrewMarketView view) => new Draft
        {
            ColonyId = colonyId,
            Listed = view.Listed,
            Reserve = (int)Math.Clamp(view.Reserve, 0, int.MaxValue),
            Ask = (int)Math.Clamp(Math.Round(ClampAsk(view.Fee, view.DefaultFee)), 0, int.MaxValue),
        };

        static void ValueCell(string name, string value, string tip)
        {
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(Styles.DescriptiveColor, name);
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(value);
            Tip(tip);
        }

        static void Tip(string tip)
        {
            if (!ImGui.IsItemHovered())
                return;
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(tip);
            ImGui.EndTooltip();
        }

        static void SellerCell(CrewOfferView offer)
        {
            string name = offer.SellerColonyName;
            string shown = ResearchWindow.FitText(name, ImGui.GetContentRegionAvail().X, t => ImGui.CalcTextSize(t).X);
            ImGui.TextUnformatted(shown);
            string? faction = FactionTip(offer);
            if (!ImGui.IsItemHovered())
                return;
            if (shown == name && faction == null)
                return;
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(name);
            if (faction != null)
                ImGui.TextUnformatted(faction);
            ImGui.EndTooltip();
        }

        static void NameCell(string name)
        {
            string shown = ResearchWindow.FitText(name, ImGui.GetContentRegionAvail().X, t => ImGui.CalcTextSize(t).X);
            ImGui.TextUnformatted(shown);
            if (ImGui.IsItemHovered() && shown != name)
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(name);
                ImGui.EndTooltip();
            }
        }
    }
}
