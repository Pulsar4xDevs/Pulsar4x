using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class RuinsDB : BaseDataBlob
    {
        /// <summary>
        /// Ruins size descriptors
        /// </summary>
        public enum RSize : byte
        {
            NoRuins,
            Outpost,
            Settlement,
            Colony,
            City,
            Count
        }

        /// <summary>
        /// Ruins Quality descriptors
        /// </summary>
        public enum RQuality : byte
        {
            Destroyed,
            Ruined,
            PartiallyIntact,
            Intact,
            MultipleIntact,
            Count
        }

        /// <summary>
        /// How many ruins are on this world. or something.
        /// </summary>
        [JsonProperty]
        public uint RuinCount { get; internal set; }

        /// <summary>
        /// What kinds of things should be found in this ruin? including sophistication of killbots?
        /// </summary>
        [JsonProperty]
        public int RuinTechLevel { get; internal set; }

        /// <summary>
        /// How big are these ruins?
        /// </summary>
        [JsonProperty]
        public RSize RuinSize { get; internal set; }

        /// <summary>
        /// What shape are these ruins in?
        /// </summary>
        [JsonProperty]
        public RQuality RuinQuality { get; internal set; }

        /// <summary>
        /// Empty constructor for RuinsDataBlob.
        /// </summary>
        public RuinsDB()
        {
            RuinCount = 0;
            RuinTechLevel = 0;
            RuinSize = RSize.Count;
            RuinQuality = RQuality.Count;
        }

        /// <summary>
        /// Constructor for RuinsDataBlob.
        /// </summary>
        /// <param name="ruinCount"></param>
        /// <param name="ruinTechLevel"> What kinds of things should be found in this ruin? including sophistication of killbots?</param>
        /// <param name="ruinSize">How big are these ruins?</param>
        /// <param name="ruinQuality"> What shape are these ruins in?</param>
        public RuinsDB(uint ruinCount, int ruinTechLevel, RSize ruinSize, RQuality ruinQuality)
        {
            RuinCount = ruinCount;
            RuinTechLevel = ruinTechLevel;
            RuinSize = ruinSize;
            RuinQuality = ruinQuality;
        }
        
        public override object Clone()
        {
            return new RuinsDB(RuinCount, RuinTechLevel, RuinSize, RuinQuality);
        }
    }
}
