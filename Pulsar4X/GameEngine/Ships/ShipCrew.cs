using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.People;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Extensions;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Industry;
using Pulsar4X.JumpPoints;
using Pulsar4X.Movement;
using Pulsar4X.People;
using Pulsar4X.Storage;

namespace Pulsar4X.Ships
{
    /// <summary>
    /// Crew capacity, crew aboard and what a short crew does (crew plan phases 1-3).
    /// <para>Berths come from parts with a CrewQuartersAtb: crew quarters, and a few on every bridge.</para>
    /// <para>
    /// Crew aboard (ShipCrewDB) fill the engines first, then the bridge, then the work parts. An engine
    /// (warp drive or reaction engine) below its own crew runs at aboard / engine crew. The work parts
    /// (geo and grav surveyors, asteroid miners, shuttlebays, construction bays) share what is left:
    /// F = clamp((aboard - engine crew - bridge crew) / work crew, 0, 1), and they run at F².
    /// Nothing else degrades: lasers, fire control, sensors, reactors and the bridge run at full.
    /// TODO(crew): weapon crew waits for the damage rework.
    /// </para>
    /// </summary>
    public static class ShipCrew
    {
        /// <summary>Crew of a part list split by what the crew does. Capacity is the most that can be aboard.</summary>
        public readonly record struct CrewSplit(int Required, int Berths, int Engine, int Bridge, int Work)
        {
            public int Capacity => Math.Min(Required, Berths);
        }

        // ---- phase 1: berths ----

        /// <summary>Berths one copy of the part gives; 0 for parts without quarters.</summary>
        public static int BerthsPer(ComponentDesign design)
            => design != null && design.TryGetAttribute<CrewQuartersAtb>(out var q) ? q.Berths : 0;

        /// <summary>Berths the parts give: crew quarters plus bridge berths.</summary>
        public static int Berths(IEnumerable<(ComponentDesign design, int count)> components)
            => components?.Sum(c => BerthsPer(c.design) * c.count) ?? 0;

        /// <summary>Crew the parts need: Σ part crew × count, as ShipDesign.CrewReq.</summary>
        public static int CrewRequired(IEnumerable<(ComponentDesign design, int count)> components)
            => components?.Sum(c => (c.design?.CrewReq ?? 0) * c.count) ?? 0;

        /// <summary>Designer warnings about crew: fewer berths than the parts need.</summary>
        public static List<string> DesignCrewWarnings(IEnumerable<(ComponentDesign design, int count)> components)
        {
            var warnings = new List<string>();
            if (components == null) return warnings;
            var list = components.ToList();
            int crew = CrewRequired(list), berths = Berths(list);
            if (berths < crew)
                warnings.Add($"Crew {crew} but only {berths} berths: add crew quarters for {crew - berths} more.");
            return warnings;
        }

        // ---- phase 3: who does what ----

        public static bool IsEngine(ComponentDesign d)
            => d.HasAttribute<WarpDriveAtb>() || d.HasAttribute<NewtonionThrustAtb>();

        public static bool IsBridge(ComponentDesign d) => d.HasAttribute<AdminSpaceAtb>();

        /// <summary>Parts whose rate scales by F²: surveyors, miners, shuttlebays (rate, not range), construction bays.</summary>
        public static bool IsWorkPart(ComponentDesign d)
            => d.HasAttribute<GeoSurveyAtb>() || d.HasAttribute<GravSurveyAtb>() || d.HasAttribute<AsteroidMinerAtb>()
               || d.HasAttribute<CargoTransferAtb>() || d.HasAttribute<ConstructionAtb>();

