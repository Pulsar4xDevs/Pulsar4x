using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class FactionDB : BaseDataBlob
    {
        public List<Entity> Species;

        public List<StarSystem> KnownSystems;

        public List<Entity> Colonies;

        public List<Entity> ShipClasses;

        public int BaseResearchRate { get; set; }

        public FactionDB()
            : this(new List<Entity>(), new List<StarSystem>(), new List<Entity>(), 100 )
        {

        }

        public FactionDB(
            List<Entity> species,
            List<StarSystem> knownSystems,
            List<Entity> population,
            int baseResearchRate)
        {
            Species = species;
            KnownSystems = knownSystems;
            Colonies = population;
            ShipClasses = new List<Entity>();
            BaseResearchRate = baseResearchRate;
        }
        

        public FactionDB(FactionDB factionDB)
        {
            Species = new List<Entity>();
            KnownSystems = new List<StarSystem>();
            Colonies = new List<Entity>();
            ShipClasses = new List<Entity>();
            BaseResearchRate = factionDB.BaseResearchRate;
        }

        public override object Clone()
        {
            return new FactionDB(this);
        }
    }
}