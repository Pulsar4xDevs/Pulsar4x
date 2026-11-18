using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Client helpers for the phase 8 crew market panel: ask clamp, offer enablement, buy reasons, and labels.
    /// No ImGui and no game.
    /// </summary>
    public class MultiFactionEarthPhase8UiTests
    {
        const decimal Tuition = 9000m;
        const decimal DefaultFee = 13500m;

        static CrewOfferView Offer(long available = 150, decimal fee = DefaultFee, string reason = "")
            => new CrewOfferView(20, "ECN Earth", 2, "European Continental Network",
                1, "Human", available, fee, reason);

        [Test]
        public void ClampAsk_NeverGoesBelowTheDefaultFee()
        {
            Assert.That(ColonyCrewMarketDisplay.ClampAsk(0, DefaultFee), Is.EqualTo(DefaultFee));
            Assert.That(ColonyCrewMarketDisplay.ClampAsk(DefaultFee - 1, DefaultFee), Is.EqualTo(DefaultFee));
            Assert.That(ColonyCrewMarketDisplay.ClampAsk(DefaultFee, DefaultFee), Is.EqualTo(DefaultFee));
            Assert.That(ColonyCrewMarketDisplay.ClampAsk(DefaultFee * 2, DefaultFee), Is.EqualTo(DefaultFee * 2));
            Assert.That(DefaultFee, Is.EqualTo(Tuition * 1.5m));
        }

        [Test]
        public void OfferEnabled_WhenReasonIsEmpty()
        {
            Assert.That(ColonyCrewMarketDisplay.OfferEnabled(Offer()), Is.True);
            Assert.That(ColonyCrewMarketDisplay.OfferEnabled(Offer(reason: "")), Is.True);
            Assert.That(ColonyCrewMarketDisplay.OfferEnabled(Offer(reason: "Trade must be granted both ways.")), Is.False);
        }

        [Test]
        public void BuyReason_UsesOfferReason_Count_AndFunds()
        {
            var offer = Offer(available: 150, fee: DefaultFee);
            Assert.That(ColonyCrewMarketDisplay.BuyReason(offer, 20, 1_000_000m), Is.Null);
            Assert.That(ColonyCrewMarketDisplay.BuyReason(offer, 0, 1_000_000m),
                Is.EqualTo("Enter a count of at least 1."));
            Assert.That(ColonyCrewMarketDisplay.BuyReason(offer, 200, 1_000_000m),
                Is.EqualTo("Only 150 are on offer."));
            Assert.That(ColonyCrewMarketDisplay.BuyReason(offer, 20, 100_000m),
                Is.EqualTo("Need 270,000 cr (13,500 cr each)."));

            var blocked = Offer(reason: "Trade must be granted both ways.");
            Assert.That(ColonyCrewMarketDisplay.BuyReason(blocked, 1, 1_000_000m),
                Is.EqualTo("Trade must be granted both ways."));
        }

        [Test]
        public void Credits_Share_AndSellerPayout_FormatTheFeeRules()
        {
            Assert.That(ColonyCrewMarketDisplay.Credits(DefaultFee), Is.EqualTo("13,500 cr"));
            Assert.That(ColonyCrewMarketDisplay.ShareText(0.6m), Is.EqualTo("60%"));
            Assert.That(ColonyCrewMarketDisplay.SellerPayout(DefaultFee, 0.6m), Is.EqualTo("8,100 cr"));
            Assert.That(ColonyCrewMarketDisplay.Credits(Tuition), Is.EqualTo("9,000 cr"));
        }

        [Test]
        public void LedgerLabel_ForCrewTransfer()
        {
            Assert.That(LedgerWindow.CategoryLabel("CrewTransfer"), Is.EqualTo("Crew transfer"));
        }
    }
}
