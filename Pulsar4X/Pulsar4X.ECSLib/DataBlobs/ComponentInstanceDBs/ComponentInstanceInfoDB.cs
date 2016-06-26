using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{

    public class ComponentInstanceInfoDB : BaseDataBlob
    {
        [JsonProperty]
        public Entity ParentEntity { get; internal set; }
        [JsonProperty]
        public Entity DesignEntity { get; internal set; }
        [JsonProperty]
        public bool IsEnabled { get; internal set; }
        [JsonProperty]
        public int HTKRemaining { get; internal set; }
        [JsonProperty]
        [Obsolete]
        public object StateInfo { get; internal set; }

        public ComponentInstanceInfoDB() { }

        /// <summary>
        /// Constructor for a componentInstance.
        /// ComponentInstance stores component specific data such as hit points remaining etc.
        /// </summary>
        /// <param name="designEntity">The Component Entity, MUST have a ComponentInfoDB</param>
        /// <param name="isEnabled">whether the component is enabled on construction. default=true</param>
        public ComponentInstanceInfoDB(Entity designEntity, bool isEnabled = true)
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


        public ComponentInstanceInfoDB(ComponentInstanceInfoDB instance)
        {
            DesignEntity = instance.DesignEntity;
            IsEnabled = instance.IsEnabled;
            HTKRemaining = instance.HTKRemaining;
            StateInfo = instance.StateInfo;
        }

        public override object Clone()
        {
            return new ComponentInstanceInfoDB(this);
        }
    }
}
