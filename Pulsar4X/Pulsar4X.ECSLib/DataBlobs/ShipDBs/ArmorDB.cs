using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.DataBlobs
{
    public class ArmorDefDB 
    {
        public string Name;

        public double Strenght;

        public ArmorDefDB()
        {
        }
    }

    public class ArmorDB : BaseDataBlob
    {
        public ArmorDefDB ArmorDef;

        public BitArray[] ArmorStatus;

        public ArmorDB(ArmorDB armorDef, BitArray[] armorStatus)
        {
        }

        public ArmorDB()
        {
            
        }

    }
}
