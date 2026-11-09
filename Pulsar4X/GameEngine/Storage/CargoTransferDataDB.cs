using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;

namespace Pulsar4X.Storage;

public class CargoTransferDataDB : BaseDataBlob
{
    [JsonProperty]
    internal Entity PrimaryEntity { get; private set; }
    [JsonProperty]
    internal Entity SecondaryEntity { get; private set; }
    
    /// <summary>
    /// positive amounts move INTO this entity, negitive amounts move OUT from this entity
    /// </summary>
    [JsonProperty]
    internal CargoStorageDB PrimaryStorageDB { get; private set; }
    
    /// <summary>
    /// positive amounts move OUT from this entity, negitive amounts move INTO this entity.
    /// </summary>
    [JsonProperty]
    internal CargoStorageDB SecondaryStorageDB { get; private set; }
    [JsonProperty]
    internal List<(ICargoable item, long amount)> OrderedToTransfer { get; private set; }

    [JsonProperty]
    internal SafeList<(ICargoable item, long count, double mass)> EscroHeldInPrimary { get; private set; } = new();
    [JsonProperty]
    internal SafeList<(ICargoable item, long count, double mass)> EscroHeldInSecondary { get; private set; } = new();

    /// <summary>
    /// Cancel already put every held unit back. A second Stop must not add them again.
    /// </summary>
    [JsonProperty]
    internal bool EscrowReturned { get; private set; }

    /// <summary>
    /// Colonists going aboard. Escrow subtracts them from the colony as well as the hold, so
    /// a partial transfer, a rollback and a cancel all conserve people and species.
    /// </summary>
    [JsonProperty]
    internal bool PassengersBoarded { get; private set; }

    /// <summary>
    /// A passenger line was refused because the two sides may not trade. Counted as taken
    /// so the order finishes empty instead of moving someone else's people.
    /// </summary>
    [JsonProperty]
    internal bool CrossFactionPassengers { get; private set; }

    [JsonConstructor]
    private CargoTransferDataDB(){}
        
    internal CargoTransferDataDB(Entity primary, Entity secondary, List<(ICargoable item, long amount)> itemsToTransfer)
    {
        PrimaryEntity = primary;
        SecondaryEntity = secondary;
        PrimaryStorageDB = primary.GetDataBlob<CargoStorageDB>();
        SecondaryStorageDB = secondary.GetDataBlob<CargoStorageDB>();
        OrderedToTransfer = itemsToTransfer;
        
        PrimaryStorageDB.EscroItems.Add(this);
        SecondaryStorageDB.EscroItems.Add(this);

        // Without mutual trade, cross-faction passenger lines escrow nothing.
        CrossFactionPassengers = CrossesFactions(itemsToTransfer);
        if (CrossFactionPassengers)
            return;

        for (int index = 0; index < OrderedToTransfer.Count; index++)
        {
            (ICargoable item, long amount) tuple = OrderedToTransfer[index];
            HoldForTransfer(tuple.item, tuple.amount);
        }

        MovePassengersOffColonies();
    }

    bool CrossesFactions(List<(ICargoable item, long amount)> itemsToTransfer)
    {
        if (PrimaryEntity == null || SecondaryEntity == null)
            return false;
        if (PrimaryEntity.FactionOwnerID == SecondaryEntity.FactionOwnerID)
            return false;
        bool passengers = false;
        foreach (var (item, _) in itemsToTransfer)
        {
            if (item is SpeciesCargo)
            {
                passengers = true;
                break;
            }
        }
        if (!passengers)
            return false;
        // Mutual trade lets a freighter board or land people at another faction's colony.
        var game = PrimaryEntity.Manager?.Game ?? SecondaryEntity.Manager?.Game;
        if (game != null && FactionStanceRules.CanTrade(game, PrimaryEntity.FactionOwnerID, SecondaryEntity.FactionOwnerID))
            return false;
        return true;
    }

    /// <summary>
    /// People taken into escrow leave the colony now. Escrow does not go through the store's
    /// remove, so this is the one step that takes them off the colony's books. A cancel puts them
    /// back, which is why one flag covers every line of the transfer.
    /// </summary>
    void MovePassengersOffColonies()
    {
        if (PassengersBoarded)
            return;
        PassengersBoarded = true;
        if (CrossFactionPassengers)
            return;
        TakeFromColony(PrimaryEntity, EscroHeldInPrimary);
        TakeFromColony(SecondaryEntity, EscroHeldInSecondary);
    }

