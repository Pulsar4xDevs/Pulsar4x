using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.People;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Fleets;
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
    /// Crew capacity, crew aboard and what a short crew does (crew plan phases 1-3, crew rules phase 1).
    /// <para>
    /// Two groups aboard. Crew staff the parts (<see cref="ComponentDesign.CrewReq"/>) and sleep in crew bunks
    /// (parts with a CrewQuartersAtb: crew quarters only). Officers (the captain for now) are housed in the
    /// bridge's officer slots (OfficerSlotsAtb) and are not part crew. A bridge's own operators are crew and
    /// need bunks. A design with no bridge (AdminSpaceAtb) is automated: no crew, no captain, no life support,
    /// no orders of its own (it may ride in a fleet and follow the flagship).
    /// </para>
    /// <para>
    /// Crew aboard fill the engines first, then the bridge, then the work parts. The warp drives below their own
    /// crew run at crew / engine crew (reaction engines need no crew and are not counted). The work parts
    /// (geo and grav surveyors, asteroid miners, shuttlebays, construction bays) share what is left:
    /// F = clamp((crew - engine crew - bridge crew) / work crew, 0, 1), and they run at F².
    /// Nothing else degrades: lasers, fire control, sensors, reactors and the bridge run at full.
    /// </para>
    /// <para>
    /// ShipCrewDB.Aboard is everyone aboard (crew + the captain), so life support and wages follow it. The captain
    /// (ShipInfoDB.CommanderID) boards only into an officer slot, is never drawn from or returned to a colony pool,
    /// and does not crew the engines. Under-crewed compares crew aboard with part crew only.
    /// TODO(crew): weapon crew waits for the damage rework.
    /// </para>
    /// </summary>
    public static class ShipCrew
    {
        /// <summary>
        /// Crew and officers of a part list. <see cref="Required"/> is part crew (0 when automated), <see cref="Berths"/> the
        /// crew bunks, <see cref="OfficerSlots"/> the bridge cabins. <see cref="Captain"/> is 1 when a seated captain
        /// can board (a bridge with a slot); the captain is never part crew.
        /// </summary>
        public readonly record struct CrewSplit(int Required, int Berths, int Engine, int Bridge, int Work,
            int OfficerSlots = 0, bool NeedsCaptain = false, int Captain = 0)
        {
            /// <summary>Crew the bunks can house: min(part crew, bunks). 0 when automated.</summary>
            public int Capacity => Math.Min(Required, Berths);

            /// <summary>True with no bridge: no crew, no captain, no life support, no orders of its own.</summary>
            public bool Automated => !NeedsCaptain;

            /// <summary>Officers the ship wants: 1 (the captain) when it has a bridge, else 0.</summary>
            public int OfficersRequired => NeedsCaptain ? 1 : 0;
        }

        // ---- phase 1: bunks and officer slots ----

        /// <summary>Crew bunks one copy of the part gives; 0 for parts without quarters. A bridge gives none (its berths are officer slots).</summary>
        public static int BerthsPer(ComponentDesign design)
            => design != null && !IsBridge(design) && design.TryGetAttribute<CrewQuartersAtb>(out var q) ? q.Berths : 0;

        /// <summary>Officer slots one copy of the part gives: a bridge's OfficerSlotsAtb, or the CrewQuartersAtb an old saved bridge carries.</summary>
        public static int SlotsPer(ComponentDesign design)
        {
            if (design == null || !IsBridge(design))
                return 0;
            if (design.TryGetAttribute<OfficerSlotsAtb>(out var o))
                return o.Slots;
            return design.TryGetAttribute<CrewQuartersAtb>(out var legacy) ? legacy.Berths : 0;
        }

        /// <summary>Crew bunks the parts give (crew quarters only; ShipDesign.CrewBerths).</summary>
        public static int Berths(IEnumerable<(ComponentDesign design, int count)> components)
            => components?.Sum(c => BerthsPer(c.design) * c.count) ?? 0;

        /// <summary>Officer slots the parts give (bridges).</summary>
        public static int OfficerSlots(IEnumerable<(ComponentDesign design, int count)> components)
            => components?.Sum(c => SlotsPer(c.design) * c.count) ?? 0;

        /// <summary>True when a part list has a bridge: it needs a captain. No bridge means automated.</summary>
        public static bool NeedsCaptain(IEnumerable<(ComponentDesign design, int count)> components)
            => components?.Any(c => c.design != null && c.count > 0 && IsBridge(c.design)) ?? false;

        /// <summary>Σ part crew × count, whatever the design: what the parts would need if a crew ran them.</summary>
        public static int PartCrew(IEnumerable<(ComponentDesign design, int count)> components)
            => components?.Sum(c => (c.design?.CrewReq ?? 0) * c.count) ?? 0;

        /// <summary>Crew the design needs (ShipDesign.CrewReq): the part crew, or 0 for an automated design with no bridge. Excludes the captain.</summary>
        public static int CrewRequired(IEnumerable<(ComponentDesign design, int count)> components)
        {
            var list = components as IList<(ComponentDesign design, int count)> ?? components?.ToList();
            return NeedsCaptain(list) ? PartCrew(list) : 0;
        }

        /// <summary>Designer warnings about crew: fewer crew bunks than the parts need, or a bridge with no officer slot. None for an automated design.</summary>
        public static List<string> DesignCrewWarnings(IEnumerable<(ComponentDesign design, int count)> components)
        {
            var warnings = new List<string>();
            if (components == null) return warnings;
            var list = components.ToList();
            if (!NeedsCaptain(list))
                return warnings;
            int crew = PartCrew(list), bunks = Berths(list);
            if (bunks < crew)
                warnings.Add($"Crew {crew} but only {bunks} crew bunks: add crew quarters for {crew - bunks} more.");
            if (OfficerSlots(list) < 1)
                warnings.Add("The bridge has no officer slot for a captain.");
            return warnings;
        }

        // ---- phase 3: who does what ----

        /// <summary>Engines whose crew fill first and whose power scales by crew: warp drives only (reaction engines need no crew).</summary>
        public static bool IsEngine(ComponentDesign d) => d.HasAttribute<WarpDriveAtb>();

        public static bool IsBridge(ComponentDesign d) => d.HasAttribute<AdminSpaceAtb>();

        /// <summary>Parts whose rate scales by F²: surveyors, miners, shuttlebays (rate, not range), construction bays.</summary>
        public static bool IsWorkPart(ComponentDesign d)
            => d.HasAttribute<GeoSurveyAtb>() || d.HasAttribute<GravSurveyAtb>() || d.HasAttribute<AsteroidMinerAtb>()
               || d.HasAttribute<CargoTransferAtb>() || d.HasAttribute<ConstructionAtb>();

        public static CrewSplit Split(IEnumerable<(ComponentDesign design, int count)> components)
        {
            var list = (components ?? Enumerable.Empty<(ComponentDesign, int)>()).Where(c => c.design != null && c.count > 0).ToList();
            bool needsCaptain = NeedsCaptain(list);
            int required = 0, engine = 0, bridge = 0, work = 0;
            if (needsCaptain) // an automated design runs without crew
                foreach (var (design, count) in list)
                {
                    int crew = design.CrewReq * count;
                    required += crew;
                    if (IsEngine(design))
                        engine += crew;
                    else if (IsBridge(design))
                        bridge += crew;
                    else if (IsWorkPart(design))
                        work += crew;
                }
            return new CrewSplit(required, Berths(list), engine, bridge, work, OfficerSlots(list), needsCaptain);
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

        /// <summary>The mounted parts' split; <see cref="CrewSplit.Captain"/> is 1 when a seated captain can board (a bridge with a slot).</summary>
        public static CrewSplit SplitOf(Entity ship)
        {
            int parts = ship.TryGetDataBlob<ComponentInstancesDB>(out var instances) ? instances.AllComponents.Count : 0;
            bool seated = HasCaptain(ship);
            if (ship.TryGetDataBlob<ShipCrewDB>(out var db))
            {
                if (db.CachedParts != parts || db.CachedSeated != seated)
                {
                    db.CachedSplit = WithCaptain(Split(Mounted(ship)), seated);
                    db.CachedParts = parts;
                    db.CachedSeated = seated;
                }
                return db.CachedSplit;
            }
            return WithCaptain(Split(Mounted(ship)), seated);
        }

        static CrewSplit WithCaptain(CrewSplit split, bool seated)
            => seated && split.NeedsCaptain && split.OfficerSlots >= 1 ? split with { Captain = 1 } : split;

        /// <summary>True when an officer captains the ship.</summary>
        public static bool HasCaptain(Entity ship)
            => ship != null && ship.TryGetDataBlob<ShipInfoDB>(out var info) && info.CommanderID >= 0;

        /// <summary>1 when the captain is counted in the people aboard, else 0.</summary>
        static int CaptainAboard(ShipCrewDB db) => db.CaptainAboard ? 1 : 0;

        /// <summary>
        /// Boards or lands the captain when one is seated or leaves (ShipInfoDB.CommanderID) or the bridge changes.
        /// The captain boards only a ship with a bridge and an officer slot, and counts in the people aboard
        /// (life support, wages) but is not crew: no crew are drawn or returned.
        /// </summary>
        public static void SyncCaptain(Entity ship)
        {
            if (ship == null || ship.Manager == null || !ship.TryGetDataBlob<ShipCrewDB>(out var db))
                return;
            bool has = SplitOf(ship).Captain > 0;
            if (has == db.CaptainAboard)
                return;
            db.CaptainAboard = has;
            db.Aboard = has ? db.Aboard + 1 : Math.Max(0, db.Aboard - 1);
        }

        /// <summary>
        /// After a load: boards each seated captain once (saves from before captains counted), and clears the crew of a
        /// ship with no bridge (it is automated now). Designs and crew bunks are not rewritten.
        /// </summary>
        public static void AfterLoad(Entity ship)
        {
            SyncCaptain(ship);
            if (ship.TryGetDataBlob<ShipCrewDB>(out var db) && Automated(ship))
            {
                db.CaptainAboard = false;
                if (db.Aboard != 0)
                    db.Aboard = 0;
            }
        }

        /// <summary>True when the ship has parts but no bridge: automated, no crew, no captain, no orders of its own.</summary>
        public static bool Automated(Entity ship)
            => ship != null && ship.HasDataBlob<ShipInfoDB>() && !ship.HasDataBlob<FleetDB>()
               && ship.HasDataBlob<ComponentInstancesDB>() && SplitOf(ship).Automated
               && ship.GetDataBlob<ComponentInstancesDB>().AllComponents.Count > 0;

        /// <summary>True when the ship carries crew (every ship made by ShipFactory).</summary>
        public static bool Tracked(Entity ship) => ship.HasDataBlob<ShipCrewDB>();

        /// <summary>Everyone aboard: crew plus the captain (life support and wages follow this).</summary>
        public static int Aboard(Entity ship)
            => ship.TryGetDataBlob<ShipCrewDB>(out var db) ? db.Aboard : SplitOf(ship).Capacity + SplitOf(ship).Captain;

        /// <summary>Crew aboard, not counting the captain: what the part crew requirement is compared with.</summary>
        public static int CrewAboard(Entity ship)
            => ship.TryGetDataBlob<ShipCrewDB>(out var db) ? Math.Max(0, db.Aboard - CaptainAboard(db)) : SplitOf(ship).Capacity;

        /// <summary>Officers aboard: 1 when the captain is aboard, else 0.</summary>
        public static int OfficersAboard(Entity ship)
            => ship.TryGetDataBlob<ShipCrewDB>(out var db) ? CaptainAboard(db) : SplitOf(ship).Captain;

        /// <summary>True when a tracked ship has fewer crew aboard than its part crew. Never true for an automated ship, and a missing captain is not under-crewed.</summary>
        public static bool IsUnderCrewed(Entity ship)
            => Tracked(ship) && CrewAboard(ship) < SplitOf(ship).Required;

        /// <summary>True when a tracked ship has a bridge but no captain aboard ("No captain", not under-crewed).</summary>
        public static bool NoCaptain(Entity ship)
            => Tracked(ship) && SplitOf(ship) is { NeedsCaptain: true } split && split.Captain == 0;

        /// <summary>Share of the engines' own crew filled, 0..1 (crew fill the engines first). 1 without crew tracking.</summary>
        public static double EngineFactor(Entity ship)
        {
            if (ship == null || !ship.TryGetDataBlob<ShipCrewDB>(out var db))
                return 1;
            var split = SplitOf(ship);
            return EngineFactor(split, CrewAboard(ship));
        }

        /// <summary><paramref name="crewAboard"/> is crew only: the captain is not counted.</summary>
        public static double EngineFactor(CrewSplit split, int crewAboard)
            => split.Engine <= 0 ? 1 : Math.Clamp(crewAboard / (double)split.Engine, 0, 1);

        /// <summary>F for the work parts: the crew left after the engines and then the bridge, over the work parts' crew, 0..1.</summary>
        public static double WorkFactor(CrewSplit split, int crewAboard)
            => split.Work <= 0 ? 1 : Math.Clamp((crewAboard - split.Engine - split.Bridge) / (double)split.Work, 0, 1);

        public static double WorkFactor(Entity ship)
        {
            if (ship == null || !ship.TryGetDataBlob<ShipCrewDB>(out var db))
                return 1;
            return WorkFactor(SplitOf(ship), CrewAboard(ship));
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
            int crew = CrewAboard(ship);
            if (crew >= split.Required)
                return "";
            var parts = new List<string>();
            double engine = EngineFactor(split, crew);
            if (split.Engine > 0 && engine < 1)
                parts.Add($"engines {engine * 100:0} %");
            double work = WorkFactor(split, crew);
            if (split.Work > 0 && work < 1)
                parts.Add($"work parts {work * work * 100:0} %");
            string head = $"{crew}/{split.Required} crew";
            if (split.Berths < split.Required)
                head += $" ({split.Berths} bunks)";
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
                // Multi-faction phase 5: NPC crews come from civilians, not from staff at work.
                int have = (int)Math.Min(count, Pulsar4X.Colonies.Workforce.Civilians(colony));
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
                return colony.HasDataBlob<ColonyInfoDB>() ? (int)Math.Min(count, Pulsar4X.Colonies.Workforce.Civilians(colony)) : 0;
            return colony.TryGetDataBlob<ColonyCrewPoolDB>(out var pool) ? (int)Math.Min(count, pool.Total) : 0;
        }

        /// <summary>
        /// Gives crew back to the colony. A player's crew join the colony's pool: crew aboard is not tracked
        /// by species, so they join the pool's largest species, or the colony's largest population species
        /// when the pool is empty. An NPC's crew came from the population (<see cref="CrewsFromPopulation"/>),
        /// so they go back to it, as its largest species (to the pool only if the colony has no people left).
        /// TODO(crew): return the species that boarded.
        /// </summary>
        public static void Return(Entity colony, int count)
        {
            if (count <= 0)
                return;
            if (CrewsFromPopulation(colony) && colony.TryGetDataBlob<ColonyInfoDB>(out var npcInfo))
            {
                int npcSpecies = npcInfo.Population.Where(p => p.Value > 0).OrderByDescending(p => p.Value).ThenBy(p => p.Key)
                    .Select(p => p.Key).DefaultIfEmpty(-1).First();
                if (npcSpecies >= 0)
                {
                    NavalAcademyCourse.ReturnPeople(colony, count, npcSpecies);
                    return;
                }
            }
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
            db.CachedParts = -1;
            SyncCaptain(ship);
            RefreshLifeSupport(ship);
        }

        // ---- phase 4: life support ----

        /// <summary>Life support per crew aboard now, kW: the game's kW per crew × the owner's life-support tech factor.</summary>
        public static double KWPerCrew(Entity ship)
        {
            double perCrew = ship.Manager?.Game?.Settings?.ShipLifeSupport_kWPerCrew ?? GameSettings.DefaultShipLifeSupport_kWPerCrew;
            return perCrew * CrewTech.LifeSupportFactor(ship);
        }

        /// <summary>Life support the crew aboard need now, kW. 0 without crew tracking.</summary>
        public static double LifeSupportNeed(Entity ship)
            => ship.TryGetDataBlob<ShipCrewDB>(out var db) ? db.Aboard * KWPerCrew(ship) : 0;

        /// <summary>Life support booked into the ship's must-run demand, kW (0 for a ship with no plant).</summary>
        public static double LifeSupport(Entity ship)
            => ship != null && ship.TryGetDataBlob<ShipCrewDB>(out var db) ? db.LifeSupport_kW : 0;

        /// <summary>
        /// Books the crew's life support into the plant's must-run demand: aboard × kW per crew. Called whenever
        /// crew aboard change and when the life-support tech levels up. A ship with no plant books nothing.
        /// </summary>
        public static void RefreshLifeSupport(Entity ship)
        {
            if (ship == null || !ship.TryGetDataBlob<ShipCrewDB>(out var db)
                || !ship.TryGetDataBlob<Pulsar4X.Energy.EnergyGenAbilityDB>(out var energy) || ship.Manager == null)
                return;
            double target = db.Aboard * KWPerCrew(ship);
            double delta = target - db.LifeSupport_kW;
            if (Math.Abs(delta) < 1e-12)
                return;
            DateTime at = ship.Manager.ManagerSubpulses.NextSafeInterruptTime;
            // AddDemand runs the plant up to now at the old demand, then changes it.
            energy.AddDemand(delta, at);
            db.LifeSupport_kW = target;
            // A zero step at the new demand books the next fill or empty interrupt.
            Pulsar4X.Energy.EnergyGenProcessor.EnergyGen(ship, at);
        }

        /// <summary>Rebooks life support on every ship of the faction (the life-support tech levelled up).</summary>
        public static void RefreshFactionLifeSupport(Game game, int factionId)
        {
            foreach (var system in game.Systems)
                foreach (var ship in system.GetAllEntitiesWithDataBlob<ShipCrewDB>())
                    if (ship.FactionOwnerID == factionId)
                        RefreshLifeSupport(ship);
        }

        /// <summary>The "life support failing" line for the event log.</summary>
        public static string LifeSupportFailingText(string name, double lifeSupportKW, double outputKW)
            => $"{(string.IsNullOrEmpty(name) ? "A ship" : name)}: life support failing - the batteries are empty and the plant makes {outputKW:0.#} kW against {lifeSupportKW:0.#} kW of life support and other must-run loads";

        /// <summary>
        /// After a plant step: fires LifeSupportFailing once when the batteries are empty and the plant cannot
        /// carry the must-run demand (life support and any warp); clears when it can again.
        /// </summary>
        internal static void CheckLifeSupport(Entity ship, Pulsar4X.Energy.EnergyGenAbilityDB energy, double stored, DateTime at)
        {
            if (!ship.TryGetDataBlob<ShipCrewDB>(out var db) || !(db.LifeSupport_kW > 0))
                return;
            bool failing = stored <= 1e-9 && energy.TotalOutputMax + 1e-9 < energy.Demand;
            if (failing && !db.LifeSupportFailing)
            {
                db.LifeSupportFailing = true;
                EventManager.Instance.Publish(Event.Create(
                    EventType.LifeSupportFailing,
                    at,
                    LifeSupportFailingText(ship.GetName(ship.FactionOwnerID), energy.Demand, energy.TotalOutputMax),
                    ship.FactionOwnerID,
                    ship.Manager?.ManagerID,
                    ship.Id));
            }
            else if (!failing && db.LifeSupportFailing)
                db.LifeSupportFailing = false;
        }

        /// <summary>
        /// A launch: the new ship takes min(part crew, crew bunks) from the colony (none when automated), largest species pool first.
        /// When the colony is short it launches under-crewed and an event says so. Returns crew boarded
        /// (a seated captain is aboard too but is not drawn from the colony).
        /// </summary>
        public static int BoardAtLaunch(Entity ship, Entity colony, DateTime at)
        {
            if (!ship.TryGetDataBlob<ShipCrewDB>(out var db))
            {
                db = new ShipCrewDB();
                ship.SetDataBlob(db);
            }
            SyncCaptain(ship);
            var split = SplitOf(ship);
            int captain = CaptainAboard(db);
            int boarded = Draw(colony, split.Capacity);
            db.Aboard = boarded + captain;
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

        /// <summary>Tops up crew from the colony up to <see cref="CrewSplit.Capacity"/> (part crew the bunks can
        /// house; the captain is not crew and boards only an officer slot), or by <paramref name="count"/> when ≥ 0. Returns how many boarded.</summary>
        public static int TopUp(Entity ship, Entity colony, int count = -1)
        {
            var db = ship.GetDataBlob<ShipCrewDB>();
            int room = Math.Max(0, SplitOf(ship).Capacity - CrewAboard(ship));
            int want = count < 0 ? room : Math.Min(room, count);
            int got = Draw(colony, want);
            db.Aboard += got;
            return got;
        }

        /// <summary>Sends crew down to the colony's pool: everyone but the captain when <paramref name="count"/> is negative. Returns how many left.</summary>
        public static int ToPool(Entity ship, Entity colony, int count = -1)
        {
            var db = ship.GetDataBlob<ShipCrewDB>();
            int crew = Math.Max(0, db.Aboard - CaptainAboard(db));
            int leave = count < 0 ? crew : Math.Min(crew, count);
            db.Aboard -= leave;
            Return(colony, leave);
            return leave;
        }

        /// <summary>After a refit: crew above the new capacity (part crew the bunks can house; none when automated) go to the yard colony's pool.</summary>
        public static int AfterRefit(Entity ship, Entity yard)
        {
            if (!ship.TryGetDataBlob<ShipCrewDB>(out var db))
                return 0;
            db.CachedParts = -1; // a refit can swap parts one for one; recount
            SyncCaptain(ship);   // the new bridge may have no slot, or no bridge at all
            int cap = SplitOf(ship).Capacity;
            int excess = Math.Max(0, CrewAboard(ship) - cap);
            if (excess > 0)
            {
                db.Aboard -= excess;
                Return(yard, excess);
            }
            return excess;
        }

        /// <summary>Scrapping a ship at a colony returns its crew (not the captain) to its pool, then removes the ship.</summary>
        public static int Scrap(Entity ship, Entity colony)
        {
            int returned = 0;
            if (ship.TryGetDataBlob<ShipCrewDB>(out var db))
            {
                returned = Math.Max(0, db.Aboard - CaptainAboard(db)); // the captain is an officer, not pool crew
                db.Aboard = 0;
                Return(colony, returned);
            }
            ShipFactory.DestroyShip(ship);
            return returned;
        }
    }
}
