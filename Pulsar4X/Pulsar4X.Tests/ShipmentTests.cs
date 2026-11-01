using System;
using System.Collections.Generic;
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
using Pulsar4X.Modding;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>Sealed shipments: the seal, the haul row, the claim, the load and unload trips, cancel and release.</summary>
[TestFixture]
public class ShipmentTests
{
    // ------------------------------------------------------------------ seal (market path)

    [Test]
    public void MarketSeal_TwoGoods_OneShipmentPerCargoType_AndOneRowEach()
    {
        var scene = Scene.Build();
        var result = scene.Seal(("iron", 30), ("rp-1", 10), fee: 20m);

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        var held = ShipmentBook.At(scene.Seller);
        Assert.That(held, Has.Count.EqualTo(2));
        var iron = held.Single(s => s.CargoTypeID == "general-storage");
        var fuel = held.Single(s => s.CargoTypeID == "fuel-storage");
        Assert.That(iron.Lines.Single().Units, Is.EqualTo(30));
        Assert.That(iron.MassPerUnit, Is.EqualTo(scene.Iron.MassPerUnit * 30));
        Assert.That(iron.VolumePerUnit, Is.EqualTo(scene.Iron.VolumePerUnit * 30).Within(1e-9));
        Assert.That(fuel.Lines.Single().Units, Is.EqualTo(10));

        // Both piles lost the sealed units, so nothing else can take them.
        Assert.That(scene.Units(scene.Seller, scene.Iron), Is.EqualTo(70));
        Assert.That(scene.Units(scene.Seller, scene.Fuel), Is.EqualTo(90));
        Assert.That(MarketBook.TryGet(scene.Seller, "iron", out var ironListing), Is.True);
        Assert.That(ironListing.SellQuantity, Is.EqualTo(70));
        Assert.That(MarketBook.TryGet(scene.Seller, "rp-1", out var fuelListing), Is.True);
        Assert.That(fuelListing.SellQuantity, Is.EqualTo(90));

        // One goods line per id, paid now.
        var paid = scene.BuyerInfo.Money.GetTransactionsByCategory(TransactionCategory.Trade);
        Assert.That(paid, Has.Count.EqualTo(2));
        Assert.That(scene.BuyerInfo.Money.GetCurrentFunds(), Is.EqualTo(1000m - 30 * 10 - 10 * 5));
        Assert.That(scene.SellerInfo.Money.GetCurrentFunds(), Is.EqualTo(30 * 10 + 10 * 5));

        // Two haul rows at half the fee each, and no goal.
        var book = scene.Buyer.GetDataBlob<LogiBaseDB>();
        var rows = book.Intel!.Values.Where(r => r.Kind == IntelKind.Haul).ToList();
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows.Sum(r => r.Ask), Is.EqualTo(20m));
        Assert.That(rows.All(r => r.ForSale && r.ClaimantId < 0), Is.True);
        Assert.That(rows.All(r => r.SourceId == scene.Seller.Id && r.DestId == scene.Buyer.Id), Is.True);
    }

    [Test]
    public void SealedUnits_CannotBeBoughtAgain()
    {
        var scene = Scene.Build();
        Assert.That(scene.Seal(("iron", 100), fee: 0m).Accepted, Is.True);

        var second = scene.Seal(("iron", 1), fee: 0m);

        Assert.That(second.Accepted, Is.False);
        Assert.That(ShipmentBook.At(scene.Seller), Has.Count.EqualTo(1));
        Assert.That(scene.Units(scene.Seller, scene.Iron), Is.EqualTo(0));
    }

    [Test]
    public void InsufficientFunds_RejectsWithNoShipmentOrRow()
    {
        var scene = Scene.Build(buyerFunds: 5);
        var result = scene.Seal(("iron", 30), fee: 5m);

        Assert.That(result.Accepted, Is.False);
        Assert.That(ShipmentBook.At(scene.Seller), Is.Empty);
        Assert.That(scene.Units(scene.Seller, scene.Iron), Is.EqualTo(100));
        Assert.That(scene.Buyer.GetDataBlob<LogiBaseDB>().Intel?.Values.Any(r => r.Kind == IntelKind.Haul) ?? false, Is.False);
        Assert.That(scene.BuyerInfo.Money.GetCurrentFunds(), Is.EqualTo(5m));
    }

    [Test]
    public void OneSidedFriendly_Rejects()
    {
        var scene = Scene.Build(sellerTradesWithBuyer: false);
        var result = scene.Seal(("iron", 10), fee: 1m);

        Assert.That(result.Accepted, Is.False);
        Assert.That(ShipmentBook.At(scene.Seller), Is.Empty);
        Assert.That(scene.Units(scene.Seller, scene.Iron), Is.EqualTo(100));
        Assert.That(scene.BuyerInfo.Money.GetTransactionsByCategory(TransactionCategory.Trade), Is.Empty);
    }

    [Test]
    public void FillBid_PaysTheBid_AndCountsDownTheBuyersBidQuantity()
    {
        var scene = Scene.Build();
        Assert.That(MarketBook.SetListing(scene.Buyer, new MarketListing
        {
            CargoId = "iron", BuyQuantity = 50, Bid = 8,
        }), Is.True);

        var result = new CommandTranslator(scene.Game).Translate(
            scene.SellerFaction, scene.Buyer,
            new SealShipmentCommand(scene.Buyer.Id, scene.Seller.Id, new[] { new SealLine("iron", 20) }, 4m, FillBid: true));

        Assert.That(result.Accepted, Is.True, result.RejectionReason);
        Assert.That(MarketBook.TryGet(scene.Buyer, "iron", out var bid), Is.True);
        Assert.That(bid.BuyQuantity, Is.EqualTo(30));
        Assert.That(scene.BuyerInfo.Money.GetCurrentFunds(), Is.EqualTo(1000m - 20 * 8));
        Assert.That(scene.SellerInfo.Money.GetCurrentFunds(), Is.EqualTo(20 * 8));
        Assert.That(ShipmentBook.At(scene.Seller).Single().FilledBid, Is.True);
    }

    [Test]
    public void SealedLines_AreNotOfferedOrListedAsOpenStock()
    {
        var scene = Scene.Build();
        Assert.That(scene.Seal(("iron", 30), fee: 0m).Accepted, Is.True);

        var library = scene.SellerInfo.Data.CargoGoods;
        var shipment = ShipmentBook.At(scene.Seller).Single();
        Assert.That(library.Contains(shipment.UniqueID), Is.False);
        Assert.That(library.Contains(shipment.Lines.Single().Held.UniqueID), Is.False);
        Assert.That(MarketBook.Stock(scene.Seller, scene.Iron), Is.EqualTo(70));
        Assert.That(scene.Buyer.GetDataBlob<LogiBaseDB>().Intel!.Values.Count(r => r.Kind == IntelKind.Haul), Is.EqualTo(1));
    }

    // ------------------------------------------------------------------ non-market seal and release

    [Test]
    public void NonMarketSeal_HasNoPricesLedgerFeeOrRow()
    {
        var scene = Scene.Build(buyerOwnsSeller: true);
        int sellerLedger = scene.SellerInfo.Money.GetAllTransactions().Count;
        int buyerLedger = scene.BuyerInfo.Money.GetAllTransactions().Count;

        Assert.That(ShipmentBook.TrySeal(scene.Seller, scene.Buyer,
            new List<(ICargoable, long)> { (scene.Iron, 40) }, "order-1", out var made, out var reason), Is.True, reason);

        var shipment = made.Single();
        Assert.That(shipment.IsMarket, Is.False);
        Assert.That(shipment.Fee, Is.EqualTo(0m));
        Assert.That(shipment.FleetOrder, Is.EqualTo("order-1"));
        Assert.That(shipment.Lines.Single().Price, Is.EqualTo(0m));
        Assert.That(scene.Units(scene.Seller, scene.Iron), Is.EqualTo(60));
        Assert.That(scene.SellerInfo.Money.GetAllTransactions().Count, Is.EqualTo(sellerLedger));
        Assert.That(scene.BuyerInfo.Money.GetAllTransactions().Count, Is.EqualTo(buyerLedger));
        Assert.That(scene.Buyer.GetDataBlob<LogiBaseDB>().Intel?.Count ?? 0, Is.EqualTo(0));
        Assert.That(MarketBook.TryGet(scene.Seller, "iron", out var listing) && listing.SellQuantity == 100, Is.True);
    }

    [Test]
    public void Release_ReturnsEveryLineToOpenStock()
    {
        var scene = Scene.Build(buyerOwnsSeller: true);
        Assert.That(ShipmentBook.TrySeal(scene.Seller, scene.Buyer,
            new List<(ICargoable, long)> { (scene.Iron, 40), (scene.Fuel, 10) }, "order-2", out var made, out _), Is.True);
        Assert.That(scene.Units(scene.Seller, scene.Iron), Is.EqualTo(60));

        foreach (var shipment in made)
            Assert.That(ShipmentBook.Release(scene.Game, shipment), Is.True);

        Assert.That(scene.Units(scene.Seller, scene.Iron), Is.EqualTo(100));
        Assert.That(scene.Units(scene.Seller, scene.Fuel), Is.EqualTo(100));
        Assert.That(ShipmentBook.At(scene.Seller), Is.Empty);
        var db = scene.Seller.GetDataBlob<CargoStorageDB>();
        Assert.That(db.TypeStores["general-storage"].FreeVolume,
            Is.EqualTo(db.TypeStores["general-storage"].MaxVolume - scene.Iron.VolumePerUnit * 100).Within(1e-6));
    }

    // ------------------------------------------------------------------ cancel

    [Test]
    public void Cancel_RestoresStockListingsAndLedgers_AndRemovesTheRow()
    {
        var scene = Scene.Build();
        Assert.That(scene.Seal(("iron", 30), ("rp-1", 10), fee: 20m).Accepted, Is.True);

        foreach (var shipment in ShipmentBook.At(scene.Seller).ToList())
            Assert.That(ShipmentBook.TryCancel(scene.Buyer, shipment.UniqueID, out var reason), Is.True, reason);

        Assert.That(ShipmentBook.At(scene.Seller), Is.Empty);
        Assert.That(scene.Units(scene.Seller, scene.Iron), Is.EqualTo(100));
        Assert.That(scene.Units(scene.Seller, scene.Fuel), Is.EqualTo(100));
        Assert.That(MarketBook.TryGet(scene.Seller, "iron", out var ironListing) && ironListing.SellQuantity == 100, Is.True);
        Assert.That(MarketBook.TryGet(scene.Seller, "rp-1", out var fuelListing) && fuelListing.SellQuantity == 100, Is.True);
        Assert.That(scene.BuyerInfo.Money.GetCurrentFunds(), Is.EqualTo(1000m));
        Assert.That(scene.SellerInfo.Money.GetCurrentFunds(), Is.EqualTo(0m));
        Assert.That(scene.Buyer.GetDataBlob<LogiBaseDB>().Intel!.Values.Any(r => r.Kind == IntelKind.Haul), Is.False);
    }

    [Test]
    public void Cancel_IsRefusedOnceAShipHasClaimedTheRow()
    {
        var scene = Scene.Build();
        Assert.That(scene.Seal(("iron", 30), fee: 5m).Accepted, Is.True);
        var shipment = ShipmentBook.At(scene.Seller).Single();
        Assert.That(IntelBook.TryClaim(scene.Buyer, scene.CarrierShip, shipment.UniqueID, out _), Is.True);

        Assert.That(ShipmentBook.TryCancel(scene.Buyer, shipment.UniqueID, out _), Is.False);
        Assert.That(ShipmentBook.At(scene.Seller), Has.Count.EqualTo(1));
    }

    // ------------------------------------------------------------------ claim

    [Test]
    public void Claim_ByAThirdFactionShip_IsAccepted_AndASecondClaimIsRejected()
    {
        var scene = Scene.Build(holdUnits: 50);
        Assert.That(scene.Seal(("iron", 30), fee: 5m).Accepted, Is.True);
        var shipment = ShipmentBook.At(scene.Seller).Single();
        var other = scene.ShipAt(scene.Carrier, new Vector3(5e11, 1e9, 0), 1, 50);
        scene.InRange();

        var claim = new CommandTranslator(scene.Game).Translate(
            scene.Carrier, scene.Buyer, new ClaimHaulCommand(scene.Buyer.Id, shipment.UniqueID, scene.CarrierShip.Id));

        Assert.That(claim.Accepted, Is.True, claim.RejectionReason);
        Assert.That(IntelBook.TryGet(scene.Buyer, IntelKind.Haul, shipment.UniqueID, out var row), Is.True);
        Assert.That(row.ClaimantId, Is.EqualTo(scene.CarrierShip.Id));

        var again = new CommandTranslator(scene.Game).Translate(
            scene.Carrier, scene.Buyer, new ClaimHaulCommand(scene.Buyer.Id, shipment.UniqueID, other.Id));
        Assert.That(again.Accepted, Is.False);
        Assert.That(IntelBook.TryClaim(scene.Buyer, other, shipment.UniqueID, out _), Is.False);
    }

    // ------------------------------------------------------------------ the haul, ship side

    [Test]
    public void ThirdFactionShip_LoadsAtTheSource_UnpacksAtTheDestination_AndIsPaidTheFee()
    {
        var scene = Scene.Build(holdUnits: 50);
        Assert.That(scene.Seal(("iron", 30), fee: 25m).Accepted, Is.True);
        var shipment = ShipmentBook.At(scene.Seller).Single();
        scene.InRange();
        var ship = scene.CarrierShip;
        var goal = scene.GoalFor(shipment);

        // Plan 1: the planner claims the row itself, takes the line out of the hold and loads it.
        var plan = new DeliverShipmentPlan().Plan(ship, goal, ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(plan.Actions.OfType<CargoTransferOrder>().Count(), Is.EqualTo(1));
        Assert.That(IntelBook.TryGet(scene.Buyer, IntelKind.Haul, shipment.UniqueID, out var row), Is.True);
        Assert.That(row.ClaimantId, Is.EqualTo(ship.Id));
        Assert.That(shipment.Lines.Single().Released, Is.True);

        Scene.Drain(scene.Seller);
        var line = shipment.Lines.Single();
        Assert.That(scene.Units(ship, line.Held), Is.EqualTo(30));
        Assert.That(scene.Units(scene.Seller, line.Held), Is.EqualTo(0));

        // Plan 2: aboard, so it unloads. The destination belongs to a different faction than the ship.
        plan = new DeliverShipmentPlan().Plan(ship, goal, ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(plan.Actions.OfType<CargoTransferOrder>().Count(), Is.EqualTo(1));
        Scene.Drain(scene.Buyer);
        Assert.That(scene.Units(scene.Buyer, line.Held), Is.EqualTo(30));

        // Plan 3: unpacked as the resource, the fee is paid and the row goes.
        plan = new DeliverShipmentPlan().Plan(ship, goal, ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Completed), plan.Message);
        Assert.That(scene.Units(scene.Buyer, scene.Iron), Is.EqualTo(30));
        Assert.That(scene.Units(scene.Buyer, line.Held), Is.EqualTo(0));
        Assert.That(scene.Units(ship, scene.Iron), Is.EqualTo(0));
        Assert.That(scene.CarrierInfo.Money.GetCurrentFunds(), Is.EqualTo(25m));
        Assert.That(scene.BuyerInfo.Money.GetCurrentFunds(), Is.EqualTo(1000m - 300 - 25));
        Assert.That(scene.Buyer.GetDataBlob<LogiBaseDB>().Intel!.Values.Any(r => r.Kind == IntelKind.Haul), Is.False);
        Assert.That(ShipmentBook.At(scene.Seller), Is.Empty);
    }

    [Test]
    public void TheBuyersOwnCarrier_WritesNoFeeLine()
    {
        var scene = Scene.Build(holdUnits: 50, carrierIsBuyer: true);
        Assert.That(scene.Seal(("iron", 30), fee: 25m).Accepted, Is.True);
        var shipment = ShipmentBook.At(scene.Seller).Single();
        scene.InRange();
        var ship = scene.CarrierShip;
        var goal = scene.GoalFor(shipment);
        int ledger = scene.BuyerInfo.Money.GetAllTransactions().Count;

        new DeliverShipmentPlan().Plan(ship, goal, ship.StarSysDateTime);
        Scene.Drain(scene.Seller);
        new DeliverShipmentPlan().Plan(ship, goal, ship.StarSysDateTime);
        Scene.Drain(scene.Buyer);
        var plan = new DeliverShipmentPlan().Plan(ship, goal, ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Completed), plan.Message);
        Assert.That(scene.Units(scene.Buyer, scene.Iron), Is.EqualTo(30));
        Assert.That(scene.BuyerInfo.Money.GetAllTransactions().Count, Is.EqualTo(ledger));
        Assert.That(scene.BuyerInfo.Money.GetCurrentFunds(), Is.EqualTo(1000m - 300));
    }

    [Test]
    public void APackageTooBigForOneHold_GoesInParts_WithNoSplitLine_AndTheFeeComesLast()
    {
        // The hold takes the iron line, but not the iron and titanium lines together.
        var scene = Scene.Build(holdVolume: 0.0030, stockTitanium: 100);
        Assert.That(scene.Seal(("iron", 10), ("titanium", 10), fee: 8m).Accepted, Is.True);
        var shipment = ShipmentBook.At(scene.Seller).Single();
        Assert.That(shipment.Lines, Has.Count.EqualTo(2));
        scene.InRange();
        var ship = scene.CarrierShip;
        var goal = scene.GoalFor(shipment);

        // Trip 1: iron only.
        var plan = new DeliverShipmentPlan().Plan(ship, goal, ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(shipment.Lines[0].Released, Is.True);
        Assert.That(shipment.Lines[1].Released, Is.False);
        Scene.Drain(scene.Seller);
        new DeliverShipmentPlan().Plan(ship, goal, ship.StarSysDateTime);
        Scene.Drain(scene.Buyer);

        // Back at the source for the rest. The row stays, and no fee has been paid.
        plan = new DeliverShipmentPlan().Plan(ship, goal, ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(scene.Units(scene.Buyer, scene.Iron), Is.EqualTo(10));
        Assert.That(shipment.Lines[0].Units, Is.EqualTo(10), "a crate or line is never split");
        Assert.That(scene.CarrierInfo.Money.GetCurrentFunds(), Is.EqualTo(0m));
        Assert.That(IntelBook.TryGet(scene.Buyer, IntelKind.Haul, shipment.UniqueID, out _), Is.True);
        Assert.That(shipment.Lines[1].Released, Is.True);

        Scene.Drain(scene.Seller);
        new DeliverShipmentPlan().Plan(ship, goal, ship.StarSysDateTime);
        Scene.Drain(scene.Buyer);
        plan = new DeliverShipmentPlan().Plan(ship, goal, ship.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Completed), plan.Message);
        Assert.That(scene.Units(scene.Buyer, scene.Titanium), Is.EqualTo(10));
        Assert.That(scene.CarrierInfo.Money.GetCurrentFunds(), Is.EqualTo(8m));
        Assert.That(IntelBook.TryGet(scene.Buyer, IntelKind.Haul, shipment.UniqueID, out _), Is.False);
    }

    [Test]
    public void FullDestination_UnloadsWhatFits_AndTheShipKeepsTrying()
    {
        // The destination takes about 12 of the 30 units. The rest stays aboard and the fee waits.
        var scene = Scene.Build(holdUnits: 50, buyerVolume: 0.0016);
        Assert.That(scene.Seal(("iron", 30), fee: 5m).Accepted, Is.True);
        var shipment = ShipmentBook.At(scene.Seller).Single();
        scene.InRange();
        var ship = scene.CarrierShip;
        var goal = scene.GoalFor(shipment);

        new DeliverShipmentPlan().Plan(ship, goal, ship.StarSysDateTime);
        Scene.Drain(scene.Seller);
        var plan = new DeliverShipmentPlan().Plan(ship, goal, ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Scene.Drain(scene.Buyer);

        var line = shipment.Lines.Single();
        long landed = scene.Units(scene.Buyer, line.Held);
        Assert.That(landed, Is.GreaterThan(0).And.LessThan(30));
        Assert.That(scene.Units(ship, line.Held), Is.EqualTo(30 - landed));

        plan = new DeliverShipmentPlan().Plan(ship, goal, ship.StarSysDateTime);
        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Active), plan.Message);
        Assert.That(scene.CarrierInfo.Money.GetCurrentFunds(), Is.EqualTo(0m));
        Assert.That(IntelBook.TryGet(scene.Buyer, IntelKind.Haul, shipment.UniqueID, out _), Is.True);
    }

    [Test]
    public void ClaimedByAnotherShip_TheGoalFails_AndLeavesTheClaimAlone()
    {
        var scene = Scene.Build(holdUnits: 50);
        Assert.That(scene.Seal(("iron", 30), fee: 5m).Accepted, Is.True);
        var shipment = ShipmentBook.At(scene.Seller).Single();
        var other = scene.ShipAt(scene.Carrier, new Vector3(5e11, 1e9, 0), 1, 50);
        Assert.That(IntelBook.TryClaim(scene.Buyer, other, shipment.UniqueID, out _), Is.True);
        scene.InRange();

        var plan = new DeliverShipmentPlan().Plan(scene.CarrierShip, scene.GoalFor(shipment), scene.CarrierShip.StarSysDateTime);

        Assert.That(plan.Status, Is.EqualTo(GoalStatus.Failed));
        Assert.That(plan.Message, Does.Contain("claimed"));
        Assert.That(IntelBook.TryGet(scene.Buyer, IntelKind.Haul, shipment.UniqueID, out var row) && row.ClaimantId == other.Id, Is.True);
    }

    [Test]
    public void CancellingTheGoal_ReleasesTheClaim_WhenNothingIsAboard()
    {
        var scene = Scene.Build(holdUnits: 50);
        Assert.That(scene.Seal(("iron", 30), fee: 5m).Accepted, Is.True);
        var shipment = ShipmentBook.At(scene.Seller).Single();
        Assert.That(IntelBook.TryClaim(scene.Buyer, scene.CarrierShip, shipment.UniqueID, out _), Is.True);
        var goal = scene.GoalFor(shipment);

        new DeliverShipmentPlan().Cancel(scene.CarrierShip, goal, scene.CarrierShip.StarSysDateTime, CancelTiming.Now);

        Assert.That(IntelBook.TryGet(scene.Buyer, IntelKind.Haul, shipment.UniqueID, out var row), Is.True);
        Assert.That(row.ClaimantId, Is.EqualTo(-1));
        Assert.That(row.ForSale, Is.True);
    }

    // ------------------------------------------------------------------ scene

    sealed class Scene
    {
        public Game Game = null!;
        public StarSystem System = null!;
        public Entity Star = null!;
        public Entity SellerFaction = null!;
        public Entity BuyerFaction = null!;
        public Entity Carrier = null!;
        public Entity Seller = null!;
        public Entity Buyer = null!;
        public Entity CarrierShip = null!;
        public ICargoable Iron = null!;
        public ICargoable Fuel = null!;
        public ICargoable Titanium = null!;
        public FactionInfoDB SellerInfo = null!;
        public FactionInfoDB BuyerInfo = null!;
        public FactionInfoDB CarrierInfo = null!;

        public long Units(Entity holder, ICargoable item)
            => holder.GetDataBlob<CargoStorageDB>().GetUnitsStored(item, false);

        public CommandResult Seal((string id, long units) a, decimal fee)
            => Seal(new[] { a }, fee);

        public CommandResult Seal((string id, long units) a, (string id, long units) b, decimal fee)
            => Seal(new[] { a, b }, fee);

        public CommandResult Seal(IEnumerable<(string id, long units)> lines, decimal fee)
        {
            var items = lines.Select(l => new SealLine(l.id, l.units)).ToList();
            return new CommandTranslator(Game).Translate(
                BuyerFaction, Buyer, new SealShipmentCommand(Buyer.Id, Seller.Id, items, fee));
        }

        /// <summary>The goal the claim command would create for a ship.</summary>
        public Goal GoalFor(ShipmentCargo shipment) => new Goal(GoalType.DeliverShipment)
        {
            Name = "Deliver shipment",
            CargoId = shipment.UniqueID,
            SourceEntityId = Seller.Id,
            DestEntityId = Buyer.Id,
        };

        public void InRange()
        {
            Seller.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
            Buyer.GetDataBlob<CargoStorageDB>().TransferRangeDv_mps = 1e12;
        }

        public static Scene Build(
            int holdUnits = 10,
            double holdVolume = 0,
            decimal buyerFunds = 1000,
            double buyerVolume = 100000,
            bool sellerTradesWithBuyer = true,
            bool buyerOwnsSeller = false,
            bool carrierIsBuyer = false,
            long stockTitanium = 0)
        {
            var modLoader = new ModLoader();
            var store = new ModDataStore();
            modLoader.LoadModManifest("Data/basemod/modInfo.json", store);
            var game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);

            var sellerFaction = FactionFactory.CreateFaction(game, "Sellers");
            var buyerFaction = buyerOwnsSeller ? sellerFaction : FactionFactory.CreateFaction(game, "Buyers");
            var carrier = carrierIsBuyer ? buyerFaction : FactionFactory.CreateFaction(game, "Carriers");
            var factions = new[] { sellerFaction, buyerFaction, carrier }.Distinct().ToList();
            foreach (var faction in factions)
            {
                var info = faction.GetDataBlob<FactionInfoDB>();
                info.Data.Unlock("iron");
                info.Data.Unlock("rp-1");
                info.Data.Unlock("titanium");
                foreach (var other in factions)
                {
                    if (other.Id == faction.Id)
                        continue;
                    bool grant = sellerTradesWithBuyer || faction.Id != sellerFaction.Id || other.Id != buyerFaction.Id;
                    info.Stances[other.Id] = FactionStance.Friendly;
                    FactionStanceRules.SetGrant(info, other.Id, FactionGrants.Trade, grant);
                }
            }
            var sellerInfo = sellerFaction.GetDataBlob<FactionInfoDB>();
            var buyerInfo = buyerFaction.GetDataBlob<FactionInfoDB>();
            if (buyerFunds > 0)
                buyerInfo.Money.AddIncome(game.TimePulse.GameGlobalDateTime, TransactionCategory.InitialInvestment, "test", buyerFunds);

            var system = new StarSystem();
            system.Initialize(game, "Shipments", -1);
            var star = Entity.Create();
            system.AddEntity(star, new List<BaseDataBlob>
            {
                new PositionDB(0, 0, 0),
                MassVolumeDB.NewFromMassAndRadius_m(1.989e30, 6.96342e8),
            });

            var goods = sellerInfo.Data.CargoGoods;
            var scene = new Scene
            {
                Game = game,
                System = system,
                Star = star,
                SellerFaction = sellerFaction,
                BuyerFaction = buyerFaction,
                Carrier = carrier,
                Iron = goods.GetAny("iron"),
                Fuel = goods.GetAny("rp-1"),
                Titanium = goods.GetAny("titanium"),
                SellerInfo = sellerInfo,
                BuyerInfo = buyerInfo,
                CarrierInfo = carrier.GetDataBlob<FactionInfoDB>(),
            };
            scene.Seller = scene.Colony(sellerFaction, new Vector3(1.496e11, 0, 0), 100000, stockTitanium, seller: true);
            scene.Buyer = scene.Colony(buyerFaction, new Vector3(2.28e11, 0, 0), buyerVolume, 0);
            scene.CarrierShip = scene.ShipAt(carrier, new Vector3(5e11, 0, 0), 1, holdUnits, holdVolume);
            return scene;
        }

        public Entity ShipAt(Entity owner, Vector3 at, int commanderId, int holdUnits, double holdVolume = 0)
        {
            double hold = holdVolume > 0 ? holdVolume : Iron.VolumePerUnit * (holdUnits + 0.5);
            var ship = Entity.Create(owner.Id);
            var admin = new AdminSpaceDB();
            admin.CommanderSeats.Add(new AdminSpaceAbilityState(AdminLevel.System, "command-bridge"));
            System.AddEntity(ship, new List<BaseDataBlob>
            {
                new ShipInfoDB { CommanderID = commanderId },
                admin,
                new CargoStorageDB("general-storage", hold),
                new PositionDB(at, Star),
                new ActionQueueDB(),
                new WarpAbilityDB { MaxSpeed = 1e9 },
                new NewtonThrustAbilityDB("iron"),
                new NewtonMoveDB(Star, Vector3.Zero),
                MassVolumeDB.NewFromMassAndRadius_m(1e6, 20),
            });
            var captain = CommanderFactory.CreateShipCaptain(Game);
            captain.Rank = 6;
            var officer = CommanderFactory.Create(System, owner.Id, captain);
            ship.GetDataBlob<ShipInfoDB>().CommanderID = officer.Id;
            captain.AssignedTo = ship.Id;
            return ship;
        }

        public Entity Colony(Entity owner, Vector3 at, double volume, long stockTitanium, bool seller = false)
        {
            var colony = Entity.Create(owner.Id);
            var cargo = new CargoStorageDB("general-storage", volume);
            cargo.TypeStores["fuel-storage"] = new TypeStore(volume);
            var planet = Entity.Create();
            System.AddEntity(planet, new List<BaseDataBlob>
            {
                new NameDB("Body"),
                new SystemBodyInfoDB { LengthOfDay = TimeSpan.Zero },
                new PositionDB(at, Star),
                MassVolumeDB.NewFromMassAndRadius_m(1e24, 6e6),
            });
            System.AddEntity(colony, new List<BaseDataBlob>
            {
                new NameDB("Colony"),
                new ColonyInfoDB { PlanetEntity = planet },
                cargo,
                new PositionDB(at, Star),
                MassVolumeDB.NewFromMassAndRadius_m(1e24, 6e6),
                new ActionQueueDB(),
                new LogiBaseDB { Capacity = 5 },
            });

            // Only the seller carries stock and listings. The buyer is stocked empty.
            if (seller)
            {
                Assert.That(cargo.AddCargoByUnit(Iron, 100), Is.EqualTo(100));
                Assert.That(cargo.AddCargoByUnit(Fuel, 100), Is.EqualTo(100));
                Assert.That(MarketBook.SetListing(colony, new MarketListing { CargoId = "iron", SellQuantity = 100, Ask = 10 }), Is.True);
                Assert.That(MarketBook.SetListing(colony, new MarketListing { CargoId = "rp-1", SellQuantity = 100, Ask = 5 }), Is.True);
                if (stockTitanium > 0)
                {
                    Assert.That(cargo.AddCargoByUnit(Titanium, stockTitanium), Is.EqualTo(stockTitanium));
                    Assert.That(MarketBook.SetListing(colony, new MarketListing { CargoId = "titanium", SellQuantity = stockTitanium, Ask = 2 }), Is.True);
                }
            }
            return colony;
        }

        public static void Drain(Entity holder)
        {
            var store = holder.GetDataBlob<CargoStorageDB>();
            store.TransferRate = 1_000_000;
            var blob = holder.GetDataBlob<CargoTransferDB>();
            var processor = new CargoTransferProcessor();
            for (int i = 0; i < 8; i++)
            {
                processor.ProcessEntity(blob, 10);
                if (Empty(blob))
                    break;
            }
            Assert.That(Empty(blob), Is.True, "transfer escrow did not clear");
            // Finished orders leave the queue on the next pulse, so a second trip can start a new transfer here.
            holder.Manager.Game.ProcessorManager.GetInstanceProcessor(nameof(ActionQueueProcessor))
                .ProcessEntity(holder, holder.StarSysDateTime);
        }

        static bool Empty(CargoTransferDB blob)
        {
            foreach (var tup in blob.TransferData.EscroHeldInPrimary)
                if (tup.count > 0)
                    return false;
            foreach (var tup in blob.TransferData.EscroHeldInSecondary)
                if (tup.count > 0)
                    return false;
            return true;
        }
    }
}