    static void TakeFromColony(Entity colony, SafeList<(ICargoable item, long count, double mass)> held)
    {
        if (colony == null || !colony.TryGetDataBlob<ColonyInfoDB>(out var info))
            return;
        foreach (var (item, count, _) in held)
        {
            if (item is not SpeciesCargo passengers || count <= 0)
                continue;
            if (!info.Population.TryGetValue(passengers.SpeciesId, out var people))
                continue;
            long left = Math.Max(0, people - count);
            if (left == 0)
                info.Population.Remove(passengers.SpeciesId);
            else
                info.Population[passengers.SpeciesId] = left;
        }
    }

    internal void UpdateEscro(ICargoable cargoItem, long unitAmount)
    {
        for (int index = 0; index < OrderedToTransfer.Count; index++)
        {
            (ICargoable item, long amount) tuple = OrderedToTransfer[index];
            if(tuple.item != cargoItem)
                continue;
            OrderedToTransfer[index] = (cargoItem, tuple.amount + unitAmount);
            HoldForTransfer(cargoItem, unitAmount);
            break;
        }
    }

    /// <summary>
    /// Reserve units for one line. Volume and mass stay where they are: the space stays reserved
    /// until delivery frees it once, and a cancel only puts the units back.
    /// People on a colony are not warehouse stock. A load counts them off the population, and an
    /// unload counts them out of the ship's passenger store. A colony has no passenger store, so
    /// this must not index one.
    /// </summary>
    void HoldForTransfer(ICargoable cargoItem, long unitAmount)
    {
        if (cargoItem == null)
            return;
        bool fromPrimary = unitAmount < 0;
        if (fromPrimary)
            unitAmount = -unitAmount;
        var sourceStore = fromPrimary ? PrimaryStorageDB : SecondaryStorageDB;
        var destStore = fromPrimary ? SecondaryStorageDB : PrimaryStorageDB;
        var sourceEntity = fromPrimary ? PrimaryEntity : SecondaryEntity;
        var destEntity = fromPrimary ? SecondaryEntity : PrimaryEntity;
        var held = fromPrimary ? EscroHeldInPrimary : EscroHeldInSecondary;

        if (cargoItem is SpeciesCargo passengers && OnColonyBooks(sourceEntity))
        {
            long take = Math.Min(unitAmount, PeopleOn(sourceEntity, passengers));
            // Every species shares the passenger volume. Free-unit space only sees this species,
            // so a second species would board into berths the first species already reserved.
            take = Math.Min(take, PassengerBerthsLeft(destStore, passengers));
            held.Add((cargoItem, take, cargoItem.MassPerUnit * take));
            return;
        }

        if (sourceStore == null
            || !sourceStore.TypeStores.TryGetValue(cargoItem.CargoTypeID, out var store)
            || !store.CurrentStoreInUnits.TryGetValue(cargoItem.ID, out var amountInStore))
        {
            held.Add((cargoItem, 0, 0));
            return;
        }

        long room = long.MaxValue;
        if (!(cargoItem is SpeciesCargo && OnColonyBooks(destEntity)))
        {
            if (destStore != null && destStore.TypeStores.ContainsKey(cargoItem.CargoTypeID))
                room = Math.Max(0, CargoMath.GetFreeUnitSpace(destStore, cargoItem));
            else
                room = 0;
        }

        long amountToRemove = Math.Min(unitAmount, Math.Min(amountInStore, room));
        store.CurrentStoreInUnits[cargoItem.ID] -= amountToRemove;
        held.Add((cargoItem, amountToRemove, cargoItem.MassPerUnit * amountToRemove));
    }

    static bool OnColonyBooks(Entity entity)
    {
        return entity != null && entity.IsValid && entity.HasDataBlob<ColonyInfoDB>();
    }

