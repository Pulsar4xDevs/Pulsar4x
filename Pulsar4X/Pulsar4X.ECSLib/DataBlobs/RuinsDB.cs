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
        public uint RuinCount
        {
            get { return _ruinCount; }
            internal set { _ruinCount = value; }
        }
        [JsonProperty]
        private uint _ruinCount;

        /// <summary>
        /// What kinds of things should be found in this ruin? including sophistication of killbots?
        /// </summary>
        public int RuinTechLevel
        {
            get { return _ruinTechLevel; }
            internal set { _ruinTechLevel = value; }
        }
        [JsonProperty]
        private int _ruinTechLevel;

        /// <summary>
        /// How big are these ruins?
        /// </summary>
        public RSize RuinSize
        {
            get { return _ruinSize; }
            internal set { _ruinSize = value; }
        }
        [JsonProperty]
        private RSize _ruinSize;

        /// <summary>
        /// What shape are these ruins in?
        /// </summary>
        public RQuality RuinQuality
        {
            get { return _ruinQuality; }
            internal set { _ruinQuality = value; }
        }
        [JsonProperty]
        private RQuality _ruinQuality;

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

        public RuinsDB(RuinsDB ruinsDB)
            : this(ruinsDB.RuinCount, ruinsDB.RuinTechLevel, ruinsDB.RuinSize, ruinsDB.RuinQuality)
        {
        }

        public override object Clone()
        {
            return new RuinsDB(this);
        }
    }
}
