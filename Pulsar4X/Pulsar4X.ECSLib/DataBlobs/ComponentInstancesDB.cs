using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public class ComponentInstancesDB : BaseDataBlob
    {
        /// <summary>
        /// Key is the component design entity
        /// Value is a list of specific instances of that component design, that entity will hold info on damage, cooldown etc.
        /// </summary>
        [JsonProperty]
        [PublicAPI]
        public Dictionary<Entity, List<Entity>> SpecificInstances { get; internal set; } = new Dictionary<Entity, List<Entity>>();
        
        public ComponentInstancesDB() { }



        // @todo: check to see if the instances are simply copied over, or duplicated
        public ComponentInstancesDB(ComponentInstancesDB db)
        {
            //SpecificInstances = new Dictionary<Entity, List<Entity>>(db.SpecificInstances);
            if (SpecificInstances == null)
                SpecificInstances = new Dictionary<Entity, List<Entity>>();

            SpecificInstances.Clear();

            List<Entity> instanceList = new List<Entity>();
            Entity newKey;
            Entity newEntity;

            foreach (KeyValuePair<Entity, List<Entity>> kvp in db.SpecificInstances)
            {
                instanceList = new List<Entity>();

                newKey = kvp.Key.Clone(kvp.Key.Manager);

                foreach(Entity entity in kvp.Value)
                {
                    newEntity = entity.Clone(entity.Manager);
                    newEntity.GetDataBlob<ComponentInstanceInfoDB>().DesignEntity = newKey;
                    instanceList.Add(newEntity);
                    
                }

                SpecificInstances.Add(newKey, instanceList);
            }
        }

        /// <summary>
        /// this is a somewhat shallow clone. it does not clone the referenced component instance entities!!!
        /// </summary>
        /// <returns></returns>
        public override object Clone()
        {
            return new ComponentInstancesDB(this);
        }
    }
}
