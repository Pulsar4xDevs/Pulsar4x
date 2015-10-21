using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class ResearchPointsAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        private int _pointsPerEconTick;        
        public int PointsPerEconTick { get { return _pointsPerEconTick; } internal set { _pointsPerEconTick = value; } }

        public ResearchPointsAbilityDB()
        {
        }

        /// <summary>
        /// Casts to int.
        /// </summary>
        /// <param name="pointsPerEconTick"></param>
        public ResearchPointsAbilityDB(double pointsPerEconTick)
        {
            _pointsPerEconTick = (int)pointsPerEconTick;
        }

        public ResearchPointsAbilityDB(ResearchPointsAbilityDB db)
        {

        }

        public override object Clone()
        {
            return new ResearchPointsAbilityDB(this);
        }
    }
}