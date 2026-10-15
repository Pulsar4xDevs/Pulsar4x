using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

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
        Assert.That(Info(session.FactionId).Grants.ContainsKey(otherId), Is.False);
    }

    [Test]
    public void MutualFriendlyStance_DoesNotTrade_MutualTradeGrantsDo()
    {
        var session = Connect();
        int otherId = OtherFactionId(session.FactionId);
        Info(session.FactionId).Stances[otherId] = FactionStance.Friendly;
        Info(otherId).Stances[session.FactionId] = FactionStance.Friendly;

        Assert.That(FactionStanceRules.CanTrade(_game, session.FactionId, otherId), Is.False);
        Assert.That(FactionStanceRules.CanTrade(
            Info(session.FactionId), otherId, Info(otherId), session.FactionId), Is.False);

        Assert.That(FactionStanceRules.SetGrant(Info(session.FactionId), otherId, FactionGrants.Trade, true), Is.True);
        Assert.That(FactionStanceRules.CanTrade(_game, session.FactionId, otherId), Is.False);

        Assert.That(FactionStanceRules.SetGrant(Info(otherId), session.FactionId, FactionGrants.Trade, true), Is.True);
        Assert.That(FactionStanceRules.CanTrade(_game, session.FactionId, otherId), Is.True);
        Assert.That(FactionStanceRules.CanTrade(
            Info(session.FactionId), otherId, Info(otherId), session.FactionId), Is.True);
    }

    [Test]
    public void AlliedStatus_ProjectsFriendly_AndDoesNotTrade()
    {
        var session = Connect();
        int otherId = OtherFactionId(session.FactionId);
        var body = OwnedBy(otherId);

        Info(session.FactionId).Stances[otherId] = FactionStance.Allied;
        Info(otherId).Stances[session.FactionId] = FactionStance.Allied;
        Assert.That(_projector.ProjectEntity(body, session.FactionId).Relation, Is.EqualTo(OwnerRelation.Friendly));
        Assert.That(FactionStanceRules.CanTrade(_game, session.FactionId, otherId), Is.False);
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
        Assert.That(row.TheirStance, Is.EqualTo(FactionStance.Hostile));
        Assert.That(row.GrantedByViewer, Is.EqualTo(FactionGrants.None));
        Assert.That(row.GrantedToViewer, Is.EqualTo(FactionGrants.None));
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
        Assert.That(mine.TheirStance, Is.EqualTo(FactionStance.Hostile));
        Assert.That(mine.GrantedByViewer, Is.EqualTo(FactionGrants.None));

        var theirs = _projector.ProjectStances(otherId).Single(r => r.FactionId == session.FactionId);
        Assert.That(theirs.Stance, Is.EqualTo(FactionStance.Hostile));
        Assert.That(theirs.TheirStance, Is.EqualTo(FactionStance.Friendly));
    }

    [Test]
    public void Clone_CopiesStancesAndGrants()
    {
        var session = Connect();
        int otherId = OtherFactionId(session.FactionId);
        var info = Info(session.FactionId);
        info.Stances[otherId] = FactionStance.Allied;
        info.Grants[otherId] = FactionGrants.Trade | FactionGrants.Geo;

        var copy = (FactionInfoDB)info.Clone();
        Assert.That(copy.Stances[otherId], Is.EqualTo(FactionStance.Allied));
        Assert.That(copy.Grants[otherId], Is.EqualTo(FactionGrants.Trade | FactionGrants.Geo));

        info.Stances[otherId] = FactionStance.Hostile;
        info.Grants[otherId] = FactionGrants.None;
        Assert.That(copy.Stances[otherId], Is.EqualTo(FactionStance.Allied));
        Assert.That(copy.Grants[otherId], Is.EqualTo(FactionGrants.Trade | FactionGrants.Geo));
    }

    [Test]
    public void SetGrant_WritesOneFlag_AndRejectsTheRest()
    {
        var session = Connect();
        int otherId = OtherFactionId(session.FactionId);
        var mine = Info(session.FactionId);

        var trade = _server.SubmitCommand(session,
            new SetFactionGrantCommand(session.FactionId, otherId, FactionGrants.Trade, true));
        Assert.That(trade.Accepted, Is.True, trade.RejectionReason);
        Assert.That(mine.Grants[otherId], Is.EqualTo(FactionGrants.Trade));
        Assert.That(OtherInfo(otherId).Grants.ContainsKey(session.FactionId), Is.False);

        var geo = _server.SubmitCommand(session,
            new SetFactionGrantCommand(session.FactionId, otherId, FactionGrants.Geo, true));
        Assert.That(geo.Accepted, Is.True, geo.RejectionReason);
        Assert.That(mine.Grants[otherId], Is.EqualTo(FactionGrants.Trade | FactionGrants.Geo));

        var off = _server.SubmitCommand(session,
            new SetFactionGrantCommand(session.FactionId, otherId, FactionGrants.Trade, false));
        Assert.That(off.Accepted, Is.True, off.RejectionReason);
        Assert.That(mine.Grants[otherId], Is.EqualTo(FactionGrants.Geo));

        var both = _server.SubmitCommand(session,
            new SetFactionGrantCommand(session.FactionId, otherId, FactionGrants.Trade | FactionGrants.Positions, true));
        Assert.That(both.Accepted, Is.False);
        Assert.That(mine.Grants[otherId], Is.EqualTo(FactionGrants.Geo));

        var self = _server.SubmitCommand(session,
            new SetFactionGrantCommand(session.FactionId, session.FactionId, FactionGrants.Trade, true));
        Assert.That(self.Accepted, Is.False);

        var neutral = _server.SubmitCommand(session,
            new SetFactionGrantCommand(session.FactionId, Game.NeutralFactionId, FactionGrants.Trade, true));
        Assert.That(neutral.Accepted, Is.False);

        var unknown = _server.SubmitCommand(session,
            new SetFactionGrantCommand(session.FactionId, -123456, FactionGrants.Research, true));
        Assert.That(unknown.Accepted, Is.False);

        var body = OwnedBy(session.FactionId);
        var notFaction = _server.SubmitCommand(session,
            new SetFactionGrantCommand(body.Id, otherId, FactionGrants.Grav, true));
        Assert.That(notFaction.Accepted, Is.False);
        Assert.That(mine.Grants[otherId], Is.EqualTo(FactionGrants.Geo));
        Assert.That(FactionStanceRules.CanTrade(_game, session.FactionId, otherId), Is.False);
    }

    [Test]
    public void Migrate_FillsOpenDefaultsOnce_AndKeepsAStoredNo()
    {
        var session = Connect();
        int otherId = OtherFactionId(session.FactionId);
        var info = Info(session.FactionId);
        info.Stances[otherId] = FactionStance.Friendly;
        info.Grants.Remove(otherId);

        FactionStanceRules.MigrateGrants(info);
        Assert.That(info.Grants[otherId], Is.EqualTo(FactionStanceRules.OpenDefaults));

        info.Grants[otherId] = FactionGrants.None;
        info.Stances[otherId] = FactionStance.Allied;
        FactionStanceRules.MigrateGrants(info);
        Assert.That(info.Grants[otherId], Is.EqualTo(FactionGrants.None));

        int closed = -4242;
        info.Stances[closed] = FactionStance.Hostile;
        FactionStanceRules.MigrateGrants(info);
        Assert.That(info.Grants.ContainsKey(closed), Is.False);
    }

    [Test]
    public void Grants_RevealOnlyTheMatchingSide()
    {
        var session = Connect();
        int otherId = OtherFactionId(session.FactionId);
        var system = _game.Systems[0];

        var ship = Entity.Create(otherId);
        system.AddEntity(ship, new List<BaseDataBlob>
        {
            new ShipInfoDB(),
            new NameDB("Pathfinder"),
            new PositionDB(10, 20, 30),
            new GoalsDB(),
            new CargoStorageDB("general", 100),
        });

        var colony = Entity.Create(otherId);
        system.AddEntity(colony, new List<BaseDataBlob>
        {
            new ColonyInfoDB(),
            new NameDB("Depot"),
            new PositionDB(4, 5, 6),
        });

        Assert.That(Ids(session.FactionId), Does.Not.Contain(ship.Id));
        Assert.That(Ids(session.FactionId), Does.Not.Contain(colony.Id));
        Assert.That(system.IsEntityVisibleToFaction(ship, session.FactionId), Is.False);
        Assert.That(system.IsEntityVisibleToFaction(colony, session.FactionId), Is.False);

        FactionStanceRules.SetGrant(Info(session.FactionId), otherId, FactionGrants.Positions, true);
        Assert.That(Ids(session.FactionId), Does.Not.Contain(ship.Id));

        FactionStanceRules.SetGrant(Info(otherId), session.FactionId, FactionGrants.Positions, true);
        Assert.That(system.IsEntityVisibleToFaction(ship, session.FactionId), Is.True);
        Assert.That(system.IsEntityVisibleToFaction(colony, session.FactionId), Is.False);

        var revealed = _projector.ProjectSystem(system, session.FactionId).Entities.Single(e => e.Id == ship.Id);
        Assert.That(revealed.Relation, Is.EqualTo(OwnerRelation.Hostile));
        Assert.That(revealed.GetView<NameView>()!.Name, Is.EqualTo("Pathfinder"));
        Assert.That(revealed.GetView<PositionView>()!.AbsolutePosition, Is.EqualTo(new Vec3(10, 20, 30)));
        Assert.That(revealed.GetView<ShipView>()!.DesignName, Is.EqualTo(""));
        Assert.That(revealed.HasView<OrdersView>(), Is.False);
        Assert.That(revealed.HasView<CargoStorageView>(), Is.False);

        FactionStanceRules.SetGrant(Info(otherId), session.FactionId, FactionGrants.Trade, true);
        Assert.That(system.IsEntityVisibleToFaction(colony, session.FactionId), Is.True);
        var shown = _projector.ProjectSystem(system, session.FactionId).Entities.Single(e => e.Id == colony.Id);
        Assert.That(shown.Kind, Is.EqualTo(BodyKind.Colony));
        Assert.That(shown.HasView<MarketView>(), Is.False);
        Assert.That(shown.Relation, Is.EqualTo(OwnerRelation.Hostile));
    }

    [Test]
    public void ProjectStances_ReportsAGeoGrant_WithoutOpeningTrade()
    {
        var session = Connect();
        int otherId = OtherFactionId(session.FactionId);
        Info(otherId).Stances[session.FactionId] = FactionStance.Neutral;
        var result = _server.SubmitCommand(session,
            new SetFactionGrantCommand(session.FactionId, otherId, FactionGrants.Geo, true));
        Assert.That(result.Accepted, Is.True, result.RejectionReason);

        var mine = _projector.ProjectStances(session.FactionId).Single(r => r.FactionId == otherId);
        Assert.That(mine.GrantedByViewer, Is.EqualTo(FactionGrants.Geo));
        Assert.That(mine.GrantedToViewer, Is.EqualTo(FactionGrants.None));
        Assert.That(mine.TheirStance, Is.EqualTo(FactionStance.Neutral));
        Assert.That(FactionStanceRules.CanTrade(_game, session.FactionId, otherId), Is.False);

        var theirs = _projector.ProjectStances(otherId).Single(r => r.FactionId == session.FactionId);
        Assert.That(theirs.GrantedToViewer, Is.EqualTo(FactionGrants.Geo));
        Assert.That(theirs.GrantedByViewer, Is.EqualTo(FactionGrants.None));
    }

    IEnumerable<int> Ids(int factionId)
        => _projector.ProjectSystem(_game.Systems[0], factionId).Entities.Select(e => e.Id);

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
