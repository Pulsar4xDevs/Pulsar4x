using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class ResearchPointsAtbDB : BaseDataBlob
    {
        [JsonProperty]
        private int _pointsPerEconTick;        
        public int PointsPerEconTick { get { return _pointsPerEconTick; } internal set { _pointsPerEconTick = value; } }

        public ResearchPointsAtbDB()
        {
        }

        /// <summary>
        /// Casts to int.
        /// </summary>
        /// <param name="pointsPerEconTick"></param>
        public ResearchPointsAtbDB(double pointsPerEconTick)
        {
            _pointsPerEconTick = (int)pointsPerEconTick;
        }

        public ResearchPointsAtbDB(ResearchPointsAtbDB db)
        {

        }

        public override object Clone()
        {
            return new ResearchPointsAtbDB(this);
        }
    }
}