using System.Collections.Generic;
using System.Globalization;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Relocation listings and haul rows for one office. The commands are the ones the market and intel lists used.
    /// </summary>
    public static class ShippingDisplay
    {
        public static void Display(
            EntitySnapshot office, MarketView? market, IntelBookView? book, IClientSystem? system, GlobalUIState uiState)
        {
            var relocations = new List<MarketGoodView>();
            if (market != null)
            {
                foreach (var good in market.Goods)
                {
                    if (good.Relocation)
                        relocations.Add(good);
                }
            }

            var hauls = new List<IntelRowView>();
            if (book != null)
            {
                foreach (var row in book.Rows)
                {
                    if (row.Kind == IntelKind.Haul)
                        hauls.Add(row);
                }
            }

            if (relocations.Count == 0 && hauls.Count == 0)
            {
                ImGui.Text("No relocations or hauls at this office.");
                return;
            }

            if (relocations.Count > 0)
            {
                ImGui.TextDisabled("Relocations");
                foreach (var good in relocations)
                {
                    ImGui.PushID("reloc-" + good.CargoId);
                    ImGui.Text(good.Name);
                    if (!string.IsNullOrEmpty(good.Note))
                        ImGui.TextWrapped(good.Note);
                    ImGui.TextWrapped("Stock " + good.Stock.ToString("N0", CultureInfo.InvariantCulture)
                        + "    Sell " + good.SellQuantity.ToString("N0", CultureInfo.InvariantCulture)
                        + "    Buy " + good.BuyQuantity.ToString("N0", CultureInfo.InvariantCulture)
                        + "    Reserve " + good.Reserve.ToString("N0", CultureInfo.InvariantCulture));
                    ImGui.TextWrapped("This follows the population. It does not use an office slot.");
                    if (market != null && market.CanEdit && ImGui.Button("Remove"))
                        uiState.GameClient?.SubmitCommandAsync(new ClearMarketListingCommand(office.Id, good.CargoId));
                    ImGui.PopID();
                    ImGui.Spacing();
                }
            }

            if (hauls.Count == 0)
                return;
            if (relocations.Count > 0)
                ImGui.Separator();
            ImGui.TextDisabled("Hauls");
            bool canEdit = book != null && book.CanEdit;
            foreach (var row in hauls)
            {
                ImGui.PushID(office.Id + ":" + row.Subject);
                ImGui.TextWrapped(row.Name);
                string from = string.IsNullOrEmpty(row.SourceName) ? "—" : row.SourceName;
                string to = string.IsNullOrEmpty(row.DestName) ? "—" : row.DestName;
                ImGui.TextWrapped("Fee " + row.Ask.ToString(CultureInfo.InvariantCulture)
                    + "    From " + from
                    + "    To " + to);
                ImGui.TextWrapped("Ship " + ClaimantName(system, row));
                IntelDisplay.HaulButtons(office.Id, row, canEdit, uiState);
                ImGui.PopID();
                ImGui.Spacing();
            }
        }

        static string ClaimantName(IClientSystem? system, IntelRowView row)
        {
            if (row.ClaimantId < 0)
                return "—";
            var ship = system?.GetEntity(row.ClaimantId);
            string? name = ship?.GetView<NameView>()?.Name;
            if (string.IsNullOrEmpty(name))
                return "Claimed";
            return name;
        }
    }
}
