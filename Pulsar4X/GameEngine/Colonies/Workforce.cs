using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.Logistics;
using Pulsar4X.People;

namespace Pulsar4X.Colonies
{
    /// <summary>
    /// Multi-faction phase 5 (plan §7.1, §7.6): required staff from enabled installations, the daily hire ramp,
    /// staffing shares by <see cref="StaffTier"/>, civilians for the academy / recruitment / NPC crews, and
    /// commuter contracts with same-body blocs.
    /// </summary>
    public static class Workforce
    {
        /// <summary>The hire ramp: Employed moves toward Target by at most this share of Target a day.</summary>
        public const double DailyHireShare = 0.10;
        /// <summary>A bloc's labour pool: this share of its population on the body, less staff already contracted out.</summary>
        public const double LabourPoolShare = 0.05;
        /// <summary>Signing fee per person to turn a commuter (or an off-world recruit) into a resident, paid to the bloc.</summary>
        public const decimal SigningFeePerPerson = 100m;

        // ------------------------------------------------------------------ tiers

        /// <summary>The staffing tier of an installation design (plan §7.1): its power tier, with plants, storage of
        /// energy and infrastructure on top as Essential. Anything with no power tier is Control.</summary>
        public static StaffTier TierOf(ComponentDesign design)
        {
            if (design.TryGetAttribute<InfrastructureCapacityAtb>(out _) || design.TryGetAttribute<PowerPlantAtb>(out _)
                || design.TryGetAttribute<EnergySolarGenerationAtb>(out _) || design.TryGetAttribute<EnergyStoreAtb>(out _))
                return StaffTier.Essential;
            if (design.TryGetAttribute<PowerConsumerAtb>(out var consumer) && consumer.Draw_kW > 0)
            {
                return consumer.Tier switch
                {
                    PowerTier.LifeSupport => StaffTier.Essential,
                    PowerTier.Control => StaffTier.Control,
                    PowerTier.Storage or PowerTier.Charging => StaffTier.Storage,
                    PowerTier.Industry => StaffTier.Industry,
                    PowerTier.Mining => StaffTier.Mining,
                    PowerTier.Research => StaffTier.Research,
                    _ => StaffTier.Control,
                };
            }
            return StaffTier.Control;
        }

        /// <summary>Staff by tier: each tier gets min(1, left ÷ demand) top-down; tiers with no demand are 1.</summary>
        public static Dictionary<StaffTier, double> ShareByTier(long staff, IReadOnlyDictionary<StaffTier, long> demandByTier)
        {
            var result = new Dictionary<StaffTier, double>();
            long left = Math.Max(0, staff);
            foreach (StaffTier tier in Enum.GetValues(typeof(StaffTier)))
            {
                long demand = demandByTier.TryGetValue(tier, out var d) ? d : 0;
                if (demand <= 0)
                {
                    result[tier] = 1;
                    continue;
                }
                long got = Math.Min(left, demand);
                result[tier] = got >= demand ? 1 : (double)got / demand;
                left -= got;
            }
            return result;
        }

        /// <summary>The staffed share of <paramref name="tier"/> at the colony; 1 when the colony has no workforce record
        /// (an old save before its first pass, a ship, a test colony) so output is unchanged.</summary>
        public static double Share(Entity? colony, StaffTier tier)
        {
            if (colony == null || !colony.TryGetDataBlob<ColonyWorkforceDB>(out var db))
                return 1;
            return db.TierStaffing.TryGetValue(tier, out var share) ? share : 1;
        }

        // ------------------------------------------------------------------ people

        public static long PopulationOf(Entity colony)
            => colony.TryGetDataBlob<ColonyInfoDB>(out var info) ? NavalAcademyCourse.PopulationOf(info) : 0;

        /// <summary>Residents not at work (trainees have already left the population). The academy, recruitment and
        /// NPC ship crews draw only from these.</summary>
        public static long Civilians(Entity colony)
        {
            long pop = PopulationOf(colony);
            long employed = colony.TryGetDataBlob<ColonyWorkforceDB>(out var db) ? db.Employed : 0;
            return Math.Max(0, pop - employed);
        }

