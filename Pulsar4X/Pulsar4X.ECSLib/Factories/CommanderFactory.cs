using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class CommanderFactory
    {
        public static CommanderDB CreateShipCaptain(Random rng, string themeName)
        {
            var theme = StaticRefLib.StaticData.Themes[themeName];
            var commander = new CommanderDB()
            {
                Name = theme.CommanderNames[rng.Next(0, theme.CommanderNames.Count)],
                Rank = 6,
                Type = CommanderTypes.Navy
            };

            return commander;
        }

        public static Scientist CreateScientist(Entity faction, Entity location)
        {
            //all this stuff needs a proper bit of code to get names from a file or something

            //this is going to have to be thought out properly.
            Dictionary<ResearchCategories, float> bonuses = new Dictionary<ResearchCategories, float>();
            bonuses.Add(ResearchCategories.PowerAndPropulsion, 1.1f);

            Scientist sci = new Scientist();
            sci.Name = "Augusta King";
            sci.Age = 30;
            sci.MaxLabs = 25;
            sci.Bonuses = bonuses;
            
            var factionTech = faction.GetDataBlob<FactionTechDB>();
            factionTech.AllScientists.Add((sci, location));
            
            return sci;    
        }
    }
}
