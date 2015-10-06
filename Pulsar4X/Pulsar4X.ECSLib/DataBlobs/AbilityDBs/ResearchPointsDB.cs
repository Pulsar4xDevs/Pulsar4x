using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class ResearchPointsDB : BaseDataBlob
    {
        [JsonProperty]
        private int _pointsPerEconTick;        
        public int PointsPerEconTick { get { return _pointsPerEconTick; } internal set { _pointsPerEconTick = value; } }

        public ResearchPointsDB()
        {
        }

        /// <summary>
        /// Casts to int.
        /// </summary>
        /// <param name="pointsPerEconTick"></param>
        public ResearchPointsDB(double pointsPerEconTick)
        {
            _pointsPerEconTick = (int)pointsPerEconTick;
        }

        public ResearchPointsDB(ResearchPointsDB db)
        {

        }

        public override object Clone()
        {
            return new ResearchPointsDB(this);
        }
    }
}