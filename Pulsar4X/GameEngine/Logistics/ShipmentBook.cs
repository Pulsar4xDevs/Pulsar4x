using Pulsar4X.Api;
using GameEngine.Engine.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// Seal, cancel, release, unpack and settle sealed shipments. One place for the stock edit, the listing
/// counts and, on the market path, both ledger payments. Does not move a ship and does not run a processor.
/// <para>
/// A shipment sits in the source warehouse as one unit of its own cargo. Its mass and volume are those of the
/// lines still in it, so every edit here moves volume and mass exactly rather than going through the
/// by-unit adders, which truncate on float round-off.
/// </para>
/// </summary>
public static class ShipmentBook
{
    const double Slack = 1e-9;

    // ------------------------------------------------------------------ find

    /// <summary>The shipments held in a warehouse, in id order.</summary>
    public static List<ShipmentCargo> At(Entity holder)
    {
        var found = new List<ShipmentCargo>();
        if (holder == null || !holder.TryGetDataBlob<CargoStorageDB>(out var db))
            return found;
        foreach (var store in db.TypeStores.Values)
        {
            foreach (var (id, cargo) in store.Cargoables)
            {
                if (cargo is ShipmentCargo shipment
                    && store.CurrentStoreInUnits.TryGetValue(id, out var units) && units > 0)
                    found.Add(shipment);
            }
        }
        found.Sort((a, b) => a.ID.CompareTo(b.ID));
        return found;
    }