        /// <summary>
        /// The colony's workforce record, created on first use with Employed = min(Population, Required) so an old
        /// save (or a new colony) is fully staffed when it can be and its output is unchanged.
        /// </summary>
        public static ColonyWorkforceDB? Ensure(Entity colony)
        {
            if (!colony.HasDataBlob<ColonyInfoDB>() || !colony.HasDataBlob<ComponentInstancesDB>())
                return null;
            if (colony.TryGetDataBlob<ColonyWorkforceDB>(out var db))
                return db;
            db = new ColonyWorkforceDB();
            colony.SetDataBlob(db);
            Recalc(colony, initial: true);
            return db;
        }

        /// <summary>
        /// Recomputes Required and the tier split from enabled installations, applies lay-offs at once (contract
        /// staff first), and refreshes the shares. <paramref name="initial"/> staffs a fresh record to min(pop, required).
        /// </summary>
        public static void Recalc(Entity colony, bool initial = false)
        {
            if (!colony.TryGetDataBlob<ColonyWorkforceDB>(out var db) || !colony.TryGetDataBlob<ComponentInstancesDB>(out var instances))
                return;
            var byTier = new Dictionary<StaffTier, long>();
            long required = 0;
            foreach (var (design, _) in instances.DesignsAndComponentCount)
            {
                if (design.CrewReq <= 0)
                    continue;
                int enabled = instances.GetComponentsBySpecificDesign(design.UniqueID).Count(c => c.IsEnabled);
                if (enabled == 0)
                    continue;
                long need = (long)design.CrewReq * enabled;
                var tier = TierOf(design);
                byTier[tier] = (byTier.TryGetValue(tier, out var t) ? t : 0) + need;
                required += need;
            }
            db.Required = required;
            db.RequiredByTier = byTier;
            long pop = PopulationOf(colony);
            if (initial)
                db.Employed = Math.Min(pop, required);
            LayOff(db, Math.Max(0, db.Target ?? required));
            db.Employed = Math.Min(db.Employed, pop);
            db.TierStaffing = ShareByTier(db.Employed + db.CommuterTotal, byTier);
        }

        /// <summary>Cuts staff to <paramref name="target"/>: contract staff go first (largest contract first), then own staff.</summary>
        static void LayOff(ColonyWorkforceDB db, long target)
        {
            long over = db.Employed + db.CommuterTotal - target;
            foreach (var bloc in db.Commuters.OrderByDescending(c => c.Value).ThenBy(c => c.Key).Select(c => c.Key).ToList())
            {
                if (over <= 0)
                    break;
                long cut = Math.Min(over, db.Commuters[bloc]);
                db.Commuters[bloc] -= cut;
                if (db.Commuters[bloc] <= 0)
                    db.Commuters.Remove(bloc);
                over -= cut;
            }
            if (over > 0)
                db.Employed = Math.Max(0, db.Employed - over);
        }

        /// <summary>
        /// One day: ends contracts with blocs no longer Friendly, recalculates, then hires own residents toward
        /// Target by up to <see cref="DailyHireShare"/> of Target a day while civilians are available.
        /// </summary>
        public static void RunDay(Entity colony, DateTime at)
        {
            var db = Ensure(colony);
            if (db == null)
                return;
            var game = colony.Manager?.Game;
            if (game != null)
            {
                foreach (var bloc in db.Commuters.Keys.ToList())
                    if (!FriendlyBothWays(game, colony.FactionOwnerID, bloc))
                        db.Commuters.Remove(bloc);
            }
            Recalc(colony);
            if (db.LastHireDay is DateTime last && last.Date >= at.Date)
                return;
            db.LastHireDay = at.Date;
            long target = Math.Max(0, db.Target ?? db.Required);
            long want = target - db.Employed - db.CommuterTotal;
            if (want <= 0)
                return;
            long step = Math.Max(1, (long)Math.Ceiling(target * DailyHireShare));
            long hire = Math.Min(Math.Min(want, step), Civilians(colony));
            if (hire <= 0)
                return;
            db.Employed += hire;
            db.TierStaffing = ShareByTier(db.Employed + db.CommuterTotal, db.RequiredByTier);
        }

