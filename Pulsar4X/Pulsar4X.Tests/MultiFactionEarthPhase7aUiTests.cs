using System;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Client helpers for the phase 7a UI: entity-card height clamp, consumption formatting,
    /// market flash fade, and production have/need text. No ImGui and no game.
    /// </summary>
    public class MultiFactionEarthPhase7aUiTests
    {
        [Test]
        public void EntityCardHeight_DoesNotThrowWhenWorkAreaIsMissingOrTiny()
        {
            Assert.That(EntityCard.MaxLaidOutHeight(48f, 0f), Is.EqualTo(48f));
            Assert.That(EntityCard.MaxLaidOutHeight(48f, 0f), Is.GreaterThanOrEqualTo(48f));
            Assert.That(EntityCard.ClampLaidOutHeight(120f, 48f, 0f), Is.EqualTo(48f), "WorkSize.Y 0 used to make max = -8");
            Assert.That(EntityCard.ClampLaidOutHeight(120f, 48f, 40f), Is.EqualTo(48f), "work area shorter than the min still clamps to min");
            Assert.That(EntityCard.ClampLaidOutHeight(20f, 48f, 800f), Is.EqualTo(48f));
            Assert.That(EntityCard.ClampLaidOutHeight(200f, 48f, 800f), Is.EqualTo(200f));
            Assert.That(EntityCard.ClampLaidOutHeight(900f, 48f, 800f), Is.EqualTo(792f));
            Assert.DoesNotThrow(() => EntityCard.ClampLaidOutHeight(48f, 48f, 0f));
        }

        [Test]
        public void Consumption_FormatsGoods_AndShortageDate()
        {
            var earth = new ColonyNeedsView { FoodPerDay = 0, WaterPerDay = 0, FoodStock = 0, WaterStock = 100, ShortageState = "Supplied" };
            var goods = ColonyWorkforceDisplay.Goods(earth);
            Assert.That(goods.Length, Is.EqualTo(5));
            Assert.That(goods[0].Name, Is.EqualTo("Food"));
            Assert.That(goods[1].Name, Is.EqualTo("Water"));
            Assert.That(goods[2].Name, Is.EqualTo("Consumer Goods"));
            Assert.That(goods[3].Name, Is.EqualTo("Medical Supplies"));
            Assert.That(goods[4].Name, Is.EqualTo("Luxury Goods"));
            Assert.That(ColonyWorkforceDisplay.FormatDaysOfStock(null), Is.EqualTo("—"));
            Assert.That(ColonyWorkforceDisplay.FormatDaysOfStock(12.34), Is.EqualTo("12.3 days"));
            Assert.That(ColonyWorkforceDisplay.FormatPerDay(0), Is.EqualTo("0 a day"));
            Assert.That(ColonyWorkforceDisplay.FormatStock(12345), Is.EqualTo("12,345"));
            Assert.That(ColonyWorkforceDisplay.ShortageText(earth, new DateTime(2050, 2, 1)), Is.EqualTo("Supplied"));

            var luna = new ColonyNeedsView
            {
                FoodPerDay = 2, WaterPerDay = 4, FoodStock = 360, WaterStock = 720,
                FoodDaysOfStock = 180, WaterDaysOfStock = 180,
                ShortageState = "Declining", ShortageDay = 40, FoodShort = true, WaterShort = false,
                DeathsTotal = 0, LeftTotal = 12, Satisfaction = 48, SatisfactionTarget = 60,
                GrowthFactor = 0, OutputFactor = 0.9,
            };
            Assert.That(ColonyWorkforceDisplay.ShortageStartedOn(new DateTime(2050, 3, 15), 40), Is.EqualTo(new DateTime(2050, 2, 3)));
            Assert.That(ColonyWorkforceDisplay.ShortageText(luna, new DateTime(2050, 3, 15)), Is.EqualTo("Declining since 3 Feb 2050"));
            Assert.That(ColonyWorkforceDisplay.SatisfactionText(luna), Is.EqualTo("48 / 60"));
            Assert.That(ColonyWorkforceDisplay.FactorText(luna.OutputFactor), Is.EqualTo("0.9×"));
            Assert.That(ColonyWorkforceDisplay.Goods(luna)[0].Short, Is.True);
            Assert.That(ColonyWorkforceDisplay.Goods(luna)[1].Short, Is.False);
        }

        [Test]
        public void MarketFlash_FadesOverOneAndAHalfSeconds()
        {
            Assert.That(MarketTabDisplay.FlashAmount(1.0, 1.0), Is.EqualTo(1f).Within(1e-6));
            Assert.That(MarketTabDisplay.FlashAmount(1.75, 1.0), Is.EqualTo(0.5f).Within(1e-6));
            Assert.That(MarketTabDisplay.FlashAmount(2.5, 1.0), Is.EqualTo(0f));
            Assert.That(MarketTabDisplay.FlashAmount(0.5, 1.0), Is.EqualTo(0f));
        }

        [Test]
        public void ProductionHaveNeed_AndStockOnHand()
        {
            Assert.That(ColonyProductionDisplay.HaveNeedText(5, 12), Is.EqualTo("Have 5, need 12"));
            Assert.That(ColonyProductionDisplay.HaveNeedText(0, 1000), Is.EqualTo("Have 0, need 1,000"));
            var storage = new CargoStorageView(0, 0, 0)
            {
                Stores = new[]
                {
                    new CargoTypeStoreView("bulk", "Bulk", 1, 1)
                    {
                        Items = new[]
                        {
                            new CargoItemView(1, "Iron Ore", "mineral", "", 40, 0, 0, 0, 0, 0, 0, false),
                            new CargoItemView(2, "Steel", "material", "", 7, 0, 0, 0, 0, 0, 0, false),
                        },
                    },
                },
            };
            Assert.That(ColonyProductionDisplay.StockOnHand(storage, "Iron Ore"), Is.EqualTo(40));
            Assert.That(ColonyProductionDisplay.StockOnHand(storage, "Water"), Is.EqualTo(0));
            Assert.That(ColonyProductionDisplay.StockOnHand(null, "Iron Ore"), Is.EqualTo(0));
        }
    }
}
