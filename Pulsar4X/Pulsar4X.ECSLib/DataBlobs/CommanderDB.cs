using System;
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
        [JsonProperty]
        public string Name { get; internal set; }

        [JsonProperty]
        public int Rank { get; internal set; }

        [JsonProperty]
        public CommanderTypes Type { get; internal set; }

        [JsonProperty]
        public int Experience { get; internal set; } = 0;

        [JsonProperty]
        public int ExperienceCap { get; internal set; } = 0;

        public DateTime CommissionedOn { get; internal set; }
        public DateTime RankedOn {get; internal set; }

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
            Experience = commanderDB.Experience;
            ExperienceCap = commanderDB.ExperienceCap;
            CommissionedOn = commanderDB.CommissionedOn;
            RankedOn = commanderDB.RankedOn;
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
                    return StaticRefLib.StaticData.Themes[StaticRefLib.GameSettings.CurrentTheme].NavyRanksAbbreviations[Rank] + " " + Name;
                default:
                    return Name;
            }
        }
    }
}
