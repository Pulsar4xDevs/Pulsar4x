using System;
using System.Collections.Generic;
using GameEngine.People;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Names;
using Pulsar4X.Technology;

namespace Pulsar4X.People
{
    public enum HireRole
    {
        Administrator,
        LabTechnician,
    }

    /// <summary>
    /// Turns colony population into civilians. Each colony keeps a small pool that rolls over on the monthly
    /// population pass. A hire takes one person out of the population. Every roll uses the game's seeded random.
    /// </summary>
    public static class Recruitment
    {
        /// <summary>Below this many people a colony has no candidates.</summary>
        public const long MinimumPopulation = 100_000;
        public const int MaxPoolSize = 5;

        /// <summary>Each refresh removes 0 to MaxRemoved unpicked candidates and adds 1 to MaxAdded new ones.</summary>
        public const int MaxRemoved = 2;
        public const int MaxAdded = 2;

        /// <summary>Shown on every candidate until payroll exists. Nothing is charged.</summary>
        public const int PlaceholderWage = 100;

        /// <summary>1 at the minimum, plus one per order of magnitude above it, capped at MaxPoolSize. 0 below the minimum.</summary>
        public static int PoolSize(long population)
        {
            if (population < MinimumPopulation)
                return 0;
            int size = 1;
            long step = MinimumPopulation * 10;
            while (population >= step && size < MaxPoolSize)
            {
                size++;
                step *= 10;
            }
            return size;
        }

        public static long PopulationOf(Entity colony)
            => colony.TryGetDataBlob<ColonyInfoDB>(out var info) ? NavalAcademyCourse.PopulationOf(info) : 0;

        /// <summary>Null when the colony has candidates or none have been rolled yet. Otherwise the sentence the UI shows.</summary>
        public static string? EmptyReason(Entity colony)
        {
            if (PopulationOf(colony) < MinimumPopulation)
                return "Population below " + MinimumPopulation.ToString("N0") + ".";
            if (colony.TryGetDataBlob<CandidatePoolDB>(out var pool) && pool.Candidates.Count > 0)
                return null;
            if (pool == null || pool.LastRefresh == default)
                return "First candidates arrive with the next population update.";
            return "Next candidates on " + pool.LastRefresh.AddDays(30).ToShortDateString() + ".";
        }

        /// <summary>
        /// The monthly roll. The first refresh fills the pool. Later ones remove a few unpicked candidates and
        /// add a few new ones, so the pool turns over gradually. Below the minimum the pool is empty.
        /// </summary>
        public static void Refresh(Entity colony, DateTime now)
        {
            var game = colony.Manager!.Game;
            if (!colony.TryGetDataBlob<CandidatePoolDB>(out var pool))
            {
                pool = new CandidatePoolDB();
                colony.SetDataBlob(pool);
            }

            bool first = pool.LastRefresh == default;
            pool.LastRefresh = now;
            pool.LastRemoved = 0;
            pool.LastAdded = 0;

            int size = PoolSize(PopulationOf(colony));
            if (size == 0)
            {
                pool.LastRemoved = pool.Candidates.Count;
                pool.Candidates.Clear();
                return;
            }

            if (!first)
            {
                // Leave at least one standing so a refresh never replaces the whole pool.
                int remove = Math.Min(game.RNG.Next(0, MaxRemoved + 1), Math.Max(0, pool.Candidates.Count - 1));
                for (int i = 0; i < remove; i++)
                    pool.Candidates.RemoveAt(game.RNG.Next(0, pool.Candidates.Count));
                pool.LastRemoved = remove;
            }

            while (pool.Candidates.Count > size)
            {
                pool.Candidates.RemoveAt(game.RNG.Next(0, pool.Candidates.Count));
                pool.LastRemoved++;
            }

            int add = first ? size : game.RNG.Next(1, MaxAdded + 1);
            add = Math.Min(add, size - pool.Candidates.Count);
            for (int i = 0; i < add; i++)
                pool.Candidates.Add(Roll(colony, now));
            pool.LastAdded = Math.Max(0, add);

            AutoHire(colony);
        }

        static Candidate Roll(Entity colony, DateTime now)
        {
            var game = colony.Manager!.Game;
            return new Candidate
            {
                Id = Game.GetEntityID(),
                Name = NameFactory.GetCommanderName(game),
                Age = game.RNG.Next(CommanderFactory.ScientistMinAge, CommanderFactory.ScientistMaxAge + 1),
                SpeciesId = LargestSpecies(colony),
                LabCategory = CommanderFactory.RandomTechCategory(game),
                MaxLabs = (byte)game.RNG.Next(CommanderFactory.ScientistMinLabs, CommanderFactory.ScientistMaxLabs + 1),
                JoinedOn = now,
            };
        }