        public static CrewSplit Split(IEnumerable<(ComponentDesign design, int count)> components)
        {
            int required = 0, berths = 0, engine = 0, bridge = 0, work = 0;
            foreach (var (design, count) in components ?? Enumerable.Empty<(ComponentDesign, int)>())
            {
                if (design == null || count <= 0)
                    continue;
                int crew = design.CrewReq * count;
                required += crew;
                berths += BerthsPer(design) * count;
                if (IsEngine(design))
                    engine += crew;
                else if (IsBridge(design))
                    bridge += crew;
                else if (IsWorkPart(design))
                    work += crew;
            }
            return new CrewSplit(required, berths, engine, bridge, work);
        }

        /// <summary>The parts mounted on a ship now, by design. A refit can leave these off the design.</summary>
        public static List<(ComponentDesign design, int count)> Mounted(Entity ship)
        {
            if (!ship.TryGetDataBlob<ComponentInstancesDB>(out var instances))
                return new List<(ComponentDesign, int)>();
            return instances.AllComponents.Values
                .Where(i => i.Design != null)
                .GroupBy(i => i.Design.UniqueID)
                .Select(g => (g.First().Design, g.Count()))
                .ToList();
        }

        public static CrewSplit SplitOf(Entity ship)
        {
            int parts = ship.TryGetDataBlob<ComponentInstancesDB>(out var instances) ? instances.AllComponents.Count : 0;
            if (ship.TryGetDataBlob<ShipCrewDB>(out var db))
            {
                if (db.CachedParts != parts)
                {
                    db.CachedSplit = Split(Mounted(ship));
                    db.CachedParts = parts;
                }
                return db.CachedSplit;
            }
            return Split(Mounted(ship));
        }

        /// <summary>True when the ship carries crew (every ship made by ShipFactory).</summary>
        public static bool Tracked(Entity ship) => ship.HasDataBlob<ShipCrewDB>();

        public static int Aboard(Entity ship)
            => ship.TryGetDataBlob<ShipCrewDB>(out var db) ? db.Aboard : SplitOf(ship).Capacity;

        /// <summary>Share of the engines' own crew filled, 0..1 (crew fill the engines first). 1 without crew tracking.</summary>
        public static double EngineFactor(Entity ship)
        {
            if (ship == null || !ship.TryGetDataBlob<ShipCrewDB>(out var db))
                return 1;
            var split = SplitOf(ship);
            return EngineFactor(split, db.Aboard);
        }

        public static double EngineFactor(CrewSplit split, int aboard)
            => split.Engine <= 0 ? 1 : Math.Clamp(aboard / (double)split.Engine, 0, 1);

        /// <summary>F for the work parts: the crew left after the engines and then the bridge, over the work parts' crew, 0..1.</summary>
        public static double WorkFactor(CrewSplit split, int aboard)
            => split.Work <= 0 ? 1 : Math.Clamp((aboard - split.Engine - split.Bridge) / (double)split.Work, 0, 1);

        public static double WorkFactor(Entity ship)
        {
            if (ship == null || !ship.TryGetDataBlob<ShipCrewDB>(out var db))
                return 1;
            return WorkFactor(SplitOf(ship), db.Aboard);
        }

        /// <summary>Rate multiplier for the work parts, F². 1 for colonies and ships without crew tracking.</summary>
        public static double WorkRate(Entity entity)
        {
            double f = WorkFactor(entity);
            return f * f;
        }

        /// <summary>A whole-point survey rate scaled by F², keeping the fraction for the next pass.</summary>
        internal static uint ScaleSurvey(Entity ship, uint rate, bool grav)
        {
            if (!ship.TryGetDataBlob<ShipCrewDB>(out var db))
                return rate;
            double rateF2 = WorkRate(ship);
            if (rateF2 >= 1)
                return rate;
            double carry = grav ? db.GravSurveyCarry : db.GeoSurveyCarry;
            double exact = rate * rateF2 + carry;
            uint whole = (uint)Math.Floor(exact + 1e-9);
            carry = exact - whole;
            if (grav) db.GravSurveyCarry = carry; else db.GeoSurveyCarry = carry;
            return whole;
        }