        /// <summary>Sets the wanted headcount (null = Required). Lay-offs apply at once; hiring ramps daily.</summary>
        public static void SetTarget(Entity colony, long? target)
        {
            var db = Ensure(colony);
            if (db == null)
                return;
            db.Target = target is long t ? Math.Max(0, t) : null;
            Recalc(colony);
        }

        // ------------------------------------------------------------------ commuters (§7.6)

        public static bool FriendlyBothWays(Game game, int a, int b)
        {
            if (a == b)
                return true;
            if (!game.Factions.TryGetValue(a, out var fa) || !game.Factions.TryGetValue(b, out var fb)
                || !fa.TryGetDataBlob<FactionInfoDB>(out var ia) || !fb.TryGetDataBlob<FactionInfoDB>(out var ib))
                return false;
            return ia.Stances.TryGetValue(b, out var sa) && sa >= Pulsar4X.Api.FactionStance.Friendly
                && ib.Stances.TryGetValue(a, out var sb) && sb >= Pulsar4X.Api.FactionStance.Friendly;
        }

        /// <summary>Contract staff a bloc has out on this body, across every colony there.</summary>
        public static long ContractedFrom(Entity body, int blocFactionId)
        {
            long total = 0;
            foreach (var c in body.ColoniesOnBody())
                if (c.TryGetDataBlob<ColonyWorkforceDB>(out var db) && db.Commuters.TryGetValue(blocFactionId, out var n))
                    total += n;
            return total;
        }

        /// <summary>A bloc's labour pool on the body: <see cref="LabourPoolShare"/> of its population there less what it has
        /// contracted out. (v1: it refills as soon as contracts end; the monthly refill is a later nicety.)</summary>
        public static long LabourPool(Entity body, int blocFactionId)
        {
            long pop = body.ColoniesOnBody().Where(c => c.FactionOwnerID == blocFactionId).Sum(PopulationOf);
            return Math.Max(0, (long)(pop * LabourPoolShare) - ContractedFrom(body, blocFactionId));
        }

        /// <summary>
        /// Adds (count &gt; 0) or ends (count &lt; 0) contract staff from <paramref name="bloc"/>'s faction at
        /// <paramref name="colony"/>. Same body, a different faction, Friendly both ways, within the labour pool and the
        /// colony's open posts. No fee; wages are paid into the bloc's funds monthly (phase 6). Returns null or the refusal.
        /// </summary>
        public static string? TryContract(Entity colony, Entity bloc, long count)
        {
            var db = Ensure(colony);
            var game = colony.Manager?.Game;
            if (db == null || game == null)
                return "Not a colony.";
            if (bloc.FactionOwnerID == colony.FactionOwnerID)
                return "Contract staff come from another faction.";
            if (!SameBody.Is(colony, bloc))
                return "Commuters only come from a colony on the same body.";
            if (!FriendlyBothWays(game, colony.FactionOwnerID, bloc.FactionOwnerID))
                return "Contracts need Friendly relations both ways.";
            int blocId = bloc.FactionOwnerID;
            db.Commuters.TryGetValue(blocId, out var have);
            if (count < 0)
            {
                long end = Math.Min(have, -count);
                if (have - end <= 0) db.Commuters.Remove(blocId); else db.Commuters[blocId] = have - end;
                Recalc(colony);
                return null;
            }
            var body = colony.GetDataBlob<ColonyInfoDB>().PlanetEntity;
            if (count > LabourPool(body, blocId))
                return $"The labour pool has {LabourPool(body, blocId):N0} people.";
            long target = Math.Max(0, db.Target ?? db.Required);
            long open = target - db.Employed - db.CommuterTotal;
            if (count > open)
                return $"Only {Math.Max(0, open):N0} posts are open.";
            db.Commuters[blocId] = have + count;
            Recalc(colony);
            return null;
        }

