using System;
using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Buy and ship from a friendly market. The goods list and the chart live in <see cref="MarketTabDisplay"/>.
    /// </summary>
    public static class MarketBarsDisplay
    {
        sealed class ShipDraft
        {
            public int Good;
            public int Units = 1;
            public int Buyer;
            public double Fee;
        }

        static readonly Dictionary<int, ShipDraft> _ship = new();

        /// <summary>
        /// Height to leave under the Market tab so Buy and ship sits on-screen when it will draw. Zero if it will not.
        /// Slightly generous so a 1px shortfall does not still show a scrollbar.
        /// </summary>
        public static float BottomReserve(MarketView? market, IClientSystem? system)
        {
            if (!TryBuyAndShipOptions(market, system, out _, out _))
                return 0f;

            return ImGui.GetStyle().ItemSpacing.Y * 2f
                + ImGui.GetTextLineHeightWithSpacing()
                + ImGui.GetFrameHeightWithSpacing() * 5f
                + 8f;
        }

        /// <summary>
        /// Buy and ship from a friendly market: the goods are sealed at the seller and paid for now, and a haul is
        /// posted on one of this faction's colonies in the same system. A ship claims the haul to carry it.
        /// </summary>
        public static void DisplayBuyAndShip(EntitySnapshot seller, MarketView? market, IClientSystem? system, GlobalUIState uiState)
        {
            if (!TryBuyAndShipOptions(market, system, out var goods, out var buyers))
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

        static bool TryBuyAndShipOptions(MarketView? market, IClientSystem? system, out List<MarketGoodView> goods, out List<EntitySnapshot> buyers)
        {
            goods = new List<MarketGoodView>();
            buyers = new List<EntitySnapshot>();
            if (market == null || market.CanEdit || !market.ExchangeOpen || system == null)
                return false;

            foreach (var good in market.Goods)
            {
                if (good.SellQuantity > 0 && !good.Relocation)
                    goods.Add(good);
            }
            foreach (var colony in system.Entities)
            {
                if (colony.Relation == OwnerRelation.Owned && colony.Kind == BodyKind.Colony
                    && colony.GetView<MarketView>() != null)
                    buyers.Add(colony);
            }
            return goods.Count > 0 && buyers.Count > 0;
        }
    }
}
