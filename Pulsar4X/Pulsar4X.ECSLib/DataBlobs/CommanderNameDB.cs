namespace Pulsar4X.ECSLib
{
    public class CommanderNameDB
    {
        public string First;
        public string Last;
        public bool IsFemale;

        public CommanderNameDB()
        {
        }

        public CommanderNameDB(CommanderNameDB commanderNameDB)
        {
            First = commanderNameDB.First;
            Last = commanderNameDB.Last;
            IsFemale = commanderNameDB.IsFemale;
        }
    }
}
