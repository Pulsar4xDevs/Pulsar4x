using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public enum CommandType
    {
        Invalid = 0,
        Crewed,
        Flag,
    }

    public class CommandAtbDB : BaseDataBlob
    {
        [JsonProperty]
        public CommandType CommandType { get; internal set; }

        public CommandAtbDB(CommandType commandType)
        {
            CommandType = commandType;
        }
        
        public override object Clone()
        {
            return new CommandAtbDB(CommandType);
        }
    }
}
