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
        Dictionary<Entity, List<ComponentInstance>> _specificInstances = new Dictionary<Entity, List<ComponentInstance>>();
        [PublicAPI]
        Dictionary<Entity, List<ComponentInstance>> SpecificInstances { get; set; }
        
        public ComponentInstancesDB() { }

        public ComponentInstancesDB(List<Entity> componentDesigns)
        {
            foreach (var item in componentDesigns)
            {
                ComponentInstance instance = new ComponentInstance(item);
                if (!SpecificInstances.ContainsKey(item))
                    SpecificInstances.Add(item, new List<ComponentInstance>() { instance });
                else
                {
                    SpecificInstances[item].Add(instance);
                }
            }
        }

        public ComponentInstancesDB(ComponentInstancesDB db)
        {
            SpecificInstances = new Dictionary<Entity, List<ComponentInstance>>(db.SpecificInstances);
        }

        public override object Clone()
        {
            return new ComponentInstancesDB(this);
        }
    }




    public class ComponentInstance
    {
        public Entity DesignEntity { get; internal set; }
        public bool IsEnabled { get; internal set; }
        public int HTKRemaining { get; internal set; }
        public object StateInfo { get; internal set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="designEntity">The Component Entity, MUST have a ComponentInfoDB</param>
        /// <param name="isEnabled">whether the component is enabled on construction. default=true</param>
        internal ComponentInstance(Entity designEntity, bool isEnabled = true)
        {
            if (designEntity.HasDataBlob<ComponentInfoDB>())
            {
                ComponentInfoDB componentInfo = designEntity.GetDataBlob<ComponentInfoDB>();
                DesignEntity = designEntity;
                IsEnabled = isEnabled;
                HTKRemaining = componentInfo.HTK;
            }
            else
                throw new Exception("designEntity Must contain a ComponentInfoDB");
        }

    }
}
