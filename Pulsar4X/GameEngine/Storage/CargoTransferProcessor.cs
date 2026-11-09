using System;
using System.Collections.Generic;
using Pulsar4X.Colonies;
using Pulsar4X.DataStructures;
using Pulsar4X.Datablobs;
using Pulsar4X.Orbital;
using Pulsar4X.Extensions;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;

namespace Pulsar4X.Storage
{
    public class CargoTransferProcessor : IHotloopProcessor
    {
        public static CargoDefinitionsLibrary CargoDefs;

        public TimeSpan RunFrequency
        {
            get { return TimeSpan.FromMinutes(1); }
        }

        public TimeSpan FirstRunOffset => TimeSpan.FromHours(0);

        public Type GetParameterType => typeof(CargoTransferDB);

        public void Init(Game game)
        {
            //unneeded
        }
        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            List<CargoTransferDB> dblist = manager.GetAllDataBlobsOfType<CargoTransferDB>();
            foreach(var db in dblist)
            {
                ProcessEntity(db, deltaSeconds);
            }
            return dblist.Count;
        }
        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            ProcessEntity(entity.GetDataBlob<CargoTransferDB>(), deltaSeconds);
        }


        public void ProcessEntity(CargoTransferDB transferDB, int deltaSeconds)
        {
            var transferData = transferDB.TransferData;
            var transferRange = transferDB.ParentStorageDB.TransferRangeDv_mps;
            var transferRate = transferDB.ParentStorageDB.TransferRate;
            double dv_mps = CalcDVDifference_m(transferData.PrimaryEntity, transferData.SecondaryEntity);


            double massTransferable = transferRate * deltaSeconds;
            if(dv_mps > transferRange || massTransferable <=0)
                return;//early out if we're out of range or no more mass to move.

            massTransferable -= MoveFromEscro(transferData.EscroHeldInPrimary, transferData.SecondaryStorageDB, transferData.PrimaryStorageDB, massTransferable);
            massTransferable -= MoveFromEscro(transferData.EscroHeldInSecondary, transferData.PrimaryStorageDB, transferData.SecondaryStorageDB, massTransferable);

            UpdateMassFuelAndDeltaV(transferData.PrimaryEntity);
            UpdateMassFuelAndDeltaV(transferData.SecondaryEntity);

        }

        private double MoveFromEscro(SafeList<(ICargoable item, long count, double mass)> escroList, CargoStorageDB moveTo, CargoStorageDB moveFrom, double massTransferable)
        {
            double totalMassXfered = 0;
            for (int index = 0; index < escroList.Count; index++)
            {
                (ICargoable cargoItem, long count, double mass) tuple = escroList[index];
                var cargoItem = tuple.cargoItem;
                if (cargoItem == null || tuple.count <= 0 || cargoItem.MassPerUnit <= 0)
                    continue;

                double itemMassPerUnit = cargoItem.MassPerUnit;
                double massToXfer = Math.Min(massTransferable, tuple.mass);
                // People land on a colony's books. The colony has no passenger store, so its free
                // mass is not the limit. Warehouse goods still stop at the destination's free mass.
                bool landing = IsPassengerLanding(cargoItem, moveTo);
                bool fromBooks = IsPassengerBooks(cargoItem, moveFrom);
                if (!landing)
                    massToXfer = Math.Min(massToXfer, CargoMath.GetFreeMass(moveTo, cargoItem));

                // Whole units only. Ceiling left one unit in escrow forever when a float remainder
                // was just under one item, and that leftover was then subtracted from free space again.
                long affordable = (long)Math.Floor((massToXfer + 1e-6) / itemMassPerUnit);
                long countToXfer = Math.Min(tuple.count, Math.Max(0, affordable));
                long itemsLeft = tuple.count - countToXfer;
                double massLeft = itemsLeft == 0 ? 0 : tuple.mass - countToXfer * itemMassPerUnit;
                if (massLeft < 0)
                    massLeft = 0;
                escroList[index] = (cargoItem, itemsLeft, massLeft);

                long moved;
                if (countToXfer <= 0)
                {
                    moved = 0;
                }
                else if (landing)
                {
                    var colony = moveTo.OwningEntity!;
                    var ship = moveFrom.OwningEntity!;
                    long landed = PassengerAdapter.LandAvailable(colony, ship, (SpeciesCargo)cargoItem, countToXfer);
                    long staying = countToXfer - landed;
                    // Volume was never freed. Put the people the colony cannot take back in the hold.
                    if (staying > 0)
                        PutUnitsBack(moveFrom, cargoItem, staying);
                    moved = landed;
                }
                else
                {
                    long added = moveTo.AddCargoByUnit(cargoItem, countToXfer);
                    long staying = countToXfer - added;
                    if (staying > 0)
                    {
                        if (fromBooks && cargoItem is SpeciesCargo passengers)
                            ReturnPeople(moveFrom.OwningEntity, passengers, staying);
                        else
                            PutUnitsBack(moveFrom, cargoItem, staying);
                    }
                    moved = added;
                }

                // Escrow only took the units. Release the reserved volume once, and take the mass
                // off the source once, for the units that actually left. A colony's books are not
                // a store: boarding must not change its warehouse mass or volume.
                if (!fromBooks && moved > 0 && moveFrom.TypeStores.TryGetValue(cargoItem.CargoTypeID, out var store))
                {
                    store.FreeVolume += moved * cargoItem.VolumePerUnit;
                    moveFrom.TotalStoredMass -= moved * cargoItem.MassPerUnit;
                    if (moveFrom.TotalStoredMass < 0 && moveFrom.TotalStoredMass > -1e-6)
                        moveFrom.TotalStoredMass = 0;
                }

                double massAccounted = countToXfer * itemMassPerUnit;
                if (massAccounted > massTransferable)
                    massAccounted = massTransferable;
                massTransferable -= massAccounted;
                totalMassXfered += massAccounted;
            }
            return totalMassXfered;
        }

        static bool IsPassengerLanding(ICargoable cargo, CargoStorageDB dest)
        {
            return cargo is SpeciesCargo
                && dest?.OwningEntity is { IsValid: true } colony
                && colony.HasDataBlob<ColonyInfoDB>();
        }

        static bool IsPassengerBooks(ICargoable cargo, CargoStorageDB source)
        {
            return cargo is SpeciesCargo
                && source?.OwningEntity is { IsValid: true } colony
                && colony.HasDataBlob<ColonyInfoDB>();
        }

        static void PutUnitsBack(CargoStorageDB store, ICargoable item, long count)
        {
            if (count <= 0 || store == null || item == null)
                return;
            if (!store.TypeStores.TryGetValue(item.CargoTypeID, out var type))
                return;
            if (type.CurrentStoreInUnits.ContainsKey(item.ID))
                type.CurrentStoreInUnits[item.ID] += count;
            else
                type.CurrentStoreInUnits.Add(item.ID, count);
            if (!type.Cargoables.ContainsKey(item.ID))
                type.Cargoables.Add(item.ID, item);
        }

        static void ReturnPeople(Entity colony, SpeciesCargo passengers, long count)
        {
            if (count <= 0 || colony == null || !colony.TryGetDataBlob<ColonyInfoDB>(out var info))
                return;
            if (info.Population.ContainsKey(passengers.SpeciesId))
                info.Population[passengers.SpeciesId] += count;
            else
                info.Population.Add(passengers.SpeciesId, count);
        }

        /// <summary>
        /// Add cargo and updates the entites MassTotal
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="item"></param>
        /// <param name="amountInMass"></param>
        internal static double AddCargoItems(Entity entity, ICargoable item, int amount)
        {
            CargoStorageDB cargo = entity.GetDataBlob<CargoStorageDB>();
            double amountSuccess = cargo.AddCargoByUnit(item, amount);
            UpdateMassFuelAndDeltaV(entity);
            return amountSuccess;
        }

        /// <summary>
        /// Starting cargo of a component design is one instance per count.
        /// A stored design object is not a part the yard can mount.
        /// </summary>
        internal static void AddStartingCount(Entity entity, ICargoable item, int amount)
        {
            if (item is Pulsar4X.Components.ComponentDesign design)
            {
                for (int i = 0; i < amount; i++)
                    AddCargoItems(entity, new Pulsar4X.Components.ComponentInstance(design), 1);
                return;
            }

            AddCargoItems(entity, item, amount);
        }

        /// <summary>
        /// Removes cargo and updates the entites MassTotal
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="item"></param>
        /// <param name="amountInMass"></param>
        internal static double RemoveCargoItems(Entity entity, ICargoable item, int amount)
        {
            CargoStorageDB cargo = entity.GetDataBlob<CargoStorageDB>();
            double amountSuccess = cargo.RemoveCargoByUnit(item, amount);
            UpdateMassFuelAndDeltaV(entity);
            return amountSuccess;
        }

        /// <summary>
        /// Add or Removes cargo and updates the entites MassTotal
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="item"></param>
        /// <param name="amountInMass"></param>
        internal static double AddRemoveCargoMass(Entity entity, ICargoable item, double amountInMass)
        {
            CargoStorageDB cargo = entity.GetDataBlob<CargoStorageDB>();
            double amountSuccess = cargo.AddRemoveCargoByMass(item, amountInMass);
            UpdateMassFuelAndDeltaV(entity);
            return amountSuccess;
        }
        /// <summary>
        /// Add or Removes cargo and updates the entites MassTotal
        /// </summary>
        /// <param name="storeDB"></param>
        /// <param name="item"></param>
        /// <param name="amountInMass"></param>
        internal static double AddRemoveCargoMass(CargoStorageDB storeDB, ICargoable item, double amountInMass)
        {
            double amountSuccess = storeDB.AddRemoveCargoByMass(item, amountInMass);
            UpdateMassFuelAndDeltaV(storeDB.OwningEntity);
            return amountSuccess;
        }



        /// <summary>
        /// Add or Removes cargo and updates the entites MassTotal
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="item"></param>
        /// <param name="amountInVolume"></param>
        internal static double AddRemoveCargoVolume(Entity entity, ICargoable item, double amountInVolume)
        {
            CargoStorageDB cargo = entity.GetDataBlob<CargoStorageDB>();
            double amountSuccess = cargo.AddRemoveCargoByVolume(item, amountInVolume);
            UpdateMassFuelAndDeltaV(entity);
            return amountSuccess;
        }

        internal static void UpdateMassFuelAndDeltaV(Entity entity)
        {
            if(!entity.TryGetDataBlob(out NewtonThrustAbilityDB newtdb))
                return;
            if (!entity.TryGetDataBlob(out MassVolumeDB massdb))
                return;
            if(!entity.TryGetDataBlob(out CargoStorageDB storedb))
                return;

            massdb.UpdateMassTotal();
            var cargoLib = entity.GetFactionCargoDefinitions();
            var fuelTypeID = newtdb.FuelType;
            var fuelType = cargoLib.GetAny(fuelTypeID);
            var fuelMass = storedb.GetMassStored(fuelType, false);
            newtdb.SetFuel(fuelMass, massdb.MassTotal);
        }

        /// <summary>
        /// Calculates a simplified difference in DeltaV between two enties who have the same parent
        /// for the purposes of calculating cargo transfer rate
        /// </summary>
        /// <param name="entity1"></param>
        /// <param name="entity2"></param>
        /// <returns></returns>
        public static double CalcDVDifference_m(Entity entity1, Entity entity2)
        {
            double dvDif = 0;

            Entity parent;
            double parentMass;
            double sgp;
            double r1;
            double r2;

            Entity? soi1 = entity1.GetSOIParentEntity();
            Entity? soi2 = entity2.GetSOIParentEntity();


            if(soi1 is not null && soi2 is not null && soi1 == soi2)
            {
                parent = soi1;
                parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
                sgp = GeneralMath.StandardGravitationalParameter(parentMass);

                (Vector3 pos, Vector3 Velocity) state1 = MoveMath.GetRelativeState(entity1);
                (Vector3 pos, Vector3 Velocity) state2 = MoveMath.GetRelativeState(entity2);
                r1 = state1.pos.Length();
                r2 = state2.pos.Length();
            }
            else
            {
                //StaticRefLib.EventLog.AddEvent(new Event("Cargo calc failed, entities must have same soi parent"));
                return double.PositiveInfinity;
            }

            var hohmann = OrbitalMath.Hohmann(sgp, r1, r2);
            dvDif = hohmann[0].deltaV.Length() + hohmann[1].deltaV.Length();
            return dvDif;

        }



        /// <summary>
        /// Calculates a simplified difference in DeltaV between two enties who have the same parent
        /// for the purposes of calculating cargo transfer rate
        /// </summary>
        /// <param name="sgp"></param>
        /// <param name="state1"></param>
        /// <param name="state2"></param>
        /// <returns></returns>
        public static double CalcDVDifference_m(double sgp, (Vector3 pos, Vector3 Velocity) state1, (Vector3 pos, Vector3 Velocity) state2)
        {
            var r1 = state1.pos.Length();
            var r2 = state2.pos.Length();
            var hohmann = OrbitalMath.Hohmann(sgp, r1, r2);
            return hohmann[0].deltaV.Length() + hohmann[1].deltaV.Length();
        }


        /// <summary>
        /// Calculates the transfer rate.
        /// </summary>
        /// <returns>The transfer rate.</returns>
        /// <param name="dvDifference_mps">Dv difference in m/s</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        public static int CalcTransferRate(double dvDifference_mps, CargoStorageDB from, CargoStorageDB to)
        {
            //var from = transferDB.CargoFromDB;
            //var to = transferDB.CargoToDB;
            var fromDVRange = from.TransferRangeDv_mps;
            var toDVRange = to.TransferRangeDv_mps;

            double maxRange;
            double maxXferAtMaxRange;
            double bestXferRange_ms = Math.Min(fromDVRange, toDVRange);
            double maxXferAtBestRange = from.TransferRate + to.TransferRate;

            double transferRate;

            if (from.TransferRangeDv_mps > to.TransferRangeDv_mps)
            {
                maxRange = fromDVRange;
                if (from.TransferRate > to.TransferRate)
                    maxXferAtMaxRange = from.TransferRate;
                else
                    maxXferAtMaxRange = to.TransferRate;
            }
            else
            {
                maxRange = toDVRange;
                if (to.TransferRate > from.TransferRate)
                    maxXferAtMaxRange = to.TransferRate;
                else
                    maxXferAtMaxRange = from.TransferRate;
            }

            if (dvDifference_mps < bestXferRange_ms)
                transferRate = (int)maxXferAtBestRange;
            else if (dvDifference_mps < maxRange)
                transferRate = (int)maxXferAtMaxRange;
            else
                transferRate = 0;
            return (int)transferRate;
        }


        public static (double bestDVRange, double bestRate) GetBestRangeRate(Entity from, Entity to)
        {
            var fromdb = from.GetDataBlob<CargoStorageDB>();
            var todb = to.GetDataBlob<CargoStorageDB>();
            var fromDVRange = fromdb.TransferRangeDv_mps;
            var toDVRange = todb.TransferRangeDv_mps;
            double bestXferRange_ms = Math.Min(fromDVRange, toDVRange);
            double maxXferAtBestRange = fromdb.TransferRate + todb.TransferRate;
            return (maxXferAtBestRange, bestXferRange_ms);
        }

        public static (double maxDVRange, double lowRate) GetMaxRangeRate(Entity from, Entity to)
        {
            var fromdb = from.GetDataBlob<CargoStorageDB>();
            var todb = to.GetDataBlob<CargoStorageDB>();
            var fromDVRange = fromdb.TransferRangeDv_mps;
            var toDVRange = todb.TransferRangeDv_mps;
            double maxRange;
            double maxXferAtMaxRange;

            if (fromdb.TransferRangeDv_mps > todb.TransferRangeDv_mps)
            {
                maxRange = fromDVRange;
                if (fromdb.TransferRate > todb.TransferRate)
                    maxXferAtMaxRange = fromdb.TransferRate;
                else
                    maxXferAtMaxRange = todb.TransferRate;
            }
            else
            {
                maxRange = toDVRange;
                if (todb.TransferRate > fromdb.TransferRate)
                    maxXferAtMaxRange = todb.TransferRate;
                else
                    maxXferAtMaxRange = fromdb.TransferRate;
            }

            return(maxRange, maxXferAtMaxRange);
        }
    }
}
