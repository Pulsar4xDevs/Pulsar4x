using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public class NewtonBalisticDB : BaseDataBlob
    {

        public Vector4 CurrentSpeed { get; set; } = new Vector4();

        /// <summary>
        /// Should these have the _ in front or is that for private members?
        /// </summary>
        public Guid TargetGuid;
        public DateTime CollisionDate;

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
