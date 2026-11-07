using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
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
    /// A passenger line was refused because the two sides are not one faction. Counted as taken
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

        // A colony's people stay with their faction. Cross-faction boarding escrows nothing.
        CrossFactionPassengers = CrossesFactions(itemsToTransfer);
        if (CrossFactionPassengers)
            return;

        for (int index = 0; index < OrderedToTransfer.Count; index++)
        {
            (ICargoable item, long amount) tuple = OrderedToTransfer[index];
            var cargoItem = tuple.item;
            var unitAmount = tuple.amount;
            TypeStore store;
            SafeList<(ICargoable item, long count, double mass)> itemsToRemove; //reference which list.
            long unitsStorable; 
            if (unitAmount < 0) //we're moving items from primary to secondary
            {
                unitAmount *= -1;
                store = PrimaryStorageDB.TypeStores[cargoItem.CargoTypeID];
                itemsToRemove = EscroHeldInPrimary;
                unitsStorable = CargoMath.GetFreeUnitSpace(SecondaryStorageDB, cargoItem);
            }
            else   //we're moving items from secondary to primary
            {
                store = SecondaryStorageDB.TypeStores[cargoItem.CargoTypeID];
                itemsToRemove = EscroHeldInSecondary;
                unitsStorable = CargoMath.GetFreeUnitSpace(PrimaryStorageDB, cargoItem);
            }
            if (store.CurrentStoreInUnits.ContainsKey(cargoItem.ID))
            {
                long amountInStore = store.CurrentStoreInUnits[cargoItem.ID];
                
                long amountToRemove = Math.Min(unitAmount, amountInStore);
                amountToRemove = Math.Min(unitsStorable, amountToRemove);
                store.CurrentStoreInUnits[cargoItem.ID] -= amountToRemove;
                double massToRemove = cargoItem.MassPerUnit * amountToRemove;
                itemsToRemove.Add((cargoItem,amountToRemove, massToRemove));
            }
            else
            {
                //in this case we're trying to remove items that don't exist. not sure how we should handle this yet.
                itemsToRemove.Add((cargoItem,0,0));
            }
        }

        MovePassengersOffColonies();
    }

    bool CrossesFactions(List<(ICargoable item, long amount)> itemsToTransfer)
    {
        if (PrimaryEntity == null || SecondaryEntity == null)
            return false;
        if (PrimaryEntity.FactionOwnerID == SecondaryEntity.FactionOwnerID)
            return false;
        foreach (var (item, _) in itemsToTransfer)
        {
            if (item is SpeciesCargo)
                return true;
        }
        return false;
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
            TypeStore store;
            SafeList<(ICargoable item, long count, double mass)> itemsToRemove; //reference which list.
            long unitsStorable; 
            if (unitAmount < 0) //if we're removing items
            {
                unitAmount *= -1;
                store = PrimaryStorageDB.TypeStores[cargoItem.CargoTypeID];
                itemsToRemove = EscroHeldInPrimary;
                unitsStorable = CargoMath.GetFreeUnitSpace(SecondaryStorageDB, cargoItem);
            }
            else
            {
                store = SecondaryStorageDB.TypeStores[cargoItem.CargoTypeID];
                itemsToRemove = EscroHeldInSecondary;
                unitsStorable = CargoMath.GetFreeUnitSpace(PrimaryStorageDB, cargoItem);
            }
            if (store.CurrentStoreInUnits.ContainsKey(cargoItem.ID))
            {
                long amountInStore = store.CurrentStoreInUnits[cargoItem.ID];
                
                long amountToRemove = Math.Min(unitAmount, amountInStore);
                amountToRemove = Math.Min(unitsStorable, amountToRemove);
                store.CurrentStoreInUnits[cargoItem.ID] -= amountToRemove;
                double massToRemove = cargoItem.MassPerUnit * amountToRemove;
                itemsToRemove.Add((cargoItem,amountToRemove, massToRemove));
            }
            else
            {
                //in this case we're trying to remove items that don't exist. not sure how we should handle this yet.
                itemsToRemove.Add((cargoItem,0,0));
            }
            break;
        }
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