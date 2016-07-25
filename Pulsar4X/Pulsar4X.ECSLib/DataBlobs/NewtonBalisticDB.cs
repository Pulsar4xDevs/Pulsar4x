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

        public NewtonBalisticDB()
        { }


        public NewtonBalisticDB(NewtonBalisticDB db)
        {
            CurrentSpeed = db.CurrentSpeed;
        }

        public override object Clone()
        {
            return new NewtonBalisticDB(this);
        }
    }
}
