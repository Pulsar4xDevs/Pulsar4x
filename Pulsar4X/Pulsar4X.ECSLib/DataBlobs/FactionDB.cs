using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class FactionDB : BaseDataBlob
    {
        public string Title;
        public List<Entity> Species;

        public List<StarSystem> KnownSystems;

        public List<Entity> Colonies;

        public List<Entity> ShipClasses; 

        public FactionDB(string title,
            List<Entity> species,
            List<StarSystem> knownSystems,
            List<Entity> population)
        {
            Title = title;
            Species = species;
            KnownSystems = knownSystems;
            Colonies = population;
            ShipClasses = new List<Entity>();
        }

        public FactionDB()
        {
        }

        public FactionDB(FactionDB factionDB)
        {
            Title = factionDB.Title;

            Species = new List<Entity>();
            KnownSystems = new List<StarSystem>();
            Colonies = new List<Entity>();
            ShipClasses = new List<Entity>();
        }
    }
}