        /// <summary>
        /// "Relocate to company": turns <paramref name="count"/> commuters from <paramref name="bloc"/>'s faction into
        /// residents. They leave the bloc colony's population and join this colony's, stay at work as own staff, and
        /// the colony's faction pays the bloc <see cref="SigningFeePerPerson"/> each. Returns null or the refusal.
        /// </summary>
        public static string? TryRelocateCommuters(Entity colony, Entity bloc, long count)
        {
            var db = Ensure(colony);
            var game = colony.Manager?.Game;
            if (db == null || game == null || count <= 0)
                return "Nothing to relocate.";
            if (!SameBody.Is(colony, bloc))
                return "The bloc colony is not on this body.";
            int blocId = bloc.FactionOwnerID;
            if (!db.Commuters.TryGetValue(blocId, out var have) || have < count)
                return $"Only {have:N0} commuters are under contract with that bloc.";
            if (FactionSpending.IsBlocked(colony))
                return FactionSpending.FundsNegative;
            if (!game.Factions.TryGetValue(colony.FactionOwnerID, out var payer) || !payer.TryGetDataBlob<FactionInfoDB>(out var payerInfo)
                || !game.Factions.TryGetValue(blocId, out var payee) || !payee.TryGetDataBlob<FactionInfoDB>(out var payeeInfo))
                return "Faction has no ledger.";
            var blocInfo = bloc.GetDataBlob<ColonyInfoDB>();
            if (NavalAcademyCourse.PopulationOf(blocInfo) < count)
                return "The bloc colony does not have that many people.";
            decimal fee = SigningFeePerPerson * count;
            if (payerInfo.Money.GetCurrentFunds() < fee)
                return $"The signing fee is {fee:N0} cr.";

            var info = colony.GetDataBlob<ColonyInfoDB>();
            int species = info.Population.OrderByDescending(p => p.Value).ThenBy(p => p.Key).Select(p => p.Key).DefaultIfEmpty(-1).First();
            if (species < 0)
                return "The colony has no species to join.";
            NavalAcademyCourse.TakePeople(blocInfo, (int)count);
            info.Population[species] += count;
            if (have - count <= 0) db.Commuters.Remove(blocId); else db.Commuters[blocId] = have - count;
            db.Employed += count;
            var now = colony.StarSysDateTime;
            payerInfo.Money.AddExpense(now, TransactionCategory.Wages, $"Signing fee: {count:N0} commuters relocated x {SigningFeePerPerson:N0} cr", fee);
            payeeInfo.Money.AddIncome(now, TransactionCategory.Wages, $"Signing fee: {count:N0} residents left for {colony.GetName(blocId)}", fee);
            Recalc(colony);
            return null;
        }
    }

    /// <summary>Multi-faction phase 5: the daily workforce pass (hire ramp, contract checks, shares) for every colony.</summary>
    public class WorkforceProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromDays(1);
        /// <summary>Before industry, mining and research use the shares.</summary>
        public TimeSpan FirstRunOffset => TimeSpan.FromHours(0.2);
        /// <summary>Keyed on the workforce record (every colony gets one at creation or load).</summary>
        public Type GetParameterType => typeof(ColonyWorkforceDB);
        public void Init(Game game) { }

        public void ProcessEntity(Entity entity, int deltaSeconds)
            => Workforce.RunDay(entity, entity.StarSysDateTime);

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var colonies = manager.GetAllEntitiesWithDataBlob<ColonyWorkforceDB>();
            foreach (var colony in colonies)
                Workforce.RunDay(colony, manager.StarSysDateTime);
            return colonies.Count;
        }
    }
}
