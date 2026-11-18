using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Tests
{
    /// <summary>Client helpers for phase 7b: monthly-goods status, satisfaction parts, and crew-tab counts.</summary>
    public class MultiFactionEarthPhase7bUiTests
    {
        [Test]
        public void Consumption_SevenBGoods_ShowDashBeforeFirstDraw()
        {
            var before = new ColonyNeedsView
            {
                ConsumerGoodsPerDay = 0.53, MedicalPerDay = 0.27, LuxuryPerDay = 0.13,
                ConsumerGoodsStock = 1000, MedicalStock = 500, LuxuryStock = 250,
                ConsumerGoodsDaysOfStock = 180, MedicalDaysOfStock = 180, LuxuryDaysOfStock = 180,
            };
            var goods = ColonyWorkforceDisplay.Goods(before);
            Assert.That(goods.Skip(2).Select(g => g.Name),
                Is.EqualTo(new[] { "Consumer Goods", "Medical Supplies", "Luxury Goods" }));
            Assert.That(goods.Skip(2).Select(ColonyWorkforceDisplay.StatusText), Is.EqualTo(new[] { "-", "-", "-" }));
            Assert.That(goods.Skip(2).All(g => !ColonyWorkforceDisplay.StatusIsShort(g)));
            Assert.That(ColonyWorkforceDisplay.FormatPerDay(0.53), Is.EqualTo("0.53 a day"));

            var shortMedical = before with { MedicalMet = false, MedicalShort = true, ConsumerGoodsMet = true, LuxuryMet = true };
            var after = ColonyWorkforceDisplay.Goods(shortMedical);
            Assert.That(ColonyWorkforceDisplay.StatusText(after[2]), Is.EqualTo("Met"));
            Assert.That(ColonyWorkforceDisplay.StatusText(after[3]), Is.EqualTo("Short"));
            Assert.That(ColonyWorkforceDisplay.StatusIsShort(after[3]), Is.True);
            Assert.That(ColonyWorkforceDisplay.StatusText(after[4]), Is.EqualTo("Met"));
        }

        [Test]
        public void SatisfactionParts_AreSigned_AndFactorsFormat()
        {
            var needs = new ColonyNeedsView
            {
                Satisfaction = 65, SatisfactionTarget = 70,
                MedicalFactor = 0.5, GoodsGrowthFactor = 1.21, LuxuryMigrationPull = 1.2,
                SatisfactionParts = new[]
                {
                    new SatisfactionPartView("Base", 50),
                    new SatisfactionPartView("Food short", -15),
                    new SatisfactionPartView("Consumer goods", 10),
                    new SatisfactionPartView("Even", 0),
                },
            };
            Assert.That(ColonyWorkforceDisplay.SatisfactionText(needs), Is.EqualTo("65 / 70"));
            Assert.That(ColonyWorkforceDisplay.SatisfactionPartLine(needs.SatisfactionParts[0]), Is.EqualTo("Base  +50"));
            Assert.That(ColonyWorkforceDisplay.SatisfactionPartLine(needs.SatisfactionParts[1]), Is.EqualTo("Food short  -15"));
            Assert.That(ColonyWorkforceDisplay.SatisfactionPartLine(needs.SatisfactionParts[2]), Is.EqualTo("Consumer goods  +10"));
            Assert.That(ColonyWorkforceDisplay.SatisfactionPartLine(needs.SatisfactionParts[3]), Is.EqualTo("Even  0"));
            Assert.That(ColonyWorkforceDisplay.FactorText(needs.MedicalFactor), Is.EqualTo("0.5×"));
            Assert.That(ColonyWorkforceDisplay.FactorText(needs.GoodsGrowthFactor), Is.EqualTo("1.21×"));
            Assert.That(ColonyWorkforceDisplay.FactorText(needs.LuxuryMigrationPull), Is.EqualTo("1.2×"));
        }

        [Test]
        public void CrewTab_PartRowsSumToNeeded_WithoutCaptainRow()
        {
            var ship = new ShipView("Starship", 28)
            {
                CrewByComponent = new[]
                {
                    new ShipCrewRow("b", "Bridge", 1, 18),
                    new ShipCrewRow("a", "Laser", 2, 5),
                },
                CrewBerths = 30,
                OfficerSlots = 4,
                OfficersAboard = 1,
                OfficersRequired = 1,
                PeopleAboard = 29,
                CrewTracked = true,
                CrewAboard = 28,
            };
            int fromParts = ship.CrewByComponent.Sum(r => r.Crew);
            Assert.That(fromParts, Is.EqualTo(ship.CrewRequired), "part rows equal Needed; the captain is not crew");
            Assert.That(ship.CrewByComponent.Count(r => r.DesignId == "captain"), Is.EqualTo(0));

            var rows = CrewBars.CrewByComponentRows(ship);
            Assert.That(rows.Select(r => (r.Name, r.Value, r.Percent)),
                Is.EqualTo(new[] { ("Bridge", "18", "64%"), ("Laser", "10", "36%") }));
            Assert.That(CrewBars.ShipRows(ship).Single(r => r.Id == "required").Tooltip,
                Does.Not.Contain("plus the captain"));
            Assert.That(CrewBars.RequiredVsBerths(ship.CrewRequired, ship.CrewBerths),
                Is.EqualTo("28 crew, 30 bunks"));
            Assert.That(CrewBars.Indicator(ship), Is.EqualTo("28/28 crew"));
            Assert.That(CrewRules.CrewLine(ship.CrewAboard, ship.CrewRequired, ship.CrewBerths),
                Is.EqualTo("Crew 28/28 (30 bunks)"));
            Assert.That(CrewRules.OfficersLine(ship.OfficersAboard, ship.OfficerSlots),
                Is.EqualTo("Officers 1/4"));
            Assert.That(CrewRules.PeopleAboardLine(ship.PeopleAboard),
                Is.EqualTo("Life support: 29 people aboard"));
        }
    }
}
