using System.Linq;
using GameEngine.Engine.Orders;
using GameEngine.People;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Logistics;
using Pulsar4X.People;
using Pulsar4X.Storage;
using NameDB = Pulsar4X.Names.NameDB;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class ApiMarketViewTests : ApiTestBase
    {
        [Test]
        public void OwnedColony_ProjectsTheIronListing()
        {
            var session = Connect();
            var colony = MakeColony(session.FactionId);
            ListIron(colony, session.FactionId, stock: 250, sell: 150, ask: 11m, buy: 40, bid: 7m, reserve: 100);

            var view = _projector.ProjectEntity(colony, session.FactionId).GetView<MarketView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view!.Capacity, Is.EqualTo(5));
            Assert.That(view.Goods, Has.Count.EqualTo(2));

            var iron = view.Goods[0];
            Assert.That(iron.CargoId, Is.EqualTo("iron"));
            Assert.That(iron.Name, Is.EqualTo("Iron"));
            Assert.That(iron.Stock, Is.EqualTo(250));
            Assert.That(iron.Reserve, Is.EqualTo(100));
            Assert.That(iron.SellQuantity, Is.EqualTo(150));
            Assert.That(iron.Ask, Is.EqualTo(11m));
            Assert.That(iron.BuyQuantity, Is.EqualTo(40));
            Assert.That(iron.Bid, Is.EqualTo(7m));

            var unknown = view.Goods[1];
            Assert.That(unknown.CargoId, Is.EqualTo("unknown-good"));
            Assert.That(unknown.Name, Is.EqualTo("unknown-good"));
            Assert.That(unknown.Stock, Is.EqualTo(0));
            Assert.That(unknown.SellQuantity, Is.EqualTo(1));
            Assert.That(unknown.BuyQuantity, Is.EqualTo(2));
        }

        [Test]
        public void MutuallyFriendly_SeesTheSameBook()
        {
            var session = Connect();
            var colony = MakeColony(session.FactionId);
            ListIron(colony, session.FactionId, stock: 250, sell: 150, ask: 11m, buy: 40, bid: 7m, reserve: 100);

            var other = OtherFaction(session.FactionId);
            Assert.That(other.TryGetDataBlob<FactionInfoDB>(out var otherInfo), Is.True);
            Assert.That(_game.Factions[session.FactionId].TryGetDataBlob<FactionInfoDB>(out var ownerInfo), Is.True);
            otherInfo!.Data.Unlock("iron");
            otherInfo.Stances[session.FactionId] = FactionStance.Friendly;
            ownerInfo!.Stances[other.Id] = FactionStance.Friendly;

            var owned = _projector.ProjectEntity(colony, session.FactionId);
            var friendly = _projector.ProjectEntity(colony, other.Id);
            Assert.That(friendly.Relation, Is.EqualTo(OwnerRelation.Friendly));
            Assert.That(friendly.GetView<MarketView>(), Is.Not.Null);
            AssertSame(owned.GetView<MarketView>()!, friendly.GetView<MarketView>()!);

            otherInfo.Stances[session.FactionId] = FactionStance.Allied;
            ownerInfo.Stances[other.Id] = FactionStance.Allied;
            var allied = _projector.ProjectEntity(colony, other.Id);
            Assert.That(allied.Relation, Is.EqualTo(OwnerRelation.Friendly));
            Assert.That(allied.GetView<MarketView>(), Is.Not.Null);
            AssertSame(owned.GetView<MarketView>()!, allied.GetView<MarketView>()!);
        }

        [Test]
        public void Hostile_AndDiplomaticNeutral_HaveNoMarketView()
        {
            var session = Connect();
            var colony = MakeColony(session.FactionId);
            ListIron(colony, session.FactionId, stock: 10, sell: 4, ask: 3m, buy: 2, bid: 1m, reserve: 1);

            var other = OtherFaction(session.FactionId);
            var hostile = _projector.ProjectEntity(colony, other.Id);
            Assert.That(hostile.Relation, Is.EqualTo(OwnerRelation.Hostile));
            Assert.That(hostile.HasView<MarketView>(), Is.False);

            Assert.That(other.TryGetDataBlob<FactionInfoDB>(out var otherInfo), Is.True);
            otherInfo!.Stances[session.FactionId] = FactionStance.Neutral;
            var neutral = _projector.ProjectEntity(colony, other.Id);
            Assert.That(neutral.Relation, Is.EqualTo(OwnerRelation.Neutral));
            Assert.That(neutral.HasView<MarketView>(), Is.False);
        }

        [Test]
        public void ColonyWithoutAnOffice_HasNoMarketView()
        {
            var session = Connect();
            var colony = MakeColony(session.FactionId);
            Assert.That(colony.HasDataBlob<LogiBaseDB>(), Is.False);
            var snapshot = _projector.ProjectEntity(colony, session.FactionId);
            Assert.That(snapshot.Relation, Is.EqualTo(OwnerRelation.Owned));
            Assert.That(snapshot.HasView<MarketView>(), Is.False);
        }

        [Test]
        public void OwnedOffice_CanEdit_AndHidesListedCargo()
        {
            var session = Connect();
            var colony = MakeColony(session.FactionId);
            ListIron(colony, session.FactionId, stock: 250, sell: 150, ask: 11m, buy: 40, bid: 7m, reserve: 100);
            FactionOf(session.FactionId).Data.Unlock("copper");

            var seated = new AdminSpaceDB();
            seated.CommanderSeats.Add(new AdminSpaceAbilityState(AdminLevel.Colony, "City Hall") { CommanderID = 4 });
            colony.SetDataBlob(seated);

            var view = _projector.ProjectEntity(colony, session.FactionId).GetView<MarketView>();
            Assert.That(view!.CanEdit, Is.True);
            Assert.That(view.Addable.Any(c => c.CargoId == "iron"), Is.False);
            Assert.That(view.Addable.Any(c => c.CargoId == "copper"), Is.True);
        }

        [Test]
        public void FriendlyViewer_CannotEdit()
        {
            var session = Connect();
            var colony = MakeColony(session.FactionId);
            ListIron(colony, session.FactionId, stock: 10, sell: 4, ask: 3m, buy: 2, bid: 1m, reserve: 1);
            var other = OtherFaction(session.FactionId);
            Assert.That(other.TryGetDataBlob<FactionInfoDB>(out var otherInfo), Is.True);
            otherInfo!.Data.Unlock("iron");
            otherInfo.Data.Unlock("copper");
            otherInfo.Stances[session.FactionId] = FactionStance.Friendly;
            FactionOf(session.FactionId).Stances[other.Id] = FactionStance.Friendly;

            var view = _projector.ProjectEntity(colony, other.Id).GetView<MarketView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view!.CanEdit, Is.False);
            Assert.That(view.Addable, Is.Empty);
        }

        [Test]
        public void SetListing_PostsTheBook_AndUpdatesPolicy()
        {
            var session = Connect();
            var colony = MakeColony(session.FactionId);
            OfficeWithIron(colony, session.FactionId, stock: 250);
            var policy = new ColonyMarketPolicyDB();
            policy.Rows["iron"] = new MarketPolicyRow
            {
                CargoId = "iron",
                Min = 1,
                Max = 500,
                AutoProduce = true,
                Ask = 1,
                Bid = 1,
            };
            colony.SetDataBlob(policy);

            var commands = new CommandTranslator(_game);
            var faction = _game.Factions[session.FactionId];
            var set = commands.Translate(faction, colony, new SetMarketListingCommand(
                colony.Id, "iron", 1, 11m, 2, 7m, 100));
            Assert.That(set.Accepted, Is.True);

            Assert.That(MarketBook.TryGet(colony, "iron", out var listing), Is.True);
            Assert.That(listing.SellQuantity, Is.EqualTo(1));
            Assert.That(listing.BuyQuantity, Is.EqualTo(2));
            Assert.That(listing.Ask, Is.EqualTo(11m));
            Assert.That(listing.Bid, Is.EqualTo(7m));
            Assert.That(listing.Reserve, Is.EqualTo(100));

            var row = policy.Rows["iron"];
            Assert.That(row.Min, Is.EqualTo(100));
            Assert.That(row.Ask, Is.EqualTo(11m));
            Assert.That(row.Bid, Is.EqualTo(7m));
            Assert.That(row.Max, Is.EqualTo(500));
            Assert.That(row.AutoProduce, Is.True);

            var replaced = commands.Translate(faction, colony, new SetMarketListingCommand(
                colony.Id, "iron", 9, 12m, 0, 8m, 600));
            Assert.That(replaced.Accepted, Is.True);
            Assert.That(MarketBook.TryGet(colony, "iron", out listing), Is.True);
            Assert.That(listing.SellQuantity, Is.EqualTo(9));
            Assert.That(row.Min, Is.EqualTo(600));
            Assert.That(row.Max, Is.EqualTo(600));
            Assert.That(row.AutoProduce, Is.True);
        }

        [Test]
        public void SetListing_NewRow_DoesNotAutoProduce()
        {
            var session = Connect();
            var colony = MakeColony(session.FactionId);
            OfficeWithIron(colony, session.FactionId, stock: 40);

            var set = new CommandTranslator(_game).Translate(
                _game.Factions[session.FactionId], colony,
                new SetMarketListingCommand(colony.Id, "iron", 0, 5m, 10, 3m, 40));
            Assert.That(set.Accepted, Is.True);

            Assert.That(colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy), Is.True);
            var row = policy!.Rows["iron"];
            Assert.That(row.Min, Is.EqualTo(40));
            Assert.That(row.Max, Is.EqualTo(40));
            Assert.That(row.AutoProduce, Is.False);
            Assert.That(row.Ask, Is.EqualTo(5m));
            Assert.That(row.Bid, Is.EqualTo(3m));
        }

        [Test]
        public void RunMarketWake_KeepsPricesAndReserve()
        {
            var session = Connect();
            var colony = MakeColony(session.FactionId);
            OfficeWithIron(colony, session.FactionId, stock: 250);
            var commands = new CommandTranslator(_game);
            var faction = _game.Factions[session.FactionId];
            Assert.That(commands.Translate(faction, colony,
                new SetMarketListingCommand(colony.Id, "iron", 1, 11m, 2, 7m, 100)).Accepted, Is.True);
            Assert.That(commands.Translate(faction, colony, new RunMarketCommand(colony.Id)).Accepted, Is.True);

            AgentProcessor.RunAgentNow(colony);

            Assert.That(MarketBook.TryGet(colony, "iron", out var listing), Is.True);
            Assert.That(listing.Ask, Is.EqualTo(11m));
            Assert.That(listing.Bid, Is.EqualTo(7m));
            Assert.That(listing.Reserve, Is.EqualTo(100));
            Assert.That(listing.SellQuantity, Is.EqualTo(150));
            Assert.That(listing.BuyQuantity, Is.EqualTo(0));
        }

        [Test]
        public void ClearListing_DropsTheBookAndThePolicy()
        {
            var session = Connect();
            var colony = MakeColony(session.FactionId);
            OfficeWithIron(colony, session.FactionId, stock: 250);
            var commands = new CommandTranslator(_game);
            var faction = _game.Factions[session.FactionId];
            Assert.That(commands.Translate(faction, colony,
                new ClearMarketListingCommand(colony.Id, "iron")).Accepted, Is.True);
            Assert.That(commands.Translate(faction, colony,
                new SetMarketListingCommand(colony.Id, "iron", 1, 11m, 2, 7m, 100)).Accepted, Is.True);
            Assert.That(commands.Translate(faction, colony,
                new ClearMarketListingCommand(colony.Id, "iron")).Accepted, Is.True);

            Assert.That(MarketBook.TryGet(colony, "iron", out _), Is.False);
            Assert.That(colony.TryGetDataBlob<ColonyMarketPolicyDB>(out var policy), Is.True);
            Assert.That(policy!.Rows.ContainsKey("iron"), Is.False);

            Assert.That(commands.Translate(faction, colony, new RunMarketCommand(colony.Id)).Accepted, Is.True);
            AgentProcessor.RunAgentNow(colony);
            Assert.That(MarketBook.TryGet(colony, "iron", out _), Is.False);
        }

        [Test]
        public void SetListing_RejectsCapacity_UnknownCargo_AndNoOffice()
        {
            var session = Connect();
            var colony = MakeColony(session.FactionId);
            var commands = new CommandTranslator(_game);
            var faction = _game.Factions[session.FactionId];

            var noOffice = commands.Translate(faction, colony,
                new SetMarketListingCommand(colony.Id, "iron", 0, 1m, 0, 1m, 0));
            Assert.That(noOffice.Accepted, Is.False);
            Assert.That(noOffice.RejectionReason, Is.EqualTo("The colony has no logistics office."));

            OfficeWithIron(colony, session.FactionId, stock: 10);
            colony.GetDataBlob<LogiBaseDB>().Capacity = 1;
            Assert.That(commands.Translate(faction, colony,
                new SetMarketListingCommand(colony.Id, "iron", 1, 2m, 0, 1m, 0)).Accepted, Is.True);
            FactionOf(session.FactionId).Data.Unlock("copper");
            var full = commands.Translate(faction, colony,
                new SetMarketListingCommand(colony.Id, "copper", 1, 2m, 0, 1m, 0));
            Assert.That(full.Accepted, Is.False);
            Assert.That(full.RejectionReason, Is.EqualTo("The logistics office is at capacity."));
            Assert.That(MarketBook.TryGet(colony, "copper", out _), Is.False);

            var unknown = commands.Translate(faction, colony,
                new SetMarketListingCommand(colony.Id, "unknown-good", 1, 1m, 0, 1m, 0));
            Assert.That(unknown.Accepted, Is.False);
            Assert.That(unknown.RejectionReason, Is.EqualTo("Unknown cargo id."));

            var negative = commands.Translate(faction, colony,
                new SetMarketListingCommand(colony.Id, "iron", -1, 1m, 0, 1m, 0));
            Assert.That(negative.Accepted, Is.False);
            Assert.That(negative.RejectionReason, Is.EqualTo("Prices and quantities cannot be negative."));
        }

        void OfficeWithIron(Entity colony, int factionId, long stock)
        {
            var info = FactionOf(factionId);
            info.Data.Unlock("iron");
            var store = new CargoStorageDB("general-storage", 100000);
            colony.SetDataBlob(store);
            var iron = info.Data.CargoGoods.GetAny("iron");
            Assert.That(iron, Is.Not.Null);
            Assert.That(store.AddCargoByUnit(iron!, stock), Is.EqualTo(stock));
            colony.SetDataBlob(new LogiBaseDB { Capacity = 5 });
        }

        FactionInfoDB FactionOf(int factionId)
        {
            Assert.That(_game.Factions[factionId].TryGetDataBlob<FactionInfoDB>(out var info), Is.True);
            return info!;
        }

        void ListIron(Entity colony, int factionId, long stock, long sell, decimal ask, long buy, decimal bid, long reserve)
        {
            var faction = _game.Factions[factionId];
            Assert.That(faction.TryGetDataBlob<FactionInfoDB>(out var info), Is.True);
            info!.Data.Unlock("iron");

            var store = new CargoStorageDB("general-storage", 100000);
            colony.SetDataBlob(store);
            var iron = info.Data.CargoGoods.GetAny("iron");
            Assert.That(iron, Is.Not.Null);
            Assert.That(store.AddCargoByUnit(iron!, stock), Is.EqualTo(stock));

            colony.SetDataBlob(new LogiBaseDB { Capacity = 5 });
            Assert.That(MarketBook.SetListing(colony, new MarketListing
            {
                CargoId = "iron",
                SellQuantity = sell,
                Ask = ask,
                BuyQuantity = buy,
                Bid = bid,
                Reserve = reserve,
            }), Is.True);
            Assert.That(MarketBook.SetListing(colony, new MarketListing
            {
                CargoId = "unknown-good",
                SellQuantity = 1,
                Ask = 1,
                BuyQuantity = 2,
                Bid = 1,
                Reserve = 0,
            }), Is.True);
        }

        static void AssertSame(MarketView owned, MarketView other)
        {
            Assert.That(other.Capacity, Is.EqualTo(owned.Capacity));
            Assert.That(other.Goods, Has.Count.EqualTo(owned.Goods.Count));
            for (int i = 0; i < owned.Goods.Count; i++)
            {
                var left = owned.Goods[i];
                var right = other.Goods[i];
                Assert.That(right.CargoId, Is.EqualTo(left.CargoId));
                Assert.That(right.Name, Is.EqualTo(left.Name));
                Assert.That(right.Stock, Is.EqualTo(left.Stock));
                Assert.That(right.Reserve, Is.EqualTo(left.Reserve));
                Assert.That(right.BuyQuantity, Is.EqualTo(left.BuyQuantity));
                Assert.That(right.Bid, Is.EqualTo(left.Bid));
                Assert.That(right.SellQuantity, Is.EqualTo(left.SellQuantity));
                Assert.That(right.Ask, Is.EqualTo(left.Ask));
            }
        }

        Entity OtherFaction(int sessionFactionId)
        {
            return _game.Factions.Values.First(f =>
                f.Id != sessionFactionId
                && f.Id != _game.GameMasterFaction.Id
                && f.Id != Game.NeutralFactionId);
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
