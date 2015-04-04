using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.DataBlobs
{
    public enum CommanderTypes
    {
        Navy,
        Ground,
        Civilian
    }

    public class CommanderDB
    {
        public CommanderNameDB Name;

        public int Rank; //maybe rank/title should be part of name

        public CommanderTypes Type;

        public Dictionary<string, int> Bonuses; //bonuses should be blobs of thier own maybe.
    }
}
