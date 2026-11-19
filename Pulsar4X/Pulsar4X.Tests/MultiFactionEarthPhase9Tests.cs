using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Factions;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>Multi-faction phase 9: runtime NPC companies.</summary>
[TestFixture]
[NonParallelizable]
public class MultiFactionEarthPhase9Tests
{
    const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";
    const string Iron = "iron";

    Game _game = null!;
    Entity _player = null!;
    Entity _amc = null!;
    StarSystem _sol = null!;
    DateTime Now => _game.TimePulse.GameGlobalDateTime;

    [SetUp]
    public void SetUp()
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods.Where(m => m.Mod.ModName == "Pulsar4x").Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), WarpTechStart);
        var result = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345678));
        Assert.That(result, Is.Not.Null);
        (_game, _player) = (result!.Value.game, result.Value.faction);
        _game.Settings.EnforceSingleThread = true;
        _amc = Named("Americas Compact");
        _sol = (StarSystem)_amc.Manager!;
    }

    List<Entity> Colonies() => _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>()).ToList();
    Entity Named(string name) => Colonies().Single(c => c.GetDataBlob<NameDB>().DefaultName == name);
    static FactionInfoDB Info(Entity c) => c.GetFactionOwner.GetDataBlob<FactionInfoDB>();
    static long Pop(Entity c) => c.GetDataBlob<ColonyInfoDB>().Population.Values.Sum();
    static decimal Funds(Entity c) => Info(c).Money.GetCurrentFunds();

    /// <summary>Charter a company for iron, skipping the 180-day clock but still applying caps, funds and the kit.</summary>
    Entity ForceSpawn(string cargoId = Iron, Entity? sponsor = null)
    {
        var colony = FactionSpawner.TrySpawn(_game, _sol, cargoId, sponsor ?? _amc, Now);
        Assert.That(colony, Is.Not.Null, "forced spawn");
        return colony!;
    }

    [Test]
    public void ForcedSpawn_SeatsAnIndustryHead_WhoIsNotTheChairHolder()
    {
        var colony = ForceSpawn();

        Assert.That(GameEngine.People.IndustrySeat.TryGetHead(colony, out var head), Is.True, "a spawned company starts with a head");
        Assert.That(head.FactionOwnerID, Is.EqualTo(colony.FactionOwnerID));
        var chair = colony.GetDataBlob<GameEngine.People.AdminSpaceDB>().CommanderSeats.First(seat => seat.CommanderID >= 0);
        Assert.That(chair.CommanderID, Is.Not.EqualTo(head.Id), "the head and the chair holder are different people");
    }

    [Test]
    public void ForcedSpawn_ChartersACompany_WithTheKitAndSponsorTech_AndPublishesTheEvent()
    {
        long popBefore = Pop(_amc);
        decimal fundsBefore = Funds(_amc);
        var seen = new List<Event>();
        void OnEvent(Event e) => seen.Add(e);
        EventManager.Instance.Subscribe(EventType.NpcCompanySpawned, OnEvent);
        try
        {
            var colony = ForceSpawn();
            var company = colony.GetFactionOwner;
            var info = company.GetDataBlob<FactionInfoDB>();

            Assert.That(company.GetDataBlob<NameDB>().DefaultName, Is.EqualTo("Americas Iron Co."));
            Assert.That(Pop(colony), Is.EqualTo(FactionSpawner.SeedPeople));
            Assert.That(Pop(_amc), Is.EqualTo(popBefore - FactionSpawner.SeedPeople));
            Assert.That(Funds(company), Is.EqualTo(FactionSpawner.SeedFunds));
            Assert.That(Funds(_amc), Is.EqualTo(fundsBefore - FactionSpawner.SeedFunds));
            Assert.That(info.SponsorFactionId, Is.EqualTo(_amc.FactionOwnerID));
            Assert.That(info.SpawnedGood, Is.EqualTo(Iron));
            Assert.That(info.AutoHires, Is.True);
            Assert.That(info.Data.Techs["tech-mining-production"].Level,
                Is.EqualTo(Info(_amc).Data.Techs["tech-mining-production"].Level));
            Assert.That(info.Stances[_player.Id], Is.EqualTo(FactionStance.Friendly));
            Assert.That(Info(_player).Stances[company.Id], Is.EqualTo(FactionStance.Friendly));
            Assert.That(FactionStanceRules.CanTrade(info, _player.Id, Info(_player), company.Id), Is.True);
            Assert.That(MarketBook.TryGet(colony, Iron, out var listing), Is.True);
            Assert.That(listing.Ask, Is.GreaterThan(0));

            Assert.That(seen, Has.Count.EqualTo(1));
            Assert.That(seen[0].Message, Does.Contain("Americas Compact has chartered Americas Iron Co."));
            Assert.That(seen[0].EntityId, Is.EqualTo(colony.Id));
        }
        finally
        {
            EventManager.Instance.Unsubscribe(EventType.NpcCompanySpawned, OnEvent);
        }
    }

    [Test]
    public void MonthlyCheck_Spawns_WhenUnmetPremiumDemandHasLasted180Days_AndThereIsNoSeller()
    {
        PrepareUnmetIron(_amc, Now.AddDays(-FactionSpawner.UnmetDays));
        _game.NpcCompanies.UnmetSince[_sol.ID + ":" + Iron] = Now.AddDays(-FactionSpawner.UnmetDays);

        FactionSpawner.TickSystem(_game, _sol, Now);

        var company = _game.Factions.Values.SingleOrDefault(f => f.GetDataBlob<FactionInfoDB>().SponsorFactionId == _amc.FactionOwnerID);
        Assert.That(company, Is.Not.Null, "a company is chartered after 180 days of unmet premium demand");
        Assert.That(company!.GetDataBlob<NameDB>().DefaultName, Is.EqualTo("Americas Iron Co."));
    }

    [Test]
    public void MonthlyCheck_DoesNotSpawn_WhenASellerListsTheGood()
    {
        PrepareUnmetIron(_amc, Now.AddDays(-FactionSpawner.UnmetDays));
        _game.NpcCompanies.UnmetSince[_sol.ID + ":" + Iron] = Now.AddDays(-FactionSpawner.UnmetDays);
        // AMC also sells iron, which is the usual start; make sure a seller is present.
        MarketBook.SetListing(_amc, new MarketListing { CargoId = Iron, SellQuantity = 100, Ask = 10 });

        FactionSpawner.TickSystem(_game, _sol, Now);

        Assert.That(_game.Factions.Values.Any(f => f.GetDataBlob<FactionInfoDB>().SponsorFactionId == _amc.FactionOwnerID), Is.False);
    }

    [Test]
    public void Fold_AfterSixMonthsBrokeWithNoStock_ReturnsTheColonyToTheSponsor()
    {
        var colony = ForceSpawn();
        var company = colony.GetFactionOwner;
        int companyId = company.Id;
        var info = company.GetDataBlob<FactionInfoDB>();
        Drain(info, colony);

        var record = _game.NpcCompanies.Spawned.Single(s => s.FactionId == companyId);
        record.BrokeSince = Now.AddDays(-FactionSpawner.FoldDays);

        var designs = _player.GetDataBlob<FactionInfoDB>().ShipDesigns;
        var design = designs.ContainsKey("default-ship-design-freighter")
            ? designs["default-ship-design-freighter"]
            : designs.Values.First();
        var ship = ShipFactory.CreateShip(design, company, colony.GetDataBlob<ColonyInfoDB>().PlanetEntity, "Folded Packet");
        Assert.That(ship.FactionOwnerID, Is.EqualTo(companyId));

        var workforce = _amc.GetDataBlob<ColonyWorkforceDB>();
        workforce.Commuters.TryGetValue(_amc.FactionOwnerID, out var commutersBefore);
        workforce.Commuters[companyId] = 250;

        var seen = new List<Event>();
        void OnEvent(Event e) => seen.Add(e);
        EventManager.Instance.Subscribe(EventType.NpcCompanyFolded, OnEvent);
        try
        {
            FactionSpawner.TickSystem(_game, _sol, Now);
            Assert.That(_game.Factions.ContainsKey(companyId), Is.False);
            Assert.That(colony.FactionOwnerID, Is.EqualTo(_amc.FactionOwnerID));
            Assert.That(Info(_amc).Colonies, Does.Contain(colony));
            Assert.That(ship.FactionOwnerID, Is.EqualTo(_amc.FactionOwnerID), "folded company's ship is rehomed");
            Assert.That(workforce.Commuters.ContainsKey(companyId), Is.False, "no commuter entry for the folded faction");
            workforce.Commuters.TryGetValue(_amc.FactionOwnerID, out var commutersAfter);
            Assert.That(commutersAfter, Is.EqualTo(commutersBefore + 250));
            Assert.That(seen, Has.Count.EqualTo(1));
            Assert.That(seen[0].Message, Does.Contain("folded back into Americas Compact"));
        }
        finally
        {
            EventManager.Instance.Unsubscribe(EventType.NpcCompanyFolded, OnEvent);
        }
    }

    [Test]
    public void MaskExhaustion_DoesNotSpawnOrTakePeopleOrFunds()
    {
        long people = Pop(_amc);
        decimal funds = Funds(_amc);
        int factions = _game.Factions.Count;
        // Mask indexes are never recycled; once the next index hits the headroom gate, spawning must stop.
        _game.NextFactionMaskIndex = Game.MaxFactions - FactionSpawner.FactionHeadroom;
        var colony = FactionSpawner.TrySpawn(_game, _sol, Iron, _amc, Now);
        Assert.That(colony, Is.Null, "no spawn when mask slots are exhausted");
        Assert.That(_game.Factions.Count, Is.EqualTo(factions));
        Assert.That(Pop(_amc), Is.EqualTo(people), "people were not taken");
        Assert.That(Funds(_amc), Is.EqualTo(funds), "funds were not taken");
    }

    [Test]
    public void Caps_OnePerSystemPerSixMonths_AndTheGameLimit_AndMaxFactionsMinusFour()
    {
        var first = ForceSpawn(Iron, _amc);
        Assert.That(FactionSpawner.TrySpawn(_game, _sol, "copper", Named("Sino-Pacific Coalition"), Now), Is.Null, "one per system per six months");

        _game.NpcCompanies.LastSpawnBySystem[_sol.ID] = Now.AddDays(-FactionSpawner.CooldownDays);
        _game.Settings.MaxSpawnedFactions = 1;
        Assert.That(FactionSpawner.TrySpawn(_game, _sol, "copper", Named("Sino-Pacific Coalition"), Now), Is.Null, "game cap");

        _game.Settings.MaxSpawnedFactions = 6;
        // Simulate the mask headroom cap without creating 28 factions: count is already well below,
        // so temporarily pretend Factions is full by filling dummy ids up to MaxFactions-4.
        int headroomFloor = Game.MaxFactions - FactionSpawner.FactionHeadroom;
        var added = new List<int>();
        int next = _game.Factions.Keys.Max() + 1;
        while (_game.Factions.Count < headroomFloor)
        {
            _game.Factions[next] = _player;
            added.Add(next);
            next++;
        }
        try
        {
            Assert.That(FactionSpawner.TrySpawn(_game, _sol, "copper", Named("Sino-Pacific Coalition"), Now), Is.Null, "MaxFactions-4");
        }
        finally
        {
            foreach (var id in added)
                _game.Factions.Remove(id);
        }

        // With the dummies gone and the cooldown cleared, a second charter is allowed.
        var second = FactionSpawner.TrySpawn(_game, _sol, "copper", Named("Sino-Pacific Coalition"), Now);
        Assert.That(second, Is.Not.Null, "second spawn after cooldown and with cap room");
        Assert.That(second!.GetFactionOwner.GetDataBlob<NameDB>().DefaultName, Is.EqualTo("Sino-Pacific Copper Co."));
        Assert.That(first.FactionOwnerID, Is.Not.EqualTo(second.FactionOwnerID));
    }

    [Test]
    public void SaveRoundTrip_KeepsTheSpawnedCompany()
    {
        var colony = ForceSpawn();
        var company = colony.GetFactionOwner;
        int id = company.Id;
        string json = Game.Save(_game);
        int next = EntityIDGenerator.NextId;
        var loaded = Game.Load(json);
        EntityIDGenerator.NextId = Math.Max(next, EntityIDGenerator.NextId);

        Assert.That(loaded.Factions.ContainsKey(id), Is.True);
        var loadedCompany = loaded.Factions[id];
        var info = loadedCompany.GetDataBlob<FactionInfoDB>();
        Assert.That(loadedCompany.GetDataBlob<NameDB>().DefaultName, Is.EqualTo("Americas Iron Co."));
        Assert.That(info.SponsorFactionId, Is.EqualTo(_amc.FactionOwnerID));
        Assert.That(info.SpawnedGood, Is.EqualTo(Iron));
        Assert.That(loaded.NpcCompanies.Spawned, Has.Count.EqualTo(1));
        Assert.That(loaded.NpcCompanies.Spawned[0].FactionId, Is.EqualTo(id));
        Assert.That(info.Colonies, Has.Count.EqualTo(1));
        Assert.That(Pop(info.Colonies[0]), Is.EqualTo(FactionSpawner.SeedPeople));
    }

    void PrepareUnmetIron(Entity buyer, DateTime shortSince)
    {
        // Strip every iron sale in the system so the good has no seller.
        foreach (var colony in Colonies())
        {
            if (MarketBook.TryGet(colony, Iron, out var listing) && listing.SellQuantity > 0)
            {
                listing.SellQuantity = 0;
                MarketBook.SetListing(colony, listing);
            }
        }
        var policy = _amc.GetDataBlob<ColonyMarketPolicyDB>();
        if (!policy.Rows.TryGetValue(Iron, out var row))
        {
            row = new MarketPolicyRow { CargoId = Iron, BidFactor = 0.75, UsePerDay = 1 };
            policy.Rows[Iron] = row;
        }
        row.BidFactor = 0.75;
        row.ShortSince = shortSince;
        MarketBook.SetListing(_amc, new MarketListing { CargoId = Iron, BuyQuantity = 10_000, Bid = 1 });
    }

    static void Drain(FactionInfoDB info, Entity colony)
    {
        var funds = info.Money.GetCurrentFunds();
        if (funds > 0)
            info.Money.AddExpense(colony.StarSysDateTime, TransactionCategory.Charter, "test drain", funds);
        if (colony.TryGetDataBlob<CargoStorageDB>(out var storage))
        {
            foreach (var cargo in info.Data.CargoGoods.GetAll().Values.ToList())
            {
                long have = storage.GetUnitsStored(cargo, includeEscro: false);
                if (have > 0)
                    storage.RemoveCargoByUnit(cargo, have);
            }
        }
    }

    [Test]
    public void Fold_RehomesTheIndustryHeadAndChairHolder_ToTheSponsor()
    {
        var colony = ForceSpawn();
        var company = colony.GetFactionOwner;
        int companyId = company.Id;
        var info = company.GetDataBlob<FactionInfoDB>();
        Assert.That(GameEngine.People.IndustrySeat.TryGetHead(colony, out var head), Is.True);
        var chair = colony.GetDataBlob<GameEngine.People.AdminSpaceDB>().CommanderSeats.First(s => s.CommanderID >= 0);
        Assert.That(_game.GlobalManager.TryGetGlobalEntityById(chair.CommanderID, out var chairHolder) || _sol.TryGetGlobalEntityById(chair.CommanderID, out chairHolder), Is.True);
        Drain(info, colony);
        _game.NpcCompanies.Spawned.Single(s => s.FactionId == companyId).BrokeSince = Now.AddDays(-FactionSpawner.FoldDays);

        FactionSpawner.TickSystem(_game, _sol, Now);

        Assert.That(_game.Factions.ContainsKey(companyId), Is.False);
        int sponsorId = _amc.FactionOwnerID;
        Assert.That(GameEngine.People.IndustrySeat.HasHead(colony), Is.True, "the head is still seated after the fold");
        Assert.That(GameEngine.People.IndustrySeat.TryGetHead(colony, out var headAfter), Is.True);
        Assert.That(headAfter.Id, Is.EqualTo(head.Id));
        Assert.That(head.FactionOwnerID, Is.EqualTo(sponsorId), "the head now works for the sponsor");
        Assert.That(chairHolder!.FactionOwnerID, Is.EqualTo(sponsorId), "as does the chair holder");
        Assert.That(Info(_amc).Commanders, Does.Contain(head));
        Assert.That(Info(_amc).Commanders, Does.Contain(chairHolder));
        Assert.That(head.GetDataBlob<Pulsar4X.People.CommanderDB>().AssignedTo, Is.EqualTo(colony.Id));
        Assert.That(colony.GetDataBlob<GameEngine.People.AdminSpaceDB>().CommanderSeats.Select(s => s.CommanderID), Does.Contain(chairHolder.Id));

        // The sponsor can now lose the head cleanly: the seat empties and the sponsor's NPC refills it.
        Pulsar4X.People.CommanderFactory.DestroyCommander(head);
        Assert.That(GameEngine.People.IndustrySeat.HasHead(colony), Is.False);
    }

    [Test]
    public void Fold_ClearsTheStockLevels_SoTheSponsorsJobsRepeatAsPlainJobs()
    {
        var colony = ForceSpawn();
        var company = colony.GetFactionOwner;
        int companyId = company.Id;
        var info = company.GetDataBlob<FactionInfoDB>();
        // The NPC colony has no industry of its own: give it one refining line and a repeat job on it.
        if (!colony.HasDataBlob<Pulsar4X.Industry.IndustryAbilityDB>())
            colony.SetDataBlob(new Pulsar4X.Industry.IndustryAbilityDB(new Dictionary<string, Pulsar4X.Industry.IndustryAbilityDB.ProductionLine>
            {
                ["refinery-line"] = new() { Name = "Refinery", IndustryTypeRates = new() { ["refining"] = 100 } },
            }));
        const string lineId = "refinery-line";
        var plastic = (Pulsar4X.Interfaces.IConstructableDesign)info.Data.CargoGoods.GetAny("plastic")!;
        var job = new Pulsar4X.Industry.IndustryJob(plastic);
        job.InitialiseJob(1, true);
        Pulsar4X.Industry.IndustryTools.AddJob(colony, lineId, job);
        job.StockThreshold = 1_000_000;
        Drain(info, colony);
        _game.NpcCompanies.Spawned.Single(s => s.FactionId == companyId).BrokeSince = Now.AddDays(-FactionSpawner.FoldDays);

        FactionSpawner.TickSystem(_game, _sol, Now);

        Assert.That(_game.Factions.ContainsKey(companyId), Is.False);
        Assert.That(job.StockThreshold, Is.Null);
        Assert.That(job.Auto, Is.True, "it carries on as a plain repeat job");
        Assert.That(colony.GetDataBlob<GameEngine.Engine.Orders.ShipLogDB>().Entries.Select(e => e.Text),
            Has.Some.Contains("lost its stock level because the colony changed hands; it now repeats as a plain job"));
    }
}