        static int LargestSpecies(Entity colony)
        {
            int best = -1;
            long most = 0;
            if (colony.TryGetDataBlob<ColonyInfoDB>(out var info))
            {
                foreach (var (id, count) in info.Population)
                {
                    if (count > most)
                    {
                        best = id;
                        most = count;
                    }
                }
            }
            return best;
        }

        /// <summary>Every candidate in the faction's colony pools, with the colony they stand on.</summary>
        public static List<(Candidate candidate, Entity colony)> FactionCandidates(Entity faction)
        {
            var list = new List<(Candidate, Entity)>();
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var info))
                return list;
            foreach (var colony in info.Colonies)
            {
                if (!colony.IsValid || colony.FactionOwnerID != faction.Id)
                    continue;
                if (!colony.TryGetDataBlob<CandidatePoolDB>(out var pool))
                    continue;
                foreach (var candidate in pool.Candidates)
                    list.Add((candidate, colony));
            }
            return list;
        }

        /// <summary>
        /// Hires one candidate from this colony's pool. Null on success, otherwise why not.
        /// An administrator joins the faction unassigned. A lab technician is housed on the colony.
        /// </summary>
        public static string? Hire(Entity faction, Entity colony, int candidateId, HireRole role, out Entity? admin, out Scientist? scientist)
        {
            admin = null;
            scientist = null;
            if (colony.FactionOwnerID != faction.Id)
                return "This colony belongs to another faction.";
            if (!colony.TryGetDataBlob<CandidatePoolDB>(out var pool))
                return "This colony has no candidates.";
            var candidate = pool.Candidates.Find(c => c.Id == candidateId);
            if (candidate == null)
                return "That candidate is no longer available.";

            var info = colony.GetDataBlob<ColonyInfoDB>();
            int speciesId = candidate.SpeciesId;
            if (!info.Population.TryGetValue(speciesId, out var count) || count < 1)
            {
                speciesId = LargestSpecies(colony);
                if (speciesId < 0)
                    return "The colony has nobody to hire.";
            }

            info.Population[speciesId] -= 1;
            pool.Candidates.Remove(candidate);

            var game = colony.Manager!.Game;
            if (role == HireRole.Administrator)
            {
                var db = new CommanderDB
                {
                    Name = candidate.Name,
                    Rank = 1,
                    Type = DataStructures.CommanderTypes.Civilian,
                    CommissionedOn = colony.StarSysDateTime,
                    RankedOn = colony.StarSysDateTime,
                };
                admin = CommanderFactory.Create(colony.Manager, faction.Id, db);
            }
            else
            {
                scientist = CommanderFactory.CreateScientist(faction, colony, candidate.Name, candidate.Age, candidate.LabCategory, candidate.MaxLabs);
                colony.GetDataBlob<TeamsHousedDB>().AddTeam(scientist);
            }
            return null;
        }

        static bool HasLab(Entity colony)
        {
            foreach (var lab in colony.Manager!.GetAllDataBlobsOfType<ResearcherDB>())
            {
                if (lab.LocationId == colony.Id)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// A faction that hires for itself fills empty admin seats first, then a lab technician if the colony
        /// has a lab and nobody to run it. A player faction never auto-hires.
        /// </summary>
        public static void AutoHire(Entity colony)
        {
            var game = colony.Manager!.Game;
            if (!game.Factions.TryGetValue(colony.FactionOwnerID, out var faction))
                return;
            if (!faction.TryGetDataBlob<FactionInfoDB>(out var info) || !info.AutoHires)
                return;
            if (!colony.TryGetDataBlob<CandidatePoolDB>(out var pool))
                return;

            if (colony.TryGetDataBlob<AdminSpaceDB>(out var adminSpace))
            {
                foreach (var seat in adminSpace.CommanderSeats)
                {
                    if (seat.CommanderID >= 0)
                        continue;
                    if (pool.Candidates.Count == 0)
                        return;
                    if (Hire(faction, colony, pool.Candidates[0].Id, HireRole.Administrator, out var admin, out _) != null || admin == null)
                        return;
                    var db = admin.GetDataBlob<CommanderDB>();
                    seat.CommanderID = admin.Id;
                    seat.Commander = db;
                    db.AssignedTo = colony.Id;
                }
            }

            if (pool.Candidates.Count == 0 || !HasLab(colony))
                return;
            var teams = colony.GetDataBlob<TeamsHousedDB>();
            bool hasScientist = teams.TeamsByType.TryGetValue(DataStructures.TeamTypes.Science, out var science) && science.Count > 0;
            if (!hasScientist)
                Hire(faction, colony, pool.Candidates[0].Id, HireRole.LabTechnician, out _, out _);
        }
    }
}
