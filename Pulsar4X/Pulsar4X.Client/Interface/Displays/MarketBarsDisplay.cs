using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Read-only bars for one logistics office: stock, buy quantity, and sell quantity on one scale.
    /// </summary>
    public static class MarketBarsDisplay
    {
        static readonly Vector4 StockColor = new(0.35f, 0.55f, 0.85f, 0.9f);
        static readonly Vector4 BuyColor = new(0.85f, 0.65f, 0.25f, 0.9f);
        static readonly Vector4 SellColor = new(0.35f, 0.75f, 0.40f, 0.9f);

        public static void Display(EntitySnapshot entity, MarketView? market, GlobalUIState uiState)
        {
            if (market == null)
            {
                ImGui.Text("This colony has no logistics office.");
                return;
            }

            ImGui.Text($"Listings {OfficeSlots(market)} / {market.Capacity}");
            if (!market.CanEdit && !market.ExchangeOpen)
                ImGui.Text("Trade is not mutual.");
            if (market.Goods.Count == 0)
            {
                ImGui.Text("No listings.");
            }
            else if (ImGui.BeginTable("##market-bars", 4, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Good", ImGuiTableColumnFlags.WidthFixed, 140f);
                ImGui.TableSetupColumn("Stock");
                ImGui.TableSetupColumn("Buy");
                ImGui.TableSetupColumn("Sell");
                ImGui.TableHeadersRow();

                foreach (var good in market.Goods)
                {
                    long scale = Math.Max(good.Stock, Math.Max(good.Reserve, Math.Max(good.BuyQuantity, Math.Max(good.SellQuantity, 1L))));

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(good.Name);
                    if (good.Relocation && !string.IsNullOrEmpty(good.Note))
                        ImGui.TextWrapped(good.Note);
                    else
                        ImGui.Text($"bid {good.Bid}   ask {good.Ask}");

                    ImGui.TableNextColumn();
                    string reserve = $"reserve {good.Reserve}";
                    float reserveWidth = ImGui.CalcTextSize(reserve).X + ImGui.GetStyle().ItemSpacing.X;
                    float stockWidth = Math.Max(24f, ImGui.GetContentRegionAvail().X - reserveWidth);
                    DrawBar(good.Stock, scale, StockColor, good.Stock.ToString(), stockWidth);
                    ImGui.SameLine();
                    ImGui.Text(reserve);

                    ImGui.TableNextColumn();
                    DrawBar(good.BuyQuantity, scale, BuyColor, good.BuyQuantity.ToString(), -1f);

                    ImGui.TableNextColumn();
                    DrawBar(good.SellQuantity, scale, SellColor, good.SellQuantity.ToString(), -1f);
                }

                ImGui.EndTable();
            }

            if (market.CanEdit)
            {
                DisplayEditor(entity.Id, market, uiState);
                if (entity.Kind == BodyKind.Colony)
                    DisplaySupply(entity, uiState);
            }
        }

        sealed class ShipDraft
        {
            public int Good;
            public int Units = 1;
            public int Buyer;
            public double Fee;
        }

        static readonly Dictionary<int, ShipDraft> _ship = new();

        /// <summary>
        /// Buy and ship from a friendly market: the goods are sealed at the seller and paid for now, and a haul is
        /// posted on one of this faction's colonies in the same system. A ship claims the haul to carry it.
        /// </summary>
        public static void DisplayBuyAndShip(EntitySnapshot seller, MarketView? market, IClientSystem? system, GlobalUIState uiState)
        {
            if (market == null || market.CanEdit || !market.ExchangeOpen || system == null)
                return;

            var goods = new List<MarketGoodView>();
            foreach (var good in market.Goods)
            {
                if (good.SellQuantity > 0 && !good.Relocation)
                    goods.Add(good);
            }
            var buyers = new List<EntitySnapshot>();
            foreach (var colony in system.Entities)
            {
                if (colony.Relation == OwnerRelation.Owned && colony.Kind == BodyKind.Colony
                    && colony.GetView<MarketView>() != null)
                    buyers.Add(colony);
            }
            if (goods.Count == 0 || buyers.Count == 0)
                return;

            if (!_ship.TryGetValue(seller.Id, out var draft))
            {
                draft = new ShipDraft();
                _ship[seller.Id] = draft;
            }
            draft.Good = Math.Clamp(draft.Good, 0, goods.Count - 1);
            draft.Buyer = Math.Clamp(draft.Buyer, 0, buyers.Count - 1);

            ImGui.Separator();
            ImGui.Text("Buy and ship");
            ImGui.PushID("buy-and-ship-" + seller.Id);
            if (ImGui.BeginCombo("Good", goods[draft.Good].Name))
            {
                for (int i = 0; i < goods.Count; i++)
                {
                    if (ImGui.Selectable(goods[i].Name + "###" + goods[i].CargoId, i == draft.Good))
                        draft.Good = i;
                }
                ImGui.EndCombo();
            }
            string buyerName = buyers[draft.Buyer].GetView<NameView>()?.Name ?? "Colony";
            if (ImGui.BeginCombo("Deliver to", buyerName))
            {
                for (int i = 0; i < buyers.Count; i++)
                {
                    string name = buyers[i].GetView<NameView>()?.Name ?? "Colony";
                    if (ImGui.Selectable(name + "###" + buyers[i].Id, i == draft.Buyer))
                        draft.Buyer = i;
                }
                ImGui.EndCombo();
            }
            ImGui.SetNextItemWidth(140f);
            ImGui.InputInt("Units", ref draft.Units);
            ImGui.SetNextItemWidth(140f);
            ImGui.InputDouble("Freight fee", ref draft.Fee, 0, 0, "%.2f");
            if (draft.Units < 1)
                draft.Units = 1;
            if (draft.Fee < 0)
                draft.Fee = 0;
            if (ImGui.Button("Buy and ship"))
            {
                uiState.GameClient?.SubmitCommandAsync(new SealShipmentCommand(
                    buyers[draft.Buyer].Id,
                    seller.Id,
                    new[] { new SealLine(goods[draft.Good].CargoId, draft.Units) },
                    (decimal)draft.Fee));
            }
            ImGui.PopID();
        }

        static void DisplaySupply(EntitySnapshot entity, GlobalUIState uiState)
        {
            ImGui.Separator();
            string span = entity.GetView<ColonyView>()?.CommandSpan ?? "";
            if (!string.IsNullOrEmpty(span))
                ImGui.Text(span);

            if (ImGui.Button("Run markets"))
                uiState.GameClient?.SubmitCommandAsync(new SupplyLocalCommand(entity.Id, SupplyMode.Run));
            if (ImGui.Button("Balance"))
                uiState.GameClient?.SubmitCommandAsync(new SupplyLocalCommand(entity.Id, SupplyMode.Balance));
            if (ImGui.Button("Stockpile"))
                uiState.GameClient?.SubmitCommandAsync(new SupplyLocalCommand(entity.Id, SupplyMode.Stockpile));

            ImGui.TextWrapped("Stockpile raises this colony's reserve, and that reserve stays after the order is replaced.");

            if (entity.GetView<OrdersView>() is { } orders
                && orders.goal.Name is "Run markets" or "Balance" or "Stockpile")
            {
                ImGui.Text($"{orders.goal.Name}: {orders.goal.Status}");
                if (!string.IsNullOrEmpty(orders.goal.Message))
                    ImGui.TextWrapped(orders.goal.Message);
            }
        }

        static void DisplayEditor(int entityId, MarketView market, GlobalUIState uiState)
        {
            if (market.Goods.Count == 0 && market.Addable.Count == 0)
                return;

            bool room = OfficeSlots(market) < market.Capacity;
            bool idle = !ImGui.IsAnyItemActive();
            string? selectedId = SelectedCargo(entityId, market);
            if (selectedId == null)
                return;

            var catalog = new List<(string Id, string Name)>(market.Goods.Count + market.Addable.Count);
            foreach (var good in market.Goods)
                catalog.Add((good.CargoId, good.Name));
            foreach (var choice in market.Addable)
                catalog.Add((choice.CargoId, choice.Name));
            catalog.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            ImGui.Separator();
            ImGui.PushID("market-edit-" + entityId);
            if (!_filter.TryGetValue(entityId, out string filter))
                filter = "";
            float listW = MathF.Min(320f, MathF.Max(160f, ImGui.GetContentRegionAvail().X));
            ImGui.SetNextItemWidth(listW);
            ImGui.InputTextWithHint("##good-search", "Search goods...", ref filter, 64);
            _filter[entityId] = filter;

            float listH = ImGui.GetTextLineHeightWithSpacing() * 8f;
            if (ImGui.BeginChild("##good-list", new Vector2(listW, listH), ImGuiChildFlags.Borders))
            {
                bool any = false;
                foreach (var entry in catalog)
                {
                    if (!NameMatches(entry.Name, filter))
                        continue;
                    any = true;
                    if (ImGui.Selectable(entry.Name + "###" + entry.Id, entry.Id == selectedId))
                        selectedId = entry.Id;
                }

                if (!any)
                    ImGui.TextDisabled("No match.");
            }
            ImGui.EndChild();
            _selected[entityId] = selectedId;

            MarketGoodView? listed = null;
            foreach (var good in market.Goods)
            {
                if (good.CargoId == selectedId)
                {
                    listed = good;
                    break;
                }
            }

            if (listed != null && listed.Relocation)
            {
                if (!string.IsNullOrEmpty(listed.Note))
                    ImGui.TextWrapped(listed.Note);
                ImGui.TextWrapped("This follows the population. It does not use an office slot.");
                if (ImGui.Button("Remove"))
                    uiState.GameClient?.SubmitCommandAsync(new ClearMarketListingCommand(entityId, listed.CargoId));
            }
            else if (listed != null)
            {
                var edit = Row(entityId, listed, idle);
                DrawFields(edit);
                if (ImGui.Button("Set"))
                    SubmitSet(entityId, listed.CargoId, edit, uiState);
                ImGui.SameLine();
                if (ImGui.Button("Remove"))
                    uiState.GameClient?.SubmitCommandAsync(new ClearMarketListingCommand(entityId, listed.CargoId));
            }
            else if (room)
            {
                var draft = AddDraft(entityId);
                DrawFields(draft);
                if (ImGui.Button("Add"))
                {
                    SubmitSet(entityId, selectedId, draft, uiState);
                    draft.Sell = 0;
                    draft.Buy = 0;
                    draft.Reserve = 0;
                    draft.Ask = 0;
                    draft.Bid = 0;
                }
            }
            else
            {
                ImGui.Text("This office is full.");
            }
            ImGui.PopID();
        }

        static void DrawFields(RowEdit edit)
        {
            ImGui.SetNextItemWidth(140f);
            ImGui.InputInt("Sell", ref edit.Sell);
            ImGui.SetNextItemWidth(140f);
            ImGui.InputDouble("Ask", ref edit.Ask, 0, 0, "%.2f");
            ImGui.SetNextItemWidth(140f);
            ImGui.InputInt("Buy", ref edit.Buy);
            ImGui.SetNextItemWidth(140f);
            ImGui.InputDouble("Bid", ref edit.Bid, 0, 0, "%.2f");
            ImGui.SetNextItemWidth(140f);
            ImGui.InputInt("Reserve", ref edit.Reserve);
        }

        /// <summary>Ordinary listings. A relocation follows the population and does not take a slot.</summary>
        static int OfficeSlots(MarketView market)
        {
            int count = 0;
            foreach (var good in market.Goods)
            {
                if (!good.Relocation)
                    count++;
            }
            return count;
        }

        static string? SelectedCargo(int entityId, MarketView market)
        {
            if (_selected.TryGetValue(entityId, out var id) && IsChoice(market, id))
                return id;
            if (market.Goods.Count > 0)
                return market.Goods[0].CargoId;
            if (market.Addable.Count > 0)
                return market.Addable[0].CargoId;
            return null;
        }

        static bool IsChoice(MarketView market, string cargoId)
        {
            foreach (var good in market.Goods)
            {
                if (good.CargoId == cargoId)
                    return true;
            }
            foreach (var choice in market.Addable)
            {
                if (choice.CargoId == cargoId)
                    return true;
            }
            return false;
        }

        static bool NameMatches(string name, string filter)
        {
            return string.IsNullOrEmpty(filter)
                || name.Contains(filter, StringComparison.OrdinalIgnoreCase);
        }

        static void SubmitSet(int entityId, string cargoId, RowEdit edit, GlobalUIState uiState)
        {
            uiState.GameClient?.SubmitCommandAsync(new SetMarketListingCommand(
                entityId,
                cargoId,
                edit.Sell,
                (decimal)edit.Ask,
                edit.Buy,
                (decimal)edit.Bid,
                edit.Reserve));
        }

        static RowEdit Row(int entityId, MarketGoodView good, bool idle)
        {
            var key = (entityId, good.CargoId);
            if (idle || !_rows.TryGetValue(key, out var edit))
            {
                edit = Seed(good);
                _rows[key] = edit;
            }
            return edit;
        }

        static RowEdit AddDraft(int entityId)
        {
            if (!_adds.TryGetValue(entityId, out var edit))
            {
                edit = new RowEdit();
                _adds[entityId] = edit;
            }
            return edit;
        }

        static RowEdit Seed(MarketGoodView good)
        {
            return new RowEdit
            {
                Sell = ClampToInt(good.SellQuantity),
                Buy = ClampToInt(good.BuyQuantity),
                Reserve = ClampToInt(good.Reserve),
                Ask = (double)good.Ask,
                Bid = (double)good.Bid,
            };
        }

        static int ClampToInt(long value)
        {
            if (value < 0)
                return 0;
            if (value > int.MaxValue)
                return int.MaxValue;
            return (int)value;
        }

        sealed class RowEdit
        {
            public int Sell;
            public int Buy;
            public int Reserve;
            public double Ask;
            public double Bid;
        }

        static readonly Dictionary<(int EntityId, string CargoId), RowEdit> _rows = new();
        static readonly Dictionary<int, RowEdit> _adds = new();
        static readonly Dictionary<int, string> _selected = new();
        static readonly Dictionary<int, string> _filter = new();

        static void DrawBar(long value, long scale, Vector4 color, string overlay, float width)
        {
            float fraction = value <= 0 || scale <= 0 ? 0f : (float)Math.Min(1d, (double)value / scale);
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, color);
            ImGui.ProgressBar(fraction, new Vector2(width, 0f), overlay);
            ImGui.PopStyleColor();
        }
    }
}
