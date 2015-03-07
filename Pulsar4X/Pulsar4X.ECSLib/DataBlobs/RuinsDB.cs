using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.DataBlobs
{
    class RuinsDB : BaseDataBlob
    {
        public enum RSize
        {
            NoRuins,
            Outpost,
            Settlement,
            Colony,
            City,
            Count
        }

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
        /// What kinds of things should be found in this ruin? including sophistication of killbots?
        /// </summary>
        public readonly int RuinTechLevel;

        /// <summary>
        /// How big are these ruins?
        /// </summary>
        public readonly RSize RuinSize;

        /// <summary>
        /// What shape are these ruins in?
        /// </summary>
        public readonly RQuality RuinQuality;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityID"></param>
        /// <param name="ruinTechLevel"> What kinds of things should be found in this ruin? including sophistication of killbots?</param>
        /// <param name="ruinSize">How big are these ruins?</param>
        /// <param name="ruinQuality"> What shape are these ruins in?</param>
        public RuinsDB(int ruinTechLevel, RSize ruinSize, RQuality ruinQuality)
        { }
    }
}
