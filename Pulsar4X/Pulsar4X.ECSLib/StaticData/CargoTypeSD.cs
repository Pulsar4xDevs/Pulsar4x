using System;

namespace Pulsar4X.ECSLib
{
    [StaticData(true, IDPropertyName = "ID")]
    public struct CargoTypeSD
    {
        public string Name;
        public string Description;
        public Guid ID;
    }

    public interface ICargoable
    {
        Guid ID { get; }
        Guid CargoTypeID { get;  }
        float Mass { get;  }
    }
}


