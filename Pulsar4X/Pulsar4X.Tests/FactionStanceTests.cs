using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Names;

namespace Pulsar4X.Tests;

[TestFixture]
public class FactionStanceTests : ApiTestBase
{
    [Test]
    public void MissingStance_IsHostile_AndCannotTrade()
    {
        var session = Connect();
        int otherId = OtherFactionId(session.FactionId);
        var body = OwnedBy(otherId);

        Assert.That(_projector.ProjectEntity(body, session.FactionId).Relation, Is.EqualTo(OwnerRelation.Hostile));
        Assert.That(FactionStanceRules.CanTrade(_game, session.FactionId, otherId), Is.False);
    }

    [Test]
    public void OneSidedFriendly_ProjectsFriendly_AndCannotTrade()
    {
        var session = Connect();
        int otherId = OtherFactionId(session.FactionId);
        var body = OwnedBy(otherId);

        var result = _server.SubmitCommand(session,
            new SetFactionStanceCommand(session.FactionId, otherId, FactionStance.Friendly));
        Assert.That(result.Accepted, Is.True, result.RejectionReason);

        Assert.That(_projector.ProjectEntity(body, session.FactionId).Relation, Is.EqualTo(OwnerRelation.Friendly));
        Assert.That(FactionStanceRules.CanTrade(_game, session.FactionId, otherId), Is.False);
        Assert.That(OtherInfo(otherId).Stances.ContainsKey(session.FactionId), Is.False);
    }

    [Test]
    public void MutualFriendly_CanTrade()
    {
        var session = Connect();
        int otherId = OtherFactionId(session.FactionId);
        Info(session.FactionId).Stances[otherId] = FactionStance.Friendly;
        Info(otherId).Stances[session.FactionId] = FactionStance.Friendly;

        Assert.That(FactionStanceRules.CanTrade(_game, session.FactionId, otherId), Is.True);
        Assert.That(FactionStanceRules.CanTrade(
            Info(session.FactionId), otherId, Info(otherId), session.FactionId), Is.True);
    }

    [Test]
    public void Allied_ProjectsAsFriendly_AndTradesWhenTheOtherSideIsOpen()
    {
        var session = Connect();
        int otherId = OtherFactionId(session.FactionId);
        var body = OwnedBy(otherId);

        Info(session.FactionId).Stances[otherId] = FactionStance.Allied;
        Assert.That(_projector.ProjectEntity(body, session.FactionId).Relation, Is.EqualTo(OwnerRelation.Friendly));
        Assert.That(FactionStanceRules.CanTrade(_game, session.FactionId, otherId), Is.False);

        Info(otherId).Stances[session.FactionId] = FactionStance.Friendly;
        Assert.That(FactionStanceRules.CanTrade(_game, session.FactionId, otherId), Is.True);

        Info(otherId).Stances[session.FactionId] = FactionStance.Allied;
        Assert.That(FactionStanceRules.CanTrade(_game, session.FactionId, otherId), Is.True);
    }

    [Test]
    public void SameFaction_CanTrade_NeutralIdNeverTrades()
    {
        var session = Connect();

        Assert.That(FactionStanceRules.CanTrade(_game, session.FactionId, session.FactionId), Is.True);
        Assert.That(FactionStanceRules.CanTrade(_game, session.FactionId, Game.NeutralFactionId), Is.False);
        Assert.That(FactionStanceRules.CanTrade(_game, Game.NeutralFactionId, Game.NeutralFactionId), Is.False);
        Assert.That(FactionStanceRules.CanTrade(_game, session.FactionId, -123456), Is.False);
    }

    [Test]
    public void DiplomaticNeutral_ProjectsNeutral()
    {
        var session = Connect();
        int otherId = OtherFactionId(session.FactionId);
        var body = OwnedBy(otherId);
        Info(session.FactionId).Stances[otherId] = FactionStance.Neutral;

        Assert.That(_projector.ProjectEntity(body, session.FactionId).Relation, Is.EqualTo(OwnerRelation.Neutral));
        Assert.That(FactionStanceRules.CanTrade(_game, session.FactionId, otherId), Is.False);
    }

