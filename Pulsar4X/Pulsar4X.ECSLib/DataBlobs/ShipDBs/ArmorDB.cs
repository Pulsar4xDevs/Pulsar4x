using System.Collections;

namespace Pulsar4X.ECSLib
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
