using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// People as passenger cargo: boarding and landing keep the colony's books and the ship's hold
/// in step, and automatic migration picks who leaves and where they go.
/// </summary>
[TestFixture]
public class PassengerTransportTests
{
    [Test]
    public void Boarding_TakesPeopleOffTheColony_AndPutsThemInTheHold()
    {
        var scene = Scene.Build(population: 500, capacity: 1000, berths: 100);

        long boarded = PassengerAdapter.Board(scene.Home, scene.Ship, scene.People, 400);

        Assert.That(boarded, Is.EqualTo(100));
        Assert.That(scene.Population(scene.Home), Is.EqualTo(400));
        Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(100));
    }

    [Test]
    public void Landing_AddsPeopleToTheColony()
    {
        var scene = Scene.Build(population: 500, capacity: 1000, berths: 100);
        PassengerAdapter.Board(scene.Home, scene.Ship, scene.People, 100);

        long landed = PassengerAdapter.Land(scene.Home, scene.Ship, scene.People, 100);

        Assert.That(landed, Is.EqualTo(100));
        Assert.That(scene.Population(scene.Home), Is.EqualTo(500));
        Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(0));
    }

    [Test]
    public void Landing_ClampsAtCapacity_AndKeepsTheRestAboard()
    {
        var scene = Scene.Build(population: 500, capacity: 1000, berths: 100, destPopulation: 980, destCapacity: 1000);
        PassengerAdapter.Board(scene.Home, scene.Ship, scene.People, 100);

        long landed = PassengerAdapter.Land(scene.Dest, scene.Ship, scene.People, 100);

        Assert.That(landed, Is.EqualTo(20));
        Assert.That(scene.Population(scene.Dest), Is.EqualTo(1000));
        Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(80));
        Assert.That(scene.Population(scene.Home) + scene.Population(scene.Dest) + scene.Aboard(scene.Ship),
            Is.EqualTo(500 + 980), "people are conserved");
    }

    [Test]
    public void ShipWithoutPassengerCapacity_GetsNothing_AndTheColonyKeepsItsPeople()
    {
        var scene = Scene.Build(population: 500, capacity: 1000, berths: 0);

        long boarded = PassengerAdapter.Board(scene.Home, scene.Ship, scene.People, 100);

        Assert.That(boarded, Is.EqualTo(0));
        Assert.That(scene.Population(scene.Home), Is.EqualTo(500));
    }

    [Test]
    public void OtherFactionsColony_RefusesPeopleBothWays()
    {
        var scene = Scene.Build(population: 500, capacity: 1000, berths: 100, destPopulation: 0, destCapacity: 1000);
        PassengerAdapter.Board(scene.Home, scene.Ship, scene.People, 10);
        scene.Ship.FactionOwnerID = scene.Other.Id;

        Assert.That(PassengerAdapter.Board(scene.Home, scene.Ship, scene.People, 10), Is.EqualTo(0));
        Assert.That(PassengerAdapter.Land(scene.Dest, scene.Ship, scene.People, 10), Is.EqualTo(0));
        Assert.That(scene.Population(scene.Dest), Is.EqualTo(0));
        Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(10));
    }

    [Test]
    public void LoadThroughEscrow_TakesPeopleOffTheColony_AndLandingPutsThemOnTheNewOne()
    {
        var scene = Scene.Build(population: 500, capacity: 1000, berths: 100, destPopulation: 10, destCapacity: 1000);
        scene.AtHome();

        var (primary, _) = CargoTransferOrder.CreateLoadPair(
            scene.Faction.Id, scene.Ship, scene.Home,
            new List<(ICargoable item, long amount)> { (scene.People, 60) });

        Assert.That(scene.Population(scene.Home), Is.EqualTo(440), "escrow takes them off the colony");
        Assert.That(primary.Taken(scene.People.ID), Is.EqualTo(60));

        Scene.Drain(scene.Home);
        Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(60));
        Assert.That(scene.Population(scene.Home), Is.EqualTo(440), "landing, not boarding, changes the books");

        scene.AtDest();
        CargoTransferOrder.CreateUnloadPair(
            scene.Faction.Id, scene.Ship, scene.Dest,
            new List<(ICargoable item, long amount)> { (scene.People, -60) });
        Scene.Drain(scene.Dest);

        Assert.That(scene.Population(scene.Dest), Is.EqualTo(70));
        Assert.That(scene.Population(scene.Home) + scene.Population(scene.Dest) + scene.Aboard(scene.Ship),
            Is.EqualTo(510), "people are conserved");
        Assert.That(scene.SpeciesAt(scene.Dest), Is.EqualTo(scene.Species.Id));
    }

    [Test]
    public void PartialTransfer_KeepsBothSpeciesExact()
    {
        var scene = Scene.Build(population: 500, capacity: 1000, berths: 40, destPopulation: 0, destCapacity: 1000);
        var second = scene.SecondSpecies(300);
        scene.AtHome();

        var items = new List<(ICargoable, long)>
        {
            (scene.People, 100),
            (second, 100),
        };
        var (primary, _) = CargoTransferOrder.CreateLoadPair(scene.Faction.Id, scene.Ship, scene.Home, items);

        Assert.That(primary.Taken(scene.People.ID), Is.EqualTo(40), "the berths went to the first species");
        Assert.That(primary.Taken(second.ID), Is.EqualTo(0), "the hold is full");

        Scene.Drain(scene.Home);
        var hold = scene.Ship.GetDataBlob<CargoStorageDB>();
        Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(40));
        Assert.That(hold.GetUnitsStored(second, false), Is.EqualTo(0));
        Assert.That(scene.Population(scene.Home), Is.EqualTo(460));
        Assert.That(scene.Population(scene.Home, scene.SecondSpeciesEntity), Is.EqualTo(300),
            "the second species did not move");
    }

    [Test]
    public void BoardingOneSpecies_LeavesTheOtherOnTheColony()
    {
        var scene = Scene.Build(population: 500, capacity: 1000, berths: 100);
        var second = scene.SecondSpecies(300);

        Assert.That(PassengerAdapter.Board(scene.Home, scene.Ship, second, 50), Is.EqualTo(50));
        Assert.That(PassengerAdapter.Board(scene.Home, scene.Ship, second, 500), Is.EqualTo(50),
            "the other fifty berths are all that is left");
        Assert.That(scene.Population(scene.Home), Is.EqualTo(500));
        Assert.That(scene.Population(scene.Home, scene.SecondSpeciesEntity), Is.EqualTo(200));
        Assert.That(scene.Ship.GetDataBlob<CargoStorageDB>().GetUnitsStored(second, false), Is.EqualTo(100));
    }

    [Test]
    public void Cancel_PutsThePeopleBackOnTheColony()
    {
        var scene = Scene.Build(population: 500, capacity: 1000, berths: 100);
        scene.AtHome();

        var (primary, _) = CargoTransferOrder.CreateLoadPair(
            scene.Faction.Id, scene.Ship, scene.Home,
            new List<(ICargoable item, long amount)> { (scene.People, 60) });
        Assert.That(scene.Population(scene.Home), Is.EqualTo(440));

        primary.Stop(scene.Ship.StarSysDateTime);

        Assert.That(scene.Population(scene.Home), Is.EqualTo(500));
        Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(0));
        Assert.That(scene.Home.GetDataBlob<CargoStorageDB>().EscroItems, Is.Empty);
    }

    [Test]
    public void CrossFactionTransfer_EscrowsNothing()
    {
        var scene = Scene.Build(population: 500, capacity: 1000, berths: 100);
        scene.Ship.FactionOwnerID = scene.Other.Id;
        scene.AtHome();

        var (primary, _) = CargoTransferOrder.CreateLoadPair(
            scene.Other.Id, scene.Ship, scene.Home,
            new List<(ICargoable item, long amount)> { (scene.People, 60) });

        Assert.That(primary.Taken(scene.People.ID), Is.EqualTo(0), "nobody was escrowed");
        Assert.That(scene.Population(scene.Home), Is.EqualTo(500));
        Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(0));
    }

    [Test]
    public void MotionlessShip_DoesNotStrandEscrowedPeople()
    {
        var scene = Scene.Build(population: 500, capacity: 1000, berths: 100);
        scene.AtHome();
        scene.Home.GetDataBlob<CargoStorageDB>().TransferRate = 1000;

        var (primary, _) = CargoTransferOrder.CreateLoadPair(
            scene.Faction.Id, scene.Ship, scene.Home,
            new List<(ICargoable item, long amount)> { (scene.People, 60) });
        Scene.Drain(scene.Home);

        Assert.That(scene.Population(scene.Home) + scene.Aboard(scene.Ship), Is.EqualTo(500));
    }

    [Test]
    public void MarketBuy_BoardsPeopleFromTheColonysListing()
    {
        var scene = Scene.Build(population: 500, capacity: 1000, berths: 100);
        scene.Home.GetDataBlob<CargoStorageDB>().TransferRate = 1000;
        MarketBook.SetListing(scene.Home, new MarketListing
        {
            CargoId = scene.People.UniqueID,
            SellQuantity = 200,
            Ask = 0,
        });

        scene.AtHome();
        var action = MarketExchangeAction.Create(
            scene.Ship, scene.Home.Id, scene.People.UniqueID, MarketSide.BuyFromMarket, 40);
        Run(scene, action);

        Assert.That(action.IsFinished(), Is.True);
        Assert.That(scene.Population(scene.Home), Is.EqualTo(460));
        Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(40));
        MarketBook.TryGet(scene.Home, scene.People.UniqueID, out var listing);
        Assert.That(listing.SellQuantity, Is.EqualTo(160));
    }

    [Test]
    public void MarketSell_LandsPeople_ClampedAtCapacity()
    {
        var scene = Scene.Build(population: 500, capacity: 1000, berths: 100, destPopulation: 990, destCapacity: 1000);
        PassengerAdapter.Board(scene.Home, scene.Ship, scene.People, 50);
        scene.AtDest();
        MarketBook.SetListing(scene.Dest, new MarketListing
        {
            CargoId = scene.People.UniqueID,
            BuyQuantity = 100,
            Bid = 0,
        });
        scene.Dest.GetDataBlob<CargoStorageDB>().TransferRate = 1000;

        var action = MarketExchangeAction.Create(
            scene.Ship, scene.Dest.Id, scene.People.UniqueID, MarketSide.SellToMarket, 50);
        Run(scene, action);

        Assert.That(scene.Population(scene.Dest), Is.EqualTo(1000));
        Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(40));
    }

    [Test]
    public void MarketCrossFaction_RefusesColonists()
    {
        var scene = Scene.Build(population: 500, capacity: 1000, berths: 100);
        scene.AtHome();
        var info = scene.Faction.GetDataBlob<FactionInfoDB>();
        // People are already aboard, so the exchange is refused for the faction, not for an empty hold.
        PassengerAdapter.Board(scene.Home, scene.Ship, scene.People, 10);
        var rivalInfo = scene.Other.GetDataBlob<FactionInfoDB>();
        info.Stances[scene.Other.Id] = FactionStance.Friendly;
        rivalInfo.Stances[scene.Faction.Id] = FactionStance.Friendly;
        FactionStanceRules.SetGrant(info, scene.Other.Id, FactionGrants.Trade, true);
        FactionStanceRules.SetGrant(rivalInfo, scene.Faction.Id, FactionGrants.Trade, true);
        // The rival knows the same species under the same id, so only the faction check can refuse.
        SpeciesCargo.Register(rivalInfo.Data, info.Species);
        var other = scene.Colony(scene.Other, new Vector3(1.4e11, 0, 0), 100, 1000);
        MarketBook.SetListing(other, new MarketListing
        {
            CargoId = scene.People.UniqueID,
            BuyQuantity = 100,
            Bid = 1,
        });

        var action = MarketExchangeAction.Create(
            scene.Ship, other.Id, scene.People.UniqueID, MarketSide.SellToMarket, 10);
        Run(scene, action);

        Assert.That(action.IsFinished(), Is.True);
        Assert.That(action.Status, Is.EqualTo(ActionStatus.Failed));
        Assert.That(action.Details, Is.EqualTo("People do not change factions."));
        Assert.That(scene.Population(other), Is.EqualTo(100));
        Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(10), "the passengers stay aboard");
    }

    [Test]
    public void NoPassengerCapacity_ReportsTheReason_AndMovesNobody()
    {
        var scene = Scene.Build(population: 500, capacity: 1000, berths: 0);
        scene.AtHome();
        MarketBook.SetListing(scene.Home, new MarketListing
        {
            CargoId = scene.People.UniqueID,
            SellQuantity = 100,
            Ask = 0,
        });

        var action = MarketExchangeAction.Create(
            scene.Ship, scene.Home.Id, scene.People.UniqueID, MarketSide.BuyFromMarket, 10);
        Run(scene, action);

        Assert.That(action.IsFinished(), Is.True);
        Assert.That(action.Status, Is.Not.EqualTo(ActionStatus.Failed));
        Assert.That(action.Details, Is.EqualTo("The ship has no passenger capacity."));
        Assert.That(scene.Population(scene.Home), Is.EqualTo(500));
        Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(0));
    }

    [Test]
    public void MarketLanding_ClampsAtCapacity_AndLeavesTheRestAboard()
    {
        var scene = Scene.Build(population: 500, capacity: 1000, berths: 100, destPopulation: 990, destCapacity: 1000);
        PassengerAdapter.Board(scene.Home, scene.Ship, scene.People, 50);
        scene.AtDest();
        MarketBook.SetListing(scene.Dest, new MarketListing
        {
            CargoId = scene.People.UniqueID,
            BuyQuantity = 50,
            Bid = 0,
        });

        var action = MarketExchangeAction.Create(
            scene.Ship, scene.Dest.Id, scene.People.UniqueID, MarketSide.SellToMarket, 50);
        Run(scene, action);

        Assert.That(action.IsFinished(), Is.True);
        Assert.That(scene.Population(scene.Dest), Is.EqualTo(1000));
        Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(40));
        Assert.That(scene.Population(scene.Home) + scene.Population(scene.Dest) + scene.Aboard(scene.Ship),
            Is.EqualTo(500 + 990), "people are conserved");
    }

    [Test]
    public void PassengerHoldDesign_IsBuildable_AndGivesBerths()
    {
        var scene = Scene.Build(population: 0, capacity: 1000, berths: 0);
        var info = scene.Faction.GetDataBlob<FactionInfoDB>();
        var mod = scene.Game.StartingGameData;
        bool buildable = info.IndustryDesigns.ContainsKey("default-design-passenger-hold");
        if (!buildable)
        {
            // A start that has not unlocked it yet still must be able to build it from the template.
            Assert.That(
                info.Data.ComponentTemplates.ContainsKey("passenger-hold")
                || info.Data.LockedComponentTemplates.ContainsKey("passenger-hold"),
                Is.True,
                "the passenger-hold template must ship with the mod");
            Assert.That(mod.ComponentDesigns.ContainsKey("default-design-passenger-hold"), Is.True,
                "the passenger-hold design must ship with the mod");
            foreach (var id in new[] { "passenger-hold", "stainless-steel", "aluminium", "plastic" })
                info.Data.Unlock(id);
            ComponentDesigner.StartResearched = true;
            try
            {
                ComponentDesignFromJson.Create(
                    scene.Faction, info.Data, mod.ComponentDesigns["default-design-passenger-hold"]);
            }
            finally
            {
                ComponentDesigner.StartResearched = false;
            }
            buildable = info.IndustryDesigns.ContainsKey("default-design-passenger-hold");
        }

        Assert.That(buildable, Is.True, "the passenger hold must reach the buildable design list");
        Assert.That(info.IndustryDesigns["default-design-passenger-hold"], Is.InstanceOf<ComponentDesign>());
        var component = (ComponentDesign)info.IndustryDesigns["default-design-passenger-hold"];
        Assert.That(component.HasAttribute<CargoStorageAtb>(), Is.True, "a hold stores something");
        var storage = component.GetAttribute<CargoStorageAtb>();
        Assert.That(storage.StoreTypeID, Is.EqualTo(SpeciesCargo.CargoType), "it stores people");
        Assert.That(storage.MaxVolume, Is.GreaterThan(0));
        Assert.That(component.ComponentMountType.HasFlag(ComponentMountType.ShipComponent), Is.True,
            "it mounts on a ship");

        // Put it on the hull. The ship is built with a hold already, so the berths add up.
        var ship = scene.Ship;
        if (!ship.HasDataBlob<ComponentInstancesDB>())
            ship.SetDataBlob(new ComponentInstancesDB());
        var hull = ship.GetDataBlob<CargoStorageDB>();
        long were = hull.GetFreeUnitSpace(scene.People, false);
        ship.AddComponent(component, 1);
        long now = hull.GetFreeUnitSpace(scene.People, false);
        long berths = (long)(storage.MaxVolume / SpeciesCargo.VolumePerPerson);
        Assert.That(now - were, Is.EqualTo(berths), "the hold gives the berths it advertises");
        Assert.That(PassengerAdapter.Room(scene.Home, ship, scene.Species.Id), Is.GreaterThanOrEqualTo(berths));
    }

    [Test]
    public void RepeatedBoardAndLand_KeepsPeopleAndBerthsExact()
    {
        var scene = Scene.Build(population: 5000, capacity: 10000, berths: 100);
        var shipStore = scene.Ship.GetDataBlob<CargoStorageDB>();
        var homeStore = scene.Home.GetDataBlob<CargoStorageDB>();
        double shipMassEmpty = shipStore.TotalStoredMass;
        double berthVolume = shipStore.GetFreeVolume(scene.People);
        double homeMass = homeStore.TotalStoredMass;
        double homeVolume = homeStore.GetFreeVolume("general-storage");
        double perTrip = 25 * SpeciesCargo.MassPerPerson;
        Assert.That(berthVolume, Is.GreaterThan(0));

        for (int trip = 0; trip < 3; trip++)
        {
            long boarded = PassengerAdapter.Board(scene.Home, scene.Ship, scene.People, 25);
            Assert.That(boarded, Is.EqualTo(25));
            Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(25));
            // Boarding is a cargo add on the hold, so the hold's mass and volume move with it.
            Assert.That(shipStore.TotalStoredMass, Is.EqualTo(shipMassEmpty + perTrip).Within(1e-6),
                "the hold carries the mass of the people aboard");
            Assert.That(berthVolume - shipStore.GetFreeVolume(scene.People),
                Is.EqualTo(25 * SpeciesCargo.VolumePerPerson).Within(1e-6),
                "the berths taken match the people aboard");

            scene.AtDest();
            long landed = PassengerAdapter.Land(scene.Dest, scene.Ship, scene.People, 25);
            Assert.That(landed, Is.EqualTo(25));
            Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(0), "landing takes them out of the hold");
            Assert.That(shipStore.TotalStoredMass, Is.EqualTo(shipMassEmpty).Within(1e-6),
                "the hold is back to its empty mass");
            Assert.That(berthVolume - shipStore.GetFreeVolume(scene.People), Is.EqualTo(0).Within(1e-6),
                "every berth is free again");

            Assert.That(homeStore.TotalStoredMass, Is.EqualTo(homeMass).Within(1e-6),
                "people are not warehouse stock, so the colony's stored mass never moves");
            Assert.That(homeStore.GetFreeVolume("general-storage"), Is.EqualTo(homeVolume).Within(1e-6),
                "the colony hold is untouched: boarding takes people off the books, not off a shelf");
            Assert.That(homeStore.GetUnitsStored(scene.People, false), Is.EqualTo(0),
                "a colony never warehouses its own people");
            Assert.That(scene.Population(scene.Home) + scene.Population(scene.Dest) + scene.Aboard(scene.Ship),
                Is.EqualTo(5000), "people are conserved over repeated trips");

            scene.AtHome();
        }

        Assert.That(scene.Population(scene.Dest), Is.EqualTo(75));
        Assert.That(scene.Population(scene.Home), Is.EqualTo(4925));
    }

    [Test]
    public void EscrowTransfer_RepeatedTrips_ConserveMassAndVolume()
    {
        var scene = Scene.Build(population: 5000, capacity: 10000, berths: 100, destPopulation: 0, destCapacity: 10000);
        var shipStore = scene.Ship.GetDataBlob<CargoStorageDB>();
        var homeStore = scene.Home.GetDataBlob<CargoStorageDB>();
        double shipMass = shipStore.TotalStoredMass;
        double berthVolume = shipStore.GetFreeVolume(scene.People);
        double homeMass = homeStore.TotalStoredMass;
        double homeVolume = homeStore.GetFreeVolume("general-storage");
        Assert.That(berthVolume, Is.GreaterThan(0));
        for (int trip = 0; trip < 3; trip++)
        {
            scene.AtHome();
            var (primary, _) = CargoTransferOrder.CreateLoadPair(
                scene.Faction.Id, scene.Ship, scene.Home,
                new List<(ICargoable item, long amount)> { (scene.People, 25) });
            Assert.That(primary.Taken(scene.People.ID), Is.EqualTo(25), "escrow took the people off the colony");
            Assert.That(scene.Population(scene.Home), Is.EqualTo(5000 - 25 * (trip + 1)));

            Scene.Drain(scene.Home);
            Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(25), "the hold took the people");
            Assert.That(scene.Population(scene.Home), Is.EqualTo(5000 - 25 * (trip + 1)));

            scene.AtDest();
            CargoTransferOrder.CreateUnloadPair(
                scene.Faction.Id, scene.Ship, scene.Dest,
                new List<(ICargoable item, long amount)> { (scene.People, -25) });
            Scene.Drain(scene.Dest);

            Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(0), "the hold is empty again");
            Assert.That(shipStore.TotalStoredMass, Is.EqualTo(shipMass).Within(1e-6),
                "the ship's mass is back where it started after an escrow load and unload");
            Assert.That(berthVolume - shipStore.GetFreeVolume(scene.People), Is.EqualTo(0).Within(1e-6),
                "the ship's passenger volume is back where it started");
            Assert.That(homeStore.TotalStoredMass, Is.EqualTo(homeMass).Within(1e-6),
                "people are not warehouse stock, so the colony's stored mass never moves");
            Assert.That(homeStore.GetFreeVolume("general-storage"), Is.EqualTo(homeVolume).Within(1e-6),
                "the colony hold is untouched across escrow trips");
            Assert.That(homeStore.GetUnitsStored(scene.People, false), Is.EqualTo(0),
                "a colony never warehouses its own people");
            Assert.That(scene.Population(scene.Home) + scene.Population(scene.Dest), Is.EqualTo(5000),
                "escrow conserves people over repeated trips");
        }

        Assert.That(scene.Population(scene.Dest), Is.EqualTo(75));
        Assert.That(scene.SpeciesAt(scene.Dest), Is.EqualTo(scene.Species.Id));
    }

    [Test]
    public void Overcrowding_RefreshPostsZeroPriceRelocation_AndTheMarketMovesThePeople()
    {
        // Home is over its support, Dest has room. Both are one faction, one system.
        var scene = Scene.Build(population: 1200, capacity: 1000, berths: 100, destPopulation: 0, destCapacity: 3000);
        Assert.That(MigrationPlanner.Crowded(scene.Home, scene.Species.Id), Is.EqualTo(200),
            "the scene really is overcrowded");

        // The periodic market pass refreshes the books. No hand-made listing anywhere.
        new RunMarketPlan().Plan(scene.Home, new Goal(GoalType.RunMarket), scene.Home.StarSysDateTime);

        string peopleId = scene.People.UniqueID;
        Assert.That(MarketBook.TryGet(scene.Home, peopleId, out var leaving), Is.True,
            "the crowded colony offers its people");
        Assert.That(leaving.Relocation, Is.True);
        Assert.That(leaving.SellQuantity, Is.EqualTo(200));
        Assert.That(leaving.Ask, Is.EqualTo(0), "relocation is free");

        Assert.That(MarketBook.TryGet(scene.Dest, peopleId, out var accepting), Is.True,
            "the roomy colony says it will take them");
        Assert.That(accepting.Relocation, Is.True);
        Assert.That(accepting.BuyQuantity, Is.EqualTo(200));
        Assert.That(accepting.Bid, Is.EqualTo(0), "no fare economy");

        // A second pass is idempotent and does not duplicate the offer.
        new RunMarketPlan().Plan(scene.Home, new Goal(GoalType.RunMarket), scene.Home.StarSysDateTime);
        int offers = scene.Home.GetDataBlob<LogiBaseDB>().Listings.Values.Count(l => l.Relocation)
                     + scene.Dest.GetDataBlob<LogiBaseDB>().Listings.Values.Count(l => l.Relocation);
        Assert.That(offers, Is.EqualTo(2), "one offer per end, not two per pass");

        // --- Production market resolution, through the existing transport path. ---
        Assert.That(MarketRun.TryShipGood(scene.Ship, peopleId, out var good), Is.True);
        scene.AtHome();
        var loadLeg = MarketRun.NextLeg(scene.Ship, scene.Home, scene.Dest, peopleId, good);
        Assert.That(loadLeg.Actions.OfType<MarketExchangeAction>().Count(), Is.EqualTo(1),
            "the freighter is told to buy people at the origin market");
        foreach (var action in loadLeg.Actions)
            Run(scene, action);

        Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(100), "capped by the berths, not the offer");
        Assert.That(scene.Population(scene.Home), Is.EqualTo(1100));

        // Fly the hold to the destination and hand the people over at its market.
        scene.AtDest();
        var unloadLeg = MarketRun.NextLeg(scene.Ship, scene.Home, scene.Dest, peopleId, good);
        Assert.That(unloadLeg.Actions.OfType<MarketExchangeAction>().Count(), Is.EqualTo(1),
            "the freighter is told to sell people at the destination market");
        foreach (var action in unloadLeg.Actions)
            Run(scene, action);

        Assert.That(scene.Population(scene.Dest), Is.EqualTo(100));
        Assert.That(scene.Aboard(scene.Ship), Is.EqualTo(0));
        Assert.That(scene.SpeciesAt(scene.Dest), Is.EqualTo(scene.Species.Id), "the species is kept");
        Assert.That(scene.Population(scene.Home) + scene.Population(scene.Dest), Is.EqualTo(1200),
            "nobody is created or lost");

        // The refreshed offer is retired on the next pass, now that only 100 excess remains.
        new RunMarketPlan().Plan(scene.Home, new Goal(GoalType.RunMarket), scene.Home.StarSysDateTime);
        Assert.That(MarketBook.TryGet(scene.Home, peopleId, out var again), Is.True);
        Assert.That(again.SellQuantity, Is.EqualTo(100), "the stale 200 is gone, replaced by the real excess");
        Assert.That(MarketBook.TryGet(scene.Dest, peopleId, out var take), Is.True);
        Assert.That(take.BuyQuantity, Is.EqualTo(100));
    }

    [Test]
    public void Overcrowding_DoesNotOfferPeople_WhenNoColonyCanTakeThem()
    {
        // Dest is full, so there is nowhere to send anyone: no offer at all.
        var scene = Scene.Build(population: 1200, capacity: 1000, berths: 100, destPopulation: 1000, destCapacity: 1000);

        new RunMarketPlan().Plan(scene.Home, new Goal(GoalType.RunMarket), scene.Home.StarSysDateTime);

        Assert.That(scene.Home.GetDataBlob<LogiBaseDB>().Listings.Values.Any(l => l.Relocation), Is.False,
            "a crowded colony with no destination posts nothing");
        Assert.That(scene.Dest.GetDataBlob<LogiBaseDB>().Listings.Values.Any(l => l.Relocation), Is.False);
    }

    [Test]
    public void Overcrowding_NeverPostsAnOffer_ForAnotherFactionsColony()
    {
        var scene = Scene.Build(population: 1200, capacity: 1000, berths: 100, destPopulation: 0, destCapacity: 3000);
        // Make the roomy colony somebody else's.
        scene.Dest.FactionOwnerID = scene.Other.Id;

        new RunMarketPlan().Plan(scene.Home, new Goal(GoalType.RunMarket), scene.Home.StarSysDateTime);

        Assert.That(scene.Home.GetDataBlob<LogiBaseDB>().Listings.Values.Any(l => l.Relocation), Is.False);
        Assert.That(scene.Dest.GetDataBlob<LogiBaseDB>().Listings.Values.Any(l => l.Relocation), Is.False);
    }

    [Test]
    public void Overcrowding_RefreshRetiresAnOffer_OnceTheColonyNoLongerOverflows()
    {
        var scene = Scene.Build(population: 1200, capacity: 1000, berths: 100, destPopulation: 0, destCapacity: 3000);
        new RunMarketPlan().Plan(scene.Home, new Goal(GoalType.RunMarket), scene.Home.StarSysDateTime);
        Assert.That(scene.Home.GetDataBlob<LogiBaseDB>().Listings.Values.Count(l => l.Relocation), Is.EqualTo(1));

        // The excess is gone (for instance they boarded and left), so nothing is offered any more.
        scene.Population(scene.Home, null, 1000);
        new RunMarketPlan().Plan(scene.Home, new Goal(GoalType.RunMarket), scene.Home.StarSysDateTime);

        Assert.That(scene.Home.GetDataBlob<LogiBaseDB>().Listings.Values.Any(l => l.Relocation), Is.False,
            "the stale offer is retired");
        Assert.That(scene.Dest.GetDataBlob<LogiBaseDB>().Listings.Values.Any(l => l.Relocation), Is.False);
    }

    [Test]
    public void Overcrowding_LeavesAnOrdinaryListingForTheSameSpeciesAlone()
    {
        var scene = Scene.Build(population: 1200, capacity: 1000, berths: 100, destPopulation: 0, destCapacity: 3000);
        // A player already trades people at the origin, at a price.
        MarketBook.SetListing(scene.Home, new MarketListing
        {
            CargoId = scene.People.UniqueID,
            SellQuantity = 7,
            Ask = 5,
        });

        new RunMarketPlan().Plan(scene.Home, new Goal(GoalType.RunMarket), scene.Home.StarSysDateTime);

        Assert.That(MarketBook.TryGet(scene.Home, scene.People.UniqueID, out var kept), Is.True);
        Assert.That(kept.Relocation, Is.False, "the refresh does not take over the id");
        Assert.That(kept.SellQuantity, Is.EqualTo(7));
        Assert.That(kept.Ask, Is.EqualTo(5));
        Assert.That(scene.Dest.GetDataBlob<LogiBaseDB>().Listings.Values.Any(l => l.Relocation), Is.False,
            "a half offer is not posted either");
    }

    [Test]
    public void Migration_LeavesACrowdedColony_ForTheRoomiestInTheSameSystem()
    {
        var scene = Scene.Build(population: 1500, capacity: 500, berths: 100, destPopulation: 0, destCapacity: 1000);
        var fleet = scene.Fleet();
        scene.AtHome();

        var moves = MigrationPlanner.Plan(scene.Home.Manager);

        Assert.That(moves, Has.Count.EqualTo(1));
        Assert.That(moves[0].Origin, Is.EqualTo(scene.Home));
        Assert.That(moves[0].Destination, Is.EqualTo(scene.Dest));
        Assert.That(moves[0].Fleet, Is.EqualTo(fleet));
        Assert.That(moves[0].SpeciesId, Is.EqualTo(scene.Species.Id));
        Assert.That(moves[0].Travelling, Is.EqualTo(100), "one colony's worth of room, one hold of berths");
    }

    [Test]
    public void Migration_MovesNobodyFromAColonyThatFits()
    {
        var scene = Scene.Build(population: 400, capacity: 1000, berths: 100, destPopulation: 0, destCapacity: 1000);
        scene.Fleet();
        scene.AtHome();

        Assert.That(MigrationPlanner.Plan(scene.Home.Manager), Is.Empty);
    }

    [Test]
    public void Migration_WithoutPassengerBerths_MovesNobody()
    {
        var scene = Scene.Build(population: 1500, capacity: 500, berths: 0, destPopulation: 0, destCapacity: 1000);
        scene.Fleet(berths: 0);
        scene.AtHome();

        Assert.That(MigrationPlanner.Plan(scene.Home.Manager), Is.Empty);
    }

    [Test]
    public void Migration_NeverCrossesFactions()
    {
        var scene = Scene.Build(population: 1500, capacity: 500, berths: 100, destPopulation: 0, destCapacity: 1000);
        scene.Dest.FactionOwnerID = scene.Other.Id;
        scene.Fleet();
        scene.AtHome();

        Assert.That(MigrationPlanner.Plan(scene.Home.Manager), Is.Empty);
    }

    [Test]
    public void MigrationSignal_UsesTheSameSupportNumberPopulationDeathUses()
    {
        var scene = Scene.Build(population: 600, capacity: 1500, berths: 10);
        scene.Home.GetDataBlob<ColonyLifeSupportDB>().MaxPopulation = 1500;

        Assert.That(MigrationPlanner.Support(scene.Home, scene.Species), Is.EqualTo(1500));
        Assert.That(MigrationPlanner.Capacity(scene.Home, scene.Species), Is.EqualTo(1500));
        Assert.That(MigrationPlanner.Crowded(scene.Home, scene.Species.Id), Is.EqualTo(0));
        Assert.That(PassengerAdapter.Room(scene.Home, scene.Ship, scene.Species.Id), Is.EqualTo(900));

        scene.Population(scene.Home, null, 1600);
        Assert.That(MigrationPlanner.Crowded(scene.Home, scene.Species.Id), Is.EqualTo(100));

        scene.Home.GetDataBlob<ColonyLifeSupportDB>().MaxPopulation = 1300;
        Assert.That(MigrationPlanner.Room(scene.Home, scene.Species.Id), Is.EqualTo(0), "the ceiling is what makes it full");
    }

    [Test]
    public void Relocation_DoesNotUseAnOfficeSlot_AndSellableIsThePopulation()
    {
        var scene = Scene.Build(population: 500, capacity: 1000, berths: 10);
        var book = scene.Home.GetDataBlob<LogiBaseDB>();
        book.Capacity = 1;

        Assert.That(MarketBook.SetListing(scene.Home, new MarketListing
        {
            CargoId = "iron",
            SellQuantity = 10,
            Ask = 1,
        }), Is.True);
        Assert.That(MarketBook.SetListing(scene.Home, new MarketListing
        {
            CargoId = "copper",
            SellQuantity = 10,
            Ask = 1,
        }), Is.False, "a trade good still needs a free slot");
        Assert.That(MarketBook.SetListing(scene.Home, new MarketListing
        {
            CargoId = scene.People.UniqueID,
            SellQuantity = 40,
            Relocation = true,
        }), Is.True, "a relocation does not take an office slot");

        Assert.That(MarketBook.Sellable(scene.Home, book.Listings[scene.People.UniqueID], scene.People),
            Is.EqualTo(500));
    }

    /// <summary>
    /// The queue binds the ship in <see cref="EntityAction.IsValidCommand"/> before
    /// <see cref="EntityAction.Execute"/>. These tests call Execute directly, so bind first.
    /// </summary>
    static void Run(Scene scene, EntityAction action)
    {
        Assert.That(action.IsValidCommand(scene.Game), Is.True, action.Name);
        action.Execute(scene.Ship.StarSysDateTime);
    }

    sealed class Scene
    {
        public Game Game = null!;
        public StarSystem System = null!;
        public Entity Faction = null!;
        public Entity Other = null!;
        public Entity Species = null!;
        public Entity Home = null!;
        public Entity Dest = null!;
        public Entity Ship = null!;
        public Entity Star = null!;
        public SpeciesCargo People = null!;

        /// <summary>Read the registered passenger definition, not a fresh object, so ids match production.</summary>
        public SpeciesCargo ResolveCargo(Entity species)
        {
            var data = Faction.GetDataBlob<FactionInfoDB>().Data;
            var found = data.CargoGoods.GetAny(species.Id.ToString());
            Assert.That(found, Is.Not.Null, "the species must be registered as passenger cargo");
            return (SpeciesCargo)found!;
        }

        public long Population(Entity colony, Entity? species = null)
        {
            var info = colony.GetDataBlob<ColonyInfoDB>();
            int id = (species ?? Species).Id;
            return info.Population.TryGetValue(id, out var count) ? count : 0;
        }

        public void Population(Entity colony, Entity? species, long count)
        {
            var info = colony.GetDataBlob<ColonyInfoDB>();
            info.Population[(species ?? Species).Id] = count;
        }

        public int SpeciesAt(Entity colony)
        {
            return colony.GetDataBlob<ColonyInfoDB>().Population
                .Where(pair => pair.Value > 0)
                .Select(pair => pair.Key)
                .First();
        }

        public long Aboard(Entity ship)
        {
            return ship.GetDataBlob<CargoStorageDB>().GetUnitsStored(People, false);
        }

        public Entity? SecondSpeciesEntity;

        /// <summary>The same id as <see cref="SpeciesCargo.UniqueID"/> keys the cargo definitions, so keep the one instance.</summary>
        public SpeciesCargo? SecondCargo;

        public SpeciesCargo SecondSpecies(long population)
        {
            var species = Entity.Create(Faction.Id);
            System.AddEntity(species, new List<BaseDataBlob> { new NameDB("Martians"), Habitable() });
            SecondSpeciesEntity = species;
            Population(Home, species, population);
            var info = Faction.GetDataBlob<FactionInfoDB>();
            info.Species.Add(species);
            SpeciesCargo.Register(info.Data, info.Species);
            var cargo = (SpeciesCargo)info.Data.CargoGoods.GetAny(species.Id.ToString())!;
            SecondCargo = cargo;
            return cargo;
        }

        public void AtHome()
        {
            Place(Ship, Home);
        }

        public void AtDest()
        {
            Place(Ship, Dest);
        }

        void Place(Entity ship, Entity colony)
        {
            ship.GetDataBlob<PositionDB>().RelativePosition = colony.GetDataBlob<PositionDB>().AbsolutePosition;
            var store = colony.GetDataBlob<CargoStorageDB>();
            store.TransferRate = 1_000_000;
            store.TransferRangeDv_mps = 1e12;
            ship.GetDataBlob<CargoStorageDB>().TransferRate = 1_000_000;
            ship.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        }

        public Entity Fleet(int berths = 100)
        {
            var fleet = Entity.Create(Faction.Id);
            System.AddEntity(fleet, new List<BaseDataBlob> { new NameDB("Fleet"), new FleetDB() });
            var ship = Entity.Create(Faction.Id);
            var hold = berths > 0
                ? new CargoStorageDB(SpeciesCargo.CargoType, berths * SpeciesCargo.VolumePerPerson)
                : new CargoStorageDB("general-storage", 1000);
            System.AddEntity(ship, new List<BaseDataBlob>
            {
                new NameDB("Liner"),
                new ShipInfoDB(),
                hold,
                new PositionDB(new Vector3(1.5e11, 0, 0), Star),
                new ActionQueueDB(),
                MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
            });
            fleet.GetDataBlob<FleetDB>().AddChild(ship);
            return fleet;
        }

        public static Scene Build(
            long population, long capacity, int berths,
            long destPopulation = 0, long destCapacity = 1000)
        {
            var modLoader = new ModLoader();
            var store = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
            var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
            game.Settings.EnforceSingleThread = true;

            var faction = FactionFactory.CreateFaction(game, "Settlers");
            var other = FactionFactory.CreateFaction(game, "Rivals");

            var system = new StarSystem();
            system.Initialize(game, "Migration", -1);
            system.SetActivityState(SystemActivityState.Stasis);
            // A body needs StarInfoDB to be a star; the tests below read it back by that blob.
            var star = Entity.Create();
            system.AddEntity(star, new List<BaseDataBlob>
            {
                new NameDB("Star"),
                new StarInfoDB(),
                new PositionDB(Vector3.Zero),
                MassVolumeDB.NewFromMassAndRadius_m(1.989e30, 6.96342e8),
            });

            var species = Entity.Create(faction.Id);
            system.AddEntity(species, new List<BaseDataBlob> { new NameDB("Humans"), Habitable() });

            var scene = new Scene
            {
                Game = game,
                System = system,
                Faction = faction,
                Other = other,
                Species = species,
                Star = star,
            };
            // Registration is production behaviour: a faction registers its own species.
            // The people are looked up from that registry, the same way a market or an order does.
            var info = faction.GetDataBlob<FactionInfoDB>();
            info.Species.Add(species);
            SpeciesCargo.Register(info.Data, info.Species);
            scene.People = scene.ResolveCargo(species);

            scene.Home = scene.Colony(faction, new Vector3(1.496e11, 0, 0), population, capacity);
            scene.Dest = scene.Colony(faction, new Vector3(2.28e11, 0, 0), destPopulation, destCapacity);

            var ship = Entity.Create(faction.Id);
            var hold = berths > 0
                ? new CargoStorageDB(SpeciesCargo.CargoType, berths * SpeciesCargo.VolumePerPerson)
                : new CargoStorageDB("general-storage", 1000);
            system.AddEntity(ship, new List<BaseDataBlob>
            {
                new NameDB("Ferry"),
                new ShipInfoDB(),
                hold,
                new PositionDB(new Vector3(5e11, 0, 0), star),
                new ActionQueueDB(),
                MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
            });
            scene.Ship = ship;
            return scene;
        }

        public Entity Colony(Entity owner, Vector3 at, long population, long capacity)
        {
            var star = Star;
            var planet = Entity.Create();
            System.AddEntity(planet, new List<BaseDataBlob>
            {
                new NameDB("Body"),
                Habitable(),
                // Cargo transfer reads the day length for a colony's orbital speed. No orbit blob:
                // a missing orbit stays a comfortable world, so support is still the infrastructure.
                new SystemBodyInfoDB
                {
                    Gravity = 9.8,
                    BaseTemperature = 15,
                    LengthOfDay = TimeSpan.FromDays(1),
                },
                new PositionDB(at, star),
                MassVolumeDB.NewFromMassAndRadius_m(5.972e24, 6.371e6),
            });

            var colony = Entity.Create(owner.Id);
            var info = new ColonyInfoDB { PlanetEntity = planet };
            if (population > 0)
                info.Population[Species.Id] = population;
            System.AddEntity(colony, new List<BaseDataBlob>
            {
                new NameDB("Colony"),
                info,
                new ColonyLifeSupportDB { MaxPopulation = capacity },
                new CargoStorageDB("general-storage", 1000),
                new ComponentInstancesDB(),
                new PositionDB(at, star),
                MassVolumeDB.NewFromMassAndRadius_m(5.972e24, 6.371e6),
                new ActionQueueDB(),
                // A market book so the periodic market pass and the freighter goals can see this colony.
                new LogiBaseDB { Capacity = 8 },
                new GoalsDB(),
            });
            colony.AddComponent(Support(capacity));
            return colony;
        }

        /// <summary>A species adapted to the test body: 9.8 m/s^2, 1 atm, 15 C, no toxic gases.</summary>
        static SpeciesDB Habitable()
        {
            return new SpeciesDB(9.8, 1.0, 15.0, 1.0, 0.1, 10.0, 15.0, -100.0, 100.0);
        }

        static ComponentDesign Support(long capacity)
        {
            var design = new ComponentDesign
            {
                UniqueID = "test-infrastructure-" + capacity,
                Name = "Infrastructure",
                CargoTypeID = "general-storage",
                VolumePerUnit = 1,
                MassPerUnit = 1,
                Density = 1,
            };
            design.AttributesByType[typeof(PopulationSupportAtbDB)] = new PopulationSupportAtbDB(capacity);
            return design;
        }

        public static void Drain(Entity holder)
        {
            var store = holder.GetDataBlob<CargoStorageDB>();
            store.TransferRate = 1_000_000;
            // CreateLoadPair escrows without running the order, so nothing replaces the blob.
            // A finished trip leaves the old CargoTransferDB in place. Bind the open escrow,
            // the way CargoTransferOrder.Execute does when the next order starts.
            CargoTransferDataDB data = null;
            foreach (var item in store.EscroItems)
            {
                if (!Cleared(item))
                    data = item;
            }
            Assert.That(data, Is.Not.Null, "escrow was not opened");
            var blob = new CargoTransferDB(data);
            blob.ParentStorageDB = store;
            holder.SetDataBlob(blob);
            var processor = new CargoTransferProcessor();
            for (int i = 0; i < 8; i++)
            {
                processor.ProcessEntity(blob, 10);
                if (Empty(blob))
                {
                    holder.RemoveDataBlob<CargoTransferDB>();
                    return;
                }
            }
            Assert.That(Empty(blob), Is.True, "transfer escrow did not clear");
        }

        static bool Cleared(CargoTransferDataDB data)
        {
            foreach (var tup in data.EscroHeldInPrimary)
            {
                if (tup.count > 0)
                    return false;
            }
            foreach (var tup in data.EscroHeldInSecondary)
            {
                if (tup.count > 0)
                    return false;
            }
            return true;
        }

        static bool Empty(CargoTransferDB blob)
        {
            foreach (var tup in blob.TransferData.EscroHeldInPrimary)
            {
                if (tup.count > 0)
                    return false;
            }
            foreach (var tup in blob.TransferData.EscroHeldInSecondary)
            {
                if (tup.count > 0)
                    return false;
            }
            return true;
        }
    }
}
