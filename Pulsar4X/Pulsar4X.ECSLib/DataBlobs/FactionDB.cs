using System.Collections.Generic;

namespace Pulsar4X.ECSLib.DataBlobs
{
    public class FactionDB : BaseDataBlob
    {
        public string Title;
        public List<SpeciesDB> Species;

        public List<StarSystem> KnownSystems;

        public List<ColonyInfoDB> Populations;

        public FactionDB(string title,
            List<SpeciesDB> species,
            List<StarSystem> knownSystems,
            List<ColonyInfoDB> population)
        {
            Title = title;
            Species = species;
            KnownSystems = knownSystems;
            Populations = population;
        }
    }
}