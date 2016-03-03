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
        public CommanderNameSD Name { get; internal set; }

        [JsonProperty]
        public int Rank { get; internal set; }

        [JsonProperty]
        public CommanderTypes Type { get; internal set; }

        public CommanderDB() { }

        public CommanderDB(CommanderNameSD name, int rank, CommanderTypes type)
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
    }
}
