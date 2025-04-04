using Newtonsoft.Json;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Technology
{
    public class ResearchPointsAtbDB : IComponentDesignAttribute
    {
        [JsonProperty]
        private int _pointsPerEconTick;
        public int PointsPerEconTick { get { return _pointsPerEconTick; } internal set { _pointsPerEconTick = value; } }

        [JsonProperty]
        private decimal _costPerDay;
        public decimal CostPerDay
        {
            get { return _costPerDay; }
            internal set { _costPerDay = value; }
        }

        [JsonProperty]
        private string _bonusCategory;
        public string BonusCategory
        {
            get { return _bonusCategory; }
            internal set { _bonusCategory = value; }
        }

        public ResearchPointsAtbDB()
        {
        }

        /// <summary>
        /// Casts to int.
        /// </summary>
        /// <param name="pointsPerEconTick"></param>
        public ResearchPointsAtbDB(double pointsPerEconTick, double costPerDay, string bonusCategory)
        {
            _pointsPerEconTick = (int)pointsPerEconTick;
            _costPerDay = (decimal)costPerDay;
            _bonusCategory = bonusCategory;
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
            // Create a new entity
            var entity = Entity.Create(parentEntity.FactionOwnerID);

            // Update the componentinstance
            componentInstance.SpawnedEntityId = entity.Id;

            // Add the new entity to the system
            parentEntity.Manager.AddEntity(entity);

            // Setup the ResearcherDB
            var researcherDB = new ResearcherDB(componentInstance.Design)
            {
                PointsPerDay = new(_pointsPerEconTick),
                CostPerDay = new(_costPerDay),
                LocationId = parentEntity.Id
            };

            // By default the bonus category gets a 10% bonus
            if(!string.IsNullOrEmpty(_bonusCategory))
                researcherDB.BonusCategories.Add(_bonusCategory, 0.1);

            // Finally add the db to the entity
            entity.SetDataBlob(researcherDB);
            entity.SetDataBlob(new OrderableDB());

            // Calculate the initial modifiers
            ResearchProcessor.RefreshCostModifiers(researcherDB);
            ResearchProcessor.RefreshPointModifiers(researcherDB, null, null);
        }

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            // Try to remove the entity
            if(parentEntity.Manager.TryGetEntityById(componentInstance.SpawnedEntityId, out var entity))
            {
                parentEntity.Manager.TagEntityForRemoval(entity);
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