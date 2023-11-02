using Newtonsoft.Json;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;

namespace Pulsar4X.Datablobs
{
    public class FactionTechDB : BaseDataBlob
    {
        [PublicAPI]
        [JsonProperty]
        public int ResearchPoints { get; internal set; }

        public List<(Scientist scientist, Entity atEntity)> AllScientists { get; internal set; } = new List<(Scientist, Entity)>();

        public FactionTechDB() { }

        public FactionTechDB(FactionTechDB techDB)
        {
            ResearchPoints = techDB.ResearchPoints;
            AllScientists = techDB.AllScientists;
        }

        public override object Clone()
        {
            return new FactionTechDB(this);
        }
    }
}
