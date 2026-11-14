using System;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Client;
using Pulsar4X.DataStructures;
using Pulsar4X.People;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Commanders window "Yrs of Service" / "Yrs in Rank": elapsed years since the commission (or promotion),
    /// not a calendar year. Before the fix, commanders created without dates showed ~2049.
    /// </summary>
    [TestFixture]
    public class CommanderServiceYearsTests : ApiTestBase
    {
        static readonly DateTime Now = new DateTime(2050, 1, 1);

        [Test]
        public void CommissionedTenYearsEarlier_ShowsTenYears()
        {
            Assert.That(CommanderWindow.YearsText(Now - TimeSpan.FromDays(365.25 * 10), Now), Is.EqualTo("10.0"));
            Assert.That(CommanderWindow.YearsText(new DateTime(2040, 1, 1), Now), Is.EqualTo("10.0"));
            Assert.That(CommanderWindow.YearsText(new DateTime(2047, 7, 2), Now), Is.EqualTo("2.5"));
        }

        [Test]
        public void CommissionedToday_ShowsZero()
            => Assert.That(CommanderWindow.YearsText(Now, Now), Is.EqualTo("0.0"));

        [Test]
        public void ADateAfterNow_NeverShowsNegative()
            => Assert.That(CommanderWindow.YearsText(Now.AddDays(3), Now), Is.EqualTo("0.0"));

        [Test]
        public void AnUnrecordedDate_ShowsADash_NotACalendarYear()
            => Assert.That(CommanderWindow.YearsText(default, Now), Is.EqualTo("-"));

        [Test]
        public void ACommanderCreatedWithoutDates_IsCommissionedToday_AndShowsZeroYears()
        {
            // The starting scientist and admin come from CommanderFactory.CreateScientist/CreateAdmin, which set no dates.
            var session = Connect();
            var system = _game.Systems[0];
            var scientist = CommanderFactory.Create(system, session.FactionId, CommanderFactory.CreateScientist(_game));
            var admin = CommanderFactory.Create(system, session.FactionId, CommanderFactory.CreateAdmin(_game));

            var roster = _projector.ProjectCommanders(session.FactionId)!;
            foreach (var id in new[] { scientist.Id, admin.Id })
            {
                var snap = roster.Single(c => c.Id == id);
                Assert.That(snap.CommissionedOn, Is.EqualTo(system.StarSysDateTime));
                Assert.That(snap.RankedOn, Is.EqualTo(system.StarSysDateTime));
                Assert.That(CommanderWindow.YearsText(snap.CommissionedOn, system.StarSysDateTime), Is.EqualTo("0.0"));
                Assert.That(CommanderWindow.YearsText(snap.RankedOn, system.StarSysDateTime), Is.EqualTo("0.0"));
            }
        }

        [Test]
        public void ExplicitDates_AreKept()
        {
            var session = Connect();
            var when = _game.TimePulse.GameGlobalDateTime - TimeSpan.FromDays(365.25 * 10);
            var db = new CommanderDB("Kept", 6, CommanderTypes.Navy) { CommissionedOn = when, RankedOn = when.AddYears(9) };
            var e = CommanderFactory.Create(_game.Systems[0], session.FactionId, db);
            Assert.That(e.GetDataBlob<CommanderDB>().CommissionedOn, Is.EqualTo(when));
            Assert.That(e.GetDataBlob<CommanderDB>().RankedOn, Is.EqualTo(when.AddYears(9)));
        }
    }
}
