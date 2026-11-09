using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.Logistics;
using Pulsar4X.Orbits;
using Pulsar4X.People;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Colonies
{
    /// <summary>
    /// One species leaving one colony, with the fleet that will carry it.
    /// </summary>
    public sealed class MigrationMove
    {
        public Entity Fleet = null!;
        public Entity Origin = null!;
        public Entity Destination = null!;
        public int SpeciesId;
        public long Travelling;
    }

    /// <summary>
    /// One relocation a market should advertise: this many of this species leaving
    /// <see cref="Origin"/> and the same number accepted by <see cref="Destination"/>.
    /// </summary>
    public sealed class MigrationOffer
    {
        public Entity Origin = null!;
        public Entity Destination = null!;
        public int SpeciesId;
        /// <summary>People the origin posts as leaving.</summary>
        public long Leaving;
        /// <summary>People the destination will take, already reduced by the room it promised away.</summary>
        public long Accepted;
    }

    /// <summary>
    /// Automatic colonist migration between colonies that may trade: the same faction, or another
    /// faction with mutual <see cref="FactionStanceRules.CanTrade"/> grants. No offer, no request,
    /// no UI: every colony works out for itself who would leave and where they would go, and the
    /// ships in the same system with spare passenger berths move them.
    ///
    /// Willingness, the only thing this reads:
    ///   A species leaves a colony when its own support is short,
    ///   <see cref="PopulationProcessor"/>'s number, support divided by colony cost and by how many
    ///   species live there. A colony supports nobody of that species when its colony cost is
    ///   negative, <see cref="SpeciesDBExtensions.UNSURVIVABLE_COST"/>.
    ///   It attracts that species when it has room left, <see cref="PassengerAdapter.Room"/>,
    ///   which is the same support number capped by <see cref="ColonyLifeSupportDB.MaxPopulation"/>.
    ///   The most crowded colony is the origin; the roomiest colony in the same system is the
    ///   destination. Ties break by entity id, so a game that repeats gives the same answer.
    ///
    /// Availability, which is checked after willingness and never changes it:
    ///   a ship of the same faction, in the same system, with a passenger store and spare berths.
    ///   It is loaded with <see cref="GoalType.LoadGoods"/> and flown with
    ///   <see cref="GoalType.MoveGoods"/> to unload, both reused, so the people are taken off the
    ///   colony by the transfer escrow and put back by the destination's people on arrival.
    ///
    /// The planner only reads. It builds no actions and changes nobody.
    /// </summary>
    public static class MigrationPlanner
    {
        /// <summary>Room in a colony that counts as full. Smaller gaps do not move people.</summary>
        public const long MinimumRoom = 1;

        public static List<MigrationMove> Plan(EntityManager manager)
        {
            var moves = new List<MigrationMove>();
            var colonies = manager.GetAllEntitiesWithDataBlob<ColonyInfoDB>();
            if (colonies.Count < 2)
                return moves;

            var claimable = ClaimableShips(manager);
            var spokenFor = new List<Entity>();

            foreach (var origin in colonies.OrderBy(c => c.Id))
            {
                var outbound = SpeciesServed(origin);
                foreach (var (speciesId, crowded) in outbound.OrderBy(pair => pair.Key))
                {
                    if (crowded <= 0)
                        continue;

                    Entity? destination = null;
                    long bestRoom = MinimumRoom - 1;
                    foreach (var candidate in colonies)
                    {
                        if (candidate.Id == origin.Id || !CanRelocateBetween(origin, candidate))
                            continue;
                        if (candidate.Manager != origin.Manager)
                            continue;

                        long room = Room(candidate, speciesId);
                        if (room < MinimumRoom)
                            continue;
                        if (room > bestRoom || (room == bestRoom && destination != null && candidate.Id < destination.Id))
                        {
                            destination = candidate;
                            bestRoom = room;
                        }
                    }
                    if (destination == null)
                        continue;
                    if (!claimable.TryGetValue(origin.FactionOwnerID, out var ships))
                        continue;

                    // One fleet takes one load this pass.
                    Entity? fleet = null;
                    foreach (var candidate in ships)
                    {
                        if (candidate.Manager != origin.Manager || spokenFor.Contains(candidate))
                            continue;
                        if (FleetHasPassengerRoom(candidate))
                        {
                            fleet = candidate;
                            spokenFor.Add(candidate);
                            break;
                        }
                    }
                    if (fleet == null)
                        continue;

                    long travelling = Math.Min(bestRoom, Math.Min(crowded, Berths(fleet)));
                    if (travelling < MinimumRoom)
                        continue;

                    moves.Add(new MigrationMove
                    {
                        Fleet = fleet,
                        Origin = origin,
                        Destination = destination,
                        SpeciesId = speciesId,
                        Travelling = travelling,
                    });
                }
            }

            return moves;
        }

        /// <summary>
        /// The relocation offers a market should carry: for every overcrowded colony, how many of
        /// each species should be posted as leaving, and for the roomiest colony in the same system
        /// that can take them, how many it will accept. Same system and trade-eligible factions only, one
        /// species at a time, and the numbers already account for the room promised to earlier
        /// origins so two offers cannot both claim the last berth.
        ///
        /// Read-only, like <see cref="Plan"/>: it decides the numbers and changes nobody. The
        /// caller posts and retires the listings. No ship is required here, because the offers
        /// are meant for the ordinary freighter goals to pick up.
        /// </summary>
        public static List<MigrationOffer> Offers(EntityManager manager)
        {
            var offers = new List<MigrationOffer>();
            var colonies = manager.GetAllEntitiesWithDataBlob<ColonyInfoDB>();
            if (colonies.Count < 2)
                return offers;

            // Room already promised to another origin, per colony and species.
            var promised = new Dictionary<(int Colony, int Species), long>();

            foreach (var origin in colonies.OrderBy(c => c.Id))
            {
                foreach (var (speciesId, crowded) in SpeciesServed(origin).OrderBy(pair => pair.Key))
                {
                    if (crowded < MinimumRoom)
                        continue;
                    if (!origin.TryGetDataBlob<LogiBaseDB>(out _))
                        continue;

                    Entity? destination = null;
                    long bestRoom = MinimumRoom - 1;
                    foreach (var candidate in colonies.OrderBy(c => c.Id))
                    {
                        if (candidate.Id == origin.Id)
                            continue;
                        if (!CanRelocateBetween(origin, candidate))
                            continue;
                        if (candidate.Manager != origin.Manager)
                            continue;
                        if (!candidate.HasDataBlob<LogiBaseDB>())
                            continue;

                        long taken = promised.TryGetValue((candidate.Id, speciesId), out var used) ? used : 0;
                        long room = Math.Max(0, Room(candidate, speciesId) - taken);
                        if (room < MinimumRoom)
                            continue;
                        if (room > bestRoom || (room == bestRoom && destination != null && candidate.Id < destination.Id))
                        {
                            destination = candidate;
                            bestRoom = room;
                        }
                    }
                    if (destination == null)
                        continue;

                    long leaving = Math.Min(crowded, bestRoom);
                    if (leaving < MinimumRoom)
                        continue;

                    var key = (destination.Id, speciesId);
                    promised[key] = (promised.TryGetValue(key, out var already) ? already : 0) + leaving;
                    offers.Add(new MigrationOffer
                    {
                        Origin = origin,
                        Destination = destination,
                        SpeciesId = speciesId,
                        Leaving = leaving,
                        Accepted = leaving,
                    });
                }
            }

            return offers;
        }

        /// <summary>
        /// Support for this species at this colony, the same shape <see cref="PopulationProcessor"/>
        /// uses to decide who dies. Zero means the colony supports none of that species and anyone
        /// there wants to leave.
        /// </summary>
        public static long Support(Entity colony, Entity species)
        {
            if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info)
                || !colony.TryGetDataBlob<ComponentInstancesDB>(out var instances))
                return 0;
            if (!species.TryGetDataBlob<SpeciesDB>(out var speciesDb))
                return 0;
            if (info.PlanetEntity == null)
                return 0;

            double colonyCost = ColonyCostOrComfortable(speciesDb, info.PlanetEntity);
            // Unsurvivable. Nobody of this species can stay, and nobody can arrive.
            if (colonyCost < SpeciesDBExtensions.NO_COST)
                return 0;
            // A comfortable world (cost 0) is rated at its infrastructure, the same way a cost of 1 is.
            // Population growth on a cost of 0 is uncapped. Migration will not pack past the rating.
            if (colonyCost <= SpeciesDBExtensions.NO_COST)
                colonyCost = 1;

            long supportValue = instances.GetPopulationSupportValue(info.PlanetEntity);
            int sharing = SharingSpecies(colony, info, species, colonyCost);
            if (sharing == 0)
                return 0;

            double supported = (double)supportValue / sharing / colonyCost;
            if (supported <= 0)
                return 0;
            return (long)supported;
        }

        /// <summary>
        /// Colony cost, or <see cref="SpeciesDBExtensions.NO_COST"/> when the body is missing the
        /// blobs that cost needs. A test colony and an unfinished body are treated as comfortable
        /// rather than throwing out of the market pass.
        /// </summary>
        static double ColonyCostOrComfortable(SpeciesDB species, Entity planet)
        {
            if (planet == null || !planet.HasDataBlob<SystemBodyInfoDB>() || !planet.HasDataBlob<OrbitDB>())
                return SpeciesDBExtensions.NO_COST;
            return species.ColonyCost(planet);
        }

        /// <summary>
        /// Species who share the infrastructure rating, including a comfortable species (cost 0).
        /// The species being asked about counts even when it does not live here yet, so an empty
        /// colony still has room for the people who are coming.
        /// </summary>
        static int SharingSpecies(Entity colony, ColonyInfoDB info, Entity asked, double askedCost)
        {
            int sharing = 0;
            bool countedAsked = false;
            foreach (var (otherId, count) in info.Population)
            {
                if (count <= 0)
                    continue;
                if (!colony.Manager.TryGetGlobalEntityById(otherId, out var other))
                    continue;
                if (!other.TryGetDataBlob<SpeciesDB>(out var otherDb))
                    continue;
                double cost = ColonyCostOrComfortable(otherDb, info.PlanetEntity);
                if (cost < SpeciesDBExtensions.NO_COST)
                    continue;
                sharing++;
                if (otherId == asked.Id)
                    countedAsked = true;
            }
            if (!countedAsked && askedCost >= SpeciesDBExtensions.NO_COST)
                sharing++;
            return sharing;
        }

        /// <summary>
        /// People of one species who do not fit their colony's support. Zero when they do.
        /// </summary>
        public static long Crowded(Entity colony, int speciesId)
        {
            if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info))
                return 0;
            if (!info.Population.TryGetValue(speciesId, out var people))
                return 0;
            if (!colony.Manager.TryGetGlobalEntityById(speciesId, out var species))
                return 0;

            long supported = Support(colony, species);
            if (supported <= 0)
                return people;
            long excess = people - supported;
            return excess > 0 ? excess : 0;
        }

        /// <summary>
        /// Room for one species at a colony: its capacity with the people it already has and the
        /// people on their way to it taken out. The same number <see cref="PassengerAdapter"/> uses.
        /// </summary>

        /// <summary>Same faction, or mutual trade grants between the two colony owners.</summary>
        static bool CanRelocateBetween(Entity origin, Entity candidate)
        {
            if (origin == null || candidate == null)
                return false;
            if (origin.FactionOwnerID == candidate.FactionOwnerID)
                return true;
            var game = origin.Manager?.Game ?? candidate.Manager?.Game;
            return game != null && FactionStanceRules.CanTrade(game, origin.FactionOwnerID, candidate.FactionOwnerID);
        }

        public static long Room(Entity colony, int speciesId)
        {
            if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info))
                return 0;
            if (!colony.Manager.TryGetGlobalEntityById(speciesId, out var species))
                return 0;

            info.Population.TryGetValue(speciesId, out var people);
            long capacity = Capacity(colony, species);
            // Nobody moves into a colony that supports none of that species.
            if (capacity <= 0)
                return 0;
            long room = capacity - people;
            return room > 0 ? room : 0;
        }

        /// <summary>
        /// How many of this species a colony can hold: its support, capped by the life support
        /// ceiling when a colony has one and no other species is counted against it.
        /// </summary>
        public static long Capacity(Entity colony, Entity species)
        {
            long supported = Support(colony, species);
            if (colony.TryGetDataBlob<ColonyLifeSupportDB>(out var lifeSupport)
                && lifeSupport.MaxPopulation > 0
                && lifeSupport.MaxPopulation < supported)
                return lifeSupport.MaxPopulation;
            return supported;
        }

        /// <summary>Spare berths in the passenger store of a fleet's ships.</summary>
        public static long Berths(Entity fleet)
        {
            long berths = 0;
            foreach (var ship in Ships(fleet))
            {
                if (!ship.TryGetDataBlob<CargoStorageDB>(out var hold))
                    continue;
                if (!hold.TypeStores.TryGetValue(SpeciesCargo.CargoType, out var store))
                    continue;
                double volume = store.FreeVolume;
                if (volume <= 0)
                    continue;
                berths += (long)(volume / SpeciesCargo.VolumePerPerson);
            }
            return berths;
        }

        public static bool FleetHasPassengerRoom(Entity fleet)
        {
            return Berths(fleet) >= MinimumRoom;
        }

        static IEnumerable<Entity> Ships(Entity fleet)
        {
            if (fleet.TryGetDataBlob<FleetDB>(out var fleetDb))
                return fleetDb.Children.Where(child => child.HasDataBlob<ShipInfoDB>());
            return fleet.HasDataBlob<ShipInfoDB>() ? new[] { fleet } : Array.Empty<Entity>();
        }

        /// <summary>People of each species who do not fit their colony.</summary>
        static Dictionary<int, long> SpeciesServed(Entity colony)
        {
            var crowded = new Dictionary<int, long>();
            if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info))
                return crowded;
            foreach (var (speciesId, _) in info.Population)
                crowded[speciesId] = Crowded(colony, speciesId);
            return crowded;
        }

        /// <summary>
        /// Ships of each faction that are a fleet, or a lone ship with a captain, and have a
        /// passenger store. A fleet has no captain of its own, so its ships carry the berths.
        /// </summary>
        static Dictionary<int, List<Entity>> ClaimableShips(EntityManager manager)
        {
            var byFaction = new Dictionary<int, List<Entity>>();
            foreach (var fleet in manager.GetAllEntitiesWithDataBlob<FleetDB>())
            {
                Add(byFaction, fleet);
            }
            foreach (var ship in manager.GetAllEntitiesWithDataBlob<ShipInfoDB>())
            {
                if (!ship.TryGetDataBlob<ShipInfoDB>(out var info) || info.CommanderID < 0)
                    continue;
                if (ship.HasDataBlob<FleetDB>())
                    continue;
                if (!ship.TryGetDataBlob<CargoStorageDB>(out var hold)
                    || !hold.TypeStores.ContainsKey(SpeciesCargo.CargoType))
                    continue;
                Add(byFaction, ship);
            }
            return byFaction;
        }

        static void Add(Dictionary<int, List<Entity>> byFaction, Entity fleet)
        {
            if (!byFaction.TryGetValue(fleet.FactionOwnerID, out var list))
            {
                list = new List<Entity>();
                byFaction.Add(fleet.FactionOwnerID, list);
            }
            if (!list.Contains(fleet))
                list.Add(fleet);
        }
    }
}