    static long PeopleOn(Entity colony, SpeciesCargo passengers)
    {
        if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info))
            return 0;
        return info.Population.TryGetValue(passengers.SpeciesId, out var people) ? Math.Max(0, people) : 0;
    }

    /// <summary>
    /// Berths still free on the destination, after every species already held in escrow.
    /// Volume stays reserved until delivery, so the free-volume figure alone still shows the
    /// whole hold. Do not also call <see cref="CargoMath.GetFreeUnitSpace"/>: that subtracts
    /// only this item's id and would count those units twice.
    /// </summary>
    static long PassengerBerthsLeft(CargoStorageDB dest, SpeciesCargo passengers)
    {
        if (dest == null || passengers.VolumePerUnit <= 0)
            return 0;
        if (!dest.TypeStores.TryGetValue(passengers.CargoTypeID, out var store))
            return 0;
        long berths = (long)(store.FreeVolume / passengers.VolumePerUnit);
        long reserved = 0;
        foreach (var transfer in dest.EscroItems)
        {
            reserved += SpeciesUnits(transfer.EscroHeldInPrimary);
            reserved += SpeciesUnits(transfer.EscroHeldInSecondary);
        }
        long left = berths - reserved;
        return left > 0 ? left : 0;
    }

    static long SpeciesUnits(SafeList<(ICargoable item, long count, double mass)> held)
    {
        long units = 0;
        foreach (var (item, count, _) in held)
        {
            if (item is SpeciesCargo && count > 0)
                units += count;
        }
        return units;
    }

    /// <summary>
    /// Put every unit still in escrow back on the store it was taken from.
    /// Escrow only subtracted <see cref="TypeStore.CurrentStoreInUnits"/>. Free volume and mass
    /// stay as they were, so this must not call <c>AddCargoByUnit</c>.
    /// Units the processor already moved stay where they landed.
    /// </summary>
    internal void ReturnEscrow()
    {
        if (EscrowReturned)
            return;
        EscrowReturned = true;
        Restore(SecondaryStorageDB, EscroHeldInSecondary);
        Restore(PrimaryStorageDB, EscroHeldInPrimary);
        if (PassengersBoarded && !CrossFactionPassengers)
        {
            ReturnToColony(SecondaryEntity, EscroHeldInSecondary);
            ReturnToColony(PrimaryEntity, EscroHeldInPrimary);
        }
        EscroHeldInSecondary.Clear();
        EscroHeldInPrimary.Clear();
        PrimaryStorageDB?.EscroItems.Remove(this);
        SecondaryStorageDB?.EscroItems.Remove(this);
        DropBlob(PrimaryEntity);
        DropBlob(SecondaryEntity);
    }

    void DropBlob(Entity entity)
    {
        if (entity == null || !entity.TryGetDataBlob<CargoTransferDB>(out var blob))
            return;
        if (ReferenceEquals(blob.TransferData, this))
            entity.RemoveDataBlob<CargoTransferDB>();
    }

    static void Restore(CargoStorageDB store, SafeList<(ICargoable item, long count, double mass)> held)
    {
        if (store == null)
            return;
        foreach (var (item, count, _) in held)
        {
            if (item == null || count <= 0)
                continue;
            if (!store.TypeStores.TryGetValue(item.CargoTypeID, out var type))
                continue;
            if (type.CurrentStoreInUnits.ContainsKey(item.ID))
                type.CurrentStoreInUnits[item.ID] += count;
            else
                type.CurrentStoreInUnits.Add(item.ID, count);
            if (!type.Cargoables.ContainsKey(item.ID))
                type.Cargoables.Add(item.ID, item);
        }
    }

    /// <summary>
    /// A cancelled boarding puts the people back on the colony. They were taken off the colony's
    /// books when escrow started, so this is the mirror of <see cref="TakeFromColony"/>.
    /// </summary>
    static void ReturnToColony(Entity colony, SafeList<(ICargoable item, long count, double mass)> held)
    {
        if (colony == null || !colony.TryGetDataBlob<ColonyInfoDB>(out var info))
            return;
        foreach (var (item, count, _) in held)
        {
            if (item is not SpeciesCargo passengers || count <= 0)
                continue;
            if (info.Population.ContainsKey(passengers.SpeciesId))
                info.Population[passengers.SpeciesId] += count;
            else
                info.Population.Add(passengers.SpeciesId, count);
        }
    }

    public override object Clone()
    {
        return new CargoStorageDB();
    }
}