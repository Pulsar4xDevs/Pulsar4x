﻿using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class FactionDB : BaseDataBlob
    {
        public string Title;
        public List<SpeciesDB> Species;

        public List<StarSystem> KnownSystems;

        public List<ColonyInfoDB> Populations;

        public List<Entity> ShipClasses; 

        public FactionDB(string title,
            List<SpeciesDB> species,
            List<StarSystem> knownSystems,
            List<ColonyInfoDB> population)
        {
            Title = title;
            Species = species;
            KnownSystems = knownSystems;
            Populations = population;
            ShipClasses = new List<Entity>();
        }

        public FactionDB()
        {
        }

        public FactionDB(FactionDB factionDB)
        {
            Title = factionDB.Title;

            Species = new List<SpeciesDB>();
            KnownSystems = new List<StarSystem>();
            Populations = new List<ColonyInfoDB>();
            ShipClasses = new List<Entity>();
        }
    }
}