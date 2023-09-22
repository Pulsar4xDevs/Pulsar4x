using Newtonsoft.Json;

namespace Pulsar4X.Datablobs
{
    public class RuinsDB
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
    }
}
