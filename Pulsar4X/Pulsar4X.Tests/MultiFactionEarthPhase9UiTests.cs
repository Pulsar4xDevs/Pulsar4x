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
            Assert.That(MarketAppraisalDisplay.Money(0m), Is.EqualTo("0 cr"));
            Assert.That(MarketAppraisalDisplay.Money(0.000625m), Is.EqualTo("0.000625 cr"));
            Assert.That(MarketAppraisalDisplay.Money(0.01m), Is.EqualTo("0.01 cr"));
            Assert.That(MarketAppraisalDisplay.UnitCostVsAsk(0.000625m, 0.25m),
                Is.EqualTo("0.000625 cr  (0.249375 cr below the ask of 0.25 cr)"));
        }

        [Test]
        public void ScaleCost_AndScaledMoney_ScaleLinearlyAndKeepFormatting()
        {
            Assert.That(MarketAppraisalDisplay.ScaleCost(null, 100), Is.Null);
            Assert.That(MarketAppraisalDisplay.ScaleCost(0m, 1000), Is.EqualTo(0m));
            Assert.That(MarketAppraisalDisplay.ScaleCost(0.75m, 1), Is.EqualTo(0.75m));
            Assert.That(MarketAppraisalDisplay.ScaleCost(0.75m, 100), Is.EqualTo(75m));
            Assert.That(MarketAppraisalDisplay.ScaleCost(0.75m, 1000), Is.EqualTo(750m));
            Assert.That(MarketAppraisalDisplay.ScaleCost(0.000625m, 100), Is.EqualTo(0.0625m));
            Assert.That(MarketAppraisalDisplay.ScaleCost(0.000625m, 1000), Is.EqualTo(0.625m));

            Assert.That(MarketAppraisalDisplay.ScaledMoney(null, 100), Is.EqualTo("—"));
            Assert.That(MarketAppraisalDisplay.ScaledMoney(0m, 1000), Is.EqualTo("0 cr"));
            Assert.That(MarketAppraisalDisplay.ScaledMoney(0.75m, 1), Is.EqualTo("0.75 cr"));
            Assert.That(MarketAppraisalDisplay.ScaledMoney(0.75m, 100), Is.EqualTo("75 cr"));
            Assert.That(MarketAppraisalDisplay.ScaledMoney(0.75m, 1000), Is.EqualTo("750 cr"));
            Assert.That(MarketAppraisalDisplay.ScaledMoney(0.000625m, 1), Is.EqualTo("0.000625 cr"));
            Assert.That(MarketAppraisalDisplay.ScaledMoney(0.000625m, 100), Is.EqualTo("0.0625 cr"));
            Assert.That(MarketAppraisalDisplay.ScaledMoney(0.000625m, 1000), Is.EqualTo("0.625 cr"));

            Assert.That(MarketAppraisalDisplay.ScaledQuantity(0.5, 1), Is.EqualTo("0.5"));
            Assert.That(MarketAppraisalDisplay.ScaledQuantity(0.5, 100), Is.EqualTo("50"));
            Assert.That(MarketAppraisalDisplay.ScaledQuantity(0.5, 1000), Is.EqualTo("500"));
            Assert.That(MarketAppraisalDisplay.ScaledQuantity(0, 1000), Is.EqualTo("0"));
            Assert.That(MarketAppraisalDisplay.ScaledInputCell(0.5, 0.75m, 1), Is.EqualTo("0.5  ·  0.75 cr"));
            Assert.That(MarketAppraisalDisplay.ScaledInputCell(0.5, 0.75m, 100), Is.EqualTo("50  ·  75 cr"));
            Assert.That(MarketAppraisalDisplay.ScaledInputCell(0.5, null, 1000), Is.EqualTo("500  ·  —"));

            Assert.That(MarketAppraisalDisplay.ColumnHeader(1), Is.EqualTo("Per 1"));
            Assert.That(MarketAppraisalDisplay.ColumnHeader(100), Is.EqualTo("Per 100"));
            Assert.That(MarketAppraisalDisplay.ColumnHeader(1000), Is.EqualTo("Per 1,000"));
        }

        [Test]
        public void Quantity_ShowsFractions_WholeNumbers_AndZero()
        {
            Assert.That(MarketAppraisalDisplay.Quantity(0), Is.EqualTo("0"));
            Assert.That(MarketAppraisalDisplay.Quantity(0d), Is.EqualTo("0"));
            Assert.That(MarketAppraisalDisplay.Quantity(1), Is.EqualTo("1"));
            Assert.That(MarketAppraisalDisplay.Quantity(2), Is.EqualTo("2"));
            Assert.That(MarketAppraisalDisplay.Quantity(0.5), Is.EqualTo("0.5"));
            Assert.That(MarketAppraisalDisplay.Quantity(0.5d), Is.EqualTo("0.5"));
            Assert.That(MarketAppraisalDisplay.Quantity((double)0.5m), Is.EqualTo("0.5"));
            Assert.That(MarketAppraisalDisplay.Quantity(1.25), Is.EqualTo("1.25"));
            Assert.That(MarketAppraisalDisplay.Quantity(0.000625), Is.EqualTo("0.000625"));
            // A current integer line quantity still reads as a whole number.
            Assert.That(MarketAppraisalDisplay.Quantity(1L), Is.EqualTo("1"));
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

        static ConstructibleItemView Constructible(string id, string name, bool ship = false, bool material = false)
            => new ConstructibleItemView(id, name, 10, 1, false)
            {
                IsShipDesign = ship,
                IsMaterial = material,
                ComponentType = ship ? "Ship" : material ? "Material" : "Factory",
            };

        [Test]
        public void TryResolve_ComponentId_ReturnsPanelNameAndId()
        {
            var designs = new ComponentDesignsSnapshot
            {
                Designs = new[]
                {
                    new ComponentDesignSummary("uid-factory", "Prefab Factory", "tmpl-factory", "Factory"),
                    new ComponentDesignSummary("uid-mine", "Mine", "tmpl-mine", "Mine"),
                }
            };
            var constructibles = new[]
            {
                Constructible("uid-factory", "Prefab Factory"),
                Constructible("uid-steel", "Steel", material: true),
                Constructible("uid-hull", "Starship", ship: true),
            };

            Assert.That(MarketAppraisalDisplay.TryResolve("uid-factory", designs, constructibles, out var id, out var name), Is.True);
            Assert.That(id, Is.EqualTo("uid-factory"));
            Assert.That(name, Is.EqualTo("Prefab Factory"));

            Assert.That(MarketAppraisalDisplay.TryResolve("uid-factory", designs, null, out id, out name), Is.True);
            Assert.That(id, Is.EqualTo("uid-factory"));
            Assert.That(name, Is.EqualTo("Prefab Factory"));

            Assert.That(MarketAppraisalDisplay.TryResolve("uid-steel", null, constructibles, out id, out name), Is.True);
            Assert.That(id, Is.EqualTo("uid-steel"));
            Assert.That(name, Is.EqualTo("Steel"));

            Assert.That(MarketAppraisalDisplay.TryResolve("missing", designs, constructibles, out id, out name), Is.False);
            Assert.That(id, Is.EqualTo(""));
            Assert.That(name, Is.EqualTo(""));
            Assert.That(MarketAppraisalDisplay.TryResolve("", designs, constructibles, out _, out _), Is.False);
        }

    }
}