        /// <summary>One line on what a crew shortfall limits; empty when fully crewed.</summary>
        public static string Note(Entity ship)
        {
            if (!ship.TryGetDataBlob<ShipCrewDB>(out var db))
                return "";
            var split = SplitOf(ship);
            if (db.Aboard >= split.Required)
                return "";
            var parts = new List<string>();
            double engine = EngineFactor(split, db.Aboard);
            if (split.Engine > 0 && engine < 1)
                parts.Add($"engines {engine * 100:0} %");
            double work = WorkFactor(split, db.Aboard);
            if (split.Work > 0 && work < 1)
                parts.Add($"work parts {work * work * 100:0} %");
            string head = $"{db.Aboard}/{split.Required} crew";
            if (split.Berths < split.Required)
                head += $" ({split.Berths} berths)";
            return parts.Count == 0 ? head : head + ": " + string.Join(", ", parts);
        }

        // ---- phase 3: moving crew ----

        /// <summary>
        /// Players' crew come from the colony's trained pool. A non-player faction (any faction other than
        /// the local human one, when one is recorded) crews straight from the colony's population, with no
        /// training. TODO(crew): an AI crew course, so NPCs train crew like the player.
        /// </summary>
        public static bool CrewsFromPopulation(Entity colony)
        {
            var game = colony.Manager?.Game;
            return game != null && game.LocalHumanFactionId >= 0 && colony.FactionOwnerID != game.LocalHumanFactionId;
        }

