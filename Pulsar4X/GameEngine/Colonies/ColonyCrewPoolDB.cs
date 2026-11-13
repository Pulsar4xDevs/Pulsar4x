using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Colonies
{
    /// <summary>
    /// A colony's trained ship crew waiting for a berth, by species id (crew plan phase 2). The academy's
    /// crew course fills it; a launch draws from it, largest species first; scrapping a ship, a crew-down
    /// order and a refit that drops berths return crew to it. People in the pool are not in the colony's
    /// population.
    /// </summary>
    public class ColonyCrewPoolDB : BaseDataBlob
    {
        public Dictionary<int, long> Trained { get; internal set; } = new();

        public ColonyCrewPoolDB() { }

        public ColonyCrewPoolDB(ColonyCrewPoolDB db)
        {
            Trained = new Dictionary<int, long>(db.Trained);
        }

        public override object Clone() => new ColonyCrewPoolDB(this);

        public long Total => Trained.Values.Sum();

        public void Add(int speciesId, long count)
        {
            if (count <= 0)
                return;
            Trained.TryGetValue(speciesId, out long have);
            Trained[speciesId] = have + count;
        }

        /// <summary>
        /// Takes up to <paramref name="count"/> crew, largest species pool first (ties by lower species id),
        /// and returns how many were taken. Crew aboard is not tracked by species yet.
        /// TODO(crew): track crew aboard by species so scrapping returns the same species.
        /// </summary>
        public long Take(long count, out int firstSpeciesId)
        {
            firstSpeciesId = -1;
            long taken = 0;
            while (taken < count)
            {
                var best = Trained.Where(p => p.Value > 0).OrderByDescending(p => p.Value).ThenBy(p => p.Key).FirstOrDefault();
                if (best.Value <= 0)
                    break;
                long take = Math.Min(best.Value, count - taken);
                if (best.Value - take <= 0)
                    Trained.Remove(best.Key);
                else
                    Trained[best.Key] = best.Value - take;
                if (firstSpeciesId < 0)
                    firstSpeciesId = best.Key;
                taken += take;
            }
            return taken;
        }

        public static ColonyCrewPoolDB Of(Pulsar4X.Engine.Entity colony)
        {
            if (!colony.TryGetDataBlob<ColonyCrewPoolDB>(out var pool))
            {
                pool = new ColonyCrewPoolDB();
                colony.SetDataBlob(pool);
            }
            return pool;
        }
    }
}
