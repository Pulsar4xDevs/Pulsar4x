using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public enum CommanderTypes
    {
        Navy,
        Ground,
        Civilian
    }

    public class CommanderDB : BaseDataBlob
    {
        public static readonly Dictionary<int, string> NavyRanks = new Dictionary<int, string>()
        {
            { 1, "Ensign" },
            { 2, "Lieutenant (JG)" },
            { 3, "Lieutenant" },
            { 4, "Lieutenant Commander" },
            { 5, "Commander" },
            { 6, "Captain" },
            { 7, "Rear Admiral (lower half)" },
            { 8, "Rear Admiral" },
            { 9, "Vice Admiral" },
            { 10, "Admiral" }
        };

        public static readonly Dictionary<int, string> NavyRanksAbbreviations = new Dictionary<int, string>()
        {
            { 1, "ENS" },
            { 2, "LTJG" },
            { 3, "LT" },
            { 4, "LTCR" },
            { 5, "CDR" },
            { 6, "CAPT" },
            { 7, "RDML" },
            { 8, "RADM" },
            { 9, "VADM" },
            { 10, "ADM" }
        };

        [JsonProperty]
        public string Name { get; internal set; }

        [JsonProperty]
        public int Rank { get; internal set; }

        [JsonProperty]
        public CommanderTypes Type { get; internal set; }

        public CommanderDB() { }

        public CommanderDB(string name, int rank, CommanderTypes type)
        {
            Name = name;
            Rank = rank;
            Type = type;
        }

        public CommanderDB(CommanderDB commanderDB)
        {
            //Should we create new commander? I think no but we have rank in there and same commander with different ranks is not good.
            Name = commanderDB.Name;

            Rank = commanderDB.Rank;
            Type = commanderDB.Type;
        }

        public override object Clone()
        {
            return new CommanderDB(this);
        }

        public override string ToString()
        {
            switch(Type)
            {
                case CommanderTypes.Navy:
                    return NavyRanksAbbreviations[Rank] + " " + Name;
                default:
                    return Name;
            }
        }
    }
}
