using System.Collections.Generic;

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
        public CommanderNameSD Name { get; set; }

        public int Rank { get; set; }//maybe rank/title should be part of name

        public CommanderTypes Type { get; set; }

        public Dictionary<string, int> Bonuses { get; set; }//bonuses should be blobs of thier own maybe.

        public CommanderDB()
        {
        }

        public CommanderDB(CommanderNameSD name, int rank, CommanderTypes type, Dictionary<string, int> bonuses)
        {
            Name = name;
            Rank = rank;
            Type = type;
            Bonuses = bonuses;
        }

        public CommanderDB(CommanderDB commanderDB)
        {
            //Should we create new commander? I think no but we have rank in there and same commander with different ranks is not good.
            Name = commanderDB.Name;

            Rank = commanderDB.Rank;
            Type = commanderDB.Type;

            Bonuses = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int> keyValue in commanderDB.Bonuses)
            {
                Bonuses.Add(keyValue.Key, keyValue.Value);
            }

        }
    }
}
