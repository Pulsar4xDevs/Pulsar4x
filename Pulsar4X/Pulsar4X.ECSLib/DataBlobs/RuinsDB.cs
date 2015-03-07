using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.DataBlobs
{
    class RuinsDB : BaseDataBlob
    {
        /// <summary>
        /// Ruins size descriptors
        /// </summary>
        public enum RSize
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
        public enum RQuality
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
        public uint RuinCount;

        /// <summary>
        /// What kinds of things should be found in this ruin? including sophistication of killbots?
        /// </summary>
        public int RuinTechLevel;

        /// <summary>
        /// How big are these ruins?
        /// </summary>
        public RSize RuinSize;

        /// <summary>
        /// What shape are these ruins in?
        /// </summary>
        public RQuality RuinQuality;

        /// <summary>
        /// Constructor for RuinsDataBlob
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
    }
}
