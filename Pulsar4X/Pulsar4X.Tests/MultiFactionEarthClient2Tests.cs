using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Rendering;

namespace Pulsar4X.Tests
{
    /// <summary>Client helpers for the follow-up batch: empty-view map icons and the ledger tree.</summary>
    public class MultiFactionEarthClient2Tests
    {
        [Test]
        public void SystemMapIcons_SkipsSnapshotsWithNoViews()
        {
            var empty = new EntitySnapshot { Id = 1, Views = Array.Empty<IComponentView>() };
            Assert.That(SystemMapIcons.CanIcon(empty), Is.False, "SM mode used to hit Views[0] here and throw");

            var named = new EntitySnapshot
            {
                Id = 2,
                Views = new IComponentView[] { new NameView("Luna") },
            };
            Assert.That(SystemMapIcons.CanIcon(named), Is.True);
            Assert.DoesNotThrow(() => { _ = named.GetView<NameView>(); });
        }

        [Test]
        public void LedgerTree_GroupsByYearThenMonth_WithCategoryTotals()
        {
            var rows = new[]
            {
                new LedgerEntrySnapshot(new DateTime(2050, 2, 1), FactionSnapshot.WagesCategory, "Feb wages", -200m),
                new LedgerEntrySnapshot(new DateTime(2050, 2, 1), "Research", "Feb labs", -50m),
                new LedgerEntrySnapshot(new DateTime(2050, 1, 15), "Trade", "Sold steel", 80m),
                new LedgerEntrySnapshot(new DateTime(2050, 1, 1), FactionSnapshot.WagesCategory, "Jan wages", -100m),
                new LedgerEntrySnapshot(new DateTime(2049, 12, 1), "InitialInvestment", "Start-up", 1000m),
            };

            var years = LedgerTree.Build(rows);
            Assert.That(years.Select(y => y.Year), Is.EqualTo(new[] { 2050, 2049 }));

            var y2050 = years[0];
            Assert.That(y2050.IncomeTotal, Is.EqualTo(80m));
            Assert.That(y2050.ExpenseTotal, Is.EqualTo(-350m));
            Assert.That(y2050.IncomeCategories.Single().Category, Is.EqualTo("Trade"));
            Assert.That(y2050.ExpenseCategories.Select(c => c.Category),
                Is.EqualTo(new[] { FactionSnapshot.WagesCategory, "Research" }));
            Assert.That(y2050.ExpenseCategories[0].Total, Is.EqualTo(-300m));
            Assert.That(y2050.Months.Select(m => m.Month), Is.EqualTo(new[] { 2, 1 }));

            var feb = y2050.Months[0];
            Assert.That(feb.IncomeTotal, Is.EqualTo(0m));
            Assert.That(feb.ExpenseTotal, Is.EqualTo(-250m));
            Assert.That(feb.ExpenseCategories.Select(c => (c.Label, c.Total, c.Entries.Count)),
                Is.EqualTo(new[] { ("Wages (monthly)", -200m, 1), ("Research (monthly)", -50m, 1) }));

            var jan = y2050.Months[1];
            Assert.That(jan.IncomeTotal, Is.EqualTo(80m));
            Assert.That(jan.ExpenseTotal, Is.EqualTo(-100m));
            Assert.That(jan.IncomeCategories.Single().Entries.Single().Description, Is.EqualTo("Sold steel"));

            Assert.That(years[1].IncomeTotal, Is.EqualTo(1000m));
            Assert.That(years[1].Months.Single().Month, Is.EqualTo(12));
        }

        [Test]
        public void LedgerTree_AndDateDisplay_FormatYearAndMonth()
        {
            Assert.That(LedgerTree.YearLabel(2050), Is.EqualTo("2050"));
            Assert.That(DateDisplay.FormatYear(2050, DateStyle.Iso), Is.EqualTo("2050"));
            Assert.That(DateDisplay.FormatMonthYear(new DateTime(2050, 1, 15), DateStyle.DayMonthName), Is.EqualTo("Jan 2050"));
            Assert.That(DateDisplay.FormatMonthYear(new DateTime(2050, 1, 15), DateStyle.Iso), Is.EqualTo("2050-01"));
            Assert.That(DateDisplay.FormatMonthYear(new DateTime(2050, 1, 15), DateStyle.DayFirst), Is.EqualTo("01/2050"));
            Assert.That(DateDisplay.FormatMonthYear(new DateTime(2050, 1, 15), DateStyle.MonthFirst), Is.EqualTo("01/2050"));
            Assert.That(LedgerTree.Build(null), Is.Empty);
            Assert.That(LedgerTree.Build(Array.Empty<LedgerEntrySnapshot>()), Is.Empty);
        }
    }
}
