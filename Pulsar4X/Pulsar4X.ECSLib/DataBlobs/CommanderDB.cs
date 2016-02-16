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
        public CommanderNameSD Name
        {
            get { return _name; }
            internal set { _name = value; }
        }
        [JsonProperty]
        private CommanderNameSD _name;

        public int Rank //maybe rank/title should be part of name
        {
            get { return _rank; }
            internal set { _rank = value; }
        } 
        [JsonProperty]
        private int _rank;

        public CommanderTypes Type
        {
            get { return _type; }
            internal set { _type = value; }
        }
        [JsonProperty]
        private CommanderTypes _type;

        public CommanderDB()
        {
          
        }

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
