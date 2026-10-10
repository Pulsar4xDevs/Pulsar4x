using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.DataStructures;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Industry;
using Pulsar4X.JumpPoints;
using Pulsar4X.Logistics;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class ApiIntelBookTests : ApiTestBase
    {
        [Test]
        public void OwnedOffice_ProjectsAGeoRow_FriendlySeesIt_HostileDoesNot()
        {
            var session = Connect();
            var colony = MakeColony(session.FactionId);
            Office(colony);
            var body = SurveyBody(colony);
            FinishGeo(body, session.FactionId);
            var commands = new CommandTranslator(_game);
            var owner = _game.Factions[session.FactionId];
            string subject = body.Id.ToString(CultureInfo.InvariantCulture);

            var hidden = commands.Translate(owner, colony, new SetIntelListingCommand(colony.Id, subject, IntelKind.Geo, 40m, false));
            Assert.That(hidden.Accepted, Is.True);
            var other = OtherFaction(session.FactionId);
            Befriend(session.FactionId, other.Id);

            var ownerView = _projector.ProjectEntity(colony, session.FactionId).GetView<IntelBookView>();
            var friendlyView = _projector.ProjectEntity(colony, other.Id).GetView<IntelBookView>();
            Assert.That(ownerView, Is.Not.Null);
            Assert.That(ownerView!.CanEdit, Is.True);
            Assert.That(ownerView.Rows.Any(row => row.Subject == subject && !row.ForSale), Is.True);
            Assert.That(ownerView.Candidates.Any(row => row.Kind == IntelKind.Geo && row.Subject == subject), Is.True);
            Assert.That(friendlyView, Is.Not.Null);
            Assert.That(friendlyView!.CanEdit, Is.False);
            Assert.That(friendlyView.Rows.Any(row => row.Subject == subject), Is.False);
            Assert.That(friendlyView.Candidates, Is.Empty);

            var listed = commands.Translate(owner, colony, new SetIntelListingCommand(colony.Id, subject, IntelKind.Geo, 40m, true));
            Assert.That(listed.Accepted, Is.True);
            friendlyView = _projector.ProjectEntity(colony, other.Id).GetView<IntelBookView>();
            var row = friendlyView!.Rows.Single(item => item.Subject == subject);
            Assert.That(row.Kind, Is.EqualTo(IntelKind.Geo));
            Assert.That(row.ResultTag, Is.EqualTo("partial minerals"));
            Assert.That(row.Ask, Is.EqualTo(40m));
            Assert.That(row.OwnedByViewer, Is.False);
            Assert.That(friendlyView.CanEdit, Is.False);

            other.GetDataBlob<FactionInfoDB>().Stances.Remove(session.FactionId);
            FactionOf(session.FactionId).Stances.Remove(other.Id);
            Assert.That(_projector.ProjectEntity(colony, other.Id).GetView<IntelBookView>(), Is.Null);
        }

        [Test]
        public void Set_RejectsBadRows_ClearOfMissingSucceeds()
        {
            var session = Connect();
            var colony = MakeColony(session.FactionId);
            var body = SurveyBody(colony);
            var commands = new CommandTranslator(_game);
            var owner = _game.Factions[session.FactionId];
            string subject = body.Id.ToString(CultureInfo.InvariantCulture);

            var noOffice = commands.Translate(owner, colony, new SetIntelListingCommand(colony.Id, subject, IntelKind.Geo, 1m, true));
            Assert.That(noOffice.Accepted, Is.False);
            Assert.That(noOffice.RejectionReason, Is.EqualTo("The colony has no logistics office."));

            Office(colony);
            var empty = commands.Translate(owner, colony, new SetIntelListingCommand(colony.Id, "", IntelKind.Geo, 1m, true));
            Assert.That(empty.Accepted, Is.False);
            Assert.That(empty.RejectionReason, Is.EqualTo("Subject is required."));

            var negative = commands.Translate(owner, colony, new SetIntelListingCommand(colony.Id, subject, IntelKind.Geo, -1m, true));
            Assert.That(negative.Accepted, Is.False);
            Assert.That(negative.RejectionReason, Is.EqualTo("Prices cannot be negative."));

            var unfinished = commands.Translate(owner, colony, new SetIntelListingCommand(colony.Id, subject, IntelKind.Geo, 1m, true));
            Assert.That(unfinished.Accepted, Is.False);
            Assert.That(unfinished.RejectionReason, Is.EqualTo("The seller has not finished that survey."));

            var missing = commands.Translate(owner, colony, new ClearIntelListingCommand(colony.Id, subject, IntelKind.Geo));
            Assert.That(missing.Accepted, Is.True);
            Assert.That(IntelBook.TryGet(colony, IntelKind.Geo, subject, out _), Is.False);
        }

        [Test]
        public void BuyGeo_GrantsPartial_MovesTheLedger_AndRejectsTheSecondTime()
        {
            var session = Connect();
            var colony = MakeColony(session.FactionId);
            Office(colony);
            var seller = _game.Factions[session.FactionId];
            var sellerInfo = FactionOf(session.FactionId);
            var body = SurveyBody(colony);
            var minerals = body.GetDataBlob<MineralsDB>();
            minerals.GrantFactionAccess(sellerInfo.FactionMask);
            FinishGeo(body, session.FactionId);
            var deposit = minerals.Minerals.Values.First();
            Assert.That(deposit.Amount.GetAccess(sellerInfo.FactionMask), Is.EqualTo(AccessLevel.Full));

            var buyer = OtherFaction(session.FactionId);
            var buyerInfo = buyer.GetDataBlob<FactionInfoDB>();
            Befriend(session.FactionId, buyer.Id);
            Fund(buyerInfo, 500m);
            Fund(sellerInfo, 10m);
            string subject = body.Id.ToString(CultureInfo.InvariantCulture);
            var commands = new CommandTranslator(_game);
            Assert.That(commands.Translate(seller, colony, new SetIntelListingCommand(colony.Id, subject, IntelKind.Geo, 25m, true)).Accepted, Is.True);

            decimal buyerBefore = buyerInfo.Money.GetCurrentFunds();
            decimal sellerBefore = sellerInfo.Money.GetCurrentFunds();
            int buyerLines = buyerInfo.Money.GetAllTransactions().Count;
            int sellerLines = sellerInfo.Money.GetAllTransactions().Count;

            var bought = commands.Translate(buyer, colony, new BuyIntelCommand(colony.Id, subject, IntelKind.Geo));
            Assert.That(bought.Accepted, Is.True, bought.RejectionReason);
            Assert.That(body.GetDataBlob<GeoSurveyableDB>().IsSurveyComplete(buyer.Id), Is.True);
            Assert.That(body.GetDataBlob<GeoSurveyableDB>().IsSurveyComplete(session.FactionId), Is.True);
            Assert.That(deposit.Amount.GetAccess(buyerInfo.FactionMask), Is.EqualTo(AccessLevel.Partial));
            Assert.That(deposit.Amount.GetAccess(sellerInfo.FactionMask), Is.EqualTo(AccessLevel.Full));
            Assert.That(buyerInfo.Money.GetCurrentFunds(), Is.EqualTo(buyerBefore - 25m));
            Assert.That(sellerInfo.Money.GetCurrentFunds(), Is.EqualTo(sellerBefore + 25m));
            Assert.That(buyerInfo.Money.GetAllTransactions().Any(line => line.Description == $"Geo {subject} {session.FactionId}"), Is.True);
            Assert.That(sellerInfo.Money.GetAllTransactions().Any(line => line.Description == $"Geo {subject} {buyer.Id}"), Is.True);

            var again = commands.Translate(buyer, colony, new BuyIntelCommand(colony.Id, subject, IntelKind.Geo));
            Assert.That(again.Accepted, Is.False);
            Assert.That(again.RejectionReason, Is.EqualTo("You already have this intel."));
            Assert.That(buyerInfo.Money.GetCurrentFunds(), Is.EqualTo(buyerBefore - 25m));

            Assert.That(commands.Translate(seller, colony, new SetIntelListingCommand(colony.Id, subject, IntelKind.Geo, 0m, true)).Accepted, Is.True);
            var freeBuyer = ExtraFaction(session.FactionId, buyer.Id);
            Befriend(session.FactionId, freeBuyer.Id);
            var freeInfo = freeBuyer.GetDataBlob<FactionInfoDB>();
            int lines = freeInfo.Money.GetAllTransactions().Count;
            Assert.That(commands.Translate(freeBuyer, colony, new BuyIntelCommand(colony.Id, subject, IntelKind.Geo)).Accepted, Is.True);
            Assert.That(freeInfo.Money.GetAllTransactions().Count, Is.EqualTo(lines));
            Assert.That(body.GetDataBlob<GeoSurveyableDB>().IsSurveyComplete(freeBuyer.Id), Is.True);

            var poor = ExtraFaction(session.FactionId, buyer.Id, freeBuyer.Id);
            Befriend(session.FactionId, poor.Id);
            var poorInfo = poor.GetDataBlob<FactionInfoDB>();
            decimal poorFunds = poorInfo.Money.GetCurrentFunds();
            Assert.That(commands.Translate(seller, colony, new SetIntelListingCommand(colony.Id, subject, IntelKind.Geo, poorFunds + 1m, true)).Accepted, Is.True);
            var broke = commands.Translate(poor, colony, new BuyIntelCommand(colony.Id, subject, IntelKind.Geo));
            Assert.That(broke.Accepted, Is.False);
            Assert.That(broke.RejectionReason, Is.EqualTo("Cannot afford that price."));
            Assert.That(body.GetDataBlob<GeoSurveyableDB>().IsSurveyComplete(poor.Id), Is.False);

            buyerInfo.Stances.Remove(session.FactionId);
            var oneSided = commands.Translate(buyer, colony, new BuyIntelCommand(colony.Id, subject, IntelKind.Geo));
            Assert.That(oneSided.Accepted, Is.False);
            Assert.That(oneSided.RejectionReason, Is.EqualTo("These factions cannot trade."));

            var fresh = ExtraFaction(session.FactionId, buyer.Id, freeBuyer.Id, poor.Id);
            fresh.GetDataBlob<FactionInfoDB>().Stances[session.FactionId] = FactionStance.Friendly;
            var sided = commands.Translate(fresh, colony, new BuyIntelCommand(colony.Id, subject, IntelKind.Geo));
            Assert.That(sided.Accepted, Is.False);
            Assert.That(sided.RejectionReason, Is.EqualTo("These factions cannot trade."));
            Assert.That(body.GetDataBlob<GeoSurveyableDB>().IsSurveyComplete(fresh.Id), Is.False);

            var own = commands.Translate(seller, colony, new BuyIntelCommand(colony.Id, subject, IntelKind.Geo));
            Assert.That(own.Accepted, Is.False);
            Assert.That(own.RejectionReason, Is.EqualTo("A faction cannot buy its own intel."));
            _ = buyerLines;
            _ = sellerLines;
        }

        [Test]
        public void BuyPin_CompletesAndHides_WithoutRevealingAJump()
        {
            var session = Connect();
            var colony = MakeColony(session.FactionId);
            Office(colony);
            var system = _game.Systems[0];
            var pin = system.GetAllEntitiesWithDataBlob<JPSurveyableDB>().First();
            pin.GetDataBlob<JPSurveyableDB>().SurveyPointsRemaining[session.FactionId] = 0;
            var buyer = OtherFaction(session.FactionId);
            Befriend(session.FactionId, buyer.Id);
            Fund(buyer.GetDataBlob<FactionInfoDB>(), 100m);
            string subject = pin.Id.ToString(CultureInfo.InvariantCulture);
            var commands = new CommandTranslator(_game);
            Assert.That(commands.Translate(_game.Factions[session.FactionId], colony,
                new SetIntelListingCommand(colony.Id, subject, IntelKind.Pin, 5m, true)).Accepted, Is.True);

            var discoveredBefore = system.GetAllDataBlobsOfType<JumpPointDB>()
                .Select(jump => jump.OwningEntity.Id)
                .Where(id => system.GetDataBlob<JumpPointDB>(id).IsDiscovered.Contains(buyer.Id))
                .ToList();
            system.SetupDefaultNeutralEntitiesForFaction(buyer.Id);
            Assert.That(system.IsEntityVisibleToFaction(pin, buyer.Id), Is.True);

            var bought = commands.Translate(buyer, colony, new BuyIntelCommand(colony.Id, subject, IntelKind.Pin));
            Assert.That(bought.Accepted, Is.True, bought.RejectionReason);
            Assert.That(pin.GetDataBlob<JPSurveyableDB>().IsSurveyComplete(buyer.Id), Is.True);
            Assert.That(pin.GetDataBlob<JPSurveyableDB>().IsSurveyComplete(session.FactionId), Is.True);
            Assert.That(system.IsEntityVisibleToFaction(pin, buyer.Id), Is.False);
            var discoveredAfter = system.GetAllDataBlobsOfType<JumpPointDB>()
                .Where(jump => jump.IsDiscovered.Contains(buyer.Id))
                .Select(jump => jump.OwningEntity.Id)
                .ToList();
            Assert.That(discoveredAfter, Is.EquivalentTo(discoveredBefore));
        }

        [Test]
        public void BuyJump_RevealsTheGateAndFarSide_AndDoesNotFinishAPin()
        {
            var session = Connect();
            var colony = MakeColony(session.FactionId);
            Office(colony);
            var system = _game.Systems[0];
            var near = MakeJump(system, "Near Gate");
            var far = MakeJump(system, "Far Gate Secret");
            near.GetDataBlob<JumpPointDB>().DestinationId = far.Id;
            far.GetDataBlob<JumpPointDB>().DestinationId = near.Id;
            var sellerInfo = FactionOf(session.FactionId);
            near.GetDataBlob<JumpPointDB>().IsDiscovered.Add(session.FactionId);
            sellerInfo.RememberJumpPoint(near);

            var buyer = OtherFaction(session.FactionId);
            var buyerInfo = buyer.GetDataBlob<FactionInfoDB>();
            Befriend(session.FactionId, buyer.Id);
            Fund(buyerInfo, 200m);
            buyerInfo.KnownSystems.Remove(system.ID);
            var pin = system.GetAllEntitiesWithDataBlob<JPSurveyableDB>().First();
            string subject = near.Id.ToString(CultureInfo.InvariantCulture);
            var commands = new CommandTranslator(_game);
            Assert.That(commands.Translate(_game.Factions[session.FactionId], colony,
                new SetIntelListingCommand(colony.Id, subject, IntelKind.Jump, 80m, true)).Accepted, Is.True);

            var friendly = _projector.ProjectEntity(colony, buyer.Id).GetView<IntelBookView>();
            var row = friendly!.Rows.Single(item => item.Subject == subject);
            Assert.That(row.ResultTag, Is.EqualTo("jump"));
            Assert.That(row.Name, Does.Not.Contain("Far Gate Secret"));
            Assert.That(row.Name, Does.Not.Contain(" to "));
            var owned = _projector.ProjectEntity(colony, session.FactionId).GetView<IntelBookView>();
            Assert.That(owned!.Rows.Single(item => item.Subject == subject).Name, Does.Contain(" to "));

            var bought = commands.Translate(buyer, colony, new BuyIntelCommand(colony.Id, subject, IntelKind.Jump));
            Assert.That(bought.Accepted, Is.True, bought.RejectionReason);
            Assert.That(near.GetDataBlob<JumpPointDB>().IsDiscovered, Does.Contain(buyer.Id));
            Assert.That(far.GetDataBlob<JumpPointDB>().IsDiscovered, Does.Contain(buyer.Id));
            Assert.That(buyerInfo.KnownSystems, Does.Contain(system.ID));
            Assert.That(buyerInfo.KnownJumpPoints[system.ID].Contains(near), Is.True);
            Assert.That(buyerInfo.KnownJumpPoints[system.ID].Contains(far), Is.True);
            Assert.That(system.IsEntityVisibleToFaction(near, buyer.Id), Is.True);
            Assert.That(pin.GetDataBlob<JPSurveyableDB>().IsSurveyComplete(buyer.Id), Is.False);
            Assert.That(near.GetDataBlob<JumpPointDB>().IsDiscovered, Does.Contain(session.FactionId));
        }

        [Test]
        public void IntelRows_DoNotChangeCargoCapacity_OrMarketWakes()
        {
            var session = Connect();
            var colony = MakeColony(session.FactionId);
            var info = FactionOf(session.FactionId);
            info.Data.Unlock("iron");
            var store = new CargoStorageDB("general-storage", 100000);
            colony.SetDataBlob(store);
            var iron = info.Data.CargoGoods.GetAny("iron");
            Assert.That(iron, Is.Not.Null);
            Assert.That(store.AddCargoByUnit(iron!, 20), Is.EqualTo(20));
            colony.SetDataBlob(new LogiBaseDB { Capacity = 5 });
            Assert.That(MarketBook.SetListing(colony, new MarketListing
            {
                CargoId = "iron",
                SellQuantity = 10,
                Ask = 3m,
                BuyQuantity = 0,
                Bid = 1m,
                Reserve = 0,
            }), Is.True);

            var before = _projector.ProjectEntity(colony, session.FactionId).GetView<MarketView>();
            Assert.That(before, Is.Not.Null);
            int goods = before!.Goods.Count;
            int capacity = before.Capacity;

            var body = SurveyBody(colony);
            FinishGeo(body, session.FactionId);
            string subject = body.Id.ToString(CultureInfo.InvariantCulture);
            var commands = new CommandTranslator(_game);
            Assert.That(commands.Translate(_game.Factions[session.FactionId], colony,
                new SetIntelListingCommand(colony.Id, subject, IntelKind.Geo, 9m, true)).Accepted, Is.True);

            var after = _projector.ProjectEntity(colony, session.FactionId).GetView<MarketView>();
            Assert.That(after!.Capacity, Is.EqualTo(capacity));
            Assert.That(after.Goods, Has.Count.EqualTo(goods));
            Assert.That(colony.GetDataBlob<LogiBaseDB>().Intel, Has.Count.EqualTo(1));

            new OfferStockPlan().Plan(colony, new Goal(GoalType.OfferStock), _game.TimePulse.GameGlobalDateTime);
            new RunMarketPlan().Plan(colony, new Goal(GoalType.RunMarket), _game.TimePulse.GameGlobalDateTime);
            Assert.That(colony.GetDataBlob<LogiBaseDB>().Intel, Has.Count.EqualTo(1));
            Assert.That(colony.GetDataBlob<LogiBaseDB>().Intel.Values.Single().Subject, Is.EqualTo(subject));
        }

        void Office(Entity colony) => colony.SetDataBlob(new LogiBaseDB { Capacity = 5 });

        void FinishGeo(Entity body, int factionId)
        {
            var geo = body.GetDataBlob<GeoSurveyableDB>();
            geo.GeoSurveyStatus[factionId] = 0;
        }

        Entity SurveyBody(Entity colony)
        {
            var system = (StarSystem)colony.Manager!;
            int planetId = colony.GetDataBlob<ColonyInfoDB>().PlanetEntity.Id;
            var body = system.GetAllEntitiesWithDataBlob<GeoSurveyableDB>()
                .FirstOrDefault(entity => entity.Id != planetId);
            if (body == null)
            {
                body = system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>()
                    .First(entity => entity.Id != planetId);
                body.SetDataBlob(new GeoSurveyableDB { PointsRequired = 100 });
            }
            if (!body.TryGetDataBlob<MineralsDB>(out var minerals) || minerals.Minerals.Count == 0)
            {
                minerals = new MineralsDB();
                var deposit = new MineralDeposit();
                deposit.Amount = new Masked<long>(1000, AccessLevel.None);
                minerals.Minerals[1] = deposit;
                body.SetDataBlob(minerals);
            }
            return body;
        }

        Entity MakeJump(StarSystem system, string name)
        {
            var jump = Entity.Create();
            jump.FactionOwnerID = Game.NeutralFactionId;
            system.AddEntity(jump, new List<BaseDataBlob>
            {
                new NameDB(name),
                new JumpPointDB(),
                new PositionDB(0, 0, 0),
            });
            return jump;
        }

        void Befriend(int leftId, int rightId)
        {
            FactionOf(leftId).Stances[rightId] = FactionStance.Friendly;
            FactionOf(rightId).Stances[leftId] = FactionStance.Friendly;
        }

        void Fund(FactionInfoDB info, decimal amount)
        {
            if (info.Money.GetCurrentFunds() < amount)
                info.Money.AddIncome(_game.TimePulse.GameGlobalDateTime, TransactionCategory.InitialInvestment, "test", amount);
        }

        FactionInfoDB FactionOf(int factionId)
        {
            Assert.That(_game.Factions[factionId].TryGetDataBlob<FactionInfoDB>(out var info), Is.True);
            return info!;
        }

        Entity OtherFaction(int sessionFactionId)
            => _game.Factions.Values.First(f =>
                f.Id != sessionFactionId
                && f.Id != _game.GameMasterFaction.Id
                && f.Id != Game.NeutralFactionId);

        Entity ExtraFaction(params int[] exclude)
        {
            var existing = _game.Factions.Values.FirstOrDefault(f =>
                !exclude.Contains(f.Id)
                && f.Id != _game.GameMasterFaction.Id
                && f.Id != Game.NeutralFactionId);
            if (existing != null)
                return existing;
            return FactionFactory.CreateFaction(_game, "Chart Buyer " + (_game.Factions.Count + 1));
        }

        Entity MakeColony(int factionId)
        {
            var faction = _game.Factions[factionId];
            var species = SpeciesFactory.CreateSpeciesHuman(faction, _game.GlobalManager);
            var planet = _game.Systems[0].GetAllEntitiesWithDataBlob<SystemBodyInfoDB>()
                .First(b => b.HasDataBlob<MassVolumeDB>() && b.HasDataBlob<NameDB>());
            return ColonyFactory.CreateColony(faction, species, planet, initialPopulation: 9_000_000);
        }
    }
}
