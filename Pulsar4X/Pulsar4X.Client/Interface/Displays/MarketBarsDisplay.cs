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

            ImGui.Text($"Listings {market.Goods.Count} / {market.Capacity}");
            if (!market.CanEdit && !market.ExchangeOpen)
                ImGui.Text("Trade is not mutual.");
            if (market.Goods.Count == 0)
            {
                ImGui.Text("No listings.");
            }
            else
            {
            const ImGuiTableFlags flags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp;
            if (ImGui.BeginTable("##market-bars", 4, flags))
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
                if (good.SellQuantity > 0)
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
            bool idle = !ImGui.IsAnyItemActive();
            foreach (var good in market.Goods)
            {
                var edit = Row(entityId, good, idle);
                ImGui.PushID(good.CargoId);
                ImGui.Separator();
                ImGui.Text(good.Name);
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
                if (ImGui.Button("Set"))
                    SubmitSet(entityId, good.CargoId, edit, uiState);
                ImGui.SameLine();
                if (ImGui.Button("Remove"))
                    uiState.GameClient?.SubmitCommandAsync(new ClearMarketListingCommand(entityId, good.CargoId));
                ImGui.PopID();
            }

            if (market.Addable.Count == 0)
                return;

            var draft = AddDraft(entityId);
            int index = AddIndex(entityId, market.Addable.Count);
            var selected = market.Addable[index];
            ImGui.Separator();
            ImGui.Text("Add a good");
            if (ImGui.BeginCombo("Good", selected.Name))
            {
                for (int i = 0; i < market.Addable.Count; i++)
                {
                    var choice = market.Addable[i];
                    if (ImGui.Selectable(choice.Name + "###" + choice.CargoId, i == index))
                        index = i;
                }
                ImGui.EndCombo();
            }
            _addIndex[entityId] = index;
            selected = market.Addable[index];

            ImGui.PushID("add-listing");
            ImGui.SetNextItemWidth(140f);
            ImGui.InputInt("Sell", ref draft.Sell);
            ImGui.SetNextItemWidth(140f);
            ImGui.InputDouble("Ask", ref draft.Ask, 0, 0, "%.2f");
            ImGui.SetNextItemWidth(140f);
            ImGui.InputInt("Buy", ref draft.Buy);
            ImGui.SetNextItemWidth(140f);
            ImGui.InputDouble("Bid", ref draft.Bid, 0, 0, "%.2f");
            ImGui.SetNextItemWidth(140f);
            ImGui.InputInt("Reserve", ref draft.Reserve);
            if (ImGui.Button("Add"))
            {
                SubmitSet(entityId, selected.CargoId, draft, uiState);
                draft.Sell = 0;
                draft.Buy = 0;
                draft.Reserve = 0;
                draft.Ask = 0;
                draft.Bid = 0;
            }
            ImGui.PopID();
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

        static int AddIndex(int entityId, int count)
        {
            if (!_addIndex.TryGetValue(entityId, out int index) || index < 0 || index >= count)
                index = 0;
            return index;
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
        static readonly Dictionary<int, int> _addIndex = new();

        static void DrawBar(long value, long scale, Vector4 color, string overlay, float width)
        {
            float fraction = value <= 0 || scale <= 0 ? 0f : (float)Math.Min(1d, (double)value / scale);
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, color);
            ImGui.ProgressBar(fraction, new Vector2(width, 0f), overlay);
            ImGui.PopStyleColor();
        }
    }
}
