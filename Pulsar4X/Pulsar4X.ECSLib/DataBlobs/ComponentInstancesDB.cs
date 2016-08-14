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
        [JsonProperty]
        [PublicAPI]
        public Dictionary<Entity, List<Entity>> SpecificInstances { get; internal set; } = new Dictionary<Entity, List<Entity>>();

        public ComponentInstancesDB() { }

        //public ComponentInstancesDB(List<Entity> componentDesigns)
        //{
        //    foreach (var item in componentDesigns)
        //    {
        //        ComponentInstanceInfoDB instance = new ComponentInstanceInfoDB(item);
        //        if (!SpecificInstances.ContainsKey(item))
        //            SpecificInstances.Add(item, new List<Entity>() { instance });
        //        else
        //        {
        //            SpecificInstances[item].Add(instance);
        //        }
        //    }
        //}


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

        public override object Clone()
        {
            return new ComponentInstancesDB(this);
        }
    }


}
