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

        public ComponentInstancesDB(ComponentInstancesDB db)
        {
            SpecificInstances = new Dictionary<Entity, List<Entity>>(db.SpecificInstances);
        }

        public override object Clone()
        {
            return new ComponentInstancesDB(this);
        }
    }


}