    public static bool TryFind(Entity holder, string shipmentUniqueId, out ShipmentCargo shipment)
    {
        shipment = null!;
        if (string.IsNullOrEmpty(shipmentUniqueId))
            return false;
        foreach (var candidate in At(holder))
        {
            if (candidate.UniqueID == shipmentUniqueId)
            {
                shipment = candidate;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// The shipment a goal carries. It sits at the source until closed, so look there by <see cref="Goal.CargoId"/>.
    /// </summary>
    internal static bool TryFor(Game game, Goal goal, out ShipmentCargo shipment, out Entity source)
    {
        shipment = null!;
        source = null!;
        if (game?.GlobalManager == null
            || !game.GlobalManager.TryGetGlobalEntityById(goal.SourceEntityId, out source))
            return false;
        return TryFind(source, goal.CargoId, out shipment);
    }

    // ------------------------------------------------------------------ store edits

    static void Put(TypeStore store, ICargoable item, long units)
    {
        if (store.CurrentStoreInUnits.ContainsKey(item.ID))
            store.CurrentStoreInUnits[item.ID] += units;
        else
            store.CurrentStoreInUnits.Add(item.ID, units);
        if (!store.Cargoables.ContainsKey(item.ID))
            store.Cargoables.Add(item.ID, item);
    }

    static void Take(TypeStore store, int id, long units)
    {
        if (!store.CurrentStoreInUnits.ContainsKey(id))
            return;
        long left = store.CurrentStoreInUnits[id] - units;
        if (left > 0)
        {
            store.CurrentStoreInUnits[id] = left;
            return;
        }
        store.CurrentStoreInUnits.Remove(id);
        if (store.Cargoables.ContainsKey(id))
            store.Cargoables.Remove(id);
    }

    static void Charge(CargoStorageDB db, TypeStore store, double volume, long mass)
    {
        store.FreeVolume -= volume;
        db.TotalStoredMass += mass;
    }

    /// <summary>Put a line's open cargo back into the store with its volume and mass counted.</summary>
    static void PutOpen(CargoStorageDB db, ShipmentLine line)
    {
        if (!db.TypeStores.TryGetValue(line.Open.CargoTypeID, out var store))
            return;
        Put(store, line.Open, line.Units);
        Charge(db, store, line.Open.VolumePerUnit * line.Units, line.Open.MassPerUnit * line.Units);
    }

    /// <summary>Take the hold out of the store and give back its volume and mass.</summary>
    static void DropHold(CargoStorageDB db, ShipmentCargo shipment)
    {
        if (!db.TypeStores.TryGetValue(shipment.CargoTypeID, out var store))
            return;
        Charge(db, store, -shipment.VolumePerUnit, -shipment.MassPerUnit);
        store.CurrentStoreInUnits.Remove(shipment.ID);
        store.Cargoables.Remove(shipment.ID);
    }

    // ------------------------------------------------------------------ seal

    /// <summary>
    /// A crate or design a ship's hold would refuse is not freight, so it does not go in a package either.
    /// Resources are tested by whether the source has a hold of their cargo type.
    /// </summary>
    static bool Fits(CargoStorageDB sourceDb, ICargoable item)
    {
        var design = item switch
        {
            ComponentInstance instance => instance.Design,
            ComponentDesign d => d,
            _ => null,
        };
        if (design != null && LocalConstructionJob.IsSiteOnly(design))
            return false;
        return !string.IsNullOrEmpty(item.CargoTypeID) && sourceDb.TypeStores.ContainsKey(item.CargoTypeID);
    }

    /// <summary>
    /// Non-market seal. The chosen cargo leaves open stock and becomes one shipment per cargo type. No buyer, seller,
    /// price, fee, intel row or ledger line. A fleet order uses this at build time and names itself in
    /// <paramref name="fleetOrder"/> so a failed order can release the package.
    /// A crate (a <see cref="ComponentInstance"/>) is one unit. Resources are sealed by whole lines.
    /// </summary>
    public static bool TrySeal(
        Entity source, Entity dest, IReadOnlyList<(ICargoable item, long units)> lines, string fleetOrder,
        out List<ShipmentCargo> made, out string reason)
        => Build(source, dest, lines, null, fleetOrder, -1, -1, false, 0m, out made, out reason);

    static bool Build(
        Entity source, Entity dest, IReadOnlyList<(ICargoable item, long units)> lines, IReadOnlyList<decimal>? prices,
        string fleetOrder, int buyerFaction, int sellerFaction, bool filledBid, decimal fee,
        out List<ShipmentCargo> made, out string reason)
    {
        made = new List<ShipmentCargo>();
        reason = "";
        var game = source?.Manager?.Game;
        if (source == null || dest == null || game == null)
        {
            reason = "The colony is gone.";
            return false;
        }
        if (!source.TryGetDataBlob<CargoStorageDB>(out var db) || !dest.HasDataBlob<CargoStorageDB>())
        {
            reason = "The colony has no warehouse.";
            return false;
        }
        if (source.Id == dest.Id)
        {
            reason = "Source and destination are the same.";
            return false;
        }
        if (lines == null || lines.Count == 0)
        {
            reason = "Nothing to seal.";
            return false;
        }

        // Check everything first, so a bad line leaves the stock alone.
        var seen = new HashSet<int>();
        for (int i = 0; i < lines.Count; i++)
        {
            var (item, units) = lines[i];
            if (item == null || units <= 0)
            {
                reason = "Nothing to seal.";
                return false;
            }
            if (!seen.Add(item.ID))
            {
                reason = "A good appears twice.";
                return false;
            }
            if (item is ComponentInstance && units != 1)
            {
                reason = "A crate is one unit.";
                return false;
            }
            if (!Fits(db, item))
            {
                reason = item.Name + " does not fit a ship's hold.";
                return false;
            }
            if (db.GetUnitsStored(item, false) < units)
            {
                reason = "Not enough " + item.Name + " in stock.";
                return false;
            }
        }

        var byType = new Dictionary<string, ShipmentCargo>(StringComparer.Ordinal);
        var order = new List<ShipmentCargo>();
        for (int i = 0; i < lines.Count; i++)
        {
            var (item, units) = lines[i];
            if (!byType.TryGetValue(item.CargoTypeID, out var shipment))
            {
                shipment = new ShipmentCargo(Game.GetEntityID(), item.CargoTypeID)
                {
                    SourceId = source.Id,
                    DestId = dest.Id,
                    BuyerFactionId = buyerFaction,
                    SellerFactionId = sellerFaction,
                    FilledBid = filledBid,
                    FleetOrder = fleetOrder ?? "",
                };
                byType[item.CargoTypeID] = shipment;
                order.Add(shipment);
            }

            ICargoable held = item;
            if (item is not ComponentInstance)
                held = new SealedLineCargo(Game.GetEntityID(), shipment.ID, item);
            shipment.Lines.Add(new ShipmentLine
            {
                ItemId = item.ID,
                CargoUniqueId = item.UniqueID,
                Units = units,
                Price = prices != null ? prices[i] : 0m,
                Open = item,
                Held = held,
            });
        }

        foreach (var (item, units) in lines)
            db.RemoveCargoByUnit(item, units);
        foreach (var shipment in order)
        {
            var store = db.TypeStores[shipment.CargoTypeID];
            Put(store, shipment, 1);
            Charge(db, store, shipment.VolumePerUnit, shipment.MassPerUnit);
        }

        made = order;
        return true;
    }

    // ------------------------------------------------------------------ market seal

    /// <summary>
    /// The buying office seals goods at <paramref name="seller"/> and posts the haul on itself.
    /// Buy: each line clamps to the seller's SellQuantity, Sellable and the buyer's funds, at the seller's ask.
    /// Fill a bid: each line clamps to the buyer's BuyQuantity and the seller's stock, at the buyer's bid.
    /// A cross-faction line writes one ledger pair. Ask or bid 0, or the same faction, writes none.
    /// </summary>
    public static bool TrySealMarket(
        Entity buyer, Entity seller, bool fillBid, IReadOnlyList<(ICargoable item, long units)> wanted, decimal fee,
        out List<ShipmentCargo> made, out string reason)
    {
        made = new List<ShipmentCargo>();
        reason = "";
        var game = buyer?.Manager?.Game;
        if (buyer == null || seller == null || game == null)
        {
            reason = "The office is gone.";
            return false;
        }
        if (!buyer.HasDataBlob<LogiBaseDB>() || !seller.HasDataBlob<LogiBaseDB>())
        {
            reason = "Both colonies need a logistics office.";
            return false;
        }
        if (!FactionStanceRules.CanTrade(game, buyer.FactionOwnerID, seller.FactionOwnerID))
        {
            reason = "Trade is not mutual.";
            return false;
        }
        if (fee < 0)
        {
            reason = "The fee cannot be negative.";
            return false;
        }
        if (wanted == null || wanted.Count == 0 || !seller.TryGetDataBlob<CargoStorageDB>(out var sellerDb))
        {
            reason = "Nothing to buy.";
            return false;
        }

        bool sameFaction = buyer.FactionOwnerID == seller.FactionOwnerID;
        if (!game.Factions.TryGetValue(buyer.FactionOwnerID, out var buyerFaction)
            || !game.Factions.TryGetValue(seller.FactionOwnerID, out var sellerFaction)
            || !buyerFaction.TryGetDataBlob<FactionInfoDB>(out var buyerInfo)
            || !sellerFaction.TryGetDataBlob<FactionInfoDB>(out var sellerInfo))
        {
            reason = "Faction has no ledger.";
            return false;
        }

        decimal funds = buyerInfo.Money.GetCurrentFunds();
        var picked = new List<(ICargoable item, long units)>();
        var prices = new List<decimal>();
        var listings = new List<MarketListing>();
        var seenIds = new HashSet<int>();
        foreach (var (item, units) in wanted)
        {
            if (item == null || units <= 0 || !seenIds.Add(item.ID))
                continue;

            MarketListing listing;
            long take;
            decimal price;
            if (!fillBid)
            {
                if (!MarketBook.TryGet(seller, item.UniqueID, out listing))
                    continue;
                take = Math.Min(units, Math.Min(listing.SellQuantity, MarketBook.Sellable(seller, listing, item)));
                price = listing.Ask;
            }
            else
            {
                if (!MarketBook.TryGet(buyer, item.UniqueID, out listing))
                    continue;
                long stock = MarketBook.Stock(seller, item);
                if (MarketBook.TryGet(seller, item.UniqueID, out var own))
                    stock = MarketBook.Sellable(seller, own, item);
                take = Math.Min(units, Math.Min(listing.BuyQuantity, stock));
                price = listing.Bid;
            }

            if (take > 0 && !sameFaction && price > 0)
            {
                long afford = (long)Math.Floor(funds / price);
                take = Math.Min(take, afford);
            }
            if (take <= 0)
                continue;
            if (!Fits(sellerDb, item))
                continue;

            if (!sameFaction && price > 0)
                funds -= take * price;
            picked.Add((item, take));
            prices.Add(sameFaction ? 0m : price);
            listings.Add(listing);
        }

        if (picked.Count == 0)
        {
            reason = "Nothing to buy.";
            return false;
        }

        if (!Build(seller, buyer, picked, prices, "", buyer.FactionOwnerID, seller.FactionOwnerID, fillBid, 0m,
                out made, out reason))
            return false;

        // Count down the listing and write the goods lines.
        for (int i = 0; i < picked.Count; i++)
        {
            if (fillBid)
                listings[i].BuyQuantity -= picked[i].units;
            else
                listings[i].SellQuantity -= picked[i].units;
        }
        var now = buyer.StarSysDateTime;
        foreach (var shipment in made)
        {
            foreach (var line in shipment.Lines)
            {
                if (line.Price <= 0)
                    continue;
                decimal amount = line.Price * line.Units;
                buyerInfo.Money.AddExpense(now, TransactionCategory.Trade,
                    $"shipment {shipment.ID} {line.CargoUniqueId} {seller.FactionOwnerID}", amount);
                sellerInfo.Money.AddIncome(now, TransactionCategory.Trade,
                    $"shipment {shipment.ID} {line.CargoUniqueId} {buyer.FactionOwnerID}", amount);
            }
        }

        // One fee for the whole seal, split equally over the shipments with the remainder on the last.
        decimal share = Math.Round(fee / made.Count, 2, MidpointRounding.ToZero);
        decimal posted = 0;
        for (int i = 0; i < made.Count; i++)
        {
            var shipment = made[i];
            shipment.Fee = i == made.Count - 1 ? fee - posted : share;
            posted += shipment.Fee;
            IntelBook.PostHaul(buyer, shipment.UniqueID, shipment.Fee, seller.Id, buyer.Id);
        }
        return true;
    }

    // ------------------------------------------------------------------ cancel

    /// <summary>
    /// The buying office takes a posted haul back. Only while no ship has claimed the row and every line is still
    /// in the source warehouse. Restores every line to open stock, the listing counts and both ledgers, and removes the row.
    /// </summary>
    public static bool TryCancel(Entity office, string shipmentId, out string reason)
    {
        reason = "";
        var game = office?.Manager?.Game;
        if (office == null || game == null
            || !office.TryGetDataBlob<LogiBaseDB>(out var book) || book.Intel == null
            || !book.Intel.TryGetValue(IntelBook.Key(IntelKind.Haul, shipmentId ?? ""), out var row))
        {
            reason = "That haul is not listed.";
            return false;
        }
        if (row.ClaimantId >= 0 || !row.ForSale)
        {
            reason = "A ship has claimed that haul.";
            return false;
        }
        if (!game.GlobalManager.TryGetGlobalEntityById(row.SourceId, out var source)
            || !TryFind(source, shipmentId!, out var shipment))
        {
            book.Intel.Remove(IntelBook.Key(IntelKind.Haul, shipmentId!));
            reason = "The shipment is gone.";
            return false;
        }
        foreach (var line in shipment.Lines)
        {
            if (line.Released)
            {
                reason = "Part of the shipment is already aboard.";
                return false;
            }
        }

        UnsealInPlace(source, shipment);

        // Listing counts and ledgers.
        if (shipment.IsMarket)
        {
            var listingHolder = shipment.FilledBid ? office : source;
            foreach (var line in shipment.Lines)
            {
                if (MarketBook.TryGet(listingHolder, line.CargoUniqueId, out var listing))
                {
                    if (shipment.FilledBid)
                        listing.BuyQuantity += line.Units;
                    else
                        listing.SellQuantity += line.Units;
                }
            }
            Reverse(game, shipment, source, office);
        }

        book.Intel.Remove(IntelBook.Key(IntelKind.Haul, shipmentId!));
        return true;
    }

    static void Reverse(Game game, ShipmentCargo shipment, Entity source, Entity buyer)
    {
        if (!game.Factions.TryGetValue(shipment.BuyerFactionId, out var buyerFaction)
            || !game.Factions.TryGetValue(shipment.SellerFactionId, out var sellerFaction)
            || !buyerFaction.TryGetDataBlob<FactionInfoDB>(out var buyerInfo)
            || !sellerFaction.TryGetDataBlob<FactionInfoDB>(out var sellerInfo))
            return;
        var now = buyer.StarSysDateTime;
        foreach (var line in shipment.Lines)
        {
            if (line.Price <= 0)
                continue;
            decimal amount = line.Price * line.Units;
            buyerInfo.Money.AddIncome(now, TransactionCategory.Trade,
                $"shipment {shipment.ID} {line.CargoUniqueId} {shipment.SellerFactionId} cancelled", amount);
            sellerInfo.Money.AddExpense(now, TransactionCategory.Trade,
                $"shipment {shipment.ID} {line.CargoUniqueId} {shipment.BuyerFactionId} cancelled", amount);
        }
    }

    // ------------------------------------------------------------------ release (fleet order failed or cancelled)

    /// <summary>
    /// Give a package back to open stock. Lines still in the hold go back to their open cargo id. A line that already left
    /// the hold is unsealed wherever it is: in the source store, on its carrier, or in the destination. Units on a ship
    /// that is mid-transfer are left alone and are unsealed when they land. Non-market packages only.
    /// The shipment is closed. Returns false when there is nothing to release.
    /// </summary>
    public static bool Release(Game game, ShipmentCargo shipment)
    {
        if (game?.GlobalManager == null || shipment == null || shipment.IsMarket)
            return false;
        if (!game.GlobalManager.TryGetGlobalEntityById(shipment.SourceId, out var source))
            return false;

        UnsealInPlace(source, shipment);
        foreach (var line in shipment.Lines)
        {
            if (!line.Released || line.IsCrate || line.Arrived)
                continue;
            UnsealWhereFound(game, shipment, line);
        }
        return true;
    }

    /// <summary>Return the hold's own lines to open stock, drop the hold, and drop released lines' hold claim.</summary>
    static void UnsealInPlace(Entity source, ShipmentCargo shipment)
    {
        if (!source.TryGetDataBlob<CargoStorageDB>(out var db))
            return;
        var unreleased = shipment.Lines.Where(line => !line.Released).ToList();
        DropHold(db, shipment);
        foreach (var line in unreleased)
            PutOpen(db, line);
        // Mark as released so the lines are not counted again; the hold is gone.
        foreach (var line in unreleased)
        {
            line.Released = true;
            line.Delivered = line.Units;
        }
    }

    static void UnsealWhereFound(Game game, ShipmentCargo shipment, ShipmentLine line)
    {
        var holders = new List<Entity>();
        if (game.GlobalManager.TryGetGlobalEntityById(shipment.SourceId, out var source))
            holders.Add(source);
        if (line.CarrierId >= 0 && game.GlobalManager.TryGetGlobalEntityById(line.CarrierId, out var carrier))
            holders.Add(carrier);
        if (game.GlobalManager.TryGetGlobalEntityById(shipment.DestId, out var dest))
            holders.Add(dest);
        foreach (var holder in holders)
        {
            if (!holder.TryGetDataBlob<CargoStorageDB>(out var db))
                continue;
            long units = db.GetUnitsStored(line.Held, false);
            if (units > 0)
                Swap(db, line, units);
        }
    }

    /// <summary>Swap sealed units for the open resource inside one store. Volume and mass do not change.</summary>
    static void Swap(CargoStorageDB db, ShipmentLine line, long units)
    {
        if (!db.TypeStores.TryGetValue(line.Held.CargoTypeID, out var store) || !db.TypeStores.ContainsKey(line.Open.CargoTypeID))
            return;
        var openStore = db.TypeStores[line.Open.CargoTypeID];
        Take(store, line.Held.ID, units);
        Put(openStore, line.Open, units);
        // The sealed and open volume are equal by construction. Correct for any library change.
        openStore.FreeVolume -= (line.Open.VolumePerUnit - line.Held.VolumePerUnit) * units;
        db.TotalStoredMass += (line.Open.MassPerUnit - line.Held.MassPerUnit) * units;
    }

    // ------------------------------------------------------------------ haul

    /// <summary>
    /// Move one unreleased line out of the hold and into the source store as itself, ready to load.
    /// Volume and mass do not change: the line leaves the hold's total and enters the store's own.
    /// </summary>
    internal static void ReleaseLine(Entity source, ShipmentCargo shipment, ShipmentLine line, int carrierId)
    {
        if (line.Released || !source.TryGetDataBlob<CargoStorageDB>(out var db)
            || !db.TypeStores.TryGetValue(line.Held.CargoTypeID, out var store))
            return;
        line.Released = true;
        line.CarrierId = carrierId;
        Put(store, line.Held, line.Units);
        // The hold shrank by this line, which the line itself now fills. Nothing to charge.
    }

    /// <summary>The line did not load. Return it to the hold.</summary>
    internal static void Rehold(Entity source, ShipmentCargo shipment, ShipmentLine line)
    {
        if (!line.Released || !source.TryGetDataBlob<CargoStorageDB>(out var db)
            || !db.TypeStores.TryGetValue(line.Held.CargoTypeID, out var store))
            return;
        long have = db.GetUnitsStored(line.Held, false);
        if (have < line.Units)
            return;
        Take(store, line.Held.ID, line.Units);
        line.Released = false;
        line.CarrierId = -1;
    }

    // ------------------------------------------------------------------ unpack and settle

    /// <summary>
    /// Unpack what has landed in the destination: sealed units become open units of their resource, and a crate that
    /// has arrived counts as delivered. Returns the units unpacked.
    /// </summary>
    internal static long Unpack(Entity dest, ShipmentCargo shipment)
    {
        if (!dest.TryGetDataBlob<CargoStorageDB>(out var db))
            return 0;
        long moved = 0;
        foreach (var line in shipment.Lines)
        {
            if (!line.Released || line.Arrived)
                continue;
            if (line.IsCrate)
            {
                // A crate has arrived when it sits in the destination's cargo, or is already mounted there
                // (a ship that mounts its own crates never unloads them).
                if (db.GetUnitsStored(line.Held, false) >= line.Units
                    || (line.Held is ComponentInstance crate && MountFromHoldAction.Mounted(dest, crate.ID)))
                {
                    line.Delivered = line.Units;
                    moved += line.Units;
                }
                continue;
            }
            long here = db.GetUnitsStored(line.Held, false);
            if (here <= 0)
                continue;
            long take = Math.Min(here, line.Units - line.Delivered);
            Swap(db, line, take);
            line.Delivered += take;
            moved += take;
        }
        return moved;
    }

    /// <summary>
    /// A fleet Bootstrap package that has landed but still holds a crate the destination has not mounted. Its hold stays
    /// open until the fleet order sees every crate mounted, so the order can still count them.
    /// </summary>
    internal static bool AwaitsMount(Entity dest, ShipmentCargo shipment)
    {
        if (!shipment.FleetOrder.StartsWith(BootstrapPackage.OrderPrefix, StringComparison.Ordinal))
            return false;
        foreach (var line in shipment.Lines)
        {
            if (line.Held is ComponentInstance crate && !MountFromHoldAction.Mounted(dest, crate.ID))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Every line has arrived: pay the fee, remove the intel row and close the package.
    /// The fee is paid to the carrier's faction by the buyer, and only when the carrier is another faction and the fee is above 0.
    /// The buyer pays what its funds allow.
    /// </summary>
    internal static void Complete(Game game, ShipmentCargo shipment, Entity source, int carrierFactionId, DateTime at)
    {
        if (shipment.IsMarket)
        {
            if (shipment.DestId >= 0 && game.GlobalManager.TryGetGlobalEntityById(shipment.DestId, out var office))
            {
                if (shipment.Fee > 0 && carrierFactionId != shipment.BuyerFactionId
                    && game.Factions.TryGetValue(shipment.BuyerFactionId, out var buyerFaction)
                    && game.Factions.TryGetValue(carrierFactionId, out var carrierFaction)
                    && buyerFaction.TryGetDataBlob<FactionInfoDB>(out var buyerInfo)
                    && carrierFaction.TryGetDataBlob<FactionInfoDB>(out var carrierInfo))
                {
                    decimal pay = Math.Min(shipment.Fee, Math.Max(0m, buyerInfo.Money.GetCurrentFunds()));
                    if (pay > 0)
                    {
                        buyerInfo.Money.AddExpense(at, TransactionCategory.Trade,
                            $"haul {shipment.ID} {carrierFactionId}", pay);
                        carrierInfo.Money.AddIncome(at, TransactionCategory.Trade,
                            $"haul {shipment.ID} {shipment.BuyerFactionId}", pay);
                    }
                }
                if (office.TryGetDataBlob<LogiBaseDB>(out var book) && book.Intel != null)
                    book.Intel.Remove(IntelBook.Key(IntelKind.Haul, shipment.UniqueID));
            }
        }
        Close(source, shipment);
    }

    /// <summary>Remove the hold from the source. Every line has left it, so no volume or mass is held.</summary>
    internal static void Close(Entity source, ShipmentCargo shipment)
    {
        if (source == null || !source.TryGetDataBlob<CargoStorageDB>(out var db)
            || !db.TypeStores.TryGetValue(shipment.CargoTypeID, out var store))
            return;
        store.CurrentStoreInUnits.Remove(shipment.ID);
        store.Cargoables.Remove(shipment.ID);
    }
}
