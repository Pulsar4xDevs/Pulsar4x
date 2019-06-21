using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class NewtonBalisticDB : BaseDataBlob
    {
        [JsonProperty]
        public Vector3 CurrentSpeed { get; set; } = new Vector3();
        [JsonProperty]
        public Guid TargetGuid;
        [JsonProperty]
        public DateTime CollisionDate;

        /// <summary>
        /// necessary for serializer
        /// </summary>
        public NewtonBalisticDB()
        {

        }

        public NewtonBalisticDB(Guid TgtGuid,DateTime cDate)
        {
            TargetGuid = TgtGuid;
            CollisionDate = cDate;
        }


        public NewtonBalisticDB(NewtonBalisticDB db)
        {
            CurrentSpeed = db.CurrentSpeed;
            TargetGuid = db.TargetGuid;
            CollisionDate = db.CollisionDate;
        }

        public override object Clone()
        {
            return new NewtonBalisticDB(this);
        }
    }
}
