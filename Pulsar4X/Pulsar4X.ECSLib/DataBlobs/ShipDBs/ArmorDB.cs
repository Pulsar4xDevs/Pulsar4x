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

        public ArmorDB(ArmorDefDB armorDef, BitArray[] armorStatus)
        {
        }

        public ArmorDB()
        {
            
        }

        public ArmorDB(ArmorDB armorDB)
        {
            if(armorDB.ArmorDef != null)
                ArmorDef = new ArmorDefDB() {Name = armorDB.ArmorDef.Name, Strenght = armorDB.ArmorDef.Strenght};
            if (armorDB.ArmorStatus != null)
            {
                ArmorStatus = new BitArray[armorDB.ArmorStatus.Length];
                armorDB.ArmorStatus.CopyTo(ArmorStatus, 0);
            }
        }

        public override object Clone()
        {
            return new ArmorDB(this);
        }
    }
}
