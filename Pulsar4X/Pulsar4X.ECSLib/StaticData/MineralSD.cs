using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{
    public struct MineralSD : ISerializable
    {
        public string Name;
        public string Description;
        public Guid ID;
        public Dictionary<BodyType, double> Abundance;

        public MineralSD(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString("Name");
            Description = info.GetString("Description");
            ID = (Guid)info.GetValue("ID", typeof(Guid));

            var list = (List<KeyValuePair<BodyType, double>>)info.GetValue("Abundance", typeof(List<KeyValuePair<BodyType, double>>));

            Abundance = new Dictionary<BodyType, double>();
            foreach (var item in list)
            {
                Abundance.Add(item.Key, item.Value);
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", Name);
            info.AddValue("Description", Description);
            info.AddValue("ID", ID);
            info.AddValue("Abundance", Abundance.ToList());
        }
    }
}
