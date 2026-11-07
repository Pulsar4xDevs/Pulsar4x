using System;
using Pulsar4X.DataStructures;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Extensions;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Colonies
{
    /// <summary>
    /// The one place people are turned into passenger cargo and back. A colony's people are
    /// <see cref="ColonyInfoDB.Population"/>; a ship's passengers are a <see cref="SpeciesCargo"/>
    /// pile in its passenger store. Every move changes both sides or neither.
    ///
    /// Board: colony population down, passenger cargo up.
    /// Land:  passenger cargo down, colony population up, never past the colony's capacity.
    ///
    /// Refusals return 0 and change nothing: someone else's colony, no passenger capacity on the
    /// ship, a species that does not live at the colony, a full colony. Cross-faction colonists are
    /// refused here as well as by the market and the transfer order.
    /// </summary>
    public static class PassengerAdapter
    {
        public static bool CanBoard(Entity colony, Entity ship, SpeciesCargo passengers, long count,
            out long available, out string reason)
        {
            available = 0;
            reason = "";
            if (!SameOwner(colony, ship))
            {
                reason = "The colony is not yours.";
                return false;
            }
            if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info))
            {
                reason = "The colony has no people.";
                return false;
            }
            if (!ship.TryGetDataBlob<CargoStorageDB>(out var hold)
                || !hold.TypeStores.ContainsKey(SpeciesCargo.CargoType))
            {
                reason = "The ship has no passenger capacity.";
                return false;
            }
            if (count <= 0)
                return false;
            if (!info.Population.TryGetValue(passengers.SpeciesId, out var people) || people <= 0)
            {
                reason = "No people of that species here.";
                return false;
            }

            long room = hold.GetFreeUnitSpace(passengers);
            long boardable = Math.Min(count, Math.Min(people, room));
            if (boardable <= 0)
            {
                reason = "No room aboard.";
                return false;
            }
            available = boardable;
            return true;
        }

        public static bool CanLand(Entity colony, Entity ship, SpeciesCargo passengers, long count,
            out long available, out string reason)
        {
            available = 0;
            reason = "";
            if (!SameOwner(colony, ship))
            {
                reason = "The colony is not yours.";
                return false;
            }
            if (!colony.TryGetDataBlob<ColonyInfoDB>(out _))
            {
                reason = "The colony has no people.";
                return false;
            }
            if (!ship.TryGetDataBlob<CargoStorageDB>(out var hold)
                || !hold.TypeStores.ContainsKey(SpeciesCargo.CargoType))
            {
                reason = "The ship has no passenger capacity.";
                return false;
            }
            if (count <= 0)
                return false;
            long aboard = hold.GetUnitsStored(passengers, false);
            if (aboard <= 0)
            {
                reason = "Nobody aboard.";
                return false;
            }

            long landing = Math.Min(count, Math.Min(aboard, Room(colony, ship, passengers.SpeciesId)));
            if (landing <= 0)
            {
                reason = "The colony is full.";
                return false;
            }
            available = landing;
            return true;
        }

        /// <summary>
        /// People on the colony's books, plus everyone still in transit to it. Boarding takes this
        /// out of the colony in one step, so an arrival cannot land on a colony that is already full.
        /// </summary>
        public static long Room(Entity colony, Entity ship, int speciesId)
        {
            if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info))
                return 0;
            info.Population.TryGetValue(speciesId, out var people);
            long max = colony.TryGetDataBlob<ColonyLifeSupportDB>(out var support) ? support.MaxPopulation : 0;
            // A colony with no ceiling set cannot say it has room.
            if (max <= 0)
                return 0;
            long room = max - people - InTransit(colony, ship, speciesId);
            return room > 0 ? room : 0;
        }

        /// <summary>
        /// People are taken off a colony the moment they board, so escrowed passengers already
        /// left <see cref="ColonyInfoDB.Population"/>. This is only a backstop for another transfer
        /// that is unloading onto the same colony at the same time.
        /// </summary>
        static long InTransit(Entity colony, Entity ship, int speciesId)
        {
            if (!colony.TryGetDataBlob<CargoStorageDB>(out var store))
                return 0;
            long count = 0;
            foreach (var transfer in store.EscroItems)
            {
                if (transfer.PrimaryEntity == ship)
                    count += LandingEscrow(transfer.EscroHeldInPrimary, speciesId);
                if (transfer.SecondaryEntity == ship)
                    count += LandingEscrow(transfer.EscroHeldInSecondary, speciesId);
            }
            return count;
        }

        static long LandingEscrow(SafeList<(ICargoable item, long count, double mass)> held, int speciesId)
        {
            long count = 0;
            foreach (var (item, units, _) in held)
            {
                if (item is SpeciesCargo cargo && cargo.SpeciesId == speciesId)
                    count += units;
            }
            return count;
        }

        /// <summary>
        /// Take people off the colony and put them aboard. Returns how many actually boarded.
        /// </summary>
        public static long Board(Entity colony, Entity ship, SpeciesCargo passengers, long count)
        {
            if (!CanBoard(colony, ship, passengers, count, out var available, out _))
                return 0;

            var info = colony.GetDataBlob<ColonyInfoDB>();
            var hold = ship.GetDataBlob<CargoStorageDB>();
            long aboard = hold.AddCargoByUnit(passengers, available);
            if (aboard <= 0)
                return 0;

            info.Population[passengers.SpeciesId] -= aboard;
            NoteChanged(colony, info.Population[passengers.SpeciesId]);
            return aboard;
        }

        /// <summary>
        /// Take people out of the hold and add them to the colony, clamped to its room.
        /// Returns how many actually landed.
        /// </summary>
        public static long Land(Entity colony, Entity ship, SpeciesCargo passengers, long count)
        {
            if (!CanLand(colony, ship, passengers, count, out var available, out _))
                return 0;
            return LandAvailable(colony, ship, passengers, available);
        }

        /// <summary>
        /// <see cref="Land"/> for a count the caller already checked. The cargo transfer processor
        /// has already taken the people out of the hold by the time it lands them, so it must not
        /// look at the hold again.
        /// </summary>
        internal static long LandAvailable(Entity colony, Entity ship, SpeciesCargo passengers, long count)
        {
            if (count <= 0 || !SameOwner(colony, ship))
                return 0;
            if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info))
                return 0;
            if (!ship.TryGetDataBlob<CargoStorageDB>(out var hold)
                || !hold.TypeStores.ContainsKey(SpeciesCargo.CargoType))
                return 0;

            long landing = Math.Min(count, Room(colony, ship, passengers.SpeciesId));
            if (landing <= 0)
                return 0;

            if (info.Population.ContainsKey(passengers.SpeciesId))
                info.Population[passengers.SpeciesId] += landing;
            else
                info.Population.Add(passengers.SpeciesId, landing);
            NoteChanged(colony, info.Population[passengers.SpeciesId]);
            return landing;
        }

        /// <summary>
        /// Escrow took the people off the hold but not off the books yet, so a cancel puts them
        /// back in the hold and nothing else changes. Landing is the only path that adds people.
        /// </summary>
        static bool SameOwner(Entity colony, Entity ship)
        {
            return colony != null && ship != null && colony.FactionOwnerID == ship.FactionOwnerID;
        }

        static void NoteChanged(Entity colony, long population)
        {
            EventManager.Instance.Publish(
                Event.Create(
                    EventType.PopulationChanged,
                    colony.StarSysDateTime,
                    $"{colony.GetName(colony.FactionOwnerID)} population is now {population}",
                    colony.FactionOwnerID,
                    colony.Manager.ManagerID,
                    colony.Id));
        }
    }
}