        /// <summary>Takes up to <paramref name="count"/> crew from the colony: its pool, or its population for an NPC.</summary>
        public static int Draw(Entity colony, int count)
        {
            if (count <= 0)
                return 0;
            if (CrewsFromPopulation(colony))
            {
                if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info))
                    return 0;
                int have = (int)Math.Min(count, NavalAcademyCourse.PopulationOf(info));
                if (have > 0)
                    NavalAcademyCourse.TakePeople(info, have);
                return have;
            }
            if (!colony.TryGetDataBlob<ColonyCrewPoolDB>(out var pool))
                return 0;
            return (int)pool.Take(count, out _);
        }

        /// <summary>What a draw of <paramref name="count"/> would get now, without taking anyone.</summary>
        public static int Available(Entity colony, int count)
        {
            if (count <= 0)
                return 0;
            if (CrewsFromPopulation(colony))
                return colony.TryGetDataBlob<ColonyInfoDB>(out var info) ? (int)Math.Min(count, NavalAcademyCourse.PopulationOf(info)) : 0;
            return colony.TryGetDataBlob<ColonyCrewPoolDB>(out var pool) ? (int)Math.Min(count, pool.Total) : 0;
        }

        /// <summary>
        /// Puts crew into the colony's pool. Crew aboard is not tracked by species, so they join the
        /// pool's largest species, or the colony's largest population species when the pool is empty.
        /// TODO(crew): return the species that boarded.
        /// </summary>
        public static void Return(Entity colony, int count)
        {
            if (count <= 0)
                return;
            var pool = ColonyCrewPoolDB.Of(colony);
            int species = pool.Trained.Where(p => p.Value > 0).OrderByDescending(p => p.Value).ThenBy(p => p.Key).Select(p => p.Key).DefaultIfEmpty(-1).First();
            if (species < 0 && colony.TryGetDataBlob<ColonyInfoDB>(out var info))
                species = info.Population.OrderByDescending(p => p.Value).ThenBy(p => p.Key).Select(p => p.Key).DefaultIfEmpty(-1).First();
            pool.Add(species, count);
        }

        /// <summary>Gives a new ship its full crew without drawing anyone (scenario ships, test ships).</summary>
        public static void CrewFully(Entity ship)
        {
            var db = new ShipCrewDB(Split(Mounted(ship)).Capacity);
            ship.SetDataBlob(db);
        }

        /// <summary>
        /// A launch: the new ship takes min(required, berths) from the colony, largest species pool first.
        /// When the colony is short it launches under-crewed and an event says so. Returns crew boarded.
        /// </summary>
        public static int BoardAtLaunch(Entity ship, Entity colony, DateTime at)
        {
            var split = SplitOf(ship);
            int boarded = Draw(colony, split.Capacity);
            if (!ship.TryGetDataBlob<ShipCrewDB>(out var db))
            {
                db = new ShipCrewDB();
                ship.SetDataBlob(db);
            }
            db.Aboard = boarded;
            if (boarded < split.Required && colony.Manager != null)
            {
                EventManager.Instance.Publish(Event.Create(
                    EventType.ShipUnderCrewed,
                    at,
                    LaunchShortText(ship.GetName(ship.FactionOwnerID), boarded, split.Required),
                    ship.FactionOwnerID,
                    ship.Manager?.ManagerID ?? colony.Manager.ManagerID,
                    ship.Id));
            }
            return boarded;
        }

        public static string LaunchShortText(string name, int boarded, int required)
            => $"{(string.IsNullOrEmpty(name) ? "A ship" : name)} launched with {boarded}/{required} crew; train crew at an academy";

        /// <summary>Null when the ship and colony can swap crew (the ship orbits the colony's body); otherwise why not.</summary>
        public static string? TransferRefusal(Entity ship, Entity colony)
        {
            if (!ship.TryGetDataBlob<ShipCrewDB>(out _))
                return "This ship does not carry crew.";
            if (!colony.TryGetDataBlob<ColonyInfoDB>(out var info) || info.PlanetEntity == null || !info.PlanetEntity.IsValid)
                return "The colony has no body.";
            if (ship.Manager != colony.Manager
                || !ship.TryGetDataBlob<PositionDB>(out var pos) || pos.Parent == null || pos.Parent.Id != info.PlanetEntity.Id)
                return "The ship must be in orbit of the colony.";
            return null;
        }

        /// <summary>Tops up crew from the colony to min(required, berths), or by <paramref name="count"/> when ≥ 0. Returns how many boarded.</summary>
        public static int TopUp(Entity ship, Entity colony, int count = -1)
        {
            var db = ship.GetDataBlob<ShipCrewDB>();
            int room = Math.Max(0, SplitOf(ship).Capacity - db.Aboard);
            int want = count < 0 ? room : Math.Min(room, count);
            int got = Draw(colony, want);
            db.Aboard += got;
            return got;
        }

        /// <summary>Sends crew down to the colony's pool: everyone when <paramref name="count"/> is negative. Returns how many left.</summary>
        public static int ToPool(Entity ship, Entity colony, int count = -1)
        {
            var db = ship.GetDataBlob<ShipCrewDB>();
            int leave = count < 0 ? db.Aboard : Math.Min(db.Aboard, count);
            db.Aboard -= leave;
            Return(colony, leave);
            return leave;
        }

        /// <summary>After a refit: crew above min(required, berths) of the parts now mounted go to the yard colony's pool.</summary>
        public static int AfterRefit(Entity ship, Entity yard)
        {
            if (!ship.TryGetDataBlob<ShipCrewDB>(out var db))
                return 0;
            db.CachedParts = -1; // a refit can swap parts one for one; recount
            int cap = SplitOf(ship).Capacity;
            int excess = Math.Max(0, db.Aboard - cap);
            if (excess > 0)
            {
                db.Aboard -= excess;
                Return(yard, excess);
            }
            return excess;
        }

        /// <summary>Scrapping a ship at a colony returns everyone aboard to its pool, then removes the ship.</summary>
        public static int Scrap(Entity ship, Entity colony)
        {
            int returned = 0;
            if (ship.TryGetDataBlob<ShipCrewDB>(out var db))
            {
                returned = db.Aboard;
                db.Aboard = 0;
                Return(colony, returned);
            }
            ShipFactory.DestroyShip(ship);
            return returned;
        }
    }
}
