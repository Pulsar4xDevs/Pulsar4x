using Newtonsoft.Json;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Datablobs
{
    public class ResearchPointsAtbDB : IComponentDesignAttribute
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

        public object Clone()
        {
            return new ResearchPointsAtbDB(this);
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (parentEntity.TryGetDatablob<EntityResearchDB>(out var db))
            {
                db.Labs.Add(componentInstance, _pointsPerEconTick);
            }
            else
            {
                db = new EntityResearchDB();
                db.Labs.Add(componentInstance, _pointsPerEconTick);
                parentEntity.SetDataBlob(db);
            }

            if(!parentEntity.HasDataBlob<TeamsHousedDB>())
                parentEntity.SetDataBlob(new TeamsHousedDB());
        }

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (parentEntity.TryGetDatablob<EntityResearchDB>(out var db))
            {
                db.Labs.Remove(componentInstance);
                if(db.Labs.Count == 0)
                {
                    parentEntity.RemoveDataBlob<EntityResearchDB>();
                }
            }
        }

        public string AtbName()
        {
            return "Research Points";
        }

        public string AtbDescription()
        {

            return _pointsPerEconTick.ToString();
        }
    }
}