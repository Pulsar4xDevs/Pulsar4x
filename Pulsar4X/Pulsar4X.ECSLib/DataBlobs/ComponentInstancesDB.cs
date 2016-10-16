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
        public PrIwObsDict<Entity, PrIwObsList<Entity>> SpecificInstances { get; internal set; } = new PrIwObsDict<Entity, PrIwObsList<Entity>>();


        // list of components and where in the ship they are.
        public Dictionary<Entity, double> ComponentDictionary { get; set; }

        public ComponentInstancesDB()
        {
            ComponentDictionary = new Dictionary<Entity, double>();
        }



        // @todo: check to see if the instances are simply copied over, or duplicated
        public ComponentInstancesDB(ComponentInstancesDB db)
        {
            SpecificInstances = new PrIwObsDict<Entity, PrIwObsList<Entity>>(db.SpecificInstances);
            ComponentDictionary = new Dictionary<Entity, double>(db.ComponentDictionary);
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
