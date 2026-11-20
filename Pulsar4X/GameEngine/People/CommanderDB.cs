using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using GameEngine.Engine.Orders;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Names;

namespace Pulsar4X.People
{
    public class CommanderDB : BaseDataBlob
    {
        public new static List<Type> GetDependencies() => new List<Type>() { typeof(NameDB) };

        public string Name { get; internal set; }
        public int Rank { get; internal set; }
        public CommanderTypes Type { get; internal set; }
        public int Experience { get; internal set; } = 0;
        public int ExperienceCap { get; internal set; } = 0;
        /// <summary>Per-domain XP (Command / Nav / Survey). <see cref="Experience"/> is the sum.</summary>
        public Dictionary<SkillDomain, int> Skills { get; internal set; } = new();
        public DateTime CommissionedOn { get; internal set; }
        public DateTime RankedOn { get; internal set; }
        public int AssignedTo { get; internal set; } = -1;

        /// <summary>
        /// The number the Client's portrait renderer turns into this person's face. Set once when the person is created
        /// (<see cref="CommanderFactory.Create"/>), kept for life and saved. 0 means not set yet: an old save gets one derived
        /// from the entity id on load (<see cref="Portraits"/>).
        /// </summary>
        [JsonProperty]
        public int PortraitSeed { get; internal set; }

        /// <summary>
        /// The species entity this person belongs to, or -1 when unknown (the founding staff, and anyone saved before this existed).
        /// It picks the portrait pack; a view falls back to the owning faction's primary species when it is -1.
        /// </summary>
        [JsonProperty]
        public int SpeciesId { get; internal set; } = -1;


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
            Skills = new Dictionary<SkillDomain, int>(commanderDB.Skills);
            CommissionedOn = commanderDB.CommissionedOn;
            RankedOn = commanderDB.RankedOn;
            PortraitSeed = commanderDB.PortraitSeed;
            SpeciesId = commanderDB.SpeciesId;
        }

        public override object Clone()
        {
            return new CommanderDB(this);
        }

        public override string ToString()
        {
            switch(Type)
            {
                // FIXME: need to get rid of staticreflib references
                // case CommanderTypes.Navy:
                //     return StaticRefLib.StaticData.Themes[StaticRefLib.GameSettings.CurrentTheme].NavyRanksAbbreviations[Rank] + " " + Name;
                default:
                    return Name;
            }
        }
    }
}
