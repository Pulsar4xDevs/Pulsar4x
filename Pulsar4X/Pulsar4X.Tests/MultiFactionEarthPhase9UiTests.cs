using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Client helpers for phase 9: appraisal formatting (including unpriced cases), the Charter ledger label,
    /// and the folded-company name fallback. No ImGui and no game.
    /// </summary>
    public class MultiFactionEarthPhase9UiTests
    {
        static AppraisalLine Line(string name, long qty, decimal? unit, decimal? cost, bool fromMarket, bool recursed)
            => new AppraisalLine("id-" + name, name, qty, unit, cost, fromMarket, recursed);

        static GoodAppraisal Appraisal(
            decimal? unit = 0.18m, decimal? total = 0.36m, decimal? inputs = 0.25m,
            decimal? energy = 0.06m, decimal? labour = 0.05m, string? reason = null,
            AppraisalLine[]? lines = null, string[]? missing = null)
            => new GoodAppraisal("plastic", "Plastic", 2, unit, total, inputs, energy, labour,
                lines ?? System.Array.Empty<AppraisalLine>(),
                missing ?? System.Array.Empty<string>(),
                System.Array.Empty<string>(), reason);

        [Test]
        public void Money_AndUnitCostVsAsk_HandleNullsAndTheAskDelta()
        {
            Assert.That(MarketAppraisalDisplay.Money(null), Is.EqualTo("—"));
            Assert.That(MarketAppraisalDisplay.Money(0.25m), Is.EqualTo("0.25 cr"));
            Assert.That(MarketAppraisalDisplay.UnitCostVsAsk(null, null), Is.EqualTo("Unpriced"));
            Assert.That(MarketAppraisalDisplay.UnitCostVsAsk(null, 0.5m), Is.EqualTo("Unpriced  (ask 0.5 cr)"));
            Assert.That(MarketAppraisalDisplay.UnitCostVsAsk(0.18m, null), Is.EqualTo("0.18 cr"));
            Assert.That(MarketAppraisalDisplay.UnitCostVsAsk(0.18m, 0.18m), Is.EqualTo("0.18 cr  (same as the ask)"));
            Assert.That(MarketAppraisalDisplay.UnitCostVsAsk(0.18m, 0.25m),
                Is.EqualTo("0.18 cr  (0.07 cr below the ask of 0.25 cr)"));
            Assert.That(MarketAppraisalDisplay.UnitCostVsAsk(0.3m, 0.25m),
                Is.EqualTo("0.3 cr  (0.05 cr above the ask of 0.25 cr)"));
        }

        [Test]
        public void LineMarker_DistinguishesSeedAsk_Market_AndRecursed()
        {
            Assert.That(MarketAppraisalDisplay.LineMarker(Line("Hydrocarbons", 1, 0.25m, 0.25m, fromMarket: true, recursed: false)),
                Is.EqualTo("market"));
            Assert.That(MarketAppraisalDisplay.LineMarker(Line("Iron Ore", 2, 0.1m, 0.2m, fromMarket: false, recursed: false)),
                Is.EqualTo("seed ask"));
            Assert.That(MarketAppraisalDisplay.LineMarker(Line("Steel", 1, 1.5m, 1.5m, fromMarket: false, recursed: true)),
                Is.EqualTo("seed ask, recursed"));
            Assert.That(MarketAppraisalDisplay.LineMarker(Line("Steel", 1, 1.5m, 1.5m, fromMarket: true, recursed: true)),
                Is.EqualTo("market, recursed"));
            Assert.That(MarketAppraisalDisplay.LineText(Line("Hydrocarbons", 1, 0.25m, 0.25m, true, false)),
                Is.EqualTo("Hydrocarbons  1 × 0.25 cr = 0.25 cr  (market)"));
            Assert.That(MarketAppraisalDisplay.LineText(Line("Iron Ore", 2, null, null, false, false)),
                Is.EqualTo("Iron Ore  2 × — = —  (seed ask)"));
        }

        [Test]
        public void UnpricedAppraisal_ShowsReasonAndMissingPrices()
        {
            var none = GoodAppraisal.None("mystery", "Mystery", 1, "No recipe for this good.");
            Assert.That(MarketAppraisalDisplay.IsPriced(none), Is.False);
            Assert.That(none.UnitCost, Is.Null);
            Assert.That(none.Reason, Is.EqualTo("No recipe for this good."));
            Assert.That(MarketAppraisalDisplay.MissingText(none), Is.EqualTo(""));

            var missing = Appraisal(unit: null, total: null, reason: "Some inputs have no price.",
                missing: new[] { "chromium", "nickel" });
            Assert.That(MarketAppraisalDisplay.IsPriced(missing), Is.False);
            Assert.That(MarketAppraisalDisplay.MissingText(missing), Is.EqualTo("chromium, nickel"));
            Assert.That(MarketAppraisalDisplay.UnitCostVsAsk(missing.UnitCost, 1m), Is.EqualTo("Unpriced  (ask 1 cr)"));
        }

        [Test]
        public void LedgerLabel_ForCharter()
        {
            Assert.That(LedgerWindow.CategoryLabel("Charter"), Is.EqualTo("Charter"));
        }

        [Test]
        public void FactionName_FallsBackToFoldedCompany_WhenTheStanceRowIsGone()
        {
            Assert.That(ForeignEntities.FactionName(System.Array.Empty<FactionStanceRow>(), 42),
                Is.EqualTo("Folded company"));
            var stances = new[] { new FactionStanceRow(7, "Ceres Depot Authority", FactionStance.Friendly) };
            Assert.That(ForeignEntities.FactionName(stances, 7), Is.EqualTo("Ceres Depot Authority"));
            Assert.That(ForeignEntities.FactionName(stances, 99), Is.EqualTo("Folded company"));
        }
    }
}