    [Test]
    public void SetStance_RejectsSelf_Neutral_Unknown_AndNonFactionTarget()
    {
        var session = Connect();
        int otherId = OtherFactionId(session.FactionId);
        var mine = Info(session.FactionId);
        var theirs = OtherInfo(otherId);

        var self = _server.SubmitCommand(session,
            new SetFactionStanceCommand(session.FactionId, session.FactionId, FactionStance.Friendly));
        Assert.That(self.Accepted, Is.False);
        Assert.That(mine.Stances, Is.Empty);
        Assert.That(theirs.Stances, Is.Empty);

        var neutral = _server.SubmitCommand(session,
            new SetFactionStanceCommand(session.FactionId, Game.NeutralFactionId, FactionStance.Friendly));
        Assert.That(neutral.Accepted, Is.False);
        Assert.That(mine.Stances, Is.Empty);
        Assert.That(theirs.Stances, Is.Empty);

        var unknown = _server.SubmitCommand(session,
            new SetFactionStanceCommand(session.FactionId, -123456, FactionStance.Friendly));
        Assert.That(unknown.Accepted, Is.False);
        Assert.That(mine.Stances, Is.Empty);

        var body = OwnedBy(session.FactionId);
        var notFaction = _server.SubmitCommand(session,
            new SetFactionStanceCommand(body.Id, otherId, FactionStance.Friendly));
        Assert.That(notFaction.Accepted, Is.False);
        Assert.That(mine.Stances.ContainsKey(otherId), Is.False);
        Assert.That(theirs.Stances, Is.Empty);
    }

    [Test]
    public void ProjectStances_ListsTheOtherFactionAsHostile()
    {
        var session = Connect();
        int otherId = OtherFactionId(session.FactionId);
        var rows = _projector.ProjectStances(session.FactionId);

        Assert.That(rows.Select(r => r.FactionId), Does.Not.Contain(session.FactionId));
        Assert.That(rows.Select(r => r.FactionId), Does.Not.Contain(_game.GameMasterFaction.Id));
        Assert.That(rows.Select(r => r.FactionId), Does.Not.Contain(Game.NeutralFactionId));

        var row = rows.Single(r => r.FactionId == otherId);
        var expectedName = _game.Factions[otherId].GetDataBlob<NameDB>().GetName(session.FactionId);
        Assert.That(row.Name, Is.EqualTo(expectedName));
        Assert.That(row.Stance, Is.EqualTo(FactionStance.Hostile));
    }

    [Test]
    public void ProjectStances_ReportsStoredStance_AndNotTheOtherSides()
    {
        var session = Connect();
        int otherId = OtherFactionId(session.FactionId);

        var result = _server.SubmitCommand(session,
            new SetFactionStanceCommand(session.FactionId, otherId, FactionStance.Friendly));
        Assert.That(result.Accepted, Is.True, result.RejectionReason);

        var mine = _projector.ProjectStances(session.FactionId).Single(r => r.FactionId == otherId);
        Assert.That(mine.Stance, Is.EqualTo(FactionStance.Friendly));

        var theirs = _projector.ProjectStances(otherId).Single(r => r.FactionId == session.FactionId);
        Assert.That(theirs.Stance, Is.EqualTo(FactionStance.Hostile));
    }

    [Test]
    public void Clone_CopiesStances()
    {
        var session = Connect();
        int otherId = OtherFactionId(session.FactionId);
        var info = Info(session.FactionId);
        info.Stances[otherId] = FactionStance.Allied;

        var copy = (FactionInfoDB)info.Clone();
        Assert.That(copy.Stances[otherId], Is.EqualTo(FactionStance.Allied));

        info.Stances[otherId] = FactionStance.Hostile;
        Assert.That(copy.Stances[otherId], Is.EqualTo(FactionStance.Allied));
    }

    int OtherFactionId(int viewerId)
        => _game.Factions.Keys.First(id => id != viewerId && id != _game.GameMasterFaction.Id);

    FactionInfoDB Info(int factionId)
        => _game.Factions[factionId].GetDataBlob<FactionInfoDB>();

    FactionInfoDB OtherInfo(int factionId) => Info(factionId);

    Entity OwnedBy(int factionId)
    {
        var body = _game.Systems[0].GetAllEntites().First();
        body.FactionOwnerID = factionId;
        return body;
    }
}
