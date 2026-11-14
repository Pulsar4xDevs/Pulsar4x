using System;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Client formatters and same-body buy labels for multi-faction Earth phase 3.
    /// No ImGui and no game.
    /// </summary>
    public class MultiFactionEarthPhase3UiTests
    {
        static MineralMiningRow Row(long yours, long body, double? years, bool canMine = true) =>
            new MineralMiningRow(1, "Iron Ore", "An ore.", 0, 10_000, 1.0, yours, canMine)
            {
                BodyAnnualProduction = body,
                YearsToDepletion = years,
            };

        [Test]
        public void AnnualProduction_ShowsYoursNextToTheBodyTotal()
        {
            Assert.That(ColonyMiningDisplay.FormatAnnualProduction(Row(1000, 4000, 12.5)), Is.EqualTo("1,000 / 4,000"));
            Assert.That(ColonyMiningDisplay.FormatAnnualProduction(Row(4000, 4000, 2)), Is.EqualTo("4,000 / 4,000"));
            Assert.That(ColonyMiningDisplay.FormatAnnualProduction(Row(0, 2500, 8, canMine: false)), Is.EqualTo("0 / 2,500"));
            Assert.That(ColonyMiningDisplay.FormatAnnualProduction(Row(0, 0, null, canMine: false)), Is.EqualTo("-"));
        }

        [Test]
        public void YearsToDepletion_UsesTheProjectorValue_OrADashWhenNull()
        {
            Assert.That(ColonyMiningDisplay.FormatYearsToDepletion(null), Is.EqualTo("-"));
            Assert.That(ColonyMiningDisplay.FormatYearsToDepletion(12.54), Is.EqualTo("12.5"));
            Assert.That(ColonyMiningDisplay.FormatYearsToDepletion(0), Is.EqualTo("0.000"));
            Assert.That(ColonyMiningDisplay.FormatYearsToDepletion(0.00827), Is.EqualTo("0.008"));
            Assert.That(ColonyMiningDisplay.FormatYearsToDepletion(1), Is.EqualTo("1.0"));
        }

        [Test]
        public void SameBodyBuy_IsDeliveredHere_WithNoFreightFee()
        {
            var market = new MarketView(0, Array.Empty<MarketGoodView>())
            {
                SameBodyColonyIds = new[] { 10, 11 },
            };
            Assert.That(MarketBarsDisplay.DeliversHere(market, 10), Is.True);
            Assert.That(MarketBarsDisplay.DeliversHere(market, 11), Is.True);
            Assert.That(MarketBarsDisplay.DeliversHere(market, 99), Is.False);
            Assert.That(MarketBarsDisplay.DeliversHere(null, 10), Is.False);
            Assert.That(MarketBarsDisplay.BuyActionLabel(true), Is.EqualTo("Buy, delivered here"));
            Assert.That(MarketBarsDisplay.BuyActionLabel(false), Is.EqualTo("Buy and ship"));
        }
    }
}
